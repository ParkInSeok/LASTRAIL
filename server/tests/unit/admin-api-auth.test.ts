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
});
