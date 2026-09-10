import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import type { PassPurchaseVerifier } from "../../apps/game-api/src/activity-service";

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
  beforeBatch?: () => void;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.beforeBatch?.();
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
const fixedNow = Date.parse("2026-09-06T19:00:00.000Z"); // Monday 04:00 KST.

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.create_function("unixepoch", (modifier: string) => modifier === "subsec" ? Date.now() / 1000 : Math.floor(Date.now() / 1000));
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

const reward = [{ id: "cur_credits", quantity: 7 }]; // TEST ONLY, never a launch seed.
const attendance = {
  kind: "ATTENDANCE", startAt: fixedNow, endAt: fixedNow + 10 * 86400000,
  claimUntil: fixedNow + 10 * 86400000, resetOffsetMinutes: 1140,
  counting: "DISTINCT_DAYS", dailyRewards: reward, recovery: null,
  milestones: [{ days: 2, rewards: [{ id: "cur_machine_parts", quantity: 3 }] }],
};
const pass = {
  kind: "PASS", startAt: fixedNow, endAt: fixedNow + 10 * 86400000,
  claimUntil: fixedNow + 11 * 86400000,
  levels: [{ xp: 10, free: reward, premium: [{ id: "cur_special_core", quantity: 2 }] }],
  runXp: { VICTORY: 10 },
  purchase: { provider: "GOOGLE_PLAY", appId: "test.app", productId: "test.pass" },
};

async function setup(config?: object, id = "test-activity") {
  const db = await createMigratedDb();
  const env = createEnv(db);
  if (config) db.run("INSERT INTO activity_definitions (id, policy_json) VALUES (?, ?)", [id, JSON.stringify(config)]);
  const guest = await (await app.request("/v1/auth/guest", { method: "POST" }, env)).json() as { account: { id: string }; accessToken: string };
  async function request(path: string, body?: object, key = "test-key", authenticated = true, method = "POST") {
    const sessionId = String(db.exec("SELECT id FROM auth_sessions")[0].values[0][0]);
    const token = await createAccessToken({ secret: authSecret, accountId: guest.account.id, sessionId, nowMs: Date.now(), ttlMs: 900000 });
    return app.request(path, {
      method, headers: {
        ...(authenticated ? { Authorization: `Bearer ${token}` } : {}),
        "Idempotency-Key": key, "Content-Type": "application/json",
      }, ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    }, env);
  }
  return { db, env, guest, request };
}

function seedAcceptedRun(db: initSqlJs.Database, accountId: string, runId = "test-run", status = "COMPLETED", outcome = "VICTORY") {
  db.run("INSERT OR IGNORE INTO balance_config_versions (version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES (1,1,0,'TEST','{}',0,'TEST')");
  const sequence = scalar(db, "SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations");
  db.run("INSERT INTO account_mutations (id,account_id,sequence,mutation_type,created_at) VALUES (?,?,?,'TEST_RUN',?)", [`mutation-${runId}`, accountId, sequence, Date.now()]);
  db.run("INSERT INTO runs (id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES (?,?,?,1,?,?,?)", [runId, accountId, `mutation-${runId}`, status, fixedNow, Date.now()]);
  db.run("INSERT INTO run_final_results (run_id,outcome,cleared_waves,payload_json,submitted_at) VALUES (?,?,15,'{}',?)", [runId, outcome, Date.now()]);
}

function installTestVerifier(env: Env, overrides: Record<string, unknown> = {}) {
  const verify: PassPurchaseVerifier = async (input) => ({
    provider: input.provider, appId: input.appId, productId: input.productId,
    transactionId: "TEST-store-transaction", accountId: input.accountId, activityId: input.activityId,
    environment: input.environment, verifiedAt: input.now, purchasedAt: input.now,
    expiresAt: pass.claimUntil, revoked: false, ...overrides,
  });
  (env as Env & { PASS_PURCHASE_VERIFIER: PassPurchaseVerifier }).PASS_PURCHASE_VERIFIER = verify;
}

