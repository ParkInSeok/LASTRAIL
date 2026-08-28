import { describe, expect, it } from "vitest";
import {
  customerSupportCaseTypes,
  customerSupportContractPreview,
  customerSupportContextFields,
  customerSupportMutationControlsOutOfScope,
  customerSupportSensitiveExamplesOutOfScope
} from "./supportContract";

describe("supportContract", () => {
  it("prepares launch customer support case types as stable read-only candidates", () => {
    expect(customerSupportCaseTypes).toEqual([
      "Account/Auth",
      "Run Completion",
      "Run Reward",
      "Offline Reward",
      "Story Progress/Unlock",
      "Gacha/Recruitment",
      "Integrity-restricted Reward"
    ]);
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
});
