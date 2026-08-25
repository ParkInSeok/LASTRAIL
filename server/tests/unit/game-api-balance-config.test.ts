import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";

type BalanceRow = {
  version: number;
  schema_version: number;
  required_content_version: number;
  checksum: string;
  config_json: string;
  published_at: number;
};

function createEnv(row: BalanceRow | null): Env {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("balance_config_versions");
        return {
          bind(version: number) {
            expect(version).toBe(7);
            return {
              first: async () => row,
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("game-api balance config", () => {
  it("returns a published balance config by version", async () => {
    const response = await app.request("/v1/balance-config/7", {}, createEnv({
      version: 7,
      schema_version: 1,
      required_content_version: 3,
      checksum: "sha256:abc",
      config_json: JSON.stringify({ fuel: { max: 100 }, train: { speed: 12 } }),
      published_at: 1_800_000_000,
    }));

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      version: 7,
      schemaVersion: 1,
      requiredContentVersion: 3,
      checksum: "sha256:abc",
      publishedAt: 1_800_000_000,
      payload: { fuel: { max: 100 }, train: { speed: 12 } },
    });
  });

  it("returns 404 when the requested balance config is not published", async () => {
    const response = await app.request("/v1/balance-config/7", {}, createEnv(null));

    expect(response.status).toBe(404);
    await expect(response.json()).resolves.toEqual({
      error: "BALANCE_CONFIG_NOT_PUBLISHED",
      version: 7,
    });
  });

  it("rejects invalid balance config versions", async () => {
    const response = await app.request("/v1/balance-config/latest", {}, createEnv(null));

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      error: "INVALID_BALANCE_CONFIG_VERSION",
      version: "latest",
    });
  });

  it("returns 500 when a published balance config payload is invalid JSON", async () => {
    const response = await app.request("/v1/balance-config/7", {}, createEnv({
      version: 7,
      schema_version: 1,
      required_content_version: 3,
      checksum: "sha256:broken",
      config_json: "{",
      published_at: 1_800_000_000,
    }));

    expect(response.status).toBe(500);
    await expect(response.json()).resolves.toEqual({
      error: "BALANCE_CONFIG_PAYLOAD_INVALID",
      version: 7,
    });
  });
});
