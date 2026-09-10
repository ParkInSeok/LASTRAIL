import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { setup, body } from "../helpers/departure-fixture";

describe("combat requirements and recovery", () => {
  it("returns versioned known facts and honest missing bindings without changing existing slot/risk types", async () => {
    const { preview, scalar } = await setup();
    const p = await (await preview()).json();
    expect(p).toMatchObject({ allowedOperatorSlots: 5, risk: "I", firstClearEligible: true,
      combatContract: { version: "run-combat-requirements-v2", status: "BLOCKED", canStart: false,
        riskLevel: 1, operatorSlotCount: 5, firstClearEligible: true,
        enemyFactionId: null, wavePlanId: null, rewardTableId: null, seed: null, engineOutput: null, ultimateOperatorId: "op0",
        requiredContent: { operationId: body.operationId, contentVersion: "act1.launch.content.v1" } } });
    expect(p.combatContract.blockingReasons).toContain("combat_seed_contract_missing");
    expect(p.combatContract.blockingReasons).toContain("effective_stats_profile_unapproved");
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(30);
  });
  it("refuses unready paid start even if the client claims readiness or tampers the saved diagnostic boolean", async () => {
    const { preview, start, sql, scalar, post } = await setup(); const p = await (await preview()).json();
    expect((await post("/v1/runs/start", { operationPreviewId: p.operationPreviewId, combatReady: true })).status).toBe(400);
    sql.run("UPDATE operation_previews SET preview_json=json_set(preview_json,'$.combatContract.canStart',json('true'))");
    const response = await start(p.operationPreviewId);
    expect(response.status).toBe(409); expect(await response.json()).toMatchObject({ errorCode: "combat_not_ready" });
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(30); expect(scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(0); expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
  });
  it("pins first-clear eligibility in the preview receipt with no reward grant", async () => {
    const { preview, sql, scalar } = await setup(); const first = await (await preview(body, "pin")).json();
    sql.run("INSERT INTO account_mutations VALUES('historical','a',99,'historical',NULL,0)");
    sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('historical','a','historical',1,'COMPLETED',0)");
    sql.run("INSERT INTO account_first_clears VALUES('a',?,'I',0,'historical')", [body.operationId]);
    expect(await (await preview(body, "pin")).json()).toEqual(first);
    expect(await (await preview()).json()).toMatchObject({ firstClearEligible: false, combatContract: { firstClearEligible: false } });
    expect(scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(0);
  });
  it("recovers only the owned stored start response and never starts or refunds a run", async () => {
    const { env, headers, sql, scalar, preview, start } = await setup();
    const p = await (await preview()).json();
    sql.run("INSERT INTO account_mutations VALUES('old','a',99,'RUN_START',NULL,0)");
    sql.run("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES('old','a',?,'old',1,'ACTIVE',1000)", [p.operationPreviewId]);
    const original = { accepted: true, startStatus: "STARTED", runId: "old", operationPreviewId: p.operationPreviewId, operationId: body.operationId, serverStartedAt: "1970-01-01T00:00:01.000Z", admission: { version: "departure-v1" } };
    sql.run("INSERT INTO run_snapshots VALUES('old',?,1000)", [JSON.stringify({ ...original, departureResponse: original })]);
    const response = await app.request("/v1/runs/old/start-state", { headers }, env);
    expect(response.status).toBe(200); expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(await response.json()).toMatchObject({ accepted: true, runId: "old", runStatus: "ACTIVE", recoveryStatus: "AVAILABLE", startResponse: original });
    sql.run("INSERT INTO idempotency_records VALUES('old','a','original','run_start','old',1000)");
    expect(await (await start(p.operationPreviewId, "original")).json()).toEqual(original);
    expect((await start(p.operationPreviewId, "new-key")).status).toBe(409);
    sql.run("UPDATE auth_sessions SET expires_at=0");
    expect((await app.request("/v1/runs/old/start-state", { headers }, env)).status).toBe(401);
    sql.run("UPDATE auth_sessions SET expires_at=?", [Date.now() + 60000]);
    sql.run("UPDATE auth_sessions SET status='REVOKED'");
    expect((await app.request("/v1/runs/old/start-state", { headers }, env)).status).toBe(401);
    sql.run("UPDATE auth_sessions SET status='ACTIVE'");
    sql.run("UPDATE runs SET account_id='b'");
    expect((await app.request("/v1/runs/old/start-state", { headers }, env)).status).toBe(404);
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(30); expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("distinguishes missing snapshot from unavailable source and rejects query overrides", async () => {
    const { env, headers, sql, preview, start } = await setup();
    const p = await (await preview()).json();
    sql.run("UPDATE operation_previews SET preview_json='invalid-json'");
    expect((await start(p.operationPreviewId)).status).toBe(503);
    sql.run("INSERT INTO account_mutations VALUES('old','a',99,'RUN_START',NULL,0)");
    sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('old','a','old',1,'COMPLETED',1000)");
    expect(await (await app.request("/v1/runs/old/start-state", { headers }, env)).json()).toMatchObject({ recoveryStatus: "SNAPSHOT_UNAVAILABLE", startResponse: null });
    expect((await app.request("/v1/runs/old/start-state?accountId=b", { headers }, env)).status).toBe(400);
    expect((await app.request("/v1/runs/old/start-state", {}, env)).status).toBe(401);
  });
  it("rechecks first-clear truth at preview commit without rewarding or overwriting history", async () => {
    const { adapter, sql, preview, scalar } = await setup();
    sql.run("INSERT INTO account_mutations VALUES('history','a',99,'RUN_START',NULL,0)");
    sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('history','a','history',1,'COMPLETED',0)");
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined;
      sql.run("INSERT INTO account_first_clears VALUES('a',?,'I',0,'history')", [body.operationId]); };
    expect(await (await preview()).json()).toMatchObject({ firstClearEligible: false });
    expect(scalar("SELECT COUNT(*) FROM departure_preview_receipts")).toBe(1);
    expect(scalar("SELECT COUNT(*) FROM account_first_clears")).toBe(1);
    expect(scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(0);
  });
});
