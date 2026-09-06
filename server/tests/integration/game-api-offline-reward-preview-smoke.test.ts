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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(20);

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
      const initialOfflineState = db.exec(
        `SELECT last_story_progress_id, last_balance_config_version, last_narrative_version,
                last_previewed_at, revision
         FROM account_offline_reward_state
         WHERE account_id = '${guestBody.account.id}'`,
      );
      expect(initialOfflineState[0]?.values).toEqual([
        ["none", null, "act1.2026-08-27.001", null, 0],
      ]);

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
      db.run(
        `UPDATE account_offline_reward_state
         SET last_offline_reward_claim_at = ?,
             last_story_progress_id = 'opn_06_broken_line',
             last_narrative_version = 'act1.2026-08-27.001',
             revision = 1,
             updated_at = ?
         WHERE account_id = ?;`,
        [fixedNow - 3 * 60 * 60 * 1000, fixedNow - 3 * 60 * 60 * 1000, guestBody.account.id],
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
        claimableSeconds: 10800,
        claimableHours: 3,
        offlineStorageCapHours: 10,
        minimumClaimSeconds: 600,
        maxClaimableHours: 12,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 720, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 360, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 216, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 144, source: "OFFLINE_REWARD" },
        ],
        calculation: {
          storyProgressId: "opn_06_broken_line",
          storyProgressMultiplierLabel: "2.40x",
          cappedOfflineSeconds: 10800,
          capSource: "ACTUAL_OFFLINE_DURATION",
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

  it("commits an offline reward claim once and replays duplicate idempotency without granting again", async () => {
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
        `UPDATE account_offline_reward_state
         SET last_offline_reward_claim_at = ?,
             last_story_progress_id = 'opn_06_broken_line',
             last_narrative_version = 'act1.2026-08-27.001',
             revision = 1,
             updated_at = ?
         WHERE account_id = ?;`,
        [fixedNow - 3 * 60 * 60 * 1000, fixedNow - 3 * 60 * 60 * 1000, guestBody.account.id],
      );
      db.run(
        `INSERT INTO account_story_progress (
           account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
           cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
         ) VALUES (?, 'act_01_wasteland_railway', 'opn_06_broken_line', 'opn_06_broken_line', ?, ?, ?, NULL, ?);`,
        [
          guestBody.account.id,
          JSON.stringify(["opn_06_broken_line"]),
          JSON.stringify(["operation_cleared:opn_06_broken_line"]),
          "act1.2026-08-27.001",
          fixedNow,
        ],
      );

      const response = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Idempotency-Key": "offline-claim-smoke-1",
          },
        },
        env,
      );

      expect(response.status).toBe(200);
      const body = await response.json() as {
        offlineRewardClaimId: string;
        rewardTransactionId: string;
        economyTransactionId: string;
        accountMutationId: string;
        rewardLines: Array<{ id: string; quantity: number; balanceAfter: number }>;
      };
      expect(body).toMatchObject({
        accepted: true,
        claimStatus: "CLAIMED",
        storyProgressId: "opn_06_broken_line",
        claimableSeconds: 10800,
        claimableHours: 3,
        offlineStorageCapHours: 10,
        minimumClaimSeconds: 600,
        maxClaimableHours: 12,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 720, source: "OFFLINE_REWARD", balanceAfter: 720 },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 360, source: "OFFLINE_REWARD", balanceAfter: 360 },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 216, source: "OFFLINE_REWARD", balanceAfter: 216 },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 144, source: "OFFLINE_REWARD", balanceAfter: 144 },
        ],
        calculation: {
          storyProgressId: "opn_06_broken_line",
          storyProgressMultiplierLabel: "2.40x",
          cappedOfflineSeconds: 10800,
          capSource: "ACTUAL_OFFLINE_DURATION",
        },
        narrativeVersion: "act1.2026-08-27.001",
        serverClaimedAt: "2026-08-28T00:00:00.000Z",
        errorCode: null,
        message: null,
      });
      expect(body.offlineRewardClaimId).toBe(body.rewardTransactionId);
      expect(body.economyTransactionId).toMatch(/^economy_/u);
      expect(body.accountMutationId).toMatch(/^mutation_/u);
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

      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE account_id LIKE 'account_%' AND run_id IS NULL;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM economy_transactions WHERE reason = 'OFFLINE_REWARD_CLAIM';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM currency_ledger WHERE account_id LIKE 'account_%';")).toBe(4);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(2);

      const replay = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: authorization,
            "Idempotency-Key": "offline-claim-smoke-1",
          },
        },
        env,
      );
      expect(replay.status).toBe(200);
      await expect(replay.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "DUPLICATE_REPLAY",
        offlineRewardClaimId: body.offlineRewardClaimId,
        rewardTransactionId: body.rewardTransactionId,
        rewardLines: [],
        errorCode: "duplicate_replay",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE account_id LIKE 'account_%' AND run_id IS NULL;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM economy_transactions WHERE reason = 'OFFLINE_REWARD_CLAIM';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM currency_ledger WHERE account_id LIKE 'account_%';")).toBe(4);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it("treats the same offline claim idempotency key as independent across accounts", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      const firstGuestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const secondGuestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      const firstGuest = await firstGuestResponse.json() as { account: { id: string }; accessToken: string };
      const secondGuest = await secondGuestResponse.json() as { account: { id: string }; accessToken: string };

      for (const accountId of [firstGuest.account.id, secondGuest.account.id]) {
        db.run(
          `UPDATE account_offline_reward_state
           SET last_offline_reward_claim_at = ?,
               updated_at = ?
           WHERE account_id = ?;`,
          [fixedNow - 60 * 60 * 1000, fixedNow - 60 * 60 * 1000, accountId],
        );
      }

      for (const accessToken of [firstGuest.accessToken, secondGuest.accessToken]) {
        const response = await app.request(
          "/v1/offline-rewards/claim",
          {
            method: "POST",
            headers: {
              Authorization: `Bearer ${accessToken}`,
              "Idempotency-Key": "same-key-different-accounts",
            },
          },
          env,
        );
        expect(response.status).toBe(200);
        await expect(response.json()).resolves.toMatchObject({
          accepted: true,
          claimStatus: "CLAIMED",
          storyProgressId: "none",
          claimableSeconds: 3600,
        });
      }

      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'offline_reward_claim';")).toBe(2);
      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE run_id IS NULL;")).toBe(2);
      expect(scalar(db, "SELECT COUNT(*) FROM currency_ledger;")).toBe(8);
    } finally {
      vi.useRealTimers();
    }
  });

  it("lazily prepares missing offline state during a successful claim", async () => {
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
        "UPDATE accounts SET created_at = ?, updated_at = ? WHERE id = ?;",
        [fixedNow - 60 * 60 * 1000, fixedNow - 60 * 60 * 1000, guestBody.account.id],
      );
      db.run("DELETE FROM account_offline_reward_state WHERE account_id = ?;", [guestBody.account.id]);

      const response = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "offline-lazy-state-claim-1",
          },
        },
        env,
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toMatchObject({
        accepted: true,
        claimStatus: "CLAIMED",
        storyProgressId: "none",
        claimableSeconds: 3600,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 100, source: "OFFLINE_REWARD", balanceAfter: 100 },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 50, source: "OFFLINE_REWARD", balanceAfter: 50 },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 30, source: "OFFLINE_REWARD", balanceAfter: 30 },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 20, source: "OFFLINE_REWARD", balanceAfter: 20 },
        ],
      });
      expect(scalar(db, "SELECT COUNT(*) FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(1);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects empty offline reward claims without advancing state", async () => {
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

      const response = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "offline-empty-claim-smoke-1",
          },
        },
        env,
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "REJECTED",
        offlineRewardClaimId: null,
        claimableSeconds: 0,
        rewardLines: [],
        errorCode: "offline_reward_not_claimable",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE run_id IS NULL;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'offline_reward_claim';")).toBe(0);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(0);
    } finally {
      vi.useRealTimers();
    }
  });
});
