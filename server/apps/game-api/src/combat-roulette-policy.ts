/** User-approved 2026-09-16 policy. This calculator does not authorize payouts. */
export const combatRoulettePolicy = Object.freeze({
  version: "combat-clear-roulette-20260916-v1",
  weightTotal: 10000,
  outcomes: Object.freeze([
    Object.freeze({ multiplier: 2 as const, weight: 6000 }),
    Object.freeze({ multiplier: 3 as const, weight: 3000 }),
    Object.freeze({ multiplier: 4 as const, weight: 1000 }),
  ]),
});

export function combatRouletteMultiplier(draw: number): 2 | 3 | 4 {
  if (!Number.isSafeInteger(draw) || draw < 0 || draw >= combatRoulettePolicy.weightTotal) {
    throw new Error("combat_roulette_draw_invalid");
  }
  return draw < 6000 ? 2 : draw < 9000 ? 3 : 4;
}

export type RewardLine = { kind: string; id: string; quantity: number; source?: string };
const currencies = new Set(["cur_credits", "cur_machine_parts", "cur_tactical_data", "cur_operator_exp"]);
// Current server repeat rewards use RUN_CLEAR. Future destination sources must
// be registered deliberately after the server produces and validates them.
const repeatSources = new Set(["RUN_CLEAR"]);

export function isCombatRouletteEligible(line: RewardLine): boolean {
  return line.kind === "CURRENCY" && currencies.has(line.id) && repeatSources.has(line.source ?? "");
}

export function multiplyCombatRewards<T extends RewardLine>(lines: readonly T[], multiplier: 2 | 3 | 4): T[] {
  if (![2,3,4].includes(multiplier)) throw new Error("combat_roulette_multiplier_invalid");
  return lines.map(line => {
    if (!Number.isSafeInteger(line.quantity) || line.quantity < 0) throw new Error("combat_roulette_quantity_invalid");
    const eligible = isCombatRouletteEligible(line);
    const quantity = eligible ? line.quantity * multiplier : line.quantity;
    if (!Number.isSafeInteger(quantity)) throw new Error("combat_roulette_quantity_overflow");
    return {...line, quantity};
  });
}
