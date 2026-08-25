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

type ConfirmationRow = {
  id: string;
  payload_hash: string;
};

function createEnv(options: {
  role?: AdminPrincipalRole;
  draft?: DraftRow | null;
  activeVersion?: number | null;
  nextVersion?: number;
  confirmation?: ConfirmationRow | null;
  onBatch?: (statements: unknown[]) => void;
  onRun?: (sql: string, values: unknown[]) => void;
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
  const confirmation =
    options.confirmation === undefined
      ? {
          id: "confirmation_1",
          payload_hash: "sha256:payload",
        }
      : options.confirmation;

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
          if (sql.includes("FROM admin_confirmations")) return confirmation;
          return null;
        },
        run: async () => {
          options.onRun?.(sql, values);
          return { success: true };
        },
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
          confirmationToken: "confirm_token",
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
    expect(batchCount).toBe(7);
  });

  it("creates a confirmation token for a balance publish payload", async () => {
    let insertedConfirmation = false;
    const response = await app.request(
      "/v1/admin/balance-config/drafts/draft_1/publish/confirmations",
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
          insertedConfirmation = statements.length === 1;
        },
      }),
    );

    expect(response.status).toBe(201);
    const body = await response.json();
    expect(body.confirmation).toMatchObject({
      permission: "BALANCE_PUBLISH",
      payloadHash: expect.stringMatching(/^sha256:[a-f0-9]{64}$/u),
    });
    expect(body.confirmation.id).toMatch(/^confirmation_/u);
    expect(body.confirmation.token).toMatch(/^confirm_/u);
    expect(body.confirmation.expiresAt).toEqual(expect.any(Number));
    expect(insertedConfirmation).toBe(true);
  });

  it("requires a confirmation token when publishing a balance draft", async () => {
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
        onBatch: () => {
          batchCalled = true;
        },
      }),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({ error: "BALANCE_PUBLISH_INVALID" });
    expect(batchCalled).toBe(false);
  });

  it("rejects publish when confirmation token is missing or invalid", async () => {
    let failureAuditWritten = false;
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
          confirmationToken: "confirm_wrong",
        }),
      },
      createEnv({
        confirmation: null,
        onRun: (sql, values) => {
          if (sql.includes("admin_audit_logs")) {
            failureAuditWritten = JSON.stringify(values).includes("FAILURE") && JSON.stringify(values).includes("ADMIN_CONFIRMATION_INVALID");
          }
        },
      }),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_CONFIRMATION_INVALID" });
    expect(failureAuditWritten).toBe(true);
  });

  it("does not allow an operator to publish a balance draft", async () => {
    let batchCalled = false;
    let failureAuditWritten = false;
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
          confirmationToken: "confirm_token",
        }),
      },
      createEnv({
        role: "OPERATOR",
        onBatch: () => {
          batchCalled = true;
        },
        onRun: (sql, values) => {
          if (sql.includes("admin_audit_logs")) {
            failureAuditWritten = JSON.stringify(values).includes("FAILURE") && JSON.stringify(values).includes("ADMIN_PERMISSION_DENIED");
          }
        },
      }),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_PERMISSION_DENIED" });
    expect(batchCalled).toBe(false);
    expect(failureAuditWritten).toBe(true);
  });

  it("rejects publish when the expected revision or checksum does not match", async () => {
    let failureAuditWritten = false;
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
          confirmationToken: "confirm_token",
        }),
      },
      createEnv({
        onRun: (sql, values) => {
          if (sql.includes("admin_audit_logs")) {
            failureAuditWritten = JSON.stringify(values).includes("FAILURE") && JSON.stringify(values).includes("BALANCE_DRAFT_PRECONDITION_FAILED");
          }
        },
      }),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toEqual({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" });
    expect(failureAuditWritten).toBe(true);
  });

  it("rejects publish when the draft base version is not the active version", async () => {
    let failureAuditWritten = false;
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
          confirmationToken: "confirm_token",
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
        onRun: (sql, values) => {
          if (sql.includes("admin_audit_logs")) {
            failureAuditWritten = JSON.stringify(values).includes("FAILURE") && JSON.stringify(values).includes("BALANCE_DRAFT_STALE_BASE");
          }
        },
      }),
    );

    expect(response.status).toBe(409);
    await expect(response.json()).resolves.toEqual({ error: "BALANCE_DRAFT_STALE_BASE" });
    expect(failureAuditWritten).toBe(true);
  });
});
