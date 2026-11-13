import { z } from "zod";

export const operatorShieldBaseVersion = "combat.operator-shield-base.v1";
export const operatorShieldLayerVersion = "combat.operator-shield-layer.v1";
const currentPolicy = "combat.policy-catalog.2026-09-29.1";
const nonnegative = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const identity = z.string().min(1).max(160);
const layerSchema = z.strictObject({ sourceId: z.string().min(1).max(400), recipientId: identity,
  amount: nonnegative, expiresAtMs: nonnegative });
const baseSchema = z.strictObject({
  policyVersion: z.string(), operatorId: identity, abilityId: identity,
  casterMaxHp: nonnegative.positive(),
});
const replacementSchema = z.strictObject({
  sourceId: z.string().min(1).max(400), recipientId: identity, issuedAtMs: nonnegative,
  amount: nonnegative, durationMs: nonnegative.positive(), existing: layerSchema.nullable(),
});

// The same approved definitions are used by admission binding and shield arithmetic.
export const approvedKyleShieldAbilities = [
  { id: "op_kyle.active", effect: "SHIELD_AND_TAUNT", cooldownMs: 18000, windupMs: 350,
    recoveryMs: 300, shieldCasterHpBps: 1800, shieldDurationMs: 6000,
    tauntDurationMs: 4000, maximumFrontTargets: 2 },
  { id: "op_kyle.ultimate", effect: "SHIELD_LIVING_ALL", cost: 100, windupMs: 600,
    recoveryMs: 300, shieldCasterHpBps: 2200, shieldDurationMs: 8000 },
] as const;

// Retain the original above for historical policy and personal-shield publications.
export const approvedKyleTauntV2 = {
  ...approvedKyleShieldAbilities[0], maximumFrontTargets: 3,
  tauntPolicyVersion: "kyle-taunt.v2.20261109", tauntRangeMm: 12000,
  tauntLineOfSightRequired: true, tauntScope: "CURRENT_CARRIAGE_THREATS",
  tauntTargetOrder: "DISTANCE_THEN_ORIGINAL_ORDER_THEN_ID",
} as const;

/** Resolve only approved, unmodified Kyle base shield magnitude and lifetime.
 * Actor eligibility and source/recipient modifiers belong to the producer.
 */
export function resolveApprovedOperatorShieldBase(input: unknown) {
  const c = baseSchema.parse(input);
  if (c.policyVersion !== currentPolicy) throw new Error("combat_shield_policy_unsupported");
  if (c.operatorId !== "op_kyle") throw new Error("combat_shield_ability_unsupported");
  const ability = approvedKyleShieldAbilities.find(ability => ability.id === c.abilityId);
  if (!ability) throw new Error("combat_shield_ability_unsupported");
  const amount = Number(BigInt(c.casterMaxHp) * BigInt(ability.shieldCasterHpBps) / 10000n);
  return { version: operatorShieldBaseVersion, abilityId: ability.id,
    amount, durationMs: ability.shieldDurationMs };
}

/** Apply same-source nonstacking after the producer has resolved final magnitude.
 * Source identity is producer-owned; this does not authorize a cast or recipient.
 */
export function replaceSameSourceShield(input: unknown) {
  const c = replacementSchema.parse(input);
  if (c.existing && (c.existing.sourceId !== c.sourceId || c.existing.recipientId !== c.recipientId))
    throw new Error("combat_shield_source_mismatch");
  const expiry = c.issuedAtMs + c.durationMs;
  if (!Number.isSafeInteger(expiry)) throw new Error("combat_shield_time_overflow");
  const remaining = c.existing && c.existing.expiresAtMs > c.issuedAtMs ? c.existing.amount : 0;
  if (c.existing && c.existing.expiresAtMs > c.issuedAtMs && c.amount < remaining)
    return { version: operatorShieldLayerVersion, ...c.existing, changed: false };
  return { version: operatorShieldLayerVersion, sourceId: c.sourceId, recipientId: c.recipientId,
    amount: c.amount, expiresAtMs: expiry, changed: true };
}
