import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const defaultMigrationDir = fileURLToPath(new URL("../../packages/db/migrations/", import.meta.url));

/** Validate the complete local sequence; a fixed release count must not hide later migrations. */
export function discoverMigrations(entries: readonly string[]): string[] {
  const migrations = entries.filter(name => /\.sql$/iu.test(name)).sort();
  if (migrations.length === 0) throw new Error("No migrations found");
  const numbers = new Set<number>();
  for (const file of migrations) {
    const match = /^(\d{4})_[A-Za-z0-9_-]+\.sql$/u.exec(file);
    if (!match) throw new Error(`Invalid migration filename: ${file}`);
    const number = Number(match[1]);
    if (numbers.has(number)) throw new Error(`Duplicate migration ${match[1]}: ${file}`);
    numbers.add(number);
  }
  migrations.forEach((file, index) => {
    const expected = String(index + 1).padStart(4, "0");
    if (!file.startsWith(expected + "_")) throw new Error(`Expected migration ${expected}, found ${file}`);
  });
  return migrations;
}

/** Owns one disposable in-memory DB. Does not modify a local or remote D1 database. */
export async function verifyLocalD1(migrationDir = defaultMigrationDir, log: (line: string) => void = console.log) {
  const entries = readdirSync(migrationDir, { withFileTypes: true });
  for (const entry of entries) {
    if (/\.sql$/iu.test(entry.name) && !entry.isFile()) throw new Error(`Migration must be a regular file: ${entry.name}`);
  }
  const migrations = discoverMigrations(entries.map(entry => entry.name));
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  try {
    db.run("PRAGMA foreign_keys = ON;");
    for (const file of migrations) {
      try {
        db.run(readFileSync(join(migrationDir, file), "utf8"));
      } catch (cause) {
        throw new Error(`Migration failed: ${file}`, { cause });
      }
      log(`applied ${file}`);
    }
    const fk = db.exec("PRAGMA foreign_key_check;");
    if (fk.length > 0) throw new Error(`foreign_key_check failed: ${JSON.stringify(fk)}`);
    const quick = db.exec("PRAGMA quick_check;");
    if (quick.length !== 1 || quick[0]?.values.length !== 1 || quick[0]?.values[0]?.[0] !== "ok") {
      throw new Error(`quick_check failed: ${JSON.stringify(quick)}`);
    }
    log("foreign_key_check clean");
    log("quick_check ok");
    log(`verified ${migrations.length} migrations; latest ${migrations.at(-1)!}`);
    return { migrationCount: migrations.length, lastMigration: migrations.at(-1)! };
  } finally {
    db.close();
  }
}

// Importing the validator has no database or logging side effects.
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    await verifyLocalD1();
  } catch (error) {
    console.error(error);
    process.exitCode = 1;
  }
}
