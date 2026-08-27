import { describe, expect, it } from "vitest";
import { dashboardSections, dashboardShortcuts } from "./DashboardPage";

describe("DashboardPage configuration", () => {
  it("includes ad, reward, and risk operations sections in Korean", () => {
    const sectionTitles = dashboardSections.map((section) => section.title);

    expect(sectionTitles).toContain("광고/보상형 광고");
    expect(sectionTitles).toContain("재화/보상");
    expect(sectionTitles).toContain("위험 신호");
    expect(sectionTitles).toContain("위험도/보상 불균형");
    expect(dashboardSections.flatMap((section) => section.metrics.map((metric) => metric.label))).toContain(
      "광고 완료율"
    );
  });

  it("prepares risk reward imbalance metrics without hard-coded economy amounts", () => {
    const riskRewardSection = dashboardSections.find((section) => section.title === "위험도/보상 불균형");
    const labels = riskRewardSection?.metrics.map((metric) => metric.label) ?? [];

    expect(labels).toEqual([
      "위험도별 평균 보상",
      "위험도별 클리어율",
      "위험도별 평균 실패 공세",
      "예상 보상 vs 실제 미리보기 불일치",
      "Risk IV/V 보상 수령 실패율",
      "first clear 보상 지급 상태"
    ]);
    expect(riskRewardSection?.metrics.every((metric) => metric.value === "연동 대기")).toBe(true);
  });

  it("prepares offline reward progression metrics without hard-coded reward amounts", () => {
    const offlineRewardSection = dashboardSections.find((section) => section.title === "오프라인 보상 진행도");
    const labels = offlineRewardSection?.metrics.map((metric) => metric.label) ?? [];

    expect(labels).toEqual([
      "최고 스토리 진행도",
      "기본 생산력 증가",
      "보관 한도",
      "자원 비중",
      "제외 반복 보상",
      "오프라인 수령 상태"
    ]);
    expect(offlineRewardSection?.metrics.every((metric) => metric.value === "연동 대기")).toBe(true);
    expect(offlineRewardSection?.metrics.map((metric) => metric.note).join(" ")).not.toContain("지휘권");
  });

  it("keeps dashboard shortcuts pointed at existing read-only queues", () => {
    expect(
      dashboardShortcuts.some(
        (shortcut) => shortcut.label === "의심 플레이어" && shortcut.href === "/integrity/suspicious-players"
      )
    ).toBe(true);
    expect(
      dashboardShortcuts.some((shortcut) => shortcut.label === "무결성 이벤트" && shortcut.href === "/integrity")
    ).toBe(true);
  });
});
