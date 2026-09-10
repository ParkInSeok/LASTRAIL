import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { walletId } from "./crystal-wallet";
import { verifiedStoreProof, storeOrderInsert, type StoreIdentity, type StoreProof } from "./crystal-orders";
import { recurringDefinition, recurringRetryable, recurringSequence } from "./recurring-progress";
import { requestReceipt, requestReplay } from "./recurring-grants";
export type Pass50Verifier = (input: StoreIdentity & { policyId: string; intentId: string; receipt: string; now: number }) => Promise<(StoreProof & { policyId: string; intentId: string }) | null>;
type PurchaseAction = { kind: "INTENT"; policyId: string } | { kind: "RECONCILE"; policyId: string; intentId: string; receipt: string };
export async function purchasePass50(o: { db: D1Database; accountId: string; environment: "LOCAL" | "DEV" | "PRD"; verifier?: Pass50Verifier }, action: PurchaseAction, key: string) {
  const { db, accountId } = o, operation = `pass50_${action.kind.toLowerCase()}`;
  const hash = await sha256Hex(canonicalJson(action)); // Only opaque receipt digest persists.
  for (let attempt = 0; attempt < 4; attempt++) {
    const replay = await requestReplay(db, accountId, operation, key, hash); if (replay) return replay;
    try {
      const sequence = await recurringSequence(db, accountId), { definition: d, policy: p } = await recurringDefinition(db, action.policyId);
      if (d.system !== "PASS50") throw new ActivityError(400, "recurring_kind_mismatch");
      if (!p.purchase || !o.verifier) throw new ActivityError(503, "pass_purchase_unconfigured");
      if (!Object.values(p.cosmetics).every(Boolean)) throw new ActivityError(503, "pass_cosmetic_binding_missing");
      const now = Date.now(), writes: D1PreparedStatement[] = [];
      let response: Record<string, unknown>;
      if (action.kind === "INTENT") {
        const cutoff = d.end_at - 86400000;
        if (now < d.start_at || now >= cutoff) throw new ActivityError(409, "pass_purchase_closed");
        if (await db.prepare("SELECT 1 FROM pass50_entitlements WHERE account_id=? AND policy_id=?").bind(accountId, d.id).first()) throw new ActivityError(409, "pass_already_owned");
        const intentId = walletId("pass_intent"), expiresAt = Math.min(now + 600000, d.end_at);
        writes.push(db.prepare("INSERT INTO recurring_guard(valid) SELECT CASE WHEN CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)<? THEN 1 ELSE 0 END").bind(Math.min(cutoff, expiresAt)),
          db.prepare("INSERT INTO pass_checkout_intents(id,account_id,policy_id,created_at,expires_at) VALUES(?,?,?,?,?)").bind(intentId, accountId, d.id, now, expiresAt));
        response = { accepted: true, policyId: d.id, intentId, expiresAt, purchaseEndsAt: cutoff, seasonEndAt: d.end_at, productId: p.purchase.productId };
      } else {
        const intent = await db.prepare("SELECT created_at,expires_at FROM pass_checkout_intents WHERE id=? AND account_id=? AND policy_id=?").bind(action.intentId, accountId, d.id).first<{ created_at: number; expires_at: number }>();
        if (!intent) throw new ActivityError(403, "pass_intent_not_found");
        const expected = { ...p.purchase, accountId, environment: o.environment };
        let raw: Awaited<ReturnType<Pass50Verifier>>;
        try { raw = await o.verifier({ ...expected, policyId: d.id, intentId: action.intentId, receipt: action.receipt, now }); }
        catch { throw new ActivityError(503, "purchase_verification_unavailable"); }
        if (!raw || raw.policyId !== d.id || raw.intentId !== action.intentId) throw new ActivityError(403, "purchase_not_verified");
        const proof = verifiedStoreProof(raw, expected, Date.now());
        if (intent.created_at >= d.end_at - 86400000 || proof.purchasedAt < intent.created_at || proof.purchasedAt >= intent.expires_at || proof.purchasedAt >= d.end_at) throw new ActivityError(403, "pass_purchase_outside_intent");
        const existing = await db.prepare(`SELECT o.transaction_id,o.revoked_at FROM pass50_entitlements e JOIN verified_store_orders o ON o.id=e.order_id
          WHERE e.account_id=? AND e.policy_id=?`).bind(accountId, d.id).first<{ transaction_id: string; revoked_at: number | null }>();
        if (existing?.revoked_at != null) throw new ActivityError(403, "premium_entitlement_revoked");
        if (existing && existing.transaction_id !== proof.transactionId) throw new ActivityError(409, "pass_already_owned");
        if (!existing) {
          const order = storeOrderInsert(db, proof, "PASS", action.intentId);
          writes.push(order.statement, db.prepare("INSERT INTO pass50_entitlements(account_id,policy_id,intent_id,order_id) VALUES(?,?,?,?)").bind(accountId, d.id, action.intentId, order.orderId));
        }
        response = { accepted: true, policyId: d.id, intentId: action.intentId, premium: true, seasonEndAt: d.end_at, settlementRequired: true, xpGranted: 0 };
      }
      await db.batch([db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,?,?,?)").bind(walletId("pass_purchase_mut"), accountId, sequence, operation, key, Date.now()),
        ...writes, requestReceipt(db, accountId, operation, key, hash, JSON.stringify(response)), db.prepare("DELETE FROM recurring_guard")]);
      return response;
    } catch (error) {
      const replay = await requestReplay(db, accountId, operation, key, hash); if (replay) return replay;
      if (recurringRetryable(error) && attempt < 3) continue;
      if (recurringRetryable(error)) throw new ActivityError(409, "pass_purchase_state_changed");
      throw error;
    }
  }
  throw new ActivityError(409, "pass_purchase_state_changed");
}
