import { ActivityError } from "../../game-api/src/activity-policy";
import { readCrystalPolicy, readCrystalLots, crystalBalances } from "../../game-api/src/crystal-wallet";
import { discoverMonthlyAttendance, readMonthlyAttendance } from "../../game-api/src/monthly-attendance";
import { objectivePeriods, parseRecurringDefinition, type RecurringDefinition } from "../../game-api/src/recurring-policy";

type Section<T> = { availability: "AVAILABLE"; reason: null; data: T } |
  { availability: "UNCONFIGURED" | "UNAVAILABLE"; reason: string; data: null };
const missing = (code: string): never => { throw new ActivityError(503, code); };
async function section<T>(read: () => Promise<T>): Promise<Section<T>> {
  try { return { availability: "AVAILABLE", reason: null, data: await read() }; }
  catch (error) {
    const reason = error instanceof ActivityError ? error.code : "source_read_failed";
    const unconfigured = ["crystal_policy_unconfigured", "monthly_attendance_unconfigured", "recurring_policy_unconfigured"].includes(reason);
    return { availability: unconfigured ? "UNCONFIGURED" : "UNAVAILABLE", reason, data: null };
  }
}
async function activeDefinition(db: D1Database, system: "PASS50" | "QUESTS", now: number) {
  const rows = (await db.prepare("SELECT id,system,start_at,end_at,policy_json FROM recurring_definitions WHERE system=? AND start_at<=? AND end_at>? ORDER BY id LIMIT 2")
    .bind(system, now, now).all<RecurringDefinition>()).results;
  if (rows.length === 0) return missing("recurring_policy_unconfigured");
  if (rows.length !== 1) return missing("recurring_policy_ambiguous");
  const definition = rows[0]!;
  return { definition, policy: parseRecurringDefinition(definition) };
}
async function progressFence(db: D1Database, accountId: string) {
  const row = await db.prepare(`SELECT COALESCE((SELECT MAX(id) FROM objective_events WHERE account_id=?1),0) AS latest,
    COALESCE((SELECT last_event_id FROM recurring_sync_state WHERE account_id=?1),0) AS applied`).bind(accountId).first<{ latest: number; applied: number }>();
  if (!row) return missing("source_read_failed");
  return { progressSyncPending: row.latest !== row.applied, progressSource: "PERSISTED_AWARDS_NO_SYNC_ON_ADMIN_READ" as const };
}
const cap = 100;

/** SELECT-only support projection. Never calls synchronization, claims, checkout,
 * or game auth as another player. No raw JSON, order/session/receipt credentials.
 * Sections are independent live reads, not a cross-section transaction snapshot. */
