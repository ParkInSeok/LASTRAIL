import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import gameApi from "../../apps/game-api/src/index";
import adminApi from "../../apps/admin-api/src/index";
import { launchGachaDataDraft, launchRunRewardsConfig } from "../../packages/balance/src/index";

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

  run(): { success: true; meta: { changes: number } } {
    this.database.run(this.sql, this.values);
    return { success: true, meta: { changes: this.database.getRowsModified() } };
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

const fixedNow = Date.parse("2026-08-28T00:00:00.000Z");
const authSecret = "test-secret-minimum-32-characters";

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

function createGameEnv(db: initSqlJs.Database): Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
  GOOGLE_AUTH_AUDIENCES: string;
  GOOGLE_ID_TOKEN_VERIFIER: { verifyIdToken: () => Promise<{ subject: string }> };
} {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    GOOGLE_AUTH_AUDIENCES: "local-google-client-id.apps.googleusercontent.com",
    GOOGLE_ID_TOKEN_VERIFIER: {
      verifyIdToken: async () => ({ subject: "google-act1-smoke-subject" }),
    },
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function createAdminEnv(db: initSqlJs.Database): Env {
  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function seedAdminPrincipal(db: initSqlJs.Database): void {
  db.run(
    `INSERT INTO admin_principals (
       id, access_subject, role, status, display_name, created_at, updated_at
     ) VALUES ('principal-act1-smoke-1', 'access|act1-smoke-admin', 'OPERATOR', 'ACTIVE', 'Act 1 Smoke', ?, ?);`,
    [fixedNow, fixedNow],
  );
}

function seedBalance(db: initSqlJs.Database): void {
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-act1-smoke-1', NULL, 1, 1, '{}', 'checksum-act1-smoke-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [fixedNow, fixedNow],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-act1-smoke-1', ?, ?, 'test', 'draft-act1-smoke-1');`,
    [
      501,
      JSON.stringify({
        runRewards: launchRunRewardsConfig,
        gachaLaunchDraft: launchGachaDataDraft,
      }),
      fixedNow,
    ],
  );
}

function seedOrdinaryRun(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-act1-run-start-1', ?, 1, 'RUN_START', 'run-start-act1-1', ?);`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES ('preview-act1-ordinary-1', ?, 'mutation-act1-run-start-1', 'opn_03_water_stop', 'III', 501, ?, 'CONSUMED', ?, ?);`,
    [
      accountId,
      JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        encounterType: "ORDINARY_15_WAVE",
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
      }),
      fixedNow + 900000,
      fixedNow,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES ('run-act1-ordinary-1', ?, 'preview-act1-ordinary-1', 'mutation-act1-run-start-1', 501, 'ACTIVE', ?, NULL, NULL);`,
    [accountId, fixedNow],
  );
}

function seedRaidRun(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-act1-raid-start-1', ?, 4, 'RUN_START', 'run-start-act1-raid-1', ?);`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES ('preview-act1-raid-1', ?, 'mutation-act1-raid-start-1', 'raid_raider_king_engine', 'V', 501, ?, 'CONSUMED', ?, ?);`,
    [
      accountId,
      JSON.stringify({
        operationId: "raid_raider_king_engine",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        encounterType: "RAID_BOSS",
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
      }),
      fixedNow + 900000,
      fixedNow,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES ('run-act1-raid-1', ?, 'preview-act1-raid-1', 'mutation-act1-raid-start-1', 501, 'ACTIVE', ?, NULL, NULL);`,
    [accountId, fixedNow],
  );
}

function seedStartablePreview(options: {
  db: initSqlJs.Database;
  accountId: string;
  mutationId: string;
  previewId: string;
  sequence: number;
  operationId: string;
  risk: string;
  previewJson: Record<string, unknown>;
}): void {
  options.db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES (?, ?, ?, 'RUN_PREVIEW', ?, ?);`,
    [
      options.mutationId,
      options.accountId,
      options.sequence,
      `${options.previewId}-seed`,
      fixedNow,
    ],
  );
  options.db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 501, ?, 'ACTIVE', ?, ?);`,
    [
      options.previewId,
      options.accountId,
      options.mutationId,
      options.operationId,
      options.risk,
      JSON.stringify(options.previewJson),
      fixedNow + 900000,
      fixedNow,
    ],
  );
}

