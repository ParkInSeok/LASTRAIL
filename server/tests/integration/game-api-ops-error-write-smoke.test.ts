import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";

type SqlValue = string | number | Uint8Array | null;

class SqlJsD1Statement {
  readonly sql: string;
  private readonly database: initSqlJs.Database;
  private values: SqlValue[] = [];

  constructor(database: initSqlJs.Database, sql: string) {
    this.database = database;
    this.sql = sql;
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
      while (statement.step()) {
        results.push(statement.getAsObject() as T);
      }
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
  private readonly database: initSqlJs.Database;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.database.run("BEGIN;");
    try {
      for (const statement of statements) {
        statement.run();
      }
      this.database.run("COMMIT;");
      return statements.map(() => ({ success: true }));
    } catch (error) {
      this.database.run("ROLLBACK;");
      throw error;
    }
  }
}

const authSecret = "test-secret-minimum-32-characters";
const fixedNow = Date.parse("2026-08-28T00:00:00.000Z");

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-6])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(16);

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

function firstRow<T extends Record<string, unknown>>(db: initSqlJs.Database, sql: string): T {
  const result = db.exec(sql);
  const columns = result[0]?.columns ?? [];
  const values = result[0]?.values[0] ?? [];
  return Object.fromEntries(columns.map((column, index) => [column, values[index]])) as T;
}

describe("game-api minimal ops error write integration", () => {
  it("writes a scrubbed ops error when Google auth config is missing", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);

      const response = await app.request(
        "/v1/auth/google",
        {
          method: "POST",
          headers: { "X-Request-Id": "req_google_config_missing_1" },
          body: JSON.stringify({ idToken: "raw-google-id-token-value" }),
        },
        env,
      );

      expect(response.status).toBe(500);
      await expect(response.json()).resolves.toEqual({
        errorCode: "google_auth_not_configured",
        message: "Google auth verifier is not configured.",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs;")).toBe(1);
      expect(firstRow(db, "SELECT environment, service, severity, error_code, status, source, request_id, masked FROM ops_error_logs;")).toEqual({
        environment: "DEV",
        service: "game_api",
        severity: "critical",
        error_code: "google_auth_config_missing",
        status: "open",
        source: "game_api",
        request_id: "req_google_config_missing_1",
        masked: 1,
      });
      const serializedLog = JSON.stringify(db.exec("SELECT * FROM ops_error_logs;"));
      expect(serializedLog).not.toContain("raw-google-id-token-value");
      expect(serializedLog).not.toContain("idToken");
      expect(serializedLog).not.toContain("Authorization");
    } finally {
      vi.useRealTimers();
    }
  });

  it("writes an account-correlated ops error for offline reward state conflicts without consuming claim state", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const guestBody = await guestResponse.json() as {
        account: { id: string };
        accessToken: string;
      };
      db.run(
        `UPDATE account_offline_reward_state
         SET last_offline_reward_claim_at = ?, revision = 7, updated_at = ?
         WHERE account_id = ?;`,
        [fixedNow + 60_000, fixedNow, guestBody.account.id],
      );

      const response = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "offline-conflict-idem-1",
            "X-Request-Id": "req_offline_conflict_1",
          },
        },
        env,
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "REJECTED",
        offlineRewardClaimId: null,
        errorCode: "offline_reward_state_conflict",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE run_id IS NULL;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'offline_reward_claim';")).toBe(0);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(7);
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'offline_reward_state_conflict';")).toBe(1);

      const log = firstRow(db, "SELECT account_id, idempotency_operation, idempotency_key_hash, retryable, masked FROM ops_error_logs WHERE error_code = 'offline_reward_state_conflict';");
      expect(log).toMatchObject({
        account_id: guestBody.account.id,
        idempotency_operation: "offline_reward_claim",
        retryable: 0,
        masked: 1,
      });
      expect(String(log.idempotency_key_hash)).toMatch(/^sha256:/u);
      expect(String(log.idempotency_key_hash)).not.toContain("offline-conflict-idem-1");
    } finally {
      vi.useRealTimers();
    }
  });
});
