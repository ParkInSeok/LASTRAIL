import { recurringMetricValue, objectivePeriods, objectiveDayMs, recurringObjectiveSpecs, ordinaryQuestRewards, parseRecurringDefinition, type RecurringDefinition } from "./recurring-policy";
type Event = { id: number; kind: string; occurred_at: number; status: string; ends: number; waves: number };
type Award = { id: string; policy_id: string; period_id: string; objective: string; claimed: number };
// One read statement makes the new counters, rights and applied cursor coherent.
// This does not mutate/sync, or certify missing cooperative participant sources.
export async function readObjectiveProgress(db: D1Database, accountId: string) {
  const snapshot = (await db.prepare(`SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now,
    COALESCE((SELECT last_event_id FROM recurring_sync_state WHERE account_id=?1),0) AS applied,
    (SELECT json_group_array(json_object('id',id,'system',system,'start_at',start_at,'end_at',end_at,'policy_json',policy_json)) FROM recurring_definitions) AS definitions,
    (SELECT json_group_array(json_object('id',id,'kind',kind,'occurred_at',occurred_at,'status',status,'ends',ends,'waves',COALESCE(b_completed_waves,waves))) FROM objective_events WHERE account_id=?1) AS events,
    (SELECT json_group_array(json_object('id',a.id,'policy_id',a.policy_id,'period_id',a.period_id,'objective',a.objective,
      'claimed',EXISTS(SELECT 1 FROM recurring_grants g WHERE g.account_id=?1 AND g.right_key='quest:'||a.id))) FROM recurring_awards a WHERE a.account_id=?1) AS awards`)
    .bind(accountId).first<{ now: number; applied: number; definitions: string; events: string; awards: string }>())!;
  const definitions = JSON.parse(snapshot.definitions) as RecurringDefinition[], events = JSON.parse(snapshot.events) as Event[], awards = JSON.parse(snapshot.awards) as Award[];
  const p = objectivePeriods(snapshot.now);
  const systems = ["QUESTS", "PASS50"].map(system => {
    const definition = definitions.find(d => d.system === system && d.start_at <= snapshot.now && snapshot.now < d.end_at);
    if (!definition) return { system, policyId: null, status: "NOT_CONFIGURED_FOR_CURRENT_PERIOD", objectives: [] };
    parseRecurringDefinition(definition);
    const objectives = recurringObjectiveSpecs.filter(s => system === "PASS50" || s.id !== "D1").map(spec => {
      const weekly = spec.frequency === "WEEKLY", scheduled = !(system === "PASS50" && weekly && p.passWeek === null);
      const start = weekly ? system === "QUESTS" ? p.weekStart : p.startAt + (p.passWeek ?? 0) * 7 * objectiveDayMs : p.dayStart;
      const end = start + (weekly ? 7 : 1) * objectiveDayMs;
      const periodId = weekly ? system === "QUESTS" ? `W:${start}` : `W:${p.startAt}:${p.passWeek}` : `D:${start}`;
      const sources = scheduled ? events.filter(e => e.occurred_at >= start && e.occurred_at < end && e.occurred_at <= snapshot.now) : [];
      const eligible = sources.filter(e => recurringMetricValue(e,spec.metric)>0);
      const observedCurrent = eligible.reduce((sum, e) => sum + recurringMetricValue(e,spec.metric), 0);
      const award = scheduled ? awards.find(a => a.policy_id === definition.id && a.period_id === periodId && a.objective === spec.id) : undefined;
      const observedTargetReached = scheduled && observedCurrent >= spec.target;
      const syncPending = scheduled && (eligible.some(e => e.id > snapshot.applied && recurringMetricValue(e,spec.metric)>0) || (observedTargetReached && !award));
      const unknownReasons = spec.metric === "login" ? [] : ["COOPERATIVE_PARTICIPANT_PRODUCER_MISSING",
        ...(sources.some(e => e.status === "OPERATION_BINDING_MISSING") ? ["OPERATION_BINDING_MISSING"] : []),
        ...(sources.some(e => e.status === "UNSUPPORTED_STORY_DEFEAT") ? ["STORY_DEFEAT_COST_CONTRACT_PENDING"] : [])];
      return { objectiveId: spec.id, frequency: spec.frequency, metric: spec.metric, target: spec.target,
        periodId: scheduled ? periodId : null, periodStartAt: scheduled ? start : null, periodEndAt: scheduled ? end : null,
        calendar: weekly ? system === "QUESTS" ? "MONDAY_04_KST" : "SEASON_RELATIVE_7_DAYS" : "DAILY_04_KST",
        current: scheduled && !unknownReasons.length ? observedCurrent : null, observedCurrent: scheduled ? observedCurrent : null,
        currentScope: spec.metric === "login" ? "SERVER_RECORDED_LOGIN" : "RECORDED_ELIGIBLE_OWNER_FINISH",
        coverage: !scheduled ? "NOT_SCHEDULED" : unknownReasons.length ? "PARTIAL_UNKNOWN" : "COMPLETE_FOR_RECORDED_LOGIN",
        unknownReasons: scheduled ? unknownReasons : [], observedTargetReached, syncPending,
        awardId: award?.id ?? null, status: !scheduled ? "NOT_SCHEDULED" : award ? system === "PASS50" ? "XP_APPLIED" : award.claimed ? "CLAIMED" : "EARNED"
          : observedTargetReached ? "SYNC_PENDING" : unknownReasons.length ? "UNKNOWN" : "IN_PROGRESS",
        xp: system === "PASS50" ? spec.xp : 0, rewards: system === "QUESTS" ? ordinaryQuestRewards[spec.id] : [],
      };
    });
    return { system, policyId: definition.id, status: "ACTIVE", objectives };
  });
  return { version: "objective-progress-v1", serverTime: snapshot.now, appliedEventId: snapshot.applied,
    latestEventId: events.reduce((max, e) => Math.max(max, e.id), 0), systems };
}
