import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/admin-api/src/index";

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
}

class SqlJsD1Database {
  private readonly database: initSqlJs.Database;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

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
  expect(migrations).toHaveLength(27);

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

function seedAdminAndStoryProgress(db: initSqlJs.Database): void {
  const now = Date.parse("2026-08-27T00:00:00.000Z");
  db.run(
    `INSERT INTO admin_principals (
       id, access_subject, role, status, display_name, created_at, updated_at
     ) VALUES ('principal-story-1', 'access|story-viewer', 'VIEWER', 'ACTIVE', 'Story Viewer', ?, ?);`,
    [now, now],
  );
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-story-admin-1', 'ACTIVE', ?, ?);", [
    now,
    now,
  ]);
  db.run(
    `INSERT INTO account_story_progress (
       account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
       cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
     ) VALUES (?, 'act_01_wasteland_railway', 'opn_04_scrapstar_show', 'opn_04_scrapstar_show', ?, ?, ?, NULL, ?);`,
    [
      "account-story-admin-1",
      JSON.stringify([
        "opn_01_last_departure",
        "opn_02_first_signal",
        "opn_03_water_stop",
        "opn_04_scrapstar_show",
      ]),
      JSON.stringify(["operation_cleared:opn_04_scrapstar_show"]),
      "act1.2026-08-27.001",
      now,
    ],
  );
  db.run(
    `INSERT INTO account_unlocks (account_id, unlock_id, unlocked_at)
     VALUES ('account-story-admin-1', 'operation:opn_05_night_boarders', ?);`,
    [now],
  );
}

describe("admin-api story progress local D1 smoke", () => {
  it("reads story progress list and detail without mutating migrated D1 state", async () => {
    const db = await createMigratedDb();
    seedAdminAndStoryProgress(db);
    const env = createEnv(db);
    const headers = { "Cf-Access-Subject": "access|story-viewer" };

    const listResponse = await app.request(
      "/v1/admin/story-progress?actId=act_01_wasteland_railway&limit=20",
      { headers },
      env,
    );
    expect(listResponse.status).toBe(200);
    await expect(listResponse.json()).resolves.toMatchObject({
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          accountId: "account-story-admin-1",
          storyProgress: {
            highestStoryProgressId: "opn_04_scrapstar_show",
            order: 4,
          },
        },
      ],
      nextCursor: null,
    });

    const detailResponse = await app.request(
      "/v1/admin/story-progress/account-story-admin-1",
      { headers },
      env,
    );
    expect(detailResponse.status).toBe(200);
    const detailBody = await detailResponse.json();
    expect(detailBody).toMatchObject({
      accountId: "account-story-admin-1",
      operationId: "opn_04_scrapstar_show",
      unlockedOperations: ["opn_01_last_departure", "opn_05_night_boarders"],
      supportContext: {
        source: "account_story_progress",
        readOnly: true,
        mutationEndpointsEnabled: false,
      },
    });

    const serialized = JSON.stringify(detailBody);
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("secret");

    const integrity = db.exec("PRAGMA foreign_key_check;");
    const quickCheck = db.exec("PRAGMA quick_check;");
    expect(integrity).toEqual([]);
    expect(quickCheck[0]?.values).toEqual([["ok"]]);
  });
});
