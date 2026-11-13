import { describe, expect, it } from "vitest";
import {
  mapGachaBannerListResponse,
  mapGachaPityResponse,
  mapGachaPullBatchDetailResponse,
  mapGachaPullBatchListResponse
} from "./gachaContract";

describe("gacha admin read-only contract", () => {
  it("maps banner summaries without raw probability or weight fields", () => {
    const result = mapGachaBannerListResponse({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      activeBannerId: "banner-1",
      items: [
        {
          bannerId: "banner-1",
          bannerType: "PICKUP",
          status: "ACTIVE",
          pickupOperatorId: "op_vex",
          balanceVersion: "7",
          pullCostItemId: "cur_command_ticket",
          pullCostQuantity: 1,
          pullCountOptions: [1, 10],
          pityThresholds: { rareOrHigher: 10, elite: 30, pickupElite: 60 },
          guaranteeTypes: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"],
          startsAt: "2026-08-25T11:00:00.000Z",
          endsAt: null,
          updatedAt: "2026-08-25T11:00:00.000Z",
          detailAvailable: true,
          weight: 999,
          probability: 0.5
        }
      ],
      nextCursor: null
    });

    expect(result.activeBannerId).toBe("banner-1");
    expect(result.items[0]).toMatchObject({
      bannerId: "banner-1",
      activeBannerId: "banner-1",
      pickupOperatorId: "op_vex",
      pullCountOptions: ["1", "10"],
      pityThresholds: { rareOrHigher: "10", elite: "30", pickupElite: "60" }
    });
    expect(JSON.stringify(result)).not.toContain("weight");
    expect(JSON.stringify(result)).not.toContain("probability");
  });

  it("maps account pity no-row defaults as data 없음 semantics", () => {
    const result = mapGachaPityResponse({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      accountId: "account-1",
      bannerId: "banner-1",
      pityState: {
        totalPullCount: 0,
        elitePityCount: 0,
        pickupElitePityCount: 0,
        rareOrHigherPityCount: 0,
        revision: 0,
        lastBatchId: null,
        updatedAt: null
      },
      detailAvailable: false
    });

    expect(result.detailAvailable).toBe(false);
    expect(result.pityState.totalPullCount).toBe("0");
    expect(result.pityState.lastBatchId).toBe("없음");
    expect(result.pityState.updatedAt).toBe("없음");
  });

  it("maps pull batch list and detail with safe refs only", () => {
    const list = mapGachaPullBatchListResponse({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          batchId: "batch-1",
          accountId: "account-1",
          bannerId: "banner-1",
          resultStatus: "ACCEPTED",
          idempotencyReplayKnown: true,
          pullCount: 10,
          cost: { itemId: "cur_command_ticket", quantity: 10 },
          prePityState: { totalPullCount: 0 },
          postPityState: { totalPullCount: 10 },
          guaranteeTypes: ["PICKUP_ELITE_60"],
          createdAt: "2026-08-25T11:43:20.000Z"
        }
      ],
      nextCursor: null
    });
    const detail = mapGachaPullBatchDetailResponse({
      item: {
        ...list.items[0],
        itemLedgerRefs: ["ledger-cost-1", "ledger-token-1"],
        operatorLedgerRefs: [],
        results: [
          {
            resultId: "result-1",
            pullSequence: 1,
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR_SHARD",
            reward: { id: "op_vex" },
            isPickup: true,
            wasDuplicate: true,
            guaranteeType: "ELITE_30",
            duplicateConversion: {
              displayItemId: "cur_operator_token",
              persistedTokenPattern: "cur_operator_token:op_vex",
              quantity: 30
            },
            itemLedgerRef: "ledger-token-1",
            rawRng: "must-not-copy"
          }
        ]
      },
      detailAvailable: true
    });

    expect(list.items[0].guaranteeTypes).toEqual(["60뽑 픽업 정예 확정 (PICKUP_ELITE_60)"]);
    expect(detail.item.results[0]).toMatchObject({
      resultId: "result-1",
      resultRarity: "ELITE",
      guaranteeType: "30뽑 정예 확정 (ELITE_30)",
      duplicateConversion: "cur_operator_token:op_vex x30"
    });
    expect(JSON.stringify(detail)).not.toContain("must-not-copy");
    expect(JSON.stringify(detail)).not.toContain("rawRng");
  });
});
