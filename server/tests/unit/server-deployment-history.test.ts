import { describe, expect, it } from "vitest";
import {
  buildServerDeploymentEvidence,
  buildServerDeployPreflight,
  saveServerDeploymentEvidence,
  type ServerDeploymentHistoryBucket,
} from "@lastrail/common";

const plan = {
  releaseId: "act1-launch-2026-09-06",
  environment: "prd",
  services: ["game-api", "admin-api"],
  serverVersion: "2026.09.06.1",
  source: {
    commitSha: "30b2933abcdef1234567890abcdef123456789",
    releaseBranch: "release/2026-09-06-2026.09.06.1",
    workingTreeClean: true,
  },
  deploymentTags: [
    { service: "game-api", tag: "game-api-2026.09.06.1" },
    { service: "admin-api", tag: "admin-api-2026.09.06.1" },
  ],
  database: {
    compatibility: "ADDITIVE_COMPATIBLE",
    migrationRange: "0001-0020",
  },
  verification: {
    localRegression: "PASSED",
    dryRun: "PASSED",
    remoteSmoke: "PENDING",
  },
};

function createFakeBucket(initial?: Record<string, unknown>) {
  const objects = new Map<string, { value: unknown; etag: string }>();
  let nextEtag = 1;
  for (const [key, value] of Object.entries(initial ?? {})) {
    objects.set(key, { value, etag: `etag-${nextEtag++}` });
  }

  const bucket: ServerDeploymentHistoryBucket & { snapshot(): Record<string, unknown>; etag(key: string): string | null } = {
    async get(key) {
      const object = objects.get(key);
      if (!object) return null;
      return {
        etag: object.etag,
        httpEtag: `"${object.etag}"`,
        json: async () => object.value,
      };
    },
    async put(key, value, options) {
      const current = objects.get(key);
      const ifNoneMatch = options.onlyIf instanceof Headers ? options.onlyIf.get("If-None-Match") : null;
      const ifMatch = options.onlyIf instanceof Headers ? options.onlyIf.get("If-Match") : null;
      if (ifNoneMatch === "*" && current) return null;
      if (ifMatch !== null && current?.etag !== ifMatch.replaceAll("\"", "")) return null;
      const parsed = JSON.parse(value) as unknown;
      const etag = `etag-${nextEtag++}`;
      objects.set(key, { value: parsed, etag });
      return { etag, httpEtag: `"${etag}"` };
    },
    snapshot() {
      return Object.fromEntries([...objects.entries()].map(([key, object]) => [key, object.value]));
    },
    etag(key) {
      return objects.get(key)?.etag ?? null;
    },
  };
  return bucket;
}

