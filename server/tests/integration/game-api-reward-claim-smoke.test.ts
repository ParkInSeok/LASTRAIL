import { createAccessToken } from "../../apps/game-api/src/auth";
const authSecret = "test-secret-minimum-32-characters";
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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[123][0-9]|4[0-2])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(42);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env & {AUTH_SIGNING_KEY_CURRENT:string} {
  return {
    ENVIRONMENT: "DEV",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
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
  it.each(["natural-expiry", "late-concurrent-winner"])("handles %s between preview read and commit", async (schedule) => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);
    db.run("INSERT INTO auth_sessions (id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES ('session-schedule','account-smoke-1','schedule-refresh','schedule-family',0,'ACTIVE',?,?)", [Date.now(),Date.now()+900000]);
    const token = await createAccessToken({secret:authSecret,accountId:"account-smoke-1",sessionId:"session-schedule",nowMs:Date.now(),ttlMs:900000});
    const ordinary = createEnv(db);
    const request = (env: Env, key: string) => app.request("/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim", {
      method:"POST",headers:{Authorization:`Bearer ${token}`,"Idempotency-Key":key},
      body:JSON.stringify({runId:"run-smoke-1",rewardPreviewId:"reward-preview-smoke-1"}),
    },env);
    const adapter = new SqlJsD1Database(db);
    const prepare = adapter.prepare.bind(adapter);
    let winnerId: string | undefined;
    adapter.prepare = sql => {
      const statement = prepare(sql);
      if (schedule === "late-concurrent-winner" && sql.includes("MAX(sequence)")) {
        const first = statement.first.bind(statement);
        statement.first = async <T>() => {
          const winner = await request(ordinary,"winner-key");
          expect(winner.status).toBe(200);
          winnerId = (await winner.json()).rewardClaimId;
          return first<T>();
        };
      }
      return statement;
    };
    if (schedule === "natural-expiry") {
      db.run("UPDATE run_reward_previews SET created_at=?,expires_at=?",[Date.now()-1000,Date.now()+250]);
      const batch = adapter.batch.bind(adapter);
      adapter.batch = async statements => { await new Promise(resolve=>setTimeout(resolve,400)); return batch(statements); };
    }
    try {
      const response = await request({...ordinary,DB:adapter as unknown as D1Database},"scheduled-key");
      if (schedule === "natural-expiry") {
        expect(response.status).toBe(409);
        expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[0]]);
        expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[100]]);
      } else {
        expect(response.status).toBe(200);
        expect(await response.json()).toMatchObject({status:"DUPLICATE_REPLAY",rewardClaimId:winnerId});
        expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[1]]);
        expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[1300]]);
      }
    } finally {db.close();}
  });

  it("concurrent claim keys grant once and both callers recover the same receipt", async () => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);
    db.run("INSERT INTO auth_sessions (id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES ('session-concurrent','account-smoke-1','concurrent-refresh','concurrent-family',0,'ACTIVE',?,?)", [Date.now(),Date.now()+900000]);
    const token = await createAccessToken({secret:authSecret,accountId:"account-smoke-1",sessionId:"session-concurrent",nowMs:Date.now(),ttlMs:900000});
    const env = createEnv(db);
    const claim = (key: string) => app.request("/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim", {
      method:"POST",headers:{Authorization:`Bearer ${token}`,"Idempotency-Key":key},
      body:JSON.stringify({runId:"run-smoke-1",rewardPreviewId:"reward-preview-smoke-1"}),
    },env);
    try {
      const responses = await Promise.all([claim("parallel-a"),claim("parallel-b")]);
      expect(responses.map(r=>r.status)).toEqual([200,200]);
      const results = await Promise.all(responses.map(r=>r.json()));
      expect(results.map(r=>r.status).sort()).toEqual(["CLAIMED","DUPLICATE_REPLAY"]);
      expect(results[0].rewardClaimId).toBe(results[1].rewardClaimId);
      expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[1300]]);
      expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[1]]);
      expect(db.exec("SELECT COUNT(*) FROM currency_ledger")[0]?.values).toEqual([[1]]);
    } finally {db.close();}
  });

  it("rolls back wallet and receipt together on a late write failure, then retries once", async () => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);
    db.run("INSERT INTO auth_sessions (id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES ('session-rollback','account-smoke-1','rollback-refresh','rollback-family',0,'ACTIVE',?,?)", [Date.now(),Date.now()+900000]);
    const token = await createAccessToken({secret:authSecret,accountId:"account-smoke-1",sessionId:"session-rollback",nowMs:Date.now(),ttlMs:900000});
    const env = createEnv(db);
    db.run("CREATE TRIGGER fail_claim_receipt BEFORE INSERT ON idempotency_records WHEN NEW.operation='run_reward_claim' BEGIN SELECT RAISE(ABORT, 'injected_receipt_failure'); END");
    const claim = () => app.request("/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim", {
      method:"POST",headers:{Authorization:`Bearer ${token}`,"Idempotency-Key":"rollback-key"},
      body:JSON.stringify({runId:"run-smoke-1",rewardPreviewId:"reward-preview-smoke-1"}),
    },env);
    try {
      expect((await claim()).status).toBe(500);
      expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[100]]);
      expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[0]]);
      expect(db.exec("SELECT COUNT(*) FROM currency_ledger")[0]?.values).toEqual([[0]]);
      expect(db.exec("SELECT status FROM run_reward_previews")[0]?.values).toEqual([["ACTIVE"]]);
      db.run("DROP TRIGGER fail_claim_receipt");
      expect((await claim()).status).toBe(200);
      expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[1300]]);
      expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[1]]);
    } finally {db.close();}
  });

  it.each(["status", "plan", "expiry", "owner"])("does not pay a preview whose %s changes before commit", async (change) => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);
    db.run("INSERT INTO auth_sessions (id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES ('session-fence','account-smoke-1','fence-refresh','fence-family',0,'ACTIVE',?,?)", [Date.now(),Date.now()+900000]);
    const token = await createAccessToken({secret:authSecret,accountId:"account-smoke-1",sessionId:"session-fence",nowMs:Date.now(),ttlMs:900000});
    const adapter = new SqlJsD1Database(db);
    const originalBatch = adapter.batch.bind(adapter);
    adapter.batch = async (statements) => {
      if (statements.some(s => s.sql.includes("INSERT INTO reward_transactions"))) {
        if (change === "status") db.run("UPDATE run_reward_previews SET status='INVALIDATED'");
        if (change === "plan") db.run("UPDATE run_reward_previews SET reward_plan_json='{}'");
        if (change === "expiry") db.run("UPDATE run_reward_previews SET created_at=0,expires_at=1");
        if (change === "owner") {
          db.run("INSERT INTO accounts(id,status,created_at,updated_at) VALUES('other-account','ACTIVE',1,1)");
          db.run("UPDATE run_reward_previews SET account_id='other-account'");
        }
      }
      return originalBatch(statements);
    };
    try {
      const response = await app.request("/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim", {
        method:"POST", headers:{Authorization:`Bearer ${token}`,"Idempotency-Key":"fence-claim"},
        body:JSON.stringify({runId:"run-smoke-1",rewardPreviewId:"reward-preview-smoke-1"}),
      }, {...createEnv(db),DB:adapter as unknown as D1Database});
      expect(response.status).toBe(409);
      expect(await response.json()).toMatchObject({errorCode:"reward_preview_changed"});
      expect(db.exec("SELECT balance FROM account_currencies WHERE currency='CREDITS'")[0]?.values).toEqual([[100]]);
      expect(db.exec("SELECT COUNT(*) FROM reward_transactions")[0]?.values).toEqual([[0]]);
      expect(db.exec("SELECT COUNT(*) FROM currency_ledger")[0]?.values).toEqual([[0]]);
    } finally { db.close(); }
  });

  it("commits an ACTIVE preview claim once and keeps D1 integrity clean", async () => {
    const db = await createMigratedDb();
    seedClaimableRewardPreview(db);
    db.run("INSERT INTO auth_sessions (id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES ('session-smoke-1','account-smoke-1','test-refresh','test-family',0,'ACTIVE',?,?)",[Date.now(),Date.now()+900000]);
    const authorization = `Bearer ${await createAccessToken({secret:authSecret,accountId:"account-smoke-1",sessionId:"session-smoke-1",nowMs:Date.now(),ttlMs:900000})}`;

    const response = await app.request(
      "/v1/runs/run-smoke-1/reward-preview/reward-preview-smoke-1/claim",
      {
        method: "POST",
        headers: { Authorization: authorization, "Idempotency-Key": "claim-smoke-idem-1" },
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
        headers: { Authorization: authorization, "Idempotency-Key": "claim-smoke-idem-1" },
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
