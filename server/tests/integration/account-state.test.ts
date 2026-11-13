import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { Hono } from "hono";
import { SqlJsD1Database } from "../helpers/growth-d1";
import { registerAccountStateRoutes } from "../../apps/game-api/src/account-state";
import gameApp from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { sha256Hex } from "@lastrail/common";
import snapshot from "../../packages/balance/src/launch-economy-v1.json";

async function setup() {
  const SQL = await initSqlJs();
  const sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000);
  sql.run("PRAGMA foreign_keys=ON");
  for (const file of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) {
    sql.run(readFileSync(join("packages/db/migrations", file), "utf8"));
  }
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
  sql.run("INSERT INTO account_currencies VALUES('a','CREDITS',123,1000),('b','CREDITS',999,0)");
  sql.run("INSERT INTO fuel_state VALUES('a',7,10,1000,2000)");
  sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op-a',1000),('b','op-b',0)");
  sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level,invested_operator_exp) VALUES('a',2,'op-a',3,120)");
  sql.run("INSERT INTO account_train(account_id,train_id,updated_at,level) VALUES('a','train-a',2000,2)");
  sql.run("INSERT INTO commander_presets VALUES('p','a','saved','{}',1,2000)");
  sql.run("INSERT INTO account_items VALUES('a','ticket',2,0),('b','ticket',99,0)");
  sql.run("INSERT INTO account_carriages VALUES('a','car-a',2,1),('b','car-b',9,1)");
  sql.run("INSERT INTO account_turrets VALUES('a','turret-a',3,1),('b','turret-b',9,1)");
  const db = new SqlJsD1Database(sql) as unknown as D1Database;
  const app = new Hono<{ Bindings: Env }>();
  registerAccountStateRoutes(app, async c => c.req.header("Authorization") === "Bearer test"
    ? { accountId: "a" }
    : { errorCode: "unauthorized", message: "Unauthorized", status: 401 });
  const get = (path = "/v1/account/state", authorized = true) => app.request(path,
    { headers: authorized ? { Authorization: "Bearer test" } : {} }, { DB: db } as Env);
  return { sql, db, get };
}

describe("authenticated account state", () => {
  it("uses the real session verifier and refreshes after an idempotent growth HTTP mutation", async () => {
    const { sql, db } = await setup();
    const secret = "account-state-test-secret-at-least-32-chars";
    const now = Date.now();
    sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s','a','h','f',0,'ACTIVE',?,?)", [now, now + 60000]);
    const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs: 60000 });
    const env = { DB: db, ENVIRONMENT: "DEV", AUTH_SIGNING_KEY_CURRENT: secret } as Env;
    const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json", "Idempotency-Key": "growth-refresh" };
    const get = () => gameApp.request("/v1/account/state", { headers }, env);
    expect((await get()).status).toBe(200);
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
    sql.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at,reason) VALUES('test',1,'PUBLISH','test',0,'test')");
    sql.run("INSERT INTO balance_config_active VALUES(1,1,'test',0)");
    const json = JSON.stringify(snapshot);
    sql.run("INSERT INTO economy_versions VALUES(?,?,?,0)", [snapshot.version, await sha256Hex(json), json]);
    sql.run("INSERT INTO economy_version_bindings VALUES(1,?)", [snapshot.version]);
    sql.run("INSERT INTO account_currencies VALUES('a','OPERATOR_EXP',1000,0)");
    const grow = () => gameApp.request("/v1/operators/training-core/2/level-up", {
      method: "POST", headers, body: JSON.stringify({ economyVersion: snapshot.version, targetLevel: 4 }),
    }, env);
    expect((await grow()).status).toBe(200);
    const after = await (await get()).json();
    expect(after.currencies.OPERATOR_EXP).toBe(940);
    expect(after.trainingCoreSlots[0]).toMatchObject({ trainedLevel: 4, investedOperatorExp: 180 });
    expect(after.mutationSequence).toBe(1);
    expect((await grow()).status).toBe(200);
    expect((await (await get()).json()).mutationSequence).toBe(1);
    sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");
    expect((await get()).status).toBe(401);
  });
  it("returns only the authenticated account, with owned growth state and explicit missing state", async () => {
    const { get } = await setup();
    const response = await get();
    expect(response.status).toBe(200);
    expect(response.headers.get("Cache-Control")).toBe("no-store");
    const state = await response.json();
    expect(state).toMatchObject({ accepted: true, accountId: "a", mutationSequence: 0,
      currencies: { CREDITS: 123, OPERATOR_EXP: 0 },
      fuel: { current: 7, max: 10, lastRegenAt: "1970-01-01T00:00:01.000Z" },
      train: { trainId: "train-a", level: 2 }, formation: { activePresetId: null, revision: 0, operators: [], activationSupported: true } });
    expect(state.operators).toEqual([{ operatorId: "op-a", skillTraining: 1, promotion: 0, revision: 1, acquiredAt: "1970-01-01T00:00:01.000Z" }]);
    expect(state.trainingCoreSlots).toEqual([{ slot: 2, operatorId: "op-a", trainedLevel: 3, investedOperatorExp: 120, revision: 1 }]);
    expect(state.formation.presets).toHaveLength(1);
    expect(state.items).toEqual([{ itemId: "ticket", quantity: 2 }]);
    expect(state.carriages).toEqual([{ carriageId: "car-a", level: 2, revision: 1 }]);
    expect(state.turrets).toEqual([{ turretId: "turret-a", level: 3, revision: 1 }]);
    expect(JSON.stringify(state)).not.toContain("op-b");
    expect(Number.isFinite(Date.parse(state.serverTime))).toBe(true);
  });
  it("rejects missing authentication and account selection parameters", async () => {
    const { get } = await setup();
    expect((await get("/v1/account/state", false)).status).toBe(401);
    expect((await get("/v1/account/state?accountId=b")).status).toBe(400);
  });
  it("does not fabricate an account if it disappears after authentication", async () => {
    const { sql, get } = await setup();
    sql.run("DELETE FROM operator_training_core_slots; DELETE FROM accounts WHERE id='a'");
    const response = await get();
    expect(response.status).toBe(503);
    expect(await response.json()).toEqual({ accepted: false, errorCode: "account_state_unavailable" });
  });
  it("refreshes committed state without creating mutations or initializing missing holdings", async () => {
    const { get, sql } = await setup();
    await get();
    sql.run("INSERT INTO account_mutations VALUES('m','a',1,'test',NULL,3000)");
    sql.run("UPDATE account_currencies SET balance=83 WHERE account_id='a'");
    sql.run("UPDATE operator_training_core_slots SET trained_level=4,invested_operator_exp=160,revision=2 WHERE account_id='a'");
    const state = await (await get()).json();
    expect(state.mutationSequence).toBe(1);
    expect(state.currencies.CREDITS).toBe(83);
    expect(state.trainingCoreSlots[0].trainedLevel).toBe(4);
    sql.run("DELETE FROM fuel_state; DELETE FROM account_train;");
    const empty = await (await get()).json();
    expect(empty.fuel).toBeNull();
    expect(empty.train).toBeNull();
    expect(sql.exec("SELECT COUNT(*) FROM account_mutations")[0].values[0][0]).toBe(1);
  });
});
