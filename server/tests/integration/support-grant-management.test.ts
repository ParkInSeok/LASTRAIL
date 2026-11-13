import {describe,expect,it} from 'vitest';
import admin from '../../apps/admin-api/src/index';
import {setup} from '../helpers/departure-fixture';
import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';

async function fixture(){
 const f=await setup(false,true,null);
 f.sql.run("INSERT INTO admin_principals VALUES('owner','owner','OWNER','ACTIVE',NULL,0,0),('owner2','owner2','OWNER','ACTIVE',NULL,0,0),('operator','operator','OPERATOR','ACTIVE',NULL,0,0),('viewer','viewer','VIEWER','ACTIVE',NULL,0,0)");
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const path=(target='operator')=>`/v1/admin/support/processor-grants/${target}`;
 const request=(target:string,body:object,key=crypto.randomUUID(),actor='owner')=>admin.request(path(target),{method:'POST',headers:{'Cf-Access-Subject':actor,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify(body)},env);
 const read=(target='operator',actor='owner')=>admin.request(path(target),{headers:{'Cf-Access-Subject':actor}},env);
 return {...f,env,request,read,path};
}
const grant={action:'GRANT',permission:'SUPPORT_TICKET_PROCESS',expectedRevision:0,reason:'Explicit approval'};
describe('OWNER support processor grant management v1',()=>{
 it('G01/G13 grants explicitly without inheriting support read or processing authority',async()=>{
  const f=await fixture();expect((await f.read()).status).toBe(200);
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:0}});
  expect(f.scalar("SELECT COUNT(*) FROM support_ticket_processor_grants WHERE principal_id='owner'")).toBe(0);
  expect((await f.request('operator',grant)).status).toBe(200);
  expect(await (await f.read()).json()).toMatchObject({state:{granted:true,revision:1}});
  expect(f.scalar('SELECT COUNT(*) FROM support_grant_commands')).toBe(1);
  expect(f.scalar("SELECT COUNT(*) FROM admin_audit_logs WHERE action='SUPPORT_GRANT_CHANGE'")).toBe(1);
  expect((await admin.request('/v1/admin/support/tickets/absent/processing',{headers:{'Cf-Access-Subject':'owner'}},f.env)).status).toBe(403);
 });
 it('G02/G05 permits self grant and revoking the last processor',async()=>{
  const f=await fixture();expect((await f.request('owner',grant)).status).toBe(200);
  expect((await f.request('owner',{...grant,action:'REVOKE',expectedRevision:1})).status).toBe(200);
  expect(await (await f.read('owner')).json()).toMatchObject({state:{granted:false,revision:2}});
 });
 it('G03 rejects OPERATOR and VIEWER management',async()=>{
  const f=await fixture();for(const actor of ['operator','viewer']){
   expect((await f.request('operator',grant,crypto.randomUUID(),actor)).status).toBe(403);
   expect((await f.read('operator',actor)).status).toBe(403);
  }expect(f.scalar('SELECT COUNT(*) FROM support_ticket_processor_grants')).toBe(0);
 });
 it('G04 rejects granting to disabled/viewer targets and allows inactive revoke/no change',async()=>{
  const f=await fixture();f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='operator'");
  expect((await f.request('operator',grant)).status).toBe(409);
  expect((await f.request('viewer',grant)).status).toBe(409);
  expect(await (await f.request('operator',{...grant,action:'REVOKE'})).json()).toMatchObject({result:{outcome:'NO_CHANGE',revision:0,granted:false}});
  f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
  expect((await f.request('operator',{...grant,action:'REVOKE',expectedRevision:1})).status).toBe(200);
 });
 it('G09 keeps a committed answer and blocks a pending answer overtaken by revoke',async()=>{
  const f=await fixture();await f.request('operator',grant);
  f.sql.run("INSERT INTO support_tickets(id,account_id,category,status,body_text,body_preview,body_length,server_received_at,idempotency_key,request_hash,created_at,updated_at) VALUES('ticket','a','BUG_OTHER','OPEN','Private original','Private original',16,0,'ticket-key','hash',0,0)");
  const answer=()=>admin.request('/v1/admin/support/tickets/ticket/processing',{method:'POST',headers:{'Cf-Access-Subject':'operator','Content-Type':'application/json','Idempotency-Key':crypto.randomUUID()},body:JSON.stringify({expectedRevision:0,status:'IN_REVIEW',publicReply:'Public answer'})},f.env);
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("DELETE FROM support_ticket_processor_grants WHERE principal_id='operator'");};
  expect((await answer()).status).toBe(403);
  expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(0);
  await f.request('operator',{...grant,expectedRevision:2});
  expect((await answer()).status).toBe(200);
  expect((await f.request('operator',{...grant,action:'REVOKE',expectedRevision:3})).status).toBe(200);
  expect(f.scalar('SELECT public_reply_text FROM support_ticket_commands')).toBe('Public answer');
  expect((await answer()).status).toBe(403);
  expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(1);
 });
 it('preserves legacy grants/receipts but invalidates inactive legacy grants before reactivation',async()=>{
  const SQL=await initSqlJs(),db=new SQL.Database();db.run('PRAGMA foreign_keys=ON');
  for(const file of readdirSync('packages/db/migrations').filter(f=>f.endsWith('.sql')&&f<'0096').sort())db.run(readFileSync('packages/db/migrations/'+file,'utf8'));
  db.run("INSERT INTO admin_principals VALUES('owner','owner','OWNER','ACTIVE',NULL,0,0),('inactive','inactive','OPERATOR','DISABLED',NULL,0,0)");
  db.run("INSERT INTO support_ticket_processor_grants VALUES('owner','SUPPORT_TICKET_PROCESS',0),('inactive','SUPPORT_TICKET_PROCESS',0)");
  db.run(readFileSync('packages/db/migrations/0096_support_grant_management.sql','utf8'));
  expect(db.exec("SELECT principal_id FROM support_ticket_processor_grants")[0]?.values).toEqual([['owner']]);
  db.run("UPDATE admin_principals SET status='ACTIVE' WHERE id='inactive'");
  expect(db.exec("SELECT principal_id FROM support_ticket_processor_grants")[0]?.values).toEqual([['owner']]);
  expect(db.exec("SELECT revision FROM support_grant_revisions WHERE principal_id='inactive'")[0]?.values).toEqual([[2]]);db.close();
 });
 it('exposes last change metadata without inquiry content or auth identity',async()=>{
  const f=await fixture();await f.request('operator',grant);
  const body=await (await f.read()).json();
  expect(body).toMatchObject({state:{displayName:null,lastChange:{actorId:'owner',reason:grant.reason,outcome:'APPLIED'}}});
  expect(JSON.stringify(body)).not.toContain('access_subject');
  expect(JSON.stringify(body)).not.toContain('body_text');
 });
 it('keeps management audit history OWNER-only through the existing general audit route',async()=>{
  const f=await fixture();await f.request('operator',grant);
  const audit=(actor:string)=>admin.request('/v1/admin/audit?action=SUPPORT_GRANT_CHANGE&resourceId=operator',{headers:{'Cf-Access-Subject':actor}},f.env);
  expect(await (await audit('owner')).json()).toMatchObject({items:[{action:'SUPPORT_GRANT_CHANGE',resourceId:'operator'}]});
  for(const actor of ['operator','viewer'])expect(await (await audit(actor)).json()).toMatchObject({items:[]});
 });
 it('G07/G08/G14 preserves original receipts through revoke/regrant and rejects payload conflicts',async()=>{
  const f=await fixture(),key=crypto.randomUUID();const first=await (await f.request('operator',grant,key)).json();
  expect((await f.request('operator',{...grant,action:'REVOKE',expectedRevision:1})).status).toBe(200);
  expect(await (await f.request('operator',grant,key)).json()).toEqual(first);
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:2}});
  expect((await f.request('operator',{...grant,reason:'Different'},key)).status).toBe(409);
  expect((await f.request('operator',grant)).status).toBe(409);
  expect((await f.request('operator',{...grant,expectedRevision:2})).status).toBe(200);
  expect(await (await f.read()).json()).toMatchObject({state:{granted:true,revision:3}});
  const noop=await (await f.request('operator',{...grant,expectedRevision:3})).json();
  expect(noop).toMatchObject({result:{outcome:'NO_CHANGE',revision:3}});
  expect(f.scalar('SELECT COUNT(*) FROM support_grant_commands')).toBe(4);
 });
 it('G06 rejects an OWNER command overtaken by another OWNER',async()=>{
  const f=await fixture();
  const results=await Promise.all([f.request('operator',grant,crypto.randomUUID(),'owner'),f.request('operator',grant,crypto.randomUUID(),'owner2')]);
  expect(results.map(r=>r.status).sort()).toEqual([200,409]);
  expect(f.scalar('SELECT COUNT(*) FROM support_grant_commands')).toBe(1);
  expect(f.scalar("SELECT revision FROM support_grant_revisions WHERE principal_id='operator'")).toBe(1);
 });
 it.each([false,true])('G07 resolves a same-key commit between receipt and revision reads (different payload=%s)',async different=>{
  const f=await fixture(),key=crypto.randomUUID(),prepare=f.adapter.prepare.bind(f.adapter);
  let once=true,winner:unknown;
  f.adapter.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.includes('SELECT p.id,p.display_name')){
    const first=statement.first.bind(statement);
    statement.first=async<T>()=>{
     if(once){once=false;winner=await (await f.request('operator',different?{...grant,reason:'Competing payload'}:grant,key)).json();}
     return first<T>();
    };
   }return statement;
  };
  const loser=await f.request('operator',grant,key);
  if(different){expect(loser.status).toBe(409);expect(await loser.json()).toMatchObject({error:'idempotency_conflict'});}
  else{expect(loser.status).toBe(200);expect(await loser.json()).toEqual(winner);}
  expect(f.scalar('SELECT COUNT(*) FROM support_grant_commands')).toBe(1);
  expect(f.scalar("SELECT revision FROM support_grant_revisions WHERE principal_id='operator'")).toBe(1);
 });
 it('G15 checks OWNER authority at commit and on original receipt replay',async()=>{
  const f=await fixture(),key=crypto.randomUUID();expect((await f.request('operator',grant,key)).status).toBe(200);
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='OPERATOR' WHERE id='owner'");};
  expect((await f.request('operator',{...grant,action:'REVOKE',expectedRevision:1})).status).toBe(403);
  expect((await f.request('operator',grant,key)).status).toBe(403);
  expect(f.scalar("SELECT COUNT(*) FROM support_ticket_processor_grants WHERE principal_id='operator'")).toBe(1);
 });
 it('G10/G16 invalidates on disable/demotion/delete and never restores implicitly',async()=>{
  const f=await fixture();await f.request('operator',grant);
  f.sql.run("UPDATE admin_principals SET role='OWNER' WHERE id='operator'");
  expect(await (await f.read()).json()).toMatchObject({state:{granted:true,revision:1}});
  f.sql.run("UPDATE admin_principals SET role='OPERATOR' WHERE id='operator'");
  f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='operator'");
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:2}});
  f.sql.run("UPDATE admin_principals SET role='OPERATOR' WHERE id='operator'");
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:2}});
  await f.request('operator',{...grant,expectedRevision:2});
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='operator'");
  f.sql.run("UPDATE admin_principals SET status='ACTIVE' WHERE id='operator'");
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:4}});
  await f.request('operator',{...grant,expectedRevision:4});
  f.sql.run("DELETE FROM admin_principals WHERE id='operator'");
  expect(f.scalar("SELECT COUNT(*) FROM support_ticket_processor_grants WHERE principal_id='operator'")).toBe(0);
  expect(f.scalar("SELECT revision FROM support_grant_revisions WHERE principal_id='operator'")).toBe(6);
  f.sql.run("INSERT INTO admin_principals VALUES('operator','new-operator','OPERATOR','ACTIVE',NULL,0,0)");
  expect(await (await f.read()).json()).toMatchObject({state:{granted:false,revision:6}});
 });
 it('rolls principal invalidation and deletion back when their atomic audit fails',async()=>{
  const f=await fixture();await f.request('operator',grant);
  f.sql.run("CREATE TRIGGER fail_invalidation BEFORE INSERT ON admin_audit_logs WHEN NEW.action='SUPPORT_GRANT_INVALIDATE' BEGIN SELECT RAISE(ABORT,'audit_failure'); END");
  expect(()=>f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='operator'")).toThrow('audit_failure');
  expect(()=>f.sql.run("DELETE FROM admin_principals WHERE id='operator'")).toThrow('audit_failure');
  expect(await (await f.read()).json()).toMatchObject({state:{role:'OPERATOR',granted:true,revision:1}});
 });
 it('preserves monotonically increasing revision tombstones',async()=>{
  const f=await fixture();await f.request('operator',grant);await f.request('operator',{...grant,action:'REVOKE',expectedRevision:1});
  expect(()=>f.sql.run("UPDATE support_grant_revisions SET revision=0 WHERE principal_id='operator'")).toThrow();
  expect(()=>f.sql.run("DELETE FROM support_grant_revisions WHERE principal_id='operator'")).toThrow();
  expect(await (await f.read()).json()).toMatchObject({state:{revision:2,granted:false}});
 });
 it('G11 rolls grant, receipt and revision back if audit storage fails',async()=>{
  const f=await fixture();f.sql.run("CREATE TRIGGER fail_grant_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.action='SUPPORT_GRANT_CHANGE' BEGIN SELECT RAISE(ABORT,'audit_test_failure'); END");
  expect((await f.request('operator',grant)).status).toBe(503);
  expect(f.scalar('SELECT COUNT(*) FROM support_ticket_processor_grants')).toBe(0);
  expect(f.scalar('SELECT COUNT(*) FROM support_grant_commands')).toBe(0);
  expect(await (await f.read()).json()).toMatchObject({state:{revision:0}});
 });
 it('G12 enforces trusted deployment environment and independent databases',async()=>{
  const f=await fixture(),other=await fixture();expect((await f.request('operator',{...grant,environment:'PRD'})).status).toBe(400);
  expect((await f.request('operator',grant)).status).toBe(200);
  expect(await (await other.read()).json()).toMatchObject({state:{granted:false,revision:0}});
  expect((await admin.request(f.path(),{headers:{'Cf-Access-Subject':'owner'}},{...f.env,ENVIRONMENT:'LOCAL'})).status).toBe(503);
 });
 it('validates reasons, fixed permission, target and request key without granting',async()=>{
  const f=await fixture();for(const body of [{...grant,reason:' '},{...grant,reason:'x'.repeat(501)},{...grant,permission:'SUPPORT_READ'},{...grant,expectedRevision:-1}])expect((await f.request('operator',body)).status).toBe(400);
  expect((await f.request('absent',grant)).status).toBe(404);
  expect((await f.request('operator',grant,'')).status).toBe(400);
  expect(f.scalar('SELECT COUNT(*) FROM support_ticket_processor_grants')).toBe(0);
 });
});
