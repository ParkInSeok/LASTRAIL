import { describe, expect, it } from "vitest";
import {
  offlineRewardPreviewContractFieldLabel,
  offlineRewardPreviewContractFields,
  offlineRewardPreviewContractPreview
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
});
