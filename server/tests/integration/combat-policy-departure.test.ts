import { expect, it } from "vitest";
import { setup, body } from "../helpers/departure-fixture";
import { captureCombatPolicyRequirements } from "../../apps/game-api/src/combat-policy-catalog";
it("pins the identical latest catalog in preview and committed START", async () => {
  const f = await setup(true), q = await (await f.preview(body, "catalog-quote")).json();
  const preview = JSON.parse(String(f.scalar("SELECT preview_json FROM operation_previews")));
  expect(preview.admission.version).toBe("departure-v4");
  expect(preview.admission.combatPolicyRequirements).toEqual(captureCombatPolicyRequirements());
  const response = await (await f.start(q.operationPreviewId, "catalog-start")).json();
  expect(response.admission.combatPolicyRequirements).toEqual(preview.admission.combatPolicyRequirements);
  const stored = JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
  expect(stored.departureResponse.admission.combatPolicyRequirements).toEqual(preview.admission.combatPolicyRequirements);
});
it("rejects altered policy quotes before any fuel or run write", async () => {
  const f = await setup(true), q = await (await f.preview(body, "catalog-quote")).json();
  const before = f.scalar("SELECT fuel FROM fuel_state");
  const preview = JSON.parse(String(f.scalar("SELECT preview_json FROM operation_previews")));
  preview.admission.combatPolicyRequirements.ultimate.cost = 200;
  f.sql.run("UPDATE operation_previews SET preview_json=?", [JSON.stringify(preview)]);
  expect(await (await f.start(q.operationPreviewId, "catalog-start")).json()).toMatchObject({ errorCode: "departure_quote_changed" });
  expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(before);
  expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
});
it("preserves committed historical snapshots instead of injecting the latest catalog on replay", async () => {
  const f = await setup(true), q = await (await f.preview(body, "catalog-quote")).json();
  await f.start(q.operationPreviewId, "catalog-start");
  const stored = JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
  for (const object of [stored.admission, stored.departureResponse.admission]) {
    object.version = "departure-v3";
    delete object.combatPolicyRequirements;
  }
  const historical = JSON.stringify(stored);
  f.sql.run("UPDATE run_snapshots SET snapshot_json=?", [historical]);
  f.sql.run("UPDATE operation_previews SET admission_version='departure-v3'");
  expect(await (await f.start(q.operationPreviewId, "catalog-start")).json()).toEqual(stored.departureResponse);
  expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(historical);
});

it("replays already committed v4 legacy catalog bytes and rejects obsolete unconsumed quotes",async()=>{
 const {readPinnedCombatPolicyRequirements}=await import("../../apps/game-api/src/combat-policy-catalog");
 const f=await setup(true),q=await(await f.preview(body,"old-v4")).json();await f.start(q.operationPreviewId,"old-v4-start");
 const stored=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots"))),legacy=readPinnedCombatPolicyRequirements("combat.policy-catalog.2026-09-22.1");
 stored.admission.combatPolicyRequirements=legacy;stored.departureResponse.admission.combatPolicyRequirements=legacy;
 const original=JSON.stringify(stored);f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[original]);
 expect(await(await f.start(q.operationPreviewId,"old-v4-start")).json()).toEqual(stored.departureResponse);expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(original);
 const other=await setup(true),q2=await(await other.preview()).json(),p=JSON.parse(String(other.scalar("SELECT preview_json FROM operation_previews")));
 p.admission.combatPolicyRequirements=legacy;other.sql.run("UPDATE operation_previews SET preview_json=?",[JSON.stringify(p)]);
 expect(await(await other.start(q2.operationPreviewId)).json()).toMatchObject({errorCode:"departure_quote_changed"});expect(other.scalar("SELECT count(*) FROM runs")).toBe(0);
});
