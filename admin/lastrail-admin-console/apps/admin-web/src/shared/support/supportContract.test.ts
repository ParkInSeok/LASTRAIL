import { describe, expect, it } from "vitest";
import {
  accountAuthErrorCodeLabel,
  accountAuthLinkStatusLabel,
  accountAuthProviderLabel,
  accountAuthSessionFieldLabel,
  accountAuthStateLabel,
  accountAuthVisibilityFields,
  customerSupportCombatEvidenceFields,
  customerSupportCombatForbiddenExamplesOutOfScope,
  customerSupportGachaLaunchEvidenceFields,
  customerSupportGachaLaunchPolicy,
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
  formatConcurrentUsersDisplay,
  mapSupportAccountSummaryResponse,
  customerSupportMutationControlsOutOfScope,
  customerSupportNoteFormatFields,
  customerSupportOpsErrorsEvidencePolicy,
  customerSupportSafeSnippetPlaceholders,
  customerSupportSuggestionCases,
  customerSupportSuggestionCaseLabel,
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

  it("prepares Google provider and account link read-only visibility candidates", () => {
    expect(accountAuthVisibilityFields).toEqual([
      "identityProviders",
      "providerNames",
      "linkedProviders",
      "primaryProvider",
      "hasGoogleLink",
      "activeSessionCount",
      "latestSessionCreatedAt",
      "latestSessionAt",
      "latestSessionExpiresAt",
      "latestAuthAt",
      "lastLoginAt",
      "lastLoginAgeSeconds",
      "lastSeenAt",
      "lastSeenSource",
      "concurrentUsers",
      "concurrentUsersPrecision"
    ]);
    expect(accountAuthSessionFieldLabel("lastLoginAgeSeconds")).toBe("마지막 로그인 경과 초");
    expect(accountAuthSessionFieldLabel("concurrentUsersPrecision")).toBe("동접자 정밀도");
    expect(accountAuthProviderLabel("guest")).toBe("게스트");
    expect(accountAuthProviderLabel("google")).toBe("Google");
    expect(accountAuthProviderLabel("GUEST")).toBe("게스트");
    expect(accountAuthProviderLabel("GOOGLE")).toBe("Google");
    expect(accountAuthStateLabel("guest_only")).toBe("게스트 전용");
    expect(accountAuthStateLabel("google_linked")).toBe("Google 연동");
    expect(accountAuthStateLabel("restore_candidate")).toBe("복구 후보");
    expect(accountAuthStateLabel("identity_conflict")).toBe("계정 식별 충돌");
    expect(accountAuthStateLabel("provider_unlinked")).toBe("제공자 연결 해제");
    expect(accountAuthStateLabel("session_active")).toBe("세션 활성");
    expect(accountAuthStateLabel("session_expired")).toBe("세션 만료");
    expect(accountAuthLinkStatusLabel("LINKED")).toBe("연동됨");
    expect(accountAuthLinkStatusLabel("ALREADY_LINKED")).toBe("이미 연동됨");
    expect(accountAuthErrorCodeLabel("google_identity_already_linked")).toBe("Google 계정 이미 연동됨");
    expect(accountAuthErrorCodeLabel("provider_identity_conflict")).toBe("제공자 계정 식별 충돌");
    expect(accountAuthErrorCodeLabel("google_token_invalid")).toBe("Google 토큰 유효하지 않음");
    expect(accountAuthErrorCodeLabel("google_auth_config_missing")).toBe("Google 인증 설정 누락");
    expect(accountAuthProviderLabel("future_provider")).toBe("future_provider");
    expect(accountAuthStateLabel("future_state")).toBe("future_state");
  });

  it("formats unavailable concurrent user metrics without presenting them as exact counts", () => {
    expect(formatConcurrentUsersDisplay(null, "UNAVAILABLE")).toBe("집계 불가 (UNAVAILABLE)");
    expect(formatConcurrentUsersDisplay(undefined, null)).toBe("연동 대기");
    expect(formatConcurrentUsersDisplay(42, "EXACT")).toBe("42");
    expect(formatConcurrentUsersDisplay(42, "ESTIMATED")).toBe("약 42");
  });

  it("prepares boss raid and run-start package support evidence without raw combat internals", () => {
    expect(customerSupportCombatEvidenceFields).toEqual([
      "encounterType",
      "raidId",
      "bossId",
      "bossDefeated",
      "completedPhaseCount",
      "encounterDurationSeconds",
      "startAugmentPackageRef",
      "startTacticPackageRef",
      "selectedAugmentIds",
      "selectedTacticIds",
      "source"
    ]);
    expect(customerSupportContractPreview.runCombat.readOnly).toBe(true);
    expect(customerSupportContractPreview.runCombat.encounterTypes).toEqual(["ORDINARY_15_WAVE", "RAID_BOSS"]);
    expect(customerSupportContractPreview.runCombat.startPackageSource).toBe("run_start");
    expect(customerSupportContractPreview.runCombat.packageRefPolicy).toBe("server-authored run start package");
    expect(customerSupportContractPreview.runCombat.sourceOfTruthExclusion).toBe(
      "reward/economy/story/offline/gacha Source of Truth 아님"
    );
    expect(customerSupportContractPreview.runCombat.supportFacingErrorCodes).toEqual([
      "package_ref_mismatch",
      "contract_violation"
    ]);

    const serialized = JSON.stringify(customerSupportContractPreview);
    expect(serialized).toContain("RAID_BOSS");
    expect(serialized).toContain("run_start");
    expect(serialized).toContain("ORDINARY_15_WAVE");
    for (const forbidden of customerSupportCombatForbiddenExamplesOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
    expect(serialized).not.toContain("mid-wave");
  });

  it("prepares launch gacha support visibility as read-only evidence", () => {
    expect(customerSupportGachaLaunchEvidenceFields).toEqual([
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
    expect(customerSupportGachaLaunchPolicy.readOnly).toBe(true);
    expect(customerSupportGachaLaunchPolicy.scopeStatus).toBe("TESTING_TUNING_DRAFT");
    expect(customerSupportGachaLaunchPolicy.productionFinal).toBe(false);
    expect(customerSupportGachaLaunchPolicy.seedPromotionApproved).toBe(false);
    expect(customerSupportGachaLaunchPolicy.pullCounts).toEqual(["1-pull", "10-pull"]);
    expect(customerSupportGachaLaunchPolicy.pityThresholds).toEqual({
      rare: "10",
      elite: "30",
      pickup: "60"
    });
    expect(customerSupportGachaLaunchPolicy.resultFields).toEqual([
      "pullSequence",
      "resultRarity",
      "resultType",
      "resultId",
      "reward",
      "isPickup",
      "wasDuplicate",
      "duplicateConversion"
    ]);
    expect(customerSupportGachaLaunchPolicy.actOneEliteOperatorIds).toEqual([
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
    for (const initialOperatorId of ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "op_rin", "op_cipher"]) {
      expect(customerSupportGachaLaunchPolicy.actOneEliteOperatorIds).not.toContain(initialOperatorId);
    }

    const serialized = JSON.stringify(customerSupportGachaLaunchPolicy);
    expect(serialized).not.toContain("raw RNG");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("full weights");
    expect(serialized).not.toContain("seed preimage");
    expect(serialized).not.toContain("manual grant");
    expect(serialized).not.toContain("repair");
    expect(serialized).not.toContain("reroll");
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
    expect(serialized).toContain("google");
    expect(serialized).toContain("guest");
    expect(serialized).toContain("identity_conflict");
    for (const forbidden of customerSupportMutationControlsOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
    for (const forbidden of customerSupportSensitiveExamplesOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
    expect(serialized).not.toContain("Google ID token");
    expect(serialized).not.toContain("OAuth token");
    expect(serialized).not.toContain("session secret");
    expect(serialized).not.toContain("refresh token");
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

  it("prepares case-specific read-only suggestion snippets without support actions", () => {
    expect(customerSupportSuggestionCases.map((item) => item.caseType)).toEqual([
      "google_login_link_conflict",
      "auth_session_expired",
      "reward_claim_issue",
      "offline_reward_issue",
      "gacha_duplicate_guarantee_inquiry",
      "raid_boss_only_result_inquiry",
      "start_package_ref_mismatch",
      "ops_error_observed"
    ]);
    expect(customerSupportSuggestionCaseLabel("google_login_link_conflict")).toBe("Google 로그인/연동 충돌");
    expect(customerSupportSuggestionCaseLabel("future_case")).toBe("future_case");
    expect(customerSupportSuggestionCases.every((item) => item.availability === "연동 대기")).toBe(true);
    expect(customerSupportSuggestionCases.every((item) => item.readOnly === true)).toBe(true);
    expect(customerSupportSuggestionCases.every((item) => item.copyActionEnabled === false)).toBe(true);
    expect(customerSupportSuggestionCases.every((item) => item.sendActionEnabled === false)).toBe(true);
    expect(customerSupportSuggestionCases.every((item) => item.ticketCreationEnabled === false)).toBe(true);
    expect(customerSupportSuggestionCases.every((item) => item.escalationWriteEnabled === false)).toBe(true);
    expect(customerSupportSuggestionCases.find((item) => item.caseType === "gacha_duplicate_guarantee_inquiry")?.safeSnippet).toBe(
      "중복 요청은 기존 처리 결과를 다시 보여주는 방식입니다."
    );
    expect(customerSupportSuggestionCases.find((item) => item.caseType === "raid_boss_only_result_inquiry")?.safeSnippet).toContain(
      "보스 전용 레이드"
    );
    expect(customerSupportSuggestionCases.find((item) => item.caseType === "start_package_ref_mismatch")?.safeSnippet).toContain(
      "시작 시점"
    );

    const serialized = JSON.stringify(customerSupportSuggestionCases);
    expect(serialized).not.toContain("copy action");
    expect(serialized).not.toContain("ticket creation");
    expect(serialized).not.toContain("send template");
    expect(serialized).not.toContain("raw payload");
    expect(serialized).not.toContain("raw auth header");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("full weights");
    expect(serialized).not.toContain("animation timeline");
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
        providerNames: ["guest", "google"],
        identityProviders: ["google", "guest"],
        linkedProviders: ["google"],
        primaryProvider: "google",
        hasGoogleLink: true,
        accountState: "google_linked",
        linkStatus: "LINKED",
        lastLoginAt: "2026-08-29T00:00:00.000Z",
        lastLoginAgeSeconds: 300,
        activeSessionCount: 1,
        latestSessionAt: "2026-08-29T00:00:00.000Z",
        latestSessionCreatedAt: "2026-08-28T23:55:00.000Z",
        latestSessionExpiresAt: "2026-08-30T00:00:00.000Z",
        latestAuthAt: "2026-08-29T00:00:00.000Z",
        lastSeenAt: "2026-08-29T00:04:00.000Z",
        lastSeenSource: "game_api",
        concurrentUsers: null,
        concurrentUsersPrecision: "UNAVAILABLE"
      },
      recentRuns: [{ runId: "run-1", operationId: "opn_01_last_departure", risk: "I", outcome: "VICTORY" }],
      runRewards: [{ rewardPreviewId: "reward-preview-1", rewardClaimId: "reward-claim-1", status: "CLAIMED" }],
      offlineReward: { stateSource: "account_offline_reward_state", latestClaimId: "offline-claim-1" },
      storyProgress: { highestStoryProgressId: "opn_01_last_departure", unlockedOperations: ["opn_02_first_signal"] },
      gacha: { recentBatchIds: ["batch-1"], guaranteeTypes: ["ELITE_30"] },
      idempotency: { operationTypes: ["run_finish", "offline_reward_claim"], duplicateReplayCount: 1 },
      integrity: { openSignals: 0, reviewStatus: "CLEARED" },
      opsErrors: [
        {
          errorLogId: "err-1",
          occurredAt: "2026-08-29T00:00:00.000Z",
          environment: "DEV",
          service: "game_api",
          severity: "critical",
          errorCode: "gacha_state_conflict",
          messageSummary: "Gacha state conflict while composing support evidence",
          status: "open",
          source: "game_api",
          requestId: "req-1",
          accountId: "account-1",
          runId: "run-1",
          rewardPreviewId: "reward-preview-1",
          rewardClaimId: "reward-claim-1",
          offlineRewardClaimId: "offline-claim-1",
          gachaBatchId: "batch-1",
          gachaBannerId: "banner-1",
          storyProgressId: "opn_01_last_departure",
          integrityEventId: "integrity-event-1",
          supportTicketId: "support-ticket-1",
          links: [{ label: "시스템 에러 로그", href: "/ops/error-logs/err-1" }],
          rawBody: "must-not-copy",
          stackDump: "must-not-copy",
          accessToken: "must-not-copy"
        }
      ],
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
    expect(result.authSession.providerOrder).toEqual(["GOOGLE", "GUEST"]);
    expect(result.authSession.items.find((item) => item.field === "primaryProvider")?.value).toBe("Google");
    expect(result.authSession.items.find((item) => item.field === "hasGoogleLink")?.value).toBe("연동됨");
    expect(result.authSession.items.find((item) => item.field === "lastLoginAgeSeconds")?.value).toBe("300초");
    expect(result.authSession.items.find((item) => item.field === "concurrentUsers")?.value).toBe(
      "집계 불가 (UNAVAILABLE)"
    );
    expect(result.authSession.items.find((item) => item.field === "concurrentUsersPrecision")?.value).toBe(
      "UNAVAILABLE"
    );
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
    expect(result.opsErrors).toEqual([
      {
        errorLogId: "err-1",
        occurredAt: "2026-08-29T00:00:00.000Z",
        environment: "DEV",
        service: "game_api",
        severity: "critical",
        errorCode: "gacha_state_conflict",
        messageSummary: "Gacha state conflict while composing support evidence",
        status: "open",
        source: "game_api",
        requestId: "req-1",
        correlationIds: {
          accountId: "account-1",
          runId: "run-1",
          rewardPreviewId: "reward-preview-1",
          rewardClaimId: "reward-claim-1",
          offlineRewardClaimId: "offline-claim-1",
          gachaBatchId: "batch-1",
          gachaBannerId: "banner-1",
          storyProgressId: "opn_01_last_departure",
          integrityEventId: "integrity-event-1",
          supportTicketId: "support-ticket-1"
        },
        links: [{ label: "시스템 에러 로그", href: "/ops/error-logs/err-1" }]
      }
    ]);
    expect(JSON.stringify(result)).not.toContain("refreshToken");
    expect(JSON.stringify(result)).not.toContain("refresh token hash");
    expect(JSON.stringify(result)).not.toContain("googleIdToken");
    expect(JSON.stringify(result)).not.toContain("sessionSecret");
    expect(JSON.stringify(result)).not.toContain("raw body");
    expect(JSON.stringify(result)).not.toContain("rawBody");
    expect(JSON.stringify(result)).not.toContain("stackDump");
    expect(JSON.stringify(result)).not.toContain("accessToken");
  });

  it("maps null support summary response to placeholder fallback", () => {
    const result = mapSupportAccountSummaryResponse(null, "account-missing");

    expect(result).toEqual(emptySupportAccountSummary("account-missing"));
    expect(result.sections.every((section) => section.value === "연동 대기")).toBe(true);
  });

  it("keeps support opsErrors as read-only evidence and not a repair mechanism", () => {
    expect(customerSupportOpsErrorsEvidencePolicy).toEqual({
      field: "opsErrors",
      launchRole: "read-only evidence",
      repairMechanism: false,
      futureLimit: 5,
      ordering: "critical first, latest first"
    });
  });
});
