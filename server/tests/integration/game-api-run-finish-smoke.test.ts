import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken, hashRefreshToken } from "../../apps/game-api/src/auth";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";

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

const authSecret = "test-secret-minimum-32-characters";

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

function scalar(db: initSqlJs.Database, sql: string): number | string | null {
  return (db.exec(sql)[0]?.values[0]?.[0] as number | string | undefined) ?? null;
}

async function seedFinishableRun(db: initSqlJs.Database): Promise<string> {
  const now = Date.now();
  const refreshTokenHash = await hashRefreshToken("refresh-smoke-token");
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES (?, 'ACTIVE', ?, ?);", [
    "account-smoke-1",
    now,
    now,
  ]);
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL);`,
    ["session-smoke-1", "account-smoke-1", refreshTokenHash, "family-smoke-1", now, now + 900000],
  );
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-smoke-1', NULL, 1, 1, '{}', 'checksum-smoke-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [now, now],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-smoke-1', ?, ?, 'test', 'draft-smoke-1');`,
    [1, JSON.stringify({ runRewards: launchRunRewardsConfig }), now],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-run-smoke-1', 'account-smoke-1', 1, 'RUN_START', 'idem-run-start-smoke-1', ?);`,
    [now],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 1, ?, 'CONSUMED', ?, ?);`,
    [
      "preview-smoke-1",
      "account-smoke-1",
      "mutation-run-smoke-1",
      "opn_03_water_stop",
      "III",
      JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
      }),
      now + 900000,
      now,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES (?, ?, ?, ?, 1, 'ACTIVE', ?, NULL, NULL);`,
    ["run-smoke-1", "account-smoke-1", "preview-smoke-1", "mutation-run-smoke-1", now],
  );
  db.run(
    `INSERT INTO run_snapshots (run_id, snapshot_json, created_at)
     VALUES (?, ?, ?);`,
    [
      "run-smoke-1",
      JSON.stringify({
        runVersionSnapshot: {
          source: "server_accepted_run_start",
          clientInstalled: {
            versionCode: "100",
            platform: "android",
            channel: "internal",
          },
          clientAdmission: {
            status: "ACCEPTED",
            minimumVersionCode: "90",
            updateRequired: false,
          },
          serverAcceptedDeployment: {
            environment: "DEV",
            runtimeDeploymentId: "cf-runtime-deploy-smoke",
          },
          contentRelease: {
            status: "APPROVED_COMPATIBLE",
            releaseId: "content_act1_001",
            contentVersion: "act1.2026-09-06.001",
            catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          },
          capturedAt: "2026-08-27T00:00:00.000Z",
        },
      }),
      now,
    ],
  );

  return `Bearer ${await createAccessToken({
    secret: authSecret,
    accountId: "account-smoke-1",
    sessionId: "session-smoke-1",
    nowMs: now,
    ttlMs: 900000,
  })}`;
}

async function seedFinishableStoryRaidFailureRun(db: initSqlJs.Database): Promise<string> {
  const now = Date.now();
  const refreshTokenHash = await hashRefreshToken("refresh-raid-failure-smoke-token");
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES (?, 'ACTIVE', ?, ?);", [
    "account-raid-failure-smoke-1",
    now,
    now,
  ]);
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL);`,
    [
      "session-raid-failure-smoke-1",
      "account-raid-failure-smoke-1",
      refreshTokenHash,
      "family-raid-failure-smoke-1",
      now,
      now + 900000,
    ],
  );
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-raid-failure-smoke-1', NULL, 1, 1, '{}', 'checksum-raid-failure-smoke-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [now, now],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-raid-failure-smoke-1', ?, ?, 'test', 'draft-raid-failure-smoke-1');`,
    [704, JSON.stringify({ fuel: { max: 100, runStartCost: 10, rechargeSeconds: 300 }, runRewards: launchRunRewardsConfig }), now],
  );
  db.run(
    `INSERT INTO fuel_state (account_id, fuel, max_fuel, last_regen_at, updated_at)
     VALUES ('account-raid-failure-smoke-1', 80, 100, ?, ?);`,
    [now, now],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-raid-failure-smoke-1', 'account-raid-failure-smoke-1', 1, 'RUN_START', 'idem-raid-failure-start-smoke-1', ?);`,
    [now],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 704, ?, 'CONSUMED', ?, ?);`,
    [
      "preview-raid-failure-smoke-1",
      "account-raid-failure-smoke-1",
      "mutation-raid-failure-smoke-1",
      "raid_raider_king_engine",
      "V",
      JSON.stringify({
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
      }),
      now + 900000,
      now,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES (?, ?, ?, ?, 704, 'ACTIVE', ?, NULL, NULL);`,
    [
      "run-raid-failure-smoke-1",
      "account-raid-failure-smoke-1",
      "preview-raid-failure-smoke-1",
      "mutation-raid-failure-smoke-1",
      now,
    ],
  );

  return `Bearer ${await createAccessToken({
    secret: authSecret,
    accountId: "account-raid-failure-smoke-1",
    sessionId: "session-raid-failure-smoke-1",
    nowMs: now,
    ttlMs: 900000,
  })}`;
}

