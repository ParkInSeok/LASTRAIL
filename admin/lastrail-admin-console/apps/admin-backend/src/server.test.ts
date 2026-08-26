import { describe, expect, it } from "vitest";
import type { UpstreamClient } from "./upstream";
import { buildServer } from "./server";
import { createUpstreamClient } from "./upstream";

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

  it("proxies player note creation to the versioned Admin API path", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: (async (path, init) => {
        expect(path).toBe("/v1/admin/players/account-1/notes");
        expect(init.method).toBe("POST");
        expect(init.body).toEqual({ note: "Reviewed abnormal Training Core spread." });
        return {
          note: {
            accountId: "account-1",
            action: "PLAYER_NOTE_CREATE",
            result: "SUCCESS",
            requestId: init.requestId
          }
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "POST",
      url: "/api/players/account-1/notes",
      headers: { "x-request-id": "req-note-1" },
      payload: { note: "Reviewed abnormal Training Core spread." }
    });

    expect(response.statusCode).toBe(201);
    expect(response.json()).toMatchObject({
      note: {
        accountId: "account-1",
        action: "PLAYER_NOTE_CREATE",
        result: "SUCCESS",
        requestId: "req-note-1"
      }
    });
  });

  it("proxies player note history to the versioned Admin API path", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: (async (path, init) => {
        expect(path).toBe("/v1/admin/players/account-1/notes");
        expect(init.method).toBe("GET");
        return {
          items: [
            {
              id: "audit-1",
              accountId: "account-1",
              note: "Reviewed abnormal Training Core spread.",
              authorId: "principal-1",
              authorDisplayName: "Owner",
              requestId: init.requestId,
              createdAt: "2026-08-26T03:30:00.000Z"
            }
          ]
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/players/account-1/notes",
      headers: { "x-request-id": "req-note-list-1" }
    });

    expect(response.statusCode).toBe(200);
    expect(response.json().items[0]).toMatchObject({
      id: "audit-1",
      note: "Reviewed abnormal Training Core spread.",
      requestId: "req-note-list-1"
    });
  });

  it("proxies audit search filters to the versioned Admin API path", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: (async (path, init) => {
        expect(path).toBe("/v1/admin/audit");
        expect(init.method).toBe("GET");
        expect(init.searchParams?.get("action")).toBe("PLAYER_NOTE_CREATE");
        expect(init.searchParams?.get("resourceId")).toBe("account-1");
        return {
          items: [
            {
              id: "audit-1",
              principalId: "principal-1",
              principalDisplayName: "Owner",
              role: "OWNER",
              permission: "ACCOUNT_NOTE_WRITE",
              action: "PLAYER_NOTE_CREATE",
              resourceType: "account",
              resourceId: "account-1",
              requestId: "req-note-1",
              confirmationId: null,
              payloadHash: "sha256:abc",
              changeSummary: { note: "Reviewed abnormal Training Core spread." },
              result: "SUCCESS",
              errorCode: null,
              createdAt: "2026-08-26T03:30:00.000Z"
            }
          ]
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/audit?action=PLAYER_NOTE_CREATE&resourceId=account-1"
    });

    expect(response.statusCode).toBe(200);
    expect(response.json().items[0].action).toBe("PLAYER_NOTE_CREATE");
  });

  it("proxies integrity event filters to the versioned Admin API path", async () => {
    const server = buildServer({
      config: {
        adminApiBaseUrl: "https://admin-api.example.test",
        profile: "staging",
        host: "127.0.0.1",
        port: 4174
      },
      upstream: (async (path, init) => {
        expect(path).toBe("/v1/admin/integrity/events");
        expect(init.method).toBe("GET");
        expect(init.searchParams?.get("accountId")).toBe("account-1");
        expect(init.searchParams?.get("classification")).toBe("SOFT_ANOMALY");
        expect(init.searchParams?.get("reviewState")).toBe("OPEN");
        return {
          items: [
            {
              id: "event-1",
              accountId: "account-1",
              runId: "run-1",
              classification: "SOFT_ANOMALY",
              category: "DAMAGE_ANALYSIS",
              evidenceCode: "DAMAGE_SPIKE",
              evidence: { maxDamageRatio: 2.4 },
              reviewState: "OPEN",
              assignedTo: null,
              createdAt: "2026-08-26T04:00:00.000Z"
            }
          ]
        };
      }) satisfies UpstreamClient
    });

    const response = await server.inject({
      method: "GET",
      url: "/api/integrity/events?accountId=account-1&classification=SOFT_ANOMALY&reviewState=OPEN"
    });

    expect(response.statusCode).toBe(200);
    expect(response.json().items[0].evidenceCode).toBe("DAMAGE_SPIKE");
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

  it("sends a local Access subject only for local Admin API development", async () => {
    const originalFetch = globalThis.fetch;
    const seenHeaders: Record<string, string | null> = {};
    globalThis.fetch = (async (_url: RequestInfo | URL, init?: RequestInit) => {
      const headers = new Headers(init?.headers);
      seenHeaders.subject = headers.get("Cf-Access-Subject");
      return Response.json({
        principal: {
          id: "admin-1",
          accessSubject: "access|local-owner",
          displayName: "Local Owner",
          role: "OWNER"
        }
      });
    }) as typeof fetch;

    try {
      const upstream = createUpstreamClient({
        adminApiBaseUrl: "http://127.0.0.1:8787",
        profile: "local",
        host: "127.0.0.1",
        port: 4174,
        localAccessSubject: "access|local-owner"
      });

      await upstream("/v1/admin/me", {
        method: "GET",
        requestId: "req-local"
      });

      expect(seenHeaders.subject).toBe("access|local-owner");
    } finally {
      globalThis.fetch = originalFetch;
    }
  });
});
