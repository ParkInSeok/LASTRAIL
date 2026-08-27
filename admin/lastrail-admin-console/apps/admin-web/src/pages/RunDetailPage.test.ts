import { describe, expect, it } from "vitest";
import { runRiskRewardPlaceholders, runStoryNarrativePlaceholders } from "./RunDetailPage";

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

  it("keeps risk and calculation placeholders aligned with display labels", () => {
    const notes = runRiskRewardPlaceholders.map((placeholder) => placeholder.note);

    expect(notes).toContain("위험도 I~V 표시 후보");
    expect(notes).toContain("첫 클리어 보너스 표시 후보");
    expect(notes).toContain("서버 보상 미리보기 / Unity 진단 참고 신호 분리");
  });

  it("prepares story progress and narrative status placeholders without narrative text", () => {
    const labels = runStoryNarrativePlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "작전 ID",
      "스토리 진행도",
      "첫 클리어 상태",
      "미션 내러티브 상태",
      "현지화 데이터 상태",
      "누락 데이터 상태",
      "플레이어 문의 지원 문맥"
    ]);
    expect(runStoryNarrativePlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(runStoryNarrativePlaceholders.map((placeholder) => placeholder.note).join(" ")).not.toContain("대사");
  });
});
