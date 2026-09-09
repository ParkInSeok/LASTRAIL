import { z } from "zod";
import { ActivityError, activityId } from "./activity-policy";

const amount = z.number().int().positive().max(2147483647);
const policySchema = z.object({
  currencyId: activityId,
  ticketExchange: z.object({ ticketItemId: z.literal("cur_command_ticket"), crystalsPerTicket: amount, maxTickets: amount }).strict().nullable(),
  purchase: z.object({ provider: z.enum(["GOOGLE_PLAY", "APP_STORE"]), appId: z.string().min(1).max(200),
    products: z.array(z.object({ productId: z.string().min(1).max(200), quantity: amount }).strict()).min(1).max(100)
      .refine((products) => new Set(products.map((p) => p.productId)).size === products.length),
  }).strict().nullable(),
}).strict();
export type CrystalPolicy = z.infer<typeof policySchema> & { policyVersion: string };
export type CrystalSourceType = "ATTENDANCE" | "PASS_FREE" | "PASS_PREMIUM" | "STORE_PURCHASE" | "LAUNCH_ENTITLEMENT";
export type CrystalGrant = { quantity: number; sourceType: CrystalSourceType; sourceRef: string; orderId: string | null };
export type CrystalLot = { id: string; source: "FREE" | "PAID"; remaining: number; granted: number; order_id: string | null };
export const walletId = (prefix: string) => `${prefix}_${crypto.randomUUID()}`;

export async function readCrystalPolicy(db: D1Database): Promise<CrystalPolicy> {
  const row = await db.prepare(`SELECT p.id, p.policy_json FROM crystal_policy_active a JOIN crystal_policies p ON p.id = a.policy_id WHERE a.singleton_id = 1`)
    .first<{ id: string; policy_json: string }>();
  if (!row) throw new ActivityError(503, "crystal_policy_unconfigured");
  try { return { ...policySchema.parse(JSON.parse(row.policy_json)), policyVersion: row.id }; }
  catch { throw new ActivityError(503, "crystal_policy_invalid"); }
}

export async function readCrystalLots(db: D1Database, accountId: string, currencyId: string): Promise<CrystalLot[]> {
  const result = await db.prepare(`SELECT id, source, remaining, granted, order_id FROM crystal_lots
    WHERE account_id = ? AND currency_id = ? AND remaining > 0 ORDER BY CASE source WHEN 'FREE' THEN 0 ELSE 1 END, created_at, id`)
    .bind(accountId, currencyId).all<CrystalLot>();
  return result.results;
}

function safe(value: number): number {
  if (!Number.isSafeInteger(value) || value < 0) throw new ActivityError(409, "crystal_amount_out_of_range");
  return value;
}

export function crystalBalances(lots: CrystalLot[]) {
  const freeBalance = safe(lots.filter((lot) => lot.source === "FREE").reduce((sum, lot) => sum + lot.remaining, 0));
  const paidBalance = safe(lots.filter((lot) => lot.source === "PAID").reduce((sum, lot) => sum + lot.remaining, 0));
  return { freeBalance, paidBalance, totalBalance: safe(freeBalance + paidBalance) };
}

export async function readCrystalWallet(db: D1Database, accountId: string) {
  const policy = await readCrystalPolicy(db);
  return { currencyId: policy.currencyId, policyVersion: policy.policyVersion,
    ...crystalBalances(await readCrystalLots(db, accountId, policy.currencyId)),
    spendOrder: ["FREE", "PAID"], ticketExchange: policy.ticketExchange, purchaseConfigured: policy.purchase !== null };
}

