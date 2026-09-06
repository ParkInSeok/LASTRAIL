import { describe, expect, it, vi } from "vitest";
import app from "../../apps/admin-api/src/index";

const contentRelease = {
  status: "APPROVED",
  releaseId: "content_act1_001",
  contentVersion: "act1.2026-09-06.001",
  catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
  catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  minimumVersionCode: "90",
  maximumVersionCode: "100",
};

function createEnv(initialVersion: unknown): Env & { VERSION_BUCKET: { __readStored(): unknown } } {
  let stored = JSON.stringify(initialVersion);
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
      get: vi.fn(async () => ({
        json: async () => JSON.parse(stored) as unknown,
      })),
      put: vi.fn(async (_key: string, value: string) => {
        stored = value;
      }),
      __readStored: () => JSON.parse(stored) as unknown,
    },
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("admin_principals");
        return {
          bind(accessSubject: string) {
            expect(accessSubject).toBe("access|owner-1");
            return {
              first: async () => ({
                id: "principal-owner",
                access_subject: "access|owner-1",
                role: "OWNER",
                status: "ACTIVE",
                display_name: "Owner",
              }),
            };
          },
        };
      },
    } as unknown as D1Database,
  } as unknown as Env & { VERSION_BUCKET: { __readStored(): unknown } };
}

describe("admin-api version content release safety", () => {
  it("returns sanitized content release status with the current version policy", async () => {
    const env = createEnv({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
    });

    const response = await app.request(
      "/v1/admin/versions",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
      admissionPolicy: {
        status: "CONFIGURED",
        gameApiEnforcement: "CLIENT_CONFIG_AND_PROTECTED_ROUTES",
      },
    });
  });

  it("preserves an existing approved content release when legacy minimum-version export omits it", async () => {
    const env = createEnv({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
    });

    const response = await app.request(
      "/v1/admin/versions/export",
      {
        method: "POST",
        headers: { "Cf-Access-Subject": "access|owner-1", "Content-Type": "application/json" },
        body: JSON.stringify({ version: "1.0.1", versionCode: "101", minimumVersionCode: "90" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      version: "1.0.1",
      versionCode: "101",
      minimumVersionCode: "90",
      contentRelease,
    });
    expect(env.VERSION_BUCKET.__readStored()).toMatchObject({
      version: "1.0.1",
      versionCode: "101",
      minimumVersionCode: "90",
      contentRelease,
    });
  });
});
