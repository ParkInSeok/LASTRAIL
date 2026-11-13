import { describe, expect, it } from "vitest";
import snapshot from "../../packages/balance/src/launch-economy-v1.json";
import { parseLaunchEconomy, trainingCoreExpCost, ordinaryVictoryExp } from "../../packages/balance/src/launch-economy";

describe("versioned launch economy baseline", () => {
  it("preserves Plan's full snapshot and exact five-slot EXP investment", () => {
    const config = parseLaunchEconomy(snapshot, snapshot.version);
    expect(config).toEqual(snapshot);
    expect(config.offline.hourly).toEqual([24, 2, 1, 20]);
    expect(trainingCoreExpCost(config, 1, 50)).toBe(14000);
    expect(trainingCoreExpCost(config, 1, 50) * config.growth.operatorSlots).toBe(70000);
    expect(trainingCoreExpCost(config, 1, 10)).toBe(1000);
    expect(trainingCoreExpCost(config, 10, 20)).toBe(2000);
    expect(trainingCoreExpCost(config, 20, 30)).toBe(3000);
    expect(trainingCoreExpCost(config, 30, 40)).toBe(3500);
    expect(trainingCoreExpCost(config, 40, 50)).toBe(4500);
  });
  it("uses only risk for ordinary victory EXP", () => {
    const config = parseLaunchEconomy(snapshot, snapshot.version);
    expect([1, 2, 3, 4, 5].map(risk => ordinaryVictoryExp(config, risk))).toEqual([700, 900, 1100, 1300, 1500]);
    for (const risk of [0, 6, 1.5, NaN]) expect(() => ordinaryVictoryExp(config, risk)).toThrow();
  });
  it("rejects stale versions and unknown schema fields at each boundary", () => {
    expect(() => parseLaunchEconomy(snapshot, "old")).toThrow("economy_version_mismatch");
    for (const section of [null, "offline", "ordinaryRun", "storyRaid", "activities", "growth", "worldBoss", "shop"] as const) {
      const invalid = structuredClone(snapshot);
      Object.assign(section === null ? invalid : invalid[section], { unexpected: true });
      expect(() => parseLaunchEconomy(invalid, snapshot.version)).toThrow();
    }
  });
  it.each([-1, 1.5, NaN, Infinity, Number.MAX_SAFE_INTEGER + 1])("rejects invalid resource quantity %s", value => {
    const invalid = structuredClone(snapshot);
    invalid.offline.hourly[0] = value;
    expect(() => parseLaunchEconomy(invalid, snapshot.version)).toThrow();
  });
  it("rejects malformed tables and forbidden policy switches", () => {
    const mutations = [
      (c: typeof snapshot) => { c.growth.operatorExpSteps1to10.pop(); },
      (c: typeof snapshot) => { c.ordinaryRun.baseMaterials[0]!.push(4); },
      (c: typeof snapshot) => { c.growth.researchEnabledWithoutApprovedNodeCatalog = true; },
      (c: typeof snapshot) => { c.ordinaryRun.expUsesOperationMultiplier = true; },
      (c: typeof snapshot) => { c.growth.refundActualInvestedExpBps = 9000; },
      (c: typeof snapshot) => { c.offline.capHours.reverse(); },
    ];
    for (const mutate of mutations) {
      const invalid = structuredClone(snapshot);
      mutate(invalid);
      expect(() => parseLaunchEconomy(invalid, snapshot.version)).toThrow();
    }
  });
  it("rejects invalid level ranges without calculating a charge", () => {
    const config = parseLaunchEconomy(snapshot, snapshot.version);
    for (const [from, to] of [[0, 2], [1, 51], [2, 1], [1.5, 2], [NaN, 2]]) {
      expect(() => trainingCoreExpCost(config, from!, to!)).toThrow();
    }
    expect(trainingCoreExpCost(config, 20, 20)).toBe(0);
    expect(config.growth.unknownTargetPolicy).toBe("REJECT_WITHOUT_SPENDING");
    expect(config.growth.researchEnabledWithoutApprovedNodeCatalog).toBe(false);
  });
});
