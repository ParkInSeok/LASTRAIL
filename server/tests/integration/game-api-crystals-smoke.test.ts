import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { reconcileCrystalOrder } from "../../apps/game-api/src/crystal-refunds";



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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(29);

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

const policy = { currencyId: "test_crystal", ticketExchange: { ticketItemId: "cur_command_ticket", crystalsPerTicket: 11, maxTickets: 10 },
  purchase: { provider: "GOOGLE_PLAY", appId: "test.app", products: [{ productId: "test.crystals", quantity: 20 }] } }; // TEST ONLY
async function setup(config: object | null = policy) {
  const db = await createMigratedDb(); const env = createEnv(db);
  if (config) {
    db.run("INSERT INTO crystal_policies (id, policy_json) VALUES ('test-policy', ?)", [JSON.stringify(config)]);
    db.run("INSERT INTO crystal_policy_active (singleton_id, policy_id) VALUES (1, 'test-policy')");
  }
  const guest = await (await app.request("/v1/auth/guest", { method: "POST" }, env)).json() as { account: { id: string }; accessToken: string };
  const request = (path: string, body?: object, key = "test-key", method = "POST") => app.request(path, { method,
    headers: { Authorization: `Bearer ${guest.accessToken}`, "Idempotency-Key": key, "Content-Type": "application/json" },
    ...(body ? { body: JSON.stringify(body) } : {}) }, env);
  return { db, env, guest, request };
}

function seedAcceptedRun(db: initSqlJs.Database, accountId: string, runId = "test-run", status = "COMPLETED", outcome = "VICTORY") {
  db.run("INSERT OR IGNORE INTO balance_config_versions (version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES (1,1,0,'TEST','{}',0,'TEST')");
  const sequence = scalar(db, "SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations");
  db.run("INSERT INTO account_mutations (id,account_id,sequence,mutation_type,created_at) VALUES (?,?,?,'TEST_RUN',?)", [`mutation-${runId}`, accountId, sequence, Date.now()]);
  db.run("INSERT INTO runs (id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES (?,?,?,1,?,?,?)", [runId, accountId, `mutation-${runId}`, status, fixedNow, Date.now()]);
  db.run("INSERT INTO run_final_results (run_id,outcome,cleared_waves,payload_json,submitted_at) VALUES (?,?,15,'{}',?)", [runId, outcome, Date.now()]);
}

