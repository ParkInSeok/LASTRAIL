import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type AdminPrincipalRow = {
  id: string;
  access_subject: string;
  role: "OWNER" | "OPERATOR" | "VIEWER";
  status: "ACTIVE" | "DISABLED";
  display_name: string | null;
};

function createEnv(row: AdminPrincipalRow | null): Env {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("admin_principals");
        return {
          bind(accessSubject: string) {
            expect(accessSubject).toBe("access|owner-1");
            return {
              first: async () => row,
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("admin-api auth", () => {
  const issuer = "https://last-rail.cloudflareaccess.com";
  const audience = "admin-dev-aud";
  const pagesAudience = "admin-pages-dev-aud";

  async function createAccessJwt(input: {
    subject?: string;
    email?: string;
    audience?: string;
    commonName?: string;
    kid?: string;
    expiresAt?: number;
  }): Promise<{ token: string; publicJwk: JsonWebKey & { kid: string; alg: string; use: string } }> {
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

    const kid = input.kid ?? "test-key-1";
    const header = { alg: "RS256", typ: "JWT", kid };
    const payload = {
      iss: issuer,
      aud: input.audience ?? audience,
      sub: input.subject ?? "",
      email: input.email,
      common_name: input.commonName,
      exp: input.expiresAt ?? Math.floor(Date.now() / 1000) + 300,
    };

    const encodedHeader = base64UrlEncode(JSON.stringify(header));
    const encodedPayload = base64UrlEncode(JSON.stringify(payload));
    const signingInput = `${encodedHeader}.${encodedPayload}`;
    const signature = await crypto.subtle.sign(
      "RSASSA-PKCS1-v1_5",
      keyPair.privateKey,
      new TextEncoder().encode(signingInput),
    );
    const publicJwk = await crypto.subtle.exportKey("jwk", keyPair.publicKey);

    return {
      token: `${signingInput}.${base64UrlEncodeBytes(new Uint8Array(signature))}`,
      publicJwk: { ...publicJwk, kid, alg: "RS256", use: "sig" },
    };
  }

  function base64UrlEncode(value: string): string {
    return base64UrlEncodeBytes(new TextEncoder().encode(value));
  }

  function base64UrlEncodeBytes(value: Uint8Array): string {
    return btoa(String.fromCharCode(...value)).replaceAll("+", "-").replaceAll("/", "_").replaceAll("=", "");
  }

  function createAccessEnv(row: AdminPrincipalRow | null): Env {
    const env = createEnv(row);
    env.ADMIN_AUTH_MODE = "ACCESS_JWT_REQUIRED";
    env.ADMIN_ACCESS_ISSUER = issuer;
    env.ADMIN_ACCESS_AUD = audience;
    return env;
  }

  function createDelegatedAccessEnv(row: AdminPrincipalRow | null): Env {
    const env = createAccessEnv(row);
    env.ADMIN_DELEGATED_ACCESS_ISSUER = issuer;
    env.ADMIN_DELEGATED_ACCESS_AUD = pagesAudience;
    return env;
  }

  async function withJwks<T>(jwk: JsonWebKey | JsonWebKey[], run: () => Promise<T>): Promise<T> {
    const originalFetch = globalThis.fetch;
    globalThis.fetch = (async (url: RequestInfo | URL) => {
      expect(String(url)).toBe(`${issuer}/cdn-cgi/access/certs`);
      return Response.json({ keys: Array.isArray(jwk) ? jwk : [jwk] });
    }) as typeof fetch;

    try {
      return await run();
    } finally {
      globalThis.fetch = originalFetch;
    }
  }

  it("rejects protected admin routes without an Access subject", async () => {
    const response = await app.request("/v1/admin/me", {}, createEnv(null));

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" });
  });

  it("maps an active Access subject to the admin principal", async () => {
    const response = await app.request(
      "/v1/admin/me",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv({
        id: "principal-1",
        access_subject: "access|owner-1",
        role: "OWNER",
        status: "ACTIVE",
        display_name: "Owner",
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      principal: {
        id: "principal-1",
        accessSubject: "access|owner-1",
        role: "OWNER",
        displayName: "Owner",
      },
    });
  });

  it("rejects unknown or disabled Access subjects", async () => {
    const response = await app.request(
      "/v1/admin/me",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv(null),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_PRINCIPAL_NOT_AUTHORIZED" });
  });

  it("returns the verified Access subject for DEV bootstrap without requiring a principal", async () => {
    const { token, publicJwk } = await createAccessJwt({
      subject: "access|owner-1",
      email: "owner@example.com",
    });

    await withJwks(publicJwk, async () => {
      const response = await app.request(
        "/v1/admin/bootstrap/access-subject",
        { headers: { "Cf-Access-Jwt-Assertion": token } },
        createAccessEnv(null),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toEqual({
        accessSubject: "access|owner-1",
        email: "owner@example.com",
      });
    });
  });

  it("does not expose the Access subject bootstrap endpoint outside DEV", async () => {
    const { token, publicJwk } = await createAccessJwt({ subject: "access|owner-1" });
    const env = createAccessEnv(null);
    env.ENVIRONMENT = "PRODUCTION";

    await withJwks(publicJwk, async () => {
      const response = await app.request(
        "/v1/admin/bootstrap/access-subject",
        { headers: { "Cf-Access-Jwt-Assertion": token } },
        env,
      );

      expect(response.status).toBe(404);
      await expect(response.json()).resolves.toEqual({ error: "NOT_FOUND" });
    });
  });

  it("does not trust subject headers outside local header auth mode", async () => {
    const env = createEnv({
      id: "principal-1",
      access_subject: "access|owner-1",
      role: "OWNER",
      status: "ACTIVE",
      display_name: "Owner",
    });
    env.ADMIN_AUTH_MODE = "ACCESS_JWT_REQUIRED";

    const response = await app.request(
      "/v1/admin/me",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      env,
    );

    expect(response.status).toBe(501);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" });
  });

  it("maps a valid Access JWT subject to the admin principal", async () => {
    const { token, publicJwk } = await createAccessJwt({ subject: "access|owner-1" });

    await withJwks(publicJwk, async () => {
      const response = await app.request(
        "/v1/admin/me",
        { headers: { "Cf-Access-Jwt-Assertion": token } },
        createAccessEnv({
          id: "principal-1",
          access_subject: "access|owner-1",
          role: "OWNER",
          status: "ACTIVE",
          display_name: "Owner",
        }),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toEqual({
        principal: {
          id: "principal-1",
          accessSubject: "access|owner-1",
          role: "OWNER",
          displayName: "Owner",
        },
      });
    });
  });

  it("maps a delegated Pages Access JWT subject when the upstream request used service auth", async () => {
    const { token: serviceTokenJwt, publicJwk } = await createAccessJwt({
      audience,
      commonName: "lastrail-admin-pages-preview-proxy.access",
      kid: "service-token-key",
    });
    const { token: pagesUserJwt, publicJwk: pagesPublicJwk } = await createAccessJwt({
      subject: "access|owner-1",
      email: "owner@example.com",
      audience: pagesAudience,
      kid: "pages-user-key",
    });

    await withJwks([publicJwk, pagesPublicJwk], async () => {
      const response = await app.request(
        "/v1/admin/me",
        {
          headers: {
            "Cf-Access-Jwt-Assertion": serviceTokenJwt,
            "X-Lastrail-User-Access-Jwt": pagesUserJwt,
          },
        },
        createDelegatedAccessEnv({
          id: "principal-1",
          access_subject: "access|owner-1",
          role: "OWNER",
          status: "ACTIVE",
          display_name: "Owner",
        }),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toEqual({
        principal: {
          id: "principal-1",
          accessSubject: "access|owner-1",
          role: "OWNER",
          displayName: "Owner",
        },
      });
    });
  });

  it("rejects Access JWTs for a different audience", async () => {
    const { token, publicJwk } = await createAccessJwt({ subject: "access|owner-1", audience: "other-aud" });

    await withJwks(publicJwk, async () => {
      const response = await app.request(
        "/v1/admin/me",
        { headers: { "Cf-Access-Jwt-Assertion": token } },
        createAccessEnv(null),
      );

      expect(response.status).toBe(403);
      await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_JWT_INVALID" });
    });
  });
});
