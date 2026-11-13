import { expect, it } from "vitest";
import { writeFile } from "node:fs/promises";
import { setup } from "../helpers/departure-fixture";
import { readOwnedCombatProfile, requireCompleteOwnedCombatProfile } from "../../apps/game-api/src/combat-owned-profile";

const ids = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];
const request = { expectedFormationRevision: 1, expectedLoadoutRevision: 1 };
async function owned() {
  const f = await setup(true, false);
  ids.forEach((id, slot) => {
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [id]);
    f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level) VALUES('a',?,?,?)", [slot, id, 10 + slot]);
  });
  f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators: ids.map((operatorId, slot) => ({ slot, operatorId })) })]);
  return f;
}
it("reads owned SQL state and resolves approved growth without a fixture execution profile", async () => {
  const f = await owned();
  f.sql.run("UPDATE account_operators SET promotion=2,skill_training=3,revision=2 WHERE account_id='a' AND operator_id='op_kyle'");
  const a = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(a.operators).toHaveLength(5);
  expect(a.operators[0]).toMatchObject({ operatorId: "op_kyle", level: 10, promotion: 2,
    skillTraining: 3, permanentStats: { maxHp: 1999, attack: { numerator: "4970700000", denominator: "100000000" }, defense: 250 },
    skillMagnitude: { numerator: "11000", denominator: "10000" } });
  expect(a.operators[0]?.sources.operator.revision).toBe(2);
  expect(a.sourceHash).toMatch(/^sha256:[a-f0-9]{64}$/u);
  expect(a.missing).toContain("account_research_ledger_unavailable");
  expect(a.missing).toContain("predeparture_equipment_binding_unavailable");
  expect(a.canExecute).toBe(false);
  expect(() => requireCompleteOwnedCombatProfile(a)).toThrow("combat_owned_profile_incomplete");
  expect(await readOwnedCombatProfile(f.env.DB, "a", request)).toEqual(a);
  expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json",
    JSON.stringify({ evidence: "LOCAL_SQL_TEST_SYNTHETIC_ACCOUNT_NOT_DEPLOYED_PROFILE", profile: a }, null, 2) + "\n", "utf8");
});
it("rejects a different owner, changed selection and an unowned selected entity", async () => {
  const f = await owned();
  await expect(readOwnedCombatProfile(f.env.DB, "b", request)).rejects.toMatchObject({ code: "combat_owned_selection_missing" });
  await expect(readOwnedCombatProfile(f.env.DB, "a", { ...request, expectedFormationRevision: 2 })).rejects.toMatchObject({ code: "combat_owned_selection_changed" });
  f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators: ids.map((operatorId, slot) => ({ slot, operatorId: slot === 0 ? "not_owned" : operatorId })) })]);
  await expect(readOwnedCombatProfile(f.env.DB, "a", request)).rejects.toMatchObject({ code: "combat_owned_operator_not_owned" });
});
it("never fills incomplete training data with level1 and changes source identity on revision", async () => {
  const f = await owned();
  const a = await readOwnedCombatProfile(f.env.DB, "a", request);
  f.sql.run("UPDATE operator_training_core_slots SET trained_level=30,revision=2 WHERE account_id='a' AND slot_index=0");
  const b = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(b.sourceHash).not.toBe(a.sourceHash);
  expect(b.operators[0]?.permanentStats?.maxHp).toBe(3024);
  expect(a.operators[0]?.permanentStats?.maxHp).toBe(1904);
  f.sql.run("DELETE FROM operator_training_core_slots WHERE account_id='a' AND slot_index=4");
  const c = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(c.missing).toContain("training_core_incomplete");
  expect(c.operators.every(o => o.permanentStats === null)).toBe(true);
});
it("preserves five slots and rejects unknown base definitions without narrowing the formation", async () => {
  const f = await owned();
  f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators: ids.map((operatorId, slot) => ({ slot, operatorId: slot === 0 ? "op0" : operatorId })) })]);
  const p = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(p.operators).toHaveLength(5);
  expect(p.operators[0]?.permanentStats).toBeNull();
  expect(p.missing).toContain("operator_base_definition_missing:op0");
});

it("uses the owned core level or complete-five minimum and latest cumulative promotion", async () => {
  const f = await owned();
  f.sql.run("UPDATE account_operators SET promotion=1 WHERE account_id='a' AND operator_id='op0'");
  f.sql.run("UPDATE operator_training_core_slots SET operator_id='op0' WHERE account_id='a' AND slot_index=0");
  f.sql.run("UPDATE account_operators SET promotion=1 WHERE account_id='a' AND operator_id='op_kyle'");
  const p = await readOwnedCombatProfile(f.env.DB, "a", request);
  expect(p.operators[0]?.level).toBe(10);
  expect(p.operators[1]?.level).toBe(11);
  // Promotion I changes HP only; no historical +5% ATK interpretation.
  expect(p.operators[0]?.permanentStats?.attack.numerator).toBe("4734000000");
  expect(p.operators[0]?.permanentStats?.maxHp).toBe(1999);
});

it("rejects pinned source/version mismatches and corrupt raw growth instead of resetting it", async () => {
  const f = await owned();
  const p = await readOwnedCombatProfile(f.env.DB, "a", request);
  await expect(readOwnedCombatProfile(f.env.DB, "a", { ...request, expectedDefinitionVersion: "historical" }))
    .rejects.toMatchObject({ code: "combat_owned_source_changed" });
  await expect(readOwnedCombatProfile(f.env.DB, "a", { ...request, expectedPolicyVersion: "combat.policy-catalog.2026-09-22.1" }))
    .rejects.toMatchObject({ code: "combat_owned_source_changed" });
  f.sql.run("UPDATE account_operators SET revision=2 WHERE account_id='a' AND operator_id='op_kyle'");
  await expect(readOwnedCombatProfile(f.env.DB, "a", { ...request, expectedSourceHash: p.sourceHash }))
    .rejects.toMatchObject({ code: "combat_owned_source_changed" });
  f.sql.run("UPDATE operator_training_core_slots SET trained_level=999 WHERE account_id='a' AND slot_index=0");
  await expect(readOwnedCombatProfile(f.env.DB, "a", request)).rejects.toMatchObject({ code: "combat_owned_source_invalid" });
});
