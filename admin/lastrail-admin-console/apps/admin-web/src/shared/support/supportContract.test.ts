import { describe, expect, it } from "vitest";
import {
  customerSupportCaseTypes,
  customerSupportCaseTypeLabel,
  customerSupportContractPreview,
  customerSupportContextFields,
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
});
