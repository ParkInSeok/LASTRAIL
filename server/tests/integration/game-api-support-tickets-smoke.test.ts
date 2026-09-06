import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";

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

  run(): void {
    this.database.run(this.sql, this.values);
  }
}

class SqlJsD1Database {
  constructor(private readonly database: initSqlJs.Database) {}

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.database.run("BEGIN;");
    try {
      for (const statement of statements) statement.run();
      this.database.run("COMMIT;");
      return statements.map(() => ({ success: true }));
    } catch (error) {
      this.database.run("ROLLBACK;");
      throw error;
    }
  }
}

const authSecret = "test-secret-minimum-32-characters";
const fixedNow = Date.parse("2026-08-30T00:00:00.000Z");

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(20);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env & { AUTH_SIGNING_KEY_CURRENT: string } {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

describe("game-api support ticket local D1 smoke", () => {
  it("creates a player support ticket once and replays by idempotency key", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const guestBody = await guestResponse.json() as { account: { id: string }; accessToken: string };
      const authorization = `Bearer ${guestBody.accessToken}`;
      const requestBody = {
        category: "RUN_REWARD",
        body: "보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.",
        appVersion: "1.0.0-dev",
        appVersionCode: "42",
        clientComposedAt: "2026-08-30T00:00:05.000Z",
        clientSubmittedAt: "2026-08-30T00:00:06.000Z",
      };

      const response = await app.request(
        "/v1/support/tickets",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Content-Type": "application/json",
            "Idempotency-Key": "support-ticket-smoke-1",
          },
          body: JSON.stringify(requestBody),
        },
        env,
      );

      expect(response.status).toBe(200);
      const body = await response.json() as { ticketId: string; bodyPreview: string };
      expect(body).toMatchObject({
        accepted: true,
        ticketStatus: "RECEIVED",
        category: "RUN_REWARD",
        bodyPreview: "보상 결과가 예상과 달라 고객 지원 확인을 요청합니다.",
        appVersion: "1.0.0-dev",
        appVersionCode: "42",
        clientComposedAt: "2026-08-30T00:00:05.000Z",
        clientSubmittedAt: "2026-08-30T00:00:06.000Z",
        serverReceivedAt: "2026-08-30T00:00:00.000Z",
        errorCode: null,
        message: null,
      });
      expect(body.ticketId).toMatch(/^support_ticket_/u);
      expect(JSON.stringify(body)).not.toContain("accessToken");
      expect(JSON.stringify(body)).not.toContain("refreshToken");
      expect(JSON.stringify(body)).not.toContain("Authorization");
      expect(JSON.stringify(body)).not.toContain("raw");

      const replayResponse = await app.request(
        "/v1/support/tickets",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Content-Type": "application/json",
            "Idempotency-Key": "support-ticket-smoke-1",
          },
          body: JSON.stringify(requestBody),
        },
        env,
      );
      expect(replayResponse.status).toBe(200);
      expect(await replayResponse.json()).toMatchObject({
        accepted: true,
        ticketStatus: "DUPLICATE_REPLAY",
        ticketId: body.ticketId,
        errorCode: "duplicate_replay",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM support_tickets;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'support_ticket_create';")).toBe(1);

      const conflictResponse = await app.request(
        "/v1/support/tickets",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Content-Type": "application/json",
            "Idempotency-Key": "support-ticket-smoke-1",
          },
          body: JSON.stringify({ ...requestBody, body: "같은 키로 다른 문의 내용을 보내는 경우입니다." }),
        },
        env,
      );
      expect(conflictResponse.status).toBe(409);
      expect(await conflictResponse.json()).toMatchObject({
        accepted: false,
        ticketStatus: "REJECTED",
        errorCode: "idempotency_conflict",
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects account shortcuts and invalid support ticket payloads", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);
    const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
    const guestBody = await guestResponse.json() as { accessToken: string };
    const authorization = `Bearer ${guestBody.accessToken}`;

    const queryResponse = await app.request(
      "/v1/support/tickets?accountId=account-other",
        {
          method: "POST",
          headers: { Authorization: authorization, "Content-Type": "application/json", "Idempotency-Key": "support-ticket-query" },
        body: JSON.stringify({ category: "BUG_OTHER", body: "쿼리 계정 단축값 거부 확인용 문의입니다." }),
      },
      env,
    );
    expect(queryResponse.status).toBe(400);
    expect(await queryResponse.json()).toMatchObject({ errorCode: "contract_violation" });

    const bodyShortcutResponse = await app.request(
      "/v1/support/tickets",
        {
          method: "POST",
          headers: { Authorization: authorization, "Content-Type": "application/json", "Idempotency-Key": "support-ticket-body" },
        body: JSON.stringify({ accountId: "account-other", category: "BUG_OTHER", body: "본문 계정 단축값 거부 확인용 문의입니다." }),
      },
      env,
    );
    expect(bodyShortcutResponse.status).toBe(400);
    expect(await bodyShortcutResponse.json()).toMatchObject({ errorCode: "contract_violation" });

    const invalidResponse = await app.request(
      "/v1/support/tickets",
        {
          method: "POST",
          headers: { Authorization: authorization, "Content-Type": "application/json", "Idempotency-Key": "support-ticket-invalid" },
        body: JSON.stringify({ category: "NOT_A_CATEGORY", body: "짧음" }),
      },
      env,
    );
    expect(invalidResponse.status).toBe(400);
    expect(await invalidResponse.json()).toMatchObject({
      accepted: false,
      ticketStatus: "REJECTED",
      errorCode: "support_ticket_invalid",
    });
  });
});
