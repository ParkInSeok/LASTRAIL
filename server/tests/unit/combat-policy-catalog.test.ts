import { expect, it } from "vitest";
import { captureCombatPolicyRequirements } from "../../apps/game-api/src/combat-policy-catalog";
it("pins the latest six skill definitions, not historical heal/mark/repair kits", () => {
  const policy = captureCombatPolicyRequirements();
  expect(policy.version).toBe("combat.policy-catalog.2026-09-29.1");
  expect(policy.status).toBe("REQUIRED_NOT_EXECUTABLE");
  expect(policy.skills).toHaveLength(6);
  expect(policy.skills.map(s => s.id)).toEqual(["op_eve.active", "op_eve.ultimate", "op_sera.active", "op_sera.ultimate", "op_mira.active", "op_mira.ultimate"]);
  expect(policy.skills[0]).toMatchObject({ effect: "SENTRY_CREATE", cooldownMs: 18000, windupMs: 500, recoveryMs: 300, damageBps: 4000, intervalMs: 800, lifeMs: 12000 });
  expect(policy.skills[3]).toMatchObject({ effect: "HEAL_AND_RECOVERY", healBps: 20000, recoveryReductionMs: 4000, minimumRemainingMs: 1000 });
  expect(policy.skills[4]).toMatchObject({ damageBps: 4500, shots: 6, intervalMs: 200 });
  expect(policy.skills[5]).toMatchObject({ damageBps: 4000, shots: 16, intervalMs: 250 });
});
it("pins passive source filters, latest policy references and cost versus capacity", () => {
  const p = captureCombatPolicyRequirements();
  expect(p.passives.find(x => x.operatorId === "op_eve")).toMatchObject({ source: "OWN_SENTRY", group: "NON_EQUIPMENT_OUTGOING", valueBps: 1000 });
  expect(p.passives.find(x => x.operatorId === "op_sera")).toMatchObject({ source: "OWN_HEAL", group: "HEAL_MAGNITUDE", valueBps: 1000 });
  expect(p.passives.find(x => x.operatorId === "op_mira")).toMatchObject({ source: "OWN_BASIC", group: "NON_EQUIPMENT_OUTGOING", valueBps: 1000 });
  expect(p.ultimate).toMatchObject({ cost: 100, baseCapacity: 100, expandedCapacity: 200, perBasicAction: 4, initialCharge: 0 });
  expect(p.references).toContain("combat-effect-group-map-v04");
});
it("returns independent snapshots without overwriting the catalog or widening approved scope", () => {
  const p = captureCombatPolicyRequirements();
  p.skills.length = 0;
  p.references.length = 0;
  expect(captureCombatPolicyRequirements().skills).toHaveLength(6);
  expect(captureCombatPolicyRequirements().scope).toEqual({ roster: ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"], ordinaryRisk: "I", expansion: "EXPLICIT_POLICY_REQUIRED" });
});
