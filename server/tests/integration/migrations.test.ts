import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

describe("D1 migrations", () => {
  it("apply 0001 through 0010 and pass SQLite integrity checks", async () => {
    const SQL = await initSqlJs();
    const db = new SQL.Database();
    db.run("PRAGMA foreign_keys = ON;");

    const migrationDir = join(process.cwd(), "packages/db/migrations");
    const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|10)_.+\.sql$/u.test(file)).sort();
    expect(migrations).toHaveLength(10);

    for (const migration of migrations) {
      db.run(readFileSync(join(migrationDir, migration), "utf8"));
    }

    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
    db.close();
  });
});
