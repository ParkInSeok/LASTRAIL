import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/admin-api/src/index";

type SqlValue = string | number | Uint8Array | null;

class SqlJsD1Statement {
  private readonly database: initSqlJs.Database;
  private values: SqlValue[] = [];

  constructor(database: initSqlJs.Database, readonly sql: string) {
    this.database = database;
  }

  bind(...values: SqlValue[]): SqlJsD1Statement {
    this.values = values;
    return this;
  }

  async first<T>(): Promise<T | null> {
    const statement = this.database.prepare(this.sql);
    try {
      statement.bind(this.values);
      if (!statement.step()) return null;
      return statement.getAsObject() as T;
    } finally {
      statement.free();
    }
  }

  async all<T>(): Promise<{ results: T[] }> {
    const statement = this.database.prepare(this.sql);
    const results: T[] = [];
    try {
      statement.bind(this.values);
      while (statement.step()) results.push(statement.getAsObject() as T);
      return { results };
    } finally {
      statement.free();
    }
  }
}

class SqlJsD1Database {
  constructor(private readonly database: initSqlJs.Database) {}

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }
}

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(29);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env {
  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function seedAdminSupportTicket(db: initSqlJs.Database): void {
  db.run(
    `INSERT INTO admin_principals (
       id, access_subject, role, status, display_name, created_at, updated_at
     ) VALUES ('principal-support-ticket-1', 'access|support-ticket-viewer', 'VIEWER', 'ACTIVE', 'Support Viewer', 1, 1);`,
  );
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-support-ticket-1', 'ACTIVE', 1, 1);");
  db.run(
    `INSERT INTO support_tickets (
       id, account_id, category, status, body_text, body_preview, body_length,
       app_version, app_version_code, client_composed_at, client_submitted_at,
       server_received_at, idempotency_key, request_hash, created_at, updated_at
     ) VALUES (
       'support_ticket_admin_1', 'account-support-ticket-1', 'RUN_REWARD', 'OPEN',
       '보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.',
       '보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.',
       29, '1.0.0-dev', '42', 1000, 2000, 3000,
       'support-ticket-admin-key', 'sha256:ticket-admin', 3000, 3000
     );`,
  );
  db.run(
    `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
     VALUES ('idem-support-ticket-admin-1', 'account-support-ticket-1', 'support-ticket-admin-key', 'support_ticket_create', 'support_ticket_admin_1', 3000);`,
  );
}

describe("admin-api support tickets local D1 smoke", () => {
  it("lists and reads support tickets as sanitized read-only evidence", async () => {
    const db = await createMigratedDb();
    seedAdminSupportTicket(db);
    const env = createEnv(db);

    const listResponse = await app.request(
      "/v1/admin/support/tickets?accountId=account-support-ticket-1&category=RUN_REWARD",
      { headers: { "Cf-Access-Subject": "access|support-ticket-viewer" } },
      env,
    );
    expect(listResponse.status).toBe(200);
    const listBody = await listResponse.json() as { items: Array<{ ticketId: string }> };
    expect(listBody).toMatchObject({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          ticketId: "support_ticket_admin_1",
          accountId: "account-support-ticket-1",
          category: "RUN_REWARD",
          status: "OPEN",
          bodyPreview: "보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.",
          bodyLength: 29,
          detailAvailable: true,
          appVersion: "1.0.0-dev",
          appVersionCode: "42",
          serverReceivedAt: "1970-01-01T00:00:03.000Z",
        },
      ],
      nextCursor: null,
      errorCode: null,
    });

    const detailResponse = await app.request(
      "/v1/admin/support/tickets/support_ticket_admin_1",
      { headers: { "Cf-Access-Subject": "access|support-ticket-viewer" } },
      env,
    );
    expect(detailResponse.status).toBe(200);
    const detailBody = await detailResponse.json();
    expect(detailBody).toMatchObject({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      ticket: {
        ticketId: "support_ticket_admin_1",
        accountId: "account-support-ticket-1",
        category: "RUN_REWARD",
        status: "OPEN",
        body: "보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.",
        idempotencyReplayKnown: true,
      },
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        repairEndpointsEnabled: false,
      },
      errorCode: null,
    });
    expect(JSON.stringify(detailBody)).not.toContain("Authorization");
    expect(JSON.stringify(detailBody)).not.toContain("accessToken");
    expect(JSON.stringify(detailBody)).not.toContain("refreshToken");
    expect(JSON.stringify(detailBody)).not.toContain("rawRng");
  });

  it("rejects mutation-like Admin support ticket request bodies", async () => {
    const db = await createMigratedDb();
    seedAdminSupportTicket(db);
    const env = createEnv(db);

    const response = await app.request(
      "/v1/admin/support/tickets",
      {
        method: "GET",
        headers: {
          "Cf-Access-Subject": "access|support-ticket-viewer",
          "Content-Type": "application/json",
          "content-length": "2",
        },
      },
      env,
    );
    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({ error: "contract_violation" });
  });
});
