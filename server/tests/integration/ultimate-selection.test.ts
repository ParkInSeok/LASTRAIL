import { describe, expect, it } from "vitest";
import { Buffer } from "node:buffer";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { setup, body } from "../helpers/departure-fixture";

const path = "/v1/formation/ultimate";
const pick = (operatorId: string | null, expectedRevision = 0, expectedFormationRevision = 1) => ({ rulesVersion: "ultimate-selection-v1", operatorId, expectedRevision, expectedFormationRevision });
describe("explicit ultimate selection", () => {
  it('returns a complete v2 selection without shared-owner refresh',async()=>{
    const f=await setup(false,false),request={...pick('op1'),responseVersion:'ultimate.command.v2.20261021'};
    const response=await f.post(path,request,'v2');expect(response.status).toBe(200);const receipt=await response.json();
    expect(receipt).toMatchObject({contractVersion:request.responseVersion,refreshRequired:false,owners:{ultimateSelection:{kind:'SNAPSHOT',operatorId:'op1',revision:1,formationRevision:1,selectionStatus:'SELECTED'}},invalidates:['DEPARTURE_PREVIEW']});
    expect(await(await f.post(path,request,'v2')).json()).toEqual(receipt);
  });
  it("reads null without mutation and saves an owned active member with CAS and exact replay", async () => {
    const f = await setup(false, false);
    const before = f.sql.export();
    const get = await app.request(path, { headers: f.headers }, f.env);
    expect(get.status).toBe(200);
    expect(get.headers.get("Cache-Control")).toBe("no-store");
    expect(await get.json()).toMatchObject({ operatorId: null, revision: 0, selectionStatus: "UNSELECTED" });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    const first = await f.post(path, pick("op1"), "pick"); expect(first.status).toBe(200);
    const result = await first.json(); expect(result).toMatchObject({ operatorId: "op1", revision: 1 });
    expect(await (await f.post(path, pick("op1"), "pick")).json()).toEqual(result);
    expect((await f.post(path, pick("op2"), "pick")).status).toBe(409);
    expect((await f.post(path, pick("op2"))).status).toBe(409);
    expect((await f.post(path, pick("outsider", 1))).status).toBe(403);
    expect((await f.post(path, { ...pick("op2", 1), engineOutput: 100 })).status).toBe(400);
  });
  it("pins the selection and rejects a quote after selection changes", async () => {
    const f = await setup(true, false);
    await f.post(path, pick("op1"));
    const p = await (await f.preview({ ...body, expectedUltimateRevision: 1 })).json();
    expect(p.admission.ultimateSelection).toEqual({ rulesVersion: "ultimate-selection-v1", operatorId: "op1", revision: 1 });
    expect(p.combatContract).toMatchObject({ canStart: false, ultimateOperatorId: "op1" });
    await f.post(path, pick("op2", 1));
    expect((await f.start(p.operationPreviewId)).status).toBe(409);
    expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(30);
  });
  it("clears only removed active members, preserves selection on inactive save and retaining activation", async () => {
    const f = await setup(); // Explicit test fixture chooses op0, revision 1.
    const operators = Array.from({ length: 5 }, (_, slot) => ({ slot, operatorId: `op${slot}` }));
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op5',0)");
    const save = (ops: typeof operators, expectedRevision: number) => f.post("/v1/formation/presets/p/save", { expectedRevision, name: "updated", operators: ops });
    await save(operators.map(o => o.slot === 1 ? { ...o, operatorId: "op5" } : o), 1);
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(1);
    await f.post("/v1/formation/presets/p/activate", { expectedRevision: 2, expectedFormationRevision: 1 });
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe("op0");
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(1);
    await save(operators.map(o => o.slot === 0 ? { ...o, operatorId: "op5" } : o), 2);
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe("op0");
    const response = await f.post("/v1/formation/presets/p/activate", { expectedRevision: 3, expectedFormationRevision: 2 });
    expect(response.status).toBe(200);
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe(null);
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(2);
  });
  it("rolls back a selection clear together with failed activation", async () => {
    const f = await setup();
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op5',0)");
    await f.post("/v1/formation/presets/p/save", { expectedRevision: 1, name: "remove", operators: Array.from({ length: 5 }, (_, slot) => ({ slot, operatorId: slot === 0 ? "op5" : `op${slot}` })) });
    f.sql.run("CREATE TRIGGER fail_receipt BEFORE INSERT ON formation_receipts BEGIN SELECT RAISE(ABORT,'CHECK constraint failed: valid=1'); END");
    expect((await f.post("/v1/formation/presets/p/activate", { expectedRevision: 2, expectedFormationRevision: 1 })).status).toBe(409);
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe("op0");
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(1);
    expect(f.scalar("SELECT revision FROM account_formation")).toBe(1);
  });
  it("locks all open run states but allows exact committed replay and freezes the run selection", async () => {
    const f = await setup(true, false);
    const original = await (await f.post(path, pick("op2"), "original")).json();
    const p = await (await f.preview()).json();
    const started = await (await f.start(p.operationPreviewId)).json();
    expect(started).toMatchObject({ ultimateOperatorId: "op2", admission: { ultimateSelection: { operatorId: "op2", revision: 1 } } });
    for (const status of ["ACTIVE", "FINISH_PENDING", "QUARANTINED"]) {
      f.sql.run("UPDATE runs SET status=?", [status]);
      expect((await f.post(path, pick("op3", 1))).status).toBe(409);
      expect(await (await f.post(path, pick("op2"), "original")).json()).toEqual(original);
    }
    expect(JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots"))).departureResponse.ultimateOperatorId).toBe("op2");
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe("op2");
  });
  it("allows null legacy selection in the current core without inventing a leader", async () => {
    const f = await setup(true, false);
    const p = await (await f.preview({ ...body, expectedUltimateRevision: 0 })).json();
    expect(p.combatContract).toMatchObject({ canStart: false, ultimateOperatorId: null, ultimateSelectionRevision: 0 });
    expect(p.combatContract.blockingReasons).not.toContain("ultimate_selection_required");
    expect(p.combatContract.policyBinding.blockingReasons).toContain("combat_policy_operator_unsupported");
    expect((await f.start(p.operationPreviewId)).status).toBe(200);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
    expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
    expect((await f.preview({ ...body, expectedUltimateRevision: 1 })).status).toBe(409);
  });
  it("serializes competing CAS choices and rechecks a run inserted before commit", async () => {
    const f = await setup(false, false);
    const responses = await Promise.all([f.post(path, pick("op1")), f.post(path, pick("op2"))]);
    expect(responses.map(r => r.status).sort()).toEqual([200, 409]);
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(1);
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("INSERT INTO account_mutations VALUES('race','a',999,'RUN_START','race',0)");
      f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('race','a','race',1,'ACTIVE',0)");
    };
    expect((await f.post(path, pick("op3", 1))).status).toBe(409);
    expect(f.scalar("SELECT ultimate_revision FROM account_formation")).toBe(1);
  });
  it("rechecks ownership and selection during preview/start commit races", async () => {
    const f = await setup(true);
    const p = await (await f.preview()).json();
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("UPDATE account_formation SET ultimate_operator_id='op3',ultimate_revision=2");
    };
    expect((await f.start(p.operationPreviewId)).status).toBe(409);
    expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(30);
    f.sql.run("DELETE FROM account_operators WHERE operator_id='op3'");
    expect((await f.post(path, pick("op1", 2))).status).toBe(403);
  });
  it("isolates accounts, rejects absent/revoked auth, and supports explicit clear/no-op", async () => {
    const f = await setup();
    expect((await app.request(path, {}, f.env)).status).toBe(401);
    expect(await (await f.post(path, pick(null, 1))).json()).toMatchObject({ operatorId: null, revision: 2 });
    expect(await (await f.post(path, pick(null, 2))).json()).toMatchObject({ operatorId: null, revision: 2 });
    f.sql.run("UPDATE auth_sessions SET account_id='b'");
    expect((await f.post(path, pick("op0", 2))).status).toBe(401);
    f.sql.run("UPDATE auth_sessions SET account_id='a',status='REVOKED'");
    expect((await f.post(path, pick("op0", 2))).status).toBe(401);
  });
  it("never reads or replays another account selection", async () => {
    const f = await setup();
    await f.post(path, pick("op1", 1), "shared");
    f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('sb','b','hb','fb',0,'ACTIVE',0,?)", [Date.now() + 60000]);
    const token = await createAccessToken({ secret: "departure-test-secret-at-least-32-chars", accountId: "b", sessionId: "sb", nowMs: Date.now(), ttlMs: 60000 });
    const headers = { Authorization: `Bearer ${token}`, "Idempotency-Key": "shared" };
    expect(await (await app.request(path, { headers }, f.env)).json()).toMatchObject({ operatorId: null, revision: 0, selectionStatus: "FORMATION_REQUIRED" });
    expect((await app.request(path, { method: "POST", headers, body: JSON.stringify(pick("op1", 1)) }, f.env)).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM account_formation WHERE account_id='b'")).toBe(0);
  });
  it("rejects a preview if selection changes immediately before its transaction", async () => {
    const f = await setup();
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("UPDATE account_formation SET ultimate_operator_id='op2',ultimate_revision=2");
    };
    expect((await f.preview()).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM operation_previews")).toBe(0);
  });
  it("revalidates a selection after concurrent activation removes its target", async () => {
    const f = await setup();
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op5',0)");
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("UPDATE account_formation SET formation_json=json_set(formation_json,'$.operators[1].operatorId','op5'),revision=revision+1");
    };
    expect((await f.post(path, pick("op1", 1))).status).toBe(409);
    expect(f.scalar("SELECT ultimate_operator_id FROM account_formation")).toBe("op0");
  });
});
