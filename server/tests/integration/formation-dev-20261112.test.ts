import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { Hono } from "hono";
import { SqlJsD1Database } from "../helpers/growth-d1";
import { registerFormationRoutes } from "../helpers/dev-formation-bundle-20261112.mjs";
import { readAccountState } from "../helpers/dev-formation-bundle-20261112.mjs";
import { registerFormationRoutes as originalRoutes } from "../helpers/dev-formation-original-20261112.mjs";

const operators = Array.from({ length: 5 }, (_, slot) => ({ slot, operatorId: `op${slot}` }));
async function setup(register=registerFormationRoutes) {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000);
  sql.run("PRAGMA foreign_keys=ON");
  for (const file of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f) && Number(f.slice(0,4))<=46).sort()) sql.run(readFileSync(join("packages/db/migrations", file), "utf8"));
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
  for (let i = 0; i < 6; i++) sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [`op${i}`]);
  const adapter = new SqlJsD1Database(sql), db = adapter as unknown as D1Database;
  const app = new Hono<{ Bindings: Env }>();
  register(app, async c => c.req.header("Authorization") === "a" ? { accountId: "a" } : { accountId: "b" });
  const post = (path: string, body: object, key = crypto.randomUUID(), account = "a") => app.request(path, {
    method: "POST", headers: { Authorization: account, "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify(body),
  }, { DB: db } as Env);
  const save = (body: object = { name: "Squad", expectedRevision: 0, operators }, key?: string) => post("/v1/formation/presets/p/save", body, key);
  const activate = (body: object = { expectedRevision: 1, expectedFormationRevision: 0 }, key?: string) => post("/v1/formation/presets/p/activate", body, key);
  const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
  return { sql, db, adapter, post, save, activate, scalar };
}