// Read/plan only. The caller MUST insert its account mutation and this plan in
// one D1 batch. A unique account sequence fences all eligibility/balance reads.
export async function planCrystalChange(input: {
  db: D1Database; accountId: string; operationId: string; mutationId: string;
  operation: string; now: number; grants: CrystalGrant[];
  spend?: { quantity: number; reason: "TICKET_EXCHANGE" | "ATTENDANCE_RECOVERY"; downstreamRef: string };
  expectedPolicyVersion?: string;
  request?: { key: string; hash: string };
  response?: Record<string, unknown>;
}) {
  const { db, accountId, operationId, now } = input;
  const policy = await readCrystalPolicy(db);
  if (input.expectedPolicyVersion !== undefined && input.expectedPolicyVersion !== policy.policyVersion) throw new ActivityError(409, "crystal_policy_changed");
  const lots = await readCrystalLots(db, accountId, policy.currencyId);
  const before = crystalBalances(lots);
  let left = input.spend?.quantity ?? 0;
  safe(left);
  if (left > before.totalBalance) throw new ActivityError(409, "crystal_insufficient_balance");
  const statements: D1PreparedStatement[] = [];
  let freeSpent = 0, paidSpent = 0;
  for (const lot of lots) {
    if (left === 0) break;
    const spent = Math.min(left, lot.remaining);
    left -= spent;
    if (lot.source === "FREE") freeSpent += spent; else paidSpent += spent;
    const remaining = lot.remaining - spent;
    statements.push(
      db.prepare("UPDATE crystal_lots SET remaining = remaining - ? WHERE id = ? AND account_id = ?").bind(spent, lot.id, accountId),
      db.prepare(`INSERT INTO crystal_ledger (id,lot_id,account_id,operation_id,delta,remaining_after,reason,downstream_ref,created_at)
        VALUES (?,?,?,?,?,?,?,?,?)`).bind(walletId("crystal_ledger"), lot.id, accountId, operationId, -spent, remaining, input.spend!.reason, input.spend!.downstreamRef, now),
    );
  }
  let freeGranted = 0, paidGranted = 0;
  const grantLines = [];
  for (const grant of input.grants) {
    if (!amount.safeParse(grant.quantity).success) throw new ActivityError(400, "crystal_grant_invalid");
    const source = grant.sourceType === "STORE_PURCHASE" ? "PAID" : "FREE";
    if (source === "PAID") paidGranted += grant.quantity; else freeGranted += grant.quantity;
    const lotId = walletId("crystal_lot");
    statements.push(
      db.prepare(`INSERT INTO crystal_lots (id,account_id,currency_id,source,source_type,source_ref,order_id,granted,remaining,operation_id,created_at)
        VALUES (?,?,?,?,?,?,?,?,?,?,?)`).bind(lotId, accountId, policy.currencyId, source, grant.sourceType, grant.sourceRef, grant.orderId, grant.quantity, grant.quantity, operationId, now),
      db.prepare(`INSERT INTO crystal_ledger (id,lot_id,account_id,operation_id,delta,remaining_after,reason,downstream_ref,created_at)
        VALUES (?,?,?,?,?,?,?,?,?)`).bind(walletId("crystal_ledger"), lotId, accountId, operationId, grant.quantity, grant.quantity, grant.sourceType, grant.sourceRef, now),
    );
    grantLines.push({ kind: "CRYSTAL", id: policy.currencyId, quantity: grant.quantity, source, purchaseLinked: grant.orderId !== null });
  }
  const freeBalance = safe(before.freeBalance - freeSpent + freeGranted);
  const paidBalance = safe(before.paidBalance - paidSpent + paidGranted);
  const response = { accepted: true, operationId, currencyId: policy.currencyId, policyVersion: policy.policyVersion,
    freeBalance, paidBalance, totalBalance: safe(freeBalance + paidBalance), freeSpent, paidSpent, freeGranted, paidGranted,
    ...input.response };
  statements.unshift(db.prepare(`INSERT INTO crystal_operations (id,account_id,policy_id,mutation_id,operation,idempotency_key,request_hash,response_json,created_at)
    VALUES (?,?,?,?,?,?,?,?,?)`).bind(operationId, accountId, policy.policyVersion, input.mutationId, input.operation,
    input.request?.key ?? null, input.request?.hash ?? null, JSON.stringify(response), now));
  // Prevent switching the active wallet policy after reads and before writes.
  statements.push(db.prepare(`INSERT INTO crystal_policy_active (singleton_id,policy_id)
    VALUES (CASE WHEN (SELECT policy_id FROM crystal_policy_active WHERE singleton_id = 1) = ? THEN 1 ELSE 0 END, ?)
    ON CONFLICT(singleton_id) DO NOTHING`).bind(policy.policyVersion, policy.policyVersion));
  return { statements, response, grantLines, policy };
}
