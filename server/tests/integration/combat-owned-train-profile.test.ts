import { expect, it } from "vitest";
import { writeFile } from "node:fs/promises";
import { setup } from "../helpers/departure-fixture";
import { readOwnedTrainCombatProfile } from "../../apps/game-api/src/combat-owned-train-profile";
import { readOwnedCombatProfile } from "../../apps/game-api/src/combat-owned-profile";

const request = { expectedFormationRevision: 1, expectedLoadoutRevision: 1 };
it("binds actual train and selected carriage levels to approved base and current DEF", async () => {
  const f = await setup(true, false);
  f.sql.run("UPDATE account_train SET level=6,revision=2 WHERE account_id='a'");
  f.sql.run("UPDATE account_carriages SET level=6,revision=2 WHERE account_id='a'");
  const p = await readOwnedTrainCombatProfile(f.env.DB, "a", request);
  expect(p.selectedTrain).toMatchObject({ trainId: "train_a", level: 6, revision: 2, role: "TRAIN",
    permanentStats: { maxHp: 7000, attack: 78, defense: 111, basicIntervalMs: 1000 } });
  expect(p.selectedCarriages).toHaveLength(2);
  expect(p.selectedCarriages[0]).toMatchObject({ carriageId: "car_combat", slot: 0, role: "COMBAT",
    permanentStats: { maxHp: 1540, defense: 111 }, deviceInput: { amount: 31, intervalMs: 1600 } });
  expect(p.selectedCarriages[1]).toMatchObject({ carriageId: "car_gun", role: "GUN",
    permanentStats: { maxHp: 1400, defense: 87 }, deviceInput: null, ownsIndependentBasicFire: false });
  expect(p.canExecute).toBe(false);
  expect(p.turretSlots).toEqual([1, 2].map(slot => ({ carriageId: "car_gun", slot, ownerAccountId: "a",
    turretId: null, entityId: null, status: "SELECTION_UNBOUND" })));
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-owned-train-observed-sql-test-20261001.json",
    JSON.stringify({ evidence: "LOCAL_SQL_TEST_SYNTHETIC_ACCOUNT_NOT_DEPLOYED_PROFILE", profile: p }, null, 2) + "\n", "utf8");
});
it("does not treat lifetime-scoped run equipment or unapproved research as a prerequisite", async () => {
  const f = await setup(true, false);
  const before = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(before.definitionHash).toBe("sha256:9d25a88d64239127cbecb7816a4504244947fe55957cd9eb50ec9e4e75bce515");
  const p = await readOwnedTrainCombatProfile(f.env.DB, "a", request);
  expect(before.missing).toContain("predeparture_equipment_binding_unavailable");
  expect(p.missing).not.toContain("predeparture_equipment_binding_unavailable");
  expect(p.missing).not.toContain("account_research_ledger_unavailable");
  expect(p.applicability).toMatchObject({ equipment: { predepartureRequired: false, lifecycle: "RUN_OWNED" },
    research: { predepartureRequired: false, status: "NO_APPROVED_ACTIVE_NODE_CATALOG" } });
  expect(p.missing).not.toContain("run_equipment_effect_input_binding_unbound");
  expect(p.deferredInputs).toContainEqual({ code: "run_equipment_effect_input_binding_unbound",
    stage: "RUN_ITEM_EFFECT_CONSUMPTION", requiredWhen: "ACTUAL_EQUIPPED_RUN_ITEMS_EXIST" });
  expect(p.missing).toContain("turret_slot_selection_not_persisted:car_gun");
  expect(await readOwnedCombatProfile(f.env.DB, "a", request)).toEqual(before);
});

it("uses actual separate carriage levels, leaves absent base definitions missing and pins source revisions", async () => {
  const f = await setup(true, false);
  f.sql.run("UPDATE account_train SET level=10,revision=2 WHERE account_id='a'");
  const p = await readOwnedTrainCombatProfile(f.env.DB, "a", request);
  expect(p.selectedTrain.permanentStats).toMatchObject({ maxHp: 8600, attack: 92, defense: 111 });
  expect(p.selectedCarriages[0]?.permanentStats?.maxHp).toBe(1100);
  f.sql.run("UPDATE account_carriages SET level=6,revision=2 WHERE account_id='a' AND carriage_id='car_combat'");
  const q = await readOwnedTrainCombatProfile(f.env.DB, "a", request);
  expect(q.sourceHash).not.toBe(p.sourceHash);
  expect(q.selectedCarriages[0]?.permanentStats?.maxHp).toBe(1540);
  expect(p.selectedCarriages[0]?.permanentStats?.maxHp).toBe(1100);
  f.progress(["opn_03_water_stop", "opn_05_night_boarders"]);
  expect((await f.install(["car_combat", "car_gun", "car_cargo", "car_defense"], 1)).status).toBe(200);
  const d = await readOwnedTrainCombatProfile(f.env.DB, "a", { ...request, expectedLoadoutRevision: 2 });
  expect(d.selectedCarriages[3]).toMatchObject({ carriageId: "car_defense", role: "DEFENSE", permanentStats: null });
  expect(d.missing).toContain("carriage_base_definition_missing:car_defense");
});
it("preserves owned turret kinds without inferring installed slots or mapping unknown definitions", async () => {
  const f = await setup(true, false);
  f.sql.run("INSERT INTO account_turrets(account_id,turret_id,level,revision) VALUES('a','unknown_kind',6,4)");
  const p = await readOwnedTrainCombatProfile(f.env.DB, "a", request);
  expect(p.ownedTurrets[0]).toMatchObject({ turretId: "unknown_kind", level: 6, revision: 4, permanentStats: null });
  expect(p.missing).toContain("turret_kind_definition_binding_missing:unknown_kind");
  expect(p.turretSlots.every(s => s.turretId === null)).toBe(true);
  expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
});
it("rejects ownership, revision and client installation input instead of silently correcting it", async () => {
  const f = await setup(true, false);
  await expect(readOwnedTrainCombatProfile(f.env.DB, "b", request)).rejects.toMatchObject({ code: "combat_owned_selection_missing" });
  await expect(readOwnedTrainCombatProfile(f.env.DB, "a", { ...request, expectedLoadoutRevision: 2 }))
    .rejects.toMatchObject({ code: "combat_owned_selection_changed" });
  await expect(readOwnedTrainCombatProfile(f.env.DB, "a", { ...request, turretSlots: [{ slot: 3, turretId: "free" }] }))
    .rejects.toMatchObject({ code: "combat_owned_request_invalid" });
  f.sql.run("UPDATE account_train_loadout SET loadout_json=? WHERE account_id='a'", [JSON.stringify({ rulesVersion: "train-loadout-v1", trainId: "someone_else", carriages: [{ slot: 0, carriageId: "car_combat" }, { slot: 1, carriageId: "car_gun" }] })]);
  await expect(readOwnedTrainCombatProfile(f.env.DB, "a", request)).rejects.toMatchObject({ code: "train_not_owned" });
});
