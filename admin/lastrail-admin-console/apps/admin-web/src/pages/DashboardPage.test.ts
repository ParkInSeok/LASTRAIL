import { describe, expect, it } from "vitest";
import { dashboardSections, dashboardShortcuts } from "./DashboardPage";

describe("DashboardPage configuration", () => {
  it("includes ad, reward, and risk operations sections in Korean", () => {
    const sectionTitles = dashboardSections.map((section) => section.title);

    expect(sectionTitles).toContain("광고/보상형 광고");
    expect(sectionTitles).toContain("재화/보상");
    expect(sectionTitles).toContain("위험 신호");
    expect(dashboardSections.flatMap((section) => section.metrics.map((metric) => metric.label))).toContain(
      "광고 완료율"
    );
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
