import {describe,it,expect} from 'vitest';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';

async function fixture(environment='PRD') {
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER']) f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 const env={...f.env,ENVIRONMENT:environment,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const document={presentationRevision:1,type:'GENERAL',title:'TEST_ONLY public notice',summary:'Public summary',localizedBody:{ko:'공개 본문',en:'Public body'},bodyFormat:'PLAIN_TEXT',surfaces:'WEB',visibility:'PUBLIC',channel:null,platform:null,minVersionCode:null,maxVersionCode:null,startsAt:0,endsAt:null,imageAssetId:null,action:null,popupMode:'NONE',priority:0,pinned:false};
 const command=(id:string,body:object,key=crypto.randomUUID(),role='OWNER')=>admin.request(`/v1/admin/notices/${id}/commands`,{method:'POST',headers:{'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify({environment,reason:'TEST_ONLY internal reason',...body})},env);
 const save=(id='n1',doc:object=document,revision:number|null=null,key=crypto.randomUUID(),role='OWNER')=>command(id,{command:'SAVE_DRAFT',expectedRevision:revision,document:doc},key,role);
 const publish=(id='n1',revision=1,key=crypto.randomUUID(),role='OWNER')=>command(id,{command:'PUBLISH',expectedRevision:revision},key,role);
 const read=(path='/v1/public/notices?locale=ko',headers:Record<string,string>={})=>game.request(path,{headers},{...env,CLIENT_ADMISSION_MODE:'ENFORCED'});
 const detail=(id='n1',suffix='')=>read(`/v1/public/notices/${id}?locale=ko${suffix}`);
 const getAdmin=(path='/v1/admin/notices',role='VIEWER')=>admin.request(path,{headers:{'Cf-Access-Subject':role}},env);
 return {...f,env,document,command,save,publish,read,detail,getAdmin};
}

describe('shared notices and public projection',()=>{
 it('keeps drafts private, publishes only approved fields, and reuses Admin permissions',async()=>{
  const f=await fixture();
  expect((await admin.request('/v1/admin/notices',{},f.env)).status).toBe(401);
  expect((await f.save('n1',f.document,null,'denied','VIEWER')).status).toBe(403);
  expect((await f.save('n1',f.document,null,'draft','OPERATOR')).status).toBe(200);
  expect((await f.detail()).status).toBe(404);
  expect((await f.publish('n1',1,'denied-publish','OPERATOR')).status).toBe(403);
  expect((await f.publish()).status).toBe(200);
  const response=await f.detail();expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');
  const body=await response.json();expect(body).toMatchObject({contractVersion:'public-notice-v1',environment:'PRD',notice:{noticeId:'n1',revision:2,body:'공개 본문',locale:'ko',bodyFormat:'PLAIN_TEXT'}});
  expect(JSON.stringify(body)).not.toMatch(/internal reason|author|publisher|auditId|localizedBody|OWNER/);
  expect(await (await f.getAdmin('/v1/admin/notices/n1')).json()).toMatchObject({notice:{noticeId:'n1',revision:2,status:'PUBLISHED'},permissions:{canEdit:false,canPublish:false,canWithdraw:false}});
 });
 it('preserves published content while an operator edits a new draft',async()=>{
  const f=await fixture();await f.save();await f.publish();
  expect((await f.save('n1',{...f.document,title:'New draft'},2,'edit','OPERATOR')).status).toBe(200);
  expect(await (await f.detail()).json()).toMatchObject({notice:{revision:2,title:'TEST_ONLY public notice'}});
  expect((await f.publish('n1',3)).status).toBe(200);
  expect(await (await f.detail()).json()).toMatchObject({notice:{revision:4,title:'New draft',presentationRevision:1}});
 });
 it('withdraws publication without replayed commands republishing it',async()=>{
  const f=await fixture();await f.save();const published=await (await f.publish('n1',1,'publish')).json();
  expect((await f.command('n1',{command:'WITHDRAW',expectedRevision:2},'withdraw')).status).toBe(200);
  expect(await (await f.publish('n1',1,'publish')).json()).toEqual(published);
  expect((await f.detail()).status).toBe(404);
  expect(await (await f.read()).json()).toMatchObject({items:[],nextCursor:null});
 });
 it('enforces the same surface, visibility, environment, locale and target filters on list and direct ID',async()=>{
  const f=await fixture();
  const docs=[{surfaces:'APP'},{visibility:'INTERNAL'},{visibility:'AUTHENTICATED'},{channel:'INTERNAL'},{platform:'ANDROID'},{minVersionCode:5,maxVersionCode:7},{localizedBody:{en:'English only'}}];
  for(let i=0;i<docs.length;i++){const id=`hidden${i}`;await f.save(id,{...f.document,...docs[i]});await f.publish(id);expect((await f.detail(id)).status).toBe(404);}
  expect(await (await f.read()).json()).toMatchObject({items:[]});
  expect((await f.detail('hidden3','&channel=INTERNAL')).status).toBe(400);
  expect((await f.detail('hidden5','&versionCode=6')).status).toBe(200);
  expect((await f.detail('hidden5','&versionCode=8')).status).toBe(404);
  expect((await game.request('/v1/public/notices/hidden5?locale=ko&versionCode=6',{}, {...f.env,ENVIRONMENT:'DEV'})).status).toBe(401);
 });
 it('evaluates scheduled publication and exclusive end time using server time',async()=>{
  const f=await fixture();const start=Date.now()+60000;
  await f.save('scheduled',{...f.document,startsAt:start,endsAt:start+60000});
  expect((await f.command('scheduled',{command:'SCHEDULE',expectedRevision:1})).status).toBe(200);
  expect((await f.detail('scheduled')).status).toBe(404);
  // The persisted scheduled revision is evaluated without a scheduler transition.
  const {vi}=await import('vitest'); const clock=vi.spyOn(Date,'now');
  try {clock.mockReturnValue(start);expect((await f.detail('scheduled')).status).toBe(200);
   clock.mockReturnValue(start+60000);expect((await f.detail('scheduled')).status).toBe(404);
  } finally {clock.mockRestore();}
 });
 it('requires an active exact-version DEV tester and leaves other admission routes protected',async()=>{
  const f=await fixture('DEV');await f.save();await f.publish();
  const path='/v1/public/notices?locale=ko';const headers={...f.headers,'X-LastRail-App-Version-Code':'123'};
  expect((await f.read(path)).status).toBe(401);
  expect((await f.read(path,headers)).status).toBe(403);
  f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)",[Date.now()+60000]);
  expect((await f.read(path,headers)).status).toBe(200);
  expect((await f.read(path,{...headers,'X-LastRail-App-Version-Code':'124'})).status).toBe(403);
  f.sql.run("UPDATE account_tester_entitlements SET revoked_at=1");expect((await f.read(path,headers)).status).toBe(403);
  expect((await game.request('/v1/profile',{headers}, {...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'})).status).toBe(503);
  expect((await game.request('/v1/public/notices',{method:'POST'}, {...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'})).status).toBe(503);
 });
 it('rejects hostile or unsupported input without changing the original record',async()=>{
  const f=await fixture();
  for(const change of [{title:'<script>alert(1)</script>'},{action:{url:'javascript:alert(1)'}},{imageAssetId:'unapproved'},{presentationRevision:1.1},{startsAt:100,endsAt:99},{unknown:true},{title:'x'.repeat(20000)}]){
   expect((await f.save('invalid',{...f.document,...change})).status).toBe(400);
  }
  expect((await f.command('bad',{command:'SAVE_DRAFT',expectedRevision:null,document:f.document,environment:'DEV'})).status).toBe(400);
  expect((await f.read('/v1/public/notices?locale=ko&locale=en')).status).toBe(400);
  expect((await f.read('/v1/public/notices?locale=ko&cursor=bad')).status).toBe(400);
 });
 it('serializes competing edits and converges identical commands without duplicate audit',async()=>{
  const f=await fixture();const results=await Promise.all([f.save('n1',f.document,null,'same'),f.save('n1',f.document,null,'same')]);
  expect(results.map(r=>r.status)).toEqual([200,200]);expect(await results[0]!.json()).toEqual(await results[1]!.json());
  expect((await f.save('n1',{...f.document,title:'changed'},null,'same')).status).toBe(409);
  const competing=await Promise.all([f.publish('n1',1,'p1'),f.publish('n1',1,'p2')]);expect(competing.map(r=>r.status).sort()).toEqual([200,409]);
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE action='NOTICE_COMMAND' AND result='SUCCESS'")).toBe(2);
 });
 it('rolls back publication when audit fails and rechecks permission within the write batch',async()=>{
  const f=await fixture();await f.save();
  f.sql.run("CREATE TRIGGER test_notice_audit_failure BEFORE INSERT ON admin_audit_logs WHEN NEW.result='SUCCESS' BEGIN SELECT RAISE(ABORT,'TEST_AUDIT_FAILURE'); END");
  expect((await f.publish()).status).toBe(503);expect((await f.detail()).status).toBe(404);
  f.sql.run('DROP TRIGGER test_notice_audit_failure');
  f.sql.run("INSERT INTO admin_principals VALUES('backup-owner','backup-owner','OWNER','ACTIVE',NULL,0,0)");
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
  expect((await f.publish()).status).toBe(403);expect((await f.detail()).status).toBe(404);
 });
 it('binds pagination to query context and does not turn database failure into an empty success',async()=>{
  const f=await fixture();for(const id of ['a','b','c']){await f.save(id);await f.publish(id);}
  const first=await (await f.read('/v1/public/notices?locale=ko&limit=2')).json() as {items:{noticeId:string}[];nextCursor:string};
  expect(first.items.map(x=>x.noticeId)).toEqual(['a','b']);expect(first.nextCursor).toBeTruthy();
  const next=`&cursor=${encodeURIComponent(first.nextCursor)}`;
  expect(await (await f.read('/v1/public/notices?locale=ko&limit=2'+next)).json()).toMatchObject({items:[{noticeId:'c'}],nextCursor:null});
  expect((await f.read('/v1/public/notices?locale=en&limit=2'+next)).status).toBe(400);
  f.sql.run('DROP TABLE notice_heads');expect((await f.read()).status).toBe(503);
 });
});
describe('notice recovery and retained history',()=>{
 it('recovers a same-key commit between receipt lookup and head preflight',async()=>{
  const f=await fixture();const prepare=f.env.DB.prepare.bind(f.env.DB);let intercept=true;let winner:unknown;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT payload_hash,response_json FROM notice_command_receipts')&&intercept){
    const first=statement.first.bind(statement);
    statement.first=async <T>()=>{const result=await first<T>();if(intercept){intercept=false;const response=await f.save('n1',f.document,null,'same');expect(response.status).toBe(200);winner=await response.json();}return result;};
   }
   return statement;
  };
  expect(await (await f.save('n1',f.document,null,'same')).json()).toEqual(winner);
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE result='SUCCESS'")).toBe(1);
 });
 it('retains immutable source revisions and receipts through publication and withdrawal',async()=>{
  const f=await fixture();await f.save();await f.publish();
  await f.save('n1',{...f.document,title:'Unpublished draft',presentationRevision:2},2,'draft','OPERATOR');
  expect((await f.command('n1',{command:'WITHDRAW',expectedRevision:3})).status).toBe(200);
  expect((await f.detail()).status).toBe(404);
  expect(f.scalar('SELECT count(*) FROM notice_revisions')).toBe(4);
  expect(()=>f.sql.run("UPDATE notice_revisions SET document_json='{}' WHERE revision=1")).toThrow('notice_revision_immutable');
  expect(()=>f.sql.run('DELETE FROM notice_command_receipts')).toThrow('notice_receipt_immutable');
  expect(f.scalar('SELECT count(*) FROM notice_command_receipts')).toBe(4);
  expect(await (await f.getAdmin('/v1/admin/notices/n1')).json()).toMatchObject({notice:{revision:4,status:'DRAFT',document:{title:'Unpublished draft',presentationRevision:2}},publication:null});
  expect((await f.publish('n1',4)).status).toBe(200);
  expect(await (await f.detail()).json()).toMatchObject({notice:{title:'Unpublished draft',presentationRevision:2}});
 });
 it('does not let hidden notices consume public pages and supports exact locale without fallback',async()=>{
  const f=await fixture();
  for(const [id,change] of [['a',{visibility:'INTERNAL'}],['b',{}],['c',{surfaces:'APP'}],['d',{}]] as const){await f.save(id,{...f.document,...change});await f.publish(id);}
  const response=await f.read('/v1/public/notices?locale=en&limit=2');expect(response.status).toBe(200);
  expect(await response.json()).toMatchObject({items:[{noticeId:'b',body:'Public body'},{noticeId:'d',body:'Public body'}],nextCursor:null});
  expect((await f.detail('b','&channel=INTERNAL')).status).toBe(400);
  expect((await f.read('/v1/public/notices/b?locale=ja')).status).toBe(404);
 });
 it('rejects expired publication, scheduling in the past and decreasing presentation revision',async()=>{
  const f=await fixture();
  await f.save('expired',{...f.document,endsAt:1});expect((await f.publish('expired')).status).toBe(409);
  await f.save('past');expect((await f.command('past',{command:'SCHEDULE',expectedRevision:1})).status).toBe(409);
  await f.save('n1');await f.publish();
  expect((await f.save('n1',{...f.document,presentationRevision:2},2)).status).toBe(200);
  expect((await f.save('n1',f.document,3)).status).toBe(409);
  expect((await f.command('absent',{command:'WITHDRAW',expectedRevision:null})).status).toBe(409);
 });
 it('rejects expired testers and never exposes another environment publication',async()=>{
  const f=await fixture('DEV');await f.save();await f.publish();
  f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',0,NULL,'test',1,0,0)");
  const headers={...f.headers,'X-LastRail-App-Version-Code':'123'};
  expect((await f.read('/v1/public/notices?locale=ko',headers)).status).toBe(403);
  const publicRead=await game.request('/v1/public/notices?locale=ko',{}, {...f.env,ENVIRONMENT:'PRD',CLIENT_ADMISSION_MODE:'ENFORCED'});
  expect(publicRead.status).toBe(200);expect(await publicRead.json()).toMatchObject({items:[]});
 });
});