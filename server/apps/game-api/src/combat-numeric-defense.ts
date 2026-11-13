import { z } from "zod";
import { directDamageInputSchema, resolveDirectDamageBudget } from "./combat-damage-budget";
import { ExactCombatValue, exactCombatValueSchema } from "./combat-exact-value";

export const numericDefenseBudgetVersion = "combat.numeric-defense-budget.v1";
const SCALE = 10000n, DEF_K = 1000n, WEAKENING_CAP = 3000n, PENETRATION_CAP = 6000n, EQUIPMENT_CAP = 5000n;
const magnitude = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const sourceSchema = z.strictObject({ sourceId: z.string().min(1).max(400), valueBps: magnitude });
const sourcesSchema = z.array(sourceSchema).max(1024);
const independentSchema = z.array(z.strictObject({
  groupId: z.string().min(1).max(400).refine(id => id !== "EQ06" && id !== "guard_stance"),
  valueBps: magnitude.max(10000),
})).max(1024).refine(groups => new Set(groups.map(g => g.groupId)).size === groups.length);
export const numericDefenseInputSchema = directDamageInputSchema.pick({ base: true, effects: true }).extend({
  version: z.literal(numericDefenseBudgetVersion),
  defense: exactCombatValueSchema,
  weakening: sourcesSchema,
  penetration: sourcesSchema,
  reductions: z.strictObject({
    equipment: sourcesSchema,
    guardStance: z.array(sourceSchema.extend({ valueBps: magnitude.max(10000) })).max(1024),
    // Already resolved eligible groups, each exactly once; never individual unresolved sources.
    independent: independentSchema,
  }),
});
const min = (a: bigint, b: bigint) => a < b ? a : b;
const factor = (remainingBps: bigint) => ExactCombatValue.from({ numerator: remainingBps.toString(), denominator: SCALE.toString() });

/** 44 v06 ordinary/story arithmetic only, not effect authorization or a world profile.
 * The authoritative producer must resolve eligibility, DEF growth and legacy-effect mappings.
 * No defaults or automatic armor conversion: pinned historical v1 inputs retain their resolver.
 * Invulnerability precedes this calculation; shields, HP and effect/charge execution follow it.
 */
export function resolveNumericDefenseBudget(input: unknown) {
  const c = numericDefenseInputSchema.parse(input);
  const identities = new Map<string, { group: string; valueBps: number }>();
  for (const e of c.effects) {
    const old = identities.get(e.sourceId);
    if (old && (old.group !== e.group || old.valueBps !== e.valueBps)) throw new Error("combat_effect_source_conflict");
    identities.set(e.sourceId, { group: e.group, valueBps: e.valueBps });
  }
  const unique = (group: string, sources: z.infer<typeof sourceSchema>[]) => {
    const values: bigint[] = [];
    for (const source of sources) {
      const old = identities.get(source.sourceId);
      if (old) {
        if (old.group !== group || old.valueBps !== source.valueBps) throw new Error("combat_effect_source_conflict");
        continue;
      }
      identities.set(source.sourceId, { group, valueBps: source.valueBps });
      values.push(BigInt(source.valueBps));
    }
    return values;
  };
  const sum = (values: bigint[]) => values.reduce((a, b) => a + b, 0n);
  const strongest = (values: bigint[]) => values.reduce((a, b) => a > b ? a : b, 0n);
  const weakening = min(strongest(unique("DEF_WEAKENING", c.weakening)), WEAKENING_CAP);
  const penetration = min(sum(unique("DEF_PENETRATION", c.penetration)), PENETRATION_CAP);
  const equipment = min(sum(unique("EQ06", c.reductions.equipment)), EQUIPMENT_CAP);
  const stance = strongest(unique("guard_stance", c.reductions.guardStance));
  const effectiveDefense = ExactCombatValue.from(c.defense).multiply(factor(SCALE - weakening)).multiply(factor(SCALE - penetration)).snapshot();
  const kd = DEF_K * BigInt(effectiveDefense.denominator);
  const defenseFactor = ExactCombatValue.from({ numerator: kd.toString(), denominator: (kd + BigInt(effectiveDefense.numerator)).toString() });
  // Reuse the established offense groups with neutral legacy defense; ignore its rounded projection.
  const offense = resolveDirectDamageBudget({ base: c.base, effects: c.effects, defenseBps: 0, incomingReductionBps: 0 });
  let result = ExactCombatValue.from(offense.exact).multiply(defenseFactor);
  for (const reduction of [equipment, stance, ...c.reductions.independent.map(g => BigInt(g.valueBps))]) {
    result = result.multiply(factor(SCALE - reduction));
  }
  return { version: numericDefenseBudgetVersion, effectiveDefense, exact: result.snapshot(), ordinaryBeforeShield: result.floor() };
}
