import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";

type SqlValue = string | number | Uint8Array | null;

class SqlJsD1Statement {
  private values: SqlValue[] = [];

  constructor(private readonly database: initSqlJs.Database, readonly sql: string) {}

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
const fixedNow = Date.parse("2026-09-06T00:00:00.000Z");

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[123][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(35);

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

describe("game-api auth response-loss replay local D1 smoke", () => {
  it("rejects weak auth idempotency keys before creating or rotating sessions", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);

    const guestResponse = await app.request(
      "/v1/auth/guest",
      { method: "POST", headers: { "Idempotency-Key": "short-key" } },
      env,
    );
    expect(guestResponse.status).toBe(400);
    expect(await guestResponse.json()).toMatchObject({
      errorCode: "contract_violation",
    });
    expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(0);
  });

  it("replays guest creation by Idempotency-Key without creating another guest", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const request = {
        method: "POST",
        headers: { "Idempotency-Key": "auth-guest-create-replay-1" },
      };

      const firstResponse = await app.request("/v1/auth/guest", request, env);
      const secondResponse = await app.request("/v1/auth/guest", request, env);

      expect(firstResponse.status).toBe(200);
      expect(secondResponse.status).toBe(200);
      const first = await firstResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      const second = await secondResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      expect(second.account.id).toBe(first.account.id);
      expect(second.accessToken).toBe(first.accessToken);
      expect(second.refreshToken).toBe(first.refreshToken);
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'auth_guest_create';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("collapses concurrent same-key guest creation into one account and one session", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const createRequest = () => ({
        method: "POST",
        headers: { "Idempotency-Key": "auth-guest-create-concurrent-key-1" },
      });

      const [firstResponse, secondResponse] = await Promise.all([
        app.request("/v1/auth/guest", createRequest(), env),
        app.request("/v1/auth/guest", createRequest(), env),
      ]);

      expect(firstResponse.status).toBe(200);
      expect(secondResponse.status).toBe(200);
      const first = await firstResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      const second = await secondResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      expect(second.account.id).toBe(first.account.id);
      expect(second.accessToken).toBe(first.accessToken);
      expect(second.refreshToken).toBe(first.refreshToken);
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'auth_guest_create';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("does not reissue a guest replay bundle after the replay session expired", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const request = {
        method: "POST",
        headers: { "Idempotency-Key": "auth-guest-create-replay-expired-key-1" },
      };

      const firstResponse = await app.request("/v1/auth/guest", request, env);
      expect(firstResponse.status).toBe(200);
      db.run("UPDATE auth_sessions SET expires_at = ?;", [fixedNow - 1]);

      const secondResponse = await app.request("/v1/auth/guest", request, env);

      expect(secondResponse.status).toBe(401);
      expect(await secondResponse.json()).toMatchObject({
        errorCode: "auth_replay_expired",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions;")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("replays refresh rotation by Idempotency-Key without marking the family reused", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const guest = await guestResponse.json() as { account: { id: string }; refreshToken: string };
      const request = {
        method: "POST",
        headers: { "Content-Type": "application/json", "Idempotency-Key": "auth-refresh-replay-1" },
        body: JSON.stringify({ refreshToken: guest.refreshToken }),
      };

      const firstResponse = await app.request("/v1/auth/refresh", request, env);
      const secondResponse = await app.request("/v1/auth/refresh", request, env);

      expect(firstResponse.status).toBe(200);
      expect(secondResponse.status).toBe(200);
      const first = await firstResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      const second = await secondResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      expect(second.account.id).toBe(guest.account.id);
      expect(second.accessToken).toBe(first.accessToken);
      expect(second.refreshToken).toBe(first.refreshToken);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'REUSED';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'ROTATED';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'ACTIVE';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'auth_refresh';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("collapses concurrent same-key refresh rotation into one active rotated bundle", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const guest = await guestResponse.json() as { account: { id: string }; refreshToken: string };
      const createRequest = () => ({
        method: "POST",
        headers: { "Content-Type": "application/json", "Idempotency-Key": "auth-refresh-concurrent-key-1" },
        body: JSON.stringify({ refreshToken: guest.refreshToken }),
      });

      const [firstResponse, secondResponse] = await Promise.all([
        app.request("/v1/auth/refresh", createRequest(), env),
        app.request("/v1/auth/refresh", createRequest(), env),
      ]);

      expect(firstResponse.status).toBe(200);
      expect(secondResponse.status).toBe(200);
      const first = await firstResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      const second = await secondResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };
      expect(first.account.id).toBe(guest.account.id);
      expect(second.accessToken).toBe(first.accessToken);
      expect(second.refreshToken).toBe(first.refreshToken);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'REUSED';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'ROTATED';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'ACTIVE';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'auth_refresh';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("does not mark a refresh family reused when the committed replay session expired", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const guest = await guestResponse.json() as { refreshToken: string };
      const request = {
        method: "POST",
        headers: { "Content-Type": "application/json", "Idempotency-Key": "auth-refresh-replay-expired-key-1" },
        body: JSON.stringify({ refreshToken: guest.refreshToken }),
      };

      const firstResponse = await app.request("/v1/auth/refresh", request, env);
      expect(firstResponse.status).toBe(200);
      db.run("UPDATE auth_sessions SET expires_at = ? WHERE status = 'ACTIVE';", [fixedNow - 1]);

      const secondResponse = await app.request("/v1/auth/refresh", request, env);

      expect(secondResponse.status).toBe(401);
      expect(await secondResponse.json()).toMatchObject({
        errorCode: "auth_replay_expired",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM auth_sessions WHERE status = 'REUSED';")).toBe(0);
    } finally {
      vi.useRealTimers();
    }
  });
});
