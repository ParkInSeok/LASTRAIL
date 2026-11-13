import {describe,it,expect} from 'vitest';
import app from '../../apps/admin-api/src/index';
import {setup} from '../helpers/departure-fixture';

async function fixture(){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run('INSERT INTO admin_principals VALUES(?,?,?,\'ACTIVE\',NULL,0,0)',[role,role,role]);
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const read=(role='OWNER')=>app.request('/v1/admin/runtime-policy',{headers:{'Cf-Access-Subject':role}},env);
 const body={environment:'DEV',adsEnabled:false,expectedRuntimePolicyRevision:null,reason:'TEST_ONLY disable advertising'};
 const save=(input:unknown=body,key='save',role='OWNER')=>app.request('/v1/admin/runtime-policy',{method:'POST',headers:{'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify(input)},env);
 return {...f,env,read,save,body};
}
describe('admin runtime policy CAS and audit',()=>{
 it('rejects oversized and unknown-field input as client errors without mutation',async()=>{
  const f=await fixture();
  for(const input of [{...f.body,reason:'x'.repeat(18001)},{...f.body,ready:true}]){
   const response=await f.save(input);expect(response.status).toBe(400);
   expect(await response.json()).toEqual({error:'contract_violation'});
   expect(response.headers.get('Cache-Control')).toBe('no-store');
  }
  expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
 });
 it('disables an existing ON policy and exposes its before/after audit through the existing audit API',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO ad_runtime_policies VALUES('DEV',7,1)");
  const response=await f.save({...f.body,expectedRuntimePolicyRevision:7});expect(response.status).toBe(200);
  const saved=await response.json() as {auditId:string;requestId:string};
  const audit=await app.request('/v1/admin/audit?action=RUNTIME_POLICY_UPDATE&resourceId=DEV&result=SUCCESS',{headers:{'Cf-Access-Subject':'VIEWER'}},f.env);
  expect(audit.status).toBe(200);
  expect(await audit.json()).toMatchObject({items:[{id:saved.auditId,requestId:saved.requestId,permission:'RUNTIME_POLICY_WRITE',
   changeSummary:{reason:f.body.reason,before:{adsEnabled:true,runtimePolicyRevision:7},after:{adsEnabled:false,runtimePolicyRevision:8}}}]});
  expect(await (await f.read()).json()).toMatchObject({adsEnabled:false,runtimePolicyRevision:8});
 });
 it('reuses Admin authentication and exposes role-specific mutation capability',async()=>{
  const f=await fixture();
  expect((await app.request('/v1/admin/runtime-policy',{},f.env)).status).toBe(401);
  for(const role of ['OWNER','OPERATOR','VIEWER']){
   const response=await f.read(role);expect(response.status).toBe(200);
   expect(await response.json()).toMatchObject({contractVersion:'admin-runtime-policy-v1',environment:'DEV',policyStatus:'UNKNOWN',adsEnabled:null,runtimePolicyRevision:null,permission:'RUNTIME_POLICY_READ',canWrite:role==='OWNER',canEnable:false});
  }
  for(const role of ['OPERATOR','VIEWER'])expect((await f.save(f.body,'denied',role)).status).toBe(403);
  expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
 });
 it('creates UNKNOWN with expected null, records reason and restores the exact response after later edits',async()=>{
  const f=await fixture();const first=await f.save();expect(first.status).toBe(200);const saved=await first.json();
  expect(saved).toMatchObject({accepted:true,adsEnabled:false,runtimePolicyRevision:1});
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE result='SUCCESS'")).toBe(1);
  const edit={...f.body,expectedRuntimePolicyRevision:1,reason:'TEST_ONLY repeat OFF'};
  expect((await f.save(edit,'edit')).status).toBe(200);
  expect(await (await f.save()).json()).toEqual(saved);
  expect(await (await f.read()).json()).toMatchObject({runtimePolicyRevision:2});
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE result='SUCCESS'")).toBe(2);
 });
 it('rejects environment confusion, stale revision, changed request reuse and unverified ON',async()=>{
  const f=await fixture();expect((await f.save({...f.body,environment:'PRD'})).status).toBe(400);
  expect((await f.save()).status).toBe(200);
  const stale=await f.save(f.body,'stale');expect(stale.status).toBe(409);expect(await stale.json()).toMatchObject({error:'runtime_policy_revision_conflict'});
  const conflict=await f.save({...f.body,reason:'changed'},'save');expect(conflict.status).toBe(409);expect(await conflict.json()).toMatchObject({error:'idempotency_conflict'});
  const on=await f.save({...f.body,expectedRuntimePolicyRevision:1,adsEnabled:true},'on');expect(on.status).toBe(503);expect(await on.json()).toMatchObject({error:'ad_enable_readiness_unavailable'});
  expect(f.scalar('SELECT revision FROM ad_runtime_policies')).toBe(1);
 });
 it('rolls back policy and request receipt if its success audit cannot be saved',async()=>{
  const f=await fixture();f.sql.run("CREATE TRIGGER test_audit_failure BEFORE INSERT ON admin_audit_logs WHEN NEW.result='SUCCESS' BEGIN SELECT RAISE(ABORT,'TEST_AUDIT_FAILURE'); END;");
  expect((await f.save()).status).toBe(503);
  expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM admin_runtime_policy_receipts')).toBe(0);
  f.sql.run('DROP TRIGGER test_audit_failure');expect((await f.save()).status).toBe(200);
 });
 it('serializes concurrent writes and converges same-key retries',async()=>{
  const f=await fixture();const results=await Promise.all([f.save(),f.save()]);expect(results.map(r=>r.status)).toEqual([200,200]);
  expect(await results[0]!.json()).toEqual(await results[1]!.json());
  const input={...f.body,expectedRuntimePolicyRevision:1};
  const contenders=await Promise.all([f.save(input,'a'),f.save(input,'b')]);expect(contenders.map(r=>r.status).sort()).toEqual([200,409]);
  expect(f.scalar('SELECT revision FROM ad_runtime_policies')).toBe(2);
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE result='SUCCESS'")).toBe(2);
 });
 it('recovers a same-key commit between receipt lookup and policy preflight',async()=>{
  const f=await fixture();const prepare=f.env.DB.prepare.bind(f.env.DB);let intercept=true;let winner:unknown;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT payload_hash,response_json')&&intercept){
    const first=statement.first.bind(statement);
    statement.first=async <T>()=>{
     const result=await first<T>();
     if(intercept){intercept=false;const committed=await f.save();expect(committed.status).toBe(200);winner=await committed.json();}
     return result;
    };
   }
   return statement;
  };
  const retry=await f.save();expect(retry.status).toBe(200);expect(await retry.json()).toEqual(winner);
  expect(f.scalar('SELECT count(*) FROM admin_audit_logs')).toBe(1);
 });
 it('rechecks OWNER and revision inside the atomic batch',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO admin_principals VALUES('other-owner','other-owner','OWNER','ACTIVE',NULL,0,0)");
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
  expect((await f.save()).status).toBe(403);expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
 });
});

