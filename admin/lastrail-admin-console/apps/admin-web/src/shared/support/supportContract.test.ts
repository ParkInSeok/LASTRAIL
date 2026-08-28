import { describe, expect, it } from "vitest";
import {
  customerSupportCaseTypes,
  customerSupportCaseTypeLabel,
  customerSupportContractPreview,
  customerSupportContextFields,
  customerSupportEscalationActionLabel,
  customerSupportEscalationActions,
  customerSupportEscalationCategories,
  customerSupportEscalationCategoryLabel,
  customerSupportEscalationEvidenceFields,
  customerSupportEscalationFuturePermissions,
  customerSupportEscalationSeverityLabel,
  customerSupportEscalationSeverities,
  customerSupportRepairAuditFields,
  customerSupportRepairCategories,
  customerSupportRepairCategoryLabel,
  customerSupportRepairFuturePermissions,
  customerSupportRepairLaunchStatusLabel,
  emptySupportAccountSummary,
  mapSupportAccountSummaryResponse,
  customerSupportMutationControlsOutOfScope,
  customerSupportNoteFormatFields,
  customerSupportSafeSnippetPlaceholders,
  customerSupportSensitiveExamplesOutOfScope,
  customerSupportTemplateStatuses
} from "./supportContract";

describe("supportContract", () => {
  it("prepares launch customer support case types as stable read-only candidates", () => {
    expect(customerSupportCaseTypes).toEqual([
      "account_auth",
      "run_completion",
      "run_reward",
      "offline_reward",
      "story_progress",
      "gacha_recruitment",
      "integrity_restriction"
    ]);
    expect(customerSupportCaseTypeLabel("account_auth")).toBe("계정·인증");
    expect(customerSupportCaseTypeLabel("gacha_recruitment")).toBe("모집·합류");
    expect(customerSupportCaseTypeLabel("future_case")).toBe("future_case");
  });

  it("prepares account support context fields without server integration", () => {
    expect(customerSupportContextFields).toEqual([
      "accountStatus",
      "recentRuns",
      "runRewardPreviewClaimIds",
      "offlineRewardStateClaimIds",
      "storyProgressUnlocks",
      "gachaBatchGuaranteeType",
      "idempotencyReplayStatus",
      "integrityDisposition",
      "recentOpsErrorLogs"
    ]);
    expect(customerSupportContractPreview.accountId).toBe("연동 대기");
    expect(customerSupportContractPreview.status).toBe("연동 대기");
    expect(customerSupportContractPreview.context.every((item) => item.value === "연동 대기")).toBe(true);
  });

  it("keeps support workflow launch scope read-only and free of sensitive examples", () => {
    expect(customerSupportMutationControlsOutOfScope).toEqual([
      "manual currency grants",
      "reward repair buttons",
      "offline reward timestamp reset",
      "story progress mutation",
      "unlock mutation",
      "gacha reroll/repair",
      "localization publishing",
      "narrative editing",
      "economy correction"
    ]);
    expect(customerSupportSensitiveExamplesOutOfScope).toEqual([
      "token",
      "raw auth header",
      "raw body",
      "raw RNG",
      "seed preimage",
      "per-candidate probability",
      "full weight list"
    ]);

    const serialized = JSON.stringify(customerSupportContractPreview);
    for (const forbidden of customerSupportMutationControlsOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
    for (const forbidden of customerSupportSensitiveExamplesOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
  });

  it("prepares response template placeholders without copy or send actions", () => {
    expect(customerSupportNoteFormatFields).toEqual([
      "caseType",
      "accountId",
      "primaryReferenceId",
      "observedStatus",
      "serverEvidence",
      "playerFacingSummary",
      "escalationNeeded"
    ]);
    expect(customerSupportTemplateStatuses).toEqual([
      "internalChecks",
      "playerFacingTemplate",
      "escalateWhen"
    ]);
    expect(customerSupportSafeSnippetPlaceholders).toEqual([
      "server_record_checked",
      "duplicate_replay_existing_result",
      "offline_claimable_cap_applied",
      "first_clear_finish_time_basis",
      "progress_and_text_are_separate",
      "rng_probability_not_disclosed",
      "escalated_for_ops_review"
    ]);

    const serialized = JSON.stringify(customerSupportContractPreview);
    expect(serialized).not.toContain("copy action");
    expect(serialized).not.toContain("ticket creation");
    expect(serialized).not.toContain("send template");
  });

  it("prepares escalation workflow placeholders as read-only support evidence", () => {
    expect(customerSupportEscalationSeverities).toEqual([
      "info",
      "needs_review",
      "high_risk",
      "blocked"
    ]);
    expect(customerSupportEscalationSeverityLabel("needs_review")).toBe("검토 필요");
    expect(customerSupportEscalationSeverityLabel("future_severity")).toBe("future_severity");
    expect(customerSupportEscalationCategories).toEqual([
      "reward_claim_missing_ledger",
      "reward_preview_without_claim",
      "offline_state_conflict",
      "offline_claim_duplicate_mismatch",
      "story_unlock_missing",
      "gacha_guarantee_conflict",
      "integrity_restricted_reward",
      "account_identity_conflict",
      "narrative_content_missing",
      "expected_behavior"
    ]);
    expect(customerSupportEscalationCategoryLabel("gacha_guarantee_conflict")).toBe("모집 보장 상태 충돌");
    expect(customerSupportEscalationActions).toEqual([
      "respond_with_template",
      "request_more_player_context",
      "escalate_to_operator_review",
      "escalate_to_owner_review",
      "mark_as_known_issue_candidate",
      "attach_read_only_evidence"
    ]);
    expect(customerSupportEscalationActionLabel("attach_read_only_evidence")).toBe("읽기 전용 근거 첨부");
    expect(customerSupportEscalationEvidenceFields).toEqual([
      "severity",
      "category",
      "recommendedAction",
      "primaryReferenceId",
      "readOnlyEvidencePackage"
    ]);
    expect(customerSupportEscalationFuturePermissions).toEqual([
      "SUPPORT_ESCALATION_READ",
      "SUPPORT_ESCALATION_WRITE",
      "SUPPORT_REPAIR_PREVIEW",
      "SUPPORT_REPAIR_APPROVE",
      "SUPPORT_REPAIR_EXECUTE"
    ]);
    expect(customerSupportContractPreview.escalation.readOnly).toBe(true);
    expect(customerSupportContractPreview.escalation.writeEnabled).toBe(false);
  });

  it("prepares repair compensation policy as disabled read-only placeholders", () => {
    expect(customerSupportRepairCategories).toEqual([
      "manual_currency_item_grant",
      "offline_reward_timestamp_reset",
      "story_progress_unlock_mutation",
      "gacha_result_reroll",
      "reward_economy_ledger_edit",
      "localization_narrative_publish"
    ]);
    expect(customerSupportRepairCategoryLabel("reward_economy_ledger_edit")).toBe("보상/경제 장부 편집");
    expect(customerSupportRepairCategoryLabel("future_repair")).toBe("future_repair");
    expect(customerSupportRepairLaunchStatusLabel("Disabled")).toBe("비활성");
    expect(customerSupportRepairFuturePermissions).toEqual([
      "SUPPORT_ESCALATION_READ",
      "SUPPORT_ESCALATION_WRITE",
      "SUPPORT_REPAIR_PREVIEW",
      "SUPPORT_REPAIR_APPROVE",
      "SUPPORT_REPAIR_EXECUTE"
    ]);
    expect(customerSupportRepairAuditFields).toEqual([
      "supportTicketId",
      "accountId",
      "operatorId",
      "approverId",
      "permissionUsed",
      "reasonCode",
      "affectedIds",
      "beforeAfterSanitizedSummary"
    ]);
    expect(customerSupportContractPreview.repairCompensation.readOnly).toBe(true);
    expect(customerSupportContractPreview.repairCompensation.launchStatus).toBe("Disabled");
    expect(
      customerSupportContractPreview.repairCompensation.categories.every((category) => category.status === "Disabled")
    ).toBe(true);
  });

  it("maps Admin API support account summary response into sanitized read-only sections", () => {
    const result = mapSupportAccountSummaryResponse({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      account: {
        accountId: "account-1",
        displayName: "Kyle Account",
        accountStatus: "ACTIVE"
      },
      auth: {
        providerNames: ["guest"],
        activeSessionCount: 1,
        latestSessionAt: "2026-08-29T00:00:00.000Z"
      },
      recentRuns: [{ runId: "run-1", operationId: "opn_01_last_departure", risk: "I", outcome: "VICTORY" }],
      runRewards: [{ rewardPreviewId: "reward-preview-1", rewardClaimId: "reward-claim-1", status: "CLAIMED" }],
      offlineReward: { stateSource: "account_offline_reward_state", latestClaimId: "offline-claim-1" },
      storyProgress: { highestStoryProgressId: "opn_01_last_departure", unlockedOperations: ["opn_02_first_signal"] },
      gacha: { recentBatchIds: ["batch-1"], guaranteeTypes: ["ELITE_30"] },
      idempotency: { operationTypes: ["run_finish", "offline_reward_claim"], duplicateReplayCount: 1 },
      integrity: { openSignals: 0, reviewStatus: "CLEARED" },
      opsErrors: [],
      supportNotes: { safeTemplateHints: ["server_record_checked"] },
      supportContext: {
        readOnly: true,
        repairEnabled: false,
        mutationEndpointsEnabled: false
      }
    });

    expect(result.permission).toBe("SUPPORT_READ");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(result.account.accountId).toBe("account-1");
    expect(result.sections.map((section) => section.code)).toEqual([
      "account",
      "auth",
      "recentRuns",
      "runRewards",
      "offlineReward",
      "storyProgress",
      "gacha",
      "idempotency",
      "integrity",
      "opsErrors",
      "supportNotes"
    ]);
    expect(result.supportContext).toEqual({
      readOnly: true,
      repairEnabled: false,
      mutationEndpointsEnabled: false
    });
    expect(JSON.stringify(result)).not.toContain("refreshToken");
    expect(JSON.stringify(result)).not.toContain("raw body");
  });

  it("maps null support summary response to placeholder fallback", () => {
    const result = mapSupportAccountSummaryResponse(null, "account-missing");

    expect(result).toEqual(emptySupportAccountSummary("account-missing"));
    expect(result.sections.every((section) => section.value === "연동 대기")).toBe(true);
  });
});
