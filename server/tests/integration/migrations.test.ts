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
});
