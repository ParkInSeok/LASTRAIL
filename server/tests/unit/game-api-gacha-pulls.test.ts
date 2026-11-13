import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type GachaBannerRow = {
  id: string;
  banner_type: "STANDARD" | "PICKUP";
  status: "DRAFT" | "ACTIVE" | "ENDED" | "DISABLED";
  balance_config_version: number;
  pickup_operator_id: string | null;
  starts_at: number;
  ends_at: number | null;
  pity_config_json: string;
  pool_snapshot_json: string;
};

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId: "account_1",
    sessionId: "session_1",
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(row: GachaBannerRow | null): TestEnv {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            return {
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  expect(values).toEqual(["session_1", "account_1"]);
                  return {
                    account_id: "account_1",
                    session_id: "session_1",
                    session_status: "ACTIVE",
                    session_expires_at: Date.now() + 900000,
                    account_status: "ACTIVE",
                  };
                }
                if (sql.includes("FROM idempotency_records")) {
                  expect(values).toEqual(["account_1", "gacha-idem-1", "banner_pickup_1"]);
                  return null;
                }
                if (sql.includes("FROM account_items")) {
                  expect(values).toEqual(["account_1"]);
                  return null;
                }
                expect(sql).toContain("gacha_banners");
                expect(values).toEqual(["banner_pickup_1"]);
                return row;
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

function expectNoForbiddenFields(value: unknown): void {
  const forbiddenKeys = new Set([
    "accountId",
    "accessToken",
    "refreshToken",
    "secret",
    "rngSeed",
    "rawRng",
    "rawRngMaterial",
    "probability",
    "weighted",
  ]);
  const visit = (candidate: unknown): void => {
    if (Array.isArray(candidate)) {
      for (const item of candidate) visit(item);
      return;
    }
    if (typeof candidate !== "object" || candidate === null) return;
    for (const [key, child] of Object.entries(candidate)) {
      expect(forbiddenKeys.has(key)).toBe(false);
      visit(child);
    }
  };

  visit(value);
}

describe("game-api gacha pull contract", () => {
  it("rejects pull requests without Idempotency-Key", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        body: JSON.stringify({ pullCount: 10 }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: null,
      cost: null,
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    });
  });

  it("rejects body bannerId because the path bannerId is the only source of truth", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: { "Idempotency-Key": "gacha-idem-1" },
        body: JSON.stringify({ bannerId: "banner_pickup_1", pullCount: 10 }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: 10,
      cost: {
        itemId: "cur_command_ticket",
        quantity: 10,
      },
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "contract_violation",
      message: "Request body must not include bannerId.",
    });
  });

  it("rejects pull counts other than 1 or 10", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: { "Idempotency-Key": "gacha-idem-1" },
        body: JSON.stringify({ pullCount: 3 }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: 3,
      cost: null,
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "invalid_pull_count",
      message: "pullCount must be 1 or 10.",
    });
  });

  it("returns 404 rejected when the banner does not exist", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "gacha-idem-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({ pullCount: 10 }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(404);
    await expect(response.json()).resolves.toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: 10,
      cost: {
        itemId: "cur_command_ticket",
        quantity: 10,
      },
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "banner_not_found",
      message: "Gacha banner was not found.",
    });
  });

  it("returns 409 rejected when the banner is not active", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "gacha-idem-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({ pullCount: 10 }),
      },
      createEnv({
        id: "banner_pickup_1",
        banner_type: "PICKUP",
        status: "ENDED",
        balance_config_version: 1,
        pickup_operator_id: "op_vex",
        starts_at: Date.parse("2026-08-01T00:00:00.000Z"),
        ends_at: Date.parse("2026-08-20T00:00:00.000Z"),
        pity_config_json: "{}",
        pool_snapshot_json: "{}",
      }),
    );

    expect(response.status).toBe(409);
    const body = await response.json();
    expect(body).toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: 10,
      cost: {
        itemId: "cur_command_ticket",
        quantity: 10,
      },
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "banner_inactive",
      message: "Gacha banner is not active.",
    });
  });

  it("rejects active banner pulls when the account has insufficient command tickets", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "gacha-idem-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({ pullCount: 10 }),
      },
      createEnv({
        id: "banner_pickup_1",
        banner_type: "PICKUP",
        status: "ACTIVE",
        balance_config_version: 1,
        pickup_operator_id: "op_vex",
        starts_at: Date.parse("2026-08-01T00:00:00.000Z"),
        ends_at: null,
        pity_config_json: "{}",
        pool_snapshot_json: "{}",
      }),
    );

    expect(response.status).toBe(409);
    const body = await response.json();
    expect(body).toEqual({
      accepted: false,
      resultStatus: "REJECTED",
      batchId: null,
      bannerId: "banner_pickup_1",
      pullCount: 10,
      cost: {
        itemId: "cur_command_ticket",
        quantity: 10,
      },
      results: [],
      postPityState: null,
      createdAt: null,
      errorCode: "insufficient_command_ticket",
      message: "Not enough command tickets.",
    });
    expectNoForbiddenFields(body);
  });
});