function seedGacha(db: initSqlJs.Database, accountId: string): void {
  db.run(
    `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
     VALUES (?, 'cur_command_ticket', 12, ?)
     ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at;`,
    [accountId, fixedNow],
  );
  db.run(
    `INSERT INTO gacha_banners (
       id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
     ) VALUES (
       'banner-act1-smoke-1', 'PICKUP', 'ACTIVE', 501, 'op_vex', 1, NULL,
       '{"elite":30,"pickup":60,"rare":10}',
       '{"results":[{"resultRarity":"ELITE","resultType":"ELITE_OPERATOR","resultId":"op_vex","weight":1,"isPickup":true}]}',
       ?, ?
     );`,
    [fixedNow, fixedNow],
  );
}

function scalar(db: initSqlJs.Database, sql: string): number {
  return Number(db.exec(sql)[0]?.values[0]?.[0] ?? 0);
}

function expectForbiddenFieldsAbsent(value: unknown): void {
  const serialized = JSON.stringify(value);
  for (const forbidden of [
    "accessToken",
    "refreshToken",
    "Authorization",
    "rawRng",
    "rawRngMaterial",
    "rngSeed",
    "probability",
    "fullWeights",
    "storyProgressMultiplierBps",
    "rewardCalculation",
    "rawAnimationTimeline",
    "frameData",
    "railChoiceResults",
  ]) {
    expect(serialized).not.toContain(forbidden);
  }
}

