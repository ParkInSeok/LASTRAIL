import { describe, expect, it } from "vitest";
import {
  actOneEliteOperatorIdsDraft,
  gachaDuplicateConversionSupportFields,
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
  });

  it("lists Act 1 elite operator ids as a testing tuning draft", () => {
    expect(actOneEliteOperatorIdsDraft).toEqual([
      "op_kyle",
      "op_reina",
      "op_eve",
      "op_sera",
      "op_mira",
      "op_vex",
      "op_rin",
      "op_nyx",
      "op_cipher",
      "op_iris"
    ]);
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
