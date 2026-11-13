import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { walletId } from "./crystal-wallet";

type RevocationIdentity = { orderId: string; accountId: string; environment: string; provider: string; appId: string; transactionId: string };
export type StoreRevocationVerifier = (input: RevocationIdentity & { verificationToken: string; now: number }) => Promise<
  (RevocationIdentity & { status: "REFUNDED" | "REVOKED"; verifiedAt: number }) | null>;
const proofSchema = z.object({ orderId: z.string(), accountId: z.string(), environment: z.string(), provider: z.string(), appId: z.string(),
  transactionId: z.string(), status: z.enum(["REFUNDED", "REVOKED"]), verifiedAt: z.number().int().nonnegative() });

// Server-only boundary; intentionally not registered as a public API or webhook.
// Only an authenticated store adapter may supply a verified refund/revocation.
export async function reconcileCrystalOrder(input: { db: D1Database; orderId: string; environment: "LOCAL" | "DEV" | "PRD";
  verificationToken: string; verifier?: StoreRevocationVerifier }): Promise<Record<string, unknown>> {
  const { db, orderId } = input;
  if (!input.verifier) throw new ActivityError(503, "refund_verifier_unconfigured");
  for (let attempt = 0; attempt < 3; attempt++) {
    const existing = await db.prepare("SELECT response_json FROM crystal_order_reconciliations WHERE order_id=?").bind(orderId).first<{ response_json: string }>();
    if (existing) return JSON.parse(existing.response_json);
    const order = await db.prepare("SELECT account_id,environment,provider,app_id,transaction_id,product_kind FROM verified_store_orders WHERE id=?")
      .bind(orderId).first<{ account_id: string; environment: string; provider: string; app_id: string; transaction_id: string; product_kind:string }>();
    if (!order || order.environment !== input.environment) throw new ActivityError(403, "refund_order_invalid");
    if(order.product_kind==='AD_SKIP_PERMANENT')throw new ActivityError(409,'refund_product_kind_mismatch');
    const accountId = order.account_id;
    const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0) AS value FROM account_mutations WHERE account_id=?")
      .bind(accountId).first<{ value: number }>())!.value + 1;
    const expected = { orderId, accountId, environment: order.environment, provider: order.provider, appId: order.app_id, transactionId: order.transaction_id };
    let raw;
    try { raw = await input.verifier({ ...expected, verificationToken: input.verificationToken, now: Date.now() }); }
    catch { throw new ActivityError(503, "refund_verification_unavailable"); }
    const parsed = proofSchema.safeParse(raw);
    const now = Date.now();
    if (!parsed.success || Object.entries(expected).some(([key, value]) => parsed.data[key as keyof RevocationIdentity] !== value)
      || parsed.data.verifiedAt > now || parsed.data.verifiedAt < now - 60000) throw new ActivityError(403, "refund_not_verified");
    const lots = (await db.prepare(`SELECT lot.id,lot.source,lot.granted,lot.remaining,lot.currency_id,op.policy_id
      FROM crystal_lots lot JOIN crystal_operations op ON op.id=lot.operation_id WHERE lot.order_id=? AND lot.account_id=? ORDER BY lot.id`)
      .bind(orderId, accountId).all<{ id: string; source: "FREE" | "PAID"; granted: number; remaining: number; currency_id: string; policy_id: string }>()).results;
    const policyVersions = [...new Set(lots.map((lot) => lot.policy_id))];
    const currencyIds = [...new Set(lots.map((lot) => lot.currency_id))];
    // Historical order/lot provenance owns reconciliation even if new grants or
    // spending have been disabled. A pass without crystal grants has no policy.
    const historicalPolicyId = policyVersions[0] ?? null;
    const sum = (source: "FREE" | "PAID", spent: boolean) => lots.filter((lot) => lot.source === source)
      .reduce((value, lot) => value + (spent ? lot.granted - lot.remaining : lot.remaining), 0);
    const reclaimedFree = sum("FREE", false), reclaimedPaid = sum("PAID", false), spentFree = sum("FREE", true), spentPaid = sum("PAID", true);
    if (![reclaimedFree, reclaimedPaid, spentFree, spentPaid].every(Number.isSafeInteger)) throw new ActivityError(409, "refund_amount_out_of_range");
    const downstream = (await db.prepare(`SELECT ledger.operation_id AS operationId, ledger.reason, SUM(-ledger.delta) AS crystalsSpent
      FROM crystal_ledger ledger JOIN crystal_lots lot ON lot.id=ledger.lot_id
      WHERE lot.order_id=? AND ledger.delta<0 AND ledger.reason IN ('TICKET_EXCHANGE','ATTENDANCE_RECOVERY')
      GROUP BY ledger.operation_id, ledger.reason ORDER BY ledger.operation_id`).bind(orderId)
      .all<{ operationId: string; reason: string; crystalsSpent: number }>()).results;
    const operationId = walletId("crystal_refund"), mutationId = walletId("mutation"), economyId = walletId("economy");
    const response = { accepted: true, operationId, orderId, policyVersions, currencyIds,
      policyVersion: policyVersions.length === 1 ? historicalPolicyId : null, currencyId: currencyIds.length === 1 ? currencyIds[0] : null,
      reclaimedFree, reclaimedPaid, spentFree, spentPaid, needsSupport: spentFree + spentPaid > 0, downstream, enforcement: "NONE" };
    const json = JSON.stringify(response);
    const statements = [
      db.prepare("INSERT INTO account_mutations (id,account_id,sequence,mutation_type,created_at) VALUES (?,?,?,'CRYSTAL_REFUND_RECONCILIATION',?)")
        .bind(mutationId, accountId, sequence, now),
      db.prepare("INSERT INTO economy_transactions (id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES (?,?,?,'CRYSTAL_REFUND_RECONCILIATION','STORE_ORDER',?,?)")
        .bind(economyId, accountId, mutationId, orderId, now),
      db.prepare(`INSERT INTO crystal_operations (id,account_id,policy_id,mutation_id,operation,response_json,created_at)
        VALUES (?,?,?,?,'REFUND',?,?)`).bind(operationId, accountId, historicalPolicyId, mutationId, json, now),
      db.prepare("UPDATE verified_store_orders SET revoked_at=COALESCE(revoked_at,?) WHERE id=?").bind(parsed.data.verifiedAt, orderId),
      db.prepare("UPDATE pass_entitlements SET revoked_at=COALESCE(revoked_at,?) WHERE order_id=?").bind(parsed.data.verifiedAt, orderId),
    ];
    for (const lot of lots) {
      if (lot.remaining === 0) continue;
      statements.push(db.prepare("UPDATE crystal_lots SET remaining=0 WHERE id=? AND account_id=?").bind(lot.id, accountId),
        db.prepare(`INSERT INTO crystal_ledger (id,lot_id,account_id,operation_id,delta,remaining_after,reason,downstream_ref,created_at)
          VALUES (?,?,?,?,?,0,'ORDER_RECONCILIATION',?,?)`).bind(walletId("crystal_ledger"), lot.id, accountId, operationId, -lot.remaining, orderId, now));
    }
    statements.push(
      db.prepare(`INSERT INTO crystal_order_reconciliations (order_id,operation_id,verified_status,verified_at,reclaimed_free,reclaimed_paid,spent_free,spent_paid,needs_support,response_json)
        VALUES (?,?,?,?,?,?,?,?,?,?)`).bind(orderId, operationId, parsed.data.status, parsed.data.verifiedAt, reclaimedFree, reclaimedPaid, spentFree, spentPaid, spentFree + spentPaid > 0 ? 1 : 0, json),
      db.prepare("INSERT INTO reward_transactions (id,account_id,run_id,mutation_id,balance_config_version,reward_json,created_at) VALUES (?,?,NULL,?,NULL,?,?)")
        .bind(operationId, accountId, mutationId, json, now),
    );
    try { await db.batch(statements); return response; }
    catch (error) {
      if (/UNIQUE constraint failed/iu.test(String(error))) {
        if (attempt < 2) continue;
        throw new ActivityError(409, "refund_write_conflict");
      }
      throw error;
    }
  }
  throw new ActivityError(409, "refund_write_conflict");
}