describe("Act 1 core loop full local regression smoke", () => {
  it("keeps auth, run finish, reward, offline, gacha, ops, and support read models compatible", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      seedAdminPrincipal(db);
      seedBalance(db);
      const gameEnv = createGameEnv(db);

      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      expect(guestResponse.status).toBe(200);
      const guest = await guestResponse.json() as { account: { id: string }; accessToken: string; refreshToken: string };

      const refreshResponse = await gameApi.request(
        "/v1/auth/refresh",
        { method: "POST", body: JSON.stringify({ refreshToken: guest.refreshToken }) },
        gameEnv,
      );
      expect(refreshResponse.status).toBe(200);
      const refreshed = await refreshResponse.json() as { accessToken: string; refreshToken: string };

      const googleLinkResponse = await gameApi.request(
        "/v1/auth/google/link",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${refreshed.accessToken}` },
          body: JSON.stringify({ idToken: "local-google-id-token" }),
        },
        gameEnv,
      );
      expect(googleLinkResponse.status).toBe(200);
      await expect(googleLinkResponse.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        linkStatus: "LINKED",
        account: { id: guest.account.id },
      });

      const googleSignInResponse = await gameApi.request(
        "/v1/auth/google",
        { method: "POST", body: JSON.stringify({ idToken: "local-google-id-token" }) },
        gameEnv,
      );
      expect(googleSignInResponse.status).toBe(200);
      await expect(googleSignInResponse.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        account: { id: guest.account.id },
      });

      seedOrdinaryRun(db, guest.account.id);
      const ordinaryFinishResponse = await gameApi.request(
        "/v1/runs/run-act1-ordinary-1/finish",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${refreshed.accessToken}`,
            "Idempotency-Key": "act1-ordinary-finish-1",
          },
          body: JSON.stringify({
            runId: "run-act1-ordinary-1",
            currentStage: "Complete",
            currentWaveNumber: 0,
            completedWaveCount: 15,
            encounterType: "ORDINARY_15_WAVE",
            outcome: "VICTORY",
            runtimeStateJson: "{}",
            tacticSelections: [
              { tacticId: "tactic_full_start_1", packageRef: "act1_full_start_package_v1" },
            ],
            clientCompletedAt: "2026-08-28T00:00:00.000Z",
          }),
        },
        gameEnv,
      );
      expect(ordinaryFinishResponse.status).toBe(200);
      const ordinaryFinish = await ordinaryFinishResponse.json() as { rewardPreviewId: string };
      expect(ordinaryFinish).toMatchObject({
        accepted: true,
        resultStatus: "ACCEPTED",
        balanceVersion: "501",
        errorCode: null,
      });

      const rewardPreviewResponse = await gameApi.request(
        `/v1/runs/run-act1-ordinary-1/reward-preview/${ordinaryFinish.rewardPreviewId}`,
        {},
        gameEnv,
      );
      expect(rewardPreviewResponse.status).toBe(200);
      const rewardPreview = await rewardPreviewResponse.json();
      expect(rewardPreview).toMatchObject({
        runId: "run-act1-ordinary-1",
        rewardPreviewId: ordinaryFinish.rewardPreviewId,
        status: "ACTIVE",
        claimability: "CLAIMABLE",
        balanceVersion: "501",
      });

      const rewardClaimResponse = await gameApi.request(
        `/v1/runs/run-act1-ordinary-1/reward-preview/${ordinaryFinish.rewardPreviewId}/claim`,
        {
          method: "POST",
          headers: { "Idempotency-Key": "act1-run-reward-claim-1" },
          body: JSON.stringify({
            runId: "run-act1-ordinary-1",
            rewardPreviewId: ordinaryFinish.rewardPreviewId,
          }),
        },
        gameEnv,
      );
      expect(rewardClaimResponse.status).toBe(200);
      await expect(rewardClaimResponse.json()).resolves.toMatchObject({
        status: "CLAIMED",
        rewardClaimId: expect.stringMatching(/^reward_/u),
        errorCode: null,
      });

      seedRaidRun(db, guest.account.id);
      const raidFinishResponse = await gameApi.request(
        "/v1/runs/run-act1-raid-1/finish",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${refreshed.accessToken}`,
            "Idempotency-Key": "act1-raid-finish-1",
          },
          body: JSON.stringify({
            runId: "run-act1-raid-1",
            currentStage: "Complete",
            currentWaveNumber: 0,
            completedWaveCount: 0,
            encounterType: "RAID_BOSS",
            raidResult: {
              raidId: "raid_raider_king_engine",
              bossId: "boss_raider_king_engine",
              bossDefeated: true,
              completedPhaseCount: 3,
              encounterDurationSeconds: 420,
            },
            outcome: "VICTORY",
            runtimeStateJson: "{}",
            tacticSelections: [
              { tacticId: "tactic_full_start_raid", packageRef: "act1_full_start_package_v1" },
            ],
            clientCompletedAt: "2026-08-28T00:00:00.000Z",
          }),
        },
        gameEnv,
      );
      expect(raidFinishResponse.status).toBe(200);
      await expect(raidFinishResponse.json()).resolves.toMatchObject({
        accepted: true,
        resultStatus: "ACCEPTED",
        balanceVersion: "501",
        errorCode: null,
      });
      expect(scalar(db, "SELECT cleared_waves FROM run_final_results WHERE run_id = 'run-act1-raid-1';")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM account_story_progress WHERE highest_story_progress_id = 'raid_raider_king_engine';")).toBe(1);

      db.run(
        `UPDATE account_offline_reward_state
         SET last_offline_reward_claim_at = ?, updated_at = ?
         WHERE account_id = ?;`,
        [fixedNow - 60 * 60 * 1000, fixedNow - 60 * 60 * 1000, guest.account.id],
      );
      const offlinePreviewResponse = await gameApi.request(
        "/v1/offline-rewards/preview",
        { headers: { Authorization: `Bearer ${refreshed.accessToken}` } },
        gameEnv,
      );
      expect(offlinePreviewResponse.status).toBe(200);
      await expect(offlinePreviewResponse.json()).resolves.toMatchObject({
        accepted: true,
        previewStatus: "AVAILABLE",
        storyProgressId: "raid_raider_king_engine",
        offlineStorageCapHours: 12,
        rewardLines: expect.any(Array),
      });

      const offlineClaimResponse = await gameApi.request(
        "/v1/offline-rewards/claim",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${refreshed.accessToken}`,
            "Idempotency-Key": "act1-offline-claim-1",
          },
        },
        gameEnv,
      );
      expect(offlineClaimResponse.status).toBe(200);
      await expect(offlineClaimResponse.json()).resolves.toMatchObject({
        accepted: true,
        claimStatus: "CLAIMED",
        storyProgressId: "raid_raider_king_engine",
        errorCode: null,
      });

      seedGacha(db, guest.account.id);
      const gachaResponse = await gameApi.request(
        "/v1/gacha/banners/banner-act1-smoke-1/pulls",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${refreshed.accessToken}`,
            "Idempotency-Key": "act1-gacha-pull-1",
          },
          body: JSON.stringify({ pullCount: 10 }),
        },
        gameEnv,
      );
      expect(gachaResponse.status).toBe(200);
      const gacha = await gachaResponse.json();
      expect(gacha).toMatchObject({
        accepted: true,
        resultStatus: "ACCEPTED",
        bannerId: "banner-act1-smoke-1",
        pullCount: 10,
        cost: { itemId: "cur_command_ticket", quantity: 10 },
      });
      expect(JSON.stringify(gacha)).toContain("guaranteeType");

      const supportResponse = await adminApi.request(
        `/v1/admin/support/accounts/${guest.account.id}`,
        { headers: { "Cf-Access-Subject": "access|act1-smoke-admin" } },
        createAdminEnv(db),
      );
      expect(supportResponse.status).toBe(200);
      const support = await supportResponse.json();
      expect(support).toMatchObject({
        permission: "SUPPORT_READ",
        accountId: guest.account.id,
        auth: {
          hasGoogleLink: true,
          activeSessionCount: expect.any(Number),
          concurrentUsers: null,
          concurrentUsersPrecision: "UNAVAILABLE",
        },
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          repairEndpointsEnabled: false,
        },
      });
      expectForbiddenFieldsAbsent(rewardPreview);
      expectForbiddenFieldsAbsent(gacha);
      expectForbiddenFieldsAbsent(support);
      expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
      expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    } finally {
      vi.useRealTimers();
    }
  });

  it("keeps Rail Choice Events out of the launch run finish payload contract", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      seedBalance(db);
      const gameEnv = createGameEnv(db);
      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      const guest = await guestResponse.json() as { account: { id: string }; accessToken: string };
      seedOrdinaryRun(db, guest.account.id);

      const response = await gameApi.request(
        "/v1/runs/run-act1-ordinary-1/finish",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "act1-rail-choice-forbidden-1",
          },
          body: JSON.stringify({
            runId: "run-act1-ordinary-1",
            currentStage: "Complete",
            currentWaveNumber: 0,
            completedWaveCount: 15,
            encounterType: "ORDINARY_15_WAVE",
            outcome: "VICTORY",
            runtimeStateJson: "{}",
            tacticSelections: [],
            railChoiceResults: [
              {
                choiceId: "rail_choice:opn_03_water_stop:water_rights",
                optionId: "rail_choice_option:opn_03_water_stop:water_rights:share",
              },
            ],
            clientCompletedAt: "2026-08-28T00:00:00.000Z",
          }),
        },
        gameEnv,
      );

      expect(response.status).toBe(400);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        resultStatus: "REJECTED",
        rewardPreviewId: null,
        errorCode: "contract_violation",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM run_final_results;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM run_reward_previews;")).toBe(0);
      expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'run_finish';")).toBe(0);
    } finally {
      vi.useRealTimers();
    }
  });

  it("keeps run start and Admin support metadata aligned from operation preview source refs", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      seedAdminPrincipal(db);
      seedBalance(db);
      const gameEnv = createGameEnv(db);
      const guestResponse = await gameApi.request("/v1/auth/guest", { method: "POST" }, gameEnv);
      const guest = await guestResponse.json() as { account: { id: string }; accessToken: string };

      seedStartablePreview({
        db,
        accountId: guest.account.id,
        mutationId: "mutation-start-align-ordinary",
        previewId: "preview-start-align-ordinary",
        sequence: 10,
        operationId: "opn_03_water_stop",
        risk: "III",
        previewJson: {
          operationId: "opn_03_water_stop",
          actId: "act_01_wasteland_railway",
          regionId: "region_wasteland_railway",
          encounterType: "ORDINARY_15_WAVE",
          raidId: "should_not_leak",
          bossId: "should_not_leak",
          bossPhaseRefs: ["should_not_leak"],
          allowedOperatorSlots: 5,
          startAugmentPackageRef: "act1_full_start_package_v1",
          startTacticPackageRef: "act1_full_start_package_v1",
          contentVersion: "act1.launch.content.v1",
        },
      });
      seedStartablePreview({
        db,
        accountId: guest.account.id,
        mutationId: "mutation-start-align-raid",
        previewId: "preview-start-align-raid",
        sequence: 11,
        operationId: "raid_raider_king_engine",
        risk: "V",
        previewJson: {
          operationId: "raid_raider_king_engine",
          actId: "act_01_wasteland_railway",
          regionId: "region_wasteland_railway",
          encounterType: "RAID_BOSS",
          raidId: "raid_raider_king_engine",
          bossId: "boss_raider_king_engine",
          bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
          allowedOperatorSlots: 5,
          startAugmentPackageRef: "act1_full_start_package_v1",
          startTacticPackageRef: "act1_full_start_package_v1",
          contentVersion: "act1.launch.content.v1",
        },
      });

      const ordinaryStart = await gameApi.request(
        "/v1/runs/start",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "act1-start-align-ordinary",
          },
          body: JSON.stringify({ operationPreviewId: "preview-start-align-ordinary" }),
        },
        gameEnv,
      );
      expect(ordinaryStart.status).toBe(200);
      const ordinaryBody = await ordinaryStart.json() as { runId: string };
      expect(ordinaryBody).toMatchObject({
        accepted: true,
        operationPreviewId: "preview-start-align-ordinary",
        operationId: "opn_03_water_stop",
        encounterType: "ORDINARY_15_WAVE",
        ordinary: { totalWaves: 15, startWaveNumber: 1 },
        raid: null,
        raidId: null,
        bossId: null,
        bossPhaseRefs: [],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
        balanceVersion: "501",
      });

      const raidStart = await gameApi.request(
        "/v1/runs/start",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${guest.accessToken}`,
            "Idempotency-Key": "act1-start-align-raid",
          },
          body: JSON.stringify({ operationPreviewId: "preview-start-align-raid" }),
        },
        gameEnv,
      );
      expect(raidStart.status).toBe(200);
      const raidBody = await raidStart.json() as { runId: string };
      expect(raidBody).toMatchObject({
        accepted: true,
        operationPreviewId: "preview-start-align-raid",
        operationId: "raid_raider_king_engine",
        encounterType: "RAID_BOSS",
        ordinary: null,
        raid: {
          raidId: "raid_raider_king_engine",
          bossId: "boss_raider_king_engine",
          bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
        },
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
        balanceVersion: "501",
      });

      const supportResponse = await adminApi.request(
        `/v1/admin/support/accounts/${guest.account.id}`,
        { headers: { "Cf-Access-Subject": "access|act1-smoke-admin" } },
        createAdminEnv(db),
      );
      expect(supportResponse.status).toBe(200);
      const support = await supportResponse.json() as { recentRuns: Array<Record<string, unknown>> };
      const supportOrdinary = support.recentRuns.find((run) => run.runId === ordinaryBody.runId);
      const supportRaid = support.recentRuns.find((run) => run.runId === raidBody.runId);

      expect(supportOrdinary).toMatchObject({
        operationId: "opn_03_water_stop",
        encounterType: "ORDINARY_15_WAVE",
        raidId: null,
        bossId: null,
        bossPhaseRefs: [],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
        balanceVersion: "501",
      });
      expect(supportRaid).toMatchObject({
        operationId: "raid_raider_king_engine",
        encounterType: "RAID_BOSS",
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
        balanceVersion: "501",
      });
      expectForbiddenFieldsAbsent(ordinaryBody);
      expectForbiddenFieldsAbsent(raidBody);
      expectForbiddenFieldsAbsent(support);
      expect(JSON.stringify(ordinaryBody)).not.toContain("should_not_leak");
      expect(JSON.stringify(supportOrdinary)).not.toContain("should_not_leak");
    } finally {
      vi.useRealTimers();
    }
  });
});
