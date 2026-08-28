import { describe, expect, it } from "vitest";
import {
  offlineRewardPreviewContractFieldLabel,
  offlineRewardPreviewContractFields,
  offlineRewardPreviewContractPreview,
  offlineRewardClaimContractFieldLabel,
  offlineRewardClaimContractFields,
  offlineRewardClaimContractPreview,
  offlineRewardOperationsPolicy,
  offlineRewardOperationsSeverityMappings,
  mapOfflineRewardDetailResponse,
  mapOfflineRewardListResponse,
  offlineRewardStateContractFieldLabel,
  offlineRewardStateContractFields,
  offlineRewardStateContractPreview
} from "./offlineRewardContract";

describe("offlineRewardContract", () => {
  it("prepares the Game API preview response shape without economy amounts", () => {
    expect(Object.keys(offlineRewardPreviewContractPreview)).toEqual([
      "previewStatus",
      "storyProgressId",
      "claimableSeconds",
      "claimableHours",
      "offlineStorageCapHours",
      "minimumClaimSeconds",
      "maxClaimableHours",
      "rewardLines",
      "calculation",
      "narrativeVersion",
      "serverCalculatedAt",
      "errorCode",
      "message"
    ]);
    expect(offlineRewardPreviewContractPreview.rewardLines[0]).toEqual({
      source: "OFFLINE_REWARD",
      rewardId: "연동 대기",
      quantity: "연동 대기"
    });
    expect(offlineRewardPreviewContractPreview.calculation).toEqual({
      storyProgressId: "none",
      storyProgressMultiplierLabel: "1.00x",
      cappedOfflineSeconds: "연동 대기",
      capSource: "연동 대기"
    });
  });

  it("keeps preview fields aligned with dashboard mapping candidates", () => {
    expect(offlineRewardPreviewContractFields).toEqual([
      "previewStatus",
      "storyProgressId",
      "claimableSeconds",
      "claimableHours",
      "offlineStorageCapHours",
      "minimumClaimSeconds",
      "maxClaimableHours",
      "rewardLines.source",
      "calculation.storyProgressId",
      "calculation.storyProgressMultiplierLabel",
      "calculation.cappedOfflineSeconds",
      "calculation.capSource",
      "narrativeVersion",
      "serverCalculatedAt",
      "errorCode",
      "message"
    ]);
  });

  it("labels known fields and preserves unknown fallback", () => {
    expect(offlineRewardPreviewContractFieldLabel("previewStatus")).toBe("미리보기 상태");
    expect(offlineRewardPreviewContractFieldLabel("claimableHours")).toBe("수령 가능 시간");
    expect(offlineRewardPreviewContractFieldLabel("calculation.capSource")).toBe("한도 출처");
    expect(offlineRewardPreviewContractFieldLabel("futureField")).toBe("futureField");
  });

  it("does not include sensitive internals or raw bps examples", () => {
    const serialized = JSON.stringify(offlineRewardPreviewContractPreview);

    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("raw body");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("storyProgressMultiplierBps");
  });

  it("prepares offline reward state visibility fields from the server migration", () => {
    expect(offlineRewardStateContractFields).toEqual([
      "last_offline_reward_claim_at",
      "last_story_progress_id",
      "last_balance_config_version",
      "last_narrative_version",
      "last_previewed_at",
      "revision",
      "updated_at"
    ]);
    expect(offlineRewardStateContractPreview).toEqual({
      last_offline_reward_claim_at: "연동 대기",
      last_story_progress_id: "연동 대기",
      last_balance_config_version: "연동 대기",
      last_narrative_version: "연동 대기",
      last_previewed_at: "연동 대기",
      revision: "연동 대기",
      updated_at: "연동 대기"
    });
  });

  it("labels offline reward state fields while preserving unknown fallback", () => {
    expect(offlineRewardStateContractFieldLabel("last_offline_reward_claim_at")).toBe(
      "마지막 오프라인 보상 수령 시각"
    );
    expect(offlineRewardStateContractFieldLabel("last_story_progress_id")).toBe("마지막 스토리 진행도");
    expect(offlineRewardStateContractFieldLabel("last_balance_config_version")).toBe("마지막 밸런스 설정 버전");
    expect(offlineRewardStateContractFieldLabel("last_narrative_version")).toBe("마지막 내러티브 버전");
    expect(offlineRewardStateContractFieldLabel("last_previewed_at")).toBe("마지막 미리보기 시각");
    expect(offlineRewardStateContractFieldLabel("revision")).toBe("리비전");
    expect(offlineRewardStateContractFieldLabel("updated_at")).toBe("갱신 시각");
    expect(offlineRewardStateContractFieldLabel("future_state_field")).toBe("future_state_field");
  });

  it("keeps offline reward state preview read-only and free of sensitive examples", () => {
    const serialized = JSON.stringify(offlineRewardStateContractPreview);

    expect(serialized).not.toContain("claim repair");
    expect(serialized).not.toContain("reward repair");
    expect(serialized).not.toContain("economy correction");
    expect(serialized).not.toContain("state mutation");
    expect(serialized).not.toContain("manual reset");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("raw body");
    expect(serialized).not.toContain("rawRng");
  });

  it("prepares offline reward claim response fields without executing claim mutations", () => {
    expect(offlineRewardClaimContractFields).toEqual([
      "accepted",
      "claimStatus",
      "offlineRewardClaimId",
      "rewardTransactionId",
      "economyTransactionId",
      "accountMutationId",
      "storyProgressId",
      "claimableSeconds",
      "claimableHours",
      "offlineStorageCapHours",
      "minimumClaimSeconds",
      "maxClaimableHours",
      "rewardLines.source",
      "rewardLines.balanceAfter",
      "calculation.storyProgressId",
      "calculation.storyProgressMultiplierLabel",
      "calculation.cappedOfflineSeconds",
      "calculation.capSource",
      "serverClaimedAt",
      "narrativeVersion",
      "errorCode",
      "message"
    ]);
    expect(offlineRewardClaimContractPreview).toMatchObject({
      accepted: "연동 대기",
      claimStatus: "연동 대기",
      offlineRewardClaimId: "연동 대기",
      rewardTransactionId: "연동 대기",
      economyTransactionId: "연동 대기",
      accountMutationId: "연동 대기",
      storyProgressId: "연동 대기",
      serverClaimedAt: "연동 대기",
      narrativeVersion: "연동 대기",
      errorCode: null,
      message: "연동 대기"
    });
    expect(offlineRewardClaimContractPreview.rewardLines[0]).toEqual({
      source: "OFFLINE_REWARD",
      rewardId: "연동 대기",
      quantity: "연동 대기",
      balanceAfter: "연동 대기"
    });
  });

  it("labels offline reward claim fields while preserving unknown fallback", () => {
    expect(offlineRewardClaimContractFieldLabel("claimStatus")).toBe("수령 상태");
    expect(offlineRewardClaimContractFieldLabel("offlineRewardClaimId")).toBe("오프라인 보상 수령 ID");
    expect(offlineRewardClaimContractFieldLabel("rewardTransactionId")).toBe("보상 트랜잭션 ID");
    expect(offlineRewardClaimContractFieldLabel("economyTransactionId")).toBe("경제 트랜잭션 ID");
    expect(offlineRewardClaimContractFieldLabel("accountMutationId")).toBe("계정 변경 ID");
    expect(offlineRewardClaimContractFieldLabel("rewardLines.balanceAfter")).toBe("수령 후 잔액");
    expect(offlineRewardClaimContractFieldLabel("serverClaimedAt")).toBe("서버 수령 시각");
    expect(offlineRewardClaimContractFieldLabel("futureClaimField")).toBe("futureClaimField");
  });

  it("keeps offline reward claim preview read-only and free of sensitive examples", () => {
    const serialized = JSON.stringify(offlineRewardClaimContractPreview);

    expect(serialized).not.toContain("claim button");
    expect(serialized).not.toContain("manual reset");
    expect(serialized).not.toContain("reward repair");
    expect(serialized).not.toContain("economy correction");
    expect(serialized).not.toContain("state mutation");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("raw body");
    expect(serialized).not.toContain("rawRng");
  });

  it("maps Admin API offline reward list response into read-only display state", () => {
    const result = mapOfflineRewardListResponse({
      permission: "OFFLINE_REWARD_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          accountId: "account-1",
          displayName: "Kyle Account",
          accountStatus: "ACTIVE",
          storyProgressId: "opn_02_first_signal",
          narrativeVersion: "act1.2026-08-27.001",
          offlineRewardState: {
            source: "accounts.created_at_fallback",
            lastOfflineRewardClaimAt: "2026-08-28T00:00:00.000Z",
            lastStoryProgressId: "none",
            lastBalanceConfigVersion: null,
            lastNarrativeVersion: "act1.2026-08-27.001",
            lastPreviewedAt: null,
            revision: 0,
            updatedAt: "2026-08-28T00:00:00.000Z"
          },
          lastClaim: {
            offlineRewardClaimId: "offline-claim-1",
            claimedAt: "2026-08-28T04:00:00.000Z"
          },
          supportContext: {
            readOnly: true,
            claimMutationEnabled: false,
            mutationEndpointsEnabled: false
          }
        }
      ],
      nextCursor: "cursor-2"
    });

    expect(result.permission).toBe("OFFLINE_REWARD_READ");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(result.items[0]?.offlineRewardState.source).toBe("accounts.created_at_fallback");
    expect(result.items[0]?.supportContext).toEqual({
      readOnly: true,
      claimMutationEnabled: false,
      mutationEndpointsEnabled: false
    });
    expect(JSON.stringify(result)).not.toContain("raw body");
  });

  it("maps null Admin API offline reward detail to read-only placeholders", () => {
    const result = mapOfflineRewardDetailResponse(null, "account-no-row");

    expect(result.accountId).toBe("account-no-row");
    expect(result.displayName).toBe("연동 대기");
    expect(result.offlineRewardState.source).toBe("연동 대기");
    expect(result.lastClaim.offlineRewardClaimId).toBe("연동 대기");
    expect(result.supportContext).toEqual({
      readOnly: true,
      claimMutationEnabled: false,
      mutationEndpointsEnabled: false
    });
  });

  it("prepares offline reward operations policy without trusting client state", () => {
    expect(offlineRewardOperationsPolicy.map((item) => item.code)).toEqual([
      "server_records_authoritative",
      "unity_client_state_not_source_of_truth",
      "risk_not_direct_multiplier",
      "long_absence_cap_overflow_not_claimable",
      "minimum_claim_time_not_claimable",
      "empty_claim_does_not_advance_baseline",
      "duplicate_replay_no_revision",
      "no_row_fallback_expected_before_first_claim"
    ]);
    expect(offlineRewardOperationsPolicy.every((item) => item.value === "연동 대기")).toBe(true);
    expect(JSON.stringify(offlineRewardOperationsPolicy)).not.toContain("raw body");
    expect(JSON.stringify(offlineRewardOperationsPolicy)).not.toContain("reward amount");
  });

  it("maps offline reward operations error codes to support severity buckets", () => {
    expect(offlineRewardOperationsSeverityMappings).toEqual([
      { errorCode: "offline_reward_not_claimable", severity: "info", category: "expected_behavior" },
      { errorCode: "duplicate_replay", severity: "info", category: "expected_behavior" },
      { errorCode: "offline_story_progress_missing", severity: "needs_review", category: "offline_policy_review" },
      { errorCode: "offline_reward_policy_missing", severity: "needs_review", category: "offline_policy_review" },
      { errorCode: "offline_reward_policy_invalid", severity: "needs_review", category: "offline_policy_review" },
      { errorCode: "offline_reward_state_conflict", severity: "high_risk", category: "offline_state_integrity" },
      { errorCode: "offline_reward_clock_tamper", severity: "high_risk", category: "offline_state_integrity" },
      { errorCode: "offline_reward_claim_conflict", severity: "high_risk", category: "offline_state_integrity" },
      { errorCode: "offline_reward_plan_mismatch", severity: "high_risk", category: "offline_state_integrity" },
      { errorCode: "economy_mutation_conflict", severity: "high_risk", category: "offline_state_integrity" }
    ]);
  });
});
