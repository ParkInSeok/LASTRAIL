import { describe, expect, it } from "vitest";
import { runRiskRewardPlaceholders } from "./RunDetailPage";

describe("RunDetailPage risk reward skeleton", () => {
  it("prepares run risk reward read-only placeholders without economy amounts", () => {
    const labels = runRiskRewardPlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "위험도",
      "작전 ID",
      "결과",
      "클리어 공세",
      "rewardPreviewId / rewardClaimId",
      "적용 배율",
      "first clear",
      "무결성 판정",
      "계산 메타데이터 상태"
    ]);
    expect(runRiskRewardPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
  });
});
