import { describe, expect, it } from "vitest";
import { launchGachaDataDraft } from "../../packages/balance/src/index";

function collectKeys(value: unknown): string[] {
  if (Array.isArray(value)) return value.flatMap(collectKeys);
  if (typeof value !== "object" || value === null) return [];
  return Object.entries(value).flatMap(([key, child]) => [key, ...collectKeys(child)]);
}

describe("launch gacha data draft contract", () => {
  it("keeps Act 1 elite visual scope and launch pool ids aligned without promoting production seed data", () => {
    expect(launchGachaDataDraft.status).toBe("TESTING_TUNING_DRAFT");
    expect(launchGachaDataDraft.productionFinal).toBe(false);
    expect(launchGachaDataDraft.seedPromotionApproved).toBe(false);

    expect(launchGachaDataDraft.actOneEliteOperatorIds).toEqual([
      "op_vex",
      "op_nyx",
      "op_sable",
      "op_marrow",
      "op_iris",
      "op_rail",
      "op_anchor",
      "op_lumen",
      "op_cinder",
      "op_warden",
    ]);
    expect(new Set(launchGachaDataDraft.actOneEliteOperatorIds).size).toBe(10);

    const launchBanner = launchGachaDataDraft.banners[0];
    expect(launchBanner).toMatchObject({
      bannerId: "banner_act1_pickup_vex_testing",
      bannerType: "PICKUP",
      pickupOperatorId: "op_vex",
      pityConfig: { rare: 10, elite: 30, pickup: 60 },
      cost: { itemId: "cur_command_ticket", quantityPerPull: 1 },
      allowedPullCounts: [1, 10],
    });

    const elitePoolIds = launchBanner.pool.results
      .filter((result) => result.resultRarity === "ELITE" && result.resultType === "ELITE_OPERATOR")
      .map((result) => result.resultId);
    expect(elitePoolIds).toEqual(launchGachaDataDraft.actOneEliteOperatorIds);
  });

  it("documents launch response stability without exposing RNG or weight internals to Unity", () => {
    const forbiddenKeys = new Set([
      "accountId",
      "accessToken",
      "refreshToken",
      "secret",
      "rngSeed",
      "rawRng",
      "rawRngMaterial",
      "seedPreimage",
      "probability",
      "probabilities",
      "weighted",
      "fullWeights",
      "weightTable",
    ]);

    expect(launchGachaDataDraft.clientVisibleResultFields).toEqual([
      "pullSequence",
      "resultRarity",
      "resultType",
      "resultId",
      "reward",
      "isPickup",
      "guaranteeType",
      "wasDuplicate",
      "duplicateConversion",
    ]);
    expect(launchGachaDataDraft.supportedGuaranteeTypes).toEqual([
      "NONE",
      "RARE_OR_HIGHER_10",
      "ELITE_30",
      "PICKUP_ELITE_60",
    ]);
    expect(launchGachaDataDraft.duplicateConversion).toEqual({
      eliteOperatorTokenQuantity: 50,
      rareOperatorTokenQuantity: 30,
      displayItemId: "cur_operator_token",
      persistedItemIdPattern: "cur_operator_token:<operatorId>",
    });
    expect(collectKeys(launchGachaDataDraft).some((key) => forbiddenKeys.has(key))).toBe(false);
  });
});