describe("owned squad presets", () => {
  it("reproduces deployed v2 rejection and replays a receipt committed by the old worker after patching",async()=>{
    const f=await setup(originalRoutes),body={name:'Legacy',expectedRevision:0,operators};
    expect((await f.save({...body,responseVersion:'formation.command.v2.20261021'},'rejected')).status).toBe(400);
    const legacy=await(await f.save(body,'legacy')).json();expect(legacy.refreshRequired).toBe(true);
    const app=new Hono<{Bindings:Env}>();registerFormationRoutes(app,async()=>({accountId:'a'}));
    const send=(payload:object,key:string)=>app.request('/v1/formation/presets/p/save',{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify(payload)},{DB:f.db} as Env);
    expect(await(await send(body,'legacy')).json()).toEqual(legacy);
    expect((await send({...body,responseVersion:'formation.command.v2.20261021'},'legacy')).status).toBe(409);
    expect((await send({...body,expectedRevision:1,responseVersion:'formation.command.v2.20261021'},'new-v2')).status).toBe(200);
    expect(f.scalar('SELECT COUNT(*) FROM account_mutations')).toBe(2);f.sql.close();
  });
  it("preserves numbered combat positions through save and activation independent of array order", async () => {
    const f = await setup();
    try {
      const swapped = operators.map(o => ({ ...o, operatorId: o.slot === 0 ? 'op4' : o.slot === 4 ? 'op0' : o.operatorId }));
      const responseVersion = 'formation.command.v2.20261021';
      const saved = await f.save({ name: 'Positions', expectedRevision: 0, operators: [...swapped].reverse(), responseVersion });
      expect(saved.status).toBe(200);
      expect(f.scalar('SELECT COUNT(*) FROM account_formation')).toBe(0);
      const activated = await f.activate({ expectedRevision: 1, expectedFormationRevision: 0, responseVersion });
      expect(activated.status).toBe(200);
      const result = await activated.json();
      expect(result.operators).toEqual(swapped);
      expect(result.owners.account.formation.operators).toEqual(swapped);
      expect(JSON.parse(String(f.scalar('SELECT formation_json FROM account_formation'))).operators).toEqual(swapped);
      expect(f.scalar('SELECT COUNT(*) FROM operator_training_core_slots')).toBe(0);
      expect(f.scalar("SELECT COUNT(*) FROM sqlite_master WHERE name='combat_b_carriage_instances'")).toBe(0);
    } finally { f.sql.close(); }
  });
  it("returns a versioned committed preset patch without re-reading account or changing active formation", async () => {
    const f=await setup(),body={name:'Complete',expectedRevision:0,operators,responseVersion:'formation.command.v2.20261021'};
    const response=await f.save(body,'v2');expect(response.status).toBe(200);const saved=await response.json();
    expect(saved).toMatchObject({contractVersion:body.responseVersion,refreshRequired:false,owners:{account:{kind:'PATCH',beforeMutationSequence:0,mutationSequence:1,coverage:['FORMATION_PRESETS'],preset:{id:'p',name:'Complete',revision:1,operators}}},invalidates:[]});
    expect(f.scalar('SELECT COUNT(*) FROM account_formation')).toBe(0);
    expect(await(await f.save(body,'v2')).json()).toEqual(saved);
    expect((await f.save({name:'Complete',expectedRevision:0,operators},'v2')).status).toBe(409);
    const active=await(await f.activate({expectedRevision:1,expectedFormationRevision:0,responseVersion:body.responseVersion},'active-v2')).json();
    expect(active).toMatchObject({refreshRequired:false,owners:{account:{kind:'PATCH',beforeMutationSequence:1,mutationSequence:2,coverage:['ACTIVE_FORMATION'],formation:{activePresetId:'p',activePresetRevision:1,revision:1,operators}}},invalidates:['ULTIMATE_SELECTION','DEPARTURE_PREVIEW']});
  });
  it("saves explicitly, activates a frozen selection, and preserves it when the preset changes", async () => {
    const { save, activate, scalar, db } = await setup();
    expect((await save()).status).toBe(200);
    expect(scalar("SELECT COUNT(*) FROM account_formation")).toBe(0);
    expect((await activate()).status).toBe(200);
    expect((await readAccountState(db, "a")).formation).toMatchObject({ activePresetId: "p", activePresetRevision: 1, revision: 1, operators, activationSupported: true });
    const before = scalar("SELECT formation_json FROM account_formation");
    expect((await save({ name: "New", expectedRevision: 1, operators: operators.map(o => o.slot === 0 ? { ...o, operatorId: "op5" } : o) })).status).toBe(200);
    expect(scalar("SELECT formation_json FROM account_formation")).toBe(before);
    expect(scalar("SELECT COUNT(*) FROM currency_ledger")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM operator_training_core_slots")).toBe(0);
  });
  it("rejects duplicate slots/operators, incomplete squads, unowned operators and injected stats", async () => {
    const { save, scalar } = await setup();
    for (const invalid of [operators.slice(1), operators.map(o => ({ ...o, slot: 0 })), operators.map(o => ({ ...o, operatorId: "op0" })), operators.map(o => ({ ...o, hp: 999 }))]) {
      expect((await save({ name: "X", expectedRevision: 0, operators: invalid })).status).toBe(400);
    }
    expect((await save({ name: "X", expectedRevision: 0, operators: operators.map(o => ({ ...o, operatorId: `unowned${o.slot}` })) })).status).toBe(403);
    expect(scalar("SELECT COUNT(*) FROM commander_presets")).toBe(0);
  });
  it("replays an immutable receipt before revision checks and rejects changed payloads", async () => {
    const { save, activate, scalar } = await setup();
    const first = await (await save(undefined, "same")).json();
    expect(await (await save(undefined, "same")).json()).toEqual(first);
    expect((await save({ name: "Changed", expectedRevision: 0, operators }, "same")).status).toBe(409);
    expect((await activate(undefined, "act")).status).toBe(200);
    expect((await activate(undefined, "act")).status).toBe(200);
    expect((await save()).status).toBe(409);
    expect((await activate()).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM formation_receipts")).toBe(2);
  });
  it("never activates another account's preset and revalidates lost ownership", async () => {
    const { save, activate, post, sql, scalar } = await setup();
    await save();
    expect((await post("/v1/formation/presets/p/activate", { expectedRevision: 1, expectedFormationRevision: 0 }, "b", "b")).status).toBe(409);
    sql.run("DELETE FROM account_operators WHERE operator_id='op0'");
    expect((await activate()).status).toBe(403);
    expect(scalar("SELECT COUNT(*) FROM account_formation")).toBe(0);
  });
  it("fences ownership changes between validation and commit without partial mutation", async () => {
    const { save, adapter, sql, scalar } = await setup();
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined; sql.run("DELETE FROM account_operators WHERE operator_id='op0'"); };
    expect((await save()).status).toBe(403);
    expect(scalar("SELECT COUNT(*) FROM commander_presets")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("serializes concurrent identical saves and rejects competing stale revisions", async () => {
    const { save, scalar } = await setup();
    const duplicates = await Promise.all([save(undefined, "same"), save(undefined, "same")]);
    expect(duplicates.map(r => r.status)).toEqual([200, 200]);
    const compete = await Promise.all([save({ name: "A", expectedRevision: 1, operators }), save({ name: "B", expectedRevision: 1, operators })]);
    expect(compete.map(r => r.status).sort()).toEqual([200, 409]);
    expect(scalar("SELECT revision FROM commander_presets")).toBe(2);
  });
  it("blocks activation during an open run including a start race, while still allowing preference saves", async () => {
    const { save, activate, adapter, sql, scalar } = await setup();
    await save();
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
    adapter.beforeBatch = () => {
      adapter.beforeBatch = undefined;
      sql.run("INSERT INTO account_mutations VALUES('r','a',2,'run',NULL,0)");
      sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('r','a','r',1,'ACTIVE',0)");
    };
    expect((await activate()).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM account_formation")).toBe(0);
    expect((await save({ name: "During run", expectedRevision: 1, operators })).status).toBe(200);
    for (const status of ["FINISH_PENDING", "QUARANTINED"]) {
      sql.run("UPDATE runs SET status=?", [status]);
      expect((await activate({ expectedRevision: 2, expectedFormationRevision: 0 })).status).toBe(409);
    }
  });
  it("does not reset formation revision for an identical accepted activation", async () => {
    const { save, activate } = await setup();
    await save(); await activate();
    expect(await (await activate({ expectedRevision: 1, expectedFormationRevision: 1 })).json()).toMatchObject({ formationRevision: 1, formationChanged: false });
  });
  it("rolls back preset and mutation writes when persisting the receipt fails", async () => {
    const { save, sql, scalar } = await setup();
    sql.run("CREATE TRIGGER fail_receipt BEFORE INSERT ON formation_receipts BEGIN SELECT RAISE(ABORT,'injected rollback'); END");
    expect((await save()).status).toBe(500);
    expect(scalar("SELECT COUNT(*) FROM commander_presets")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("does not overwrite a foreign preset even when the caller owns a valid squad", async () => {
    const { save, post, sql, scalar } = await setup();
    await save();
    for (const o of operators) sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('b',?,0)", [o.operatorId]);
    expect((await post("/v1/formation/presets/p/save", { name: "Foreign overwrite", expectedRevision: 0, operators }, "b-save", "b")).status).toBe(409);
    expect(scalar("SELECT name FROM commander_presets WHERE id='p'")).toBe("Squad");
    expect(scalar("SELECT COUNT(*) FROM account_mutations WHERE account_id='b'")).toBe(0);
  });
  it("rejects legacy malformed definitions without interpreting them as a valid squad", async () => {
    const { activate, sql, scalar } = await setup();
    sql.run("INSERT INTO commander_presets VALUES('p','a','legacy','not-json',1,0)");
    expect(await (await activate()).json()).toMatchObject({ accepted: false, errorCode: "preset_schema_unsupported" });
    sql.run("UPDATE commander_presets SET preset_json='{}'");
    expect((await activate()).status).toBe(409);
    expect(scalar("SELECT COUNT(*) FROM account_formation")).toBe(0);
  });
});
