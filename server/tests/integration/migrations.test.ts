import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|10)_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(10);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

describe("D1 migrations", () => {
  it("apply 0001 through 0010 and pass SQLite integrity checks", async () => {
    const db = await createMigratedDb();

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
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
    const sampleSeedPath = join(process.cwd(), "packages/db/seeds/dev/0003_admin_console_sample_data.sql");

    expect(existsSync(sampleSeedPath)).toBe(true);
    db.run(readFileSync(balanceSeedPath, "utf8"));
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
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    db.close();
  });
});
