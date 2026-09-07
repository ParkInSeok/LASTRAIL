import { describe, expect, it } from "vitest";
import {
  parseServerDeployPlan,
  validateServerDeployPlanForDeployment,
} from "@lastrail/common";

const basePlan = {
  releaseId: "act1-launch-2026-09-06",
  environment: "prd",
  services: ["game-api", "admin-api"],
  serverVersion: "2026.09.06.1",
  source: {
    commitSha: "38d7822abcdef1234567890abcdef123456789",
    releaseBranch: "release/2026-09-06-2026.09.06.1",
    workingTreeClean: true,
  },
  deploymentTags: [
    { service: "game-api", tag: "game-api-2026.09.06.1" },
    { service: "admin-api", tag: "admin-api-2026.09.06.1" },
  ],
  database: {
    compatibility: "MIN_VERSION_GATE_REQUIRED",
    minimumVersionCode: "101",
    migrationRange: "0001-0020",
  },
  api: {
    compatibility: "ROLLBACK_COMPATIBLE",
    rollbackVerified: true,
  },
  contentRelease: {
    releaseId: "act1-content-2026-09-06",
    contentVersion: "act1.2026-09-06.1",
  },
  verification: {
    localRegression: "PASSED",
    dryRun: "PASSED",
    remoteSmoke: "PENDING",
  },
};

describe("server release deploy policy", () => {
  it("accepts a release plan with explicit server version, release branch, and per-service immutable tags", () => {
    expect(parseServerDeployPlan(basePlan)).toEqual(basePlan);
    expect(validateServerDeployPlanForDeployment(basePlan, [])).toMatchObject({
      accepted: true,
      plan: basePlan,
    });
  });

  it("rejects deploy plans without a unique calendar serverVersion", () => {
    for (const serverVersion of ["", "1.0.0", "2026.9.6.1", "2026.09.06.0", "2026.02.31.1"]) {
      expect(validateServerDeployPlanForDeployment({ ...basePlan, serverVersion }, [])).toEqual({
        accepted: false,
        errorCode: "server_version_invalid",
      });
    }
    expect(validateServerDeployPlanForDeployment(basePlan, [
      { environment: "prd", service: "game-api", serverVersion: "2026.09.06.1" },
    ])).toEqual({
      accepted: false,
      errorCode: "server_version_reused",
    });
  });

  it("rejects branch and tag plans that do not preserve the exact deploy source", () => {
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      source: { ...basePlan.source, releaseBranch: "release/2026-09-06-act1" },
    }, [])).toEqual({
      accepted: false,
      errorCode: "release_branch_missing_server_version",
    });
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      deploymentTags: [{ service: "game-api", tag: "game-api-2026.09.06.1" }],
    }, [])).toEqual({
      accepted: false,
      errorCode: "deployment_tag_missing",
    });
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      deploymentTags: [
        { service: "game-api", tag: "admin-api-2026.09.06.1" },
        { service: "admin-api", tag: "admin-api-2026.09.06.1" },
      ],
    }, [])).toEqual({
      accepted: false,
      errorCode: "deployment_tag_invalid",
    });
  });

  it("requires minimum app version evidence for DB compatibility-breaking releases", () => {
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      database: { compatibility: "MIN_VERSION_GATE_REQUIRED", migrationRange: "0001-0020" },
    }, [])).toEqual({
      accepted: false,
      errorCode: "minimum_version_required_for_db_change",
    });
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      database: { compatibility: "ADDITIVE_COMPATIBLE", migrationRange: "0001-0020" },
    }, [])).toMatchObject({ accepted: true });
  });

  it("requires API-only changes to be rollback verified unless they are protected by minimum app version", () => {
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      api: { compatibility: "ROLLBACK_COMPATIBLE", rollbackVerified: false },
    }, [])).toEqual({
      accepted: false,
      errorCode: "api_rollback_verification_required",
    });
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      api: { compatibility: "MIN_VERSION_GATE_REQUIRED" },
    }, [])).toEqual({
      accepted: false,
      errorCode: "minimum_version_required_for_api_change",
    });
    expect(validateServerDeployPlanForDeployment({
      ...basePlan,
      api: { compatibility: "MIN_VERSION_GATE_REQUIRED", minimumVersionCode: "101" },
    }, [])).toMatchObject({ accepted: true });
  });

  it("does not preserve secret-like or rollback-action fields from metadata input", () => {
    const parsed = parseServerDeployPlan({
      ...basePlan,
      secret: "do-not-store",
      authToken: "do-not-store",
      rollbackDb: true,
      deploymentTags: [
        { service: "game-api", tag: "game-api-2026.09.06.1", secret: "x" },
        { service: "admin-api", tag: "admin-api-2026.09.06.1", rollbackDb: true },
      ],
    });
    expect(parsed).toEqual(basePlan);
    expect(JSON.stringify(parsed)).not.toContain("secret");
    expect(JSON.stringify(parsed)).not.toContain("authToken");
    expect(JSON.stringify(parsed)).not.toContain("rollbackDb");
  });
});
