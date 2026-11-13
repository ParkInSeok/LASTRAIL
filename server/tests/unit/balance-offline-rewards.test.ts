import { describe, expect, it } from "vitest";
import {
  calculateOfflineRewardPreview,
  launchOfflineRewardsConfig,
  parseOfflineRewardsConfig,
} from "../../packages/balance/src/index";

describe("offline rewards balance contract", () => {
  it("scales by story progress and caps claimable seconds without using run risk", () => {
    const none = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: "none",
      actualOfflineSeconds: 24 * 60 * 60,
    });
    const raid = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: "raid_raider_king_engine",
      actualOfflineSeconds: 24 * 60 * 60,
    });

    expect(none.calculation.storyProgressMultiplierBps).toBe(10000);
    expect(none.calculation.offlineStorageCapHours).toBe(4);
    expect(none.calculation.claimableSeconds).toBe(4 * 60 * 60);
    expect(raid.calculation.storyProgressMultiplierBps).toBe(30000);
    expect(raid.calculation.offlineStorageCapHours).toBe(12);
    expect(raid.calculation.claimableSeconds).toBe(12 * 60 * 60);
    expect(totalQuantity(raid.rewardLines)).toBeGreaterThan(totalQuantity(none.rewardLines));
    expect(JSON.stringify(raid)).not.toContain("risk");
  });

  it("returns no reward lines below the minimum offline duration", () => {
    const preview = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: "opn_03_water_stop",
      actualOfflineSeconds: 9 * 60 + 59,
    });

    expect(preview.rewardLines).toEqual([]);
    expect(preview.calculation.claimableSeconds).toBe(0);
    expect(preview.calculation.minimumOfflineMinutes).toBe(10);
  });

  it("excludes special core, command ticket, and command ticket fragment from launch defaults", () => {
    const preview = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: "opn_06_broken_line",
      actualOfflineSeconds: 10 * 60 * 60,
    });
    const ids = preview.rewardLines.map((line) => line.id);

    expect(ids).toContain("cur_credits");
    expect(ids).toContain("cur_machine_parts");
    expect(ids).toContain("cur_tactical_data");
    expect(ids).toContain("cur_operator_exp");
    expect(ids).not.toContain("cur_special_core");
    expect(ids).not.toContain("cur_command_ticket");
    expect(ids).not.toContain("cur_command_ticket_fragment");
  });

  it("parses immutable offline reward config and rejects risk multipliers or blocked default rewards", () => {
    const parsed = parseOfflineRewardsConfig(JSON.parse(JSON.stringify(launchOfflineRewardsConfig)));

    expect(parsed.schemaVersion).toBe(1);
    expect(parsed.storyProgressMultipliersBps.opn_01_last_departure).toBe(11500);
    expect(() =>
      parseOfflineRewardsConfig({
        ...launchOfflineRewardsConfig,
        riskMultipliersBps: { I: 8000, V: 17500 },
      }),
    ).toThrow("offline_reward_policy_invalid");
    expect(() =>
      parseOfflineRewardsConfig({
        ...launchOfflineRewardsConfig,
        baseHourlyRewards: [
          ...launchOfflineRewardsConfig.baseHourlyRewards,
          { kind: "CURRENCY", id: "cur_command_ticket", quantity: 1 },
        ],
      }),
    ).toThrow("offline_reward_policy_invalid");
  });

  it("rejects invalid story progress keys and unsafe durations at the contract boundary", () => {
    expect(() =>
      calculateOfflineRewardPreview({
        config: launchOfflineRewardsConfig,
        storyProgressKey: "missing_story_key",
        actualOfflineSeconds: 3600,
      }),
    ).toThrow("offline_story_progress_missing");
    expect(() =>
      calculateOfflineRewardPreview({
        config: launchOfflineRewardsConfig,
        storyProgressKey: "none",
        actualOfflineSeconds: -1,
      }),
    ).toThrow("contract_violation");
  });
});

function totalQuantity(lines: Array<{ quantity: number }>): number {
  return lines.reduce((total, line) => total + line.quantity, 0);
}
