import { describe, expect, it } from "vitest";
import {
  supportAccountContextCards,
  supportCaseTypeCards,
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
});
