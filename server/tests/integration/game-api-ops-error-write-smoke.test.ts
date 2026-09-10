import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";

type SqlValue = string | number | Uint8Array | null;
const encoder = new TextEncoder();

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

  run(): { success: true; meta: { changes: number } } {
    this.database.run(this.sql, this.values);
    return { success: true, meta: { changes: this.database.getRowsModified() } };
  }
}

class SqlJsD1Database {
  private readonly database: initSqlJs.Database;
  private readonly failBatchSqlIncludes: string | null;

  constructor(database: initSqlJs.Database, options: { failBatchSqlIncludes?: string } = {}) {
    this.database = database;
    this.failBatchSqlIncludes = options.failBatchSqlIncludes ?? null;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.database.run("BEGIN;");
    try {
      for (const statement of statements) {
        if (this.failBatchSqlIncludes !== null && statement.sql.includes(this.failBatchSqlIncludes)) {
          throw new Error(`simulated batch conflict: ${this.failBatchSqlIncludes}`);
        }
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
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[123][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(31);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(
  db: initSqlJs.Database,
  options: { failBatchSqlIncludes?: string; googleAudiences?: string } = {},
): Env & { AUTH_SIGNING_KEY_CURRENT: string; GOOGLE_AUTH_AUDIENCES?: string } {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    GOOGLE_AUTH_AUDIENCES: options.googleAudiences,
    DB: new SqlJsD1Database(db, options) as unknown as D1Database,
  };
}

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

function firstRow<T extends Record<string, unknown>>(db: initSqlJs.Database, sql: string): T {
  const result = db.exec(sql);
  const columns = result[0]?.columns ?? [];
  const values = result[0]?.values[0] ?? [];
  return Object.fromEntries(columns.map((column, index) => [column, values[index]])) as T;
}

async function createSignedGoogleIdToken(): Promise<string> {
  const keyPair = await crypto.subtle.generateKey(
    {
      name: "RSASSA-PKCS1-v1_5",
      modulusLength: 2048,
      publicExponent: new Uint8Array([1, 0, 1]),
      hash: "SHA-256",
    },
    true,
    ["sign", "verify"],
  );
  const header = { alg: "RS256", kid: "jwks-failure-key", typ: "JWT" };
  const payload = {
    iss: "https://accounts.google.com",
    aud: "unit-google-client.apps.googleusercontent.com",
    sub: "google-jwks-failure-subject",
    exp: Math.floor((fixedNow + 60_000) / 1000),
  };
  const signingInput = `${base64UrlJson(header)}.${base64UrlJson(payload)}`;
  const signature = await crypto.subtle.sign("RSASSA-PKCS1-v1_5", keyPair.privateKey, encoder.encode(signingInput));
  return `${signingInput}.${base64UrlEncode(new Uint8Array(signature))}`;
}

function base64UrlJson(value: unknown): string {
  return base64UrlEncode(encoder.encode(JSON.stringify(value)));
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
}

async function createGuest(env: Env): Promise<{ accountId: string; accessToken: string }> {
  const response = await app.request("/v1/auth/guest", { method: "POST" }, env);
  const body = await response.json() as {
    account: { id: string };
    accessToken: string;
  };
  return { accountId: body.account.id, accessToken: body.accessToken };
}

function seedGachaBanner(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-ops-gacha-1', NULL, 1, 1, '{}', 'checksum-ops-gacha-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [fixedNow, fixedNow],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (
       401, 1, 1, 'checksum-ops-gacha-1', '{}', ?, 'test', 'draft-ops-gacha-1'
     );`,
    [fixedNow],
  );
  db.run(
    `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
     VALUES (?, 'cur_command_ticket', 1, ?);`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO gacha_banners (
       id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at,
       pity_config_json, pool_snapshot_json, created_at, updated_at
     ) VALUES (
       'banner-ops-gacha-1', 'PICKUP', 'ACTIVE', 401, 'op_vex', 1, NULL,
       '{"elite":30,"pickup":60,"rare":10}',
       '{"results":[{"resultRarity":"ELITE","resultType":"ELITE_OPERATOR","resultId":"op_vex","weight":1,"isPickup":true}]}',
       ?, ?
     );`,
    [fixedNow, fixedNow],
  );
}

function seedFinishableRun(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-ops-run-1', NULL, 1, 1, '{}', 'checksum-ops-run-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [fixedNow, fixedNow],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-ops-run-1', ?, ?, 'test', 'draft-ops-run-1');`,
    [402, JSON.stringify({ runRewards: launchRunRewardsConfig }), fixedNow],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-ops-run-start-1', ?, 2, 'RUN_START', 'idem-ops-run-start-1', ?);`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 402, ?, 'CONSUMED', ?, ?);`,
    [
      "preview-ops-run-1",
      accountId,
      "mutation-ops-run-start-1",
      "opn_03_water_stop",
      "III",
      JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
      }),
      fixedNow + 900000,
      fixedNow,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES ('run-ops-story-1', ?, 'preview-ops-run-1', 'mutation-ops-run-start-1', 402, 'ACTIVE', ?, NULL, NULL);`,
    [accountId, fixedNow],
  );
}

function seedStartableOperationPreview(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-ops-run-start-1', NULL, 1, 1, '{}', 'checksum-ops-run-start-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [fixedNow, fixedNow],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-ops-run-start-1', ?, ?, 'test', 'draft-ops-run-start-1');`,
    [403, JSON.stringify({ runRewards: launchRunRewardsConfig }), fixedNow],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-ops-run-start-preview-1', ?, 2, 'OPERATION_PREVIEW', 'idem-ops-run-start-preview-1', ?);`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 403, ?, 'ACTIVE', ?, ?);`,
    [
      "preview-ops-run-start-1",
      accountId,
      "mutation-ops-run-start-preview-1",
      "opn_03_water_stop",
      "III",
      JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        encounterType: "ORDINARY_15_WAVE",
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
      }),
      fixedNow + 900000,
      fixedNow,
    ],
  );
}

