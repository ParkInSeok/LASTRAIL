import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken, hashRefreshToken, verifyAccessToken } from "../../apps/game-api/src/auth";

type BoundStatement = {
  sql: string;
  values: unknown[];
};

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

function createEnv(): TestEnv {
  const statements: BoundStatement[] = [];
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: "test-secret-minimum-32-characters",
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            return {
              sql,
              values,
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  return {
                    account_id: "account_1",
                    session_id: "session_1",
                    session_status: "ACTIVE",
                    session_expires_at: Date.parse("2026-09-25T00:00:00.000Z"),
                    account_status: "ACTIVE",
                  };
                }
                return null;
              },
            };
          },
        };
      },
      async batch(batchStatements: BoundStatement[]) {
        statements.push(...batchStatements);
        return batchStatements.map(() => ({ success: true }));
      },
      __statements: statements,
    } as unknown as D1Database,
  };
}

function createRefreshEnv(row: Record<string, unknown> | null): TestEnv {
  const statements: BoundStatement[] = [];
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: "test-secret-minimum-32-characters",
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            return {
              sql,
              values,
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  return row;
                }
                return null;
              },
            };
          },
        };
      },
      async batch(batchStatements: BoundStatement[]) {
        statements.push(...batchStatements);
        return batchStatements.map(() => ({ success: true }));
      },
      __statements: statements,
    } as unknown as D1Database,
  };
}

describe("game-api auth", () => {
  it("creates a signed access token and rejects tampered tokens", async () => {
    const secret = "test-secret-minimum-32-characters";
    const token = await createAccessToken({
      secret,
      accountId: "account_1",
      sessionId: "session_1",
      nowMs: Date.parse("2026-08-26T00:00:00.000Z"),
      ttlMs: 900000,
    });

    await expect(verifyAccessToken({
      secret,
      token,
      nowMs: Date.parse("2026-08-26T00:10:00.000Z"),
    })).resolves.toEqual({
      accountId: "account_1",
      sessionId: "session_1",
      expiresAt: Date.parse("2026-08-26T00:15:00.000Z"),
    });

    await expect(verifyAccessToken({
      secret,
      token: `${token.slice(0, -1)}x`,
      nowMs: Date.parse("2026-08-26T00:10:00.000Z"),
    })).resolves.toBeNull();
  });

  it("hashes refresh tokens without storing the plaintext shape", async () => {
    const hash = await hashRefreshToken("refresh_plaintext_token");

    expect(hash).toMatch(/^[a-f0-9]{64}$/u);
    expect(hash).not.toContain("refresh_plaintext_token");
  });

  it("creates a guest account and returns bearer tokens", async () => {
    const env = createEnv();
    const response = await app.request("/v1/auth/guest", { method: "POST" }, env);

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      tokenType: "Bearer",
      account: { status: "ACTIVE" },
    });
    expect(body.account.id).toMatch(/^account_/u);
    expect(body.accessToken).toEqual(expect.any(String));
    expect(body.accessTokenExpiresAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);
    expect(body.refreshToken).toMatch(/^refresh_/u);
    expect(body.refreshTokenExpiresAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const sql = db.__statements.map((statement) => statement.sql).join("\n");
    expect(sql).toContain("INSERT INTO accounts");
    expect(sql).toContain("INSERT INTO auth_identities");
    expect(sql).toContain("INSERT INTO auth_sessions");
    expect(sql).toContain("INSERT INTO player_profiles");
    expect(sql).toContain("INSERT INTO account_runtime_state");
    expect(sql).toContain("INSERT INTO account_tutorial_state");

    const authSessionStatement = db.__statements.find((statement) => statement.sql.includes("INSERT INTO auth_sessions"));
    expect(authSessionStatement?.values.some((value) => typeof value === "string" && value.startsWith("refresh_"))).toBe(false);
  });

  it("rejects gacha pull account scope when Authorization is missing", async () => {
    const response = await app.request(
      "/v1/gacha/banners/banner_pickup_1/pulls",
      {
        method: "POST",
        headers: { "Idempotency-Key": "gacha-idem-1" },
        body: JSON.stringify({ pullCount: 10 }),
      },
      createEnv(),
    );

    expect(response.status).toBe(401);
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
      errorCode: "auth_required",
      message: "Authorization bearer token is required.",
    });
  });

  it("rotates an active refresh token and returns a new bearer token pair", async () => {
    const refreshToken = "refresh_plaintext_token";
    const refreshTokenHash = await hashRefreshToken(refreshToken);
    const env = createRefreshEnv({
      session_id: "session_1",
      account_id: "account_1",
      refresh_token_hash: refreshTokenHash,
      family_id: "family_1",
      generation: 0,
      session_status: "ACTIVE",
      session_expires_at: Date.parse("2026-09-25T00:00:00.000Z"),
      account_status: "ACTIVE",
    });

    const response = await app.request(
      "/v1/auth/refresh",
      {
        method: "POST",
        body: JSON.stringify({ refreshToken }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      tokenType: "Bearer",
      account: {
        id: "account_1",
        status: "ACTIVE",
      },
    });
    expect(body.accessToken).toEqual(expect.any(String));
    expect(body.refreshToken).toMatch(/^refresh_/u);
    expect(body.refreshToken).not.toBe(refreshToken);

    const verified = await verifyAccessToken({
      secret: env.AUTH_SIGNING_KEY_CURRENT,
      token: String(body.accessToken),
      nowMs: Date.now(),
    });
    expect(verified?.accountId).toBe("account_1");

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const sql = db.__statements.map((statement) => statement.sql).join("\n");
    expect(sql).toContain("UPDATE auth_sessions");
    expect(sql).toContain("INSERT INTO auth_sessions");
    expect(db.__statements.find((statement) => statement.sql.includes("UPDATE auth_sessions"))?.values).toContain("session_1");
    const insertStatement = db.__statements.find((statement) => statement.sql.includes("INSERT INTO auth_sessions"));
    expect(insertStatement?.values).toContain("account_1");
    expect(insertStatement?.values).toContain("family_1");
    expect(insertStatement?.values).toContain(1);
    expect(insertStatement?.values.some((value) => value === body.refreshToken)).toBe(false);
  });

  it("marks a reused rotated refresh token family as REUSED", async () => {
    const refreshToken = "refresh_reused_token";
    const env = createRefreshEnv({
      session_id: "session_1",
      account_id: "account_1",
      refresh_token_hash: await hashRefreshToken(refreshToken),
      family_id: "family_1",
      generation: 0,
      session_status: "ROTATED",
      session_expires_at: Date.parse("2026-09-25T00:00:00.000Z"),
      account_status: "ACTIVE",
    });

    const response = await app.request(
      "/v1/auth/refresh",
      {
        method: "POST",
        body: JSON.stringify({ refreshToken }),
      },
      env,
    );

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({
      errorCode: "refresh_token_reused",
      message: "Refresh token was already rotated.",
    });

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    expect(db.__statements.map((statement) => statement.sql).join("\n")).toContain("UPDATE auth_sessions");
    expect(db.__statements[0]?.values).toContain("family_1");
  });
});
