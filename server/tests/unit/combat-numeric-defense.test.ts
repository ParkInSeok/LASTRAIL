import { expect, it } from "vitest";
import * as numeric from "../../apps/game-api/src/combat-numeric-defense";
import { resolveDirectDamageBudget } from "../../apps/game-api/src/combat-damage-budget";
const value = (numerator: string, denominator = "1") => ({ numerator, denominator });
const source = (sourceId: string, valueBps: number) => ({ sourceId, valueBps });
const input = () => ({ version: "combat.numeric-defense-budget.v1", base: value("10000"), effects: [],
  defense: value("500"), weakening: [source("weak", 2000)], penetration: [source("pierce", 2500)],
  reductions: { equipment: [], guardStance: [], independent: [{ groupId: "a", valueBps: 1000 }, { groupId: "b", valueBps: 2000 }] } });
it("matches the approved K1000 example without early rounding", () => {
  expect(numeric.resolveNumericDefenseBudget(input())).toMatchObject({ version: "combat.numeric-defense-budget.v1",
    effectiveDefense: value("300"), exact: value("72000", "13"), ordinaryBeforeShield: "5538" });
});
it.each([["0","10000"],["100","9090"],["250","8000"],["500","6666"],["1000","5000"],["2000","3333"],["4000","2000"]])("resolves DEF %s with no armor 60%% ceiling", (defense, damage) => {
  const c = input(); c.defense = value(defense); c.weakening = []; c.penetration = []; c.reductions.independent = [];
  expect(numeric.resolveNumericDefenseBudget(c).ordinaryBeforeShield).toBe(damage);
});
it("uses strongest weakening capped at 30% and sums penetration capped at 60%", () => {
  const c = input(); c.weakening = [source("w1", 4000),source("w2", 2000)]; c.penetration = [source("p1", 4000),source("p2", 4000)];
  expect(numeric.resolveNumericDefenseBudget(c).effectiveDefense).toEqual(value("140"));
});
it("deduplicates sources before summing penetration and equipment reductions", () => {
  const c = input(); c.weakening = []; c.penetration = [source("p", 2000),source("p", 2000)]; c.reductions.independent = [];
  const r = numeric.resolveNumericDefenseBudget({...c,reductions:{...c.reductions,equipment:[source("eq", 2000),source("eq", 2000)]}});
  expect(r.effectiveDefense).toEqual(value("400")); expect(r.ordinaryBeforeShield).toBe("5714");
});
it("caps EQ06 at 50%, selects strongest guard stance, then multiplies independent groups", () => {
  const c = input(); c.defense = value("0"); c.reductions.independent = [{groupId:"OC01",valueBps:1000}];
  expect(numeric.resolveNumericDefenseBudget({...c,reductions:{...c.reductions,equipment:[source("eq1",4000),source("eq2",4000)],guardStance:[source("g1",2500),source("g2",2000)]}}).ordinaryBeforeShield).toBe("3375");
});
it("retains offensive group caps and exact fractions until the final floor", () => {
  const c=input(); c.base=value("19","10"); c.defense=value("1000","3"); c.weakening=[]; c.penetration=[]; c.reductions.independent=[];
  expect(numeric.resolveNumericDefenseBudget(c)).toMatchObject({exact:value("57","40"),ordinaryBeforeShield:"1"});
  expect(numeric.resolveNumericDefenseBudget({...c,base:value("100"),defense:value("0"),effects:[{sourceId:"gear",group:"EQUIPMENT_DIRECT",valueBps:4000},{sourceId:"out",group:"NON_EQUIPMENT_OUTGOING",valueBps:5000},{sourceId:"mark",group:"MARK",valueBps:4000}]}).ordinaryBeforeShield).toBe("218");
});
it("does not promote zero or sub-unit damage to one", () => {
  const c=input(); c.base=value("1","10"); expect(numeric.resolveNumericDefenseBudget(c).ordinaryBeforeShield).toBe("0");
  c.base=value("0"); expect(numeric.resolveNumericDefenseBudget(c).exact).toEqual(value("0"));
});
it("preserves integers beyond Number safe range", () => {
  const c=input(); c.base=value("9007199254740993"); c.defense=value("0"); c.reductions.independent=[];
  expect(numeric.resolveNumericDefenseBudget(c).ordinaryBeforeShield).toBe("9007199254740993");
});
it("rejects conflicting identities and unresolved duplicate independent groups", () => {
  const c=input();
  expect(()=>numeric.resolveNumericDefenseBudget({...c,penetration:[source("p",1000),source("p",2000)]})).toThrow("combat_effect_source_conflict");
  expect(()=>numeric.resolveNumericDefenseBudget({...c,penetration:[source("weak",2000)]})).toThrow("combat_effect_source_conflict");
  expect(()=>numeric.resolveNumericDefenseBudget({...c,reductions:{...c.reductions,independent:[{groupId:"a",valueBps:1000},{groupId:"a",valueBps:1000}]}})).toThrow();
});
it.each([{version:"combat.direct-damage-budget.v1"},{defense:value("-1")},{defense:value("1","0")},{defenseBps:1000},{penetration:[source("p",-1)]},{weakening:[source("w",0.5)]}])("rejects malformed values or mixing legacy policy %j", patch => {
  expect(()=>numeric.resolveNumericDefenseBudget({...input(),...patch})).toThrow();
});
it("rejects independent EQ06 and guard_stance bypasses and reduction over 100%", () => {
  for(const independent of [[{groupId:"EQ06",valueBps:1000}],[{groupId:"guard_stance",valueBps:1000}],[{groupId:"x",valueBps:10001}]]) {
    const c=input(); expect(()=>numeric.resolveNumericDefenseBudget({...c,reductions:{...c.reductions,independent}})).toThrow();
  }
});
it("keeps historical v1 results and requires an explicit numeric policy version", () => {
  expect(resolveDirectDamageBudget({base:value("19","10"),effects:[],defenseBps:1000,incomingReductionBps:0})).toEqual({version:"combat.direct-damage-budget.v1",exact:value("171","100"),ordinaryBeforeShield:"1"});
  expect(()=>numeric.resolveNumericDefenseBudget({...input(),version:undefined})).toThrow();
});
