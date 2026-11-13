import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { SqlJsD1Database } from "../helpers/growth-d1";

const base = { rulesVersion: "train-loadout-v1", trainId: "train_a", expectedRevision: 0,
  carriages: [{ slot: 0, carriageId: "car_combat" }, { slot: 1, carriageId: "car_gun" }] };
const water = "opn_03_water_stop", night = "opn_05_night_boarders";
it('returns the final v2 loadout and owner revision without broad refresh',async()=>{
 const f=await setup(),body={...base,responseVersion:'train-loadout.command.v2.20261021'};
 const response=await app.request('/v1/trains/loadout',{method:'POST',headers:{...f.headers,'Idempotency-Key':'v2'},body:JSON.stringify(body)},f.env);
 expect(response.status).toBe(200);const receipt=await response.json();
 expect(receipt).toMatchObject({contractVersion:body.responseVersion,refreshRequired:false,owners:{trainLoadout:{kind:'SNAPSHOT',revision:1,loadout:{trainId:base.trainId,carriages:base.carriages}}},invalidates:['DEPARTURE_PREVIEW']});
});
async function setup() {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000); sql.run("PRAGMA foreign_keys=ON");
  for (const f of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) sql.run(readFileSync(join("packages/db/migrations", f), "utf8"));
  const now = Date.now(), secret = "train-loadout-test-secret-at-least-32-chars";
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
  sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s','a','h','f',0,'ACTIVE',?,?)", [now, now + 60000]);
  sql.run("INSERT INTO account_train(account_id,train_id,revision,updated_at) VALUES('a','train_a',1,0)");
  for (const id of ["car_combat", "car_gun", "car_cargo", "car_defense", "car_medical", "unknown"]) sql.run("INSERT INTO account_carriages(account_id,carriage_id) VALUES('a',?)", [id]);
  sql.run("INSERT INTO account_turrets(account_id,turret_id) VALUES('a','turret_owned')");
  const adapter = new SqlJsD1Database(sql);
  const env = { DB: adapter as unknown as D1Database, ENVIRONMENT: "DEV", AUTH_SIGNING_KEY_CURRENT: secret } as Env;
  const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs: 60000 });
  const headers = { Authorization: `Bearer ${token}` };
  const get = () => app.request("/v1/trains/loadout", { headers }, env);
  const post = (body: object = base, key = crypto.randomUUID()) => app.request("/v1/trains/loadout", { method: "POST", headers: { ...headers, "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify(body) }, env);
  const progress = (ids: string[]) => sql.run("INSERT INTO account_story_progress(account_id,act_id,highest_cleared_operation_id,highest_story_progress_id,cleared_operations_json,story_flags_json,updated_at) VALUES('a','act_01','unused','unused',?,'[]',0) ON CONFLICT(account_id) DO UPDATE SET cleared_operations_json=excluded.cleared_operations_json", [JSON.stringify(ids)]);
  const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
  const open = (status = "ACTIVE") => {
    sql.run("INSERT OR IGNORE INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
    sql.run("INSERT INTO account_mutations VALUES('run','a',99,'run',NULL,0)");
    sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('run','a','run',1,?,0)", [status]);
  };
  return { sql, adapter, env, headers, get, post, progress, scalar, open };
}
describe("owned train installation", () => {
  it("rejects GET bodies and query account overrides", async () => {
    const { env, headers } = await setup();
    const request = new Request("http://localhost/v1/trains/loadout", { method: "POST", headers, body: "{}" });
    Object.defineProperty(request, "method", { value: "GET" });
    expect((await app.fetch(request, env)).status).toBe(400);
    expect((await app.request("/v1/trains/loadout?accountId=b", { headers }, env)).status).toBe(400);
  });
  it("reads actual inventory, locked/unsupported capabilities and empty selection without creating ownership", async () => {
    const { get, scalar, env } = await setup();
    const response = await get(); expect(response.status).toBe(200);
    expect(response.headers.get("Cache-Control")).toContain("no-store");
    const body = await response.json();
    expect(body).toMatchObject({ rulesVersion: base.rulesVersion, unlockedSlotCount: 2, revision: 0, loadout: null, openRun: false });
    expect(body.ownedCarriages).toEqual(expect.arrayContaining([
      expect.objectContaining({ carriageId: "car_combat", installationSupported: true, installable: true, reason: null }),
      expect.objectContaining({ carriageId: "car_cargo", installationSupported: true, installable: false, reason: "story_locked" }),
      expect.objectContaining({ carriageId: "car_medical", installationSupported: false, reason: "medical_unlock_unapproved" }),
      expect.objectContaining({ carriageId: "unknown", installationSupported: false, reason: "carriage_definition_unsupported" }),
    ]));
    expect(body.ownedTurrets).toEqual([expect.objectContaining({ turretId: "turret_owned", installationSupported: false, reason: "turret_installation_undefined" })]);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
    expect((await app.request("/v1/trains/loadout", {}, env)).status).toBe(401);
  });
  it("installs once, replays after changed eligibility, and rejects changed content and stale revisions", async () => {
    const { post, get, scalar, open } = await setup();
    const r = await post(base, "same"); expect(r.status).toBe(200);
    const body = await r.json(); expect(body).toMatchObject({ accepted: true, revision: 1, changed: true });
    expect(await (await get()).json()).toMatchObject({ revision: 1, loadout: { trainId: "train_a", carriages: base.carriages } });
    expect((await post()).status).toBe(409);
    expect((await post({ ...base, expectedRevision: 1 }, "same")).status).toBe(409);
    const noop = await (await post({ ...base, expectedRevision: 1 })).json(); expect(noop).toMatchObject({ revision: 1, changed: false });
    open(); expect(await (await post(base, "same")).json()).toEqual(body);
    expect(scalar("SELECT COUNT(*) FROM train_loadout_receipts")).toBe(2);
    expect(scalar("SELECT COUNT(*) FROM account_carriages")).toBe(6);
  });
  it("rejects duplicate IDs/slots, holes, incomplete slots, unknown keys and unsupported definitions", async () => {
    const { post, scalar } = await setup();
    for (const carriages of [[base.carriages[0]], [base.carriages[0], base.carriages[0]], [{ slot: 0, carriageId: "car_combat" }, { slot: 1, carriageId: "car_combat" }], [{ slot: 0, carriageId: "car_combat" }, { slot: 2, carriageId: "car_gun" }]]) expect((await post({ ...base, carriages })).status).toBe(400);
    expect((await post({ ...base, hp: 999 })).status).toBe(400);
    for (const carriageId of ["car_medical", "unknown"]) expect((await post({ ...base, carriages: [base.carriages[0], { slot: 1, carriageId }] })).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("requires owned locomotive and carriage rows, never guesses acquisition", async () => {
    const { post, sql, scalar } = await setup();
    expect((await post({ ...base, trainId: "foreign" })).status).toBe(403);
    sql.run("DELETE FROM account_carriages WHERE carriage_id='car_gun'");
    expect((await post()).status).toBe(403);
    expect(scalar("SELECT COUNT(*) FROM account_train_loadout")).toBe(0);
  });
  it("uses server milestone gates for 2/3/4 slots and refuses inconsistent night-only history", async () => {
    const { post, get, progress } = await setup();
    const three = { ...base, carriages: [...base.carriages, { slot: 2, carriageId: "car_cargo" }] };
    expect((await post({ ...base, carriages: [base.carriages[0], { slot: 1, carriageId: "car_cargo" }] })).status).toBe(409);
    progress([water]); expect((await post(three)).status).toBe(200);
    progress([water, night]);
    expect((await post({ ...three, expectedRevision: 1, carriages: [...three.carriages, { slot: 3, carriageId: "car_defense" }] })).status).toBe(200);
    progress([night]);
    expect(await (await get()).json()).toMatchObject({ unlockedSlotCount: null, progressError: "story_progress_inconsistent" });
    expect((await post({ ...base, expectedRevision: 2 })).status).toBe(409);
  });
  it("requires combat and all unlocked slots, with no invented duplicate instances", async () => {
    const { post, progress } = await setup(); progress([water]);
    expect((await post()).status).toBe(400);
    expect((await post({ ...base, carriages: [{ slot: 0, carriageId: "car_gun" }, { slot: 1, carriageId: "car_cargo" }, { slot: 2, carriageId: "car_defense" }] })).status).toBe(409);
  });
  it("blocks all open run statuses and a run starting at commit", async () => {
    for (const status of ["ACTIVE", "FINISH_PENDING", "QUARANTINED"]) {
      const { post, open, scalar } = await setup(); open(status);
      expect((await post()).status).toBe(409); expect(scalar("SELECT COUNT(*) FROM account_train_loadout")).toBe(0);
    }
    const { post, adapter, open, scalar } = await setup();
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined; open(); };
    expect((await post()).status).toBe(409); expect(scalar("SELECT COUNT(*) FROM train_loadout_receipts")).toBe(0);
  });
  it("rechecks ownership/progress at commit and rolls back all local writes", async () => {
    for (const change of ["DELETE FROM account_carriages WHERE carriage_id='car_gun'", "DELETE FROM account_train"]) {
      const { post, adapter, sql, scalar } = await setup();
      adapter.beforeBatch = () => { adapter.beforeBatch = undefined; sql.run(change); };
      expect((await post()).status).toBe(403); expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
    }
    const { post, adapter, progress, scalar } = await setup();
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined; progress([water]); };
    expect((await post()).status).toBe(400); expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("serializes duplicate and competing installs and atomically rolls back receipt failure", async () => {
    const { post, scalar, sql } = await setup();
    expect((await Promise.all([post(base, "same"), post(base, "same")])).map(r => r.status)).toEqual([200, 200]);
    const swapped = [ { slot: 0, carriageId: "car_gun" }, { slot: 1, carriageId: "car_combat" } ];
    expect((await Promise.all([post({ ...base, expectedRevision: 1, carriages: swapped }), post({ ...base, expectedRevision: 1, carriages: swapped })])).map(r => r.status).sort()).toEqual([200, 409]);
    sql.run("CREATE TRIGGER fail_loadout BEFORE INSERT ON train_loadout_receipts BEGIN SELECT RAISE(ABORT,'injected rollback'); END");
    expect((await post({ ...base, expectedRevision: 2 })).status).toBe(500);
    expect(scalar("SELECT revision FROM account_train_loadout")).toBe(2);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(2);
  });
});
