import { describe, expect, it } from "vitest";
import { setup as createFixture, body, operations } from "../helpers/departure-fixture";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";
const setup = () => createFixture(true);
describe("S11 preview and retained atomic start core (HTTP combat gate tested separately)", () => {
  it("quotes stored fuel and owned active squad/train, starts with one debit and exact replay", async () => {
    const { preview, start, scalar } = await setup();
    const r = await preview(body, "quote"); expect(r.status).toBe(200);
    const p = await r.json();
    expect(p).toMatchObject({ accepted: true, operationId: operations[0], quote: { fuelCost: 10, availableFuel: 30, balanceVersion: "1" }, formationRevision: 1, loadoutRevision: 1 });
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(30);
    expect(await (await preview(body, "quote")).json()).toEqual(p);
    expect((await preview({ ...body, risk: "II" }, "quote")).status).toBe(409);
    const started = await start(p.operationPreviewId, "start"); expect(started.status).toBe(200);
    const response = await started.json();
    expect(response).toMatchObject({ accepted: true, startStatus: "STARTED", admission: { formationRevision: 1, loadoutRevision: 1, fuelCost: 10 }, operationId: operations[0] });
    expect(await (await start(p.operationPreviewId, "start")).json()).toEqual(response);
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(20);
    expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(1);
    expect(scalar("SELECT COUNT(*) FROM runs")).toBe(1);
    expect(scalar("SELECT status FROM operation_previews")).toBe("CONSUMED");
  });
  it("rejects foreign/unknown operations, locked progress, stats and unsupported risk without creating previews", async () => {
    const { preview, scalar } = await setup();
    for (const data of [{ ...body, operationId: "category_invented" }, { ...body, risk: "VI" }, { ...body, fuelCost: 0 }]) expect((await preview(data)).status).toBe(400);
    expect((await preview({ ...body, operationId: operations[1] })).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM operation_previews")).toBe(0);
  });
  it("refuses absent formation, lost ownership, stale selections and missing fuel/cost policy", async () => {
    for (const statement of ["DELETE FROM account_formation", "DELETE FROM account_operators WHERE operator_id='op0'", "DELETE FROM account_carriages WHERE carriage_id='car_gun'", "DELETE FROM fuel_state", "UPDATE balance_config_versions SET config_json='{}'"]) {
      const { preview, sql, scalar } = await setup(); sql.run(statement);
      expect((await preview()).status).toBeGreaterThanOrEqual(400); expect(scalar("SELECT COUNT(*) FROM operation_previews")).toBe(0);
    }
    const { preview } = await setup(); expect((await preview({ ...body, expectedFormationRevision: 2 })).status).toBe(409);
  });
  it("revalidates ownership, loadout, progression, expiration and fuel before start, never consumes failed quotes", async () => {
    for (const change of ["DELETE FROM account_operators WHERE operator_id='op0'", "DELETE FROM account_carriages WHERE carriage_id='car_gun'", "UPDATE account_train_loadout SET revision=2", "UPDATE account_formation SET revision=2", "UPDATE operation_previews SET expires_at=0", "UPDATE fuel_state SET fuel=9"]) {
      const { preview, start, sql, scalar } = await setup(); const p = await (await preview()).json();
      sql.run(change); expect((await start(p.operationPreviewId)).status).toBeGreaterThanOrEqual(400);
      expect(scalar("SELECT COUNT(*) FROM runs")).toBe(0); expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(0);
      expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
    }
  });
  it("pins the balance quote but rejects changed active policy until a fresh preview", async () => {
    const { preview, start, sql, scalar } = await setup(); const p = await (await preview()).json();
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(2,1,1,'test2','{\"fuel\":{\"runStartCost\":20}}',0,'test')");
    sql.run("UPDATE balance_config_versions SET config_json=? WHERE version=2", [JSON.stringify({ fuel: { runStartCost: 20 }, runRewards: launchRunRewardsConfig })]);
    sql.run("UPDATE balance_config_active SET version=2");
    expect((await start(p.operationPreviewId)).status).toBe(409); expect(scalar("SELECT fuel FROM fuel_state")).toBe(30);
    expect(await (await preview()).json()).toMatchObject({ quote: { fuelCost: 20, balanceVersion: "2" } });
  });
  it("starts at most once across duplicate requests and competing previews", async () => {
    const { preview, start, scalar } = await setup(); const p = await (await preview()).json();
    const requests = await Promise.all([start(p.operationPreviewId, "same"), start(p.operationPreviewId, "same")]);
    expect(requests.map(r => r.status)).toEqual([200, 200]);
    expect(await requests[0].json()).toEqual(await requests[1].json());
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(20);
    const other = await setup(); const p1 = await (await other.preview()).json(), p2 = await (await other.preview()).json();
    expect((await Promise.all([other.start(p1.operationPreviewId), other.start(p2.operationPreviewId)])).map(r => r.status).sort()).toEqual([200, 409]);
    expect(other.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("rolls back fuel/consume/run on snapshot failure and catches ownership changes at commit", async () => {
    const { preview, start, sql, scalar } = await setup(); const p = await (await preview()).json();
    sql.run("CREATE TRIGGER fail_run_snapshot BEFORE INSERT ON run_snapshots BEGIN SELECT RAISE(ABORT,'injected rollback'); END");
    expect((await start(p.operationPreviewId)).status).toBe(409);
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(30); expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
    expect(scalar("SELECT COUNT(*) FROM runs")).toBe(0); expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(0);
    sql.run("DROP TRIGGER fail_run_snapshot"); expect((await start(p.operationPreviewId)).status).toBe(200);
    const other = await setup(); const q = await (await other.preview()).json();
    other.adapter.beforeBatch = () => { other.adapter.beforeBatch = undefined; other.sql.run("DELETE FROM account_operators WHERE operator_id='op0'"); };
    expect((await other.start(q.operationPreviewId)).status).toBeGreaterThanOrEqual(400); expect(other.scalar("SELECT fuel FROM fuel_state")).toBe(30);
  });
  it("preserves all six existing story IDs and raid metadata after real prerequisite records", async () => {
    const { preview, start, progress, install } = await setup(); progress(operations.slice(0, 6));
    expect((await install(["car_combat", "car_gun", "car_cargo", "car_defense"], 1)).status).toBe(200);
    for (const operationId of operations) expect((await preview({ ...body, operationId, expectedLoadoutRevision: 2 })).status).toBe(200);
    const p = await (await preview({ ...body, operationId: operations[6], expectedLoadoutRevision: 2 })).json();
    expect(await (await start(p.operationPreviewId)).json()).toMatchObject({ encounterType: "RAID_BOSS", raidId: operations[6], bossId: "boss_raider_king_engine", bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"] });
  });
  it("keeps legacy preview consumption atomic and blocks a second legacy run", async () => {
    const { preview, start, sql, scalar } = await setup();
    const p = await (await preview()).json();
    sql.run("UPDATE operation_previews SET admission_version=NULL");
    sql.run("CREATE TRIGGER fail_legacy_snapshot BEFORE INSERT ON run_snapshots BEGIN SELECT RAISE(ABORT,'injected rollback'); END");
    expect((await start(p.operationPreviewId, "legacy")).status).toBe(409);
    expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
    expect(scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    sql.run("DROP TRIGGER fail_legacy_snapshot");
    expect((await start(p.operationPreviewId, "legacy")).status).toBe(200);
    sql.run("UPDATE operation_previews SET status='ACTIVE'");
    expect((await start(p.operationPreviewId, "other")).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM runs")).toBe(1);
    expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
  });
  it("fences commit-time fuel, policy, growth and expiration changes", async () => {
    for (const change of ["UPDATE fuel_state SET fuel=9", "UPDATE balance_config_versions SET config_json=json_set(config_json,'$.fuel.runStartCost',20)", "UPDATE account_operators SET revision=2", "UPDATE operation_previews SET expires_at=0"]) {
      const { preview, start, adapter, sql, scalar } = await setup(); const p = await (await preview()).json();
      adapter.beforeBatch = () => { adapter.beforeBatch = undefined; sql.run(change); };
      expect((await start(p.operationPreviewId)).status).toBe(409);
      expect(scalar("SELECT COUNT(*) FROM fuel_ledger")).toBe(0); expect(scalar("SELECT COUNT(*) FROM runs")).toBe(0);
      expect(scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
    }
  });
  it("feeds accepted start into the existing finish/reward-preview flow with a full policy", async () => {
    const { preview, start, sql, scalar, post } = await setup();
    sql.run("UPDATE balance_config_versions SET config_json=?", [JSON.stringify({ fuel: { runStartCost: 10 }, runRewards: launchRunRewardsConfig })]);
    const p = await (await preview()).json(), r = await (await start(p.operationPreviewId, "run")).json();
    const finish = await post(`/v1/runs/${r.runId}/finish`, { runId: r.runId, currentStage: "Complete", currentWaveNumber: 0, completedWaveCount: 15,
      outcome: "VICTORY", runtimeStateJson: "{}", tacticSelections: [{ packageRef: "act1_full_start_package_v1" }], clientCompletedAt: new Date().toISOString() });
    expect(finish.status).toBe(200); expect(await finish.json()).toMatchObject({ accepted: true, resultStatus: "ACCEPTED", balanceVersion: "1" });
    expect(scalar("SELECT status FROM runs")).toBe("COMPLETED");
    expect(scalar("SELECT COUNT(*) FROM run_reward_previews")).toBe(1);
    expect(scalar("SELECT fuel FROM fuel_state")).toBe(20);
    expect(await (await start(p.operationPreviewId, "run")).json()).toEqual(r);
  });
  it("does not quote paid departure when the active policy cannot reward that operation", async () => {
    const { preview, sql, scalar } = await setup();
    sql.run("UPDATE balance_config_versions SET config_json='{\"fuel\":{\"runStartCost\":10}}'");
    expect((await preview()).status).toBe(503);
    expect(scalar("SELECT COUNT(*) FROM operation_previews")).toBe(0);
  });
});
