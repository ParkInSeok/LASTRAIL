import { expect, it } from "vitest";
import { resolveApprovedOperatorShieldBase, replaceSameSourceShield } from "../../apps/game-api/src/combat-shield-budget";

const base = (overrides: Record<string, unknown> = {}) => resolveApprovedOperatorShieldBase({
  policyVersion: "combat.policy-catalog.2026-09-29.1", operatorId: "op_kyle",
  abilityId: "op_kyle.active", casterMaxHp: 3024, ...overrides,
});
const replace = (overrides: Record<string, unknown> = {}) => replaceSameSourceShield({
  sourceId: "kyle-1:op_kyle.active", recipientId: "ally-1", issuedAtMs: 1000,
  amount: 544, durationMs: 6000, existing: null, ...overrides,
});

it("calculates the approved unmodified Kyle base from caster HP", () => {
  expect(base()).toEqual({ version: "combat.operator-shield-base.v1", abilityId: "op_kyle.active",
    amount: 544, durationMs: 6000 });
  expect(base({ abilityId: "op_kyle.ultimate" })).toMatchObject({ amount: 665, durationMs: 8000 });
});

it("replaces a same-source layer using the producer's final amount after modifiers", () => {
  const existing = { sourceId: "kyle-1:op_kyle.active", recipientId: "ally-1", amount: 600, expiresAtMs: 3000 };
  expect(replace({ existing })).toMatchObject({ amount: 600, expiresAtMs: 3000, changed: false });
  expect(replace({ existing, amount: 620 })).toMatchObject({ amount: 620, expiresAtMs: 7000, changed: true });
  expect(replace({ issuedAtMs: 4000, existing })).toMatchObject({ amount: 544, expiresAtMs: 10000, changed: true });
});

it("rejects unapproved definitions and unrelated existing layers", () => {
  expect(() => base({ policyVersion: "combat.policy-catalog.2026-09-22.1" })).toThrow("combat_shield_policy_unsupported");
  expect(() => base({ operatorId: "op_sera" })).toThrow("combat_shield_ability_unsupported");
  expect(() => replace({ existing: { sourceId: "kyle-2:op_kyle.active", recipientId: "ally-1", amount: 5, expiresAtMs: 3000 } })).toThrow("combat_shield_source_mismatch");
});