describe('admin ad publication readiness',()=>{
 const path='/v1/admin/runtime-policy/disclosure-readiness';
 async function readyFixture(){
  const f=await fixture();
  const policy={version:'READY_OFFLINE',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000]};
  const read=(role='OWNER',suffix='')=>app.request(path+suffix,{headers:{'Cf-Access-Subject':role}},f.env);
  const seed=()=>f.sql.run("INSERT INTO offline_ad_policies VALUES('READY_OFFLINE','DEV','ACTIVE',?,0,'PRIVATE_APPROVER')",[JSON.stringify(policy)]);
  const publish=async()=>{
   const r=await app.request('/v1/admin/probability-disclosures/source?consumer=OFFLINE&policyVersion=READY_OFFLINE',{headers:{'Cf-Access-Subject':'OWNER'}},f.env);
   expect(r.status).toBe(200);const source=await r.json() as {sourceDigest:string;disclosureId:string};
   const posted=await app.request('/v1/admin/probability-disclosures',{method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':'publish'},body:JSON.stringify({environment:'DEV',consumer:'OFFLINE',policyVersion:policy.version,expectedSourceDigest:source.sourceDigest,reason:'PRIVATE_REASON'})},f.env);
   expect(posted.status).toBe(200);return source;
  };
  return {...f,readiness:read,policy,seed,publish};
 }
 it('requires Admin auth and lets all existing read roles inspect explicit missing policies',async()=>{
  const f=await readyFixture();
  expect((await app.request(path,{},f.env)).status).toBe(401);
  for(const role of ['OWNER','OPERATOR','VIEWER']){
   const r=await f.readiness(role);expect(r.status).toBe(200);expect(r.headers.get('Cache-Control')).toBe('no-store');
   expect(await r.json()).toMatchObject({contractVersion:'admin-ad-disclosure-readiness-v1',scope:'PUBLICATION_ONLY',environment:'DEV',canEnable:false,items:[{consumer:'OFFLINE',state:'NO_ACTIVE_POLICY',disclosureId:null},{consumer:'COMBAT',state:'NO_ACTIVE_POLICY',disclosureId:null}]});
  }
 });
 it('reports unpublished then validated published policy without granting enable authority',async()=>{
  const f=await readyFixture();f.seed();
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'UNPUBLISHED',policyVersion:'READY_OFFLINE'},{state:'NO_ACTIVE_POLICY'}]});
  const source=await f.publish();
  const r=await f.readiness();const body=await r.json();
  expect(body).toMatchObject({canEnable:false,enableBlockReason:'ad_enable_readiness_unavailable',items:[{state:'PUBLISHED',sourceDigest:source.sourceDigest,disclosureId:source.disclosureId,publicPath:'/v1/public/probability-disclosures/'+source.disclosureId},{state:'NO_ACTIVE_POLICY'}]});
  expect(JSON.stringify(body)).not.toMatch(/PRIVATE_|config_json|adUnit|appId|reason|actorId|auditId/);
  const on=await f.save({...f.body,adsEnabled:true},'on');expect(on.status).toBe(503);
  expect((await f.save({...f.body,adsEnabled:true,ready:true},'override')).status).toBe(400);
  expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
 });
 it('does not treat old published source as the current changed or retired policy',async()=>{
  const f=await readyFixture();f.seed();await f.publish();
  f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify({...f.policy,weightsBps:[5000,5000,0]})]);
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'UNPUBLISHED'},{state:'NO_ACTIVE_POLICY'}]});
  f.sql.run("UPDATE offline_ad_policies SET status='RETIRED'");
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'NO_ACTIVE_POLICY'},{state:'NO_ACTIVE_POLICY'}]});
 });
 it('distinguishes invalid source and corrupted publication',async()=>{
  const f=await readyFixture();f.seed();await f.publish();
  f.sql.run('DROP TRIGGER probability_disclosure_no_update');f.sql.run("UPDATE probability_disclosures SET payload_json='{}'");
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'INVALID_DISCLOSURE',publicPath:null},{state:'NO_ACTIVE_POLICY'}]});
  f.sql.run("UPDATE offline_ad_policies SET config_json='{}'");
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'INVALID_SOURCE',publicPath:null},{state:'NO_ACTIVE_POLICY'}]});
 });
 it('represents storage unavailability explicitly and leaves OFF writes usable',async()=>{
  const f=await readyFixture();f.sql.run('DROP TABLE offline_ad_policies');
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'UNAVAILABLE',publicPath:null},{state:'NO_ACTIVE_POLICY'}]});
  expect((await f.save()).status).toBe(200);
 });
 it('excludes another environment and rejects query/body overrides',async()=>{
  const f=await readyFixture();f.seed();await f.publish();
  const r=await app.request(path,{headers:{'Cf-Access-Subject':'OWNER'}},{...f.env,ENVIRONMENT:'PRD'});
  expect(r.status).toBe(200);expect(await r.json()).toMatchObject({environment:'PRD',items:[{state:'NO_ACTIVE_POLICY'},{state:'NO_ACTIVE_POLICY'}]});
  for(const suffix of ['?environment=PRD','?ready=true','?consumer=OFFLINE'])expect((await f.readiness('OWNER',suffix)).status).toBe(400);
 });

 it('detects source changes during inspection instead of reporting the old publication as current',async()=>{
  const f=await readyFixture();f.seed();await f.publish();const prepare=f.env.DB.prepare.bind(f.env.DB);let intercept=true;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT disclosure_id,payload_json,payload_digest FROM probability_disclosures')&&intercept){
    const first=statement.first.bind(statement);
    statement.first=async <T>()=>{const row=await first<T>();if(intercept){intercept=false;f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify({...f.policy,weightsBps:[5000,5000,0]})]);}return row;};
   }
   return statement;
  };
  expect(await (await f.readiness()).json()).toMatchObject({items:[{state:'SOURCE_CHANGED',publicPath:null},{state:'NO_ACTIVE_POLICY'}]});
 });
});
