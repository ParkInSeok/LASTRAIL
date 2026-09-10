import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { walletId } from "./crystal-wallet";
import { planActivityGrant, type ActivityGrantReward } from "./activity-grant-plan";
import { pass50Rewards } from "./recurring-policy";
import { recurringDefinition, recurringRetryable, recurringSequence, passEarnedXp, type ObjectiveAward } from "./recurring-progress";
import { bulkMemberGuard, bulkMemberSuccess, type BulkMember } from "./recurring-bulk-hooks";
export type RecurringClaimAction = { kind: "PASS_LEVEL"; policyId: string; level: number; track: "FREE" | "PREMIUM" }
  | { kind: "QUEST" | "MAIL"; awardId: string };
export async function requestReplay(db: D1Database, accountId: string, operation: string, key: string, hash: string) {
  const r = await db.prepare("SELECT request_hash,response_json FROM recurring_requests WHERE account_id=? AND operation=? AND idempotency_key=?").bind(accountId, operation, key).first<{ request_hash: string; response_json: string }>();
  if (!r) return null;
  if (r.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
  return JSON.parse(r.response_json) as Record<string, unknown>;
}
export function requestReceipt(db: D1Database, accountId: string, operation: string, key: string, hash: string, json: string) {
  return db.prepare("INSERT INTO recurring_requests(account_id,operation,idempotency_key,request_hash,response_json) VALUES(?,?,?,?,?)").bind(accountId, operation, key, hash, json);
}
export async function claimRecurringRight(db: D1Database, accountId: string, action: RecurringClaimAction, key?: string, bulk?: BulkMember) {
  const operation = `recurring_${action.kind.toLowerCase()}`, hash = await sha256Hex(canonicalJson(action));
  for (let attempt = 0; attempt < 4; attempt++) {
    const previous = key ? await requestReplay(db, accountId, operation, key, hash) : null; if (previous) return previous;
    try {
      if (bulk && !(await db.prepare("SELECT 1 FROM recurring_bulk_members WHERE bulk_id=? AND member_index=? AND status='RETRY_PENDING'").bind(bulk.bulkId, bulk.index).first())) throw new ActivityError(409, "bulk_member_finalized");
      const sequence = await recurringSequence(db, accountId), now = Date.now();
      let policyId: string, rightKey: string, rewards: ActivityGrantReward[], orderId: string | null = null;
      let sourceType: "QUEST" | "PASS_FREE" | "PASS_PREMIUM" = "QUEST";
      let award: ObjectiveAward | null = null;
      const guards: D1PreparedStatement[] = bulk ? [bulkMemberGuard(db, accountId, bulk)] : [];
      if (action.kind === "PASS_LEVEL") {
        const { definition: d } = await recurringDefinition(db, action.policyId);
        if (d.system !== "PASS50") throw new ActivityError(400, "recurring_kind_mismatch");
        policyId = d.id; rightKey = `pass:${d.start_at}:${action.level}:${action.track}`;
        const row = pass50Rewards()[action.level - 1];
        if (!row) throw new ActivityError(400, "contract_violation");
        rewards = action.track === "FREE" ? row.free : row.premium;
        sourceType = action.track === "FREE" ? "PASS_FREE" : "PASS_PREMIUM";
        // Existing right replay is checked before today's entitlement/state below.
      } else {
        award = await db.prepare("SELECT * FROM recurring_awards WHERE id=? AND account_id=? AND system='QUESTS'").bind(action.awardId, accountId).first<ObjectiveAward>();
        if (!award) throw new ActivityError(403, "quest_right_not_found");
        policyId = award.policy_id; rightKey = `quest:${award.id}`; rewards = JSON.parse(award.rewards_json) as ActivityGrantReward[];
      }
      const existing = await db.prepare("SELECT response_json FROM recurring_grants WHERE account_id=? AND right_key=?").bind(accountId, rightKey).first<{ response_json: string }>();
      if (existing) {
        const replayWrites = key ? [requestReceipt(db, accountId, operation, key, hash, existing.response_json)] : [];
        if (bulk) replayWrites.push(bulkMemberGuard(db, accountId, bulk), bulkMemberSuccess(db, bulk, existing.response_json, true), db.prepare("DELETE FROM recurring_guard"));
        if (replayWrites.length) await db.batch(replayWrites);
        return JSON.parse(existing.response_json) as Record<string, unknown>;
      }
      if (action.kind === "PASS_LEVEL") {
        const { definition: d, policy: p } = await recurringDefinition(db, policyId);
        if (now < d.start_at || await passEarnedXp(db, accountId, policyId) < action.level * 100) throw new ActivityError(409, "pass_level_not_earned");
        if (action.track === "PREMIUM") {
          const entitled = await db.prepare(`SELECT e.order_id FROM pass50_entitlements e JOIN verified_store_orders o ON o.id=e.order_id AND o.account_id=e.account_id
            WHERE e.account_id=? AND e.policy_id=? AND o.revoked_at IS NULL`).bind(accountId, policyId).first<{ order_id: string }>();
          if (!entitled) throw new ActivityError(403, "premium_entitlement_required");
          orderId = entitled.order_id;
          const slot = pass50Rewards()[action.level - 1]!.cosmeticSlot;
          if (slot) {
            const itemId = p.cosmetics[slot as keyof typeof p.cosmetics];
            if (!itemId) throw new ActivityError(503, "pass_cosmetic_binding_missing");
            rewards = [...rewards, { kind: "ITEM", id: itemId, quantity: 1 }];
          }
          guards.push(db.prepare("INSERT INTO recurring_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM verified_store_orders WHERE id=? AND account_id=? AND revoked_at IS NULL) THEN 1 ELSE 0 END").bind(orderId, accountId));
        }
      } else if (action.kind === "QUEST") {
        if (now >= award!.period_end) throw new ActivityError(409, "quest_mail_pending");
        guards.push(db.prepare(`INSERT INTO recurring_guard(valid) SELECT CASE WHEN
          CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)<? AND NOT EXISTS(SELECT 1 FROM quest_mail WHERE award_id=?) THEN 1 ELSE 0 END`).bind(award!.period_end, award!.id));
      } else {
        const mail = await db.prepare("SELECT sent_at,expires_at FROM quest_mail WHERE award_id=? AND account_id=?").bind(award!.id, accountId).first<{ sent_at: number; expires_at: number }>();
        if (!mail) throw new ActivityError(409, "quest_mail_pending");
        if (now >= mail.expires_at) throw new ActivityError(409, "quest_mail_expired");
        guards.push(db.prepare("INSERT INTO recurring_guard(valid) SELECT CASE WHEN CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)<? THEN 1 ELSE 0 END").bind(mail.expires_at));
      }
      const mutationId = walletId("recurring_claim_mut"), economyId = walletId("economy"), claimId = walletId("recurring_claim");
      const plan = await planActivityGrant({ db, accountId, mutationId, claimId, economyId, rewards, operation, now, sourceType, orderId });
      const response = { accepted: true, rightKey, policyId, claimId, rewardTransactionId: claimId, accountMutationId: mutationId,
        serverClaimedAt: now, rewardLines: plan.lines, wallet: plan.wallet, refreshRequired: true };
      const json = JSON.stringify(response);
      const writes = [db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,?,?,?)").bind(mutationId, accountId, sequence, operation, key ?? null, now),
        ...guards, db.prepare("INSERT INTO economy_transactions(id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES(?,?,?,?,'ACTIVITY',?,?)").bind(economyId, accountId, mutationId, operation, claimId, now),
        ...plan.statements, db.prepare("INSERT INTO reward_transactions(id,account_id,run_id,mutation_id,balance_config_version,reward_json,created_at) VALUES(?,?,NULL,?,NULL,?,?)").bind(claimId, accountId, mutationId, json, now),
        db.prepare("INSERT INTO recurring_grants(account_id,right_key,policy_id,claim_id,response_json) VALUES(?,?,?,?,?)").bind(accountId, rightKey, policyId, claimId, json),
        db.prepare("DELETE FROM recurring_guard")];
      if (key) writes.push(requestReceipt(db, accountId, operation, key, hash, json));
      if (bulk) writes.push(bulkMemberSuccess(db, bulk, json, false));
      await db.batch(writes); return response;
    } catch (error) {
      const completed = key ? await requestReplay(db, accountId, operation, key, hash) : null; if (completed) return completed;
      if (recurringRetryable(error) && attempt < 3) continue;
      if (recurringRetryable(error)) throw new ActivityError(409, "recurring_state_changed");
      throw error;
    }
  }
  throw new ActivityError(409, "recurring_state_changed");
}
export async function sendExpiredQuestMail(db: D1Database, accountId: string) {
  const awards = (await db.prepare(`SELECT a.* FROM recurring_awards a WHERE a.account_id=? AND a.system='QUESTS' AND a.period_end<=?
    AND NOT EXISTS(SELECT 1 FROM quest_mail m WHERE m.award_id=a.id)
    AND NOT EXISTS(SELECT 1 FROM recurring_grants g WHERE g.account_id=a.account_id AND g.right_key='quest:'||a.id)`).bind(accountId, Date.now()).all<ObjectiveAward>()).results;
  for (const a of awards) {
    for (let attempt = 0; attempt < 4; attempt++) {
      const sequence = await recurringSequence(db, accountId);
      try {
        await db.batch([db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,?,?,'QUEST_MAIL_SEND',?)").bind(walletId("mail_mut"), accountId, sequence, Date.now()),
          db.prepare(`INSERT INTO quest_mail(award_id,account_id,sent_at,expires_at)
            SELECT ?1,?2,clock.now,clock.now+1209600000 FROM (SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now) clock
            WHERE clock.now>=?3 AND NOT EXISTS(SELECT 1 FROM recurring_grants WHERE account_id=?2 AND right_key=?4)
            ON CONFLICT(award_id) DO NOTHING`).bind(a.id, accountId, a.period_end, `quest:${a.id}`)]);
        break;
      } catch (error) { if (recurringRetryable(error) && attempt < 3) continue; throw error; }
    }
  }
}
