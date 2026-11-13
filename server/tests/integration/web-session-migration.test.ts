import { readFileSync } from "node:fs";
import { join } from "node:path";
import initSqlJs from "sql.js";
import { describe, expect, it } from "vitest";

describe("web session storage", () => {
  it("supports multiple browser sessions without occupying or revoking a game session", async () => {
    const SQL = await initSqlJs();
    const db = new SQL.Database();
    try {
      for (const file of ["0001_initial.sql", "0024_tester_entitlements.sql", "0070_homepage_web_sessions.sql"]) {
        db.run(readFileSync(join("packages/db/migrations", file), "utf8"));
      }
      db.run("INSERT INTO accounts(id, status, created_at, updated_at) VALUES ('account_1','ACTIVE',1,1)");
      db.run(`INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at)
        VALUES ('game_1','account_1','game_hash','family_1',0,'ACTIVE',1,9999999)`);
      db.run(`INSERT INTO web_sessions(token_hash,csrf_hash,account_id,created_at,expires_at)
        VALUES ('web_a','csrf_a','account_1',1,9999999),('web_b','csrf_b','account_1',1,9999999)`);
      db.run("UPDATE web_sessions SET revoked_at=2 WHERE token_hash='web_a'");
      expect(db.exec("SELECT token_hash FROM web_sessions WHERE revoked_at IS NULL")[0]?.values).toEqual([["web_b"]]);
      expect(db.exec("SELECT id,status FROM auth_sessions")[0]?.values).toEqual([["game_1", "ACTIVE"]]);
    } finally { db.close(); }
  });
});
