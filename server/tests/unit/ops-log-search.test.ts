import { describe, expect, it } from "vitest";
import { opsLogSearch } from "../../apps/admin-api/src/ops-log-search";
describe("ops log category and search", () => {
  it("never exposes server events on the unconnected frontend category", () => {
    expect(opsLogSearch("frontend", "").clauses).toEqual(["1 = 0"]);
    expect(opsLogSearch("server", "").clauses).toEqual([]);
    expect(() => opsLogSearch("other", "")).toThrow();
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
