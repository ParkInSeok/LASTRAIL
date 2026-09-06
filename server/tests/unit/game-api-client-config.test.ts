import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";

describe("game-api client config", () => {
  const baseEnv = {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
  } as const;

  function createVersionBucket(value: unknown) {
    return {
      get: async (key: string) => {
        expect(key).toBe("versions/current.json");
        return {
          json: async () => value,
        };
      },
    };
  }

  it("fails closed when version policy storage is unavailable", async () => {
    const response = await app.request("/v1/client-config", {}, {
      ...baseEnv,
    });

    expect(response.status).toBe(200);

    const body = await response.json();
    expect(body).toMatchObject({
      accepted: false,
      clientAdmission: {
        status: "VERSION_POLICY_UNCONFIGURED",
        installedVersionCode: null,
        minimumVersionCode: null,
        updateRequired: true,
      },
      contentRelease: {
        status: "UNCONFIGURED",
        releaseId: null,
        catalogUrl: null,
      },
      errorCode: "version_policy_unconfigured",
    });
    expect(JSON.stringify(body)).not.toContain("admin");
  });

  it("requires the installed version code before accepting a configured app", async () => {
    const response = await app.request("/v1/client-config", {}, {
      ...baseEnv,
      VERSION_BUCKET: createVersionBucket({
        version: "1.0.0",
        versionCode: "100",
        minimumVersionCode: "90",
        contentRelease: {
          status: "APPROVED",
          releaseId: "content_act1_001",
          contentVersion: "act1.2026-09-06.001",
          catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
          catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          minimumVersionCode: "90",
          maximumVersionCode: "100",
        },
      }),
    } as never);

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      clientAdmission: {
        status: "APP_VERSION_REQUIRED",
        installedVersionCode: null,
        minimumVersionCode: "90",
        updateRequired: true,
      },
      errorCode: "app_version_required",
    });
  });

  it("requires an update when installed version code is below server minimum", async () => {
    const response = await app.request("/v1/client-config", {
      headers: { "X-LastRail-App-Version-Code": "89", "X-LastRail-App-Platform": "android", "X-LastRail-App-Channel": "internal" },
    }, {
      ...baseEnv,
      VERSION_BUCKET: createVersionBucket({
        version: "1.0.0",
        versionCode: "100",
        minimumVersionCode: "90",
        contentRelease: {
          status: "APPROVED",
          releaseId: "content_act1_001",
          contentVersion: "act1.2026-09-06.001",
          catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
          catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          minimumVersionCode: "90",
          maximumVersionCode: "100",
        },
      }),
    } as never);

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      clientAdmission: {
        status: "UPDATE_REQUIRED",
        installedVersionCode: "89",
        minimumVersionCode: "90",
        updateRequired: true,
        platform: "android",
        channel: "internal",
      },
      contentRelease: {
        status: "INCOMPATIBLE",
        releaseId: null,
      },
      errorCode: "client_update_required",
    });
  });

  it("returns only the approved compatible content release for an accepted app", async () => {
    const response = await app.request("/v1/client-config", {
      headers: { "X-LastRail-App-Version-Code": "100", "X-LastRail-App-Platform": "android", "X-LastRail-App-Channel": "internal" },
    }, {
      ...baseEnv,
      VERSION_BUCKET: createVersionBucket({
        version: "1.0.0",
        versionCode: "100",
        minimumVersionCode: "90",
        contentRelease: {
          status: "APPROVED",
          releaseId: "content_act1_001",
          contentVersion: "act1.2026-09-06.001",
          catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
          catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          minimumVersionCode: "90",
          maximumVersionCode: "100",
        },
      }),
    } as never);

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accepted: true,
      serverVersion: {
        version: "1.0.0",
        versionCode: "100",
        minimumVersionCode: "90",
      },
      clientAdmission: {
        status: "ACCEPTED",
        installedVersionCode: "100",
        minimumVersionCode: "90",
        updateRequired: false,
        platform: "android",
        channel: "internal",
      },
      contentRelease: {
        status: "APPROVED_COMPATIBLE",
        releaseId: "content_act1_001",
        contentVersion: "act1.2026-09-06.001",
        catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
        catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        compatibility: {
          minimumVersionCode: "90",
          maximumVersionCode: "100",
        },
      },
      errorCode: null,
      message: null,
    });
    expect(JSON.stringify(body)).not.toContain("admin");
    expect(JSON.stringify(body)).not.toContain("secret");
  });
});
