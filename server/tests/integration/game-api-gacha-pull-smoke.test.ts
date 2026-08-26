import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

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
    AUTH_SIGNING_KEY_CURRENT: "test-secret-minimum-32-characters",
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

async function createAuthorizationHeader(): Promise<string> {
  const token = await createAccessToken({
    secret: "test-secret-minimum-32-characters",
    accountId: "account-gacha-smoke-1",
    sessionId: "session-gacha-smoke-1",
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function seedBaseGachaState(db: initSqlJs.Database, options: { alreadyOwnsPickup: boolean; tickets: number }): void {
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-gacha-smoke-1', 'ACTIVE', 1, 1);");
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES ('session-gacha-smoke-1', 'account-gacha-smoke-1', 'hash-gacha-smoke-1', 'family-gacha-smoke-1', 0, 'ACTIVE', 1, 4102444800000, NULL);`,
  );
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-gacha-smoke-1', NULL, 1, 1, '{}', 'checksum-gacha-smoke-1', 1, 'PUBLISHED', 'test', 1, 1
     );`,
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (
       301, 1, 1, 'checksum-gacha-smoke-1', '{}', 1, 'test', 'draft-gacha-smoke-1'
     );`,
  );
  db.run(
    `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
     VALUES ('account-gacha-smoke-1', 'cur_command_ticket', ${options.tickets}, 1);`,
  );
  db.run(
    `INSERT INTO gacha_banners (
       id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
     ) VALUES (
       'banner-gacha-smoke-1',
       'PICKUP',
       'ACTIVE',
       301,
       'op_vex',
       1,
       NULL,
       '{"elite":30,"pickup":60,"rare":10}',
       '{"results":[{"resultRarity":"ELITE","resultType":"ELITE_OPERATOR","resultId":"op_vex","weight":1,"isPickup":true}]}',
       1,
       1
     );`,
  );

  if (options.alreadyOwnsPickup) {
    db.run(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES ('mutation-existing-op-1', 'account-gacha-smoke-1', 1, 'SEED_OPERATOR', 'seed-existing-op-1', 1);`,
    );
    db.run(
      `INSERT INTO account_operators (account_id, operator_id, skill_training, promotion, acquired_at, revision, last_mutation_id)
       VALUES ('account-gacha-smoke-1', 'op_vex', 1, 0, 1, 1, 'mutation-existing-op-1');`,
    );
  }
}

function scalar(db: initSqlJs.Database, sql: string): unknown {
  return db.exec(sql)[0]?.values[0]?.[0];
}

describe("game-api gacha pull local D1 smoke", () => {
  it("commits an ACTIVE banner pull by spending command tickets and acquiring a new operator", async () => {
    const db = await createMigratedDb();
    seedBaseGachaState(db, { alreadyOwnsPickup: false, tickets: 1 });

    const response = await app.request(
      "/v1/gacha/banners/banner-gacha-smoke-1/pulls",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "gacha-smoke-idem-new-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({ pullCount: 1 }),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accepted: true,
      resultStatus: "ACCEPTED",
      bannerId: "banner-gacha-smoke-1",
      pullCount: 1,
      cost: { itemId: "cur_command_ticket", quantity: 1 },
      results: [
        {
          pullSequence: 1,
          resultRarity: "ELITE",
          resultType: "ELITE_OPERATOR",
          resultId: "op_vex",
          reward: { kind: "OPERATOR", id: "op_vex", quantity: 1 },
          isPickup: true,
          guaranteeType: "NONE",
          wasDuplicate: false,
          duplicateConversion: null,
        },
      ],
      postPityState: {
        totalPullCount: 1,
        elitePityCount: 0,
        pickupElitePityCount: 0,
        rareOrHigherPityCount: 0,
        revision: 1,
      },
      errorCode: null,
      message: null,
    });
    expect(body.batchId).toMatch(/^gacha_batch_/u);

    expect(scalar(db, "SELECT quantity FROM account_items WHERE account_id = 'account-gacha-smoke-1' AND item_id = 'cur_command_ticket';")).toBe(0);
    expect(scalar(db, "SELECT COUNT(*) FROM account_operators WHERE account_id = 'account-gacha-smoke-1' AND operator_id = 'op_vex';")).toBe(1);
    expect(scalar(db, "SELECT COUNT(*) FROM progression_ledger WHERE account_id = 'account-gacha-smoke-1';")).toBe(1);
    expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_batches WHERE account_id = 'account-gacha-smoke-1';")).toBe(1);
    expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_results WHERE account_id = 'account-gacha-smoke-1';")).toBe(1);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });

  it("replays a duplicate operator pull without spending tickets or granting shards again", async () => {
    const db = await createMigratedDb();
    seedBaseGachaState(db, { alreadyOwnsPickup: true, tickets: 2 });

    const request = {
      method: "POST",
      headers: {
        "Idempotency-Key": "gacha-smoke-idem-dupe-1",
        Authorization: await createAuthorizationHeader(),
      },
      body: JSON.stringify({ pullCount: 1 }),
    };
    const firstResponse = await app.request("/v1/gacha/banners/banner-gacha-smoke-1/pulls", request, createEnv(db));

    expect(firstResponse.status).toBe(200);
    const firstBody = await firstResponse.json();
    expect(firstBody).toMatchObject({
      accepted: true,
      resultStatus: "ACCEPTED",
      results: [
        {
          resultType: "ELITE_OPERATOR",
          resultId: "op_vex",
          reward: {
            kind: "ITEM",
            id: "cur_operator_token",
            operatorId: "op_vex",
            quantity: 50,
          },
          wasDuplicate: true,
          duplicateConversion: {
            sourceResultType: "ELITE_OPERATOR",
            sourceResultId: "op_vex",
            itemId: "cur_operator_token",
            operatorId: "op_vex",
            quantity: 50,
          },
        },
      ],
    });

    const secondResponse = await app.request("/v1/gacha/banners/banner-gacha-smoke-1/pulls", request, createEnv(db));
    expect(secondResponse.status).toBe(200);
    await expect(secondResponse.json()).resolves.toMatchObject({
      accepted: false,
      resultStatus: "DUPLICATE_REPLAY",
      batchId: firstBody.batchId,
      errorCode: "duplicate_replay",
      results: firstBody.results,
      postPityState: firstBody.postPityState,
    });

    expect(scalar(db, "SELECT quantity FROM account_items WHERE account_id = 'account-gacha-smoke-1' AND item_id = 'cur_command_ticket';")).toBe(1);
    expect(scalar(db, "SELECT quantity FROM account_items WHERE account_id = 'account-gacha-smoke-1' AND item_id = 'cur_operator_token:op_vex';")).toBe(50);
    expect(scalar(db, "SELECT COUNT(*) FROM item_ledger WHERE account_id = 'account-gacha-smoke-1';")).toBe(2);
    expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_batches WHERE account_id = 'account-gacha-smoke-1';")).toBe(1);
    expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_results WHERE account_id = 'account-gacha-smoke-1';")).toBe(1);
    expect(scalar(db, "SELECT total_pull_count FROM player_gacha_pity WHERE account_id = 'account-gacha-smoke-1' AND banner_id = 'banner-gacha-smoke-1';")).toBe(1);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });
});
