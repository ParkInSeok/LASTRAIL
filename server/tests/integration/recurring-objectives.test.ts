import { afterEach, describe, expect, it, vi } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import { setup as baseSetup } from "../helpers/departure-fixture";
import { registerRecurringRoutes } from "../../apps/game-api/src/recurring-routes";
import { objectiveFinishEvent } from "../../apps/game-api/src/objective-events";
import { readRecurringState, syncRecurringAccount } from "../../apps/game-api/src/recurring-progress";
import { settleRecurringAccount, runRecurringJobs } from "../../apps/game-api/src/recurring-jobs";
import { sendExpiredQuestMail } from "../../apps/game-api/src/recurring-grants";
import type { Pass50Verifier } from "../../apps/game-api/src/pass50-purchase";
const start = Date.parse("2026-08-31T19:00:00Z"), end = Date.parse("2026-09-30T19:00:00Z"), day = 86400000;
afterEach(() => vi.restoreAllMocks());
async function setup() {
  let now = start;
  vi.spyOn(Date, "now").mockImplementation(() => now);
  const f = await baseSetup();
  const policy = { version: "recurring-v1", cosmetics: { FRAME: "TEST_FRAME", OPERATOR_SKIN: "TEST_OPERATOR_SKIN", TRAIN_SKIN: "TEST_TRAIN_SKIN" }, purchase: { provider: "GOOGLE_PLAY", appId: "TEST_APP", productId: "TEST_PASS" } };
  f.sql.run("INSERT INTO recurring_definitions VALUES('pass-sep','PASS50',?,?,?)", [start, end, JSON.stringify(policy)]);
  f.sql.run("INSERT INTO recurring_definitions VALUES('quests','QUESTS',?,?,?)", [start - day, Date.parse("2026-11-01T19:00:00Z"), JSON.stringify({ ...policy, purchase: null })]);
  f.sql.run("INSERT INTO objective_operation_bindings VALUES('TEST_ORDINARY','TEST_CONTENT','TEST_BINDING','ORDINARY_15')");
  f.sql.run("INSERT INTO crystal_policies VALUES('wallet','{\"currencyId\":\"TEST_CRYSTAL\",\"ticketExchange\":null,\"purchase\":null}')");
  f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'wallet')");
  let verifier: Pass50Verifier = async input => ({ ...input, transactionId: "TEST_STORE_TRANSACTION", purchasedAt: now, verifiedAt: now, revoked: false });
  const env = { ...f.env, PASS50_PURCHASE_VERIFIER: ((input) => verifier(input)) as Pass50Verifier };
  const app = new Hono<{ Bindings: Env }>();
  registerRecurringRoutes(app, async c => c.req.header("Authorization") ? { accountId: c.req.header("Authorization")! } : { status: 401, errorCode: "auth", message: "auth" });
  const request = (path: string, body?: object, key = crypto.randomUUID(), account = "a", method = "POST") => app.request(path, { method,
    headers: { Authorization: account, "Idempotency-Key": key }, ...(body ? { body: JSON.stringify(body) } : {}) }, env);
  const login = (key?: string) => request("/v1/recurring/login", undefined, key);
  const read = () => readRecurringState(f.env.DB, "a");
  const record = async (outcome = "VICTORY", waves = 15, operationId = "TEST_ORDINARY", runId = crypto.randomUUID()) => {
    const mid = crypto.randomUUID(), seq = Number(f.scalar("SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations WHERE account_id='a'"));
    f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,'a',?,'TEST_FINISH',?)", [mid, seq, now]);
    f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES(?,'a',?,1,'COMPLETED',?,?)", [runId, mid, now, now]);
    f.sql.run("INSERT INTO run_final_results(run_id,outcome,cleared_waves,payload_json,submitted_at) VALUES(?,?,?,'{}',?)", [runId, outcome, waves, now]);
    const input = { accountId: "a", runId, operationId, contentVersion: "TEST_CONTENT", outcome, completedWaves: waves, fullCompletion: outcome === "VICTORY" };
    await f.env.DB.batch([await objectiveFinishEvent(f.env.DB, input)]); return input;
  };
  return { ...f, env, request, login, read, record, sync: () => syncRecurringAccount(f.env.DB, "a"),
    set: (d: number, offset = 0) => { now = start + d * day + offset; }, setVerifier: (v: Pass50Verifier) => { verifier = v; } };
}
describe("shared recurring objectives and rights", () => {
  it("lists unmet approved objectives read-only, distinguishing recorded zero from unknown finish totals", async () => {
    const f = await setup(), before = f.sql.export();
    const response = await f.request("/v1/recurring/state", undefined, undefined, "a", "GET");
    expect(response.status).toBe(200); const state = await response.json();
    const [quests, pass] = state.objectiveProgress.systems;
    expect(quests.objectives.map((o: { objectiveId: string }) => o.objectiveId)).toEqual(["D2", "D3", "W1", "W2", "W3"]);
    expect(pass.objectives).toHaveLength(6);
    expect(pass.objectives[0]).toMatchObject({ objectiveId: "D1", current: 0, observedCurrent: 0, target: 1, status: "IN_PROGRESS", syncPending: false });
    expect(quests.objectives[0]).toMatchObject({ current: null, observedCurrent: 0, target: 1, status: "UNKNOWN", coverage: "PARTIAL_UNKNOWN", awardId: null });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("reports unsynced threshold progress without granting, then links the persisted right and claim", async () => {
    const f = await setup(); await f.login(); await f.record();
    let progress = (await f.read()).objectiveProgress;
    expect(progress.systems[0]!.objectives[0]).toMatchObject({ observedCurrent: 1, current: null, observedTargetReached: true, syncPending: true, status: "SYNC_PENDING", awardId: null });
    expect(f.scalar("SELECT COUNT(*) FROM recurring_awards")).toBe(0);
    await f.sync(); progress = (await f.read()).objectiveProgress;
    const d2 = progress.systems[0]!.objectives[0]!;
    expect(d2).toMatchObject({ status: "EARNED", syncPending: false, coverage: "PARTIAL_UNKNOWN" });
    expect((await f.request(`/v1/quests/${d2.awardId}/claim`)).status).toBe(200);
    expect((await f.read()).objectiveProgress.systems[0]!.objectives[0]!.status).toBe("CLAIMED");
  });
  it("uses independent weekly counters at Monday04 and resets both daily counters at04", async () => {
    const f = await setup(); f.set(5); await f.login(); await f.record(); await f.sync();
    f.set(6); const progress = (await f.read()).objectiveProgress;
    const quests = progress.systems[0]!.objectives, pass = progress.systems[1]!.objectives;
    expect(quests.find(o => o.objectiveId === "W1")).toMatchObject({ current: 0, periodStartAt: start + 6 * day, calendar: "MONDAY_04_KST" });
    expect(pass.find(o => o.objectiveId === "W1")).toMatchObject({ current: 1, periodStartAt: start, calendar: "SEASON_RELATIVE_7_DAYS" });
    expect(pass.find(o => o.objectiveId === "D1")).toMatchObject({ current: 0, periodStartAt: start + 6 * day });
  });
  it("keeps delayed old-day sources out of today's daily totals and scopes pending to the contributing metric", async () => {
    const f = await setup(); await f.record(); f.set(1);
    let progress = (await f.read()).objectiveProgress;
    expect(progress.systems[0]!.objectives.find(o => o.objectiveId === "D2")).toMatchObject({ observedCurrent: 0, syncPending: false });
    expect(progress.systems[0]!.objectives.find(o => o.objectiveId === "W2")).toMatchObject({ observedCurrent: 1, syncPending: true });
    expect(progress.systems[0]!.objectives.find(o => o.objectiveId === "W1")).toMatchObject({ current: 0, syncPending: false });
    await f.sync(); progress = (await f.read()).objectiveProgress;
    expect(progress.systems[0]!.objectives.find(o => o.objectiveId === "D2")).toMatchObject({ observedCurrent: 0, awardId: null });
    expect(progress.systems[0]!.objectives.find(o => o.objectiveId === "W2")).toMatchObject({ observedCurrent: 1, syncPending: false });
  });
  it("marks pass week5 not scheduled and diagnoses unsupported sources without fabricating zero coverage", async () => {
    const f = await setup(); f.set(28);
    await f.record("VICTORY", 15, "UNMAPPED_ID"); await f.record("FAILURE", 5, "opn_01_last_departure");
    const progress = (await f.read()).objectiveProgress;
    expect(progress.systems[1]!.objectives.filter(o => o.frequency === "WEEKLY").every(o => o.status === "NOT_SCHEDULED" && o.current === null && o.periodId === null)).toBe(true);
    expect(progress.systems[0]!.objectives[0]!.unknownReasons).toEqual(["COOPERATIVE_PARTICIPANT_PRODUCER_MISSING", "OPERATION_BINDING_MISSING", "STORY_DEFEAT_COST_CONTRACT_PENDING"]);
    expect(progress.systems[0]!.objectives[0]).toMatchObject({ observedCurrent: 0, status: "UNKNOWN" });
    f.set(30); expect((await f.read()).objectiveProgress.systems[1]).toMatchObject({ status: "NOT_CONFIGURED_FOR_CURRENT_PERIOD", policyId: null, objectives: [] });
  });
  it("records authenticated login once, excludes GET mutations and replays old login across a new day", async () => {
    const f = await setup(), before = f.sql.export(); await f.read();
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    const original = await (await f.login("same-login")).json(); await f.login(); await f.sync();
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(40);
    f.set(1); expect(await (await f.login("same-login")).json()).toEqual(original); await f.sync();
    expect(f.scalar("SELECT COUNT(*) FROM objective_events")).toBe(1);
    expect((await f.request("/v1/recurring/login", { xp: 999 })).status).toBe(400);
  });
  it("counts ordinary defeats, known story success/boss and preserves unsupported source diagnostics", async () => {
    const f = await setup();
    await f.record("FAILURE", 0); await f.record("FAILURE", 1); await f.record("FAILURE", 14);
    await f.record("VICTORY", 15, "opn_01_last_departure");
    await f.record("FAILURE", 5, "opn_01_last_departure");
    await f.record("VICTORY", 0, "raid_raider_king_engine"); await f.record("FAILURE", 0, "raid_raider_king_engine");
    await f.record("VICTORY", 15, "UNMAPPED_ID");
    await f.sync(); const state = await f.read();
    expect(f.scalar("SELECT SUM(ends) FROM objective_events")).toBe(4);
    expect(f.scalar("SELECT SUM(waves) FROM objective_events")).toBe(30);
    expect(state.eventStatus).toContainEqual({ status: "UNSUPPORTED_STORY_DEFEAT", count: 1 });
    expect(state.eventStatus).toContainEqual({ status: "OPERATION_BINDING_MISSING", count: 1 });
    expect(state.capabilities.cooperativeParticipantProducer).toBe("NOT_IMPLEMENTED");
    expect(state.policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(160);
  });
  it("uses DB finish commit time and keeps it through delayed processing/replay", async () => {
    const f = await setup(); f.set(0, day - 1);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.set(1); };
    const input = await f.record();
    const at = f.scalar("SELECT occurred_at FROM objective_events"); expect(at).toBe(start + day);
    f.set(4); await f.env.DB.batch([await objectiveFinishEvent(f.env.DB, input)]); await f.sync();
    expect(f.scalar("SELECT COUNT(*) FROM objective_events")).toBe(1);
    expect(f.scalar("SELECT occurred_at FROM objective_events")).toBe(at);
    expect((await f.read()).awards.every(a => a.earned_at === at)).toBe(true);
  });
  it("grants daily200 and season weekly600 once while ordinary week uses Monday", async () => {
    const f = await setup();
    for (let d = 0; d < 7; d++) { f.set(d); await f.login(); await f.record(); await f.record(); }
    await Promise.all([f.sync(), f.sync()]); const state = await f.read();
    expect(state.policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(2000);
    expect(state.awards.filter(a => a.system === "PASS50" && a.objective.startsWith("W"))).toHaveLength(3);
    expect(new Set(state.awards.filter(a => a.system === "QUESTS" && a.objective.startsWith("W")).map(a => a.period_id)).size).toBe(1);
    for (let i = 0; i < 4; i++) await f.record();
    await f.sync(); const monday = await f.read();
    expect(new Set(monday.awards.filter(a => a.system === "QUESTS" && a.objective.startsWith("W")).map(a => a.period_id)).size).toBe(2);
    expect(monday.policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(2000);
    expect(f.scalar("SELECT COUNT(*) FROM pass_xp_events")).toBe(0); // Legacy uncapped run XP is not this path.
  });
  it("does not create week5 pass XP or carry old season XP into the next month", async () => {
    const f = await setup(); f.set(28); await f.login(); for (let i = 0; i < 6; i++) await f.record();
    await f.sync(); expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(200);
    f.set(30); await f.login(); await f.record(); await f.sync();
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(200);
  });
  it("pays actual quest currencies once and mails only earned unclaimed rights without paying on send", async () => {
    const f = await setup(); await f.record(); await f.record(); await f.sync();
    const rights = (await f.read()).awards.filter(a => a.system === "QUESTS");
    const d2 = rights.find(a => a.objective === "D2")!, d3 = rights.find(a => a.objective === "D3")!;
    expect((await f.request(`/v1/quests/${d2.id}/claim`)).status).toBe(200);
    expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(100);
    expect(f.scalar("SELECT quantity FROM account_items WHERE item_id='cur_credits'")).toBeUndefined();
    f.set(1); await sendExpiredQuestMail(f.env.DB, "a");
    expect(f.scalar("SELECT COUNT(*) FROM quest_mail")).toBe(1);
    expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBeUndefined();
    const claimed = await f.request(`/v1/quest-mail/${d3.id}/claim`, undefined, "mail"); expect(claimed.status).toBe(200);
    f.set(20); expect(await (await f.request(`/v1/quest-mail/${d3.id}/claim`, undefined, "mail")).json()).toEqual(await claimed.json());
    expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(50);
  });
  it("expires mail exactly14 days after actual delayed send and never renews it", async () => {
    const f = await setup(); await f.record(); await f.sync(); const award = (await f.read()).awards.find(a => a.system === "QUESTS")!;
    f.set(10); await sendExpiredQuestMail(f.env.DB, "a"); const expires = Number(f.scalar("SELECT expires_at FROM quest_mail"));
    expect(expires).toBe(start + 24 * day);
    f.set(24); expect((await f.request(`/v1/quest-mail/${award.id}/claim`)).status).toBe(409);
    await f.sync(); await sendExpiredQuestMail(f.env.DB, "a"); expect(f.scalar("SELECT expires_at FROM quest_mail")).toBe(expires);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants")).toBe(0);
  });
  it("rolls back a claim crossing mail expiry and preserves the unspent wallet", async () => {
    const f = await setup(); await f.record(); await f.sync(); f.set(1); await sendExpiredQuestMail(f.env.DB, "a");
    const id = (await f.read()).mail[0]!.award_id; f.set(14, day - 1);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.set(15); };
    expect((await f.request(`/v1/quest-mail/${id}/claim`)).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants")).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM crystal_lots")).toBe(0);
  });
  it("settles earned free levels offline with shared manual receipts and no premium grants", async () => {
    const f = await setup(); await f.login(); await f.record(); await f.record(); await f.sync();
    const manual = await (await f.request("/v1/passes/pass-sep/levels/claim", { level: 1, track: "FREE" }, "manual")).json();
    f.set(30); await runRecurringJobs(f.env.DB); await runRecurringJobs(f.env.DB);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE right_key LIKE 'pass:%'")).toBe(2);
    expect(await (await f.request("/v1/passes/pass-sep/levels/claim", { level: 1, track: "FREE" }, "new-key")).json()).toEqual(manual);
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.settlementStatus).toBe("SETTLED_FOR_RECORDED_SOURCES");
  });
  it("reconciles a store-verified pre-cutoff intent after season end without XP or next-season entitlement", async () => {
    const f = await setup(); await f.login(); await f.record(); await f.record(); await f.sync();
    f.set(28, day - 60000);
    const intent = await (await f.request("/v1/passes/pass-sep/checkout-intents", undefined, "intent")).json();
    const boughtAt = start + 29 * day; // Within the issued10-minute intent, after checkout cutoff.
    f.setVerifier(async input => ({ ...input, transactionId: "paid-once", purchasedAt: boughtAt, verifiedAt: Date.now(), revoked: false }));
    f.set(30); expect((await f.request("/v1/passes/pass-sep/checkout-intents")).status).toBe(409);
    const body = { intentId: intent.intentId, receipt: "TEST_STORE_OPAQUE" };
    expect((await f.request("/v1/passes/pass-sep/purchases/reconcile", body, "pay")).status).toBe(200);
    await settleRecurringAccount(f.env.DB, "a");
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE right_key LIKE '%:PREMIUM'")).toBe(2);
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='PASS_PREMIUM'")).toBe(250);
    expect(f.scalar("SELECT COUNT(*) FROM crystal_lots WHERE source_type='PASS_PREMIUM' AND source='PAID'")).toBe(0);
    expect((await f.request("/v1/passes/pass-sep/purchases/reconcile", body, "pay")).status).toBe(200);
    expect(f.scalar("SELECT COUNT(*) FROM verified_store_orders")).toBe(1);
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(200);
  });
  it("caps at50 and resumes bounded settlement, including premium materials and cosmetics", async () => {
    const f = await setup();
    const intent = await (await f.request("/v1/passes/pass-sep/checkout-intents")).json();
    expect((await f.request("/v1/passes/pass-sep/purchases/reconcile", { intentId: intent.intentId, receipt: "TEST" })).status).toBe(200);
    for (let d = 0; d < 17; d++) { f.set(d); await f.login(); await f.record(); await f.record(); }
    await f.sync();
    const policy = (await f.read()).policies.find(p => p.policyId === "pass-sep")!;
    expect(policy.level).toBe(50); expect(policy.xp).toBe(5000); expect(policy.earnedXp).toBe(5200);
    f.set(30); expect(await settleRecurringAccount(f.env.DB, "a")).toEqual({ granted: 20, more: true });
    for (let i = 0; i < 5; i++) await settleRecurringAccount(f.env.DB, "a");
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE right_key LIKE 'pass:%'")).toBe(100);
    expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(45000);
    expect(f.scalar("SELECT quantity FROM account_items WHERE item_id='TEST_TRAIN_SKIN'")).toBe(1);
    expect(await settleRecurringAccount(f.env.DB, "a")).toEqual({ granted: 0, more: false });
  });
  it("shares manual and settlement claims under concurrent requests and applies delayed sources to their old season", async () => {
    const f = await setup(); await f.login(); await f.record(); await f.record();
    f.set(30);
    await f.sync();
    const [manual] = await Promise.all([f.request("/v1/passes/pass-sep/levels/claim", { level: 1, track: "FREE" }), settleRecurringAccount(f.env.DB, "a")]);
    expect(manual.status).toBe(200);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE right_key LIKE 'pass:%'")).toBe(2);
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='PASS_FREE'")).toBe(50);
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.earnedXp).toBe(200);
  });
  it("rejects a new reconciliation for a revoked order while preserving the original receipt", async () => {
    const f = await setup();
    const intent = await (await f.request("/v1/passes/pass-sep/checkout-intents")).json();
    const body = { intentId: intent.intentId, receipt: "TEST_RECEIPT" };
    const first = await f.request("/v1/passes/pass-sep/purchases/reconcile", body, "original");
    expect(first.status).toBe(200); const receipt = await first.json();
    f.sql.run("UPDATE verified_store_orders SET revoked_at=?", [start]);
    const next = await f.request("/v1/passes/pass-sep/purchases/reconcile", body, "new");
    expect(next.status).toBe(403);
    expect(await (await f.request("/v1/passes/pass-sep/purchases/reconcile", body, "original")).json()).toEqual(receipt);
    expect((await f.read()).policies.find(p => p.policyId === "pass-sep")!.premium).toBe(false);
  });
  it("rejects forged, wrong-account, wrong-intent and post-intent purchases without grants", async () => {
    const f = await setup(); const intent = await (await f.request("/v1/passes/pass-sep/checkout-intents")).json();
    f.setVerifier(async input => ({ ...input, intentId: "wrong", transactionId: "bad", purchasedAt: start, verifiedAt: Date.now(), revoked: false }));
    const body = { intentId: intent.intentId, receipt: "forged" };
    expect((await f.request("/v1/passes/pass-sep/purchases/reconcile", body)).status).toBe(403);
    expect((await f.request("/v1/passes/pass-sep/purchases/reconcile", body, undefined, "b")).status).toBe(403);
    expect(f.scalar("SELECT COUNT(*) FROM pass50_entitlements")).toBe(0);
    expect((await f.request("/v1/passes/pass-sep/levels/claim", { level: 1, track: "PREMIUM" })).status).toBe(409);
  });
});
