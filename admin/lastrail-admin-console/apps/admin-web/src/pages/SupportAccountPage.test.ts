import { describe, expect, it } from "vitest";
import {
  supportAccountAuthEvidenceCards,
  supportAccountContextCards,
  supportCaseTypeCards,
  supportCombatEvidenceCards,
  supportGachaLaunchEvidenceCards,
  supportEscalationActionCards,
  supportEscalationCategoryCards,
  supportEscalationEvidenceCards,
  supportEscalationSeverityCards,
  supportRepairAuditCards,
  supportRepairPolicyCards,
  supportSummarySectionCards,
  supportOutOfScopeControls,
  supportResponseTemplateCards,
  supportSafeSnippetCards,
  supportSuggestionCards
} from "./SupportAccountPage";

describe("SupportAccountPage configuration", () => {
  it("prepares launch customer support case cards", () => {
    expect(supportCaseTypeCards.map((card) => card.code)).toEqual([
      "account_auth",
      "run_completion",
      "run_reward",
      "offline_reward",
      "story_progress",
      "gacha_recruitment",
      "integrity_restriction"
    ]);
    expect(supportCaseTypeCards.every((card) => card.value === "연동 대기")).toBe(true);
  });

  it("prepares read-only account support context placeholders", () => {
    expect(supportSummarySectionCards.map((card) => card.code)).toEqual([
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
    expect(supportSummarySectionCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(supportAccountContextCards.map((card) => card.field)).toEqual([
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
    expect(supportAccountContextCards.find((card) => card.field === "recentOpsErrorLogs")?.note).toContain(
      "latest 5"
    );
    expect(supportAccountContextCards.find((card) => card.field === "recentOpsErrorLogs")?.note).toContain(
      "critical first"
    );
    expect(supportAccountContextCards.find((card) => card.field === "recentOpsErrorLogs")?.note).toContain(
      "source/correlation ids"
    );
    expect(supportAccountContextCards.every((card) => card.value === "연동 대기")).toBe(true);
  });

  it("prepares Google provider and account recovery evidence placeholders", () => {
    expect(supportAccountAuthEvidenceCards.map((card) => card.code)).toEqual([
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
    expect(supportAccountAuthEvidenceCards.map((card) => card.label).join(" ")).toContain("Google");
    expect(supportAccountAuthEvidenceCards.map((card) => card.label)).toContain("주 제공자");
    expect(supportAccountAuthEvidenceCards.find((card) => card.code === "concurrentUsers")?.note).toContain(
      "UNAVAILABLE/null은 확정 동접자 수 아님"
    );
    expect(supportAccountAuthEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(JSON.stringify(supportAccountAuthEvidenceCards)).not.toContain("Google ID token");
    expect(JSON.stringify(supportAccountAuthEvidenceCards)).not.toContain("raw auth header");
  });

  it("prepares run and raid support evidence placeholders without combat internals", () => {
    expect(supportCombatEvidenceCards.map((card) => card.code)).toEqual([
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
    expect(supportCombatEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(supportCombatEvidenceCards.find((card) => card.code === "source")?.note).toContain("run_start");
    expect(supportCombatEvidenceCards.find((card) => card.code === "startAugmentPackageRef")?.note).toContain(
      "server-authored run start package"
    );
    expect(supportCombatEvidenceCards.find((card) => card.code === "encounterType")?.note).toContain(
      "ORDINARY_15_WAVE / RAID_BOSS"
    );
    expect(supportCombatEvidenceCards.map((card) => card.note).join(" ")).toContain(
      "reward/economy/story/offline/gacha Source of Truth 아님"
    );

    const serialized = JSON.stringify(supportCombatEvidenceCards);
    expect(serialized).not.toContain("raw combat timeline");
    expect(serialized).not.toContain("animation frame");
    expect(serialized).not.toContain("debug payload");
    expect(serialized).not.toContain("mid-wave");
  });

  it("prepares gacha launch support evidence placeholders", () => {
    expect(supportGachaLaunchEvidenceCards.map((card) => card.code)).toEqual([
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
    expect(supportGachaLaunchEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "pityThresholds")?.note).toContain(
      "draft/contract candidate"
    );
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "TESTING_TUNING_DRAFT"
    );
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "productionFinal=false"
    );
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "seedPromotionApproved=false"
    );
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "actOneEliteVisualScope")?.note).toContain(
      "op_sable"
    );
    expect(supportGachaLaunchEvidenceCards.find((card) => card.code === "actOneEliteVisualScope")?.note).not.toContain(
      "op_kyle"
    );

    const serialized = JSON.stringify(supportGachaLaunchEvidenceCards);
    expect(serialized).not.toContain("raw RNG");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("full weights");
    expect(serialized).not.toContain("seed preimage");
    expect(serialized).not.toContain("manual grant");
    expect(serialized).not.toContain("reroll");
  });

  it("does not expose mutation controls in the support skeleton", () => {
    expect(supportOutOfScopeControls).toEqual([
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
  });

  it("prepares response template availability without exposing long template text", () => {
    expect(supportResponseTemplateCards.map((card) => card.code)).toEqual([
      "internalChecks",
      "playerFacingTemplate",
      "escalateWhen"
    ]);
    expect(supportResponseTemplateCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(supportSafeSnippetCards.map((card) => card.code)).toEqual([
      "server_record_checked",
      "duplicate_replay_existing_result",
      "offline_claimable_cap_applied",
      "first_clear_finish_time_basis",
      "progress_and_text_are_separate",
      "rng_probability_not_disclosed",
      "escalated_for_ops_review"
    ]);
    expect(supportSafeSnippetCards.every((card) => card.value === "연동 대기")).toBe(true);
  });

  it("prepares case-specific CS suggestion cards as read-only availability", () => {
    expect(supportSuggestionCards.map((card) => card.code)).toEqual([
      "google_login_link_conflict",
      "auth_session_expired",
      "reward_claim_issue",
      "offline_reward_issue",
      "gacha_duplicate_guarantee_inquiry",
      "raid_boss_only_result_inquiry",
      "start_package_ref_mismatch",
      "ops_error_observed"
    ]);
    expect(supportSuggestionCards.map((card) => card.label)).toContain("Google 로그인/연동 충돌");
    expect(supportSuggestionCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(supportSuggestionCards.find((card) => card.code === "gacha_duplicate_guarantee_inquiry")?.note).toContain(
      "중복 요청은 기존 처리 결과"
    );
    expect(supportSuggestionCards.find((card) => card.code === "raid_boss_only_result_inquiry")?.note).toContain(
      "보스 전용 레이드"
    );
    expect(supportSuggestionCards.find((card) => card.code === "start_package_ref_mismatch")?.note).toContain(
      "시작 시점"
    );

    const serialized = JSON.stringify(supportSuggestionCards);
    expect(serialized).not.toContain("copy action");
    expect(serialized).not.toContain("send template");
    expect(serialized).not.toContain("ticket creation");
    expect(serialized).not.toContain("raw payload");
    expect(serialized).not.toContain("raw auth header");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("full weights");
    expect(serialized).not.toContain("animation timeline");
  });

  it("prepares escalation placeholders without write actions", () => {
    expect(supportEscalationSeverityCards.map((card) => card.code)).toEqual([
      "info",
      "needs_review",
      "high_risk",
      "blocked"
    ]);
    expect(supportEscalationCategoryCards.map((card) => card.code)).toEqual([
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
    expect(supportEscalationActionCards.map((card) => card.code)).toEqual([
      "respond_with_template",
      "request_more_player_context",
      "escalate_to_operator_review",
      "escalate_to_owner_review",
      "mark_as_known_issue_candidate",
      "attach_read_only_evidence"
    ]);
    expect(supportEscalationEvidenceCards.map((card) => card.code)).toEqual([
      "severity",
      "category",
      "recommendedAction",
      "primaryReferenceId",
      "readOnlyEvidencePackage"
    ]);
    expect(
      [
        ...supportEscalationSeverityCards,
        ...supportEscalationCategoryCards,
        ...supportEscalationActionCards,
        ...supportEscalationEvidenceCards
      ].every((card) => card.value === "연동 대기")
    ).toBe(true);
  });

  it("prepares repair compensation placeholders as disabled launch policy", () => {
    expect(supportRepairPolicyCards.map((card) => card.code)).toEqual([
      "manual_currency_item_grant",
      "offline_reward_timestamp_reset",
      "story_progress_unlock_mutation",
      "gacha_result_reroll",
      "reward_economy_ledger_edit",
      "localization_narrative_publish"
    ]);
    expect(supportRepairPolicyCards.every((card) => card.value === "비활성")).toBe(true);
    expect(supportRepairAuditCards.map((card) => card.code)).toEqual([
      "supportTicketId",
      "accountId",
      "operatorId",
      "approverId",
      "permissionUsed",
      "reasonCode",
      "affectedIds",
      "beforeAfterSanitizedSummary"
    ]);
    expect(supportRepairAuditCards.every((card) => card.value === "연동 대기")).toBe(true);
  });
});
