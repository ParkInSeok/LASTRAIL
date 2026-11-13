import {it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {UnityBuildManagement} from '../../apps/admin-api/src/unity-build-management';
import {UnityBuildRunnerCoordinator} from '../../apps/admin-api/src/unity-build-runner';
import app from '../../apps/admin-api/src/index';
const actor={id:'OWNER',role:'OWNER' as const};
const profile={id:'SYNTHETIC_NOT_OPERATING',platform:'SYNTHETIC',environment:'DEV' as const,unityVersion:'TEST_ONLY',configurationHash:'a'.repeat(64),dependencyLockHash:'b'.repeat(64)};
const request={environment:'DEV',profileId:profile.id,platform:profile.platform,commit:'c'.repeat(40),contentVersion:'test-content',appVersion:'1.0.0',buildNumber:11,note:'Synthetic management test only'};
async function fixture(){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 return {...f,service:new UnityBuildManagement(f.env.DB,'DEV',[profile])};
}
it('defaults to no approved profile or runner and never creates a fake job',async()=>{
 const f=await fixture();try{
  const service=new UnityBuildManagement(f.env.DB,'DEV');
  expect(service.capabilities()).toMatchObject({readiness:'NOT_READY',runnerReady:false,profiles:[]});
  await expect(service.request(actor,request,'key')).rejects.toThrow('unity_build_profile_not_ready');
  expect(f.scalar('SELECT count(*) FROM admin_jobs')).toBe(0);
 }finally{f.sql.close();}
});
it('actual admin routes authenticate, deny VIEWER writes and keep unconfigured builds/logs/artifacts unavailable',async()=>{
 const f=await fixture();try{
  const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
  expect((await app.request('/v1/admin/unity-builds/capabilities',{},env)).status).toBe(401);
  for(const role of ['OWNER','OPERATOR','VIEWER']){
   const headers={'Cf-Access-Subject':role};
   const r=await app.request('/v1/admin/unity-builds/capabilities',{headers},env);
   expect(r.status).toBe(200);expect(r.headers.get('Cache-Control')).toBe('no-store');
   expect(await r.json()).toMatchObject({runnerReady:false,profiles:[],canExecute:false});
   const post=await app.request('/v1/admin/unity-builds',{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':'actual-test'},body:JSON.stringify(request)},env);
   expect(post.status).toBe(role==='VIEWER'?403:409);
  }
  const job=(await f.service.request(actor,request,'internal-fixture')).job;
  const headers={'Cf-Access-Subject':'VIEWER'};
  const logs=await app.request(`/v1/admin/unity-builds/${job.jobId}/logs?after=0`,{headers},env);
  expect(await logs.json()).toMatchObject({status:'UNAVAILABLE',entries:[]});
  expect((await app.request(`/v1/admin/unity-builds/${job.jobId}/artifacts/output/download`,{headers},env)).status).toBe(503);
  expect((await app.request(`/v1/admin/unity-builds/${job.jobId}/logs?after=-1`,{headers},env)).status).toBe(400);
  expect((await new UnityBuildManagement(f.env.DB,'PRD').read(job.jobId).catch(e=>e)).code).toBe('unity_build_not_found');
 }finally{f.sql.close();}
});
it('permits current OPERATOR but refuses VIEWER, revoked identity and a permission change before commit',async()=>{
 const f=await fixture();try{
  const operator={id:'OPERATOR',role:'OPERATOR' as const};
  await expect(f.service.request({id:'VIEWER',role:'VIEWER'},request,'denied')).rejects.toThrow('admin_permission_denied');
  await f.service.request(operator,request,'operator');
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='OPERATOR'");
  await expect(f.service.request(operator,request,'operator')).rejects.toThrow('admin_permission_denied');
  f.sql.run("UPDATE admin_principals SET status='ACTIVE' WHERE id='OPERATOR'");
  const originalBatch=f.env.DB.batch.bind(f.env.DB);
  f.env.DB.batch=async statements=>{
   f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OPERATOR'");
   return originalBatch(statements);
  };
  await expect(f.service.request(operator,{...request,buildNumber:99},'race')).rejects.toMatchObject({status:403,code:'admin_permission_denied'});
  expect(f.scalar('SELECT count(*) FROM admin_jobs')).toBe(1);
 }finally{f.sql.close();}
});
it('same original key creates one pending job, reserves the number, and replays the original result after cancellation',async()=>{
 const f=await fixture();try{
  const result=await f.service.request(actor,request,'key');
  expect(result.job).toMatchObject({status:'PENDING',stage:'WAITING_FOR_RUNNER',runnerReady:false});
  expect(await f.service.request(actor,request,'key')).toEqual(result);
  await expect(f.service.request(actor,{...request,note:'changed'},'key')).rejects.toThrow('idempotency_conflict');
  await expect(f.service.request(actor,request,'another')).rejects.toThrow('unity_build_number_conflict');
  const cancelled=await f.service.cancel(actor,result.job.jobId,{expectedUpdatedAt:result.job.updatedAt,reason:'cancel synthetic'},'cancel');
  expect(cancelled.job.status).toBe('CANCELLED');
  expect(await f.service.request(actor,request,'key')).toEqual(result);
  expect(await f.service.cancel(actor,result.job.jobId,{expectedUpdatedAt:result.job.updatedAt,reason:'cancel synthetic'},'cancel')).toEqual(cancelled);
  expect(f.scalar('SELECT count(*) FROM admin_jobs')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM admin_audit_logs')).toBe(2);
 }finally{f.sql.close();}
});
it('refuses leased cancellation and rolls back allocation if audit fails',async()=>{
 const f=await fixture();try{
  f.sql.run("CREATE TRIGGER no_build_audit BEFORE INSERT ON admin_audit_logs BEGIN SELECT RAISE(ABORT,'test'); END");
  await expect(f.service.request(actor,request,'key')).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM admin_jobs')).toBe(0);
  f.sql.run('DROP TRIGGER no_build_audit');
  const r=await f.service.request(actor,request,'key');
  f.sql.run("UPDATE admin_jobs SET lease_owner='synthetic-runner'");
  await expect(f.service.cancel(actor,r.job.jobId,{expectedUpdatedAt:r.job.updatedAt,reason:'stop'},'cancel')).rejects.toThrow('unity_build_cancel_requires_runner');
  expect(f.scalar('SELECT status FROM admin_jobs')).toBe('PENDING');
 }finally{f.sql.close();}
});
it('fences one runner attempt, preserves disconnected work and refuses unverified success',async()=>{
 const f=await fixture();try{
  const job=(await f.service.request(actor,request,'key')).job;
  const other=(await f.service.request(actor,{...request,buildNumber:12},'other')).job;
  const runner=new UnityBuildRunnerCoordinator(f.env.DB,'DEV');
  await expect(runner.claim('test-runner',job.jobId,'attempt',Date.now()+60000)).rejects.toThrow('unity_build_runner_not_ready');
  // Synthetic server-owned readiness evidence only; no process is launched.
  const owner=new UnityBuildRunnerCoordinator(f.env.DB,'DEV',{prepare:async()=>[f.env.DB.prepare('INSERT INTO unity_build_write_guard VALUES(1)')]});
  const claimed=await owner.claim('test-runner',job.jobId,'attempt',Date.now()+60000);
  expect(claimed.stage).toBe('PREFLIGHT');
  await expect(owner.claim('test-runner',other.jobId,'attempt-other',Date.now()+60000)).rejects.toThrow('unity_build_runner_busy');
  const unknown=await owner.report('test-runner','attempt','disconnect',{expectedRevision:1,kind:'CONNECTION_LOST'});
  expect(unknown.stage).toBe('RESULT_UNKNOWN');
  expect(await owner.claim('test-runner',job.jobId,'attempt',Date.now()+60000)).toEqual(unknown);
  await expect(owner.report('another-runner','attempt','fake',{expectedRevision:2,kind:'RESUME'})).rejects.toThrow('unity_build_attempt_conflict');
  const resumed=await owner.report('test-runner','attempt','resume',{expectedRevision:2,kind:'RESUME'});
  expect(resumed.stage).toBe('PREFLIGHT');
  await expect(owner.report('test-runner','attempt','failure',{expectedRevision:3,kind:'FAILED'})).rejects.toThrow('unity_build_result_evidence_unavailable');
  expect((await f.service.read(job.jobId)).job.status).toBe('RUNNING');
  await expect(owner.claim('test-runner',other.jobId,'still-busy',Date.now()+60000)).rejects.toThrow('unity_build_runner_busy');
  await expect(owner.report('test-runner','attempt','stale',{expectedRevision:1,kind:'FAILED'})).rejects.toThrow('unity_build_revision_conflict');
  await expect(owner.report('test-runner','attempt','success',{expectedRevision:3,kind:'ARTIFACTS_VERIFIED'})).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM unity_build_attempts')).toBe(1);
 }finally{f.sql.close();}
});
it('checks creator authority at execution and fences revocation during readiness capture',async()=>{
 const f=await fixture();try{
  const operator={id:'OPERATOR',role:'OPERATOR' as const};
  const job=(await f.service.request(operator,request,'job')).job;
  const proof=()=>[f.env.DB.prepare('INSERT INTO unity_build_write_guard VALUES(1)')];
  const runner=new UnityBuildRunnerCoordinator(f.env.DB,'DEV',{prepare:async()=>proof()});
  f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OPERATOR'");
  await expect(runner.claim('runner',job.jobId,'attempt',Date.now()+60000)).rejects.toThrow('unity_build_creator_permission_revoked');
  f.sql.run("UPDATE admin_principals SET role='OPERATOR' WHERE id='OPERATOR'");
  const racing=new UnityBuildRunnerCoordinator(f.env.DB,'DEV',{prepare:async()=>{
   f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='OPERATOR'");return proof();
  }});
  await expect(racing.claim('runner',job.jobId,'attempt',Date.now()+60000)).rejects.toThrow('unity_build_creator_permission_revoked');
  expect(f.scalar('SELECT count(*) FROM unity_build_attempts')).toBe(0);
  expect((await f.service.read(job.jobId)).job.status).toBe('PENDING');
 }finally{f.sql.close();}
});
it('recovers exact winning claim, runner report and cancel receipts across deterministic commit races',async()=>{
 const f=await fixture();try{
  const job=(await f.service.request(actor,request,'job')).job;
  const runner=new UnityBuildRunnerCoordinator(f.env.DB,'DEV',{prepare:async()=>[f.env.DB.prepare('INSERT INTO unity_build_write_guard VALUES(1)')]});
  const originalBatch=f.env.DB.batch.bind(f.env.DB),expires=Date.now()+60000;
  let once=true,winner:unknown;
  f.env.DB.batch=async statements=>{
   if(once){once=false;winner=await runner.claim('runner',job.jobId,'attempt',expires);}
   return originalBatch(statements);
  };
  expect(await runner.claim('runner',job.jobId,'attempt',expires)).toEqual(winner);
  once=true;const command={expectedRevision:1,kind:'PREFLIGHT_PASSED'};
  f.env.DB.batch=async statements=>{
   if(once){once=false;winner=await runner.report('runner','attempt','step',command);}
   return originalBatch(statements);
  };
  expect(await runner.report('runner','attempt','step',command)).toEqual(winner);
  f.env.DB.batch=originalBatch;
  const other=(await f.service.request(actor,{...request,buildNumber:22},'other')).job;
  once=true;const cancel={expectedUpdatedAt:other.updatedAt,reason:'race'};
  f.env.DB.batch=async statements=>{
   if(once){once=false;winner=await f.service.cancel(actor,other.jobId,cancel,'cancel');}
   return originalBatch(statements);
  };
  expect(await f.service.cancel(actor,other.jobId,cancel,'cancel')).toEqual(winner);
  expect(f.scalar('SELECT count(*) FROM unity_build_attempts')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM unity_build_runner_receipts')).toBe(1);
 }finally{f.sql.close();}
});
it('recovers artifact registration without rebuilding and distinguishes requested cancellation from verified stop',async()=>{
 const f=await fixture();try{
  const proof=()=>[f.env.DB.prepare('INSERT INTO unity_build_write_guard VALUES(1)')];
  const owner=new UnityBuildRunnerCoordinator(f.env.DB,'DEV',{prepare:async()=>proof(),verify:async()=>proof()});
  const job=(await f.service.request(actor,request,'key')).job;
  await owner.claim('synthetic-runner',job.jobId,'attempt',Date.now()+60000);
  await owner.report('synthetic-runner','attempt','preflight',{expectedRevision:1,kind:'PREFLIGHT_PASSED'});
  await owner.report('synthetic-runner','attempt','source',{expectedRevision:2,kind:'SOURCE_READY'});
  await owner.report('synthetic-runner','attempt','build',{expectedRevision:3,kind:'BUILD_SUCCEEDED',evidenceRef:'synthetic-original-proof'});
  await owner.report('synthetic-runner','attempt','upload-fail',{expectedRevision:4,kind:'REGISTRATION_FAILED'});
  expect((await f.service.read(job.jobId)).job.stage).toBe('REGISTRATION_RECOVERY_REQUIRED');
  const done=await owner.report('synthetic-runner','attempt','uploaded',{expectedRevision:5,kind:'ARTIFACTS_VERIFIED',evidenceRef:'synthetic-artifact-proof'});
  expect(done.stage).toBe('SUCCEEDED');
  expect(await owner.report('synthetic-runner','attempt','uploaded',{expectedRevision:5,kind:'ARTIFACTS_VERIFIED',evidenceRef:'synthetic-artifact-proof'})).toEqual(done);
  const other=(await f.service.request(actor,{...request,buildNumber:22},'next')).job;
  await owner.claim('synthetic-runner',other.jobId,'attempt2',Date.now()+60000);
  const current=(await f.service.read(other.jobId)).job;
  const requested=await f.service.cancel(actor,other.jobId,{expectedUpdatedAt:current.updatedAt,reason:'stop synthetic'},'stop');
  expect(requested.job).toMatchObject({status:'RUNNING',cancelRequested:true});
  expect((await owner.report('synthetic-runner','attempt2','stopped',{expectedRevision:2,kind:'CANCEL_CONFIRMED',evidenceRef:'synthetic-stopped-proof'})).stage).toBe('CANCELLED');
  expect(f.scalar('SELECT count(*) FROM unity_build_attempts')).toBe(2);
 }finally{f.sql.close();}
});
