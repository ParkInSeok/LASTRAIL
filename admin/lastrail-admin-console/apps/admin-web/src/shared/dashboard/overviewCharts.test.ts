import { describe, expect, it } from "vitest";
import { overviewChartDefinitions, overviewDateLabels } from "./overviewCharts";

describe("overview chart layout", () => {
  it("uses KST calendar dates for both ranges", () => {
    const now = new Date("2026-09-06T16:00:00Z");
    expect(overviewDateLabels(7, now)).toEqual(["09.01", "09.07"]);
    expect(overviewDateLabels(30, now)).toEqual(["08.09", "09.07"]);
  });
  it("keeps money and percentage separate and contains no fabricated points", () => {
    expect(overviewChartDefinitions.map(c => c.title)).toEqual(["접속자 추이", "신규 가입자 추이", "광고 시청 추이", "광고 수익 추이", "패스 결제율 추이", "가입자 대비 접속률 추이"]);
    expect(overviewChartDefinitions.every(c => !("points" in c))).toBe(true);
  });
});