describe("game-api minimal ops error write integration", () => {
  it("writes a scrubbed ops error when Google auth config is missing", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);

      const response = await app.request(
        "/v1/auth/google",
        {
          method: "POST",
          headers: { "X-Request-Id": "req_google_config_missing_1" },
          body: JSON.stringify({ idToken: "raw-google-id-token-value" }),
        },
        env,
      );

      expect(response.status).toBe(500);
      await expect(response.json()).resolves.toEqual({
        errorCode: "google_auth_not_configured",
        message: "Google auth verifier is not configured.",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs;")).toBe(1);
      expect(firstRow(db, "SELECT environment, service, severity, error_code, status, source, request_id, masked FROM ops_error_logs;")).toEqual({
        environment: "DEV",
        service: "game_api",
        severity: "critical",
        error_code: "google_auth_config_missing",
        status: "open",
        source: "game_api",
        request_id: "req_google_config_missing_1",
        masked: 1,
      });
      const serializedLog = JSON.stringify(db.exec("SELECT * FROM ops_error_logs;"));
      expect(serializedLog).not.toContain("raw-google-id-token-value");
      expect(serializedLog).not.toContain("idToken");
      expect(serializedLog).not.toContain("Authorization");
    } finally {
      vi.useRealTimers();
    }
  });

  it("writes a scrubbed ops error when Google JWKS verification is unavailable", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("raw-jwks-upstream-body", { status: 503 }));
    try {
      const db = await createMigratedDb();
      const env = createEnv(db, { googleAudiences: "unit-google-client.apps.googleusercontent.com" });
      const idToken = await createSignedGoogleIdToken();

      const response = await app.request(
        "/v1/auth/google",
        {
          method: "POST",
          headers: { "X-Request-Id": "req_google_jwks_unavailable_1" },
          body: JSON.stringify({ idToken }),
        },
        env,
      );

      expect(response.status).toBe(503);
      await expect(response.json()).resolves.toEqual({
        errorCode: "google_auth_temporarily_unavailable",
        message: "Google auth verification is temporarily unavailable.",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'google_auth_verifier_unavailable';")).toBe(1);
      expect(firstRow(db, "SELECT environment, service, severity, error_code, status, source, request_id, masked, retryable FROM ops_error_logs WHERE error_code = 'google_auth_verifier_unavailable';")).toEqual({
        environment: "DEV",
        service: "game_api",
        severity: "error",
        error_code: "google_auth_verifier_unavailable",
        status: "open",
        source: "game_api",
        request_id: "req_google_jwks_unavailable_1",
        masked: 1,
        retryable: 1,
      });
      const serializedLog = JSON.stringify(db.exec("SELECT * FROM ops_error_logs;"));
      expect(serializedLog).not.toContain(idToken);
      expect(serializedLog).not.toContain("google-jwks-failure-subject");
      expect(serializedLog).not.toContain("idToken");
      expect(serializedLog).not.toContain("Authorization");
      expect(serializedLog).not.toContain("raw-jwks-upstream-body");
    } finally {
      fetchSpy.mockRestore();
      vi.useRealTimers();
    }
  });

  it("writes an account-correlated ops error for offline reward state conflicts without consuming claim state", async () => {
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
        `UPDATE account_offline_reward_state
         SET last_offline_reward_claim_at = ?, revision = 7, updated_at = ?
         WHERE account_id = ?;`,
        [fixedNow + 60_000, fixedNow, guestBody.account.id],
      );

      const response = await app.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guestBody.accessToken}`,
            "Idempotency-Key": "offline-conflict-idem-1",
            "X-Request-Id": "req_offline_conflict_1",
          },
        },
        env,
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        claimStatus: "REJECTED",
        offlineRewardClaimId: null,
        errorCode: "offline_reward_state_conflict",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM reward_transactions WHERE run_id IS NULL;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'offline_reward_claim';")).toBe(0);
      expect(scalar(db, "SELECT revision FROM account_offline_reward_state WHERE account_id LIKE 'account_%';")).toBe(7);
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'offline_reward_state_conflict';")).toBe(1);

      const log = firstRow(db, "SELECT account_id, idempotency_operation, idempotency_key_hash, retryable, masked FROM ops_error_logs WHERE error_code = 'offline_reward_state_conflict';");
      expect(log).toMatchObject({
        account_id: guestBody.account.id,
        idempotency_operation: "offline_reward_claim",
        retryable: 0,
        masked: 1,
      });
      expect(String(log.idempotency_key_hash)).toMatch(/^sha256:/u);
      expect(String(log.idempotency_key_hash)).not.toContain("offline-conflict-idem-1");
    } finally {
      vi.useRealTimers();
    }
  });

  it("writes a fail-soft ops error for gacha commit state conflicts without granting rewards", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const setupEnv = createEnv(db);
      const guest = await createGuest(setupEnv);
      seedGachaBanner(db, guest.accountId);

      const response = await app.request(
        "/v1/gacha/banners/banner-ops-gacha-1/pulls",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "gacha-conflict-idem-1",
            "X-Request-Id": "req_gacha_conflict_1",
          },
          body: JSON.stringify({ pullCount: 1 }),
        },
        createEnv(db, { failBatchSqlIncludes: "INSERT INTO gacha_pull_batches" }),
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        resultStatus: "REJECTED",
        batchId: null,
        errorCode: "gacha_state_conflict",
      });
      expect(scalar(db, "SELECT quantity FROM account_items WHERE account_id LIKE 'account_%' AND item_id = 'cur_command_ticket';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_batches;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM gacha_pull_results;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'gacha_pull';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'gacha_state_conflict';")).toBe(1);

      const log = firstRow(db, "SELECT account_id, gacha_banner_id, idempotency_operation, idempotency_key_hash, retryable, masked FROM ops_error_logs WHERE error_code = 'gacha_state_conflict';");
      expect(log).toMatchObject({
        account_id: guest.accountId,
        gacha_banner_id: "banner-ops-gacha-1",
        idempotency_operation: "gacha_pull",
        retryable: 1,
        masked: 1,
      });
      expect(String(log.idempotency_key_hash)).toMatch(/^sha256:/u);
      expect(String(log.idempotency_key_hash)).not.toContain("gacha-conflict-idem-1");
    } finally {
      vi.useRealTimers();
    }
  });

  it("writes a fail-soft ops error for story progress commit conflicts without finishing the run", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const setupEnv = createEnv(db);
      const guest = await createGuest(setupEnv);
      seedFinishableRun(db, guest.accountId);

      const response = await app.request(
        "/v1/runs/run-ops-story-1/finish",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "story-conflict-idem-1",
            "X-Request-Id": "req_story_conflict_1",
          },
          body: JSON.stringify({
            runId: "run-ops-story-1",
            currentStage: "Complete",
            currentWaveNumber: 0,
            completedWaveCount: 15,
            outcome: "VICTORY",
            runtimeStateJson: "{}",
            tacticSelections: [],
            clientCompletedAt: "2026-08-28T00:00:00.000Z",
          }),
        },
        createEnv(db, { failBatchSqlIncludes: "INSERT INTO account_story_progress" }),
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        runId: "run-ops-story-1",
        accepted: false,
        resultStatus: "REJECTED",
        rewardPreviewId: null,
        errorCode: "story_progress_state_conflict",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM run_final_results WHERE run_id = 'run-ops-story-1';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM run_reward_previews WHERE run_id = 'run-ops-story-1';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'run_finish';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'story_progress_state_conflict';")).toBe(1);

      const log = firstRow(db, "SELECT account_id, run_id, story_progress_id, idempotency_operation, idempotency_key_hash, retryable, masked FROM ops_error_logs WHERE error_code = 'story_progress_state_conflict';");
      expect(log).toMatchObject({
        account_id: guest.accountId,
        run_id: "run-ops-story-1",
        story_progress_id: "opn_03_water_stop",
        idempotency_operation: "run_finish",
        retryable: 1,
        masked: 1,
      });
      expect(String(log.idempotency_key_hash)).toMatch(/^sha256:/u);
      expect(String(log.idempotency_key_hash)).not.toContain("story-conflict-idem-1");
    } finally {
      vi.useRealTimers();
    }
  });

  it("reopens a consumed operation preview and logs a fail-soft ops error when run start persistence fails", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const setupEnv = createEnv(db);
      const guest = await createGuest(setupEnv);
      seedStartableOperationPreview(db, guest.accountId);

      const response = await app.request(
        "/v1/runs/start",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "run-start-conflict-idem-1",
            "X-Request-Id": "req_run_start_conflict_1",
          },
          body: JSON.stringify({ operationPreviewId: "preview-ops-run-start-1" }),
        },
        createEnv(db, { failBatchSqlIncludes: "INSERT INTO run_snapshots" }),
      );

      expect(response.status).toBe(409);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        startStatus: "REJECTED",
        operationPreviewId: "preview-ops-run-start-1",
        errorCode: "run_start_state_conflict",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM runs WHERE preview_id = 'preview-ops-run-start-1';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM run_snapshots;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'run_start';")).toBe(0);
      expect(firstRow(db, "SELECT status FROM operation_previews WHERE id = 'preview-ops-run-start-1';")).toEqual({
        status: "ACTIVE",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'run_start_state_conflict';")).toBe(1);

      const log = firstRow(db, "SELECT account_id, run_id, idempotency_operation, idempotency_key_hash, correlation_json, retryable, masked FROM ops_error_logs WHERE error_code = 'run_start_state_conflict';");
      expect(log).toMatchObject({
        account_id: guest.accountId,
        run_id: null,
        idempotency_operation: "run_start",
        retryable: 1,
        masked: 1,
      });
      expect(String(log.idempotency_key_hash)).toMatch(/^sha256:/u);
      expect(String(log.idempotency_key_hash)).not.toContain("run-start-conflict-idem-1");
      expect(String(log.correlation_json)).toContain("preview-ops-run-start-1");
      expect(String(log.correlation_json)).toContain("\"runId\":\"run_");
      expect(JSON.stringify(db.exec("SELECT * FROM ops_error_logs WHERE error_code = 'run_start_state_conflict';"))).not.toContain("rawRng");
      expect(JSON.stringify(db.exec("SELECT * FROM ops_error_logs WHERE error_code = 'run_start_state_conflict';"))).not.toContain("animationTimeline");
    } finally {
      vi.useRealTimers();
    }
  });
});
