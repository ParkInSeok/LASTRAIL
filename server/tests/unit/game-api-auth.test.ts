import { describe, expect, it, vi } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken, hashRefreshToken, verifyAccessToken } from "../../apps/game-api/src/auth";

type BoundStatement = {
  sql: string;
  values: unknown[];
};

const encoder = new TextEncoder();

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
  GOOGLE_AUTH_AUDIENCES?: string;
  GOOGLE_ID_TOKEN_VERIFIER?: {
    verifyIdToken: (idToken: string, audiences: string[]) => Promise<{
      subject: string;
      email?: string;
      emailVerified?: boolean;
    }>;
  };
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

function createGoogleEnv(options: {
  linkedAccountId?: string | null;
  linkedAccountStatus?: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  verifierSubject?: string;
  idToken?: string;
  useDefaultVerifier?: boolean;
  googleAudiences?: string;
} = {}): TestEnv {
  const env = createEnv();
  env.GOOGLE_AUTH_AUDIENCES = options.googleAudiences ?? "unit-google-client.apps.googleusercontent.com";
  if (options.useDefaultVerifier !== true) {
    env.GOOGLE_ID_TOKEN_VERIFIER = {
      verifyIdToken: async (idToken, audiences) => {
        expect(idToken).toBe(options.idToken ?? "google.id.token");
        expect(audiences).toEqual(["unit-google-client.apps.googleusercontent.com"]);
        return {
          subject: options.verifierSubject ?? "google-sub-1",
          email: "commander@example.com",
          emailVerified: true,
        };
      },
    };
  }

  const statements: BoundStatement[] = [];
  env.DB = {
    prepare(sql: string) {
      return {
        bind(...values: unknown[]) {
          return {
            sql,
            values,
            first: async () => {
              if (sql.includes("FROM auth_sessions")) {
                return {
                  account_id: "account_guest_1",
                  session_id: "session_guest_1",
                  session_status: "ACTIVE",
                  session_expires_at: Date.parse("2026-09-25T00:00:00.000Z"),
                  account_status: "ACTIVE",
                  account_created_at: Date.parse("2026-08-26T00:00:00.000Z"),
                };
              }
              if (sql.includes("FROM auth_identities") && sql.includes("provider = 'GOOGLE'")) {
                if (options.linkedAccountId === null) return null;
                return {
                  account_id: options.linkedAccountId ?? "account_google_1",
                  account_status: options.linkedAccountStatus ?? "ACTIVE",
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
  } as unknown as D1Database;
  return env;
}

async function createSignedGoogleIdToken(payloadOverrides: Record<string, unknown> = {}, headerOverrides: Record<string, unknown> = {}) {
  const keyPair = await crypto.subtle.generateKey(
    {
      name: "RSASSA-PKCS1-v1_5",
      modulusLength: 2048,
      publicExponent: new Uint8Array([1, 0, 1]),
      hash: "SHA-256",
    },
    true,
    ["sign", "verify"],
  );
  const publicJwk = await crypto.subtle.exportKey("jwk", keyPair.publicKey);
  const payload = {
    iss: "https://accounts.google.com",
    aud: "unit-google-client.apps.googleusercontent.com",
    sub: "google-sub-from-default-verifier",
    exp: Math.floor((Date.now() + 60_000) / 1000),
    ...payloadOverrides,
  };
  const signingInput = `${base64UrlJson({ alg: "RS256", kid: "default-verifier-key", ...headerOverrides })}.${base64UrlJson(payload)}`;
  const signature = await crypto.subtle.sign("RSASSA-PKCS1-v1_5", keyPair.privateKey, encoder.encode(signingInput));
  return {
    token: `${signingInput}.${base64UrlEncode(new Uint8Array(signature))}`,
    jwks: {
      keys: [{ ...publicJwk, kid: "default-verifier-key", alg: "RS256", use: "sig" }],
    },
  };
}

function base64UrlJson(value: unknown): string {
  return base64UrlEncode(encoder.encode(JSON.stringify(value)));
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
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
    expect(sql).toContain("INSERT INTO account_offline_reward_state");

    const accountStatement = db.__statements.find((statement) => statement.sql.includes("INSERT INTO accounts"));
    const offlineStateStatement = db.__statements.find((statement) => statement.sql.includes("INSERT INTO account_offline_reward_state"));
    expect(offlineStateStatement?.values[0]).toBe(accountStatement?.values[0]);
    expect(offlineStateStatement?.values[1]).toBe(accountStatement?.values[1]);
    expect(offlineStateStatement?.values[2]).toBe("none");
    expect(offlineStateStatement?.sql).toContain("last_balance_config_version");
    expect(offlineStateStatement?.sql).toContain("last_previewed_at");
    expect(offlineStateStatement?.sql).toContain("NULL, ?, NULL, 0");
    expect(offlineStateStatement?.values[3]).toBe("act1.2026-08-27.001");
    expect(offlineStateStatement?.values[4]).toBe(accountStatement?.values[1]);

    const authSessionStatement = db.__statements.find((statement) => statement.sql.includes("INSERT INTO auth_sessions"));
    expect(authSessionStatement?.values.some((value) => typeof value === "string" && value.startsWith("refresh_"))).toBe(false);
  });

  it("signs in with a verified Google identity and does not persist raw Google token data", async () => {
    const env = createGoogleEnv({ linkedAccountId: "account_google_1" });
    const response = await app.request(
      "/v1/auth/google",
      {
        method: "POST",
        body: JSON.stringify({ idToken: "google.id.token" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      tokenType: "Bearer",
      account: {
        id: "account_google_1",
        status: "ACTIVE",
      },
    });
    expect(body.accessToken).toEqual(expect.any(String));
    expect(body.refreshToken).toMatch(/^refresh_/u);

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const serializedValues = JSON.stringify(db.__statements.flatMap((statement) => statement.values));
    expect(serializedValues).not.toContain("google.id.token");
    expect(serializedValues).not.toContain("commander@example.com");
    expect(db.__statements.map((statement) => statement.sql).join("\n")).toContain("INSERT INTO auth_sessions");
  });

  it("uses the production Google verifier adapter when no test verifier override is provided", async () => {
    const fixture = await createSignedGoogleIdToken();
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify(fixture.jwks), {
      headers: {
        "Content-Type": "application/json",
        "Cache-Control": "public, max-age=3600",
      },
    }));
    try {
      const env = createGoogleEnv({
        linkedAccountId: "account_google_1",
        idToken: fixture.token,
        useDefaultVerifier: true,
      });
      const response = await app.request(
        "/v1/auth/google",
        {
          method: "POST",
          body: JSON.stringify({ idToken: fixture.token }),
        },
        env,
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        account: {
          id: "account_google_1",
          status: "ACTIVE",
        },
      });
      expect(fetchSpy).toHaveBeenCalledWith("https://www.googleapis.com/oauth2/v3/certs");
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it("keeps Google verifier config and invalid token failures stable without exposing verifier details", async () => {
    const validFixture = await createSignedGoogleIdToken();
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify(validFixture.jwks), {
      headers: {
        "Content-Type": "application/json",
        "Cache-Control": "public, max-age=3600",
      },
    }));
    try {
      const emptyAudienceEnv = createGoogleEnv({
        linkedAccountId: "account_google_1",
        idToken: validFixture.token,
        useDefaultVerifier: true,
        googleAudiences: " , ,, ",
      });
      const emptyAudienceResponse = await app.request(
        "/v1/auth/google",
        {
          method: "POST",
          body: JSON.stringify({ idToken: validFixture.token }),
        },
        emptyAudienceEnv,
      );
      expect(emptyAudienceResponse.status).toBe(500);
      await expect(emptyAudienceResponse.json()).resolves.toEqual({
        errorCode: "google_auth_not_configured",
        message: "Google auth verifier is not configured.",
      });

      const invalidCases = [
        await createSignedGoogleIdToken({ aud: "evil-client.apps.googleusercontent.com" }),
        await createSignedGoogleIdToken({ iss: "https://evil.example" }),
        await createSignedGoogleIdToken({ exp: Math.floor((Date.now() - 60_000) / 1000) }),
        await createSignedGoogleIdToken({}, { alg: "HS256" }),
      ];
      for (const fixture of invalidCases) {
        const env = createGoogleEnv({
          linkedAccountId: "account_google_1",
          idToken: fixture.token,
          useDefaultVerifier: true,
        });
        const response = await app.request(
          "/v1/auth/google",
          {
            method: "POST",
            body: JSON.stringify({ idToken: fixture.token }),
          },
          env,
        );
        expect(response.status).toBe(401);
        const body = await response.json();
        expect(body).toEqual({
          errorCode: "google_id_token_invalid",
          message: "Google ID token is invalid.",
        });
        const serializedBody = JSON.stringify(body);
        expect(serializedBody).not.toContain(fixture.token);
        expect(serializedBody).not.toContain("google_id_token_audience_invalid");
        expect(serializedBody).not.toContain("google_id_token_issuer_invalid");
        expect(serializedBody).not.toContain("google_id_token_expired");
        expect(serializedBody).not.toContain("google_id_token_algorithm_invalid");
      }
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it("links a guest account to a Google identity through bearer auth", async () => {
    const env = createGoogleEnv({ linkedAccountId: null });
    const accessToken = await createAccessToken({
      secret: env.AUTH_SIGNING_KEY_CURRENT,
      accountId: "account_guest_1",
      sessionId: "session_guest_1",
      nowMs: Date.now(),
      ttlMs: 15 * 60 * 1000,
    });

    const response = await app.request(
      "/v1/auth/google/link",
      {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ idToken: "google.id.token" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      tokenType: "Bearer",
      linkStatus: "LINKED",
      account: {
        id: "account_guest_1",
        status: "ACTIVE",
      },
    });

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const googleIdentityInsert = db.__statements.find((statement) => statement.sql.includes("INSERT INTO auth_identities"));
    expect(googleIdentityInsert?.values).toContain("account_guest_1");
    expect(googleIdentityInsert?.values).toContain("google-sub-1");
    expect(JSON.stringify(db.__statements.flatMap((statement) => statement.values))).not.toContain("google.id.token");
  });

  it("treats same-account Google link as idempotent already-linked success", async () => {
    const env = createGoogleEnv({ linkedAccountId: "account_guest_1" });
    const accessToken = await createAccessToken({
      secret: env.AUTH_SIGNING_KEY_CURRENT,
      accountId: "account_guest_1",
      sessionId: "session_guest_1",
      nowMs: Date.now(),
      ttlMs: 15 * 60 * 1000,
    });

    const response = await app.request(
      "/v1/auth/google/link",
      {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ idToken: "google.id.token" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      tokenType: "Bearer",
      linkStatus: "ALREADY_LINKED",
      account: {
        id: "account_guest_1",
        status: "ACTIVE",
      },
    });

    const db = env.DB as unknown as { __statements: BoundStatement[] };
    const sql = db.__statements.map((statement) => statement.sql).join("\n");
    expect(sql).not.toContain("INSERT INTO auth_identities");
    expect(sql).toContain("INSERT INTO auth_sessions");
  });

  it("rejects Google link when the provider identity belongs to another account", async () => {
    const env = createGoogleEnv({ linkedAccountId: "account_other_1" });
    const accessToken = await createAccessToken({
      secret: env.AUTH_SIGNING_KEY_CURRENT,
      accountId: "account_guest_1",
      sessionId: "session_guest_1",
      nowMs: Date.now(),
      ttlMs: 15 * 60 * 1000,
    });

    const response = await app.request(
      "/v1/auth/google/link",
      {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ idToken: "google.id.token", accountId: "account_guest_1" }),
      },
      env,
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      errorCode: "contract_violation",
      message: "accountId must not be supplied by the client.",
    });

    const conflictResponse = await app.request(
      "/v1/auth/google/link",
      {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ idToken: "google.id.token" }),
      },
      env,
    );

    expect(conflictResponse.status).toBe(409);
    await expect(conflictResponse.json()).resolves.toEqual({
      errorCode: "google_identity_already_linked",
      message: "Google identity is already linked to another account.",
    });
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