async function seedStartableRunPreview(db: initSqlJs.Database): Promise<string> {
  const now = Date.now();
  const refreshTokenHash = await hashRefreshToken("refresh-start-smoke-token");
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES (?, 'ACTIVE', ?, ?);", [
    "account-start-smoke-1",
    now,
    now,
  ]);
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL);`,
    ["session-start-smoke-1", "account-start-smoke-1", refreshTokenHash, "family-start-smoke-1", now, now + 900000],
  );
  db.run(
    `INSERT INTO balance_config_drafts (
       id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
     ) VALUES (
       'draft-start-smoke-1', NULL, 1, 1, '{}', 'checksum-start-smoke-1', 1, 'PUBLISHED', 'test', ?, ?
     );`,
    [now, now],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (?, 1, 1, 'checksum-start-smoke-1', ?, ?, 'test', 'draft-start-smoke-1');`,
    [701, JSON.stringify({ runRewards: launchRunRewardsConfig }), now],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES ('mutation-start-smoke-1', 'account-start-smoke-1', 1, 'RUN_START', 'idem-start-smoke-1', ?);`,
    [now],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
     ) VALUES (?, ?, ?, ?, ?, 701, ?, 'ACTIVE', ?, ?);`,
    [
      "preview-start-smoke-1",
      "account-start-smoke-1",
      "mutation-start-smoke-1",
      "raid_raider_king_engine",
      "V",
      JSON.stringify({
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
      }),
      now + 900000,
      now,
    ],
  );

  return `Bearer ${await createAccessToken({
    secret: authSecret,
    accountId: "account-start-smoke-1",
    sessionId: "session-start-smoke-1",
    nowMs: now,
    ttlMs: 900000,
  })}`;
}

