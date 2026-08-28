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

describe("game-api offline reward preview local D1 smoke", () => {
  it("returns server-calculated offline reward preview from authenticated account story progress", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);

      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      expect(guestResponse.status).toBe(200);
      const guestBody = await guestResponse.json() as {
        account: { id: string };
        accessToken: string;
      };
      const authorization = `Bearer ${guestBody.accessToken}`;

      db.run(
        "UPDATE accounts SET created_at = ?, updated_at = ? WHERE id = ?;",
        [fixedNow - 20 * 60 * 60 * 1000, fixedNow, guestBody.account.id],
      );
      db.run(
        `INSERT INTO account_story_progress (
           account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
           cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
         ) VALUES (?, 'act_01_wasteland_railway', 'opn_06_broken_line', 'opn_06_broken_line', ?, ?, ?, NULL, ?);`,
        [
          guestBody.account.id,
          JSON.stringify([
            "opn_01_last_departure",
            "opn_02_first_signal",
            "opn_03_water_stop",
            "opn_04_scrapstar_show",
            "opn_05_night_boarders",
            "opn_06_broken_line",
          ]),
          JSON.stringify(["operation_cleared:opn_06_broken_line"]),
          "act1.2026-08-27.001",
          fixedNow,
        ],
      );

      const response = await app.request(
        "/v1/offline-rewards/preview",
        { method: "GET", headers: { Authorization: authorization } },
        env,
      );
      expect(response.status).toBe(200);
      const body = await response.json();
      expect(body).toMatchObject({
        accepted: true,
        previewStatus: "AVAILABLE",
        storyProgressId: "opn_06_broken_line",
        claimableSeconds: 36000,
        claimableHours: 10,
        offlineStorageCapHours: 10,
        minimumClaimSeconds: 600,
        maxClaimableHours: 12,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 2400, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 1200, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 720, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 480, source: "OFFLINE_REWARD" },
        ],
        calculation: {
          storyProgressId: "opn_06_broken_line",
          storyProgressMultiplierLabel: "2.40x",
          cappedOfflineSeconds: 36000,
          capSource: "STORY_PROGRESS_STORAGE_CAP",
        },
        narrativeVersion: "act1.2026-08-27.001",
        serverCalculatedAt: "2026-08-28T00:00:00.000Z",
        errorCode: null,
        message: null,
      });

      const serialized = JSON.stringify(body);
      expect(serialized).not.toContain("cur_special_core");
      expect(serialized).not.toContain("cur_command_ticket");
      expect(serialized).not.toContain("cur_command_ticket_fragment");
      expect(serialized).not.toContain("accessToken");
      expect(serialized).not.toContain("refreshToken");
      expect(serialized).not.toContain("rawRng");
      expect(serialized).not.toContain("storyProgressMultiplierBps");
      expect(serialized).not.toContain("accountId");
      expect(serialized).not.toContain("secret");
    } finally {
      vi.useRealTimers();
    }
  });
});
