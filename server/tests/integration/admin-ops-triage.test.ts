import {describe,it,expect} from 'vitest';
import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import app from '../../apps/admin-api/src/index';
import {SqlJsD1Database} from '../helpers/growth-d1';

const base='/v1/admin/ops/error-logs';
const headers={'Cf-Access-Subject':'ops'};
async function setup(){
 const SQL=await initSqlJs(),db=new SQL.Database();
 db.run('PRAGMA foreign_keys=ON');
 for(const name of readdirSync('packages/db/migrations').filter(n=>n.endsWith('.sql')).sort())db.run(readFileSync('packages/db/migrations/'+name,'utf8'));
 db.run("INSERT INTO admin_principals VALUES('ops','ops','OPERATOR','ACTIVE','Ops',0,0)");
 db.run("INSERT INTO accounts(id,status,created_at,updated_at) VALUES('a','ACTIVE',0,0),('b','ACTIVE',0,0)");
 const adapter=new SqlJsD1Database(db);
 const env={ENVIRONMENT:'LOCAL',ADMIN_AUTH_MODE:'LOCAL_HEADER',DB:adapter} as unknown as Env;
 const seed=(id:string,environment='DEV',version='1',account:string|null='a',count=3,phase='RUNTIME',at=1000)=>{
  db.run(`INSERT INTO ops_error_logs(id,occurred_at,environment,service,severity,error_code,message_summary,status,source,account_id,correlation_json,sanitized_context_json,retryable,masked,created_at)
   VALUES(?,?,?,'game_api','error','frontend_unity_error','Safe summary','open','game_api',?,'{}',?,0,1,?)`,
   [id,at,environment,account,JSON.stringify({appVersion:version,appVersionCode:version,serverVersion:'s1',contractVersion:'v2',phase,occurrenceCount:count,lastOccurredAt:at+100}),at+100]);
 };
 const get=(path:string)=>app.request(base+path,{headers},env);
 return {db,adapter,env,seed,get};
}

describe('ops investigation read projection',()=>{
 it('uses the same filters for list and summary, separating occurrences and unknown accounts',async()=>{
  const {seed,get}=await setup();seed('one');seed('two','DEV','1','a',5);seed('three','DEV','1',null,2);seed('prod','PRD');
  const list=await get('?environment=DEV&limit=1');expect(list.status).toBe(200);
  expect((await list.json()).items).toHaveLength(1);
  const summary=await get('/summary?environment=DEV&limit=1');expect(summary.status).toBe(200);
  expect(await summary.json()).toMatchObject({recentErrorCount:3,eventCount:3,occurrenceCount:10,affectedAccountCount:1,unattributedEventCount:1,collectionAvailable:true,scope:'FILTERED_ALL'});
 });
 it('groups by environment, phase and exact builds without mutating original events',async()=>{
  const {seed,get,db}=await setup();seed('one');seed('two','DEV','1','b',5);seed('build','DEV','2');seed('phase','DEV','1','a',3,'RECOVERY');seed('prod','PRD');
  const response=await get('/groups?environment=DEV&limit=1');expect(response.status).toBe(200);
  const body=await response.json();expect(body.totalGroupCount).toBe(3);expect(body.items).toHaveLength(1);expect(body.nextCursor).toBe('1');
  const all=await (await get('/groups?environment=DEV')).json();
  expect(all.items.find((x:{eventCount:number})=>x.eventCount===2)).toMatchObject({occurrenceCount:8,affectedAccountCount:2,phase:'RUNTIME',appVersion:'1',firstOccurredAt:new Date(1000).toISOString(),lastOccurredAt:new Date(1100).toISOString()});
  expect(db.exec('SELECT COUNT(*) FROM ops_error_logs')[0].values[0][0]).toBe(5);
 });
 it('rejects invalid time filters and revoked read authority',async()=>{
  const {seed,get,db}=await setup();seed('one');
  expect((await get('/summary?occurredFrom=invalid')).status).toBe(400);
  expect((await get('?occurredFrom=2026-10-20&occurredTo=2026-10-19')).status).toBe(400);
  db.run("UPDATE admin_principals SET role='VIEWER'");expect((await get('/groups')).status).toBe(403);
 });
 it('reports missing occurrence data and query failures explicitly, with full totals beyond a page',async()=>{
  const {seed,get,db}=await setup();seed('one');db.run("UPDATE ops_error_logs SET sanitized_context_json='{}'");
  expect(await (await get('/summary')).json()).toMatchObject({eventCount:1,occurrenceCount:null,occurrenceUnknownEventCount:1});
  expect(await (await get('/groups?cursor=5')).json()).toMatchObject({totalGroupCount:1,items:[],nextCursor:null});
  db.run('DROP TABLE ops_error_logs');const unavailable=await get('/summary');expect(unavailable.status).toBe(503);
  expect(await unavailable.json()).toEqual({error:'ops_error_collection_unavailable',collectionAvailable:false});
 });
});

