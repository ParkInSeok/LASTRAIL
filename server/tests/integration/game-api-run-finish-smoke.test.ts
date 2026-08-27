import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken, hashRefreshToken } from "../../apps/game-api/src/auth";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";

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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-3])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(13);

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

async function seedFinishableRun(db: initSqlJs.Database): Promise<string> {
  const now = Date.now();
  const refreshTokenHash = await hashRefreshToken("refresh-smoke-token");
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES (?, 'ACTIVE', ?, ?);", [
    "account-smoke-1",
    now,
    now,
  ]);
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL);`,
    ["session-smoke-1", "account-smoke-1", refreshTokenHash, "family-smoke-1", now, now + 900000],
  );
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-smoke-1', NULL, 1, 1, '{}', 'checksum-smoke-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [now, now],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-smoke-1', ?, ?, 'test', 'draft-smoke-1');`,
    [1, JSON.stringify({ runRewards: launchRunRewardsConfig }), now],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-run-smoke-1', 'account-smoke-1', 1, 'RUN_START', 'idem-run-start-smoke-1', ?);`,
    [now],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 1, ?, 'CONSUMED', ?, ?);`,
    [
      "preview-smoke-1",
      "account-smoke-1",
      "mutation-run-smoke-1",
      "opn_03_water_stop",
      "III",
      JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
      }),
      now + 900000,
      now,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES (?, ?, ?, ?, 1, 'ACTIVE', ?, NULL, NULL);`,
    ["run-smoke-1", "account-smoke-1", "preview-smoke-1", "mutation-run-smoke-1", now],
  );

  return `Bearer ${await createAccessToken({
    secret: authSecret,
    accountId: "account-smoke-1",
    sessionId: "session-smoke-1",
    nowMs: now,
    ttlMs: 900000,
  })}`;
}

describe("game-api run finish local D1 smoke", () => {
  it("creates a reward preview once from an accepted run finish", async () => {
    const db = await createMigratedDb();
    const authorization = await seedFinishableRun(db);

    const response = await app.request(
      "/v1/runs/run-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify({
          runId: "run-smoke-1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run-smoke-1",
      accepted: true,
      resultStatus: "ACCEPTED",
      rewardClaimId: null,
      balanceVersion: "1",
      errorCode: null,
      message: null,
    });
    expect(body.rewardPreviewId).toMatch(/^reward_preview_/u);
    expect(db.exec("SELECT outcome, cleared_waves FROM run_final_results WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([
      ["VICTORY", 15],
    ]);
    expect(db.exec("SELECT status FROM runs WHERE id = 'run-smoke-1';")[0]?.values).toEqual([["COMPLETED"]]);
    const previewRows = db.exec(
      "SELECT status, balance_config_version, reward_plan_json FROM run_reward_previews WHERE run_id = 'run-smoke-1';",
    );
    expect(previewRows[0]?.values.length).toBe(1);
    const rewardPlanJson = String(previewRows[0]?.values[0]?.[2]);
    const rewardPlan = JSON.parse(rewardPlanJson) as {
      rewardLines: Array<{ source?: string }>;
      calculation: { risk: string; firstClearEligible: boolean; firstClearApplied: boolean };
    };
    expect(rewardPlan.rewardLines.length).toBeGreaterThan(0);
    expect(rewardPlan.rewardLines.some((line) => line.source === "FIRST_CLEAR")).toBe(true);
    expect(rewardPlan.calculation.risk).toBe("III");
    expect(rewardPlan.calculation.firstClearEligible).toBe(true);
    expect(rewardPlan.calculation.firstClearApplied).toBe(true);
    expect(
      db.exec(
        "SELECT operation_key, risk, run_id FROM account_first_clears WHERE account_id = 'account-smoke-1';",
      )[0]?.values,
    ).toEqual([["opn_03_water_stop", "III", "run-smoke-1"]]);
    expect(db.exec("SELECT operation, result_ref FROM idempotency_records WHERE account_id = 'account-smoke-1';")[0]?.values).toEqual([
      ["run_finish", body.rewardPreviewId],
    ]);

    const replayResponse = await app.request(
      "/v1/runs/run-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify({
          runId: "run-smoke-1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      createEnv(db),
    );

    expect(replayResponse.status).toBe(200);
    await expect(replayResponse.json()).resolves.toMatchObject({
      runId: "run-smoke-1",
      accepted: false,
      resultStatus: "DUPLICATE_REPLAY",
      rewardPreviewId: body.rewardPreviewId,
      balanceVersion: "1",
      errorCode: "duplicate_replay",
    });
    expect(db.exec("SELECT COUNT(*) FROM run_final_results WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("SELECT COUNT(*) FROM run_reward_previews WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });
});
