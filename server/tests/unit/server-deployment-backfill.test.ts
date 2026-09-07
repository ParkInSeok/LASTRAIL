import { describe, expect, it, vi } from "vitest";
import { backfillServerDeploymentEvidence } from "../../scripts/cloudflare/server-deployment-backfill";

const target = { environment: "dev", service: "game-api", serverVersion: "2026.09.07.1", commitSha: "57d2a109134f99d2feb577cd4db23aa3dd7e08fd", cloudflareDeploymentId: "d465732a-7513-44d9-8644-a73531bbb705" } as const;
const evidence = {
  ...target, releaseId: "local-fixture", releaseBranch: "release/2026-09-07-dev-2026.09.07.1", deploymentTag: "game-api-2026.09.07.1",
  previousDeploymentId: null, database: { compatibility: "ADDITIVE_COMPATIBLE", migrationRange: "0001-0020" },
  contentRelease: { releaseId: "fake-content", contentVersion: "fake-v1" },
  verification: { localRegression: "PASSED", dryRun: "PASSED", remoteSmoke: "PASSED" },
  pointerStatus: "CURRENT", createdAt: "2026-09-07T01:00:00.000Z",
};
function fakeBucket() {
  const objects = new Map<string, string>();
  return {
    get: vi.fn(async (key: string) => objects.has(key) ? { httpEtag: '"fixed"', json: async () => JSON.parse(objects.get(key)!) } : null),
    put: vi.fn(async (key: string, value: string, options: { onlyIf?: Headers }) => {
      if (objects.has(key) && options.onlyIf?.get("If-None-Match") === "*") return null;
      objects.set(key, value); return { httpEtag: '"fixed"' };
    }),
  };
}
describe("local no-deploy evidence adapter (fake bucket only)", () => {
  it("backfills a recorded deployment without any deploy/smoke executor", async () => {
    const bucket = fakeBucket();
    const result = await backfillServerDeploymentEvidence(bucket, evidence, target, null);
    expect(result.accepted).toBe(true);
    expect(bucket.put.mock.calls.map(([key]) => key)).toEqual([
      "server-deployments/dev/game-api/2026.09.07.1.json", "server-deployments/dev/game-api/current.json",
    ]);
  });
  it.each(["environment", "service", "serverVersion", "commitSha", "cloudflareDeploymentId"])("rejects mismatched %s before store access", async (field) => {
    const bucket = fakeBucket();
    await expect(backfillServerDeploymentEvidence(bucket, evidence, { ...target, [field]: "mismatch" }, null)).rejects.toThrow("deployment_target_mismatch");
    expect(bucket.get).not.toHaveBeenCalled(); expect(bucket.put).not.toHaveBeenCalled();
  });
  it.each(["PENDING", "BLOCKED", "FAILED"])("does not upgrade %s remote smoke to PASS", async (remoteSmoke) => {
    const bucket = fakeBucket();
    await expect(backfillServerDeploymentEvidence(bucket, { ...evidence, verification: { ...evidence.verification, remoteSmoke } }, target, null)).rejects.toThrow("deployment_backfill_verification_incomplete");
    expect(bucket.put).not.toHaveBeenCalled();
  });
});