export async function readAdminPlayerActivities(db: D1Database, accountId: string, now = Date.now()) {
  const exists = await db.prepare("SELECT id FROM accounts WHERE id=?").bind(accountId).first<{ id: string }>();
  if (!exists) return null;
  const shop = await section(async () => {
    const policy = await readCrystalPolicy(db), periods = objectivePeriods(now);
    const wallet = crystalBalances(await readCrystalLots(db, accountId, policy.currencyId));
    const claimed = await db.prepare("SELECT 1 FROM shop_daily_ticket_claims WHERE account_id=? AND offer_family='COMMAND_TICKET_DAILY' AND day_start=?")
      .bind(accountId, periods.dayStart).first();
    const ticketsConfigured = policy.ticketExchange?.crystalsPerTicket === 100;
    return { policyVersion: policy.policyVersion, currencyId: policy.currencyId, wallet, dayStart: periods.dayStart, nextResetAt: periods.dayEnd,
      ticketOffersConfigured: ticketsConfigured, dailyRemaining: ticketsConfigured ? claimed ? 0 : 1 : null,
      ticketUnavailableReason: ticketsConfigured ? null : "shop_launch_price_unconfigured",
      purchaseConfigured: policy.purchase !== null, configuredProductCount: policy.purchase?.products.length ?? 0,
      purchaseVerificationAvailable: null, purchaseVerificationReason: "GAME_RUNTIME_NOT_OBSERVED", cashPrice: null, cashPriceSource: "STORE_REQUIRED",
      materialShopAvailability: "UNCONFIGURED" };
  });
  const monthlyAttendance = await section(async () => {
    const discovery = await discoverMonthlyAttendance(db, accountId);
    if (discovery.httpStatus !== 200) return missing(discovery.body.errorCode ?? "monthly_attendance_unavailable");
    if (!discovery.body.activityId) return missing("monthly_attendance_unconfigured");
    const current = await readMonthlyAttendance({ db, accountId, activityId: discovery.body.activityId });
    return { activityId: current.activityId, policyVersion: current.policyVersion, cycleId: current.cycleId,
      active: current.active, canAttend: discovery.body.canAttend, unavailableReason: current.unavailableReason,
      revision: current.revision, completedCells: current.completedCells, todayAttended: current.todayAttended,
      recoveryUsed: current.recovery.used, recoverableCount: current.recovery.recoverableCount,
      cells: current.cells.map(row => ({ cell: row.cell, claimed: row.receipt !== null, source: row.receipt?.source ?? null,
        dayIndex: row.receipt?.dayIndex ?? null, unavailableReason: row.unavailableReason })) };
  });
  const pass = await section(async () => {
    const { definition: d, policy } = await activeDefinition(db, "PASS50", now);
    const xp = await db.prepare("SELECT COALESCE(SUM(xp),0) AS value FROM recurring_awards WHERE account_id=? AND policy_id=? AND system='PASS50'")
      .bind(accountId, d.id).first<{ value: number }>();
    if (!xp || !Number.isSafeInteger(xp.value) || xp.value < 0) return missing("pass_progress_invalid");
    const entitlement = await db.prepare(`SELECT 1 FROM pass50_entitlements e JOIN verified_store_orders o ON o.id=e.order_id AND o.account_id=e.account_id
      WHERE e.account_id=? AND e.policy_id=? AND o.revoked_at IS NULL`).bind(accountId, d.id).first();
    return { policyId: d.id, startAt: d.start_at, endAt: d.end_at, earnedXp: xp.value, xp: Math.min(5000, xp.value), level: Math.min(50, Math.floor(xp.value / 100)),
      premium: entitlement !== null, purchaseConfigured: policy.purchase !== null, cosmeticBindingsComplete: Object.values(policy.cosmetics).every(Boolean),
      ...await progressFence(db, accountId) };
  });
  const ordinaryQuests = await section(async () => {
    const { definition: d } = await activeDefinition(db, "QUESTS", now);
    const rows = (await db.prepare(`SELECT a.id,a.objective,a.period_id,a.earned_at,a.period_end,
      EXISTS(SELECT 1 FROM recurring_grants g WHERE g.account_id=a.account_id AND g.right_key='quest:'||a.id) AS claimed,
      EXISTS(SELECT 1 FROM quest_mail m WHERE m.account_id=a.account_id AND m.award_id=a.id) AS mailed
      FROM recurring_awards a WHERE a.account_id=? AND a.policy_id=? AND a.system='QUESTS' ORDER BY a.earned_at DESC,a.id LIMIT 101`)
      .bind(accountId, d.id).all<{ id: string; objective: string; period_id: string; earned_at: number; period_end: number; claimed: number; mailed: number }>()).results;
    return { policyId: d.id, startAt: d.start_at, endAt: d.end_at, limit: cap, hasMore: rows.length > cap, ...await progressFence(db, accountId),
      items: rows.slice(0, cap).map(row => ({ awardId: row.id, objectiveId: row.objective, periodId: row.period_id, earnedAt: row.earned_at, periodEnd: row.period_end,
        status: row.claimed ? "CLAIMED" : row.mailed ? "MAILED" : row.period_end <= now ? "MAIL_PENDING" : "EARNED" })) };
  });
  const mail = await section(async () => {
    const rows = (await db.prepare(`SELECT m.award_id,m.sent_at,m.expires_at,a.policy_id,a.objective,
      EXISTS(SELECT 1 FROM recurring_grants g WHERE g.account_id=m.account_id AND g.right_key='quest:'||m.award_id) AS claimed
      FROM quest_mail m JOIN recurring_awards a ON a.id=m.award_id AND a.account_id=m.account_id AND a.system='QUESTS'
      WHERE m.account_id=? ORDER BY m.sent_at DESC,m.award_id LIMIT 101`).bind(accountId)
      .all<{ award_id: string; sent_at: number; expires_at: number; policy_id: string; objective: string; claimed: number }>()).results;
    return { limit: cap, hasMore: rows.length > cap, claimabilitySource: "RECORDED_RIGHT_AND_EXPIRY_NOT_MUTATION_PREFLIGHT",
      items: rows.slice(0, cap).map(row => ({ awardId: row.award_id, policyId: row.policy_id, objectiveId: row.objective,
        sentAt: row.sent_at, expiresAt: row.expires_at, status: row.claimed ? "CLAIMED" : now >= row.expires_at ? "EXPIRED" : "CLAIMABLE" })) };
  });
  return { version: "admin-player-activities-v1", accountId, observedAt: now, consistency: "INDEPENDENT_LIVE_READS", readOnly: true,
    shop, monthlyAttendance, pass, ordinaryQuests, mail };
}
