import { describe, expect, it } from "vitest";
import { buildGachaPool, validateGachaPool } from "../../packages/balance/src/gacha-policy";

const elites = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris", "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden"];
// Arbitrary test-only non-elite proportions. No launch rare/common decision
// is encoded by these tests; the generator must accept explicit caller data.
const nonElite = [
  { resultId: "test_rare", resultRarity: "RARE" as const, resultType: "TRAINING_MATERIAL" as const, weight: 3, quantity: 1 },
  { resultId: "test_common", resultRarity: "COMMON" as const, resultType: "TRAINING_MATERIAL" as const, weight: 7, quantity: 1 },
];
describe("approved elite policy generator", () => {
  it("keeps 15 elites at exactly four percent with equal normal chances", () => {
    const pool = buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: null, nonEliteResults: nonElite });
    const total = pool.results.reduce((sum, r) => sum + r.weight, 0);
    const elite = pool.results.filter((r) => r.resultRarity === "ELITE");
    expect(elite.map((r) => r.resultId)).toEqual(elites);
    expect(elite.reduce((sum, r) => sum + r.weight, 0) * 25).toBe(total);
    expect(new Set(elite.map((r) => r.weight)).size).toBe(1);
    expect(validateGachaPool(pool, null)).toBe(true);
  });
  it("keeps the same full pool and non-elite probabilities while making pickup twice each other elite", () => {
    const normal = buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: null, nonEliteResults: nonElite });
    const pickup = buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: "op_vex", nonEliteResults: nonElite });
    const total = pickup.results.reduce((sum, r) => sum + r.weight, 0);
    const normalTotal = normal.results.reduce((sum, r) => sum + r.weight, 0);
    expect(pickup.results.map((r) => r.resultId)).toEqual(normal.results.map((r) => r.resultId));
    expect(pickup.results.find((r) => r.resultId === "op_vex")!.weight * 200).toBe(total);
    for (const result of pickup.results) {
      const original = normal.results.find((r) => r.resultId === result.resultId)!;
      expect(Number.isSafeInteger(result.weight) && result.weight > 0).toBe(true);
      if (result.resultRarity !== "ELITE") expect(result.weight * normalTotal).toBe(original.weight * total);
      else if (result.resultId !== "op_vex") expect(result.weight * 400).toBe(total);
    }
    expect(validateGachaPool(pickup, "op_vex")).toBe(true);
  });
  it("handles the approved single-elite boundary without creating a new category endpoint", () => {
    const pool = buildGachaPool({ eliteOperatorIds: ["op_vex"], pickupOperatorId: "op_vex", nonEliteResults: nonElite });
    expect(pool.results[0]!.weight * 25).toBe(pool.results.reduce((sum, r) => sum + r.weight, 0));
  });
  it("rejects empty/duplicate elite pools, outside pickup, duplicate candidates and invalid supplied mass", () => {
    for (const ids of [[], ["op_vex", "op_vex"], [""]]) {
      expect(() => buildGachaPool({ eliteOperatorIds: ids, pickupOperatorId: null, nonEliteResults: nonElite })).toThrow();
    }
    expect(() => buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: "missing", nonEliteResults: nonElite })).toThrow();
    for (const results of [[], [nonElite[0]!, nonElite[0]!], [{ ...nonElite[0]!, weight: 0 }], [{ ...nonElite[0]!, weight: 0.5 }]]) {
      expect(() => buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: "op_vex", nonEliteResults: results })).toThrow();
    }
  });
  it("rejects tampering with fixed elite mass instead of normalizing away the error", () => {
    const pool = buildGachaPool({ eliteOperatorIds: elites, pickupOperatorId: "op_vex", nonEliteResults: nonElite });
    pool.results[0]!.weight += 1;
    expect(validateGachaPool(pool, "op_vex")).toBe(false);
  });
});
