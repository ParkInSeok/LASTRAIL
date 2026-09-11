import { describe, expect, it } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import app, { resolveAuthContext } from "../../apps/game-api/src/index";
import { body } from "../helpers/departure-fixture";
import { ce03Inputs as inputs, ce03OperatorIds as ids, ce03ProducerFixture as fixture } from "../helpers/ce03-producer-fixture";
import { registerLocalCombatProducer, prepareLocalCombatDraft, finalizeLocalCombatArtifacts } from "../../apps/game-api/src/ce03-local-producer";
import { bindingSchema, profileSchema } from "../../apps/game-api/src/ce03-local-schema";
import { validateLocalBindingClosure } from "../../apps/game-api/src/ce03-local-producer";

describe("isolated CE03 producer without start/debit", () => {
  it("authenticates actual disposable ownership, produces closed draft, and never writes or declares ready", async () => {
    const f = await fixture(), before = f.sql.export();
    const response = await f.request(); expect(response.status).toBe(200);
    const result = await response.json();
    expect(result).toMatchObject({ ready: false, status: "MISSING_EXECUTION_EVIDENCE", missing: ["executionEvidence"] });
    expect(result.draft.template.selectedUltimateOperatorId).toBe("op_reina");
    expect(result.draft.template.operators.map((o: { operatorId: string }) => o.operatorId)).toEqual(ids);
    expect(result.ownedSelection.ownedState.trainingCoreSlots[0].revision).toBe(1);
    expect(result.draft.template.waves).toHaveLength(15);
    expect(result.draft.template.waves[14].spawns.map((s: { resolvedMaxHp: number }) => s.resolvedMaxHp)).toEqual([564,564,423,423,1185]);
    expect(result.draft.bindings).toHaveLength(226);
    expect(result.draft.bindings[0]).not.toHaveProperty("assetPath");
    expect(result.draft.bindings[0].importValidation).not.toHaveProperty("importerType");
    expect(Buffer.from(before).equals(Buffer.from(f.sql.export()))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    expect((await app.request("/__local/ce03/prepare", { method: "POST", headers: f.headers, body: JSON.stringify(body) }, f.env)).status).toBe(404);
  });
  it("rejects missing auth, caller owner/stats, stale revisions and non-loopback access", async () => {
    const f = await fixture();
    expect((await f.request(body, { ...f.headers, Authorization: "" })).status).toBe(401);
    expect((await f.request({ ...body, accountId: "b", skillRank: 5 })).status).toBe(400);
    expect((await f.request({ ...body, expectedUltimateRevision: 0 })).status).toBe(409);
    expect((await f.local.request("https://example.com/__local/ce03/prepare", { method: "POST", headers: f.headers, body: JSON.stringify(body) }, f.env)).status).toBe(403);
    f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");
    expect((await f.request()).status).toBe(401);
  });
  it("projects catalog types by owned ID with exact revisions and rejects swapped or missing ownership", async () => {
    const f = await fixture();
    f.sql.run("UPDATE account_carriages SET revision=7 WHERE carriage_id='car_combat'");
    f.sql.run("UPDATE account_carriages SET revision=9 WHERE carriage_id='car_gun'");
    const before = f.sql.export();
    const result = await (await f.request()).json();
    expect(result.carriageOwnership).toEqual({ rulesVersion: "train-loadout-v1", loadoutRevision: result.ownedSelection.loadoutRevision,
      carriages: [{ carriageId: "car_combat", slot: 0, level: 1, revision: 7, type: "Combat" },
        { carriageId: "car_gun", slot: 1, level: 1, revision: 9, type: "Gun" }] });
    expect(Buffer.from(before).equals(Buffer.from(f.sql.export()))).toBe(true);
    const forged = await f.request({ ...body, carriageOwnership: result.carriageOwnership });
    expect(forged.status).toBe(400);
    const saved = f.sql.exec("SELECT loadout_json FROM account_train_loadout")[0]!.values[0]![0] as string;
    const swapped = JSON.parse(saved);
    swapped.carriages.forEach((c: { slot: number }) => { c.slot = 1 - c.slot; });
    f.sql.run("UPDATE account_train_loadout SET loadout_json=?", [JSON.stringify(swapped)]);
    const response = await f.request();
    expect(response.status).toBe(409);
    expect(await response.json()).not.toHaveProperty("carriageOwnership");
    f.sql.run("UPDATE account_train_loadout SET loadout_json=?", [saved]);
    f.sql.run("DELETE FROM account_carriages WHERE carriage_id='car_gun'");
    expect((await f.request()).status).toBe(403);
  });
  it("rejects absent core ownership, changed growth and implicit ultimate", async () => {
    const f = await fixture();
    f.sql.run("DELETE FROM operator_training_core_slots WHERE slot_index=0");
    expect((await f.request()).status).toBe(409);
    f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id) VALUES('a',0,'op_kyle')");
    f.sql.run("UPDATE account_operators SET skill_training=2,revision=revision+1 WHERE operator_id='op_kyle'");
    expect((await f.request()).status).toBe(409);
    f.sql.run("UPDATE account_operators SET skill_training=1 WHERE operator_id='op_kyle'");
    f.sql.run("UPDATE account_formation SET ultimate_operator_id=NULL");
    expect((await f.request()).status).toBe(409);
  });
  it("fails explicitly without execution evidence and refuses altered source evidence", async () => {
    const f = await fixture(), result = await (await f.request()).json();
    await expect(finalizeLocalCombatArtifacts(result.draft, undefined)).rejects.toMatchObject({ code: "ce03_execution_evidence_missing" });
    await expect(prepareLocalCombatDraft({ ...inputs(), selectionJson: inputs().selectionJson.replace('494', '495') }, result.ownedSelection))
      .rejects.toMatchObject({ code: "ce03_source_hash_mismatch" });
  });
  it("detects actual ownership revision change while preparing inputs", async () => {
    const f = await fixture(), source = inputs(), json = source.selectionJson;
    let changed = false;
    Object.defineProperty(source, "selectionJson", { get: () => {
      if (!changed) { changed = true; f.sql.run("UPDATE account_operators SET revision=revision+1 WHERE operator_id='op_kyle'"); }
      return json;
    } });
    const local = new Hono<{ Bindings: Env }>();
    registerLocalCombatProducer(local, resolveAuthContext, { inputs: source, disposableDatabase: f.env.DB, allowedAccountIds: ["a"] });
    const response = await local.request("http://127.0.0.1/__local/ce03/prepare", { method: "POST", headers: f.headers, body: JSON.stringify(body) }, f.env);
    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ errorCode: "ce03_selection_changed_during_prepare" });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
  });
  it("rejects invalid signed64 IDs, missing dependencies and wrong typed role", async () => {
    const f = await fixture(), { draft } = await (await f.request()).json();
    const b = structuredClone(draft.bindings[0]);
    b.locator.localFileId = "9223372036854775808";
    expect(bindingSchema.safeParse(b).success).toBe(false);
    b.locator.localFileId = "-0"; expect(bindingSchema.safeParse(b).success).toBe(false);
    const broken = structuredClone(draft.bindings); broken[0].dependencies.push("missing");
    expect(() => validateLocalBindingClosure(broken, draft.template)).toThrow("ce03_binding_dependency_invalid");
    draft.template.operators[0].presentation.controller = "binding.scene.interiorLane";
    expect(() => validateLocalBindingClosure(draft.bindings, draft.template)).toThrow("ce03_binding_role_invalid");
  });
  it("validates closed profile fields and rejects unknown effects/numeric coercion", async () => {
    const f = await fixture(), { draft } = await (await f.request()).json();
    // Test-only artifact bytes are executed below, never claimed as Unity conformance.
    const fixtureIdentity = (value: number) => value;
    const algorithmSource = fixtureIdentity.toString();
    const vectorsJson = JSON.stringify({ cases: [{ input: 1, expected: 1 }] });
    expect(fixtureIdentity(1)).toBe(1);
    const artifact = await finalizeLocalCombatArtifacts(draft, { scope: "DISPOSABLE_FIXTURE", algorithmSource, vectorsJson,
      verification: { passed: 1, failed: 0 } });
    expect(artifact.ready).toBe(false); expect(artifact.frozen).toBe(false);
    expect(profileSchema.safeParse(artifact.profile).success).toBe(true);
    const bad = structuredClone(artifact.profile); bad.template.operators[0].active.effects[0].type = "burn";
    expect(profileSchema.safeParse(bad).success).toBe(false);
    const extra = structuredClone(artifact.profile); extra.template.execution.ready = true;
    expect(profileSchema.safeParse(extra).success).toBe(false);
    const fractional = structuredClone(artifact.profile); fractional.template.train.stats.atk = 60.5;
    expect(profileSchema.safeParse(fractional).success).toBe(false);
    await expect(finalizeLocalCombatArtifacts(draft, { scope: "LOCAL_CONSUMER", algorithmSource: "# candidate policy", vectorsJson,
      verification: { passed: 1, failed: 0 } })).rejects.toMatchObject({ code: "ce03_execution_evidence_invalid" });
  });
});
