import { describe, expect, it } from "vitest";
import {
  actOneEliteOperatorDraftBoundary,
  actOneEliteOperatorIdsDraft,
  gachaDuplicateConversionSupportFields,
  gachaLaunchTicketAcquisitionPolicy,
  gachaLaunchResultFields,
  gachaLaunchSupportVisibilityCards,
  gachaReadOnlySections,
  gachaSupportPullCountVisibility
} from "./GachaPage";

describe("GachaPage configuration", () => {
  it("covers banner, pity, pull history, guarantee, errors, conversion, and ledger read-only needs", () => {
    const sectionTitles = gachaReadOnlySections.map((section) => section.title);

    expect(sectionTitles).toContain("배너 상태");
    expect(sectionTitles).toContain("계정별 천장 상태");
    expect(sectionTitles).toContain("뽑기 이력");
    expect(sectionTitles).toContain("보장 발동");
    expect(sectionTitles).toContain("거절/오류 코드");
    expect(sectionTitles).toContain("중복/조각 변환");
    expect(sectionTitles).toContain("아이템 원장 연결");
  });

  it("marks every section as pending server query API integration", () => {
    expect(gachaReadOnlySections.every((section) => section.status === "조회 API 대기")).toBe(true);
  });

  it("uses Korean labels while keeping stable backend codes visible", () => {
    const fields = gachaReadOnlySections.flatMap((section) => section.fields);

    expect(fields).toContain("60뽑 픽업 정예 확정 (PICKUP_ELITE_60)");
    expect(fields).toContain("이전 결과 재조회 (DUPLICATE_REPLAY)");
    expect(fields).toContain("지휘권 부족 (insufficient_command_ticket)");
  });

  it("prepares launch gacha support visibility without live economy truth", () => {
    expect(gachaLaunchSupportVisibilityCards.map((card) => card.code)).toEqual([
      "activeBannerId",
      "bannerType",
      "pickupOperatorId",
      "pullCountVisibility",
      "pityThresholds",
      "guaranteeType",
      "duplicateConversion",
      "resultFields",
      "ticketAcquisition",
      "actOneEliteVisualScope"
    ]);
    expect(gachaSupportPullCountVisibility).toEqual(["1-pull", "10-pull"]);
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "pityThresholds")?.note).toContain(
      "rare 10 / elite 30 / pickup 60"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "pityThresholds")?.note).toContain(
      "draft/contract candidate"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "actOneEliteVisualScope")?.value).toBe(
      "TESTING_TUNING_DRAFT"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "productionFinal=false"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "seedPromotionApproved=false"
    );
  });

  it("shows launch non-repeat command ticket sources without putting tickets into offline repeat rewards", () => {
    expect(gachaLaunchTicketAcquisitionPolicy).toEqual({
      totalNonRepeatTickets: 12,
      costItemId: "cur_command_ticket",
      offlineRepeatIncludesCommandTicket: false,
      excludedOfflineRepeatRewards: ["cur_command_ticket", "cur_command_ticket_fragment", "cur_special_core"],
      boundary: "launch candidate/draft, not production seed/config promotion",
      sources: [
        { code: "launch_login_bonus", label: "login bonus", quantity: 5, repeatable: false },
        {
          code: "opn_03_water_stop_first_clear",
          label: "opn_03_water_stop first clear",
          quantity: 1,
          repeatable: false,
          state: "promoted_local_launch_config"
        },
        {
          code: "opn_05_night_boarders_first_clear",
          label: "opn_05_night_boarders first clear",
          quantity: 1,
          repeatable: false,
          state: "promoted_local_launch_config"
        },
        {
          code: "opn_06_broken_line_first_clear",
          label: "opn_06_broken_line first clear",
          quantity: 2,
          repeatable: false,
          state: "promoted_local_launch_config"
        },
        {
          code: "raid_raider_king_engine_story_raid_first_clear",
          label: "raid_raider_king_engine story raid first clear",
          quantity: 3,
          repeatable: false,
          state: "promoted_local_launch_config"
        }
      ],
      firstClearTickets: { quantity: 7, state: "promoted_local_launch_config" },
      loginBonusTickets: { quantity: 5, state: "pending_endpoint_state_table" }
    });
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "launch non-repeat total 12"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "offline repeat reward excludes cur_command_ticket / cur_command_ticket_fragment / cur_special_core"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "opn_03_water_stop first clear 1 repeatable=false"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "raid_raider_king_engine story raid first clear 3 repeatable=false"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "launch candidate/draft, not production seed/config promotion"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "first-clear 7 promoted_local_launch_config"
    );
    expect(gachaLaunchSupportVisibilityCards.find((card) => card.code === "ticketAcquisition")?.note).toContain(
      "login bonus 5 pending_endpoint_state_table"
    );
  });

  it("lists Act 1 elite operator ids from the server launch gacha draft only", () => {
    expect(actOneEliteOperatorIdsDraft).toEqual([
      "op_vex",
      "op_nyx",
      "op_sable",
      "op_marrow",
      "op_iris",
      "op_rail",
      "op_anchor",
      "op_lumen",
      "op_cinder",
      "op_warden"
    ]);
    expect(actOneEliteOperatorDraftBoundary).toEqual({
      scopeStatus: "TESTING_TUNING_DRAFT",
      productionFinal: false,
      seedPromotionApproved: false
    });
    for (const initialOperatorId of ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "op_rin", "op_cipher"]) {
      expect(actOneEliteOperatorIdsDraft).not.toContain(initialOperatorId);
    }
  });

  it("prepares duplicate conversion and result fields without sensitive gacha internals", () => {
    expect(gachaDuplicateConversionSupportFields).toEqual([
      "displayItemId",
      "persistedTokenPattern",
      "rareConversionQuantityCandidate",
      "eliteConversionQuantityCandidate"
    ]);
    expect(gachaLaunchResultFields).toEqual([
      "pullSequence",
      "resultRarity",
      "resultType",
      "resultId",
      "reward",
      "isPickup",
      "wasDuplicate",
      "duplicateConversion"
    ]);

    const serialized = JSON.stringify({
      gachaReadOnlySections,
      gachaLaunchSupportVisibilityCards,
      gachaDuplicateConversionSupportFields,
      gachaLaunchResultFields,
      actOneEliteOperatorIdsDraft
    });
    expect(serialized).not.toContain("raw RNG");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("full weights");
    expect(serialized).not.toContain("seed preimage");
    expect(serialized).not.toContain("token secret");
    expect(serialized).not.toContain("manual grant");
    expect(serialized).not.toContain("reroll");
  });
});
