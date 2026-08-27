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

function createEnv(db: initSqlJs.Database): Env {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function seedClaimableRewardPreview(db: initSqlJs.Database): void {
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-smoke-1', 'ACTIVE', 1, 1);");
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-smoke-1', NULL, 1, 1, '{}', 'checksum-smoke-1', 1, 'PUBLISHED', 'test', 1, 1
     );`,
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (
       1, 1, 1, 'checksum-smoke-1', '{}', 1, 'test', 'draft-smoke-1'
     );`,
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-run-smoke-1', 'account-smoke-1', 1, 'RUN_START', 'idem-run-start-smoke-1', 1);`,
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES (
       'run-smoke-1', 'account-smoke-1', NULL, 'mutation-run-smoke-1', 1, 'FINISH_PENDING', 1, NULL, NULL
     );`,
  );
  db.run(
    `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
     VALUES ('account-smoke-1', 'CREDITS', 100, 1);`,
  );
  db.run(
    `INSERT INTO run_reward_previews (
       id, account_id, run_id, balance_config_version, status, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     ) VALUES (
       'reward-preview-smoke-1',
       'account-smoke-1',
       'run-smoke-1',
       1,
       'ACTIVE',
       '{"rewardLines":[{"kind":"CURRENCY","id":"cur_credits","quantity":1200,"source":"RUN_CLEAR"}]}',
       1798156800000,
       1798070400000,
       NULL,
       NULL
     );`,
  );
}

describe("game-api reward claim local D1 smoke", () => {
  it("commits an ACTIVE preview claim once and keeps D1 integrity clean", async () => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);

    const response = await app.request(
      "/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim",
      {
        method: "POST",
        headers: { "Idempotency-Key": "claim-smoke-idem-1" },
        body: JSON.stringify({ runId: "run-smoke-1", rewardPreviewId: "reward-preview-smoke-1" }),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run-smoke-1",
      rewardPreviewId: "reward-preview-smoke-1",
      status: "CLAIMED",
      balanceVersion: "1",
      rewardLines: [
        {
          kind: "CURRENCY",
          id: "cur_credits",
          quantity: 1200,
          source: "RUN_CLEAR",
          balanceAfter: 1300,
        },
      ],
      errorCode: null,
      message: null,
    });
    expect(body.rewardClaimId).toMatch(/^reward_claim_/u);

    const secondResponse = await app.request(
      "/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim",
      {
        method: "POST",
        headers: { "Idempotency-Key": "claim-smoke-idem-1" },
        body: JSON.stringify({ runId: "run-smoke-1", rewardPreviewId: "reward-preview-smoke-1" }),
      },
      createEnv(db),
    );
    await expect(secondResponse.json()).resolves.toMatchObject({
      rewardClaimId: body.rewardClaimId,
      status: "DUPLICATE_REPLAY",
      errorCode: "duplicate_replay",
    });

    expect(db.exec("SELECT balance FROM account_currencies WHERE account_id = 'account-smoke-1' AND currency = 'CREDITS';")[0]?.values).toEqual([[1300]]);
    expect(db.exec("SELECT COUNT(*) FROM reward_transactions WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("SELECT COUNT(*) FROM currency_ledger WHERE account_id = 'account-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("SELECT operation, result_ref FROM idempotency_records WHERE account_id = 'account-smoke-1';")[0]?.values).toEqual([
      ["run_reward_claim", body.rewardClaimId],
    ]);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });
});