describe('ops investigation commands',()=>{
 const command=(env:Env,id:string,key:string,body:unknown)=>app.request(`${base}/${id}/investigation`,{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify(body)},env);
 const body=(status:string,expectedVersion:number)=>({status,expectedVersion,reason:'Investigated safely',fixBuild:null,evidence:null,verificationResult:null});
 it('commits state and before/after audit once, returns final state and recovers same-key response',async()=>{
  const {seed,env,db}=await setup();seed('one');
  const response=await command(env,'one','request-1',body('acknowledged',0));expect(response.status).toBe(200);
  const accepted=await response.json();expect(accepted).toMatchObject({accepted:true,investigation:{status:'acknowledged',version:1,updatedBy:'ops'}});
  const replay=await command(env,'one','request-1',body('acknowledged',0));expect(await replay.json()).toEqual(accepted);
  expect(db.exec("SELECT COUNT(*) FROM admin_audit_logs WHERE permission='OPS_ERROR_LOG_WRITE'")[0].values[0][0]).toBe(1);
  expect(JSON.parse(String(db.exec('SELECT change_summary_json FROM admin_audit_logs')[0].values[0][0]))).toMatchObject({before:{status:'open',version:0},after:{status:'acknowledged',version:1},reason:'Investigated safely'});
  expect(db.exec('PRAGMA foreign_key_check')).toEqual([]);
  expect(db.exec('SELECT status FROM ops_error_logs')[0].values[0][0]).toBe('open');
  const conflict=await command(env,'one','request-2',body('acknowledged',0));expect(conflict.status).toBe(409);expect(await conflict.json()).toMatchObject({current:{version:1,status:'acknowledged'}});
  expect((await command(env,'one','request-1',body('verifying',1))).status).toBe(409);
 });
 it('requires verification evidence and distinguishes target-build recurrence from older-build occurrences',async()=>{
  const {seed,env,get}=await setup();seed('one');
  expect((await command(env,'one','a',body('resolved',0))).status).toBe(400);
  await command(env,'one','b',body('acknowledged',0));await command(env,'one','c',body('verifying',1));
  expect((await command(env,'one','d',body('resolved',2))).status).toBe(400);
  const resolved=await command(env,'one','e',{...body('resolved',2),fixBuild:{appVersion:'2',appVersionCode:'2',serverVersion:'s1'},evidence:'QA-20261019-001',verificationResult:'PASS'});
  expect(resolved.status).toBe(200);
  const at=Date.now()+1000;seed('old','DEV','1','a',1,'RUNTIME',at);seed('recurrence','DEV','2','a',1,'RUNTIME',at);seed('other-env','PRD','2','a',1,'RUNTIME',at);
  const detail=await (await get('/one/investigation')).json();
  expect(detail.recurrence).toMatchObject({targetBuildEventCount:1,otherBuildEventCount:1,targetBuildRecurred:true});
  expect(detail.recurrence.items.map((x:{id:string})=>x.id).sort()).toEqual(['old','recurrence']);
  expect(detail.investigation.status).toBe('resolved');
 });
 it('rolls back on audit failure and authority revocation at commit, without broadening read authority',async()=>{
  const {seed,env,adapter,db}=await setup();seed('one');
  db.run("CREATE TRIGGER deny_ops_audit BEFORE INSERT ON admin_audit_logs BEGIN SELECT RAISE(ABORT,'audit unavailable'); END");
  expect((await command(env,'one','fail',body('acknowledged',0))).status).toBe(503);
  expect(db.exec('SELECT COUNT(*) FROM ops_error_investigations')[0].values[0][0]).toBe(0);
  db.run('DROP TRIGGER deny_ops_audit');
  adapter.beforeBatch=()=>db.run("UPDATE admin_principals SET role='VIEWER'");
  expect((await command(env,'one','revoke',body('acknowledged',0))).status).toBe(403);
  expect(db.exec('SELECT COUNT(*) FROM ops_error_investigations')[0].values[0][0]).toBe(0);
 });
 it('allows OWNER explicitly and denies VIEWER',async()=>{
  const {seed,env,db}=await setup();seed('one');db.run("UPDATE admin_principals SET role='VIEWER'");
  expect((await command(env,'one','denied',body('acknowledged',0))).status).toBe(403);
  db.run("UPDATE admin_principals SET role='OWNER'");expect((await command(env,'one','owner',body('acknowledged',0))).status).toBe(200);
 });
 it('serializes competing operators and recovers committed ACK loss with the original key',async()=>{
  const {seed,env,adapter,db}=await setup();seed('one');
  const responses=await Promise.all([command(env,'one','race-a',body('acknowledged',0)),command(env,'one','race-b',body('acknowledged',0))]);
  expect(responses.map(r=>r.status).sort()).toEqual([200,409]);
  const batch=adapter.batch.bind(adapter);adapter.batch=async statements=>{await batch(statements);throw new Error('ACK lost');};
  const recovered=await command(env,'one','lost',body('verifying',1));expect(recovered.status).toBe(200);
  expect(await recovered.json()).toMatchObject({investigation:{version:2,status:'verifying'}});
  expect(db.exec('SELECT COUNT(*) FROM admin_audit_logs')[0].values[0][0]).toBe(2);
 });
 it('returns the same receipt when an identical request commits between receipt lookup and state read',async()=>{
  const {seed,env,adapter}=await setup();seed('one');
  const prepare=adapter.prepare.bind(adapter);let injected=false;let winner:unknown;
  adapter.prepare=(sql:string)=>{const statement=prepare(sql);const first=statement.first.bind(statement);
   statement.first=async<T>()=>{const result=await first<T>();if(sql.startsWith('SELECT payload_hash,response_json')&&!injected){injected=true;winner=await (await command(env,'one','same-race',body('acknowledged',0))).json();}return result;};return statement;};
  const response=await command(env,'one','same-race',body('acknowledged',0));expect(response.status).toBe(200);expect(await response.json()).toEqual(winner);
 });
 it('separates unknown recurrence builds and always links a target recurrence beyond the first page',async()=>{
  const {seed,env,db,get}=await setup();seed('one');await command(env,'one','r1',body('acknowledged',0));await command(env,'one','r2',body('verifying',1));
  await command(env,'one','r3',{...body('resolved',2),fixBuild:{appVersion:'2',appVersionCode:'2',serverVersion:'s1'},evidence:'QA-case',verificationResult:'PASS'});
  const at=Date.now()+1000;for(let i=0;i<51;i++)seed('old-'+i,'DEV','1','a',1,'RUNTIME',at);
  seed('unknown','DEV','1','a',1,'RUNTIME',at);db.run("UPDATE ops_error_logs SET sanitized_context_json=json_remove(sanitized_context_json,'$.appVersionCode') WHERE id='unknown'");
  seed('target-last','DEV','2','a',1,'RUNTIME',at+1);
  expect(await (await get('/one/investigation')).json()).toMatchObject({recurrence:{targetBuildEventCount:1,otherBuildEventCount:51,unknownBuildEventCount:1,latestTargetEventId:'target-last',hasMore:true}});
 });
 it('retains investigation and receipt after original diagnostic retention, without fabricating recurrence',async()=>{
  const {seed,env,db,get}=await setup();seed('one');
  const response=await command(env,'one','retained',body('acknowledged',0));expect(response.status).toBe(200);
  db.run('DELETE FROM ops_error_logs');
  expect(await (await command(env,'one','retained',body('acknowledged',0))).json()).toEqual(await response.json());
  expect(await (await get('/one/investigation')).json()).toMatchObject({investigation:{version:1},recurrence:{available:false}});
 });
 it('rejects oversized and unexpected fields without writing',async()=>{
  const {seed,env,db}=await setup();seed('one');
  expect((await command(env,'one','invalid',{...body('acknowledged',0),rawBody:'secret'})).status).toBe(400);
  expect((await command(env,'one','oversize',{...body('acknowledged',0),reason:'x'.repeat(9000)})).status).toBe(400);
  expect(db.exec('SELECT COUNT(*) FROM admin_audit_logs')[0].values[0][0]).toBe(0);
 });
});
