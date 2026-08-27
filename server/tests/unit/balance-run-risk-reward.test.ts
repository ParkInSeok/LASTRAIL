import { describe, expect, it } from "vitest";
import {
  calculateRunRewardPreview,
  launchRunRewardsConfig,
} from "../../packages/balance/src/index";

describe("run risk reward balance contract", () => {
  it("scales repeat rewards by risk, operation progression, outcome, and waves", () => {
    const riskI = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_01_last_departure",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "I",
      outcome: "VICTORY",
      clearedWaves: 15,
      firstClearEligible: false,
      integrityDisposition: "ALLOW",
    });
    const riskV = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_01_last_departure",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "V",
      outcome: "VICTORY",
      clearedWaves: 15,
      firstClearEligible: false,
      integrityDisposition: "ALLOW",
    });
    const op6 = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_06_broken_line",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "I",
      outcome: "VICTORY",
      clearedWaves: 15,
      firstClearEligible: false,
      integrityDisposition: "ALLOW",
    });
    const partialFailure = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_01_last_departure",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "I",
      outcome: "FAILURE",
      clearedWaves: 6,
      firstClearEligible: false,
      integrityDisposition: "ALLOW",
    });
    const abandoned = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_01_last_departure",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "I",
      outcome: "ABANDONED",
      clearedWaves: 6,
      firstClearEligible: false,
      integrityDisposition: "ALLOW",
    });

    expect(quantity(riskV.rewardLines, "cur_credits")).toBeGreaterThan(
      quantity(riskI.rewardLines, "cur_credits"),
    );
    expect(quantity(op6.rewardLines, "cur_credits")).toBeGreaterThan(
      quantity(riskI.rewardLines, "cur_credits"),
    );
    expect(quantity(partialFailure.rewardLines, "cur_credits")).toBeLessThan(
      quantity(riskI.rewardLines, "cur_credits"),
    );
    expect(abandoned.rewardLines).toEqual([]);
  });

  it("keeps first clear rewards as separate reward lines", () => {
    const preview = calculateRunRewardPreview({
      config: launchRunRewardsConfig,
      operationId: "opn_03_water_stop",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      risk: "III",
      outcome: "VICTORY",
      clearedWaves: 15,
      firstClearEligible: true,
      integrityDisposition: "ALLOW",
    });

    expect(preview.rewardLines).toContainEqual({
      kind: "CURRENCY",
      id: "cur_machine_parts",
      quantity: 160,
      source: "FIRST_CLEAR",
    });
    expect(preview.calculation.firstClearApplied).toBe(true);
    expect(preview.calculation.riskMultiplierBps).toBe(12000);
    expect(preview.calculation.operationProgressionMultiplierBps).toBe(12000);
  });

  it("rejects missing risk reward policy instead of guessing multipliers", () => {
    expect(() =>
      calculateRunRewardPreview({
        config: {
          ...launchRunRewardsConfig,
          riskMultipliersBps: {
            I: 8000,
            II: 10000,
            III: 12000,
            IV: 14500,
          },
        },
        operationId: "opn_01_last_departure",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        risk: "V",
        outcome: "VICTORY",
        clearedWaves: 15,
        firstClearEligible: false,
        integrityDisposition: "ALLOW",
      }),
    ).toThrow(/reward_multiplier_missing/u);
  });
});

function quantity(lines: Array<{ id: string; quantity: number }>, id: string): number {
  return lines.find((line) => line.id === id)?.quantity ?? 0;
}
