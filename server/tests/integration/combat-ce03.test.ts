import { describe, expect, it } from "vitest";
import { Buffer } from "node:buffer";
import app from "../../apps/game-api/src/index";
import { setup } from "../helpers/departure-fixture";
import { combatRewardReference } from "../../apps/game-api/src/combat-reward-reference";
const finishBody = (runId: string) => ({ runId, outcome: "VICTORY", currentStage: "Complete", currentWaveNumber: 0,
  completedWaveCount: 15, encounterType: "ORDINARY_15_WAVE", runtimeStateJson: "{}", tacticSelections: [], clientCompletedAt: new Date().toISOString() });
async function startedFixture() {
  const f = await setup(true), preview = await (await f.preview()).json();
  const response = await (await f.start(preview.operationPreviewId, "original-start")).json();
  const read = () => app.request(`/v1/runs/${response.runId}/lifecycle`, { headers: f.headers }, f.env);
  return { ...f, preview, response, read };
}
describe("CE03 grounded reward references and read-only lifecycle", () => {
  it("pins preview and accepted reference, preserves exact original replay, reads without mutations", async () => {
    const f = await startedFixture();
    expect(f.response.rewardReference).toEqual(f.preview.rewardReference);
    expect(f.response.rewardReference).toMatchObject({ version: "run-reward-ref-v1", balanceVersion: "1", operationId: "opn_01_last_departure", risk: "I", scope: "BASE_RUN_REWARDS" });
    expect(f.response.rewardReference.configSha256).toMatch(/^[a-f0-9]{64}$/u);
    const published = await (await app.request("/v1/balance-config/1", {}, f.env)).json();
    expect(await combatRewardReference(JSON.stringify(published.payload, null, 2), published.version, "opn_01_last_departure", "I")).toEqual(f.response.rewardReference);
    expect(await (await f.start(f.preview.operationPreviewId, "original-start")).json()).toEqual(f.response);
    const before = f.sql.export(), state = await (await f.read()).json();
    expect(state).toMatchObject({ nextAction: "RECOVER_COMBAT_CONSUMER", startResponse: f.response, newAttemptAuthorized: false, finish: { status: "NOT_RECORDED" }, reward: { status: "NOT_AVAILABLE" } });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(1);
  });
  it("finishes against historical balance despite active replacement and claims only after explicit action", async () => {
    const f = await startedFixture();
    f.sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) SELECT 2,schema_version,required_content_version,'test2',config_json,0,'test' FROM balance_config_versions WHERE version=1");
    f.sql.run("UPDATE balance_config_active SET version=2");
    const body = finishBody(f.response.runId);
    const result = await f.post(`/v1/runs/${f.response.runId}/finish`, body, "finish");
    expect(result.status).toBe(200); const finished = await result.json(); expect(finished.accepted).toBe(true);
    const state = await (await f.read()).json(); expect(state).toMatchObject({ nextAction: "REWARD_REVIEW", finish: { status: "RECORDED", rewardPreviewId: finished.rewardPreviewId }, reward: { status: "CLAIMABLE", receipt: null } });
    expect(f.scalar("SELECT COUNT(*) FROM reward_transactions WHERE run_id='" + f.response.runId + "'")).toBe(0);
    const claimBody = { runId: f.response.runId, rewardPreviewId: finished.rewardPreviewId };
    const claim = await f.post(`/v1/runs/${f.response.runId}/reward-preview/${finished.rewardPreviewId}/claim`, claimBody, "claim");
    expect(claim.status).toBe(200);
    expect(await (await f.read()).json()).toMatchObject({ nextAction: "RETURN_TO_LOBBY", reward: { status: "CLAIMED" } });
    f.sql.run("UPDATE run_reward_previews SET created_at=0,expires_at=1");
    expect(await (await f.read()).json()).toMatchObject({ reward: { status: "CLAIMED" }, disposition: "REWARDED", uiReturnAllowed: true });
    expect(await (await f.post(`/v1/runs/${f.response.runId}/finish`, body, "finish")).json()).toMatchObject({ resultStatus: "DUPLICATE_REPLAY", rewardPreviewId: finished.rewardPreviewId });
    expect(f.scalar("SELECT COUNT(*) FROM run_final_results")).toBe(1);
    f.sql.run("UPDATE reward_transactions SET reward_json='null'");
    expect(await (await f.read()).json()).toMatchObject({ nextAction: "REVIEW_REQUIRED", uiReturnAllowed: true, reward: { status: "UNAVAILABLE", receipt: null } });
  });
  it("rejects changed pinned historical rewards before creating final results or grants", async () => {
    const f = await startedFixture();
    const config = JSON.parse(String(f.scalar("SELECT config_json FROM balance_config_versions WHERE version=1")));
    config.runRewards.operations.opn_01_last_departure.baseRewards[0].quantity++;
    f.sql.run("UPDATE balance_config_versions SET config_json=? WHERE version=1", [JSON.stringify(config)]);
    const response = await f.post(`/v1/runs/${f.response.runId}/finish`, finishBody(f.response.runId));
    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ errorCode: "combat_reward_reference_changed" });
    expect(f.scalar("SELECT COUNT(*) FROM run_final_results")).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(0);
  });
  it("distinguishes pending, quarantined, missing snapshot and expired reward without refund or entry permission", async () => {
    const f = await startedFixture();
    f.sql.run("UPDATE runs SET status='FINISH_PENDING'"); expect((await (await f.read()).json()).nextAction).toBe("RESUME_FINISH");
    f.sql.run("UPDATE runs SET status='QUARANTINED'"); expect((await (await f.read()).json()).nextAction).toBe("REVIEW_REQUIRED");
    f.sql.run("UPDATE runs SET status='ACTIVE'");
    const finished = await (await f.post(`/v1/runs/${f.response.runId}/finish`, finishBody(f.response.runId))).json();
    expect(finished.accepted).toBe(true);
    f.sql.run("UPDATE run_reward_previews SET created_at=0,expires_at=1");
    expect(await (await f.read()).json()).toMatchObject({ nextAction: "REVIEW_REQUIRED", newAttemptAuthorized: false, reward: { status: "EXPIRED" } });
    f.sql.run("DELETE FROM run_snapshots"); expect((await (await f.read()).json()).startResponse).toBe(null);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(1);
    expect((await app.request(`/v1/runs/${f.response.runId}/lifecycle`, {}, f.env)).status).toBe(401);
    f.sql.run("UPDATE runs SET account_id='b'");
    expect((await f.read()).status).toBe(404);
  });
});
