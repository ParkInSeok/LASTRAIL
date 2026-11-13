import {describe,it,expect} from 'vitest';
import app from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';

async function fixture(){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const plan={applicationId:'LASTRAIL',serviceScope:'GAME_AND_WEB',webMode:'READ_ONLY',title:'TEST_ONLY 점검 계획',summary:'공개 전 미리보기',noticeId:null,drainStartsAt:1000,startsAt:2000,estimatedEndAt:3000,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15};
 const command=(body:object,key=crypto.randomUUID(),role='OWNER',id='m1')=>app.request(`/v1/admin/maintenance-plans/${id}/commands`,{method:'POST',headers:{'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify({environment:'DEV',reason:'TEST_ONLY internal reason',...body})},env);
 const save=(document:object=plan,expectedDraftRevision:number|null=null,key=crypto.randomUUID(),role='OWNER',id='m1')=>command({command:'SAVE_DRAFT',expectedDraftRevision,plan:document},key,role,id);
 const read=(path='/v1/admin/maintenance-plans/m1',role='VIEWER')=>app.request(path,{headers:{'Cf-Access-Subject':role}},env);
 const preview=(at:number)=>read(`/v1/admin/maintenance-plans/m1/preview?at=${at}`);
 return {...f,env,plan,command,save,read,preview};
}

describe('maintenance draft authority and preview-only contract',()=>{
 it('uses existing auth, allows only OWNER to save and never claims an applied policy',async()=>{
  const f=await fixture();expect((await app.request('/v1/admin/maintenance-plans',{},f.env)).status).toBe(401);
  for(const role of ['OPERATOR','VIEWER'])expect((await f.save(f.plan,null,'denied',role)).status).toBe(403);
  expect((await f.save()).status).toBe(200);
  const r=await f.read();expect(r.status).toBe(200);expect(r.headers.get('Cache-Control')).toBe('no-store');
  expect(await r.json()).toMatchObject({contractVersion:'admin-maintenance-plan-v1',plan:{maintenanceId:'m1',draftRevision:1,planState:'DRAFT'},application:{applied:false,currentPolicyRevision:null,status:'UNCONNECTED'},permissions:{canSaveDraft:false,canApply:false}});
  expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);
 });
 it('previews exact drain/start boundaries without treating the estimated end as completion',async()=>{
  const f=await fixture();await f.save();
  for(const [at,gameMode,webMode] of [[999,'NORMAL','NORMAL'],[1000,'DRAINING','NORMAL'],[1999,'DRAINING','NORMAL'],[2000,'MAINTENANCE','READ_ONLY'],[4000,'MAINTENANCE','READ_ONLY']] as const){
   const r=await f.preview(at);expect(r.status).toBe(200);
   expect(await r.json()).toMatchObject({preview:{kind:'HYPOTHETICAL_SCHEDULE',applied:false,at,effectiveGameMode:gameMode,effectiveWebMode:webMode},application:{applied:false,currentPolicyRevision:null}});
  }
 });
 it('keeps game and web targets independent and emergency proposals have no drain',async()=>{
  const f=await fixture();await f.save({...f.plan,serviceScope:'WEB',webMode:'MAINTENANCE',drainStartsAt:null,existingRunPolicy:null});
  expect(await (await f.preview(2500)).json()).toMatchObject({preview:{effectiveGameMode:'NORMAL',effectiveWebMode:'MAINTENANCE'}});
  await f.save({...f.plan,serviceScope:'GAME',webMode:null,drainStartsAt:null,existingRunPolicy:'EMERGENCY_PRESERVE'},1);
  expect(await (await f.preview(1500)).json()).toMatchObject({preview:{effectiveGameMode:'NORMAL',effectiveWebMode:'NORMAL'}});
  expect(await (await f.preview(2500)).json()).toMatchObject({preview:{effectiveGameMode:'MAINTENANCE',effectiveWebMode:'NORMAL'}});
 });
 it('rejects incoherent schedules, unsupported public references and readiness supplied by the client',async()=>{
  const f=await fixture();
  for(const changes of [{drainStartsAt:3000},{estimatedEndAt:1999},{statusFreshnessSeconds:0},{retryAfterSeconds:1.5},{title:'<script>bad</script>'},{noticeId:'unverified'},{serviceScope:'WEB'},{serviceScope:'GAME',webMode:'READ_ONLY'},{existingRunPolicy:'EMERGENCY_PRESERVE'},{ready:true}])expect((await f.save({...f.plan,...changes})).status).toBe(400);
  expect((await f.command({command:'SAVE_DRAFT',expectedDraftRevision:null,plan:f.plan,environment:'PRD'})).status).toBe(400);
  expect((await f.command({command:'SAVE_DRAFT',expectedDraftRevision:null,plan:f.plan,ready:true})).status).toBe(400);
 });
 it('blocks every operational transition without changing the draft or creating a false success receipt',async()=>{
  const f=await fixture();await f.save();
  for(const command of ['SCHEDULE','ACTIVATE','EXTEND','COMPLETE','CANCEL']){
   const r=await f.command({command,expectedDraftRevision:1});expect(r.status).toBe(503);expect(await r.json()).toMatchObject({error:'maintenance_apply_unavailable'});
  }
  expect(f.scalar('SELECT count(*) FROM maintenance_plan_revisions')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM maintenance_plan_receipts')).toBe(1);
  expect((await game.request('/v1/public/maintenance-plans/m1',{},f.env)).status).toBe(404);
 });
 it('restores original save results after later edits and rejects changed-key payloads or stale edits',async()=>{
  const f=await fixture();const original=await (await f.save(f.plan,null,'save')).json();
  expect((await f.save({...f.plan,title:'revised'},1,'edit')).status).toBe(200);
  expect(await (await f.save(f.plan,null,'save')).json()).toEqual(original);
  expect((await f.save({...f.plan,title:'changed'},null,'save')).status).toBe(409);
  expect((await f.save(f.plan,1,'stale')).status).toBe(409);
  expect(await (await f.read()).json()).toMatchObject({plan:{draftRevision:2,document:{title:'revised'}}});
 });
 it('serializes edits and rolls back if the success audit fails',async()=>{
  const f=await fixture();const same=await Promise.all([f.save(f.plan,null,'same'),f.save(f.plan,null,'same')]);
  expect(same.map(r=>r.status)).toEqual([200,200]);expect(await same[0]!.json()).toEqual(await same[1]!.json());
  const competing=await Promise.all([f.save({...f.plan,title:'a'},1,'a'),f.save({...f.plan,title:'b'},1,'b')]);expect(competing.map(r=>r.status).sort()).toEqual([200,409]);
  f.sql.run("CREATE TRIGGER test_maintenance_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.result='SUCCESS' BEGIN SELECT RAISE(ABORT,'TEST_AUDIT'); END;");
  expect((await f.save(f.plan,2,'rollback')).status).toBe(503);
  expect(f.scalar('SELECT count(*) FROM maintenance_plan_revisions')).toBe(2);
  expect(f.scalar('SELECT count(*) FROM maintenance_plan_receipts')).toBe(2);
 });
 it('rechecks OWNER in the atomic write and preserves historical revisions',async()=>{
  const f=await fixture();await f.save();
  f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
  expect((await f.save(f.plan,1)).status).toBe(403);
  expect(f.scalar('SELECT count(*) FROM maintenance_plan_revisions')).toBe(1);
  expect(()=>f.sql.run('DELETE FROM maintenance_plan_revisions')).toThrow('maintenance_plan_revision_immutable');
 });
 it('does not treat invalid queries or failed storage as normal service status',async()=>{
  const f=await fixture();await f.save();
  for(const q of ['at=-1','at=1.5','at=1&at=2','at=9007199254740992','ready=true'])expect((await f.read('/v1/admin/maintenance-plans/m1/preview?'+q)).status).toBe(400);
  expect((await f.read('/v1/admin/maintenance-plans?limit=51')).status).toBe(400);
  expect((await f.read('/v1/admin/maintenance-plans/missing')).status).toBe(404);
  f.sql.run('DROP TABLE maintenance_plan_heads');expect((await f.read()).status).toBe(503);
 });
});
describe('maintenance read and retry boundaries',()=>{
 it('recovers identical commit after an initial empty receipt read',async()=>{
  const f=await fixture();const prepare=f.env.DB.prepare.bind(f.env.DB);let intercept=true;let winner:unknown;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT payload_hash,response_json FROM maintenance_plan_receipts')&&intercept){const first=statement.first.bind(statement);
    statement.first=async <T>()=>{const result=await first<T>();if(intercept){intercept=false;const r=await f.save(f.plan,null,'same');expect(r.status).toBe(200);winner=await r.json();}return result;};
   }
   return statement;
  };
  const replay=await f.save(f.plan,null,'same');expect(replay.status).toBe(200);expect(await replay.json()).toEqual(winner);
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE action='MAINTENANCE_DRAFT_SAVE'")).toBe(1);
 });
 it('paginates administrative drafts and keeps environment isolation',async()=>{
  const f=await fixture();
  for(const id of ['a','b','c'])expect((await f.save(f.plan,null,id,'OWNER',id)).status).toBe(200);
  const first=await f.read('/v1/admin/maintenance-plans?limit=2');expect(first.status).toBe(200);
  expect(await first.json()).toMatchObject({items:[{maintenanceId:'a'},{maintenanceId:'b'}],nextAfter:'b',application:{status:'UNCONNECTED'}});
  expect(await (await f.read('/v1/admin/maintenance-plans?limit=2&after=b')).json()).toMatchObject({items:[{maintenanceId:'c'}],nextAfter:null});
  const prd=await app.request('/v1/admin/maintenance-plans',{headers:{'Cf-Access-Subject':'OWNER'}},{...f.env,ENVIRONMENT:'PRD'});
  expect(prd.status).toBe(200);expect(await prd.json()).toMatchObject({items:[],application:{status:'UNCONNECTED',currentPolicyRevision:null}});
 });
 it('does not allow readiness commands or another identifier to reuse a saved request key',async()=>{
  const f=await fixture();await f.save(f.plan,null,'save');
  expect((await f.save(f.plan,null,'save','OWNER','m2')).status).toBe(409);
  expect((await f.command({command:'ACTIVATE',expectedDraftRevision:1},'activate','VIEWER')).status).toBe(403);
  expect((await f.save({...f.plan,startsAt:Number.MAX_SAFE_INTEGER+1},1,'invalid')).status).toBe(400);
  expect(()=>f.sql.run('DELETE FROM maintenance_plan_receipts')).toThrow('maintenance_plan_receipt_immutable');
 });
});