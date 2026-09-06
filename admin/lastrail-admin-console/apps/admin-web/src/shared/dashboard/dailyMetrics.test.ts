import { describe, expect, it } from "vitest";
import { mapDailyMetrics, dailySeries } from "./dailyMetrics";

describe("daily source metrics", () => {
  it("does not promote legacy dau aliases or unavailable revenue to values", () => {
    const data = mapDailyMetrics({ sourceAvailability: { sessionCreatedAccounts: true, signups: true, adRewards: true, adRevenue: false }, points: [{ date: "2026-09-06", dau: 99, signups: 0, adRevenue: 0 }] });
    expect(data?.points[0].sessionCreatedAccounts).toBeNull();
    expect(data?.points[0].signups).toBe(0);
    expect(data?.points[0].adRevenue).toBeNull();
    expect(dailySeries(data, "signups", 7, "2026-09-06T12:00:00Z")).toEqual([{ date: "2026-09-06", value: 0 }]);
  });
  it("filters KST days without inventing points for missing dates", () => {
    const data = mapDailyMetrics({ sourceAvailability: { signups: true }, points: [{ date: "2026-08-31", signups: 2 }, { date: "2026-09-07", signups: 3 }] });
    expect(dailySeries(data, "signups", 7, "2026-09-06T15:01:00Z")).toEqual([{ date: "2026-09-07", value: 3 }]);
    expect(dailySeries(data, "signups", 30, "2026-09-06T15:01:00Z")).toHaveLength(2);
    expect(mapDailyMetrics(undefined)).toBeNull();
  });
});
