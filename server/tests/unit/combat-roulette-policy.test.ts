import { describe, expect, it } from "vitest";
import { combatRouletteMultiplier, multiplyCombatRewards } from "../../apps/game-api/src/combat-roulette-policy";

describe("approved combat roulette policy", () => {
  it.each([[0,2],[5999,2],[6000,3],[8999,3],[9000,4],[9999,4]])("draw %s maps to total multiplier %s", (draw, expected) => {
    expect(combatRouletteMultiplier(draw)).toBe(expected);
  });
  it.each([-1,10000,1.5,NaN,Infinity])("rejects invalid draw %s", draw => {
    expect(() => combatRouletteMultiplier(draw)).toThrow();
  });
  it("uses the same multiplier for repeat currencies and preserves first-clear/rare/item lines", () => {
    const lines = [
      {kind:"CURRENCY",id:"cur_credits",quantity:240,source:"RUN_CLEAR"},
      {kind:"CURRENCY",id:"cur_machine_parts",quantity:4,source:"RUN_CLEAR"},
      {kind:"CURRENCY",id:"cur_tactical_data",quantity:2,source:"RUN_CLEAR"},
      {kind:"CURRENCY",id:"cur_operator_exp",quantity:450,source:"RUN_CLEAR"},
      {kind:"CURRENCY",id:"cur_credits",quantity:100,source:"FIRST_CLEAR"},
      {kind:"CURRENCY",id:"cur_special_core",quantity:1,source:"RUN_CLEAR"},
      {kind:"ITEM",id:"cur_command_ticket",quantity:1,source:"FIRST_CLEAR"},
      {kind:"CURRENCY",id:"cur_credits",quantity:9,source:"UNKNOWN"},
    ];
    const original = structuredClone(lines);
    expect(multiplyCombatRewards(lines,3).map(l => l.quantity)).toEqual([720,12,6,1350,100,1,1,9]);
    expect(lines).toEqual(original);
  });
  it("rejects invalid quantities, multipliers and overflowing grants", () => {
    const line={kind:"CURRENCY",id:"cur_credits",quantity:Number.MAX_SAFE_INTEGER,source:"RUN_CLEAR"};
    expect(() => multiplyCombatRewards([line],4)).toThrow();
    for (const quantity of [-1,0.1,NaN]) expect(() => multiplyCombatRewards([{...line,quantity}],2)).toThrow();
    expect(() => multiplyCombatRewards([{...line,quantity:1}],5 as 2)).toThrow();
  });
});
