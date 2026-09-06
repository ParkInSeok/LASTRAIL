// Ephemeral read-only bucket for localhost Hono verification, never remote R2.
export function deploymentBucket(policy: unknown) {
  const objects: Record<string, unknown> = { "versions/current.json": policy };
  for (const service of ["game-api", "admin-api"]) {
    const evidence = {
      releaseId: "local-release", environment: "dev", service, serverVersion: "2026.09.06.1",
      commitSha: "30b2933abcdef1234567890abcdef123456789",
      releaseBranch: "release/local", deploymentTag: `${service}-local-1`,
      cloudflareDeploymentId: `local-${service}`, previousDeploymentId: null,
      database: { compatibility: "ADDITIVE_COMPATIBLE", migrationRange: "0001-0020" }, contentRelease: null,
      verification: { localRegression: "PASSED", dryRun: "PASSED", remoteSmoke: "PASSED" },
      pointerStatus: service === "admin-api" ? "DEGRADED" : "CURRENT", createdAt: "2026-09-06T00:00:00.000Z"
    };
    objects[`server-deployments/dev/${service}/2026.09.06.1.json`] = evidence;
    objects[`server-deployments/dev/${service}/current.json`] = {
      environment: "dev", service, serverVersion: "2026.09.06.1", historyKey: `server-deployments/dev/${service}/2026.09.06.1.json`,
      releaseId: evidence.releaseId, cloudflareDeploymentId: evidence.cloudflareDeploymentId, updatedAt: evidence.createdAt
    };
    objects[`server-deployments/dev/${service}/invalid.json`] = {};
  }
  return {
    get: async (key: string) => key in objects ? { etag: "local-only", json: async () => objects[key] } : null,
    put: async () => { throw new Error("Local deployment bucket is read-only"); }
  };
}
