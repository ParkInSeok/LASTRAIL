import { describe, expect, it } from "vitest";
import type { UpstreamClient } from "./upstream";
import { buildServer } from "./server";

describe("admin backend routes", () => {
  it("returns bootstrap identity with request id", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174,
        cloudflareAccessClientId: "client-id",
        cloudflareAccessClientSecret: "client-secret",
        adminApiToken: "admin-token"
      },
      upstream: (async (path) => {
        expect(path).toBe("/v1/admin/me");
        return {
          principal: {
            id: "admin-1",
            accessSubject: "access|operator-1",
            displayName: "Operator One",
            role: "OPERATOR"
          }
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/bootstrap",
      headers: { "x-request-id": "req-test" }
    });

    expect(response.statusCode).toBe(200);
    expect(response.headers["x-request-id"]).toBe("req-test");
    expect(response.json().admin.permissions).toContain("ACCOUNT_READ");
    expect(response.json().admin.permissions).not.toContain("BALANCE_PUBLISH");
  });

  it("proxies players to the versioned Admin API path", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: (async (path, init) => {
        expect(path).toBe("/v1/admin/players");
        expect(init.searchParams?.get("q")).toBe("Kyle");
        return {
          items: [
            {
              accountId: "account-1",
              displayName: "Kyle Account",
              status: "ACTIVE",
              syncLevel: 30,
              lastSeenAt: "2026-08-25T12:00:00.000Z"
            }
          ]
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/players?q=Kyle"
    });

    expect(response.statusCode).toBe(200);
    expect(response.json().items[0].accountId).toBe("account-1");
  });

  it("normalizes schema validation failures from upstream", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: async () => ({ items: [{ accountId: "" }] })
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/players"
    });

    expect(response.statusCode).toBe(502);
    expect(response.json().error.code).toBe("UPSTREAM_SCHEMA_INVALID");
    expect(response.json().error.requestId).toEqual(expect.any(String));
  });
});
