import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";

type SqlValue = string | number | Uint8Array | null;

class SqlJsD1Statement {
  readonly sql: string;
  private readonly database: initSqlJs.Database;
  private values: SqlValue[] = [];

  constructor(database: initSqlJs.Database, sql: string) {
    this.database = database;
    this.sql = sql;
  }

  bind(...values: SqlValue[]): SqlJsD1Statement {
    this.values = values;
    return this;
  }

  async first<T>(): Promise<T | null> {
    const statement = this.database.prepare(this.sql);
    try {
      statement.bind(this.values);
      if (!statement.step()) return null;
      return statement.getAsObject() as T;
    } finally {
      statement.free();
    }
  }

  async all<T>(): Promise<{ results: T[] }> {
    const statement = this.database.prepare(this.sql);
    const results: T[] = [];
    try {
      statement.bind(this.values);
      while (statement.step()) {
        results.push(statement.getAsObject() as T);
      }
      return { results };
    } finally {
      statement.free();
    }
  }

  run(): void {
    this.database.run(this.sql, this.values);
  }
}

class SqlJsD1Database {
  private readonly database: initSqlJs.Database;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.database.run("BEGIN;");
    try {
      for (const statement of statements) {
        statement.run();
      }
      this.database.run("COMMIT;");
      return statements.map(() => ({ success: true }));
    } catch (error) {
      this.database.run("ROLLBACK;");
      throw error;
    }
  }
}

