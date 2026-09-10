import { afterEach, describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { sha256Hex } from "@lastrail/common";
import snapshot from "../../packages/balance/src/launch-economy-v1.json";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { SqlJsD1Database } from "../helpers/growth-d1";

afterEach(() => vi.restoreAllMocks());
async function setup(bound = true) {
  const now = Date.now(); vi.spyOn(Date, "now").mockReturnValue(now);
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.create_function("unixepoch", () => now / 1000);
  sql.run("PRAGMA foreign_keys=ON");
  for (const file of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) sql.run(readFileSync(join("packages/db/migrations", file), "utf8"));
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',?,?)", [now - 3600000, now]);
  sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s','a','h','f',0,'ACTIVE',?,?)", [now, now + 60000]);
  if (bound) {
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
    sql.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at,reason) VALUES('test',1,'PUBLISH','test',0,'test')");
    sql.run("INSERT INTO balance_config_active VALUES(1,1,'test',0)");
    const json = JSON.stringify(snapshot);
    sql.run("INSERT INTO economy_versions VALUES(?,?,?,0)", [snapshot.version, await sha256Hex(json), json]);
    sql.run("INSERT INTO economy_version_bindings VALUES(1,?)", [snapshot.version]);
  }
  const adapter = new SqlJsD1Database(sql);
  const secret = "offline-binding-test-secret-at-least-32-chars";
  const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs: 60000 });
  const env = { DB: adapter as unknown as D1Database, ENVIRONMENT: "DEV", AUTH_SIGNING_KEY_CURRENT: secret } as Env;
  const headers = { Authorization: `Bearer ${token}` };
  const preview = () => app.request("/v1/offline-rewards/preview", { headers }, env);
  const claim = (key = "claim") => app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": key } }, env);
  const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
  return { sql, adapter, preview, claim, scalar };
}

describe("offline rewards bound economy adapter", () => {
  it("uses approved hourly24/2/1/20 for preview and claim and preserves the binding in receipts", async () => {
    const { preview, claim, scalar, sql } = await setup();
    const previewBody = await (await preview()).json();
    expect(previewBody.rewardLines.map((r: { quantity: number }) => r.quantity)).toEqual([24, 2, 1, 20]);
    expect(previewBody.economyVersion).toBe(snapshot.version);
    expect((await claim()).status).toBe(200);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(24);
    expect(scalar("SELECT balance_config_version FROM reward_transactions")).toBe(1);
    expect(scalar("SELECT last_balance_config_version FROM account_offline_reward_state")).toBe(1);
    expect(scalar("SELECT last_reward_transaction_id FROM account_offline_reward_state")).toBe(scalar("SELECT id FROM reward_transactions"));
    sql.run("DELETE FROM balance_config_active");
    const replay = await (await claim()).json();
    expect(replay.claimStatus).toBe("DUPLICATE_REPLAY");
    expect(replay.economyVersion).toBe(snapshot.version);
    expect(scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(1);
  });
  it("retains legacy unbound behavior instead of activating a policy implicitly", async () => {
    const { preview } = await setup(false);
    const body = await (await preview()).json();
    expect(body.rewardLines.map((r: { quantity: number }) => r.quantity)).toEqual([100, 50, 30, 20]);
    expect(body.economyVersion).toBeNull();
  });
  it("rejects policy changes between read and commit with zero partial grants", async () => {
    const { claim, adapter, sql, scalar } = await setup();
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined; sql.run("DELETE FROM balance_config_active"); };
    expect((await claim()).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM account_currencies")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM account_offline_reward_state")).toBe(0);
  });
  it("serializes competing claim keys without collecting the same elapsed time twice", async () => {
    const { claim, scalar } = await setup();
    const responses = await Promise.all([claim("first"), claim("second")]);
    expect(responses.map(r => r.status).sort()).toEqual([200, 409]);
    expect(scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(1);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(24);
  });
  it("preserves the existing story progress multiplier and twelve-hour cap", async () => {
    const { sql, preview } = await setup();
    sql.run("UPDATE accounts SET created_at=?", [Date.now() - 86400000]);
    sql.run("INSERT INTO account_story_progress(account_id,act_id,highest_cleared_operation_id,highest_story_progress_id,cleared_operations_json,story_flags_json,narrative_version,updated_at) VALUES('a','act_01','raid_raider_king_engine','raid_raider_king_engine','[]','[]','test',0)");
    const body = await (await preview()).json();
    expect(body.rewardLines.map((r: { quantity: number }) => r.quantity)).toEqual([864, 72, 36, 720]);
    expect(body.offlineStorageCapHours).toBe(12);
  });
});
