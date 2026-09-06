import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { DailyCharts } from "./DailyCharts";
import { mapDailyMetrics } from "./dailyMetrics";
import { RunVersionEvidence } from "../runs/RunVersionEvidence";
import { mapRunVersions } from "../runs/runVersions";

describe("launch evidence rendering", () => {
  it("renders real daily zero and explicitly distinguishes session diagnostics", () => {
    const html = renderToStaticMarkup(<DailyCharts days={7} calculatedAt="2026-09-06T12:00:00Z" data={mapDailyMetrics({ sourceAvailability: { signups: true }, points: [{ date: "2026-09-06", signups: 0 }] })} />);
    expect(html).toContain("2026-09-06: 0");
    expect(html).toContain("활동 DAU 아님");
    expect(html).toContain("집계 원천 없음");
    expect(html).not.toContain("광고 수익 추이");
  });
  it("shows classified loading and absence without fake charts", () => {
    expect(renderToStaticMarkup(<DailyCharts days={7} calculatedAt="" state="조회 중" />)).toContain("조회 중");
    expect(renderToStaticMarkup(<DailyCharts days={7} calculatedAt="" />)).toContain("일별 집계 미연동");
  });
  it("renders unfinished evidence without start metadata in its place", () => {
    const html = renderToStaticMarkup(<RunVersionEvidence value={mapRunVersions({ runFinishVersionSnapshotStatus: "NOT_FINISHED" })} />);
    expect(html).toContain("종료 승인 시점");
    expect(html).toContain("미종료");
    expect(html).not.toContain("<button");
  });
});
