import { describe, expect, it } from "vitest";
import {
  actOneEliteOperatorDraftBoundary,
  actOneEliteOperatorIdsDraft,
  gachaDuplicateConversionSupportFields,
  gachaLaunchTicketAcquisitionPolicy,
  gachaLaunchSupportCardsForReadState,
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

  it("marks gacha sections by current read-only availability", () => {
    expect(gachaReadOnlySections.map((section) => [section.title, section.status])).toEqual([
      ["배너 상태", "조회 가능"],
      ["계정별 천장 상태", "계정 ID 필요"],
      ["뽑기 이력", "조회 가능"],
      ["보장 발동", "조회 가능"],
      ["거절/오류 코드", "조회 가능"],
      ["중복/조각 변환", "조회 가능"],
      ["아이템 원장 연결", "조회 가능"]
    ]);
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
      "accountPityState",
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

  it("maps gacha banner and batch API data into launch support cards", () => {
    const cards = gachaLaunchSupportCardsForReadState({
      banners: {
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        activeBannerId: "banner-1",
        nextCursor: null,
        items: [
          {
            bannerId: "banner-1",
            bannerType: "PICKUP",
            status: "ACTIVE",
            activeBannerId: "banner-1",
            pickupOperatorId: "op_vex",
            balanceVersion: "7",
            pullCostItemId: "cur_command_ticket",
            pullCostQuantity: "1",
            pullCountOptions: ["1", "10"],
            pityThresholds: { rareOrHigher: "10", elite: "30", pickupElite: "60" },
            guaranteeTypes: [
              "보장 없음 (NONE)",
              "10회 희귀 이상 보장 (RARE_OR_HIGHER_10)",
              "30뽑 정예 확정 (ELITE_30)",
              "60뽑 픽업 정예 확정 (PICKUP_ELITE_60)"
            ],
            startsAt: "2026-08-25T11:00:00.000Z",
            endsAt: "없음",
            updatedAt: "2026-08-25T11:00:00.000Z",
            detailAvailable: true
          }
        ]
      },
      batches: {
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        items: [
          {
            batchId: "batch-1",
            accountId: "account-1",
            bannerId: "banner-1",
            resultStatus: "ACCEPTED",
            idempotencyReplayKnown: "예",
            pullCount: "10",
            cost: { itemId: "cur_command_ticket", quantity: "10", itemLedgerRef: "없음" },
            prePityState: null,
            postPityState: null,
            guaranteeTypes: ["60뽑 픽업 정예 확정 (PICKUP_ELITE_60)"],
            createdAt: "2026-08-25T11:43:20.000Z"
          }
        ],
        nextCursor: null
      },
      detail: null,
      isLoading: false,
      error: null
    });

    expect(cards.find((card) => card.code === "activeBannerId")?.value).toBe("banner-1");
    expect(cards.find((card) => card.code === "pickupOperatorId")?.value).toBe("op_vex");
    expect(cards.find((card) => card.code === "pullCountVisibility")?.value).toBe("1 / 10");
    expect(cards.find((card) => card.code === "accountPityState")?.value).toBe("계정 ID 필요");
    expect(cards.find((card) => card.code === "resultFields")?.value).toBe("batch-1 / 10 pulls / ACCEPTED");
    expect(cards.find((card) => card.code === "pityThresholds")?.value).toContain("rare 10");
  });

  it("maps account pity state when account-scoped query data exists", () => {
    const cards = gachaLaunchSupportCardsForReadState({
      pity: {
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        accountId: "account-1",
        bannerId: "banner-1",
        pityState: {
          totalPullCount: "12",
          elitePityCount: "12",
          pickupElitePityCount: "12",
          rareOrHigherPityCount: "2",
          revision: "3",
          lastBatchId: "batch-1",
          updatedAt: "2026-08-31T06:00:00.000Z"
        },
        detailAvailable: true
      },
      isLoading: false,
      error: null
    });

    expect(cards.find((card) => card.code === "accountPityState")?.value).toBe(
      "account-1 / banner-1 / total=12 / elite=12 / pickup=12 / rare=2"
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
        {
          code: "launch_login_bonus",
          label: "login bonus",
          quantity: 5,
          repeatable: false,
          state: "support_summary_read_model_available"
        },
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
      loginBonusTickets: { quantity: 5, state: "support_summary_read_model_available" }
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
      "login bonus 5 support_summary_read_model_available"
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
