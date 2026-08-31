import { describe, expect, it } from "vitest";
import {
  offlineRewardDetailPlaceholders,
  offlineRewardFilterStatus,
  offlineRewardListColumns,
  offlineRewardOperationsPolicyCards,
  offlineRewardOperationsSeverityCards,
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
    expect(offlineRewardDetailPlaceholders.every((item) => item.value === "계정 선택 대기")).toBe(true);
  });

  it("shows mutation flags as disabled support context", () => {
    expect(offlineRewardSupportFlags).toEqual([
      "supportContext.readOnly=true",
      "supportContext.claimMutationEnabled=false",
      "supportContext.mutationEndpointsEnabled=false"
    ]);
  });

  it("shows offline operations policy and severity mapping placeholders", () => {
    expect(offlineRewardOperationsPolicyCards.map((card) => card.code)).toEqual([
      "server_records_authoritative",
      "unity_client_state_not_source_of_truth",
      "risk_not_direct_multiplier",
      "long_absence_cap_overflow_not_claimable",
      "minimum_claim_time_not_claimable",
      "empty_claim_does_not_advance_baseline",
      "duplicate_replay_no_revision",
      "no_row_fallback_expected_before_first_claim"
    ]);
    expect(offlineRewardFilterStatus).toBe("필터 기준 준비");
    expect(offlineRewardOperationsPolicyCards.every((card) => card.value === "정책 기준")).toBe(true);
    expect(offlineRewardOperationsSeverityCards.every((card) => card.value === "운영 분류 기준")).toBe(true);
    expect(offlineRewardOperationsSeverityCards.map((card) => `${card.errorCode}:${card.severity}`)).toContain(
      "offline_reward_not_claimable:info"
    );
    expect(offlineRewardOperationsSeverityCards.map((card) => `${card.errorCode}:${card.severity}`)).toContain(
      "offline_reward_state_conflict:high_risk"
    );
  });
});