const authSecret = "test-secret-minimum-32-characters";

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();


  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env & { AUTH_SIGNING_KEY_CURRENT: string } {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

import { prepareLaunchRewards, launchEntitlementStatements } from "../../apps/game-api/src/launch-entitlements";

it('rejects hidden login payload changes even when Content-Length is absent', async () => {
 const {env,authorization}=await guestDb();
 const response=await app.request('/v1/login-bonus/claim',{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':'hidden-body'},body:'{"quantity":999}'},env);
 expect(response.status).toBe(400);
});

describe("launch source entitlements on local SQL", () => {
  it("keeps materials but suppresses a consumed source across risk and run versions", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);
    const guest = await (await app.request("/v1/auth/guest", {method:"POST"}, env)).json() as {account:{id:string}};
    const account = guest.account.id;
    const lines = [
      {kind:"CURRENCY",id:"cur_machine_parts",quantity:160,source:"FIRST_CLEAR"},
      {kind:"ITEM",id:"cur_command_ticket",quantity:1,source:"FIRST_CLEAR"},
    ];
    const first = await prepareLaunchRewards(env.DB, account, "opn_03_water_stop", lines, false);
    expect(first.lines).toEqual(lines);
    await env.DB.batch(launchEntitlementStatements(env.DB, account, first.sources, "claim1", 1));
    const second = await prepareLaunchRewards(env.DB, account, "opn_03_water_stop", lines, false);
    expect(second.lines).toEqual([lines[0]]);
    expect(second.sources).toEqual([]);
  });
});
import { sha256Hex } from "@lastrail/common";
async function setupBound(db:initSqlJs.Database) {
 const config=readFileSync(join(process.cwd(),"packages/balance/src/launch-economy-v1.json"),"utf8");
 const version=JSON.parse(config).version;
 db.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'balance','{}',1,'test'),(2,1,0,'old','{}',1,'test')");
 db.run("INSERT INTO economy_versions VALUES(?,?,?,?)",[version,await sha256Hex(config),config,1]);
 db.run("INSERT INTO economy_version_bindings VALUES(1,?)",[version]);
 db.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at) VALUES('activation',1,'PUBLISH','test',1)");
 db.run("INSERT INTO balance_config_active VALUES(1,1,'activation',1)");
}
function policy(db:initSqlJs.Database) {
 db.run("INSERT INTO crystal_policies VALUES('wallet',?)",[JSON.stringify({currencyId:"cur_crystal",ticketExchange:null,purchase:null})]);
 db.run("INSERT INTO crystal_policy_active VALUES(1,'wallet')");
}
async function guestDb() {
 const db=await createMigratedDb(), env=createEnv(db);
 const guest=await (await app.request('/v1/auth/guest',{method:'POST'},env)).json() as {account:{id:string};accessToken:string};
 return {db,env,account:guest.account.id,authorization:`Bearer ${guest.accessToken}`};
}
function pending(db:initSqlJs.Database, account:string, run:string, version:number, risk:number, operation='opn_06_broken_line') {
 db.run("INSERT INTO account_mutations VALUES(?,?,?,?,?,?)",[`m_${run}`,account,scalar(db,`SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations WHERE account_id='${account}'`),'TEST',run,1]);
 db.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES(?,?,?,?,'COMPLETED',1)",[run,account,`m_${run}`,version]);
 db.run("INSERT INTO account_first_clears VALUES(?,?,?,?,?)",[account,operation,risk,1,run]);
 const plan={calculation:{operationId:operation},rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:5,source:'BASE'},{kind:'CURRENCY',id:'cur_credits',quantity:10,source:'FIRST_CLEAR'},{kind:'ITEM',id:'cur_command_ticket',quantity:2,source:'FIRST_CLEAR'},{kind:'CURRENCY',id:'cur_special_core',quantity:1,source:'FIRST_CLEAR'}]};
 db.run("INSERT INTO run_reward_previews(id,account_id,run_id,status,balance_config_version,reward_plan_json,expires_at,created_at) VALUES(?,?,?,'ACTIVE',?,?,?,1)",[`p_${run}`,account,run,version,JSON.stringify(plan),Date.now()+100000]);
}
async function claimRun(env:Env,authorization:string,run:string,key=run) {
 return app.request(`/v1/runs/${run}/reward-preview/p_${run}/claim`,{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':key,'Content-Type':'application/json'},body:JSON.stringify({runId:run,rewardPreviewId:`p_${run}`})},env);
}
it('replaces only future login grant; relogin and replay cannot add tickets or crystals',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 db.run("INSERT INTO account_items VALUES(?,'cur_command_ticket',9,1)",[account]);
 const request=(key:string)=>app.request('/v1/login-bonus/claim',{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':key}},env);
 expect((await request('login')).status).toBe(200);
 expect(scalar(db,'SELECT SUM(remaining) FROM crystal_lots')).toBe(500);
 expect(scalar(db,"SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(9);
 await request('login');await request('relogin');
 expect(scalar(db,'SELECT SUM(remaining) FROM crystal_lots')).toBe(500);
});
it('preserves old pinned tickets, excludes another risk and version, and accumulates material lines',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'old',2,1);pending(db,account,'new',1,2);
 expect((await claimRun(env,authorization,'old')).status).toBe(200);
 expect((await claimRun(env,authorization,'new')).status).toBe(200);
 expect(scalar(db,"SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(2);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='SPECIAL_CORE'")).toBe(1);
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_lots')).toBe(0);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(30);
 expect((await claimRun(env,authorization,'new','old')).status).toBe(409);
});
it('fails closed without policy and rolls back all source and reward mutations',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);pending(db,account,'new',1,2);
 expect((await claimRun(env,authorization,'new')).status).toBe(503);
 expect(scalar(db,'SELECT COUNT(*) FROM reward_transactions')).toBe(0);
 expect(scalar(db,'SELECT COUNT(*) FROM account_launch_entitlements')).toBe(0);
 policy(db);expect((await claimRun(env,authorization,'new')).status).toBe(200);
 expect(scalar(db,'SELECT SUM(remaining) FROM crystal_lots')).toBe(200);
});
it('recognizes historical claimed reward lines without entitlement receipts and permits remaining sources',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'historical',2,1);await claimRun(env,authorization,'historical');
 db.run('DELETE FROM account_launch_entitlements');
 pending(db,account,'later',1,3);await claimRun(env,authorization,'later');
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_lots')).toBe(0);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='SPECIAL_CORE'")).toBe(1);
 const remaining=await prepareLaunchRewards(env.DB,account,'opn_03_water_stop',[{kind:'ITEM',id:'cur_command_ticket',quantity:1,source:'FIRST_CLEAR'}],true);
 expect(remaining.lines[0]?.quantity).toBe(100);
});
it('concurrent duplicate source claims cannot grant both tickets and crystals',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'a',2,1);pending(db,account,'b',1,2);
 await Promise.all([claimRun(env,authorization,'a'),claimRun(env,authorization,'b')]);
 const tickets=scalar(db,"SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'");
 const crystals=scalar(db,'SELECT SUM(remaining) FROM crystal_lots');
 expect(tickets*100+crystals).toBe(200);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='SPECIAL_CORE'")).toBe(1);
 expect(scalar(db,'SELECT COUNT(*) FROM account_launch_entitlements')).toBe(2);
});
it('failure inside the atomic batch rolls back wallet, materials and source receipts',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);pending(db,account,'rollback',1,2);
 db.run("CREATE TRIGGER injected_failure BEFORE INSERT ON crystal_ledger BEGIN SELECT RAISE(ABORT,'injected'); END");
 expect((await claimRun(env,authorization,'rollback')).status).toBeGreaterThanOrEqual(400);
 expect(scalar(db,'SELECT COUNT(*) FROM reward_transactions')).toBe(0);
 expect(scalar(db,'SELECT COUNT(*) FROM account_launch_entitlements')).toBe(0);
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_lots')).toBe(0);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(0);
 db.run('DROP TRIGGER injected_failure');expect((await claimRun(env,authorization,'rollback')).status).toBe(200);
});
it('schema expansion preserves lots, ledgers and foreign keys with existing grants',async()=>{
 const {db,env,authorization}=await guestDb();await setupBound(db);policy(db);
 await app.request('/v1/login-bonus/claim',{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':'grant'}},env);
 const beforeLots=db.exec('SELECT * FROM crystal_lots'), beforeLedger=db.exec('SELECT * FROM crystal_ledger');
 db.run(readFileSync(join(process.cwd(),'packages/db/migrations/0026_launch_crystal_sources.sql'),'utf8'));
 expect(db.exec('SELECT * FROM crystal_lots')).toEqual(beforeLots);
 expect(db.exec('SELECT * FROM crystal_ledger')).toEqual(beforeLedger);
 expect(db.exec('PRAGMA foreign_key_check')).toEqual([]);
});
it('historical login claim remains consumed after launch activation and wallet policy removal does not invalidate replay',async()=>{
 const {db,env,authorization}=await guestDb();
 const request=(key:string)=>app.request('/v1/login-bonus/claim',{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':key}},env);
 await request('before');await setupBound(db);
 expect((await request('before')).status).toBe(200);expect((await request('after')).status).toBe(200);
 expect(scalar(db,"SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(5);
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_lots')).toBe(0);
});
it('claim replay precedes current policy validation and rejects changed body fields',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);pending(db,account,'replay',1,1);
 expect((await claimRun(env,authorization,'replay')).status).toBe(200);
 db.run('DELETE FROM crystal_policy_active');
 expect((await claimRun(env,authorization,'replay')).status).toBe(200);
 expect(scalar(db,'SELECT SUM(remaining) FROM crystal_lots')).toBe(200);
 const altered=await app.request('/v1/runs/replay/reward-preview/p_replay/claim',{method:'POST',headers:{Authorization:authorization,'Idempotency-Key':'replay','Content-Type':'application/json'},body:JSON.stringify({runId:'replay',rewardPreviewId:'p_replay',quantity:900})},env);
 expect(altered.status).toBe(400);
});
it('finite launch source catalog totals twelve tickets and three special cores',async()=>{
 const {db,env,account}=await guestDb();policy(db);
 let crystals=0,cores=0;
 for (const [operation,tickets,core] of [['launch_login_bonus_act1_v1',5,0],['opn_03_water_stop',1,0],['opn_05_night_boarders',1,0],['opn_06_broken_line',2,1],['raid_raider_king_engine',3,2]] as const) {
  const source=operation.startsWith('launch_')?'LAUNCH_LOGIN_BONUS':'FIRST_CLEAR';
  const plan=await prepareLaunchRewards(env.DB,account,operation,[{kind:'ITEM',id:'cur_command_ticket',quantity:tickets,source},...(core?[{kind:'CURRENCY',id:'cur_special_core',quantity:core,source}]:[])],true);
  crystals+=plan.lines.filter(l=>l.kind==='CRYSTAL').reduce((n,l)=>n+l.quantity,0);
  cores+=plan.lines.filter(l=>l.id==='cur_special_core').reduce((n,l)=>n+l.quantity,0);
  await env.DB.batch(launchEntitlementStatements(env.DB,account,plan.sources,operation,1));
  expect((await prepareLaunchRewards(env.DB,account,operation,plan.lines,true)).lines).toEqual([]);
 }
 expect(crystals).toBe(1200);expect(cores).toBe(3);
});
it('the database rejects two independently prepared claims of the same source',async()=>{
 const {env,account,db}=await guestDb();
 const lines=[{kind:'ITEM',id:'cur_command_ticket',quantity:1,source:'FIRST_CLEAR'}];
 const [a,b]=await Promise.all([prepareLaunchRewards(env.DB,account,'opn_03_water_stop',lines,false),prepareLaunchRewards(env.DB,account,'opn_03_water_stop',lines,false)]);
 expect(a.sources).toHaveLength(1);expect(b.sources).toHaveLength(1);
 await env.DB.batch(launchEntitlementStatements(env.DB,account,a.sources,'a',1));
 await expect(env.DB.batch(launchEntitlementStatements(env.DB,account,b.sources,'b',1))).rejects.toThrow();
 expect(scalar(db,'SELECT COUNT(*) FROM account_launch_entitlements')).toBe(1);
});
it('concurrent different-run reuse of one idempotency key rejects the changed target',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'key-a',1,1);pending(db,account,'key-b',1,2,'raid_raider_king_engine');
 const results=await Promise.all([claimRun(env,authorization,'key-a','shared'),claimRun(env,authorization,'key-b','shared')]);
 expect(results.map(r=>r.status).sort()).toEqual([200,409]);
 const bodies=await Promise.all(results.map(r=>r.json())) as Array<{status:string;errorCode:string}>;
 expect(bodies.filter(b=>b.errorCode==='idempotency_conflict')).toHaveLength(1);
 expect(scalar(db,'SELECT COUNT(*) FROM reward_transactions')).toBe(1);
});
it('concurrent different sources retry safely and preserve both ordinary grants',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'source-a',1,1);pending(db,account,'source-b',1,2,'raid_raider_king_engine');
 const results=await Promise.all([claimRun(env,authorization,'source-a'),claimRun(env,authorization,'source-b')]);
 expect(results.map(r=>r.status)).toEqual([200,200]);
 expect(scalar(db,'SELECT SUM(remaining) FROM crystal_lots')).toBe(500);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='SPECIAL_CORE'")).toBe(3);
 expect(scalar(db,"SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(30);
 expect(scalar(db,'SELECT COUNT(*) FROM reward_transactions')).toBe(2);
});
it('preview after a competing claim displays the actual remaining grant and remains consistent after claim',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);
 pending(db,account,'preview-a',1,1);pending(db,account,'preview-b',1,2);
 await claimRun(env,authorization,'preview-a');
 const preview=await (await app.request('/v1/runs/preview-b/reward-preview/p_preview-b',{},env)).json() as {rewardLines:Array<{id:string;quantity:number}>};
 expect(preview.rewardLines.map(l=>l.id)).toEqual(['cur_credits','cur_credits']);
 await claimRun(env,authorization,'preview-b');
 const after=await (await app.request('/v1/runs/preview-b/reward-preview/p_preview-b',{},env)).json() as {rewardLines:Array<{id:string;quantity:number}>};
 expect(after.rewardLines).toEqual(preview.rewardLines);
 const before=scalar(db,'SELECT COUNT(*) FROM crystal_ledger');await claimRun(env,authorization,'preview-b');
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_ledger')).toBe(before);
});
it('rejects a wallet policy switch between conversion preview and wallet planning without any grant',async()=>{
 const {db,env,account,authorization}=await guestDb();await setupBound(db);policy(db);pending(db,account,'policy-race',1,1);
 db.run("INSERT INTO crystal_policies VALUES('next-wallet',?)",[JSON.stringify({currencyId:'cur_other_crystal',ticketExchange:null,purchase:null})]);
 const originalPrepare=env.DB.prepare.bind(env.DB);let reads=0;
 env.DB.prepare=((sql:string)=>{
  if(sql.includes('SELECT p.id, p.policy_json') && ++reads===2) db.run("UPDATE crystal_policy_active SET policy_id='next-wallet' WHERE singleton_id=1");
  return originalPrepare(sql);
 }) as typeof env.DB.prepare;
 const response=await claimRun(env,authorization,'policy-race');expect(response.status).toBe(409);
 expect(scalar(db,'SELECT COUNT(*) FROM crystal_lots')).toBe(0);
 expect(scalar(db,'SELECT COUNT(*) FROM account_launch_entitlements')).toBe(0);
 expect(scalar(db,'SELECT COUNT(*) FROM reward_transactions')).toBe(0);
});
