import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

const migrationDir = join(process.cwd(), "packages/db/migrations");
const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();

if (migrations.length !== 24) {
  throw new Error(`Expected 24 migrations, found ${migrations.length}`);
}

const SQL = await initSqlJs();
const db = new SQL.Database();
db.run("PRAGMA foreign_keys = ON;");

for (const migration of migrations) {
  db.run(readFileSync(join(migrationDir, migration), "utf8"));
  console.log(`applied ${migration}`);
}

const fk = db.exec("PRAGMA foreign_key_check;");
if (fk.length > 0) {
  throw new Error(`foreign_key_check failed: ${JSON.stringify(fk)}`);
}

const quick = db.exec("PRAGMA quick_check;")[0]?.values?.[0]?.[0];
if (quick !== "ok") {
  throw new Error(`quick_check failed: ${String(quick)}`);
}

console.log("foreign_key_check clean");
console.log("quick_check ok");
db.close();
