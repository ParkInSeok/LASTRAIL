import { describe, expect, it } from "vitest";
import {
  launchEconomyBalanceDraft,
  launchOfflineRewardsConfig,
  launchRunRewardsConfig,
} from "../../packages/balance/src/index";

describe("launch economy balance draft contract", () => {
  it("keeps draft active run reward values separate from live run reward config", () => {
    expect(launchEconomyBalanceDraft.status).toBe("TESTING_TUNING_DRAFT");
    expect(launchEconomyBalanceDraft.seedPromotionApproved).toBe(false);
    expect(launchEconomyBalanceDraft.productionFinal).toBe(false);

    expect(
      launchEconomyBalanceDraft.activeRunBaseRepeatRewards.opn_01_last_departure,
    ).toEqual({
      cur_credits: 120,
      cur_machine_parts: 8,
      cur_tactical_data: 4,
      cur_operator_exp: 700,
    });
    expect(
      launchEconomyBalanceDraft.activeRunBaseRepeatRewards.raid_raider_king_engine,
    ).toEqual({
      cur_credits: 360,
      cur_machine_parts: 28,
      cur_tactical_data: 16,
      cur_operator_exp: 1600,
    });
    expect(
      launchRunRewardsConfig.operations.opn_01_last_departure.baseRewards,
    ).not.toContainEqual({ kind: "CURRENCY", id: "cur_credits", quantity: 120 });
  });

  it("keeps draft first-clear controlled rewards candidate-only", () => {
    expect(
      launchEconomyBalanceDraft.activeRunFirstClearRewards.opn_04_scrapstar_show
        .candidateRewards,
    ).toEqual([{ id: "cur_command_ticket_fragment", quantity: 1 }]);
    expect(
      launchEconomyBalanceDraft.activeRunFirstClearRewards.opn_06_broken_line
        .candidateRewards,
    ).toEqual([{ id: "cur_special_core", quantity: 1 }]);
    expect(
      launchEconomyBalanceDraft.activeRunFirstClearRewards.raid_raider_king_engine
        .candidateRewards,
    ).toEqual([{ id: "cur_special_core", quantity: 2 }]);
    expect(launchEconomyBalanceDraft.candidateOnlyRewardIds).toEqual([
      "cur_command_ticket_fragment",
      "cur_special_core",
    ]);
  });

  it("promotes launch gacha command ticket acquisition into server-owned first-clear reward config", () => {
    expect(launchEconomyBalanceDraft.gachaTicketAcquisition.status).toBe("LAUNCH_CONFIG");
    expect(launchEconomyBalanceDraft.gachaTicketAcquisition.totalNonRepeatTickets).toBe(12);
    expect(launchEconomyBalanceDraft.gachaTicketAcquisition.sources).toEqual([
      {
        sourceType: "LOGIN_BONUS",
        sourceId: "launch_login_bonus",
        itemId: "cur_command_ticket",
        quantity: 5,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_03_water_stop",
        itemId: "cur_command_ticket",
        quantity: 1,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_05_night_boarders",
        itemId: "cur_command_ticket",
        quantity: 1,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_06_broken_line",
        itemId: "cur_command_ticket",
        quantity: 2,
        repeatable: false,
      },
      {
        sourceType: "STORY_RAID_FIRST_CLEAR",
        sourceId: "raid_raider_king_engine",
        itemId: "cur_command_ticket",
        quantity: 3,
        repeatable: false,
      },
    ]);
    expect(
      launchEconomyBalanceDraft.gachaTicketAcquisition.sources.reduce(
        (sum, source) => sum + source.quantity,
        0,
      ),
    ).toBe(12);
    expect(launchEconomyBalanceDraft.gachaTicketAcquisition.sources.every((source) => !source.repeatable)).toBe(true);
    expect(launchRunRewardsConfig.operations.opn_03_water_stop.firstClearRewards).toContainEqual({
      kind: "ITEM",
      id: "cur_command_ticket",
      quantity: 1,
    });
    expect(launchRunRewardsConfig.operations.opn_05_night_boarders.firstClearRewards).toContainEqual({
      kind: "ITEM",
      id: "cur_command_ticket",
      quantity: 1,
    });
    expect(launchRunRewardsConfig.operations.opn_06_broken_line.firstClearRewards).toContainEqual({
      kind: "ITEM",
      id: "cur_command_ticket",
      quantity: 2,
    });
    expect(launchRunRewardsConfig.operations.raid_raider_king_engine.firstClearRewards).toContainEqual({
      kind: "ITEM",
      id: "cur_command_ticket",
      quantity: 3,
    });
    const ticketTotal = Object.values(launchRunRewardsConfig.operations).reduce((sum, operation) => (
      sum + operation.firstClearRewards
        .filter((line) => line.kind === "ITEM" && line.id === "cur_command_ticket")
        .reduce((lineSum, line) => lineSum + line.quantity, 0)
    ), 0);
    expect(ticketTotal).toBe(7);
  });

  it("keeps draft offline hourly rewards separate from live offline reward config", () => {
    expect(launchEconomyBalanceDraft.offlineBaseHourlyRewards).toEqual({
      cur_credits: 24,
      cur_machine_parts: 2,
      cur_tactical_data: 1,
      cur_operator_exp: 80,
    });
    expect(launchEconomyBalanceDraft.offlineRepeatBlockedRewardIds).toEqual([
      "cur_special_core",
      "cur_command_ticket",
      "cur_command_ticket_fragment",
    ]);
    expect(launchOfflineRewardsConfig.baseHourlyRewards).not.toContainEqual({
      kind: "CURRENCY",
      id: "cur_credits",
      quantity: 24,
    });
  });
});
