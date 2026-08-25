import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type AdminPrincipalRole = "OWNER" | "OPERATOR" | "VIEWER";

type DraftRow = {
  id: string;
  base_version: number | null;
  schema_version: number;
  required_content_version: number;
  config_json: string;
  checksum: string;
  revision: number;
  status: "DRAFT" | "PUBLISHED";
};

function createEnv(options: {
  role?: AdminPrincipalRole;
  draft?: DraftRow | null;
  activeVersion?: number | null;
  nextVersion?: number;
  onBatch?: (statements: unknown[]) => void;
} = {}): Env {
  const role = options.role ?? "OWNER";
  const draft = options.draft ?? {
    id: "draft_1",
    base_version: 1,
    schema_version: 1,
    required_content_version: 1,
    config_json: '{"fuel":{"max":120}}',
    checksum: "sha256:abc",
    revision: 2,
    status: "DRAFT",
  };
  const activeVersion = options.activeVersion ?? 1;
  const nextVersion = options.nextVersion ?? 2;

  const prepare = (sql: string) => ({
    bind(...values: unknown[]) {
      return {
        values,
        first: async () => {
          if (sql.includes("admin_principals")) {
            return {
              id: "principal-1",
              access_subject: "access|owner-1",
              role,
              status: "ACTIVE",
              display_name: "Owner",
            };
          }
          if (sql.includes("FROM balance_config_drafts")) return draft;
          if (sql.includes("FROM balance_config_active")) return activeVersion === null ? null : { version: activeVersion };
          if (sql.includes("MAX(version)")) return { next_version: nextVersion };
          return null;
        },
        run: async () => ({ success: true }),
      };
    },
  });

  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare,
      batch: async (statements: unknown[]) => {
        options.onBatch?.(statements);
        return statements.map(() => ({ success: true }));
      },
    } as unknown as D1Database,
  };
}

describe("admin-api balance publish", () => {
  it("allows an owner to publish a current balance draft", async () => {
    let batchCount = 0;
    const response = await app.request(
      "/v1/admin/balance-config/drafts/draft_1/publish",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          expectedRevision: 2,
          expectedChecksum: "sha256:abc",
        }),
      },
      createEnv({
        onBatch: (statements) => {
          batchCount = statements.length;
        },
      }),
    );

    expect(response.status).toBe(201);
    await expect(response.json()).resolves.toMatchObject({
      publish: {
        draftId: "draft_1",
        version: 2,
        checksum: "sha256:abc",
      },
    });
    expect(batchCount).toBe(5);
  });

  it("does not allow an operator to publish a balance draft", async () => {
    let batchCalled = false;
    const response = await app.request(
      "/v1/admin/balance-config/drafts/draft_1/publish",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          expectedRevision: 2,
          expectedChecksum: "sha256:abc",
        }),
      },
      createEnv({
        role: "OPERATOR",
        onBatch: () => {
          batchCalled = true;
        },
      }),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_PERMISSION_DENIED" });
    expect(batchCalled).toBe(false);
  });

  it("rejects publish when the expected revision or checksum does not match", async () => {
    const response = await app.request(
      "/v1/admin/balance-config/drafts/draft_1/publish",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          expectedRevision: 1,
          expectedChecksum: "sha256:abc",
        }),
      },
      createEnv(),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toEqual({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" });
  });

  it("rejects publish when the draft base version is not the active version", async () => {
    const response = await app.request(
      "/v1/admin/balance-config/drafts/draft_1/publish",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
        },
        body: JSON.stringify({
          expectedRevision: 2,
          expectedChecksum: "sha256:abc",
        }),
      },
      createEnv({
        draft: {
          id: "draft_1",
          base_version: 1,
          schema_version: 1,
          required_content_version: 1,
          config_json: '{"fuel":{"max":120}}',
          checksum: "sha256:abc",
          revision: 2,
          status: "DRAFT",
        },
        activeVersion: 2,
      }),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toEqual({ error: "BALANCE_DRAFT_STALE_BASE" });
  });
});
