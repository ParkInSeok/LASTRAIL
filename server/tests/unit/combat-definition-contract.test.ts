import { expect, it } from "vitest";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { publishCombatDefinitionContract, readCombatDefinitionContract, requireExecutableCombatDefinition } from "../../apps/game-api/src/combat-definition-contract";

it("publishes real current definitions with stable content identity, without fabricating a START profile", async () => {
  const a = await publishCombatDefinitionContract();
  const b = await publishCombatDefinitionContract();
  expect(a).toEqual(b);
  expect(a.definitionVersion).toBe("combat.policy-catalog.2026-09-29.1");
  expect(a.contentHash).toBe(`sha256:${await sha256Hex(canonicalJson(a.definition))}`);
  expect(a.definition.operators).toHaveLength(5);
  expect(a.definition.operators.flatMap(o => o.abilities)).toHaveLength(10);
  expect(a.definition.policy.numericDefense.operatorBase.op_kyle).toBe(250);
  expect(a.definition.operators[0]?.passive).toMatchObject({ valueFlat: 83 });
  expect(a.definition.operators[2]?.abilities[0]).toMatchObject({ defense: 0 });
  expect(a.definition.effectiveStatProfiles).toBeNull();
  expect(a.definition.startSnapshot).toBeNull();
  expect(a.canExecute).toBe(false);
  expect(await readCombatDefinitionContract(JSON.parse(JSON.stringify(a)))).toEqual(a);
});

it("rejects absent fields, unknown schema and historical versions instead of defaulting to current", async () => {
  const a = await publishCombatDefinitionContract();
  for (const value of [null, {}, { ...a, schemaVersion: "unknown" },
    { ...a, definitionVersion: "combat.policy-catalog.2026-09-22.1" }, { ...a, contentHash: undefined }]) {
    await expect(readCombatDefinitionContract(value)).rejects.toThrow("combat_definition_contract_mismatch");
  }
});

it("rejects changed definitions even if the caller recomputes the hash", async () => {
  const a = await publishCombatDefinitionContract();
  a.definition.operators[0]!.abilities[0]!.effect = "UNAPPROVED_EFFECT";
  a.contentHash = `sha256:${await sha256Hex(canonicalJson(a.definition))}`;
  await expect(readCombatDefinitionContract(a)).rejects.toThrow("combat_definition_contract_mismatch");
});

it("rejects invented execution evidence and reports missing inputs and handlers", async () => {
  const a = await publishCombatDefinitionContract();
  await expect(readCombatDefinitionContract({ ...a, canExecute: true })).rejects.toThrow("combat_definition_contract_mismatch");
  expect(a.blockingReasons).toContain("effective_stat_profiles_missing");
  expect(a.blockingReasons).toContain("combat_effect_handler_missing:SHIELD_AND_TAUNT");
  expect(() => requireExecutableCombatDefinition(a)).toThrow("combat_definition_execution_unavailable");
});

it("does not expose shared mutable catalog or published state", async () => {
  const a = await publishCombatDefinitionContract();
  a.definition.policy.numericDefense.operatorBase.op_kyle = 1;
  expect((await publishCombatDefinitionContract()).definition.policy.numericDefense.operatorBase.op_kyle).toBe(250);
});
