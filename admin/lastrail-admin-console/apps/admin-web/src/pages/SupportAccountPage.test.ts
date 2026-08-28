import { describe, expect, it } from "vitest";
import {
  supportAccountContextCards,
  supportCaseTypeCards,
  supportOutOfScopeControls
} from "./SupportAccountPage";

describe("SupportAccountPage configuration", () => {
  it("prepares launch customer support case cards", () => {
    expect(supportCaseTypeCards.map((card) => card.code)).toEqual([
      "Account/Auth",
      "Run Completion",
      "Run Reward",
      "Offline Reward",
      "Story Progress/Unlock",
      "Gacha/Recruitment",
      "Integrity-restricted Reward"
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
});
