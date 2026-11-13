import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type AdminPrincipalRole = "OWNER" | "OPERATOR" | "VIEWER";

function createEnv(role: AdminPrincipalRole): Env {
  const statements: string[] = [];

  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare(sql: string) {
        statements.push(sql);
        if (sql.includes("admin_principals")) {
          return {
            bind(accessSubject: string) {
              expect(accessSubject).toBe("access|owner-1");
              return {
                first: async () => ({
                  id: "principal-1",
                  access_subject: "access|owner-1",
                  role,
                  status: "ACTIVE",
                  display_name: "Owner",
                }),
              };
            },
          };
        }

        expect(sql).toContain("INSERT INTO balance_config_drafts");
        return {
          bind(...values: unknown[]) {
            const [id, baseVersion, schemaVersion, requiredContentVersion, configJson, checksum, createdBy] = values;
            expect(String(id)).toMatch(/^draft_/u);
            expect(baseVersion).toBe(1);
            expect(schemaVersion).toBe(1);
            expect(requiredContentVersion).toBe(1);
            expect(configJson).toBe('{"fuel":{"max":120},"train":{"baseSpeed":11}}');
            expect(String(checksum)).toMatch(/^sha256:[a-f0-9]{64}$/u);
            expect(createdBy).toBe("principal-1");
            return {
              run: async () => ({ success: true }),
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("admin-api balance drafts", () => {
  it("allows an owner to create a balance config draft", async () => {
    const response = await app.request(
      "/v1/admin/balance-config/drafts",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          baseVersion: 1,
          schemaVersion: 1,
          requiredContentVersion: 1,
          config: {
            train: { baseSpeed: 11 },
            fuel: { max: 120 },
          },
        }),
      },
      createEnv("OWNER"),
    );

    expect(response.status).toBe(201);
    const body = await response.json();
    expect(body).toMatchObject({
      draft: {
        baseVersion: 1,
        schemaVersion: 1,
        requiredContentVersion: 1,
        revision: 1,
        status: "DRAFT",
      },
    });
    expect(body.draft.id).toMatch(/^draft_/u);
    expect(body.draft.checksum).toMatch(/^sha256:[a-f0-9]{64}$/u);
  });

  it("does not allow a viewer to create a balance config draft", async () => {
    const response = await app.request(
      "/v1/admin/balance-config/drafts",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          schemaVersion: 1,
          requiredContentVersion: 1,
          config: { fuel: { max: 120 } },
        }),
      },
      createEnv("VIEWER"),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_PERMISSION_DENIED" });
  });
});
