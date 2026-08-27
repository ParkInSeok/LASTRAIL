import { describe, expect, it } from "vitest";
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

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-4])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(14);

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

describe("game-api story progress local D1 smoke", () => {
  it("returns default and stored story progress from authenticated account context only", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);

    const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
    expect(guestResponse.status).toBe(200);
    const guestBody = await guestResponse.json() as {
      account: { id: string };
      accessToken: string;
    };
    const authorization = `Bearer ${guestBody.accessToken}`;

    const defaultResponse = await app.request(
      "/v1/story-progress",
      { method: "GET", headers: { Authorization: authorization } },
      env,
    );
    expect(defaultResponse.status).toBe(200);
    await expect(defaultResponse.json()).resolves.toMatchObject({
      accepted: true,
      storyProgress: {
        highestStoryProgressId: "none",
        highestClearedOperationId: null,
        order: 0,
      },
      clearedOperationIds: [],
      unlockedOperations: ["opn_01_last_departure"],
      narrativeVersion: "act1.2026-08-27.001",
    });

    const now = Date.parse("2026-08-27T00:00:00.000Z");
    db.run(
      `INSERT INTO account_story_progress (
         account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
         cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
       ) VALUES (?, 'act_01_wasteland_railway', 'opn_02_first_signal', 'opn_02_first_signal', ?, ?, ?, NULL, ?);`,
      [
        guestBody.account.id,
        JSON.stringify(["opn_01_last_departure", "opn_02_first_signal"]),
        JSON.stringify(["operation_cleared:opn_02_first_signal"]),
        "act1.2026-08-27.001",
        now,
      ],
    );
    db.run(
      `INSERT INTO account_unlocks (account_id, unlock_id, unlocked_at)
       VALUES (?, 'operation:opn_03_water_stop', ?);`,
      [guestBody.account.id, now],
    );

    const storedResponse = await app.request(
      "/v1/story-progress",
      { method: "GET", headers: { Authorization: authorization } },
      env,
    );
    expect(storedResponse.status).toBe(200);
    const storedBody = await storedResponse.json();
    expect(storedBody).toMatchObject({
      accepted: true,
      actId: "act_01_wasteland_railway",
      operationId: "opn_02_first_signal",
      storyProgress: {
        highestStoryProgressId: "opn_02_first_signal",
        highestClearedOperationId: "opn_02_first_signal",
        order: 2,
      },
      clearedOperationIds: ["opn_01_last_departure", "opn_02_first_signal"],
      narrativeFlags: ["operation_cleared:opn_02_first_signal"],
      unlockedOperations: ["opn_01_last_departure", "opn_03_water_stop"],
      updatedAt: "2026-08-27T00:00:00.000Z",
    });

    const forbiddenQueryResponse = await app.request(
      `/v1/story-progress?accountId=${guestBody.account.id}`,
      { method: "GET", headers: { Authorization: authorization } },
      env,
    );
    expect(forbiddenQueryResponse.status).toBe(400);
    await expect(forbiddenQueryResponse.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "contract_violation",
    });

    const serialized = JSON.stringify(storedBody);
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("accountId");
    expect(serialized).not.toContain("secret");
  });
});
