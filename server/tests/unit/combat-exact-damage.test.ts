import { expect, it } from "vitest";
import { ExactCombatValue } from "../../apps/game-api/src/combat-exact-value";
import { resolveDirectDamageBudget } from "../../apps/game-api/src/combat-damage-budget";
const value = (numerator: string, denominator = "1") => ({ numerator, denominator });
it("keeps 0.6 + 0.6 as 1.2 and floors only at the final boundary", () => {
  const total = ExactCombatValue.from(value("6", "10")).add(ExactCombatValue.from(value("6", "10")));
  expect(total.snapshot()).toEqual(value("6", "5"));
  expect(total.floor()).toBe("1");
  expect(ExactCombatValue.from(JSON.parse(JSON.stringify(total.snapshot()))).floor()).toBe("1");
});
it("preserves values beyond JS safe integers and repeating fractions exactly", () => {
  expect(ExactCombatValue.from(value("9007199254740993")).add(ExactCombatValue.from(value("1"))).floor()).toBe("9007199254740994");
  const third = ExactCombatValue.from(value("1", "3"));
  expect(third.add(third).add(third).snapshot()).toEqual(value("1"));
});
it.each([value("-1"),value("1", "0"),value("1.5"),value("1e4"),value("01"),value("1", "01"),value("9".repeat(257))])("rejects malformed or out of range external values %j", input => {
  expect(() => ExactCombatValue.from(input)).toThrow();
});
it("fails explicitly on arithmetic overflow without modifying the original", () => {
  const n = "9".repeat(256), original = ExactCombatValue.from(value(n));
  expect(() => original.add(original)).toThrow("combat_numeric_range_exceeded");
  expect(original.snapshot()).toEqual(value(n));
});
it("floors ordinary damage after defense, not before", () => {
  const r = resolveDirectDamageBudget({ base: value("19", "10"), effects: [], defenseBps: 1000, incomingReductionBps: 0 });
  expect(r.exact).toEqual(value("171", "100"));
  expect(r.ordinaryBeforeShield).toBe("1");
});
const effect = (sourceId: string, group: string, valueBps: number) => ({ sourceId, group, valueBps });
it("separates equipment, capped non-equipment and strongest mark multipliers", () => {
  const r = resolveDirectDamageBudget({ base: value("100"), effects: [effect("gear", "EQUIPMENT_DIRECT", 4000),
    effect("out", "NON_EQUIPMENT_OUTGOING", 5000), effect("mark1", "MARK", 1000), effect("mark2", "MARK", 4000)],
    defenseBps: 2000, incomingReductionBps: 1000 });
  expect(r.exact).toEqual(value("19656", "125"));
  expect(r.ordinaryBeforeShield).toBe("157");
});
it("deduplicates the same source and rejects conflicting or unknown group assignments", () => {
  const e = effect("same", "EQUIPMENT_DIRECT", 1000);
  expect(resolveDirectDamageBudget({ base: value("100"), effects: [e,e], defenseBps: 0, incomingReductionBps: 0 }).ordinaryBeforeShield).toBe("110");
  expect(() => resolveDirectDamageBudget({ base: value("100"), effects: [e,{...e,valueBps:2000}], defenseBps:0,incomingReductionBps:0 })).toThrow("combat_effect_source_conflict");
  expect(() => resolveDirectDamageBudget({ base: value("100"), effects: [effect("x","UNKNOWN",1000)], defenseBps:0,incomingReductionBps:0 })).toThrow();
});
it("never promotes zero to one or loses exact sub-unit world damage", () => {
  const r = resolveDirectDamageBudget({ base: value("3", "5"), effects: [], defenseBps:0,incomingReductionBps:0 });
  expect(r).toMatchObject({ exact: value("3", "5"), ordinaryBeforeShield: "0" });
});