describe("attendance and pass foundations (explicit TEST policies)", () => {
  it("atomically pays configured TEST inventory for a missed day and prevents double recovery", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow + 86400000);
    try {
      const recovery = { itemId: "test_paid_currency", quantity: 5, maxPerCampaign: 2, missedDaysOnly: true };
      const { db, guest, request } = await setup({ ...attendance, recovery });
      const missedDay = Math.floor((fixedNow - 1140 * 60000) / 86400000);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay }, "recover")).status).toBe(409);
      db.run("INSERT INTO account_items (account_id,item_id,quantity,updated_at) VALUES (?,'test_paid_currency',10,?)", [guest.account.id, Date.now()]);
      const recovered = await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay }, "recover");
      expect(recovered.status).toBe(200);
      expect(await recovered.json()).toMatchObject({ totalDays: 1, cost: { id: "test_paid_currency", quantity: 5, balanceAfter: 5 } });
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay }, "recover")).status).toBe(200);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay }, "other")).status).toBe(409);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay + 1 }, "today")).status).toBe(409);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: missedDay - 1 }, "before-campaign")).status).toBe(409);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='test_paid_currency'")).toBe(5);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(1);
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "today-visit")).status).toBe(200);
      expect((await request("/v1/attendance/test-activity/milestones/claim", { days: 2 }, "milestone")).status).toBe(200);
      expect(scalar(db, "SELECT count(*) FROM account_login_bonus_claims")).toBe(0);
    } finally { vi.useRealTimers(); }
  });
  it("registers authenticated routes and fails closed without definitions", async () => {
    const { db, request } = await setup();
    expect((await request("/v1/attendance/test-activity/check-in", undefined, "a", false)).status).toBe(401);
    expect((await request("/v1/attendance/test-activity/check-in")).status).toBe(503);
    expect(scalar(db, "SELECT count(*) FROM reward_transactions")).toBe(0);
  });

  it("grants one daily reward, replays, counts nonconsecutive visits and claims a milestone once", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, request } = await setup(attendance);
      const first = await request("/v1/attendance/test-activity/check-in");
      expect(first.status).toBe(200);
      const receipt = await first.json();
      expect(receipt).toMatchObject({ accepted: true, totalDays: 1, rewardLines: [{ id: "cur_credits", quantity: 7 }] });
      expect(await (await request("/v1/attendance/test-activity/check-in")).json()).toEqual(receipt);
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "different-key")).status).toBe(409);
      expect((await request("/v1/attendance/test-activity/milestones/claim", { days: 2 }, "milestone")).status).toBe(409);
      // Refresh session by server-supported refresh would obscure the time-boundary test;
      // obtain a new access token for this same account with the existing signing helper below.
      vi.setSystemTime(fixedNow + 2 * 86400000);
      const tokenModule = await import("../../apps/game-api/src/auth");
      const accountId = String(db.exec("SELECT id FROM accounts")[0].values[0][0]);
      const sessionId = String(db.exec("SELECT id FROM auth_sessions")[0].values[0][0]);
      const token = await tokenModule.createAccessToken({ secret: authSecret, accountId, sessionId, nowMs: Date.now(), ttlMs: 900000 });
      const post = (path: string, body?: object, key = "day-three") => app.request(path, { method: "POST", headers: { Authorization: `Bearer ${token}`, "Idempotency-Key": key, "Content-Type": "application/json" }, ...(body ? { body: JSON.stringify(body) } : {}) }, createEnv(db));
      expect((await post("/v1/attendance/test-activity/check-in")).status).toBe(200);
      expect((await post("/v1/attendance/test-activity/milestones/claim", { days: 2 }, "milestone")).status).toBe(200);
      expect((await post("/v1/attendance/test-activity/milestones/claim", { days: 2 }, "another")).status).toBe(409);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='cur_credits'")).toBe(14);
      expect(scalar(db, "SELECT count(*) FROM account_login_bonus_claims")).toBe(0);
      expect(scalar(db, "SELECT count(*) FROM item_ledger")).toBe(3);
    } finally { vi.useRealTimers(); }
  });

  it("rejects caller time/XP/premium fields, unavailable premium verifier and unearned rewards", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, request } = await setup(pass);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "FREE", premium: true })).status).toBe(400);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "FREE" })).status).toBe(409);
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "unverified", productId: "test.pass" })).status).toBe(503);
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "forged", xp: 1000 })).status).toBe(400);
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "forged" })).status).toBe(409);
      expect(scalar(db, "SELECT count(*) FROM reward_transactions")).toBe(0);
      expect(scalar(db, "SELECT count(*) FROM pass_entitlements")).toBe(0);
    } finally { vi.useRealTimers(); }
  });

  it("uses KST 04:00 boundaries and refuses a new check-in exactly at campaign end", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow - 1);
    try {
      const { db, request } = await setup({ ...attendance, startAt: fixedNow - 86400000 });
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "before")).status).toBe(200);
      vi.setSystemTime(fixedNow);
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "at-reset")).status).toBe(200);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(2);
      vi.setSystemTime(attendance.endAt);
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "end")).status).toBe(409);
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "before")).status).toBe(200); // Historical replay.
    } finally { vi.useRealTimers(); }
  });

  it("deduplicates concurrent check-ins and rolls back all writes on a ledger failure", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, request } = await setup(attendance);
      const responses = await Promise.all([request("/v1/attendance/test-activity/check-in"), request("/v1/attendance/test-activity/check-in")]);
      expect(responses.map((r) => r.status)).toEqual([200, 200]);
      expect(await responses[0].json()).toEqual(await responses[1].json());
      expect(scalar(db, "SELECT count(*) FROM item_ledger")).toBe(1);
      vi.setSystemTime(fixedNow + 86400000);
      db.run("CREATE TRIGGER test_fail_ledger BEFORE INSERT ON item_ledger BEGIN SELECT RAISE(ABORT, 'injected ledger fault'); END");
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "retryable")).status).toBe(500);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(1);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='cur_credits'")).toBe(7);
      db.run("DROP TRIGGER test_fail_ledger");
      expect((await request("/v1/attendance/test-activity/check-in", undefined, "retryable")).status).toBe(200);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(2);
      expect(db.exec("PRAGMA foreign_key_check")).toEqual([]);
    } finally { vi.useRealTimers(); }
  });

  it("derives XP only from the authenticated owner's accepted run and deduplicates the event", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, guest, request } = await setup(pass);
      seedAcceptedRun(db, guest.account.id);
      const first = await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "xp");
      expect(first.status).toBe(200);
      expect(await first.json()).toMatchObject({ xp: 10, level: 1, rewardLines: [] });
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "xp")).status).toBe(200);
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "new-xp")).status).toBe(409);
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "other-run" }, "xp")).status).toBe(409);
      expect(scalar(db, "SELECT SUM(xp) FROM pass_xp_events")).toBe(10);
      const free = await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "FREE" }, "free");
      expect(free.status).toBe(200);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "PREMIUM" }, "premium")).status).toBe(403);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "PREMIUM" }, "free")).status).toBe(409);
      seedAcceptedRun(db, guest.account.id, "quarantined", "QUARANTINED");
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "quarantined" }, "quarantined")).status).toBe(409);
      db.run("INSERT INTO accounts (id,created_at,updated_at) VALUES ('other',0,0)");
      seedAcceptedRun(db, "other", "other-run");
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "other-run" }, "not-owner")).status).toBe(409);
    } finally { vi.useRealTimers(); }
  });

  it("persists only server-verified premium, separates free/premium claims, and enforces revocation", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, guest, request } = await setup({ ...pass, levels: [...pass.levels, { xp: 20, free: reward, premium: reward }] });
      seedAcceptedRun(db, guest.account.id);
      await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "xp");
      installTestVerifier(env, { accountId: "other" });
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "TEST-private-receipt", productId: "test.pass" }, "purchase")).status).toBe(403);
      installTestVerifier(env);
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "TEST-private-receipt", productId: "test.pass" }, "purchase")).status).toBe(200);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "PREMIUM" }, "premium")).status).toBe(200);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "FREE" }, "free")).status).toBe(200);
      seedAcceptedRun(db, guest.account.id, "test-run-2");
      await request("/v1/passes/test-activity/xp/claim", { runId: "test-run-2" }, "xp-2");
      db.run("UPDATE pass_entitlements SET revoked_at = ?", [Date.now()]);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 2, track: "PREMIUM" }, "premium-2")).status).toBe(403);
      const receipts = db.exec("SELECT response_json FROM activity_claims");
      expect(JSON.stringify(receipts)).not.toContain("TEST-private-receipt");
      expect(scalar(db, "SELECT count(*) FROM pass_entitlements")).toBe(1);
    } finally { vi.useRealTimers(); }
  });

  it("honors XP earning end and reward claim grace as separate explicit boundaries", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, guest, request } = await setup(pass);
      seedAcceptedRun(db, guest.account.id);
      await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "xp");
      vi.setSystemTime(pass.endAt);
      expect((await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "end-xp")).status).toBe(409);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "FREE" }, "free")).status).toBe(200);
      vi.setSystemTime(pass.claimUntil);
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "PREMIUM" }, "expired")).status).toBe(409);
    } finally { vi.useRealTimers(); }
  });

  it("rejects purchase proof that expires while the trusted verifier is awaiting", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup(pass);
      (env as Env & { PASS_PURCHASE_VERIFIER: PassPurchaseVerifier }).PASS_PURCHASE_VERIFIER = async (input) => {
        vi.setSystemTime(input.now + 2000);
        return { ...input, transactionId: "short-lived", verifiedAt: input.now, purchasedAt: input.now,
          expiresAt: input.now + 1000, revoked: false };
      };
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "expired-during-verification", productId: "test.pass" })).status).toBe(403);
      expect(scalar(db, "SELECT count(*) FROM pass_entitlements")).toBe(0);
    } finally { vi.useRealTimers(); }
  });

  it.each(["expired", "revoked"])("fences premium %s inside the actual DB batch", async (reason) => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, guest, request } = await setup(pass);
      seedAcceptedRun(db, guest.account.id);
      await request("/v1/passes/test-activity/xp/claim", { runId: "test-run" }, "xp");
      installTestVerifier(env, { expiresAt: fixedNow + 1000 });
      await request("/v1/passes/test-activity/purchases/verify", { receipt: "test", productId: "test.pass" }, "purchase");
      const binding = env.DB as unknown as SqlJsD1Database;
      binding.beforeBatch = () => {
        binding.beforeBatch = undefined;
        if (reason === "expired") vi.setSystemTime(fixedNow + 2000);
        else db.run("UPDATE pass_entitlements SET revoked_at = ?", [Date.now()]);
      };
      expect((await request("/v1/passes/test-activity/rewards/claim", { level: 1, track: "PREMIUM" }, "premium")).status).toBe(403);
      expect(scalar(db, "SELECT count(*) FROM item_ledger")).toBe(0);
    } finally { vi.useRealTimers(); }
  });

  it("accepts an adapter's genuine completion timestamp after its store await", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup(pass);
      (env as Env & { PASS_PURCHASE_VERIFIER: PassPurchaseVerifier }).PASS_PURCHASE_VERIFIER = async (input) => {
        vi.setSystemTime(input.now + 1000);
        return { ...input, transactionId: "completed-later", verifiedAt: Date.now(), purchasedAt: input.now,
          expiresAt: pass.claimUntil, revoked: false };
      };
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "test", productId: "test.pass" })).status).toBe(200);
      expect(scalar(db, "SELECT verified_at FROM pass_entitlements")).toBe(fixedNow + 1000);
    } finally { vi.useRealTimers(); }
  });

  it("rolls back recovery charges with a failed reward and serializes competing recoveries", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow + 3 * 86400000);
    try {
      const recovery = { itemId: "test_paid_currency", quantity: 5, maxPerCampaign: 1, missedDaysOnly: true };
      const { db, env, guest, request } = await setup({ ...attendance, recovery });
      db.run("INSERT INTO account_items (account_id,item_id,quantity,updated_at) VALUES (?,'test_paid_currency',5,?)", [guest.account.id, Date.now()]);
      const day = Math.floor((fixedNow - 1140 * 60000) / 86400000);
      db.run("CREATE TRIGGER test_fail_reward BEFORE INSERT ON reward_transactions BEGIN SELECT RAISE(ABORT, 'injected reward fault'); END");
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: day }, "recover-a")).status).toBe(500);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='test_paid_currency'")).toBe(5);
      expect(scalar(db, "SELECT count(*) FROM item_ledger")).toBe(0);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(0);
      db.run("DROP TRIGGER test_fail_reward");
      const competing = await Promise.all([
        request("/v1/attendance/test-activity/recover", { dayIndex: day }, "recover-a"),
        request("/v1/attendance/test-activity/recover", { dayIndex: day + 1 }, "recover-b"),
      ]);
      expect(competing.map((r) => r.status).sort()).toEqual([200, 409]);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='test_paid_currency'")).toBe(0);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(1);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: day + 2 }, "recover-c")).status).toBe(409);
      vi.setSystemTime(attendance.endAt);
      expect((await request("/v1/attendance/test-activity/recover", { dayIndex: day + 2 }, "expired")).status).toBe(409);
      expect((env.DB as unknown as SqlJsD1Database).beforeBatch).toBeUndefined();
    } finally { vi.useRealTimers(); }
  });

  it("re-evaluates the day if DB execution crosses 04:00 KST", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow + 86400000 - 1);
    try {
      const { db, env, request } = await setup(attendance);
      const binding = env.DB as unknown as SqlJsD1Database;
      binding.beforeBatch = () => { binding.beforeBatch = undefined; vi.setSystemTime(fixedNow + 86400000); };
      const response = await request("/v1/attendance/test-activity/check-in");
      expect(response.status).toBe(200);
      expect(scalar(db, "SELECT day_index FROM attendance_visits")).toBe(Math.floor((fixedNow + 86400000 - 68400000) / 86400000));
      expect(scalar(db, "SELECT count(*) FROM reward_transactions")).toBe(1);
    } finally { vi.useRealTimers(); }
  });

  it("keeps definitions immutable and rejects oversized untrusted payloads", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, request } = await setup(pass);
      expect(() => db.run("UPDATE activity_definitions SET policy_json='{}'")).toThrow(/immutable/u);
      expect(() => db.run("DELETE FROM activity_definitions")).toThrow(/immutable/u);
      expect((await request("/v1/passes/test-activity/purchases/verify", { receipt: "x".repeat(20000), productId: "test.pass" })).status).toBe(400);
      expect(scalar(db, "SELECT count(*) FROM activity_claims")).toBe(0);
    } finally { vi.useRealTimers(); }
  });
});
