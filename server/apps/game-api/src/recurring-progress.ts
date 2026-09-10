import { ActivityError } from "./activity-policy";
import { walletId } from "./crystal-wallet";
import { objectivePeriods, objectiveDayMs, ordinaryQuestRewards, pass50Rewards, parseRecurringDefinition, recurringObjectiveSpecs, type RecurringDefinition } from "./recurring-policy";
import { readObjectiveProgress } from "./recurring-objective-state";
export type ObjectiveEvent = { id: number; kind: string; occurred_at: number; status: string; ends: number; waves: number };
export type ObjectiveAward = { id: string; account_id: string; policy_id: string; system: string; period_id: string; objective: string; earned_at: number; period_end: number; xp: number; rewards_json: string };
export const recurringRetryable = (error: unknown) => /UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(error));
export async function recurringSequence(db: D1Database, accountId: string) {
  return (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(accountId).first<{ value: number }>())!.value;
}
export async function recurringDefinition(db: D1Database, id: string) {
  const d = await db.prepare("SELECT * FROM recurring_definitions WHERE id=?").bind(id).first<RecurringDefinition>();
  if (!d) throw new ActivityError(503, "recurring_not_configured");
  return { definition: d, policy: parseRecurringDefinition(d) };
}
function deriveAwards(accountId: string, definitions: RecurringDefinition[], events: ObjectiveEvent[]) {
  const awards = new Map<string, ObjectiveAward>();
  for (const d of definitions) {
    parseRecurringDefinition(d);
    const counts = new Map<string, { login: number; ends: number; waves: number }>();
    for (const e of events) {
      if (e.status !== "ELIGIBLE" || e.occurred_at < d.start_at || e.occurred_at >= d.end_at) continue;
      const p = objectivePeriods(e.occurred_at), groups = [{ id: `D:${p.dayStart}`, end: p.dayEnd, weekly: false }];
      if (d.system === "QUESTS") groups.push({ id: `W:${p.weekStart}`, end: p.weekStart + 7 * objectiveDayMs, weekly: true });
      else if (p.passWeek !== null) groups.push({ id: `W:${p.startAt}:${p.passWeek}`, end: p.startAt + (p.passWeek + 1) * 7 * objectiveDayMs, weekly: true });
      for (const group of groups) {
        const total = counts.get(group.id) ?? { login: 0, ends: 0, waves: 0 };
        total.login += e.kind === "LOGIN" ? 1 : 0; total.ends += e.ends; total.waves += e.waves; counts.set(group.id, total);
        const checks = recurringObjectiveSpecs.filter(s => s.frequency === (group.weekly ? "WEEKLY" : "DAILY")).map(s => ({ ...s, count: total[s.metric] }));
        for (const check of checks) {
          if (check.count < check.target || (d.system === "QUESTS" && check.id === "D1")) continue;
          const identity = `${d.system}:${group.id}:${check.id}`;
          if (!awards.has(identity)) awards.set(identity, { id: walletId("objective_award"), account_id: accountId, policy_id: d.id, system: d.system,
            period_id: group.id, objective: check.id, earned_at: e.occurred_at, period_end: group.end,
            xp: d.system === "PASS50" ? check.xp : 0, rewards_json: JSON.stringify(d.system === "QUESTS" ? ordinaryQuestRewards[check.id] : []) });
        }
      }
    }
  }
  return [...awards.values()];
}
export async function syncRecurringAccount(db: D1Database, accountId: string) {
  for (let attempt = 0; attempt < 4; attempt++) {
    const sequence = await recurringSequence(db, accountId);
    const events = (await db.prepare("SELECT id,kind,occurred_at,status,ends,waves FROM objective_events WHERE account_id=? ORDER BY occurred_at,id").bind(accountId).all<ObjectiveEvent>()).results;
    const definitions = (await db.prepare("SELECT * FROM recurring_definitions ORDER BY start_at,id").all<RecurringDefinition>()).results;
    const maxEvent = Math.max(0, ...events.map(e => e.id));
    const existing = (await db.prepare("SELECT * FROM recurring_awards WHERE account_id=?").bind(accountId).all<ObjectiveAward>()).results;
    const identities = new Set(existing.map(a => `${a.system}:${a.period_id}:${a.objective}`));
    const next = deriveAwards(accountId, definitions, events).filter(a => !identities.has(`${a.system}:${a.period_id}:${a.objective}`));
    const mutationId = walletId("objective_sync"), now = Date.now();
    const writes = [db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,?,?,'OBJECTIVE_SYNC',?)").bind(mutationId, accountId, sequence, now),
      db.prepare(`INSERT INTO recurring_guard(valid) SELECT CASE WHEN
        COALESCE((SELECT MAX(id) FROM objective_events WHERE account_id=?),0)=?
        AND (SELECT COUNT(*) FROM recurring_definitions)=? THEN 1 ELSE 0 END`).bind(accountId, maxEvent, definitions.length)];
    for (const a of next) writes.push(db.prepare("INSERT INTO recurring_awards(id,account_id,policy_id,system,period_id,objective,earned_at,period_end,xp,rewards_json) VALUES(?,?,?,?,?,?,?,?,?,?)")
      .bind(a.id, accountId, a.policy_id, a.system, a.period_id, a.objective, a.earned_at, a.period_end, a.xp, a.rewards_json));
    writes.push(db.prepare("INSERT INTO recurring_sync_state(account_id,last_event_id) VALUES(?,?) ON CONFLICT(account_id) DO UPDATE SET last_event_id=excluded.last_event_id").bind(accountId, maxEvent), db.prepare("DELETE FROM recurring_guard"));
    try { await db.batch(writes); return { accepted: true, addedObjectives: next.length, lastEventId: maxEvent }; }
    catch (error) { if (recurringRetryable(error) && attempt < 3) continue; if (recurringRetryable(error)) throw new ActivityError(409, "recurring_state_changed"); throw error; }
  }
  throw new ActivityError(409, "recurring_state_changed");
}
export async function passEarnedXp(db: D1Database, accountId: string, policyId: string) {
  return (await db.prepare("SELECT COALESCE(SUM(xp),0) AS value FROM recurring_awards WHERE account_id=? AND policy_id=? AND system='PASS50'").bind(accountId, policyId).first<{ value: number }>())!.value;
}
export async function readRecurringState(db: D1Database, accountId: string) {
  const now = Date.now();
  const definitions = (await db.prepare("SELECT * FROM recurring_definitions ORDER BY start_at,id").all<RecurringDefinition>()).results;
  const awards = (await db.prepare("SELECT * FROM recurring_awards WHERE account_id=? ORDER BY earned_at,id").bind(accountId).all<ObjectiveAward>()).results;
  const grants = (await db.prepare("SELECT right_key,response_json FROM recurring_grants WHERE account_id=?").bind(accountId).all<{ right_key: string; response_json: string }>()).results;
  const mail = (await db.prepare("SELECT * FROM quest_mail WHERE account_id=? ORDER BY sent_at,award_id").bind(accountId).all<{ award_id: string; sent_at: number; expires_at: number }>()).results;
  const eventStatus = (await db.prepare("SELECT status,COUNT(*) AS count FROM objective_events WHERE account_id=? GROUP BY status").bind(accountId).all<{ status: string; count: number }>()).results;
  const fence = await db.prepare(`SELECT COALESCE((SELECT MAX(id) FROM objective_events WHERE account_id=?1),0) AS latest,
    COALESCE((SELECT last_event_id FROM recurring_sync_state WHERE account_id=?1),0) AS applied`).bind(accountId).first<{ latest: number; applied: number }>();
  const policies = [];
  for (const d of definitions) {
    const p = parseRecurringDefinition(d);
    const earnedXp = awards.filter(a => a.policy_id === d.id).reduce((sum, a) => sum + a.xp, 0);
    const entitlement = d.system === "PASS50" ? await db.prepare(`SELECT e.order_id FROM pass50_entitlements e JOIN verified_store_orders o ON o.id=e.order_id AND o.account_id=e.account_id
      WHERE e.account_id=? AND e.policy_id=? AND o.revoked_at IS NULL`).bind(accountId, d.id).first() : null;
    const rows = d.system === "PASS50" ? pass50Rewards().map(row => ({ ...row,
      cosmeticItemId: row.cosmeticSlot ? p.cosmetics[row.cosmeticSlot as keyof typeof p.cosmetics] : null,
      freeState: grants.some(g => g.right_key === `pass:${d.start_at}:${row.level}:FREE`) ? "CLAIMED" : earnedXp >= row.xp ? "EARNED" : "LOCKED",
      premiumState: grants.some(g => g.right_key === `pass:${d.start_at}:${row.level}:PREMIUM`) ? "CLAIMED" : !entitlement ? "PREMIUM_REQUIRED" : earnedXp >= row.xp ? "EARNED" : "LOCKED" })) : [];
    const pendingGrants = rows.some(row => row.freeState === "EARNED" || row.premiumState === "EARNED");
    policies.push({ policyId: d.id, system: d.system, startAt: d.start_at, endAt: d.end_at, active: d.start_at <= now && now < d.end_at,
      purchaseEndsAt: d.system === "PASS50" ? d.end_at - objectiveDayMs : null,
      xp: Math.min(5000, earnedXp), earnedXp, level: Math.min(50, Math.floor(earnedXp / 100)), premium: entitlement !== null,
      cosmeticBindingsComplete: Object.values(p.cosmetics).every(Boolean), purchaseConfigured: p.purchase !== null, rewardRows: rows,
      settlementStatus: d.system !== "PASS50" || now < d.end_at ? "NOT_ENDED" : fence!.latest !== fence!.applied ? "SOURCE_SYNC_PENDING" : pendingGrants ? "SETTLEMENT_PENDING" : "SETTLED_FOR_RECORDED_SOURCES" });
  }
  const objectiveProgress = await readObjectiveProgress(db, accountId);
  return { accepted: true, version: "recurring-v1", serverTime: now, periods: objectivePeriods(now), policies, objectiveProgress,
    progressSyncPending: fence!.latest !== fence!.applied,
    capabilities: { ordinaryOperationBinding: "REQUIRES_EXPLICIT_VERSIONED_MAPPING", storyDefeat: "UNSUPPORTED_COST_CONTRACT_PENDING",
      cooperativeParticipantProducer: "NOT_IMPLEMENTED", cooperativeProgress: "UNKNOWN_FOR_UNLINKED_ORIGINAL_PARTICIPANTS", existingOwnerFinishProducer: true },
    eventStatus, awards: awards.map(a => ({ ...a, rewards: JSON.parse(a.rewards_json), rewards_json: undefined,
      status: a.system === "PASS50" ? "XP_APPLIED" : grants.some(g => g.right_key === `quest:${a.id}`) ? "CLAIMED" : mail.some(m => m.award_id === a.id) ? "MAILED" : a.period_end <= now ? "MAIL_PENDING" : "EARNED" })),
    mail: mail.map(m => ({ ...m, status: grants.some(g => g.right_key === `quest:${m.award_id}`) ? "CLAIMED" : now >= m.expires_at ? "EXPIRED" : "CLAIMABLE" })),
    grants: grants.map(g => JSON.parse(g.response_json)),
  };
}
