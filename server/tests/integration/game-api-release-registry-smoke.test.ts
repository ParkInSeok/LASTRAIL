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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[123][0-9]|4[0-2])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(42);

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

const release = { status: "APPROVED", releaseId: "test-internal", contentVersion: "test-content", catalogUrl: "https://example.test/releases/test-internal/catalog.bin",
  catalogHash: `sha256:${"a".repeat(64)}`, minimumVersionCode: "2", maximumVersionCode: "2" };
const document = { version: "0.1.0", versionCode: "1", minimumVersionCode: "1", clientReleaseRegistry: { schemaVersion: 1, entries: [
  { version: "0.1.0", versionCode: "1", status: "STABLE", contentRelease: { ...release, releaseId: "test-stable", minimumVersionCode: "1", maximumVersionCode: "1" } },
  { version: "0.2.0", versionCode: "2", status: "INTERNAL", contentRelease: release },
] } };
async function setupRegistry(environment: "DEV" | "PRD" = "PRD") {
  const db = await createMigratedDb();
  const policy = structuredClone(document);
  const env = Object.assign(createEnv(db), { ENVIRONMENT: environment, CLIENT_ADMISSION_MODE: "ENFORCED", VERSION_BUCKET: { get: async () => ({ httpEtag: '"test-registry-revision"', json: async () => policy }) } });
  const request = (path: string, method = "GET", token?: string, code = "2", pin?: string) => app.request(path, { method,
    headers: { "X-LastRail-App-Version-Code": code, "X-LastRail-App-Version": "0.2.0", "X-LastRail-App-Platform": "Android",
      ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(pin ? { "X-LastRail-Content-Release-Id": pin } : {}) } }, env);
  return { db, env, request, policy };
}
describe("exact registry admission and safe internal bootstrap", () => {
  it("withholds internal catalogs but permits actual guest authentication and authenticated hydration", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, request } = await setupRegistry();
      const bootstrap = await (await request("/v1/client-config")).json();
      expect(bootstrap).toMatchObject({ accepted: false, clientAdmission: { status: "AUTH_REQUIRED", distribution: "INTERNAL" }, bootstrap: { canAuthenticate: true, contentAuthorized: false } });
      expect(JSON.stringify(bootstrap)).not.toContain("catalog.bin");
      const guest = await request("/v1/auth/guest", "POST");
      expect(guest.status).toBe(200);
      const auth = await guest.json() as { account: { id: string }; accessToken: string };
      expect((await request("/v1/profile", "GET", auth.accessToken)).status).toBe(200);
      expect((await request("/v1/client-config/authorized", "GET", auth.accessToken)).status).toBe(403);
      expect((await request("/v1/wallet/crystals", "GET", auth.accessToken)).status).toBe(403);
      expect(scalar(db, "SELECT count(*) FROM reward_transactions")).toBe(0);
      db.run(`INSERT INTO account_tester_entitlements (account_id,environment,version_code,status,expires_at,granted_by,created_at,updated_at)
        VALUES (?,'PRD','2','ACTIVE',?,'TEST_ONLY',?,?)`, [auth.account.id, fixedNow + 10000, fixedNow, fixedNow]);
      const authorized = await request("/v1/client-config/authorized", "GET", auth.accessToken);
      expect(authorized.status).toBe(200);
      expect(await authorized.json()).toMatchObject({ accepted: true, registryRevision: '"test-registry-revision"', contentRelease: { releaseId: "test-internal" } });
      expect((await request("/v1/wallet/crystals", "GET", auth.accessToken)).status).toBe(409);
      expect((await request("/v1/wallet/crystals", "GET", auth.accessToken, "2", "test-stable")).status).toBe(409);
      const admitted = await request("/v1/wallet/crystals", "GET", auth.accessToken, "2", "test-internal");
      expect(admitted.status).toBe(503); // Correctly reaches unconfigured wallet only after admission.
      expect(await admitted.json()).toMatchObject({ errorCode: "crystal_policy_unconfigured" });
      db.run("UPDATE account_tester_entitlements SET status='REVOKED',revoked_at=?", [fixedNow]);
      expect((await request("/v1/client-config/authorized", "GET", auth.accessToken)).status).toBe(403);
    } finally { vi.useRealTimers(); }
  });
  it("does exact lookup without higher-code inference and preserves stable pre-auth content", async () => {
    const { request } = await setupRegistry();
    expect(await (await request("/v1/client-config", "GET", undefined, "99")).json()).toMatchObject({ accepted: false, errorCode: "client_version_unregistered", bootstrap: { canAuthenticate: false } });
    expect((await request("/v1/auth/guest", "POST", undefined, "99")).status).toBe(403);
    expect(await (await request("/v1/client-config", "GET", undefined, "1")).json()).toMatchObject({ accepted: true, clientAdmission: { distribution: "STABLE" } });
  });
  it("blocks disabled and below-minimum auth, and expired or wrong-environment tester grants", async () => {
    vi.useFakeTimers(); vi.setSystemTime(fixedNow);
    try {
      const { db, policy, request } = await setupRegistry();
      expect((await request("/v1/client-config/authorized")).status).toBe(401);
      const auth = await (await request("/v1/auth/guest", "POST")).json() as { account: { id: string }; accessToken: string };
      db.run(`INSERT INTO account_tester_entitlements (account_id,environment,version_code,status,expires_at,granted_by,created_at,updated_at)
        VALUES (?,'DEV','2','ACTIVE',?,'TEST_ONLY',?,?)`, [auth.account.id, fixedNow + 10000, fixedNow, fixedNow]);
      expect((await request("/v1/client-config/authorized", "GET", auth.accessToken)).status).toBe(403);
      db.run("UPDATE account_tester_entitlements SET environment='PRD',expires_at=?", [fixedNow]);
      expect((await request("/v1/client-config/authorized", "GET", auth.accessToken)).status).toBe(403);
      expect((await request("/v1/story-progress", "GET", auth.accessToken)).status).toBe(200);
      for (const path of ["/v1/gacha/pull", "/v1/attendance/example/check-in", "/v1/runs/start"]) {
        expect((await request(path, "POST", auth.accessToken)).status).toBe(403);
      }
      policy.clientReleaseRegistry.entries[1]!.status = "DISABLED";
      expect((await request("/v1/auth/guest", "POST")).status).toBe(403);
      const disabled = await (await request("/v1/client-config", "GET", auth.accessToken)).json();
      expect(disabled).toMatchObject({ errorCode: "client_version_disabled", bootstrap: { canAuthenticate: false } });
      expect(JSON.stringify(disabled)).not.toContain("catalog.bin");
      policy.versionCode = "2"; policy.minimumVersionCode = "2";
      expect((await request("/v1/auth/guest", "POST", undefined, "1")).status).toBe(426);
    } finally { vi.useRealTimers(); }
  });
  it("DEV serves registered STABLE content without tester lookup while retaining auth, exact code, minimum and pin gates", async () => {
    const { db, env, policy, request } = await setupRegistry("DEV");
    policy.clientReleaseRegistry.entries[1]!.status = "STABLE";
    const prepare = vi.spyOn(env.DB, "prepare");
    const bootstrap = await (await request("/v1/client-config")).json();
    expect(bootstrap).toMatchObject({ accepted: true, clientAdmission: { distribution: "STABLE" }, bootstrap: { authenticationRequired: false, contentAuthorized: true } });
    const auth = await (await request("/v1/auth/guest", "POST")).json() as { accessToken: string };
    expect((await request("/v1/client-config/authorized", "GET", auth.accessToken)).status).toBe(200);
    expect(prepare.mock.calls.some(([sql]) => sql.includes("account_tester_entitlements"))).toBe(false);
    expect(scalar(db, "SELECT count(*) FROM account_tester_entitlements")).toBe(0);
    expect((await request("/v1/wallet/crystals", "GET", undefined, "2", "test-internal")).status).toBe(401);
    expect((await request("/v1/wallet/crystals", "GET", auth.accessToken)).status).toBe(409);
    const wallet = await request("/v1/wallet/crystals", "GET", auth.accessToken, "2", "test-internal");
    expect(wallet.status).toBe(503);
    expect(await wallet.json()).toMatchObject({ errorCode: "crystal_policy_unconfigured" });
    expect((await request("/v1/auth/guest", "POST", undefined, "99")).status).toBe(403);
    policy.versionCode = "2"; policy.minimumVersionCode = "2";
    expect((await request("/v1/auth/guest", "POST", undefined, "1")).status).toBe(426);
    policy.clientReleaseRegistry.entries[1]!.status = "DISABLED";
    expect((await request("/v1/auth/guest", "POST")).status).toBe(403);
  });
  it("rejects unsupported INTERNAL policy in DEV instead of entering a tester-auth flow", async () => {
    const { env, request } = await setupRegistry("DEV");
    const prepare = vi.spyOn(env.DB, "prepare");
    const bootstrap = await (await request("/v1/client-config")).json();
    expect(bootstrap).toMatchObject({ accepted: false, errorCode: "version_policy_unconfigured", bootstrap: { canAuthenticate: false, authenticationRequired: false } });
    expect(JSON.stringify(bootstrap)).not.toContain("catalog.bin");
    expect((await request("/v1/auth/guest", "POST")).status).toBe(503);
    expect(prepare.mock.calls.some(([sql]) => sql.includes("account_tester_entitlements"))).toBe(false);
  });
});
