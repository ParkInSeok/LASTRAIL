import { describe, expect, it } from "vitest";
import { buildOpsErrorLogInsertEnvelope } from "../../packages/common/src/ops-error-log";
import { opsLogSearch } from "../../apps/admin-api/src/ops-log-search";
describe("server version provenance", () => {
  const base = { id: "log-1", occurredAtMs: 1, environment: "DEV" as const, service: "game_api" as const, source: "game_api" as const, severity: "error" as const, errorCode: "test_error", messageSummary: "test" };
  it("keeps trusted deployment identity instead of context identity", async () => {
    const result = await buildOpsErrorLogInsertEnvelope({ ...base, serverVersion: "deploy-1", context: { serverVersion: "client-value", route: "/test" } });
    expect(JSON.parse(result.sanitizedContextJson)).toEqual({ route: "/test", serverVersion: "deploy-1" });
    const legacy = await buildOpsErrorLogInsertEnvelope(base);
    expect(JSON.parse(legacy.sanitizedContextJson).serverVersion).toBeNull();
  });
  it("filters the stored runtime version with exact bound matching", () => {
    const search = opsLogSearch("server", "", "deploy-1");
    expect(search.values).toEqual(["deploy-1"]);
    expect(search.clauses.join()).toContain("$.serverVersion");
    expect(search.clauses.join()).not.toContain("deploy-1");
    expect(() => opsLogSearch("server", "", "' OR 1=1")).toThrow();
  });
});
