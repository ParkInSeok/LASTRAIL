import { describe, expect, it } from "vitest";
import { buildLogFilters, emptyLogFilters, logTime } from "./ErrorLogsPage";
import { mapOpsErrorLogListResponse } from "../shared/opsErrorLogs/opsErrorLogContract";

describe("Error log browser", () => {
  it("includes category and all applied filters before server pagination", () => {
    const result = buildLogFilters("server", { ...emptyLogFilters, q: " request-1 ", severity: "error", environment: "DEV", from: "2026-09-06T10:00" }, "25");
    expect(result).toMatchObject({ category: "server", q: "request-1", severity: "error", environment: "DEV", cursor: "25", limit: "25" });
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
