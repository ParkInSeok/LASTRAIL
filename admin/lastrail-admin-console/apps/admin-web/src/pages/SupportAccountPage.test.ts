import { describe, expect, it } from "vitest";
import {
  supportAccountAuthEvidenceCards,
  supportAccountContextCards,
  supportCaseTypeCards,
  supportCombatEvidenceCards,
  supportEscalationActionCards,
  supportEscalationCategoryCards,
  supportEscalationEvidenceCards,
  supportEscalationSeverityCards,
  supportRepairAuditCards,
  supportRepairPolicyCards,
  supportSummarySectionCards,
  supportOutOfScopeControls,
  supportResponseTemplateCards,
  supportSafeSnippetCards
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
    expect(supportAccountContextCards.every((card) => card.value === "연동 대기")).toBe(true);
  });

  it("prepares Google provider and account recovery evidence placeholders", () => {
    expect(supportAccountAuthEvidenceCards.map((card) => card.code)).toEqual([
      "providerNames",
      "linkedProviders",
      "accountState",
      "linkStatus",
      "lastLoginAt",
      "lastLoginAge",
      "activeSessionCount",
      "latestSessionAt",
      "sanitizedIdentityRef"
    ]);
    expect(supportAccountAuthEvidenceCards.map((card) => card.label)).toContain("Google");
    expect(supportAccountAuthEvidenceCards.map((card) => card.label)).toContain("계정 식별 충돌");
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

    const serialized = JSON.stringify(supportCombatEvidenceCards);
    expect(serialized).not.toContain("raw combat timeline");
    expect(serialized).not.toContain("animation frame");
    expect(serialized).not.toContain("debug payload");
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