describe("server deployment history store", () => {
  it("writes immutable deployment history and a current pointer with sanitized evidence", async () => {
    const preflight = buildServerDeployPreflight(plan, [], { environment: "prd", service: "game-api" });
    expect(preflight).toMatchObject({ accepted: true });
    if (!preflight.accepted) return;

    const evidence = buildServerDeploymentEvidence(preflight.plan, "game-api", {
      cloudflareDeploymentId: "cf-deploy-game-20260906-1",
      previousDeploymentId: "cf-deploy-game-previous",
      createdAt: "2026-09-06T00:00:00.000Z",
    });
    const bucket = createFakeBucket();
    const result = await saveServerDeploymentEvidence(bucket, evidence);

    expect(result).toMatchObject({
      accepted: true,
      historyKey: "server-deployments/prd/game-api/2026.09.06.1.json",
      currentKey: "server-deployments/prd/game-api/current.json",
      currentPointerStatus: "CURRENT",
      idempotentReplay: false,
    });
    expect(bucket.snapshot()).toMatchObject({
      "server-deployments/prd/game-api/2026.09.06.1.json": {
        releaseId: "act1-launch-2026-09-06",
        environment: "prd",
        service: "game-api",
        serverVersion: "2026.09.06.1",
        commitSha: "30b2933abcdef1234567890abcdef123456789",
        releaseBranch: "release/2026-09-06-2026.09.06.1",
        deploymentTag: "game-api-2026.09.06.1",
        cloudflareDeploymentId: "cf-deploy-game-20260906-1",
        previousDeploymentId: "cf-deploy-game-previous",
        database: { compatibility: "ADDITIVE_COMPATIBLE", migrationRange: "0001-0020" },
        verification: { localRegression: "PASSED", dryRun: "PASSED", remoteSmoke: "PENDING" },
        pointerStatus: "CURRENT",
      },
      "server-deployments/prd/game-api/current.json": {
        environment: "prd",
        service: "game-api",
        serverVersion: "2026.09.06.1",
        historyKey: "server-deployments/prd/game-api/2026.09.06.1.json",
        cloudflareDeploymentId: "cf-deploy-game-20260906-1",
        updatedAt: "2026-09-06T00:00:00.000Z",
      },
    });
    expect(JSON.stringify(bucket.snapshot())).not.toContain("secret");
    expect(JSON.stringify(bucket.snapshot())).not.toContain("Authorization");
    expect(JSON.stringify(bucket.snapshot())).not.toContain("rawRng");
  });

  it("treats the same immutable history evidence as an idempotent replay", async () => {
    const preflight = buildServerDeployPreflight(plan, [], { environment: "prd", service: "game-api" });
    if (!preflight.accepted) throw new Error("preflight rejected");
    const evidence = buildServerDeploymentEvidence(preflight.plan, "game-api", {
      cloudflareDeploymentId: "cf-deploy-game-20260906-1",
      createdAt: "2026-09-06T00:00:00.000Z",
    });
    const bucket = createFakeBucket();

    await saveServerDeploymentEvidence(bucket, evidence);
    const replay = await saveServerDeploymentEvidence(bucket, evidence, {
      expectedCurrentEtag: bucket.etag("server-deployments/prd/game-api/current.json"),
    });

    expect(replay).toMatchObject({
      accepted: true,
      idempotentReplay: true,
      currentPointerStatus: "CURRENT",
    });
  });

  it("rejects attempts to overwrite an immutable history record with different evidence", async () => {
    const preflight = buildServerDeployPreflight(plan, [], { environment: "prd", service: "game-api" });
    if (!preflight.accepted) throw new Error("preflight rejected");
    const bucket = createFakeBucket();
    const evidence = buildServerDeploymentEvidence(preflight.plan, "game-api", {
      cloudflareDeploymentId: "cf-deploy-game-20260906-1",
      createdAt: "2026-09-06T00:00:00.000Z",
    });
    await saveServerDeploymentEvidence(bucket, evidence);

    const conflict = await saveServerDeploymentEvidence(bucket, {
      ...evidence,
      cloudflareDeploymentId: "cf-deploy-game-20260906-different",
    });

    expect(conflict).toEqual({
      accepted: false,
      errorCode: "deployment_history_immutable_conflict",
      historyKey: "server-deployments/prd/game-api/2026.09.06.1.json",
      currentPointerStatus: "DEGRADED",
    });
  });

  it("fails the current pointer update on stale CAS without changing the immutable history result", async () => {
    const preflight = buildServerDeployPreflight(plan, [], { environment: "prd", service: "game-api" });
    if (!preflight.accepted) throw new Error("preflight rejected");
    const bucket = createFakeBucket();
    const firstEvidence = buildServerDeploymentEvidence(preflight.plan, "game-api", {
      cloudflareDeploymentId: "cf-deploy-game-20260906-1",
      createdAt: "2026-09-06T00:00:00.000Z",
    });
    await saveServerDeploymentEvidence(bucket, firstEvidence);
    const staleEtag = bucket.etag("server-deployments/prd/game-api/current.json");

    const secondPlan = {
      ...plan,
      releaseId: "act1-hotfix-2026-09-06",
      serverVersion: "2026.09.06.2",
      source: {
        ...plan.source,
        releaseBranch: "release/2026-09-06-2026.09.06.2",
      },
      deploymentTags: [
        { service: "game-api", tag: "game-api-2026.09.06.2" },
        { service: "admin-api", tag: "admin-api-2026.09.06.2" },
      ],
    };
    const secondPreflight = buildServerDeployPreflight(secondPlan, [], { environment: "prd", service: "game-api" });
    if (!secondPreflight.accepted) throw new Error("second preflight rejected");
    await saveServerDeploymentEvidence(bucket, buildServerDeploymentEvidence(secondPreflight.plan, "game-api", {
      cloudflareDeploymentId: "cf-deploy-game-20260906-2",
      createdAt: "2026-09-06T01:00:00.000Z",
    }));

    const thirdPlan = {
      ...secondPlan,
      releaseId: "act1-hotfix-2026-09-06-b",
      serverVersion: "2026.09.06.3",
      source: {
        ...plan.source,
        releaseBranch: "release/2026-09-06-2026.09.06.3",
      },
      deploymentTags: [
        { service: "game-api", tag: "game-api-2026.09.06.3" },
        { service: "admin-api", tag: "admin-api-2026.09.06.3" },
      ],
    };
    const thirdPreflight = buildServerDeployPreflight(thirdPlan, [], { environment: "prd", service: "game-api" });
    if (!thirdPreflight.accepted) throw new Error("third preflight rejected");
    const staleResult = await saveServerDeploymentEvidence(
      bucket,
      buildServerDeploymentEvidence(thirdPreflight.plan, "game-api", {
        cloudflareDeploymentId: "cf-deploy-game-20260906-3",
        createdAt: "2026-09-06T02:00:00.000Z",
      }),
      { expectedCurrentEtag: staleEtag },
    );

    expect(staleResult).toEqual({
      accepted: false,
      errorCode: "deployment_current_pointer_conflict",
      historyKey: "server-deployments/prd/game-api/2026.09.06.3.json",
      currentKey: "server-deployments/prd/game-api/current.json",
      currentPointerStatus: "DEGRADED",
    });
  });

  it("rejects preflight when the requested service or environment does not match the deploy target", () => {
    expect(buildServerDeployPreflight(plan, [], { environment: "dev", service: "game-api" })).toEqual({
      accepted: false,
      errorCode: "deployment_target_mismatch",
    });
    expect(buildServerDeployPreflight(plan, [], { environment: "prd", service: "async-worker" })).toEqual({
      accepted: false,
      errorCode: "deployment_target_mismatch",
    });
  });
});
