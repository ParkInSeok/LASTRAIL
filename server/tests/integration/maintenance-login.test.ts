import {describe,expect,it} from 'vitest';
import game from '../../apps/game-api/src/index';
import {hashPassword} from '../../apps/game-api/src/password-auth';
import {hashRefreshToken} from '../../apps/game-api/src/auth';
import {MaintenancePlanStore} from '../../apps/admin-api/src/maintenance-plan-store';
import {MaintenanceRuntimePolicyStore} from '../../apps/admin-api/src/maintenance-runtime-policy-store';
import {setup} from '../helpers/departure-fixture';

const origin='https://dev.lastrail.app',password='TestOnlyStrongPassword-123!';
async function fixture(){
 const f=await setup(false,true,null),record=await hashPassword(password);
 f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,
  password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,
  [record.passwordHash,record.algorithm,record.paramsJson,'tester@example.invalid']);
 const env={...f.env,WEB_ALLOWED_ORIGIN:origin};
 const actor={id:'maintenance_test',role:'OWNER' as const};
 const plan=new MaintenancePlanStore(f.env.DB,'DEV'),store=new MaintenanceRuntimePolicyStore(f.env.DB,{environment:'DEV',applicationId:'LASTRAIL'});
 const activate=async(scope:'GAME'|'GAME_AND_WEB'='GAME_AND_WEB',future=false)=>{
  const now=Date.now();
  await plan.command(actor,'login-maint',{command:'SAVE_DRAFT',environment:'DEV',expectedDraftRevision:null,reason:'TEST_ONLY',plan:{applicationId:'LASTRAIL',
   serviceScope:scope,webMode:scope==='GAME'?null:'READ_ONLY',title:'Update',summary:'Test maintenance',noticeId:null,
   drainStartsAt:future?now-1000:null,startsAt:future?now+60000:now-1000,estimatedEndAt:future?now+120000:now-500,
   existingRunPolicy:future?'DRAIN_THEN_PRESERVE':'EMERGENCY_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15}},'login-plan');
  await store.transition(actor,{command:future?'SCHEDULE':'ACTIVATE',expectedRevision:1,maintenanceId:'login-maint',draftRevision:1},'TEST_ONLY','login-maint');
 };
 const login=(web=false)=>game.request(web?'/v1/web/sessions/password':'/v1/auth/password/login',{method:'POST',headers:{'Content-Type':'application/json',Origin:origin},
  body:JSON.stringify({loginId:'tester',password})},env);
 return {...f,env,activate,login};
}
describe('maintenance login commit boundary',()=>{
 it('blocks both login channels during an update without revoking existing sessions',async()=>{
  const f=await fixture();const web=await f.login(true);expect(web.status).toBe(200);
  await f.activate();
  for(const isWeb of [false,true]){
   const response=await f.login(isWeb);expect(response.status).toBe(503);
   expect(await response.json()).toMatchObject({error:'maintenance_login_blocked'});
  }
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
  expect(f.scalar('SELECT COUNT(*) FROM web_sessions WHERE revoked_at IS NULL')).toBe(1);
 });
 it.each([false,true])('rejects a login overtaken by committed maintenance (web=%s)',async isWeb=>{
  const f=await fixture();
  const batch=f.adapter.batch.bind(f.adapter);
  f.adapter.batch=async statements=>{f.adapter.batch=batch;await f.activate();return batch(statements);};
  const response=await f.login(isWeb);expect(response.status).toBe(503);
  expect(await response.json()).toMatchObject({error:'maintenance_login_blocked'});
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
  expect(f.scalar('SELECT COUNT(*) FROM web_sessions')).toBe(0);
 });
 it('allows logins during DRAINING but full GAME maintenance also blocks new WEB login',async()=>{
  const scheduled=await fixture();await scheduled.activate('GAME_AND_WEB',true);
  expect((await scheduled.login()).status).toBe(200);expect((await scheduled.login(true)).status).toBe(200);
  const independent=await fixture();await independent.activate('GAME');
  expect((await independent.login()).status).toBe(503);expect((await independent.login(true)).status).toBe(503);
 });
 it('guards guest/Google/refresh issuance and preserves committed replay results',async()=>{
  const f=await fixture();
  const refreshToken='test-only-refresh-token-maintenance';
  f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'",[await hashRefreshToken(refreshToken)]);
  f.sql.run("INSERT INTO auth_identities(id,account_id,provider,provider_subject,linked_at) VALUES('google-maint','a','GOOGLE','google-test',0)");
  const env={...f.env,GOOGLE_AUTH_AUDIENCES:'test-only-audience',WEB_GOOGLE_AUTH_AUDIENCES:'test-only-audience',
   GOOGLE_ID_TOKEN_VERIFIER:{verifyIdToken:async()=>({subject:'google-test',audiences:['test-only-audience'],expiresAt:Date.now()+60000})}};
  const post=(path:string,body:object,key?:string)=>game.request(path,{method:'POST',headers:{'Content-Type':'application/json',Origin:origin,...(key?{'Idempotency-Key':key}:{})},body:JSON.stringify(body)},env);
  const guest=await post('/v1/auth/guest',{},'guest-maintenance-original');expect(guest.status).toBe(200);
  const guestBody=await guest.json();
  await f.activate();
  for(const [path,body] of [['/v1/auth/guest',{}],['/v1/auth/google',{idToken:'TEST_ONLY'}],['/v1/web/sessions/google',{idToken:'TEST_ONLY'}],['/v1/auth/refresh',{refreshToken}]] as const){
   const response=await post(path,body);expect(response.status).toBe(503);expect(await response.json()).toMatchObject({error:'maintenance_login_blocked'});
  }
  expect((await post('/v1/auth/guest',{},'guest-maintenance-original')).status).toBe(200);
  expect(await (await post('/v1/auth/guest',{},'guest-maintenance-original')).json()).toMatchObject({account:guestBody.account});
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
  expect(f.scalar('SELECT COUNT(*) FROM web_sessions')).toBe(0);
 });
 it('classifies a missing login policy table as unavailable, not confirmed maintenance',async()=>{
  const f=await fixture();f.sql.run('DROP TABLE maintenance_admission_guard');
  const response=await f.login();expect(response.status).toBe(503);
  expect(await response.json()).toMatchObject({error:'maintenance_policy_unavailable',confirmed:false});
  expect((await game.request('/v1/public/maintenance/login',{},f.env)).status).toBe(503);
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
 });
 it.each(['title','webMode','existingRunPolicy','statusFreshnessSeconds','unknown','unicodeTitle','emptyTitle','controlTitle','nulTitle','estimatedEndAt'])('rejects restored malformed source consistently (%s)',async field=>{
  const f=await fixture();await f.activate();
  const document=JSON.parse(String(f.scalar("SELECT document_json FROM maintenance_plan_revisions WHERE maintenance_id='login-maint'")));
  if(field==='title')delete document.title;
  else if(field==='unknown')document.unknown=true;
  else if(field==='unicodeTitle')document.title='😀'.repeat(101);
  else if(field==='emptyTitle')document.title='\uFEFF\u3000';
  else if(field==='controlTitle')document.title='Invalid\u0001';
  else if(field==='nulTitle')document.title='Invalid\u0000hidden';
  else if(field==='estimatedEndAt')document.estimatedEndAt=document.startsAt;
  else document[field]=field==='statusFreshnessSeconds'?0:null;
  // Simulate a corrupted restored source; ordinary immutable command paths cannot do this.
  f.sql.run('DROP TRIGGER maintenance_plan_revision_no_update');
  f.sql.run("UPDATE maintenance_plan_revisions SET document_json=? WHERE maintenance_id='login-maint'",[JSON.stringify(document)]);
  for(const web of [false,true]){
   const response=await f.login(web);expect(response.status).toBe(503);
   expect(await response.json()).toMatchObject({error:'maintenance_policy_unavailable',confirmed:false});
  }
  expect((await game.request('/v1/public/maintenance/login',{},f.env)).status).toBe(503);
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
  expect(f.scalar('SELECT COUNT(*) FROM web_sessions')).toBe(0);
 });
 it('offers safe unauthenticated status and requires confirmed release rather than estimated time',async()=>{
  const f=await fixture();await f.activate();
  const response=await game.request('/v1/public/maintenance/login',{}, {...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'});
  expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');
  const body=await response.json();
  expect(body).toMatchObject({contractVersion:'maintenance-login-v1',gameLoginAllowed:false,webLoginAllowed:false,title:'Update',retryAfterSeconds:15});
  expect(JSON.stringify(body)).not.toContain('maintenance_test');expect(JSON.stringify(body)).not.toContain('TEST_ONLY');
  const store=new MaintenanceRuntimePolicyStore(f.env.DB,{environment:'DEV',applicationId:'LASTRAIL'});
  await store.transition({id:'maintenance_test',role:'OWNER'},{command:'COMPLETE',expectedRevision:2},'TEST_ONLY','complete');
  expect(await (await game.request('/v1/public/maintenance/login',{},f.env)).json()).toMatchObject({gameLoginAllowed:true,webLoginAllowed:true,policyRevision:3});
  expect((await game.request('/v1/public/maintenance/login?environment=PRD',{},f.env)).status).toBe(400);
 });
});
