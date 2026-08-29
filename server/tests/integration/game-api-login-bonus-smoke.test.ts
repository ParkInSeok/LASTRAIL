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
const fixedNow = Date.parse("2026-08-30T00:00:00.000Z");

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-7])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(17);

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

describe("game-api launch login bonus local D1 smoke", () => {
  it("commits the launch command ticket login bonus once and replays by idempotency key", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      expect(guestResponse.status).toBe(200);
      const guestBody = await guestResponse.json() as { account: { id: string }; accessToken: string };
      const authorization = `Bearer ${guestBody.accessToken}`;

      const claimResponse = await app.request(
        "/v1/login-bonus/claim",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Idempotency-Key": "launch-login-bonus-smoke-1",
          },
        },
        env,
      );

      expect(claimResponse.status).toBe(200);
      const claimBody = await claimResponse.json() as {
        loginBonusClaimId: string;
        rewardTransactionId: string;
        economyTransactionId: string;
        accountMutationId: string;
        rewardLines: Array<{ kind: string; id: string; quantity: number; source: string; balanceAfter: number }>;
      };
      expect(claimBody).toMatchObject({
        accepted: true,
        claimStatus: "CLAIMED",
        bonusId: "launch_login_bonus_act1_v1",
        rewardLines: [
          { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS", balanceAfter: 5 },
        ],
        serverClaimedAt: "2026-08-30T00:00:00.000Z",
        errorCode: null,
        message: null,
      });
      expect(claimBody.loginBonusClaimId).toBe(claimBody.rewardTransactionId);
      expect(claimBody.economyTransactionId).toMatch(/^economy_/u);
      expect(claimBody.accountMutationId).toMatch(/^mutation_/u);
      const serialized = JSON.stringify(claimBody);
      expect(serialized).not.toContain("accessToken");
      expect(serialized).not.toContain("refreshToken");
      expect(serialized).not.toContain("rawRng");
      expect(serialized).not.toContain("probability");
      expect(serialized).not.toContain("fullWeights");
      expect(serialized).not.toContain("accountId");
      expect(serialized).not.toContain("secret");

      expect(scalar(db, `SELECT quantity FROM account_items WHERE account_id = '${guestBody.account.id}' AND item_id = 'cur_command_ticket';`)).toBe(5);
      expect(scalar(db, "SELECT COUNT(*) FROM item_ledger WHERE reason = 'LAUNCH_LOGIN_BONUS';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM account_login_bonus_claims WHERE bonus_id = 'launch_login_bonus_act1_v1';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'login_bonus_claim';")).toBe(1);

      const replayResponse = await app.request(
        "/v1/login-bonus/claim",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Idempotency-Key": "launch-login-bonus-smoke-1",
          },
        },
        env,
      );
      expect(replayResponse.status).toBe(200);
      await expect(replayResponse.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "DUPLICATE_REPLAY",
        bonusId: "launch_login_bonus_act1_v1",
        loginBonusClaimId: claimBody.loginBonusClaimId,
        rewardLines: [],
        errorCode: "duplicate_replay",
      });
      expect(scalar(db, `SELECT quantity FROM account_items WHERE account_id = '${guestBody.account.id}' AND item_id = 'cur_command_ticket';`)).toBe(5);
      expect(scalar(db, "SELECT COUNT(*) FROM item_ledger WHERE reason = 'LAUNCH_LOGIN_BONUS';")).toBe(1);

      const alreadyClaimedResponse = await app.request(
        "/v1/login-bonus/claim",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Idempotency-Key": "launch-login-bonus-smoke-2",
          },
        },
        env,
      );
      expect(alreadyClaimedResponse.status).toBe(200);
      await expect(alreadyClaimedResponse.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "ALREADY_CLAIMED",
        bonusId: "launch_login_bonus_act1_v1",
        loginBonusClaimId: claimBody.loginBonusClaimId,
        rewardLines: [],
        errorCode: "already_claimed",
      });
      expect(scalar(db, `SELECT quantity FROM account_items WHERE account_id = '${guestBody.account.id}' AND item_id = 'cur_command_ticket';`)).toBe(5);
      expect(scalar(db, "SELECT COUNT(*) FROM item_ledger WHERE reason = 'LAUNCH_LOGIN_BONUS';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'login_bonus_claim';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects missing idempotency key or caller-supplied query/body fields", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);
    const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
    const guestBody = await guestResponse.json() as { accessToken: string };
    const authorization = `Bearer ${guestBody.accessToken}`;

    const missingKey = await app.request(
      "/v1/login-bonus/claim",
      { method: "POST", headers: { Authorization: authorization } },
      env,
    );
    expect(missingKey.status).toBe(400);
    await expect(missingKey.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      errorCode: "contract_violation",
    });

    const queryResponse = await app.request(
      "/v1/login-bonus/claim?accountId=account_other",
      {
        method: "POST",
        headers: {
          Authorization: authorization,
          "Idempotency-Key": "launch-login-bonus-query",
        },
      },
      env,
    );
    expect(queryResponse.status).toBe(400);
    await expect(queryResponse.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      errorCode: "contract_violation",
    });

    const bodyResponse = await app.request(
      "/v1/login-bonus/claim",
      {
        method: "POST",
        headers: {
          Authorization: authorization,
          "Idempotency-Key": "launch-login-bonus-body",
          "content-length": "21",
        },
        body: JSON.stringify({ rewardAmount: 5 }),
      },
      env,
    );
    expect(bodyResponse.status).toBe(400);
    await expect(bodyResponse.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      errorCode: "contract_violation",
    });
  });
});
