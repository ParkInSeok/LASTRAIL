import { z } from "zod";
import { ExactCombatValue, exactCombatValueSchema } from "./combat-exact-value";
export const directDamageBudgetVersion = "combat.direct-damage-budget.v1";
const bps = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const effectSchema = z.strictObject({
  // Stable composite identity supplied by the authoritative resolver, including owner/effect identity.
  // Copies of a partner-owned source must keep the same identity; do not mint a local duplicate.
  sourceId: z.string().min(1).max(400),
  group: z.enum(["EQUIPMENT_DIRECT", "NON_EQUIPMENT_OUTGOING", "MARK"]), valueBps: bps,
});
export const directDamageInputSchema = z.strictObject({ base: exactCombatValueSchema, effects: z.array(effectSchema).max(1024),
  defenseBps: bps.max(10000), incomingReductionBps: bps.max(10000) });
const SCALE = 10000n, OUTGOING_CAP = 3000n, MARK_CAP = 2000n;
const min = (a: bigint, b: bigint) => a < b ? a : b;
/** Caller supplies the source magnitude once and only condition/ownership-eligible effects.
 * This resolver cannot authorize effects, health changes or score awards from client input.
 */
export function resolveDirectDamageBudget(input: unknown) {
  const parsed = directDamageInputSchema.parse(input);
  const unique = new Map<string, z.infer<typeof effectSchema>>();
  for (const effect of parsed.effects) {
    const previous = unique.get(effect.sourceId);
    if (previous && (previous.group !== effect.group || previous.valueBps !== effect.valueBps)) throw new Error("combat_effect_source_conflict");
    unique.set(effect.sourceId, effect);
  }
  let equipment = 0n, outgoing = 0n, mark = 0n;
  for (const effect of unique.values()) {
    const magnitude = BigInt(effect.valueBps);
    if (effect.group === "EQUIPMENT_DIRECT") equipment += magnitude;
    else if (effect.group === "NON_EQUIPMENT_OUTGOING") outgoing += magnitude;
    else if (magnitude > mark) mark = magnitude;
  }
  let result = ExactCombatValue.from(parsed.base);
  for (const numerator of [SCALE + equipment, SCALE + min(outgoing, OUTGOING_CAP), SCALE + min(mark, MARK_CAP),
    SCALE - BigInt(parsed.defenseBps), SCALE - BigInt(parsed.incomingReductionBps)]) {
    result = result.multiply(ExactCombatValue.from({ numerator: numerator.toString(), denominator: SCALE.toString() }));
  }
  return { version: directDamageBudgetVersion, exact: result.snapshot(), ordinaryBeforeShield: result.floor() };
}
