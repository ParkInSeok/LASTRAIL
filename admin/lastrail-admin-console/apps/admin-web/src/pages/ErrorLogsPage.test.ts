import { describe, expect, it } from "vitest";
import { buildLogFilters, emptyLogFilters, logTime } from "./ErrorLogsPage";
import { mapOpsErrorLogListResponse } from "../shared/opsErrorLogs/opsErrorLogContract";

describe("Error log browser", () => {
  it("separates client version filters from server deployment identity", () => {
    const filters = { ...emptyLogFilters, appVersion: "0.1.0", appVersionCode: "1", serverVersion: "deployment-1" };
    expect(buildLogFilters("frontend", filters)).toMatchObject({ appVersion: "0.1.0", appVersionCode: "1" });
    expect(buildLogFilters("frontend", filters)).not.toHaveProperty("serverVersion");
    expect(buildLogFilters("server", filters)).not.toHaveProperty("appVersion");
    const data = mapOpsErrorLogListResponse({ items: [{ id: "client-1", appVersion: "0.1.0", appVersionCode: 1 }, { id: "legacy" }] });
    expect(data.items[0]).toMatchObject({ appVersion: "0.1.0", appVersionCode: "1" });
    expect(data.items[1]).toMatchObject({ appVersion: null, appVersionCode: null });
  });
  it("sends deployment version and preserves missing historical versions", () => {
    expect(buildLogFilters("server", { ...emptyLogFilters, serverVersion: " deploy-1 " }).serverVersion).toBe("deploy-1");
    const data = mapOpsErrorLogListResponse({ items: [{ id: "log-1", serverVersion: "deploy-1" }, { id: "old-log" }] });
    expect(data.items.map((row) => row.serverVersion)).toEqual(["deploy-1", null]);
  });
  it("includes category and all applied filters before server pagination", () => {
    const result = buildLogFilters("server", { ...emptyLogFilters, q: " request-1 ", severity: "error", from: "2026-09-06T10:00" }, "25");
    expect(result).toMatchObject({ category: "server", q: "request-1", severity: "error", cursor: "25", limit: "25" });
    expect(result).not.toHaveProperty("environment");
    expect(result.occurredFrom).toBe(new Date("2026-09-06T10:00").toISOString());
    expect(buildLogFilters("frontend", emptyLogFilters)).toMatchObject({ category: "frontend", cursor: "", service: "", q: "" });
  });
  it("distinguishes frontend collection unavailable from successful empty server logs", () => {
    expect(mapOpsErrorLogListResponse({ items: [], collectionAvailable: false }).collectionAvailable).toBe(false);
    expect(mapOpsErrorLogListResponse({ items: [] }).collectionAvailable).toBe(true);
  });
  it("formats timestamp values in KST while preserving unknown values", () => {
    expect(logTime("2026-09-06T00:00:00Z")).toContain("09:00:00");
    expect(logTime("없음")).toBe("없음");
  });
});
