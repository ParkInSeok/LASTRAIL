import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type BoundStatement = {
  sql: string;
  values: unknown[];
};

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(accountId = "account_1", sessionId = "session_1"): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId,
    sessionId,
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(options: {
  accountId?: string;
  sessionId?: string;
  profile?: {
    account_id: string;
    display_name: string | null;
    display_name_normalized: string | null;
    display_name_updated_at: number | null;
    commander_xp: number;
    created_at: number;
    updated_at: number;
  } | null;
  normalizedOwnerAccountId?: string | null;
} = {}): TestEnv {
  const accountId = options.accountId ?? "account_1";
  const sessionId = options.sessionId ?? "session_1";
  const profile = options.profile ?? {
    account_id: accountId,
    display_name: null,
    display_name_normalized: null,
    display_name_updated_at: null,
    commander_xp: 0,
    created_at: Date.parse("2026-08-26T00:00:00.000Z"),
    updated_at: Date.parse("2026-08-26T00:00:00.000Z"),
  };
  const statements: BoundStatement[] = [];

  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            const statement = { sql, values };
            return {
              sql,
              values,
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  expect(values).toEqual([sessionId, accountId]);
                  return {
                    account_id: accountId,
                    session_id: sessionId,
                    session_status: "ACTIVE",
                    session_expires_at: Date.now() + 900000,
                    account_status: "ACTIVE",
                    account_created_at: Date.parse("2026-08-26T00:00:00.000Z"),
                  };
                }
                if (sql.includes("FROM player_profiles") && sql.includes("WHERE account_id = ?")) {
                  expect(values).toEqual([accountId]);
                  return profile;
                }
                if (sql.includes("FROM player_profiles") && sql.includes("display_name_normalized = ?")) {
                  if (options.normalizedOwnerAccountId === undefined) return null;
                  if (options.normalizedOwnerAccountId === null) return null;
                  return { account_id: options.normalizedOwnerAccountId };
                }
                return null;
              },
              run: async () => {
                statements.push(statement);
                return { success: true, meta: { changes: 1 } };
              },
            };
          },
        };
      },
      __statements: statements,
    } as unknown as D1Database,
  };
}

describe("game-api profile display name contract", () => {
  it("returns the authenticated account profile without using a player-supplied account id", async () => {
    const response = await app.request(
      "/v1/profile",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv({
        profile: {
          account_id: "account_1",
          display_name: "Rail Captain",
          display_name_normalized: "rail captain",
          display_name_updated_at: Date.parse("2026-08-27T00:00:00.000Z"),
          commander_xp: 120,
          created_at: Date.parse("2026-08-26T00:00:00.000Z"),
          updated_at: Date.parse("2026-08-27T00:00:00.000Z"),
        },
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      accepted: true,
      account: {
        id: "account_1",
      },
      profile: {
        displayName: "Rail Captain",
        commanderXp: 120,
        displayNameUpdatedAt: "2026-08-27T00:00:00.000Z",
        nextDisplayNameChangeAvailableAt: "2026-09-03T00:00:00.000Z",
      },
      errorCode: null,
      message: null,
    });
  });

  it("rejects profile reads with account id query shortcuts", async () => {
    const response = await app.request(
      "/v1/profile?accountId=account_other",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv(),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "contract_violation",
    });
  });

  it("checks display name availability using normalized uniqueness", async () => {
    const response = await app.request(
      "/v1/profile/display-name/check?displayName=%20Rail%20%20Captain%20",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv({ normalizedOwnerAccountId: null }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      accepted: true,
      displayName: "Rail Captain",
      available: true,
      errorCode: null,
      message: null,
    });
  });

  it("reports a display name as unavailable when another account owns the normalized value", async () => {
    const response = await app.request(
      "/v1/profile/display-name/check?displayName=rail%20captain",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv({ normalizedOwnerAccountId: "account_other" }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      accepted: true,
      displayName: "rail captain",
      available: false,
      errorCode: "display_name_taken",
      message: "Display name is already taken.",
    });
  });

  it("updates the authenticated account display name and stores the normalized key", async () => {
    const env = createEnv({ normalizedOwnerAccountId: null });
    const response = await app.request(
      "/v1/profile/display-name",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ displayName: "  Rail   Captain  " }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accepted: true,
      displayName: "Rail Captain",
      changeStatus: "UPDATED",
      errorCode: null,
      message: null,
    });
    expect(body.displayNameUpdatedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);
    expect(body.nextDisplayNameChangeAvailableAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const update = db.__statements.find((statement) => statement.sql.includes("UPDATE player_profiles"));
    expect(update?.sql).toContain("display_name_normalized");
    expect(update?.values[0]).toBe("Rail Captain");
    expect(update?.values[1]).toBe("rail captain");
    expect(update?.values[4]).toBe("account_1");
  });

  it("treats re-submitting the current normalized display name as idempotent success", async () => {
    const response = await app.request(
      "/v1/profile/display-name",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ displayName: "rail captain" }),
      },
      createEnv({
        profile: {
          account_id: "account_1",
          display_name: "Rail Captain",
          display_name_normalized: "rail captain",
          display_name_updated_at: Date.now(),
          commander_xp: 0,
          created_at: Date.parse("2026-08-26T00:00:00.000Z"),
          updated_at: Date.now(),
        },
        normalizedOwnerAccountId: "account_1",
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      displayName: "Rail Captain",
      changeStatus: "UNCHANGED",
      errorCode: null,
    });
  });

  it("rejects changing to a different display name during the cooldown window", async () => {
    const response = await app.request(
      "/v1/profile/display-name",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ displayName: "Signal Keeper" }),
      },
      createEnv({
        profile: {
          account_id: "account_1",
          display_name: "Rail Captain",
          display_name_normalized: "rail captain",
          display_name_updated_at: Date.now(),
          commander_xp: 0,
          created_at: Date.parse("2026-08-26T00:00:00.000Z"),
          updated_at: Date.now(),
        },
        normalizedOwnerAccountId: null,
      }),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "display_name_change_cooldown",
    });
  });

  it("rejects invalid display names and account id shortcuts", async () => {
    const invalid = await app.request(
      "/v1/profile/display-name",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ displayName: "a" }),
      },
      createEnv(),
    );
    expect(invalid.status).toBe(400);
    await expect(invalid.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "invalid_display_name",
    });

    const shortcut = await app.request(
      "/v1/profile/display-name?accountId=account_other",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ displayName: "Rail Captain" }),
      },
      createEnv(),
    );
    expect(shortcut.status).toBe(400);
    await expect(shortcut.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "contract_violation",
    });
  });
});
