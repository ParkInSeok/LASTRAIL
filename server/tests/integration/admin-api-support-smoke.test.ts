import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/admin-api/src/index";

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
}

class SqlJsD1Database {
  private readonly database: initSqlJs.Database;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }
}

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(19);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env {
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

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

function seedSupportAccount(db: initSqlJs.Database): void {
  const now = Date.parse("2026-08-27T00:00:00.000Z");
  const runStartedAt = Date.parse("2026-08-27T01:00:00.000Z");
  const runFinishedAt = Date.parse("2026-08-27T01:15:00.000Z");
  const offlineClaimAt = Date.parse("2026-08-27T02:00:00.000Z");
  const gachaAt = Date.parse("2026-08-27T03:00:00.000Z");
  const loginBonusAt = Date.parse("2026-08-27T00:45:00.000Z");
  const activeSessionExpiresAt = Date.parse("2099-09-03T00:00:00.000Z");

  db.run(
    `INSERT INTO admin_principals (
       id, access_subject, role, status, display_name, created_at, updated_at
     ) VALUES ('principal-support-1', 'access|support-viewer', 'VIEWER', 'ACTIVE', 'Support Viewer', ?, ?);`,
    [now, now],
  );
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-support-1', 'ACTIVE', ?, ?);", [
    now - 24 * 60 * 60 * 1000,
    now,
  ]);
  db.run(
    `INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
     VALUES ('account-support-1', 'Conductor', 0, ?, ?);`,
    [now, now],
  );
  db.run(
    `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
     VALUES ('identity-support-1', 'account-support-1', 'GUEST', 'guest-support-1', ?);`,
    [now],
  );
  db.run(
    `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
     VALUES ('identity-support-google-1', 'account-support-1', 'GOOGLE', 'google-subject-support-1', ?);`,
    [now + 1],
  );
  db.run(
    `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
     VALUES
       ('session-support-1', 'account-support-1', 'hash-support-1', 'family-support-1', 0, 'ACTIVE', ?, ?, NULL),
       ('session-support-expired-1', 'account-support-1', 'hash-support-expired-1', 'family-support-expired-1', 0, 'ACTIVE', ?, ?, NULL),
      ('session-support-rotated-1', 'account-support-1', 'hash-support-rotated-1', 'family-support-rotated-1', 0, 'ROTATED', ?, ?, ?);`,
    [
      now,
      activeSessionExpiresAt,
      now - 2 * 24 * 60 * 60 * 1000,
      now - 24 * 60 * 60 * 1000,
      now + 30 * 60 * 1000,
      now + 7 * 24 * 60 * 60 * 1000,
      now + 45 * 60 * 1000,
    ],
  );
  db.run(
    `INSERT INTO balance_config_versions (
       version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
     ) VALUES (1, 1, 1, 'sha256:support', '{}', ?, 'principal-support-1', NULL);`,
    [now],
  );
  db.run(
    `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
     VALUES
       ('mutation-run-support-1', 'account-support-1', 1, 'RUN_START', 'run-start-support-1', ?),
       ('mutation-offline-support-1', 'account-support-1', 2, 'OFFLINE_REWARD_CLAIM', 'offline-support-1', ?),
       ('mutation-gacha-support-1', 'account-support-1', 3, 'GACHA_PULL', 'gacha-support-1', ?),
       ('mutation-login-bonus-support-1', 'account-support-1', 4, 'LOGIN_BONUS_CLAIM', 'login-bonus-support-1', ?);`,
    [runStartedAt, offlineClaimAt, gachaAt, loginBonusAt],
  );
  db.run(
    `INSERT INTO operation_previews (
       id, account_id, mutation_id, objective, risk, balance_config_version,
       preview_json, status, expires_at, created_at
     ) VALUES (?, 'account-support-1', 'mutation-run-support-1', 'Act 1 operation', 'IV', 1, ?, 'CONSUMED', ?, ?);`,
    [
      "operation-preview-support-1",
      JSON.stringify({ operationId: "opn_04_scrapstar_show", actId: "act_01_wasteland_railway" }),
      runStartedAt + 60 * 60 * 1000,
      runStartedAt,
    ],
  );
  db.run(
    `INSERT INTO runs (
       id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
     ) VALUES ('run-support-1', 'account-support-1', 'operation-preview-support-1', 'mutation-run-support-1', 1, 'COMPLETED', ?, ?, NULL);`,
    [runStartedAt, runFinishedAt],
  );
  db.run(
    `INSERT INTO run_final_results (run_id, outcome, cleared_waves, payload_json, submitted_at)
     VALUES ('run-support-1', 'VICTORY', 15, '{}', ?);`,
    [runFinishedAt],
  );
  db.run(
    `INSERT INTO run_reward_previews (
       id, account_id, run_id, balance_config_version, status, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     ) VALUES ('reward_preview_support_1', 'account-support-1', 'run-support-1', 1, 'ACTIVE', ?, ?, ?, NULL, NULL);`,
    [
      JSON.stringify({
        rewardLines: [{ kind: "CURRENCY", id: "cur_credits", quantity: 180, source: "RUN_REWARD" }],
      }),
      runFinishedAt + 24 * 60 * 60 * 1000,
      runFinishedAt,
    ],
  );
  db.run(
    `INSERT INTO account_story_progress (
       account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
       cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
     ) VALUES ('account-support-1', 'act_01_wasteland_railway', 'opn_04_scrapstar_show', 'opn_04_scrapstar_show', ?, ?, ?, 'run-support-1', ?);`,
    [
      JSON.stringify(["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show"]),
      JSON.stringify(["operation_cleared:opn_04_scrapstar_show"]),
      "act1.2026-08-27.001",
      runFinishedAt,
    ],
  );
  db.run(
    `INSERT INTO account_unlocks (account_id, unlock_id, unlocked_at)
     VALUES ('account-support-1', 'operation:opn_05_night_boarders', ?);`,
    [runFinishedAt],
  );
  db.run(
    `INSERT INTO account_offline_reward_state (
       account_id, last_offline_reward_claim_at, last_story_progress_id,
       last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
     ) VALUES ('account-support-1', ?, 'opn_04_scrapstar_show', 1, 'act1.2026-08-27.001', NULL, 1, ?);`,
    [offlineClaimAt, offlineClaimAt],
  );
  db.run(
    `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
     VALUES ('offline_claim_support_1', 'account-support-1', NULL, 'mutation-offline-support-1', 1, ?, ?);`,
    [
      JSON.stringify({
        storyProgressId: "opn_04_scrapstar_show",
        rewardLines: [{ kind: "CURRENCY", id: "cur_credits", quantity: 240, source: "OFFLINE_REWARD" }],
      }),
      offlineClaimAt,
    ],
  );
  db.run(
    `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
     VALUES ('login_bonus_claim_support_1', 'account-support-1', NULL, 'mutation-login-bonus-support-1', NULL, ?, ?);`,
    [
      JSON.stringify({
        bonusId: "launch_login_bonus_act1_v1",
        rewardLines: [
          { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS", balanceAfter: 5 },
        ],
      }),
      loginBonusAt,
    ],
  );
  db.run(
    `INSERT INTO account_login_bonus_claims (
       account_id, bonus_id, reward_transaction_id, claimed_at, idempotency_key, reward_json
     ) VALUES ('account-support-1', 'launch_login_bonus_act1_v1', 'login_bonus_claim_support_1', ?, 'login-bonus-support-key-1', ?);`,
    [
      loginBonusAt,
      JSON.stringify({
        rewardLines: [
          { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS", balanceAfter: 5 },
        ],
      }),
    ],
  );
  db.run(
    `INSERT INTO gacha_banners (
       id, banner_type, status, balance_config_version, pickup_operator_id,
       starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
     ) VALUES ('banner-support-1', 'PICKUP', 'ACTIVE', 1, 'operator_pickup_1', ?, NULL, '{}', ?, ?, ?);`,
    [
      now,
      JSON.stringify({ results: [{ resultId: "operator_pickup_1", rarity: "ELITE", weight: 1 }] }),
      now,
      now,
    ],
  );
  db.run(
    `INSERT INTO gacha_pull_batches (
       id, account_id, banner_id, mutation_id, idempotency_key, pull_count,
       cost_item_id, cost_quantity, cost_ledger_id, pre_pity_json, post_pity_json,
       rng_seed_hash, result_summary_json, created_at
     ) VALUES ('gacha_batch_support_1', 'account-support-1', 'banner-support-1', 'mutation-gacha-support-1',
       'gacha-support-key-1', 10, 'cur_command_ticket', 10, NULL, '{}', ?, 'sha256:gacha-support', ?, ?);`,
    [
      JSON.stringify({ bannerId: "banner-support-1", totalPullCount: 10 }),
      JSON.stringify({
        responseResults: [{ resultId: "operator_pickup_1", pullIndex: 1, guaranteeType: "PICKUP_ELITE_60" }],
        postPityState: { bannerId: "banner-support-1", totalPullCount: 10 },
      }),
      gachaAt,
    ],
  );
  db.run(
    `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
     VALUES
       ('idem-run-support-1', 'account-support-1', 'finish-support-key-1', 'run_finish', 'reward_preview_support_1', ?),
       ('idem-offline-support-1', 'account-support-1', 'offline-support-key-1', 'offline_reward_claim', 'offline_claim_support_1', ?),
       ('idem-gacha-support-1', 'account-support-1', 'gacha-support-key-1', 'gacha_pull', 'gacha_batch_support_1', ?),
       ('idem-login-bonus-support-1', 'account-support-1', 'login-bonus-support-key-1', 'login_bonus_claim', 'login_bonus_claim_support_1', ?);`,
    [runFinishedAt, offlineClaimAt, gachaAt, loginBonusAt],
  );
  db.run(
    `INSERT INTO integrity_events (
       id, account_id, run_id, balance_config_version, run_start_build_id, submission_build_id,
       original_validator_version_id, runtime_revision, evaluated_validator_version_id,
       category, evidence_code, evidence_json, fingerprint, classification, created_at
     ) VALUES ('integrity-support-1', 'account-support-1', 'run-support-1', 1, NULL, NULL, NULL, 1, NULL,
       'RUN_VALIDATION', 'ABNORMAL_CLEAR_TIME', '{}', 'fingerprint-support-1', 'SOFT_ANOMALY', ?);`,
    [gachaAt],
  );
  db.run(
    `INSERT INTO suspicious_player_reviews (
       account_id, status, assigned_to, reason, updated_by, updated_at, created_at
     ) VALUES ('account-support-1', 'REVIEWING', NULL, 'support smoke', 'principal-support-1', ?, ?);`,
    [gachaAt, gachaAt],
  );
  db.run(
    `INSERT INTO ops_error_logs (
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     ) VALUES
       ('ops-support-critical', ?, 'DEV', 'game_api', 'critical', 'gacha_state_conflict',
        'Gacha state conflict detected.', 'open', 'game_api', 'req-gacha', 'account-support-1',
        NULL, NULL, NULL, NULL, 'gacha_batch_support_1', 'banner-support-1', NULL, NULL, NULL,
        'gacha_pull', 'sha256:gacha-key', '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-error', ?, 'DEV', 'game_api', 'error', 'offline_reward_state_conflict',
        'Offline reward state conflict detected.', 'acknowledged', 'game_api', 'req-offline', 'account-support-1',
        NULL, NULL, NULL, 'offline_claim_support_1', NULL, NULL, NULL, NULL, NULL,
        'offline_reward_claim', 'sha256:offline-key', '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-old', ?, 'DEV', 'game_api', 'error', 'google_auth_config_missing',
        'Google auth config missing.', 'open', 'game_api', 'req-google', 'account-support-1',
        NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
        NULL, NULL, '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-warning-new', ?, 'DEV', 'game_api', 'warning', 'story_progress_state_conflict',
        'Story progress state conflict detected.', 'open', 'game_api', 'req-story-new', 'account-support-1',
        'run-support-1', NULL, NULL, NULL, NULL, NULL, 'opn_04_scrapstar_show', NULL, NULL,
        'run_finish', 'sha256:story-new-key', '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-warning-old', ?, 'DEV', 'game_api', 'warning', 'offline_reward_state_conflict',
        'Older offline warning.', 'open', 'game_api', 'req-warning-old', 'account-support-1',
        NULL, NULL, NULL, 'offline_claim_support_1', NULL, NULL, NULL, NULL, NULL,
        'offline_reward_claim', 'sha256:warning-old-key', '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-info-excluded', ?, 'DEV', 'game_api', 'info', 'google_auth_config_missing',
        'Newest info should not displace higher severity rows.', 'open', 'game_api', 'req-info', 'account-support-1',
        NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
        NULL, NULL, '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?),
       ('ops-support-other-account', ?, 'DEV', 'game_api', 'critical', 'story_progress_state_conflict',
        'Other account conflict.', 'open', 'game_api', 'req-story-other', NULL,
        NULL, NULL, NULL, NULL, NULL, NULL, 'opn_04_scrapstar_show', NULL, NULL,
        'run_finish', 'sha256:other-key', '{}', '{"safe":true}', 0, 1, NULL, NULL, NULL, NULL, NULL, ?);`,
    [
      gachaAt + 3000,
      gachaAt + 3000,
      gachaAt + 4000,
      gachaAt + 4000,
      gachaAt + 1000,
      gachaAt + 1000,
      gachaAt + 2000,
      gachaAt + 2000,
      gachaAt + 500,
      gachaAt + 500,
      gachaAt + 6000,
      gachaAt + 6000,
      gachaAt + 5000,
      gachaAt + 5000,
    ],
  );
}

describe("admin-api support account summary local D1 smoke", () => {
  it("reads sanitized support sections without mutating migrated D1 state", async () => {
    const db = await createMigratedDb();
    seedSupportAccount(db);
    const env = createEnv(db);

    const beforeMutations = scalar(db, "SELECT COUNT(*) FROM account_mutations;");
    const beforeIdempotency = scalar(db, "SELECT COUNT(*) FROM idempotency_records;");
    const response = await app.request(
      "/v1/admin/support/accounts/account-support-1",
      { headers: { "Cf-Access-Subject": "access|support-viewer" } },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      accountId: "account-support-1",
      account: {
        accountStatus: "ACTIVE",
        displayName: "Conductor",
      },
      auth: {
        identityProviders: ["GOOGLE", "GUEST"],
        providerNames: ["GOOGLE", "GUEST"],
        linkedProviders: ["GOOGLE", "GUEST"],
        primaryProvider: "GOOGLE",
        hasGoogleLink: true,
        activeSessionCount: 1,
        latestSessionCreatedAt: "2026-08-27T00:30:00.000Z",
        latestSessionAt: "2026-08-27T00:30:00.000Z",
        latestAuthAt: "2026-08-27T00:30:00.000Z",
        lastLoginAt: "2026-08-27T00:30:00.000Z",
        latestSessionExpiresAt: "2099-09-03T00:00:00.000Z",
        lastSeenAt: null,
        lastSeenSource: "not_available_without_activity_source",
        concurrentUsers: null,
        concurrentUsersPrecision: "UNAVAILABLE",
      },
      recentRuns: [
        {
          runId: "run-support-1",
          operationId: "opn_04_scrapstar_show",
          actId: "act_01_wasteland_railway",
          outcome: "VICTORY",
          rewardPreviewId: "reward_preview_support_1",
        },
      ],
      offlineRewards: {
        storyProgressId: "opn_04_scrapstar_show",
        lastClaim: {
          offlineRewardClaimId: "offline_claim_support_1",
        },
      },
      storyProgress: {
        storyProgress: {
          highestStoryProgressId: "opn_04_scrapstar_show",
          order: 4,
        },
        unlockedOperations: ["opn_01_last_departure", "opn_05_night_boarders"],
      },
      gacha: {
        recentBatches: [
          {
            batchId: "gacha_batch_support_1",
            guaranteeTypes: ["PICKUP_ELITE_60"],
          },
        ],
      },
      loginBonus: {
        bonusId: "launch_login_bonus_act1_v1",
        claimStatus: "CLAIMED",
        claimedAt: "2026-08-27T00:45:00.000Z",
        rewardTransactionId: "login_bonus_claim_support_1",
        expectedRewardLines: [
          { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS" },
        ],
        rewardLineSummary: [
          { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS", balanceAfter: 5 },
        ],
        idempotencyReplayKnown: true,
        readOnly: true,
      },
      integrity: {
        openSignals: 1,
        accountReviewStatus: "REVIEWING",
      },
      opsErrors: [
        {
          errorLogId: "ops-support-critical",
          occurredAt: "2026-08-27T03:00:03.000Z",
          environment: "DEV",
          service: "game_api",
          severity: "critical",
          errorCode: "gacha_state_conflict",
          messageSummary: "Gacha state conflict detected.",
          status: "open",
          source: "game_api",
          requestId: "req-gacha",
          retryable: false,
          masked: true,
          links: {
            accountId: "account-support-1",
            gachaBatchId: "gacha_batch_support_1",
            gachaBannerId: "banner-support-1",
          },
        },
        {
          errorLogId: "ops-support-error",
          errorCode: "offline_reward_state_conflict",
          severity: "error",
          status: "acknowledged",
          offlineRewardClaimId: "offline_claim_support_1",
        },
        {
          errorLogId: "ops-support-old",
          errorCode: "google_auth_config_missing",
          severity: "error",
        },
        {
          errorLogId: "ops-support-warning-new",
          errorCode: "story_progress_state_conflict",
          severity: "warning",
        },
        {
          errorLogId: "ops-support-warning-old",
          errorCode: "offline_reward_state_conflict",
          severity: "warning",
        },
      ],
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        repairEndpointsEnabled: false,
      },
    });
    expect(body.auth.lastLoginAgeSeconds).toBeGreaterThan(0);
    expect(body.opsErrors).toHaveLength(5);
    expect(body.opsErrors.map((item: { errorLogId: string }) => item.errorLogId)).not.toContain("ops-support-info-excluded");
    expect(body.opsErrors.map((item: { errorLogId: string }) => item.errorLogId)).not.toContain("ops-support-other-account");
    expect(body.idempotency).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ operation: "run_finish", resultRef: "reward_preview_support_1" }),
        expect.objectContaining({ operation: "offline_reward_claim", resultRef: "offline_claim_support_1" }),
        expect.objectContaining({ operation: "login_bonus_claim", resultRef: "login_bonus_claim_support_1" }),
        expect.objectContaining({ operation: "gacha_pull", resultRef: "gacha_batch_support_1" }),
      ]),
    );

    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("refresh_token_hash");
    expect(serialized).not.toContain("Authorization");
    expect(serialized).not.toContain("rawBody");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rngSeed");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("fullWeight");
    expect(serialized).not.toContain("sanitizedContext");
    expect(serialized).not.toContain("rawStack");
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("storyProgressMultiplierBps");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("X-Account-Id");

    expect(scalar(db, "SELECT COUNT(*) FROM account_mutations;")).toBe(beforeMutations);
    expect(scalar(db, "SELECT COUNT(*) FROM idempotency_records;")).toBe(beforeIdempotency);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
  });
});
