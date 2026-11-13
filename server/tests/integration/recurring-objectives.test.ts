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
  it("returns the actual cumulative balance for repeated reward resources in one grant", async () => {
    const f = await setup(), award = "duplicate-reward";
    f.sql.run("INSERT INTO recurring_awards VALUES(?,'a','quests','QUESTS','test','D2',?,?,0,?)", [award,Date.now(),Date.now()+day,JSON.stringify([
      {kind:"CURRENCY",id:"cur_credits",quantity:10},{kind:"CURRENCY",id:"cur_credits",quantity:20},
      {kind:"ITEM",id:"duplicate-item",quantity:2},{kind:"ITEM",id:"duplicate-item",quantity:3}
    ])]);
    const response = await f.request(`/v1/quests/${award}/claim`,undefined,"duplicate-resources");
    expect(response.status).toBe(200); const receipt = await response.json();
    expect(receipt.owners.account.currencies.CREDITS).toBe(f.scalar("SELECT balance FROM account_currencies WHERE account_id='a' AND currency='CREDITS'"));
    expect(receipt.owners.account.items).toEqual([{itemId:"duplicate-item",quantity:5}]);
    expect(f.scalar("SELECT quantity FROM account_items WHERE account_id='a' AND item_id='duplicate-item'")).toBe(5);
  });
  it("stores versioned final balances with the grant and never upgrades a legacy receipt on replay", async () => {
    const f = await setup(); await f.record(); await f.sync();
    const award = String(f.scalar("SELECT id FROM recurring_awards WHERE account_id='a' AND system='QUESTS' AND objective='D2'"));
    const receipt = await (await f.request(`/v1/quests/${award}/claim`, undefined, "balance-original")).json();
    expect(receipt.owners.account).toMatchObject({ accountId: "a", kind: "PATCH", crystalWallet: { freeBalance: 10, totalBalance: 10 } });
    expect(receipt.owners.account.mutationSequence).toBe(receipt.owners.account.beforeMutationSequence + 1);
    expect(receipt.owners.account.mutationSequence).toBe(f.scalar("SELECT sequence FROM account_mutations WHERE id='" + receipt.accountMutationId + "'"));
    const legacy = { ...receipt }; delete legacy.owners; delete legacy.stateContract;
    f.sql.run("UPDATE recurring_grants SET response_json=? WHERE account_id='a' AND right_key=?", [JSON.stringify(legacy), receipt.rightKey]);
    expect(await (await f.request(`/v1/quests/${award}/claim`, undefined, "legacy-replay")).json()).toEqual(legacy);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants")).toBe(1);
  });
  it("does not report a complete original list when persisted membership is missing", async () => {
    const f = await setup();
    const body = { kind: "QUEST", awardIds: ["missing-a", "missing-b"] };
    const result = await (await f.request("/v1/recurring/bulk-claims", body, "damaged")).json();
    f.sql.run("DELETE FROM recurring_bulk_members WHERE bulk_id=? AND member_index=1", [result.bulkId]);
    expect((await f.request("/v1/recurring/bulk-claims", body, "damaged")).status).toBe(503);
  });
  it('coalesces different request keys for one earned right under overlapping service execution',async()=>{
    const f=await setup();try{
      await f.record();await f.sync();
      const award=String(f.scalar("SELECT id FROM recurring_awards WHERE account_id='a' AND system='QUESTS' AND objective='D2'"));
      const responses=await Promise.all([f.request(`/v1/quests/${award}/claim`,undefined,'view-left'),f.request(`/v1/quests/${award}/claim`,undefined,'view-right')]);
      expect(responses.map(r=>r.status)).toEqual([200,200]);const receipts=await Promise.all(responses.map(r=>r.json()));expect(receipts[1]).toEqual(receipts[0]);
      expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE account_id='a'")).toBe(1);
      expect(f.scalar("SELECT COUNT(*) FROM recurring_requests WHERE idempotency_key IN ('view-left','view-right')")).toBe(2);
    }finally{f.sql.close();}
  });
  it("rejects empty, duplicate, oversized or mixed bulk membership and conflicts on changed original body", async () => {
    const f = await setup();
    for (const body of [{ kind: "QUEST", awardIds: [] }, { kind: "MAIL", awardIds: ["x", "x"] },
      { kind: "QUEST", awardIds: Array.from({ length: 51 }, (_, i) => `x${i}`) },
      { kind: "QUEST", awardIds: ["x"], levels: [1] }]) expect((await f.request("/v1/recurring/bulk-claims", body)).status).toBe(400);
    const original = { kind: "QUEST", awardIds: ["missing"] };
    expect((await f.request("/v1/recurring/bulk-claims", original, "fixed")).status).toBe(200);
    expect((await f.request("/v1/recurring/bulk-claims", { ...original, awardIds: ["different"] }, "fixed")).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_bulk_requests")).toBe(1);
  });
  it("keeps confirmed failures and successful receipts fixed while COMPLETE distinguishes failure count", async () => {
    const f = await setup(); await f.login(); await f.record(); await f.record(); await f.sync();
    const body = { kind: "PASS_LEVEL", policyId: "pass-sep", track: "FREE", levels: [1, 3] };
    const first = await (await f.request("/v1/recurring/bulk-claims", body, "bulk")).json();
    expect(first).toMatchObject({ status: "COMPLETE", successCount: 1, failureCount: 1, pendingCount: 0 });
    expect(first.items.map((i: { status: string }) => i.status)).toEqual(["SUCCESS", "FAILED"]);
    f.set(1); await f.login(); await f.record(); await f.record(); await f.sync();
    expect(await (await f.request("/v1/recurring/bulk-claims", body, "bulk")).json()).toEqual(first);
    const retry = await (await f.request("/v1/recurring/bulk-claims", body, "explicit-new-key")).json();
    expect(retry.items.map((i: { status: string }) => i.status)).toEqual(["REPLAYED", "SUCCESS"]);
    expect(retry.resultContract).toMatchObject({ version: "data.bulk-result.v1.20261021", grantedCount: 1, alreadyGrantedCount: 1, unpaidCount: 0, unknownCount: 0, coverage: "ORIGINAL_REQUEST", totalsScope: "SUCCESS_MEMBERS_ONLY" });
    expect(retry.items[0]).toMatchObject({ outcome: "ALREADY_GRANTED", includeInAcquiredTotal: false, recovery: "NONE" });
    expect(first.items[1]).toMatchObject({ outcome: "NOT_GRANTED", includeInAcquiredTotal: false, recovery: "NEW_CONFIRMATION_AFTER_REASON_RESOLVED" });
    const before = f.sql.export();
    expect(await (await f.request(`/v1/recurring/bulk-claims/${first.bulkId}`, undefined, undefined, "a", "GET")).json()).toEqual(first);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect((await f.request(`/v1/recurring/bulk-claims/${first.bulkId}`, undefined, undefined, "b", "GET")).status).toBe(403);
  });
  it("resumes a frozen50-member list in bounded steps without adding targets", async () => {
    const f = await setup();
    const body = { kind: "PASS_LEVEL", policyId: "pass-sep", track: "FREE", levels: Array.from({ length: 50 }, (_, i) => i + 1) };
    let result = await (await f.request("/v1/recurring/bulk-claims", body, "fifty")).json();
    expect(result).toMatchObject({ status: "PARTIAL", failureCount: 10, pendingCount: 40, totalCount: 50 });
    for (let i = 0; i < 4; i++) result = await (await f.request("/v1/recurring/bulk-claims", body, "fifty")).json();
    expect(result).toMatchObject({ status: "COMPLETE", successCount: 0, failureCount: 50, pendingCount: 0 });
    expect(f.scalar("SELECT COUNT(*) FROM recurring_bulk_members")).toBe(50);
  });
  it("preserves another member's success and rolls back every reward item on a transient grant failure", async () => {
    const f = await setup(); await f.record(); await f.record(); await f.sync();
    const awards = (await f.read()).awards.filter(a => a.system === "QUESTS");
    const body = { kind: "QUEST", awardIds: [awards.find(a => a.objective === "D2")!.id, awards.find(a => a.objective === "D3")!.id] };
    f.sql.run("CREATE TRIGGER test_grant_failure BEFORE INSERT ON currency_ledger WHEN NEW.currency='OPERATOR_EXP' BEGIN SELECT RAISE(ABORT,'simulated temporary failure'); END");
    const first = await (await f.request("/v1/recurring/bulk-claims", body, "retry")).json();
    expect(first).toMatchObject({ status: "PARTIAL", successCount: 1, pendingCount: 1, failureCount: 0 });
    expect(first.resultContract).toMatchObject({ grantedCount: 1, alreadyGrantedCount: 0, unpaidCount: 0, unknownCount: 1 });
    expect(first.items[1]).toMatchObject({ outcome: "UNKNOWN", recovery: "ORIGINAL_KEY_AND_BODY", includeInAcquiredTotal: false });
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='QUEST'")).toBe(10);
    expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBeUndefined();
    f.sql.run("DROP TRIGGER test_grant_failure");
    const final = await (await f.request("/v1/recurring/bulk-claims", body, "retry")).json();
    expect(final).toMatchObject({ status: "COMPLETE", successCount: 2, pendingCount: 0 });
    expect(final.items[0]).toEqual(first.items[0]);
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='QUEST'")).toBe(20);
  });
  it("recovers lost grant responses and races with manual and season-worker claims without duplicate payout", async () => {
    const f = await setup(); await f.login(); await f.record(); await f.record(); await f.sync(); f.set(30);
    const originalBatch = f.adapter.batch.bind(f.adapter); let lost = false;
    vi.spyOn(f.adapter, "batch").mockImplementation(async statements => {
      const result = await originalBatch(statements);
      if (!lost && statements.some(s => s.sql.startsWith("UPDATE recurring_bulk_members SET status="))) { lost = true; throw new Error("response lost after commit"); }
      return result;
    });
    const body = { kind: "PASS_LEVEL", policyId: "pass-sep", track: "FREE", levels: [1, 2] };
    await Promise.all([f.request("/v1/recurring/bulk-claims", body, "racing"),
      f.request("/v1/passes/pass-sep/levels/claim", { level: 1, track: "FREE" }), settleRecurringAccount(f.env.DB, "a")]);
    const final = await (await f.request("/v1/recurring/bulk-claims", body, "racing")).json();
    expect(final).toMatchObject({ status: "COMPLETE", successCount: 2, failureCount: 0 });
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants WHERE right_key LIKE 'pass:%'")).toBe(2);
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='PASS_FREE'")).toBe(50);
  });
  it("concurrently resumes the same bulk key with one durable list and one grant per right", async () => {
    const f = await setup(); await f.record(); await f.record(); await f.sync();
    const body = { kind: "QUEST", awardIds: (await f.read()).awards.filter(a => a.system === "QUESTS").map(a => a.id) };
    const results = await Promise.all([f.request("/v1/recurring/bulk-claims", body, "same-bulk"), f.request("/v1/recurring/bulk-claims", body, "same-bulk")]);
    expect(results.map(r => r.status)).toEqual([200, 200]);
    const final = await (await f.request("/v1/recurring/bulk-claims", body, "same-bulk")).json();
    expect(final).toMatchObject({ status: "COMPLETE", successCount: 2, failureCount: 0 });
    expect(f.scalar("SELECT COUNT(*) FROM recurring_bulk_requests")).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants")).toBe(2);
  });
  it("resumes after losing the request-persistence response without changing the frozen list", async () => {
    const f = await setup(); const originalBatch = f.adapter.batch.bind(f.adapter); let lost = false;
    vi.spyOn(f.adapter, "batch").mockImplementation(async statements => {
      const result = await originalBatch(statements);
      if (!lost && statements.some(s => s.sql.startsWith("INSERT INTO recurring_bulk_requests"))) { lost = true; throw new Error("lost after request commit"); }
      return result;
    });
    const body = { kind: "QUEST", awardIds: ["missing1", "missing2"] };
    expect((await f.request("/v1/recurring/bulk-claims", body, "lost-request")).status).toBe(503);
    const resumed = await (await f.request("/v1/recurring/bulk-claims", body, "lost-request")).json();
    expect(resumed).toMatchObject({ status: "COMPLETE", failureCount: 2, totalCount: 2 });
    expect(f.scalar("SELECT COUNT(*) FROM recurring_bulk_requests")).toBe(1);
    expect((await f.request("/v1/recurring/bulk-claims", { ...body, awardIds: ["missing2", "missing1"] }, "lost-request")).status).toBe(409);
  });
  it("fixes expired mail failure at commit boundary while retaining a previously claimed mail receipt", async () => {
    const f = await setup(); await f.record(); await f.record(); await f.sync(); f.set(1); await sendExpiredQuestMail(f.env.DB, "a");
    const mail = (await f.read()).mail; await f.request(`/v1/quest-mail/${mail[0]!.award_id}/claim`);
    const body = { kind: "MAIL", awardIds: mail.map(m => m.award_id) };
    f.set(14, day - 1); const originalBatch = f.adapter.batch.bind(f.adapter); let crossed = false;
    vi.spyOn(f.adapter, "batch").mockImplementation(async statements => {
      if (!crossed && statements.some(s => s.sql.startsWith("INSERT INTO reward_transactions"))) { crossed = true; f.set(15); }
      return originalBatch(statements);
    });
    await f.request("/v1/recurring/bulk-claims", body, "expiry");
    const final = await (await f.request("/v1/recurring/bulk-claims", body, "expiry")).json();
    expect(final).toMatchObject({ status: "COMPLETE", successCount: 1, failureCount: 1 });
    expect(final.items.find((i: { status: string }) => i.status === "FAILED").errorCode).toBe("quest_mail_expired");
    expect(f.scalar("SELECT COUNT(*) FROM recurring_grants")).toBe(1);
  });
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
