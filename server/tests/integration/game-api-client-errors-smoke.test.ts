import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import gameApi from "../../apps/game-api/src/index";
import adminApi from "../../apps/admin-api/src/index";

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
  expect(migrations).toHaveLength(24);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createGameEnv(db: initSqlJs.Database): Env & { AUTH_SIGNING_KEY_CURRENT: string } {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function createAdminEnv(db: initSqlJs.Database): Env {
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

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

describe("game-api frontend-safe client error ingest local D1 smoke", () => {
  it("stores bounded Unity diagnostics and exposes them through Admin frontend filters", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const gameEnv = createGameEnv(db);
      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      const guestBody = await guestResponse.json() as { account: { id: string }; accessToken: string };
      db.run(
        `INSERT INTO admin_principals (
           id, access_subject, role, status, display_name, created_at, updated_at
         ) VALUES ('principal-client-error-1', 'access|client-error-operator', 'OPERATOR', 'ACTIVE', 'Ops Operator', ?, ?);`,
        [fixedNow, fixedNow],
      );

      const ingestResponse = await gameApi.request(
        "/v1/client-errors",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Content-Type": "application/json",
            "Idempotency-Key": "client-error-smoke-1",
          },
          body: JSON.stringify({
            events: [
              {
                kind: "UNITY_EXCEPTION",
                occurredAt: "2026-08-30T00:00:00.000Z",
                occurrenceCount: 3,
                appVersion: "1.0.0-dev",
                appVersionCode: "42",
                scope: "AUTHENTICATED_CURRENT_SESSION",
              },
            ],
          }),
        },
        gameEnv,
      );
      expect(ingestResponse.status).toBe(200);
      const ingestBody = await ingestResponse.json() as { ingestId: string };
      expect(ingestBody).toMatchObject({
        accepted: true,
        ingestStatus: "RECEIVED",
        acceptedEventCount: 1,
        droppedEventCount: 0,
        errorCode: null,
        message: null,
      });
      expect(ingestBody.ingestId).toMatch(/^client_error_ingest_/u);

      const replayResponse = await gameApi.request(
        "/v1/client-errors",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Content-Type": "application/json",
            "Idempotency-Key": "client-error-smoke-1",
          },
          body: JSON.stringify({
            events: [
              {
                kind: "UNITY_EXCEPTION",
                occurredAt: "2026-08-30T00:00:00.000Z",
                occurrenceCount: 3,
                appVersion: "1.0.0-dev",
                appVersionCode: "42",
                scope: "AUTHENTICATED_CURRENT_SESSION",
              },
            ],
          }),
        },
        gameEnv,
      );
      expect(replayResponse.status).toBe(200);
      expect(await replayResponse.json()).toMatchObject({
        accepted: true,
        ingestStatus: "DUPLICATE_REPLAY",
        ingestId: ingestBody.ingestId,
        errorCode: "duplicate_replay",
      });

      const adminResponse = await adminApi.request(
        "/v1/admin/ops/error-logs?category=frontend&appVersion=1.0.0-dev&appVersionCode=42&accountId=" + guestBody.account.id,
        { headers: { "Cf-Access-Subject": "access|client-error-operator" } },
        createAdminEnv(db),
      );
      expect(adminResponse.status).toBe(200);
      const adminBody = await adminResponse.json() as { collectionAvailable: boolean; items: Array<Record<string, unknown>> };
      expect(adminBody.collectionAvailable).toBe(true);
      expect(adminBody.items).toHaveLength(1);
      expect(adminBody.items[0]).toMatchObject({
        service: "game_api",
        source: "game_api",
        severity: "warning",
        errorCode: "frontend_unity_exception",
        accountId: guestBody.account.id,
        appVersion: "1.0.0-dev",
        appVersionCode: "42",
        retryable: false,
        masked: true,
      });
      const serialized = JSON.stringify(adminBody);
      expect(serialized).not.toContain("Authorization");
      expect(serialized).not.toContain("accessToken");
      expect(serialized).not.toContain("refreshToken");
      expect(serialized).not.toContain("rawLog");
      expect(serialized).not.toContain("stack");
      expect(serialized).not.toContain("password");
      expect(serialized).not.toContain("probability");
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects raw client log payloads and account shortcuts", async () => {
    const db = await createMigratedDb();
    const gameEnv = createGameEnv(db);
    const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
    const guestBody = await guestResponse.json() as { accessToken: string };
    const headers = {
      Authorization: `Bearer ${guestBody.accessToken}`,
      "Content-Type": "application/json",
      "Idempotency-Key": "client-error-invalid-1",
    };

    const response = await gameApi.request(
      "/v1/client-errors",
      {
        method: "POST",
        headers,
        body: JSON.stringify({
          accountId: "account-other",
          events: [
            {
              kind: "UNITY_ERROR",
              occurredAt: "2026-08-30T00:00:00.000Z",
              occurrenceCount: 1,
              rawLog: "do not accept me",
              stack: "do not accept me",
              scope: "AUTHENTICATED_CURRENT_SESSION",
            },
          ],
        }),
      },
      gameEnv,
    );

    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({
      accepted: false,
      ingestStatus: "REJECTED",
      errorCode: "contract_violation",
    });
  });

  it("rate limits new frontend diagnostic batches per account without blocking idempotency replay", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const gameEnv = createGameEnv(db);
      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      const guestBody = await guestResponse.json() as { account: { id: string }; accessToken: string };
      const event = {
        kind: "UNITY_ERROR",
        occurredAt: "2026-08-30T00:00:00.000Z",
        occurrenceCount: 1,
        appVersion: "1.0.0-dev",
        appVersionCode: "42",
        scope: "AUTHENTICATED_CURRENT_SESSION",
      };
      let firstIngestId = "";

      for (let index = 1; index <= 12; index += 1) {
        const response = await gameApi.request(
          "/v1/client-errors",
          {
            method: "POST",
            headers: {
              Authorization: `Bearer ${guestBody.accessToken}`,
              "Content-Type": "application/json",
              "Idempotency-Key": `client-error-rate-${index}`,
            },
            body: JSON.stringify({ events: [{ ...event, occurrenceCount: index }] }),
          },
          gameEnv,
        );
        expect(response.status).toBe(200);
        const body = await response.json() as { ingestId: string };
        if (index === 1) firstIngestId = body.ingestId;
      }

      const limitedResponse = await gameApi.request(
        "/v1/client-errors",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Content-Type": "application/json",
            "Idempotency-Key": "client-error-rate-13",
          },
          body: JSON.stringify({ events: [{ ...event, occurrenceCount: 13 }] }),
        },
        gameEnv,
      );
      expect(limitedResponse.status).toBe(429);
      expect(limitedResponse.headers.get("Retry-After")).toBe("600");
      expect(await limitedResponse.json()).toMatchObject({
        accepted: false,
        ingestStatus: "REJECTED",
        retryAfterSeconds: 600,
        errorCode: "client_error_ingest_rate_limited",
      });
      expect(scalar(db, `SELECT COUNT(*) FROM idempotency_records WHERE account_id = '${guestBody.account.id}' AND operation = 'client_error_ingest';`)).toBe(12);
      expect(scalar(db, `SELECT COUNT(*) FROM ops_error_logs WHERE account_id = '${guestBody.account.id}' AND idempotency_operation = 'client_error_ingest';`)).toBe(12);

      const replayResponse = await gameApi.request(
        "/v1/client-errors",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Content-Type": "application/json",
            "Idempotency-Key": "client-error-rate-1",
          },
          body: JSON.stringify({ events: [{ ...event, occurrenceCount: 1 }] }),
        },
        gameEnv,
      );
      expect(replayResponse.status).toBe(200);
      expect(await replayResponse.json()).toMatchObject({
        accepted: true,
        ingestStatus: "DUPLICATE_REPLAY",
        ingestId: firstIngestId,
        errorCode: "duplicate_replay",
      });
      expect(scalar(db, `SELECT COUNT(*) FROM idempotency_records WHERE account_id = '${guestBody.account.id}' AND operation = 'client_error_ingest';`)).toBe(12);
    } finally {
      vi.useRealTimers();
    }
  });

  it("reads client error ingest receipt by Idempotency-Key after response loss without resending events", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const gameEnv = createGameEnv(db);
      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      const guestBody = await guestResponse.json() as { account: { id: string }; accessToken: string };
      const createResponse = await gameApi.request(
        "/v1/client-errors",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Content-Type": "application/json",
            "Idempotency-Key": "client-error-receipt-1",
          },
          body: JSON.stringify({
            events: [
              {
                kind: "UNITY_ASSERT",
                occurredAt: "2026-08-30T00:00:00.000Z",
                occurrenceCount: 2,
                scope: "AUTHENTICATED_CURRENT_SESSION",
              },
            ],
          }),
        },
        gameEnv,
      );
      const created = await createResponse.json() as { ingestId: string };

      const receiptResponse = await gameApi.request(
        "/v1/client-errors/receipt",
        {
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "client-error-receipt-1",
          },
        },
        gameEnv,
      );
      expect(receiptResponse.status).toBe(200);
      expect(await receiptResponse.json()).toMatchObject({
        accepted: true,
        receiptStatus: "FOUND",
        ingestId: created.ingestId,
        acceptedEventCount: 1,
        errorCode: null,
        message: null,
      });

      const missingResponse = await gameApi.request(
        "/v1/client-errors/receipt",
        {
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "client-error-receipt-missing",
          },
        },
        gameEnv,
      );
      expect(missingResponse.status).toBe(404);
      expect(await missingResponse.json()).toMatchObject({
        accepted: false,
        receiptStatus: "NOT_FOUND",
        ingestId: null,
        errorCode: "client_error_ingest_receipt_not_found",
      });
    } finally {
      vi.useRealTimers();
    }
  });
});
