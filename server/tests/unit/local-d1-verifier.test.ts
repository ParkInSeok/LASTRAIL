import { afterAll, describe, expect, it } from "vitest";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { join, resolve, sep } from "node:path";
import { discoverMigrations, verifyLocalD1 } from "../../scripts/validation/verify-local-d1";

const root = resolve("D:/Project/LASTRAIL/LocalTemp/local-d1-verifier-tests");
mkdirSync(root, { recursive: true });
const dirs: string[] = [];
function fixture(files: Record<string, string>) {
  const dir = mkdtempSync(join(root, "case-")); dirs.push(dir);
  for (const [name, sql] of Object.entries(files)) writeFileSync(join(dir, name), sql);
  return dir;
}
afterAll(() => { for (const dir of dirs) { if (!resolve(dir).startsWith(root + sep)) throw Error("Unsafe test cleanup"); rmSync(dir, { recursive: true }); } });

describe("local D1 migration verifier", () => {
  it("discovers every sequential migration beyond the old ceiling", () => {
    const names = Array.from({length: 50}, (_, i) => String(i + 1).padStart(4, "0") + "_test.sql");
    expect(discoverMigrations([...names].reverse().concat("README.md"))).toEqual(names);
  });
  it.each([
    [[], "No migrations"],
    [["0002_second.sql"], "Expected migration 0001"],
    [["0001_first.sql", "0003_third.sql"], "Expected migration 0002"],
    [["0001_a.sql", "0001_b.sql"], "Duplicate migration 0001"],
    [["0049.sql"], "Invalid migration filename"],
    [["0001_test.SQL"], "Invalid migration filename"],
  ])("rejects an incomplete or ambiguous sequence %j", (names, message) => {
    expect(() => discoverMigrations(names as string[])).toThrow(message as string);
  });
  it("applies real SQL and validates database integrity", async () => {
    const dir = fixture({"0001_parent.sql": "CREATE TABLE parent(id INTEGER PRIMARY KEY);", "0002_child.sql": "CREATE TABLE child(id INTEGER PRIMARY KEY,parent_id INTEGER REFERENCES parent(id)); INSERT INTO parent VALUES(1); INSERT INTO child VALUES(1,1);"});
    const log: string[] = [];
    expect(await verifyLocalD1(dir, line => log.push(line))).toEqual({migrationCount:2,lastMigration:"0002_child.sql"});
    expect(log).toContain("foreign_key_check clean"); expect(log).toContain("quick_check ok");
  });
  it("reports the exact migration that cannot apply", async () => {
    await expect(verifyLocalD1(fixture({"0001_bad.sql":"INVALID SQL;"}), () => {})).rejects.toThrow("0001_bad.sql");
  });
  it("rejects orphan rows even if a migration disabled FK enforcement", async () => {
    const dir = fixture({"0001_bad.sql":"PRAGMA foreign_keys=OFF; CREATE TABLE parent(id INTEGER PRIMARY KEY); CREATE TABLE child(parent_id INTEGER REFERENCES parent(id)); INSERT INTO child VALUES(999);"});
    await expect(verifyLocalD1(dir, () => {})).rejects.toThrow("foreign_key_check failed");
  });
});