describe("game-api run finish local D1 smoke", () => {
  it("starts a raid boss run from an active operation preview and stores a sanitized snapshot", async () => {
    const db = await createMigratedDb();
    const authorization = await seedStartableRunPreview(db);

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: authorization,
          "Idempotency-Key": "run-start-smoke-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview-start-smoke-1" }),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accepted: true,
      startStatus: "STARTED",
      operationPreviewId: "preview-start-smoke-1",
      operationId: "raid_raider_king_engine",
      encounterType: "RAID_BOSS",
      riskLevel: "V",
      balanceVersion: "701",
      ordinary: null,
      raid: {
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
      },
      startAugmentPackageRef: "act1_full_start_package_v1",
      startTacticPackageRef: "act1_full_start_package_v1",
      errorCode: null,
    });

    const runId = String(body.runId);
    expect(runId).toMatch(/^run_/u);
    expect(db.exec(`SELECT status FROM operation_previews WHERE id = 'preview-start-smoke-1';`)[0]?.values).toEqual([["CONSUMED"]]);
    expect(db.exec(`SELECT account_id, preview_id, status, balance_config_version FROM runs WHERE id = '${runId}';`)[0]?.values).toEqual([
      ["account-start-smoke-1", "preview-start-smoke-1", "ACTIVE", 701],
    ]);
    const snapshotRows = db.exec(`SELECT snapshot_json FROM run_snapshots WHERE run_id = '${runId}';`);
    const snapshot = JSON.parse(String(snapshotRows[0]?.values[0]?.[0])) as Record<string, unknown>;
    expect(snapshot).toMatchObject({
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
    });
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("animationTimeline");

    const replayResponse = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: authorization,
          "Idempotency-Key": "run-start-smoke-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview-start-smoke-1" }),
      },
      createEnv(db),
    );

    expect(replayResponse.status).toBe(200);
    await expect(replayResponse.json()).resolves.toMatchObject({
      accepted: true,
      startStatus: "STARTED",
      runId,
      operationPreviewId: "preview-start-smoke-1",
      errorCode: null,
    });
    expect(db.exec(`SELECT COUNT(*) FROM runs WHERE preview_id = 'preview-start-smoke-1';`)[0]?.values).toEqual([[1]]);
    expect(db.exec(`SELECT COUNT(*) FROM run_snapshots WHERE run_id = '${runId}';`)[0]?.values).toEqual([[1]]);
    expect(db.exec(`SELECT operation, result_ref FROM idempotency_records WHERE operation = 'run_start';`)[0]?.values).toEqual([
      ["run_start", runId],
    ]);
    expect(db.exec("PRAGMA foreign_key_check;")[0]?.values ?? []).toEqual([]);
  });

  it("creates a reward preview once from an accepted run finish", async () => {
    const db = await createMigratedDb();
    const authorization = await seedFinishableRun(db);

    const response = await app.request(
      "/v1/runs/run-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify({
          runId: "run-smoke-1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run-smoke-1",
      accepted: true,
      resultStatus: "ACCEPTED",
      rewardClaimId: null,
      balanceVersion: "1",
      errorCode: null,
      message: null,
    });
    expect(body.rewardPreviewId).toMatch(/^reward_preview_/u);
    expect(db.exec("SELECT outcome, cleared_waves FROM run_final_results WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([
      ["VICTORY", 15],
    ]);
    const finalPayload = JSON.parse(String(db.exec("SELECT payload_json FROM run_final_results WHERE run_id = 'run-smoke-1';")[0]?.values[0]?.[0])) as Record<string, unknown>;
    expect(finalPayload.runVersionSnapshot).toMatchObject({
      source: "server_accepted_run_start",
      clientInstalled: {
        versionCode: "100",
        platform: "android",
        channel: "internal",
      },
      clientAdmission: {
        status: "ACCEPTED",
        minimumVersionCode: "90",
        updateRequired: false,
      },
      serverAcceptedDeployment: {
        environment: "DEV",
        runtimeDeploymentId: "cf-runtime-deploy-smoke",
      },
      contentRelease: {
        status: "APPROVED_COMPATIBLE",
        releaseId: "content_act1_001",
        contentVersion: "act1.2026-09-06.001",
        catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
      },
    });
    expect(JSON.stringify(finalPayload)).not.toContain("catalogUrl");
    expect(JSON.stringify(finalPayload)).not.toContain("Authorization");
    expect(JSON.stringify(finalPayload)).not.toContain("rawRng");
    expect(db.exec("SELECT status FROM runs WHERE id = 'run-smoke-1';")[0]?.values).toEqual([["COMPLETED"]]);
    const previewRows = db.exec(
      "SELECT status, balance_config_version, reward_plan_json FROM run_reward_previews WHERE run_id = 'run-smoke-1';",
    );
    expect(previewRows[0]?.values.length).toBe(1);
    const rewardPlanJson = String(previewRows[0]?.values[0]?.[2]);
    const rewardPlan = JSON.parse(rewardPlanJson) as {
      rewardLines: Array<{ kind?: string; id?: string; quantity?: number; source?: string }>;
      calculation: { risk: string; firstClearEligible: boolean; firstClearApplied: boolean };
    };
    expect(rewardPlan.rewardLines.length).toBeGreaterThan(0);
    expect(rewardPlan.rewardLines.some((line) => line.source === "FIRST_CLEAR")).toBe(true);
    expect(rewardPlan.rewardLines).toContainEqual({
      kind: "ITEM",
      id: "cur_command_ticket",
      quantity: 1,
      source: "FIRST_CLEAR",
    });
    expect(rewardPlan.calculation.risk).toBe("III");
    expect(rewardPlan.calculation.firstClearEligible).toBe(true);
    expect(rewardPlan.calculation.firstClearApplied).toBe(true);
    expect(
      db.exec(
        "SELECT operation_key, risk, run_id FROM account_first_clears WHERE account_id = 'account-smoke-1';",
      )[0]?.values,
    ).toEqual([["opn_03_water_stop", "III", "run-smoke-1"]]);
    expect(
      db.exec(
        `SELECT highest_story_progress_id, highest_cleared_operation_id, cleared_operations_json, story_flags_json, last_progress_run_id
         FROM account_story_progress
         WHERE account_id = 'account-smoke-1';`,
      )[0]?.values,
    ).toEqual([
      [
        "opn_03_water_stop",
        "opn_03_water_stop",
        JSON.stringify(["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop"]),
        JSON.stringify(["operation_cleared:opn_03_water_stop"]),
        "run-smoke-1",
      ],
    ]);
    expect(
      db.exec(
        "SELECT unlock_id FROM account_unlocks WHERE account_id = 'account-smoke-1' AND unlock_id = 'operation:opn_04_scrapstar_show';",
      )[0]?.values,
    ).toEqual([["operation:opn_04_scrapstar_show"]]);
    expect(
      db.exec(
        "SELECT progression_type, target_id FROM progression_ledger WHERE account_id = 'account-smoke-1' AND progression_type = 'STORY_PROGRESS';",
      )[0]?.values,
    ).toEqual([["STORY_PROGRESS", "opn_03_water_stop"]]);
    expect(db.exec("SELECT operation, result_ref FROM idempotency_records WHERE account_id = 'account-smoke-1';")[0]?.values).toEqual([
      ["run_finish", body.rewardPreviewId],
    ]);

    const replayResponse = await app.request(
      "/v1/runs/run-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify({
          runId: "run-smoke-1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      createEnv(db),
    );

    expect(replayResponse.status).toBe(200);
    await expect(replayResponse.json()).resolves.toMatchObject({
      runId: "run-smoke-1",
      accepted: false,
      resultStatus: "DUPLICATE_REPLAY",
      rewardPreviewId: body.rewardPreviewId,
      balanceVersion: "1",
      errorCode: "duplicate_replay",
    });
    expect(db.exec("SELECT COUNT(*) FROM run_final_results WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("SELECT COUNT(*) FROM run_reward_previews WHERE run_id = 'run-smoke-1';")[0]?.values).toEqual([[1]]);
    expect(db.exec("SELECT COUNT(*) FROM progression_ledger WHERE account_id = 'account-smoke-1';")[0]?.values).toEqual([[1]]);

    const claimResponse = await app.request(
      `/v1/runs/run-smoke-1/reward-preview/${body.rewardPreviewId}/claim`,
      {
        method: "POST",
        headers: { "Idempotency-Key": "run-reward-claim-ticket-smoke-idem-1" },
        body: JSON.stringify({ runId: "run-smoke-1", rewardPreviewId: body.rewardPreviewId }),
      },
      createEnv(db),
    );

    expect(claimResponse.status).toBe(200);
    await expect(claimResponse.json()).resolves.toMatchObject({
      runId: "run-smoke-1",
      rewardPreviewId: body.rewardPreviewId,
      status: "CLAIMED",
      rewardLines: expect.arrayContaining([
        {
          kind: "ITEM",
          id: "cur_command_ticket",
          quantity: 1,
          source: "FIRST_CLEAR",
          balanceAfter: 1,
        },
      ]),
      errorCode: null,
    });
    expect(
      db.exec(
        "SELECT item_id, quantity FROM account_items WHERE account_id = 'account-smoke-1' AND item_id = 'cur_command_ticket';",
      )[0]?.values,
    ).toEqual([["cur_command_ticket", 1]]);
    expect(
      db.exec(
        "SELECT item_id, delta, quantity_after, reason, source_type FROM item_ledger WHERE account_id = 'account-smoke-1' AND item_id = 'cur_command_ticket';",
      )[0]?.values,
    ).toEqual([["cur_command_ticket", 1, 1, "RUN_REWARD_ITEM_GRANT", "RUN_REWARD_CLAIM"]]);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });

  it("commits story raid failure stamina refund once and replays without another fuel grant", async () => {
    const db = await createMigratedDb();
    const authorization = await seedFinishableStoryRaidFailureRun(db);

    const requestBody = {
      runId: "run-raid-failure-smoke-1",
      currentStage: "Complete",
      currentWaveNumber: 0,
      completedWaveCount: 0,
      encounterType: "RAID_BOSS",
      raidResult: {
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossDefeated: false,
        completedPhaseCount: 2,
        encounterDurationSeconds: 360,
      },
      outcome: "FAILURE",
      runtimeStateJson: "{}",
      tacticSelections: [
        { tacticId: "tactic_full_start_raid", packageRef: "act1_full_start_package_v1" },
      ],
      clientCompletedAt: "2026-08-27T00:00:00.000Z",
    };

    const response = await app.request(
      "/v1/runs/run-raid-failure-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-raid-failure-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify(requestBody),
      },
      createEnv(db),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run-raid-failure-smoke-1",
      accepted: true,
      resultStatus: "ACCEPTED",
      balanceVersion: "704",
      staminaRefund: {
        status: "COMMITTED",
        refundType: "STAMINA",
        reason: "STORY_RAID_FAILURE",
        policyRef: "act1_story_raid_failure_stamina_refund_v1",
      },
      errorCode: null,
    });
    expect(scalar(db, "SELECT fuel FROM fuel_state WHERE account_id = 'account-raid-failure-smoke-1';")).toBe(90);
    expect(
      db.exec(
        "SELECT delta, fuel_after, reason FROM fuel_ledger WHERE account_id = 'account-raid-failure-smoke-1';",
      )[0]?.values,
    ).toEqual([[10, 90, "STORY_RAID_FAILURE_REFUND"]]);
    expect(
      db.exec(
        "SELECT mutation_type FROM account_mutations WHERE account_id = 'account-raid-failure-smoke-1' ORDER BY sequence;",
      )[0]?.values,
    ).toEqual([["RUN_START"], ["STORY_RAID_STAMINA_REFUND"]]);
    expect(scalar(db, "SELECT COUNT(*) FROM account_first_clears WHERE account_id = 'account-raid-failure-smoke-1';")).toBe(0);
    expect(scalar(db, "SELECT COUNT(*) FROM account_story_progress WHERE account_id = 'account-raid-failure-smoke-1';")).toBe(0);

    const finalPayload = JSON.parse(String(db.exec(
      "SELECT payload_json FROM run_final_results WHERE run_id = 'run-raid-failure-smoke-1';",
    )[0]?.values[0]?.[0])) as { staminaRefund?: { status?: string } };
    expect(finalPayload.staminaRefund?.status).toBe("COMMITTED");

    const replayResponse = await app.request(
      "/v1/runs/run-raid-failure-smoke-1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-raid-failure-smoke-idem-1",
          Authorization: authorization,
        },
        body: JSON.stringify(requestBody),
      },
      createEnv(db),
    );

    expect(replayResponse.status).toBe(200);
    await expect(replayResponse.json()).resolves.toMatchObject({
      runId: "run-raid-failure-smoke-1",
      accepted: false,
      resultStatus: "DUPLICATE_REPLAY",
      rewardPreviewId: body.rewardPreviewId,
      staminaRefund: {
        status: "COMMITTED",
        reason: "STORY_RAID_FAILURE",
      },
      errorCode: "duplicate_replay",
    });
    expect(scalar(db, "SELECT fuel FROM fuel_state WHERE account_id = 'account-raid-failure-smoke-1';")).toBe(90);
    expect(scalar(db, "SELECT COUNT(*) FROM fuel_ledger WHERE account_id = 'account-raid-failure-smoke-1';")).toBe(1);
    expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records WHERE operation = 'run_finish';")).toBe(1);
    expect(JSON.stringify(body)).not.toContain("runStartCost");
    expect(JSON.stringify(body)).not.toContain("fuelAfter");
    expect(JSON.stringify(body)).not.toContain("rawRng");
    expect(JSON.stringify(body)).not.toContain("animationTimeline");
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });
});
