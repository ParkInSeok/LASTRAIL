import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|1[0-4])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(14);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

describe("D1 migrations", () => {
  it("apply 0001 through 0014 and pass SQLite integrity checks", async () => {
    const db = await createMigratedDb();

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });

  it("creates account-level suspicious player review tables", async () => {
    const db = await createMigratedDb();

    const reviews = db.exec("PRAGMA table_info(suspicious_player_reviews);");
    const history = db.exec("PRAGMA table_info(suspicious_player_review_history);");

    expect(reviews[0]?.values.map((row) => row[1])).toEqual([
      "account_id",
      "status",
      "assigned_to",
      "reason",
      "updated_by",
      "updated_at",
      "created_at",
    ]);
    expect(history[0]?.values.map((row) => row[1])).toEqual([
      "id",
      "account_id",
      "from_status",
      "to_status",
      "reason",
      "created_by",
      "created_at",
      "request_id",
      "audit_log_id",
    ]);
    db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-review-1', 'ACTIVE', 1, 1);");
    db.run(
      `INSERT INTO admin_principals (
         id, access_subject, role, status, display_name, created_at, updated_at
       ) VALUES (
         'principal-review-1', 'access|review-1', 'OWNER', 'ACTIVE', 'Review Owner', 1, 1
       );`
    );
    db.run(
      `INSERT INTO suspicious_player_reviews (
         account_id, status, assigned_to, reason, updated_by, updated_at, created_at
       ) VALUES (
         'account-review-1', 'REWARD_HELD', NULL, 'mark-only review', 'principal-review-1', 2, 1
       );`
    );

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("creates run reward preview table for claimable run rewards", async () => {
    const db = await createMigratedDb();

    const preview = db.exec("PRAGMA table_info(run_reward_previews);");
    const indexes = db.exec("PRAGMA index_list(run_reward_previews);");

    expect(preview[0]?.values.map((row) => row[1])).toEqual([
      "id",
      "account_id",
      "run_id",
      "balance_config_version",
      "status",
      "reward_plan_json",
      "expires_at",
      "created_at",
      "claimed_at",
      "reward_claim_id",
    ]);
    expect(indexes[0]?.values.map((row) => row[1])).toEqual(
      expect.arrayContaining([
        "idx_run_reward_previews_account_status_expires",
        "idx_run_reward_previews_run_status",
      ])
    );
    db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-reward-1', 'ACTIVE', 1, 1);");
    db.run(
      `INSERT INTO balance_config_drafts (
         id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
       ) VALUES (
         'draft-reward-1', NULL, 1, 1, '{}', 'checksum-reward-1', 1, 'PUBLISHED', 'test', 1, 1
       );`
    );
    db.run(
      `INSERT INTO balance_config_versions (
         version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
       ) VALUES (
         101, 1, 1, 'checksum-reward-1', '{}', 1, 'test', 'draft-reward-1'
       );`
    );
    db.run(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES ('mutation-run-1', 'account-reward-1', 1, 'RUN_START', 'idem-run-start-1', 1);`
    );
    db.run(
      `INSERT INTO runs (
         id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id
       ) VALUES (
         'run-reward-1', 'account-reward-1', NULL, 'mutation-run-1', 101, 'FINISH_PENDING', 1, NULL, NULL
       );`
    );
    db.run(
      `INSERT INTO run_reward_previews (
         id, account_id, run_id, balance_config_version, status, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
       ) VALUES (
         'reward-preview-1', 'account-reward-1', 'run-reward-1', 101, 'ACTIVE', '{"lines":[]}', 86400001, 1, NULL, NULL
       );`
    );

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("creates gacha pity and result history tables with inventory ledger linkage", async () => {
    const db = await createMigratedDb();

    const tableColumns = (tableName: string) =>
      db.exec(`PRAGMA table_info(${tableName});`)[0]?.values.map((row) => row[1]);
    const indexNames = (tableName: string) =>
      db.exec(`PRAGMA index_list(${tableName});`)[0]?.values.map((row) => row[1]) ?? [];

    expect(tableColumns("account_items")).toEqual(["account_id", "item_id", "quantity", "updated_at"]);
    expect(tableColumns("item_ledger")).toEqual([
      "id",
      "account_id",
      "mutation_id",
      "item_id",
      "delta",
      "quantity_after",
      "reason",
      "source_type",
      "source_id",
      "created_at",
    ]);
    expect(tableColumns("gacha_banners")).toEqual([
      "id",
      "banner_type",
      "status",
      "balance_config_version",
      "pickup_operator_id",
      "starts_at",
      "ends_at",
      "pity_config_json",
      "pool_snapshot_json",
      "created_at",
      "updated_at",
    ]);
    expect(tableColumns("player_gacha_pity")).toEqual([
      "account_id",
      "banner_id",
      "total_pull_count",
      "elite_pity_count",
      "pickup_elite_pity_count",
      "rare_or_higher_pity_count",
      "revision",
      "last_batch_id",
      "updated_at",
    ]);
    expect(tableColumns("gacha_pull_batches")).toEqual([
      "id",
      "account_id",
      "banner_id",
      "mutation_id",
      "idempotency_key",
      "pull_count",
      "cost_item_id",
      "cost_quantity",
      "cost_ledger_id",
      "pre_pity_json",
      "post_pity_json",
      "rng_seed_hash",
      "result_summary_json",
      "created_at",
    ]);
    expect(tableColumns("gacha_pull_results")).toEqual([
      "id",
      "batch_id",
      "account_id",
      "banner_id",
      "pull_sequence",
      "result_rarity",
      "result_type",
      "result_id",
      "is_pickup",
      "guarantee_type",
      "was_duplicate",
      "duplicate_conversion_json",
      "operator_id",
      "item_ledger_id",
      "progression_ledger_id",
      "created_at",
    ]);
    expect(indexNames("item_ledger")).toContain("idx_item_ledger_account_item_created");
    expect(indexNames("gacha_pull_batches")).toEqual(
      expect.arrayContaining(["idx_gacha_pull_batches_account_created", "idx_gacha_pull_batches_banner_created"])
    );
    expect(indexNames("gacha_pull_results")).toEqual(
      expect.arrayContaining(["idx_gacha_pull_results_banner_rarity_created", "idx_gacha_pull_results_account_created"])
    );

    db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-gacha-1', 'ACTIVE', 1, 1);");
    db.run(
      `INSERT INTO balance_config_drafts (
         id, base_version, schema_version, required_content_version, config_json, checksum, revision, status, created_by, created_at, updated_at
       ) VALUES (
         'draft-gacha-1', NULL, 1, 1, '{}', 'checksum-gacha-1', 1, 'PUBLISHED', 'test', 1, 1
       );`
    );
    db.run(
      `INSERT INTO balance_config_versions (
         version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
       ) VALUES (
         201, 1, 1, 'checksum-gacha-1', '{}', 1, 'test', 'draft-gacha-1'
       );`
    );
    db.run(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES ('mutation-gacha-1', 'account-gacha-1', 1, 'GACHA_PULL', 'idem-gacha-1', 1);`
    );
    db.run(
      `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
       VALUES ('account-gacha-1', 'cur_command_ticket', 9, 2);`
    );
    db.run(
      `INSERT INTO item_ledger (
         id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at
       ) VALUES (
         'item-ledger-cost-1', 'account-gacha-1', 'mutation-gacha-1', 'cur_command_ticket', -1, 9, 'GACHA_PULL_COST', 'GACHA_PULL', 'gacha-batch-1', 2
       );`
    );
    db.run(
      `INSERT INTO gacha_banners (
         id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
       ) VALUES (
         'banner-gacha-1', 'PICKUP', 'ACTIVE', 201, 'op_vex', 1, NULL, '{"elite":30,"pickup":60,"rare":10}', '{"operators":["op_vex"]}', 1, 1
       );`
    );
    db.run(
      `INSERT INTO gacha_pull_batches (
         id, account_id, banner_id, mutation_id, idempotency_key, pull_count, cost_item_id, cost_quantity, cost_ledger_id,
         pre_pity_json, post_pity_json, rng_seed_hash, result_summary_json, created_at
       ) VALUES (
         'gacha-batch-1', 'account-gacha-1', 'banner-gacha-1', 'mutation-gacha-1', 'idem-gacha-1', 1,
         'cur_command_ticket', 1, 'item-ledger-cost-1', '{"elite":29}', '{"elite":0}', 'seed-hash-1', '{"elite":1}', 2
       );`
    );
    db.run(
      `INSERT INTO player_gacha_pity (
         account_id, banner_id, total_pull_count, elite_pity_count, pickup_elite_pity_count, rare_or_higher_pity_count, revision, last_batch_id, updated_at
       ) VALUES (
         'account-gacha-1', 'banner-gacha-1', 30, 0, 0, 0, 2, 'gacha-batch-1', 2
       );`
    );
    db.run(
      `INSERT INTO gacha_pull_results (
         id, batch_id, account_id, banner_id, pull_sequence, result_rarity, result_type, result_id, is_pickup, guarantee_type,
         was_duplicate, duplicate_conversion_json, operator_id, item_ledger_id, progression_ledger_id, created_at
       ) VALUES (
         'gacha-result-1', 'gacha-batch-1', 'account-gacha-1', 'banner-gacha-1', 1, 'ELITE', 'ELITE_OPERATOR',
         'op_vex', 1, 'ELITE_30', 0, NULL, 'op_vex', NULL, NULL, 2
       );`
    );

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("creates account story progress read model for durable story state", async () => {
    const db = await createMigratedDb();

    const columns = db.exec("PRAGMA table_info(account_story_progress);");
    const indexes = db.exec("PRAGMA index_list(account_story_progress);");

    expect(columns[0]?.values.map((row) => row[1])).toEqual([
      "account_id",
      "act_id",
      "highest_cleared_operation_id",
      "highest_story_progress_id",
      "cleared_operations_json",
      "story_flags_json",
      "narrative_version",
      "last_progress_run_id",
      "updated_at",
    ]);
    expect(indexes[0]?.values.map((row) => row[1])).toContain("idx_account_story_progress_act_progress");

    db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-story-1', 'ACTIVE', 1, 1);");
    db.run(
      `INSERT INTO account_story_progress (
         account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
         cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
       ) VALUES (
         'account-story-1', 'act_01_wasteland_railway', 'opn_02_first_signal', 'opn_02_first_signal',
         '["opn_01_last_departure","opn_02_first_signal"]', '["first_signal_restored"]',
         'act1-narrative-v1', NULL, 2
       );`,
    );

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("applies the DEV balance config seed and marks version 1 active", async () => {
    const db = await createMigratedDb();
    const seedPath = join(process.cwd(), "packages/db/seeds/dev/0001_balance_config_v1.sql");

    expect(existsSync(seedPath)).toBe(true);
    db.run(readFileSync(seedPath, "utf8"));

    const active = db.exec(`
      SELECT active.version, versions.schema_version, versions.required_content_version, versions.config_json
      FROM balance_config_active active
      JOIN balance_config_versions versions ON versions.version = active.version
      WHERE active.singleton_id = 1
    `);

    expect(active[0]?.values).toHaveLength(1);
    expect(active[0]?.values[0]?.[0]).toBe(1);
    expect(active[0]?.values[0]?.[1]).toBe(1);
    expect(active[0]?.values[0]?.[2]).toBe(1);
    expect(JSON.parse(String(active[0]?.values[0]?.[3]))).toEqual({
      fuel: {
        max: 100,
        runStartCost: 10,
        rechargeSeconds: 300,
      },
      train: {
        baseSpeed: 10,
      },
    });
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("applies the local admin principal seed for Admin Console development", async () => {
    const db = await createMigratedDb();
    const seedPath = join(process.cwd(), "packages/db/seeds/dev/0002_local_admin_principal.sql");

    expect(existsSync(seedPath)).toBe(true);
    db.run(readFileSync(seedPath, "utf8"));

    const principal = db.exec(`
      SELECT access_subject, role, status, display_name
      FROM admin_principals
      WHERE id = 'dev-local-owner'
    `);

    expect(principal[0]?.values).toEqual([["access|local-owner", "OWNER", "ACTIVE", "Local Owner"]]);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });

  it("applies sample Admin Console read-only data for local verification", async () => {
    const db = await createMigratedDb();
    const balanceSeedPath = join(process.cwd(), "packages/db/seeds/dev/0001_balance_config_v1.sql");
    const adminSeedPath = join(process.cwd(), "packages/db/seeds/dev/0002_local_admin_principal.sql");
    const sampleSeedPath = join(process.cwd(), "packages/db/seeds/dev/0003_admin_console_sample_data.sql");

    expect(existsSync(sampleSeedPath)).toBe(true);
    db.run(readFileSync(balanceSeedPath, "utf8"));
    db.run(readFileSync(adminSeedPath, "utf8"));
    db.run(readFileSync(sampleSeedPath, "utf8"));

    const account = db.exec(`
      SELECT a.id, pp.display_name, ac.balance
      FROM accounts a
      JOIN player_profiles pp ON pp.account_id = a.id
      JOIN account_currencies ac ON ac.account_id = a.id AND ac.currency = 'OPERATOR_EXP'
      WHERE a.id = 'sample-account-1'
    `);
    const core = db.exec(`
      SELECT COUNT(*), MIN(trained_level), MAX(trained_level)
      FROM operator_training_core_slots
      WHERE account_id = 'sample-account-1'
    `);
    const run = db.exec(`
      SELECT r.id, r.account_id, r.balance_config_version, rs.snapshot_json
      FROM runs r
      JOIN run_snapshots rs ON rs.run_id = r.id
      WHERE r.id = 'sample-run-1'
    `);

    expect(account[0]?.values).toEqual([["sample-account-1", "Kyle Account", 1840]]);
    expect(core[0]?.values).toEqual([[5, 30, 33]]);
    expect(JSON.parse(String(run[0]?.values[0]?.[3]))).toMatchObject({
      operatorIds: ["kyle", "reina", "eve", "sera", "mira"],
      ultimateOperatorId: "kyle",
    });
    const review = db.exec(`
      SELECT status, reason
      FROM suspicious_player_reviews
      WHERE account_id = 'sample-account-1'
    `);
    expect(review[0]?.values).toEqual([["REVIEWING", "Sample account-level mark-only review."]]);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });
});
