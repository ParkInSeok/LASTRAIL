import { z } from 'zod';
import { ExactCombatValue } from './combat-exact-value';
import { numericDefenseInputSchema, resolveNumericDefenseBudget } from './combat-numeric-defense';

export const nyxMitigationVersion = 'combat.nyx-mitigation-budget.v1.20261107';
const schema = z.strictObject({ version: z.literal(nyxMitigationVersion), original: numericDefenseInputSchema,
  mitigationRateReductions: z.array(z.strictObject({ sourceId: z.string().min(1).max(400),
    valueBps: z.union([z.literal(800), z.literal(1500)]) })).max(1024) });

/** Eligible effects come from the authoritative zone owner. This arithmetic cannot authorize membership, HP writes, or START. */
export function resolveNyxMitigationBudget(input: unknown) {
  const value = schema.parse(input), original = value.original;
  const occupied = new Set([...original.effects, ...original.weakening, ...original.penetration,
    ...original.reductions.equipment, ...original.reductions.guardStance].map(source => source.sourceId));
  const unique = new Map<string, number>();
  let strongest = 0;
  for (const effect of value.mitigationRateReductions) {
    if (occupied.has(effect.sourceId) || unique.has(effect.sourceId) && unique.get(effect.sourceId) !== effect.valueBps)
      throw new Error('combat_effect_source_conflict');
    unique.set(effect.sourceId, effect.valueBps); strongest = Math.max(strongest, effect.valueBps);
  }
  const previous = resolveNumericDefenseBudget(original), defense = previous.effectiveDefense;
  const k = 1000n * BigInt(defense.denominator), denominator = k + BigInt(defense.numerator);
  const raised = k * 10000n + BigInt(strongest) * denominator;
  const capped = raised < denominator * 10000n ? raised : denominator * 10000n;
  // Replace only the old DEF factor in the unrounded result; all existing offense/reduction groups remain once.
  const exact = ExactCombatValue.from(previous.exact).multiply(ExactCombatValue.from({
    numerator: capped.toString(), denominator: (k * 10000n).toString(),
  }));
  return { version: nyxMitigationVersion, effectiveDefense: defense, appliedMitigationRateReductionBps: strongest,
    exact: exact.snapshot(), ordinaryBeforeShield: exact.floor() };
}
