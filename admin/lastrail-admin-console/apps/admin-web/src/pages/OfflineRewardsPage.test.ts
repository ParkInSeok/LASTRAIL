import { describe, expect, it } from "vitest";
import {
  offlineRewardDetailPlaceholders,
  offlineRewardListColumns,
  offlineRewardReadOnlyFilters,
  offlineRewardSupportFlags
} from "./OfflineRewardsPage";

describe("OfflineRewardsPage configuration", () => {
  it("prepares read-only filters and list columns for the Admin API shape", () => {
    expect(offlineRewardReadOnlyFilters).toEqual([
      "accountId",
      "storyProgressId",
      "narrativeVersion",
      "source",
      "limit",
      "cursor"
    ]);
    expect(offlineRewardListColumns).toEqual([
      "계정",
      "상태",
      "스토리 진행도",
      "state source",
      "마지막 수령 기준",
      "마지막 claim",
      "revision",
      "read-only"
    ]);
  });

  it("keeps offline reward detail placeholders read-only", () => {
    expect(offlineRewardDetailPlaceholders.map((item) => item.field)).toEqual([
      "offlineRewardState.source",
      "offlineRewardState.lastOfflineRewardClaimAt",
      "offlineRewardState.lastStoryProgressId",
      "offlineRewardState.lastBalanceConfigVersion",
      "offlineRewardState.lastNarrativeVersion",
      "offlineRewardState.lastPreviewedAt",
      "offlineRewardState.revision",
      "offlineRewardState.updatedAt",
      "lastClaim.offlineRewardClaimId",
      "lastClaim.claimedAt"
    ]);
    expect(offlineRewardDetailPlaceholders.every((item) => item.value === "연동 대기")).toBe(true);
  });

  it("shows mutation flags as disabled support context", () => {
    expect(offlineRewardSupportFlags).toEqual([
      "supportContext.readOnly=true",
      "supportContext.claimMutationEnabled=false",
      "supportContext.mutationEndpointsEnabled=false"
    ]);
  });
});
