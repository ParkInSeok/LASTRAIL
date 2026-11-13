import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { planCrystalChange, readCrystalPolicy, walletId } from "./crystal-wallet";
import { storeOrderInsert, verifiedStoreProof } from "./crystal-orders";
import type { CrystalPurchaseVerifier } from "./crystal-orders";
import { objectivePeriods } from "./recurring-policy";

export type CrystalAction = { kind: "EXCHANGE"; policyVersion: string; ticketCount: number }
  | { kind: "SHOP_EXCHANGE"; policyVersion: string; ticketCount: number; offer: "NORMAL" | "DAILY"; expectedDayStart: number }
  | { kind: "PURCHASE"; policyVersion: string; productId: string; receipt: string };

type CrystalOptions = { db: D1Database; accountId: string; environment: "LOCAL" | "DEV" | "PRD"; verifier?: CrystalPurchaseVerifier };
export async function claimCrystals(input: CrystalOptions, action: CrystalAction, key: string): Promise<Record<string, unknown>> {
  try { return await claimCrystalsAttempt(input, action, key); }
  catch (error) {
    // A concurrent identical request may commit after our initial receipt read
    // but before a quota, balance, policy or clock check rejects this attempt.
    const replay = await input.db.prepare("SELECT request_hash,response_json FROM crystal_operations WHERE account_id=? AND operation=? AND idempotency_key=?")
      .bind(input.accountId, `crystal_${action.kind.toLowerCase()}`, key).first<{ request_hash: string; response_json: string }>();
    if (replay) {
      if (replay.request_hash !== await sha256Hex(canonicalJson(action))) throw new ActivityError(409, "idempotency_conflict");
      return JSON.parse(replay.response_json);
    }
    throw error;
  }
}
async function claimCrystalsAttempt(input: { db: D1Database; accountId: string; environment: "LOCAL" | "DEV" | "PRD";
  verifier?: CrystalPurchaseVerifier }, action: CrystalAction, key: string): Promise<Record<string, unknown>> {
  const { db, accountId } = input;
  const operation = `crystal_${action.kind.toLowerCase()}`;
  const hash = await sha256Hex(canonicalJson(action));
  for (let attempt = 0; attempt < 3; attempt++) {
    const replay = await db.prepare("SELECT request_hash,response_json FROM crystal_operations WHERE account_id=? AND operation=? AND idempotency_key=?")
      .bind(accountId, operation, key).first<{ request_hash: string; response_json: string }>();
    if (replay) {
      if (replay.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
      return JSON.parse(replay.response_json);
    }
    const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0) AS value FROM account_mutations WHERE account_id=?")
      .bind(accountId).first<{ value: number }>())!.value + 1;
    const policy = await readCrystalPolicy(db);
    if (policy.policyVersion !== action.policyVersion) throw new ActivityError(409, "crystal_policy_changed");
    const operationId = walletId("crystal_op"), mutationId = walletId("mutation"), economyId = walletId("economy");
    const now = Date.now();
    const before: D1PreparedStatement[] = [], after: D1PreparedStatement[] = [];
    let plan;
    if (action.kind === "EXCHANGE" || action.kind === "SHOP_EXCHANGE") {
      const exchange = policy.ticketExchange;
      if (!exchange) throw new ActivityError(503, "crystal_exchange_unconfigured");
      if (!Number.isInteger(action.ticketCount) || action.ticketCount < 1 || action.ticketCount > exchange.maxTickets) throw new ActivityError(400, "contract_violation");
      const daily = action.kind === "SHOP_EXCHANGE" && action.offer === "DAILY";
      if (action.kind === "SHOP_EXCHANGE" && exchange.crystalsPerTicket !== 100) throw new ActivityError(503, "shop_launch_price_unconfigured");
      if (daily) {
        const period = objectivePeriods(now);
        if (action.expectedDayStart !== period.dayStart) throw new ActivityError(409, "shop_day_changed");
        if (action.ticketCount !== 1) throw new ActivityError(400, "contract_violation");
        if (await db.prepare("SELECT 1 FROM shop_daily_ticket_claims WHERE account_id=? AND offer_family='COMMAND_TICKET_DAILY' AND day_start=?").bind(accountId, period.dayStart).first()) throw new ActivityError(409, "shop_daily_limit_reached");
        before.push(db.prepare("INSERT INTO shop_day_guard(valid) SELECT CASE WHEN CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)>=? AND CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)<? THEN 1 ELSE 0 END").bind(period.dayStart, period.dayEnd));
        after.push(db.prepare("INSERT INTO shop_daily_ticket_claims(account_id,offer_family,day_start,operation_id) VALUES(?,'COMMAND_TICKET_DAILY',?,?)").bind(accountId, period.dayStart, operationId), db.prepare("DELETE FROM shop_day_guard"));
      }
      const cost = action.ticketCount * (daily ? 50 : exchange.crystalsPerTicket);
      if (!Number.isSafeInteger(cost)) throw new ActivityError(400, "contract_violation");
      const quantity = (await db.prepare("SELECT quantity FROM account_items WHERE account_id=? AND item_id=?")
        .bind(accountId, exchange.ticketItemId).first<{ quantity: number }>())?.quantity ?? 0;
      const quantityAfter = quantity + action.ticketCount;
      if (!Number.isSafeInteger(quantityAfter)) throw new ActivityError(409, "reward_balance_overflow");
      plan = await planCrystalChange({ db, accountId, operationId, mutationId, operation, now, grants: [],
        spend: { quantity: cost, reason: "TICKET_EXCHANGE", downstreamRef: operationId }, expectedPolicyVersion: action.policyVersion,
        request: { key, hash }, response: { ticketsGranted: action.ticketCount, ticketItemId: exchange.ticketItemId, ticketBalanceAfter: quantityAfter,
          ...(action.kind === "SHOP_EXCHANGE" ? { offer: action.offer, cost, dayStart: action.expectedDayStart, dailyRemaining: daily ? 0 : null } : {}) } });
      after.push(
        db.prepare(`INSERT INTO account_items (account_id,item_id,quantity,updated_at) VALUES (?,?,?,?)
          ON CONFLICT(account_id,item_id) DO UPDATE SET quantity=account_items.quantity+?,updated_at=excluded.updated_at`)
          .bind(accountId, exchange.ticketItemId, action.ticketCount, now, action.ticketCount),
        db.prepare(`INSERT INTO item_ledger (id,account_id,mutation_id,item_id,delta,quantity_after,reason,source_type,source_id,created_at)
          VALUES (?,?,?,?,?,?,'CRYSTAL_TICKET_EXCHANGE','CRYSTAL',?,?)`)
          .bind(walletId("item_ledger"), accountId, mutationId, exchange.ticketItemId, action.ticketCount, quantityAfter, operationId, now),
      );
    } else {
      if (!policy.purchase || !input.verifier) throw new ActivityError(503, "crystal_purchase_verifier_unconfigured");
      const product = policy.purchase.products.find((product) => product.productId === action.productId);
      if (!product) throw new ActivityError(400, "purchase_product_mismatch");
      const expected = { accountId, environment: input.environment, provider: policy.purchase.provider,
        appId: policy.purchase.appId, productId: product.productId };
      let result;
      try { result = await input.verifier({ ...expected, receipt: action.receipt, now }); }
      catch { throw new ActivityError(503, "purchase_verification_unavailable"); }
      if (result?.state !== "PURCHASED") throw new ActivityError(403, "purchase_not_verified");
      const proof = verifiedStoreProof(result, expected, Date.now());
      const order = storeOrderInsert(db, proof, "CRYSTAL", operationId);
      before.push(order.statement);
      plan = await planCrystalChange({ db, accountId, operationId, mutationId, operation, now, expectedPolicyVersion: action.policyVersion,
        grants: [{ quantity: product.quantity, sourceType: "STORE_PURCHASE", sourceRef: operationId, orderId: order.orderId }],
        request: { key, hash }, response: { purchasedQuantity: product.quantity, purchaseLinked: true } });
    }
    const statements = [
      db.prepare("INSERT INTO account_mutations (id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES (?,?,?,?,?,?)")
        .bind(mutationId, accountId, sequence, operation, key, now),
      db.prepare("INSERT INTO economy_transactions (id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES (?,?,?,?,'CRYSTAL',?,?)")
        .bind(economyId, accountId, mutationId, operation, operationId, now),
      ...before, ...plan.statements, ...after,
      db.prepare("INSERT INTO reward_transactions (id,account_id,run_id,mutation_id,balance_config_version,reward_json,created_at) VALUES (?,?,NULL,?,NULL,?,?)")
        .bind(operationId, accountId, mutationId, JSON.stringify(plan.response), now),
      db.prepare("INSERT INTO idempotency_records (id,account_id,idempotency_key,operation,result_ref,created_at) VALUES (?,?,?,?,?,?)")
        .bind(walletId("idem"), accountId, key, operation, operationId, now),
    ];
    try { await db.batch(statements); return plan.response; }
    catch (error) {
      if (/CHECK constraint failed: valid=1/iu.test(String(error))) throw new ActivityError(409, "shop_day_changed");
      if (/UNIQUE constraint failed/iu.test(String(error))) {
        if (attempt < 2) continue;
        throw new ActivityError(409, "crystal_write_conflict");
      }
      if (/CHECK constraint failed.*singleton_id/iu.test(String(error))) throw new ActivityError(409, "crystal_policy_changed");
      throw error;
    }
  }
  throw new ActivityError(409, "crystal_write_conflict");
}