describe("local crystal wallet (explicit TEST policy only)", () => {
  it("revokes an entitlement-only pass order even when a crystal currency was never configured", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup(null);
      const pass = { kind: "PASS", startAt: fixedNow, endAt: fixedNow + 86400000, claimUntil: fixedNow + 86400000,
        levels: [{ xp: 10, free: [], premium: [] }], runXp: {}, purchase: { provider: "GOOGLE_PLAY", appId: "test.app", productId: "test.pass" } };
      db.run("INSERT INTO activity_definitions (id,policy_json) VALUES ('test-pass',?)", [JSON.stringify(pass)]);
      Object.assign(env, { PASS_PURCHASE_VERIFIER: async (input: { accountId: string; activityId: string; environment: string; provider: string; appId: string; productId: string; now: number }) => ({ ...input,
        transactionId: "entitlement-only-order", purchasedAt: input.now, verifiedAt: input.now, revoked: false, expiresAt: pass.claimUntil }) });
      expect((await request("/v1/passes/test-pass/purchases/verify", { productId: "test.pass", receipt: "test" }, "purchase")).status).toBe(200);
      const orderId = String(db.exec("SELECT order_id FROM pass_entitlements")[0].values[0][0]);
      const verifier = async (input: { orderId: string; accountId: string; environment: string; provider: string; appId: string; transactionId: string; now: number }) => ({ ...input, status: "REVOKED" as const, verifiedAt: input.now });
      expect(await reconcileCrystalOrder({ db: env.DB, environment: "DEV", orderId, verifier, verificationToken: "test" }))
        .toMatchObject({ reclaimedFree: 0, reclaimedPaid: 0, policyVersion: null, currencyId: null, needsSupport: false });
      expect(scalar(db, "SELECT count(*) FROM pass_entitlements WHERE revoked_at IS NOT NULL")).toBe(1);
    } finally { vi.useRealTimers(); }
  });
  it("grants both pass tracks FREE and reclaims only premium-order linked rewards", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, guest, request } = await setup();
      const pass = { kind: "PASS", startAt: fixedNow, endAt: fixedNow + 86400000, claimUntil: fixedNow + 86400000,
        levels: [{ xp: 10, free: [{ kind: "CRYSTAL", quantity: 5 }], premium: [{ kind: "CRYSTAL", quantity: 9 }] }],
        runXp: { VICTORY: 10 }, purchase: { provider: "GOOGLE_PLAY", appId: "test.app", productId: "test.pass" } };
      db.run("INSERT INTO activity_definitions (id,policy_json) VALUES ('test-pass',?)", [JSON.stringify(pass)]);
      seedAcceptedRun(db, guest.account.id);
      await request("/v1/passes/test-pass/xp/claim", { runId: "test-run" }, "xp");
      expect((await request("/v1/passes/test-pass/rewards/claim", { level: 1, track: "FREE" }, "free")).status).toBe(200);
      expect((await request("/v1/passes/test-pass/rewards/claim", { level: 1, track: "PREMIUM" }, "premium")).status).toBe(403);
      Object.assign(env, { PASS_PURCHASE_VERIFIER: async (input: { accountId: string; activityId: string; environment: string; provider: string; appId: string; productId: string; now: number }) => ({ ...input,
        transactionId: "test-pass-order", purchasedAt: input.now, verifiedAt: input.now, revoked: false, expiresAt: pass.claimUntil }) });
      expect((await request("/v1/passes/test-pass/purchases/verify", { productId: "test.pass", receipt: "test-pass" }, "purchase")).status).toBe(200);
      const premium = await request("/v1/passes/test-pass/rewards/claim", { level: 1, track: "PREMIUM" }, "premium");
      expect(premium.status).toBe(200);
      expect(await premium.json()).toMatchObject({ rewardLines: [{ kind: "CRYSTAL", source: "FREE", purchaseLinked: true, quantity: 9 }] });
      expect(scalar(db, "SELECT SUM(remaining) FROM crystal_lots WHERE source='PAID'")).toBe(0);
      const orderId = String(db.exec("SELECT order_id FROM pass_entitlements")[0].values[0][0]);
      expect(scalar(db, "SELECT count(*) FROM crystal_lots WHERE source_type='PASS_PREMIUM' AND order_id IS NOT NULL")).toBe(1);
      const verifier = async (input: { orderId: string; accountId: string; environment: string; provider: string; appId: string; transactionId: string; now: number }) => ({ ...input, status: "REVOKED" as const, verifiedAt: input.now });
      const result = await reconcileCrystalOrder({ db: env.DB, environment: "DEV", orderId, verifier, verificationToken: "test" });
      expect(result).toMatchObject({ reclaimedFree: 9, reclaimedPaid: 0, spentFree: 0, spentPaid: 0, needsSupport: false });
      expect(scalar(db, "SELECT SUM(remaining) FROM crystal_lots WHERE source='FREE'")).toBe(5);
      expect(scalar(db, "SELECT count(*) FROM pass_entitlements WHERE revoked_at IS NOT NULL")).toBe(1);
    } finally { vi.useRealTimers(); }
  });

  it("uses the wallet for recovery and atomically rolls back spend and grant on reward failure", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow + 86400000);
    try {
      const { db, request } = await setup();
      const attendance = { kind: "ATTENDANCE", startAt: fixedNow, endAt: fixedNow + 3 * 86400000, claimUntil: fixedNow + 3 * 86400000,
        resetOffsetMinutes: 1140, counting: "DISTINCT_DAYS", milestones: [], dailyRewards: [{ kind: "CRYSTAL", quantity: 7 }],
        recovery: { kind: "CRYSTAL", walletPolicyVersion: "test-policy", quantity: 5, maxPerCampaign: 1, missedDaysOnly: true } };
      db.run("INSERT INTO activity_definitions (id,policy_json) VALUES ('test-attendance',?)", [JSON.stringify(attendance)]);
      expect((await request("/v1/attendance/test-attendance/check-in", undefined, "visit")).status).toBe(200);
      const dayIndex = Math.floor((fixedNow - 68400000) / 86400000);
      db.run("CREATE TRIGGER test_reward_fail BEFORE INSERT ON reward_transactions BEGIN SELECT RAISE(ABORT,'test fault'); END");
      expect((await request("/v1/attendance/test-attendance/recover", { dayIndex }, "recover")).status).toBe(500);
      expect(scalar(db, "SELECT SUM(remaining) FROM crystal_lots")).toBe(7);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(1);
      db.run("DROP TRIGGER test_reward_fail");
      const recovery = await request("/v1/attendance/test-attendance/recover", { dayIndex }, "recover");
      expect(recovery.status).toBe(200);
      expect(await recovery.json()).toMatchObject({ cost: { kind: "CRYSTAL", quantity: 5, freeSpent: 5, paidSpent: 0 }, wallet: { freeBalance: 9, paidBalance: 0 } });
      expect((await request("/v1/attendance/test-attendance/recover", { dayIndex }, "again")).status).toBe(409);
      expect(scalar(db, "SELECT count(*) FROM attendance_visits")).toBe(2);
    } finally { vi.useRealTimers(); }
  });

  it("serializes competing exchanges and rejects changed policies and client paid flags", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup();
      Object.assign(env, { CRYSTAL_PURCHASE_VERIFIER: async (input: { accountId: string; environment: string; provider: string; appId: string; productId: string; now: number }) => ({ ...input,
        transactionId: "concurrent-order", purchasedAt: input.now, verifiedAt: input.now, revoked: false, state: "PURCHASED" }) });
      expect((await request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "test", paid: true }, "bad")).status).toBe(400);
      await request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "test" }, "purchase");
      const responses = await Promise.all([
        request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 }, "a"),
        request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 }, "b"),
      ]);
      expect(responses.map((r) => r.status).sort()).toEqual([200, 409]);
      expect(scalar(db, "SELECT SUM(remaining) FROM crystal_lots")).toBe(9);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(1);
      expect((await request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "wrong-policy", ticketCount: 1 }, "wrong")).status).toBe(409);
      expect(db.exec("PRAGMA foreign_key_check")).toEqual([]);
    } finally { vi.useRealTimers(); }
  });
  it("reconciles only a verified order once, preserving exchanged tickets and recording spent deficits", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup();
      Object.assign(env, { CRYSTAL_PURCHASE_VERIFIER: async (input: { accountId: string; environment: string; provider: string; appId: string; productId: string; now: number }) => ({
        accountId: input.accountId, environment: input.environment, provider: input.provider, appId: input.appId, productId: input.productId,
        transactionId: "test-refund-order", purchasedAt: input.now, verifiedAt: input.now, revoked: false, state: "PURCHASED",
      }) });
      await request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "test" }, "purchase");
      await request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 }, "exchange");
      const orderId = String(db.exec("SELECT id FROM verified_store_orders")[0].values[0][0]);
      await expect(reconcileCrystalOrder({ db: env.DB, orderId, environment: "DEV", verificationToken: "test" })).rejects.toThrow("refund_verifier_unconfigured");
      db.run("DELETE FROM crystal_policy_active"); // Disabling new spending must not disable historical reconciliation.
      const verify = async (input: { orderId: string; accountId: string; environment: string; provider: string; appId: string; transactionId: string; now: number }) => ({ ...input, status: "REFUNDED" as const, verifiedAt: input.now });
      const result = await reconcileCrystalOrder({ db: env.DB, orderId, environment: "DEV", verificationToken: "test", verifier: verify });
      expect(result).toMatchObject({ reclaimedFree: 0, reclaimedPaid: 9, spentFree: 0, spentPaid: 11, needsSupport: true, enforcement: "NONE" });
      expect(await reconcileCrystalOrder({ db: env.DB, orderId, environment: "DEV", verificationToken: "test", verifier: verify })).toEqual(result);
      expect(scalar(db, "SELECT SUM(remaining) FROM crystal_lots")).toBe(0);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(1);
      expect(scalar(db, "SELECT count(*) FROM crystal_order_reconciliations")).toBe(1);
      db.run("INSERT INTO crystal_policy_active (singleton_id,policy_id) VALUES (1,'test-policy')");
      expect((await request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "test" }, "new-key")).status).toBe(409);
    } finally { vi.useRealTimers(); }
  });
  it("fails closed without a configured currency and rejects unverified paid purchase", async () => {
    const missing = await setup(null);
    expect((await missing.request("/v1/wallet/crystals", undefined, "get", "GET")).status).toBe(503);
    const configured = await setup();
    const response = await configured.request("/v1/wallet/crystals", undefined, "get", "GET");
    expect(response.status).toBe(200);
    expect(await response.json()).toMatchObject({ currencyId: "test_crystal", policyVersion: "test-policy", freeBalance: 0, paidBalance: 0, totalBalance: 0 });
    expect((await configured.request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "unverified" })).status).toBe(503);
    expect((await configured.request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 })).status).toBe(409);
  });

  it("routes attendance CRYSTAL rewards into FREE lots and spends free-first for tickets", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, env, request } = await setup();
      const attendance = { kind: "ATTENDANCE", startAt: fixedNow, endAt: fixedNow + 86400000, claimUntil: fixedNow + 86400000,
        resetOffsetMinutes: 1140, counting: "DISTINCT_DAYS", recovery: null, milestones: [], dailyRewards: [{ kind: "CRYSTAL", quantity: 7 }] };
      db.run("INSERT INTO activity_definitions (id,policy_json) VALUES ('test-attendance',?)", [JSON.stringify(attendance)]);
      expect((await request("/v1/attendance/test-attendance/check-in", undefined, "attendance")).status).toBe(200);
      const balance = await (await request("/v1/wallet/crystals", undefined, "get", "GET")).json();
      expect(balance).toMatchObject({ freeBalance: 7, paidBalance: 0 });
      expect(scalar(db, "SELECT count(*) FROM account_items WHERE item_id='test_crystal'")).toBe(0);
      // Trusted test store adapter is installed explicitly; no production adapter.
      Object.assign(env, { CRYSTAL_PURCHASE_VERIFIER: async (input: { accountId: string; environment: string; provider: string; appId: string; productId: string; now: number }) => ({
        accountId: input.accountId, environment: input.environment, provider: input.provider, appId: input.appId,
        productId: input.productId, transactionId: "test-order-1", purchasedAt: input.now, verifiedAt: input.now, revoked: false, state: "PURCHASED",
      }) });
      expect((await request("/v1/wallet/crystals/purchases/verify", { policyVersion: "test-policy", productId: "test.crystals", receipt: "test-private-receipt" }, "purchase")).status).toBe(200);
      const exchange = await request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 }, "exchange");
      expect(exchange.status).toBe(200);
      const receipt = await exchange.json();
      expect(receipt).toMatchObject({ freeSpent: 7, paidSpent: 4, freeBalance: 0, paidBalance: 16, ticketsGranted: 1 });
      expect(await (await request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 1 }, "exchange")).json()).toEqual(receipt);
      expect((await request("/v1/wallet/crystals/tickets/exchange", { policyVersion: "test-policy", ticketCount: 2 }, "exchange")).status).toBe(409);
      expect(scalar(db, "SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(1);
      expect(scalar(db, "SELECT count(*) FROM crystal_ledger WHERE delta < 0")).toBe(2);
    } finally { vi.useRealTimers(); }
  });
});
