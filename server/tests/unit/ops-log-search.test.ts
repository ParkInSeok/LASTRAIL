import { describe, expect, it } from "vitest";
import { opsLogSearch } from "../../apps/admin-api/src/ops-log-search";
describe("ops log category and search", () => {
  it("filters frontend category to safe client diagnostics only", () => {
    expect(opsLogSearch("frontend", "").clauses).toEqual([
      "json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.clientDiagnosticCategory') = 'frontend'",
    ]);
    expect(opsLogSearch("server", "").clauses).toEqual([
      "COALESCE(json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.clientDiagnosticCategory'), '') != 'frontend'",
    ]);
    expect(() => opsLogSearch("other", "")).toThrow();
  });

  it("filters frontend app version metadata before pagination", () => {
    const search = opsLogSearch("frontend", "", undefined, "1.0.0-dev", "42");
    expect(search.clauses.join("\n")).toContain("$.clientDiagnosticCategory");
    expect(search.clauses.join("\n")).toContain("$.appVersion");
    expect(search.clauses.join("\n")).toContain("$.appVersionCode");
    expect(search.values).toEqual(["1.0.0-dev", "42"]);
    expect(() => opsLogSearch("frontend", "", undefined, "x".repeat(65), "42")).toThrow();
    expect(() => opsLogSearch("frontend", "", undefined, "1.0.0-dev", "not-number")).toThrow();
  });

  it("binds literal search instead of interpreting wildcard or SQL input", () => {
    const value = "%' OR 1=1 --";
    const search = opsLogSearch("server", value);
    expect(search.clauses.join()).not.toContain(value);
    expect(search.values.every((item) => item === value)).toBe(true);
    expect(search.clauses.join()).toContain("message_summary");
    expect(search.clauses.join()).toContain("request_id");
    expect(() => opsLogSearch("server", "a".repeat(201))).toThrow();
  });
});
