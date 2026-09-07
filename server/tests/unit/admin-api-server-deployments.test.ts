import { describe, expect, it, vi } from "vitest";
import app from "../../apps/admin-api/src/index";

const currentPointer = {
  environment: "dev",
  service: "game-api",
  serverVersion: "2026.09.06.1",
  historyKey: "server-deployments/dev/game-api/2026.09.06.1.json",
  releaseId: "act1-launch-2026-09-06",
  cloudflareDeploymentId: "cf-deploy-game-20260906-1",
  updatedAt: "2026-09-06T00:00:00.000Z",
};

const history = {
  releaseId: "act1-launch-2026-09-06",
  environment: "dev",
  service: "game-api",
  serverVersion: "2026.09.06.1",
  commitSha: "30b2933abcdef1234567890abcdef123456789",
  releaseBranch: "release/2026-09-06-2026.09.06.1",
  deploymentTag: "game-api-2026.09.06.1",
  cloudflareDeploymentId: "cf-deploy-game-20260906-1",
  previousDeploymentId: "cf-deploy-game-previous",
  database: {
    compatibility: "ADDITIVE_COMPATIBLE",
    migrationRange: "0001-0020",
  },
  api: {
    compatibility: "ROLLBACK_COMPATIBLE",
    rollbackVerified: true,
  },
  contentRelease: null,
  verification: {
    localRegression: "PASSED",
    dryRun: "PASSED",
    remoteSmoke: "PASSED",
  },
  pointerStatus: "CURRENT",
  createdAt: "2026-09-06T00:00:00.000Z",
};

function createEnv(objects: Record<string, unknown>, role: "OWNER" | "OPERATOR" | "VIEWER" = "VIEWER"): Env {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    VERSION_BUCKET: {
      get: vi.fn(async (key: string) => {
        if (!(key in objects)) return null;
        return {
          etag: "etag-current",
          json: async () => objects[key],
        };
      }),
      put: vi.fn(),
    },
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("admin_principals");
        return {
          bind(accessSubject: string) {
            expect(accessSubject).toBe("access|admin-1");
            return {
              first: async () => ({
                id: "principal-admin",
                access_subject: "access|admin-1",
                role,
                status: "ACTIVE",
                display_name: "Admin",
              }),
            };
          },
        };
      },
    } as unknown as D1Database,
  } as unknown as Env;
}

describe("admin-api server deployment read model", () => {
  it("returns the current server deployment pointer for the authenticated admin environment", async () => {
    const response = await app.request(
      "/v1/admin/server-deployments/current?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({ "server-deployments/dev/game-api/current.json": currentPointer }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      environment: "DEV",
      service: "game-api",
      current: currentPointer,
      permission: "SERVER_DEPLOYMENT_READ",
      permissionMode: "ROLE_PERMISSION",
      errorCode: null,
    });
  });

  it("returns immutable deployment history without exposing secrets or rollback actions", async () => {
    const response = await app.request(
      "/v1/admin/server-deployments/history/2026.09.06.1?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({ "server-deployments/dev/game-api/2026.09.06.1.json": history }),
    );

    expect(response.status).toBe(200);
    const body = await response.json() as unknown;
    expect(body).toMatchObject({
      accepted: true,
      environment: "DEV",
      service: "game-api",
      history,
      permission: "SERVER_DEPLOYMENT_READ",
      permissionMode: "ROLE_PERMISSION",
      errorCode: null,
    });
    expect(JSON.stringify(body)).not.toContain("secret");
    expect(JSON.stringify(body)).not.toContain("rollbackDb");
    expect(JSON.stringify(body)).not.toContain("Authorization");
  });

  it("rejects mismatched environment, invalid service, body, and missing objects with stable codes", async () => {
    await expect(app.request(
      "/v1/admin/server-deployments/current?environment=PRD&service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({ "server-deployments/dev/game-api/current.json": currentPointer }),
    ).then(async (response) => ({ status: response.status, body: await response.json() }))).resolves.toMatchObject({
      status: 400,
      body: { error: { code: "deployment_target_mismatch" } },
    });

    await expect(app.request(
      "/v1/admin/server-deployments/current?service=bad",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({}),
    ).then(async (response) => ({ status: response.status, body: await response.json() }))).resolves.toMatchObject({
      status: 400,
      body: { error: { code: "server_deployment_query_invalid" } },
    });

    await expect(app.request(
      "/v1/admin/server-deployments/current?service=game-api",
      {
        method: "GET",
        headers: { "Cf-Access-Subject": "access|admin-1", "Content-Length": "2" },
      },
      createEnv({}),
    ).then(async (response) => ({ status: response.status, body: await response.json() }))).resolves.toMatchObject({
      status: 400,
      body: { error: "contract_violation" },
    });

    await expect(app.request(
      "/v1/admin/server-deployments/current?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({}),
    ).then(async (response) => ({ status: response.status, body: await response.json() }))).resolves.toMatchObject({
      status: 404,
      body: { accepted: false, current: null, errorCode: "server_deployment_current_not_found" },
    });
  });

  it("rejects stored history whose target fields do not match the requested key", async () => {
    const serviceMismatch = await app.request(
      "/v1/admin/server-deployments/history/2026.09.06.1?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({
        "server-deployments/dev/game-api/2026.09.06.1.json": {
          ...history,
          service: "admin-api",
          deploymentTag: "admin-api-2026.09.06.1",
        },
      }),
    );

    expect(serviceMismatch.status).toBe(502);
    await expect(serviceMismatch.json()).resolves.toEqual({
      error: { code: "server_deployment_history_target_mismatch" },
    });

    const versionMismatch = await app.request(
      "/v1/admin/server-deployments/history/2026.09.06.1?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({
        "server-deployments/dev/game-api/2026.09.06.1.json": {
          ...history,
          serverVersion: "2026.09.06.2",
          deploymentTag: "game-api-2026.09.06.2",
        },
      }),
    );

    expect(versionMismatch.status).toBe(502);
    await expect(versionMismatch.json()).resolves.toEqual({
      error: { code: "server_deployment_history_target_mismatch" },
    });

    const envMismatch = await app.request(
      "/v1/admin/server-deployments/history/2026.09.06.1?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({
        "server-deployments/dev/game-api/2026.09.06.1.json": {
          ...history,
          environment: "prd",
        },
      }),
    );

    expect(envMismatch.status).toBe(502);
    await expect(envMismatch.json()).resolves.toEqual({
      error: { code: "server_deployment_history_target_mismatch" },
    });
  });

  it("rejects stored current pointers whose target fields do not match the requested key", async () => {
    const response = await app.request(
      "/v1/admin/server-deployments/current?service=game-api",
      { headers: { "Cf-Access-Subject": "access|admin-1" } },
      createEnv({
        "server-deployments/dev/game-api/current.json": {
          ...currentPointer,
          service: "admin-api",
        },
      }),
    );

    expect(response.status).toBe(502);
    await expect(response.json()).resolves.toEqual({
      error: { code: "server_deployment_current_target_mismatch" },
    });
  });
});
