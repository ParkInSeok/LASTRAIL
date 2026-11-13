import {canonicalJson,sha256Hex} from '@lastrail/common';
import {describe,it,expect} from 'vitest';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
async function fixture(environment='PRD'){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 const env={...f.env,ENVIRONMENT:environment,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const policy={version:'TEST_OFFLINE',environment,placement:'OFFLINE_SUPPLY',weightsBps:[1,0,9999],challengeLifetimeMs:60000,callbackGraceMs:60000,maxAdsPerDay:5,dayStartUtcMinutes:0,adUnit:'PRIVATE_AD_UNIT',appId:'PRIVATE_APP_ID',rewardItem:'PRIVATE_PROVIDER_ITEM',rewardAmount:'1'};
 f.sql.run("INSERT INTO offline_ad_policies VALUES(?,?,'ACTIVE',?,0,'PRIVATE_APPROVER')",[policy.version,environment,JSON.stringify(policy)]);
 f.sql.run("INSERT INTO combat_ad_policies VALUES(?,?,'ACTIVE',?)",['TEST_COMBAT',environment,JSON.stringify({...policy,version:'TEST_COMBAT',placement:'COMBAT_CLEAR',weightsBps:[6000,3000,1000]})]);
 const source=(consumer='OFFLINE',version='TEST_OFFLINE',role='VIEWER')=>admin.request(`/v1/admin/probability-disclosures/source?consumer=${consumer}&policyVersion=${version}`,{headers:{'Cf-Access-Subject':role}},env);
 const publish=(digest:string,key=crypto.randomUUID(),role='OWNER',extra:object={})=>admin.request('/v1/admin/probability-disclosures',{method:'POST',headers:{'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify({environment,consumer:'OFFLINE',policyVersion:'TEST_OFFLINE',expectedSourceDigest:digest,reason:'TEST_ONLY internal approval',...extra})},env);
 const read=(id:string,headers:Record<string,string>={})=>game.request(`/v1/public/probability-disclosures/${id}`,{headers},{...env,CLIENT_ADMISSION_MODE:'ENFORCED'});
 const candidate=async()=>{const r=await source();expect(r.status).toBe(200);return await r.json() as {sourceDigest:string;disclosureId:string};};
 return {...f,env,policy,source,publish,read,candidate};
}
describe('immutable multiplier probability disclosures',()=>{
 it('requires Admin auth, projects exact source weights and never exposes provider configuration',async()=>{
  const f=await fixture();expect((await admin.request('/v1/admin/probability-disclosures/source?consumer=OFFLINE&policyVersion=TEST_OFFLINE',{},f.env)).status).toBe(401);
  const r=await f.source();expect(r.status).toBe(200);const body=await r.json();
  expect(body).toMatchObject({contractVersion:'admin-probability-source-v1',scope:'MULTIPLIER_DISTRIBUTION',canPublish:false,distribution:[{multiplier:2,numerator:1,denominator:10000,percentage:'0.01'},{multiplier:3,numerator:0,denominator:10000,percentage:'0.00'},{multiplier:4,numerator:9999,denominator:10000,percentage:'99.99'}]});
  expect(JSON.stringify(body)).not.toMatch(/PRIVATE_|adUnit|appId|rewardItem|approved_by|config_json/);
  const combat=await (await f.source('COMBAT','TEST_COMBAT')).json();expect(combat).toMatchObject({distribution:[{multiplier:2,numerator:6000,percentage:'60.00'},{multiplier:3,numerator:3000,percentage:'30.00'},{multiplier:4,numerator:1000,percentage:'10.00'}]});
 });
 it('publishes only with OWNER approval and preserves the original response',async()=>{
  const f=await fixture();const p=await f.candidate();expect((await f.read(p.disclosureId)).status).toBe(404);
  expect((await f.publish(p.sourceDigest,'denied','OPERATOR')).status).toBe(403);
  const r=await f.publish(p.sourceDigest,'publish');expect(r.status).toBe(200);const receipt=await r.json();
  const publicRead=await f.read(p.disclosureId);expect(publicRead.status).toBe(200);expect(publicRead.headers.get('Cache-Control')).toBe('no-store');
  const body=await publicRead.json();expect(body).toMatchObject({contractVersion:'probability-disclosure-v1',disclosureId:p.disclosureId,consumer:'OFFLINE',policyVersion:'TEST_OFFLINE',scope:'MULTIPLIER_DISTRIBUTION',sourceDigest:p.sourceDigest,publicSchemaVersion:1});
  expect(JSON.stringify(body)).not.toMatch(/PRIVATE_|internal approval|OWNER|auditId|actor/);
  expect(await (await f.publish(p.sourceDigest,'publish')).json()).toEqual(receipt);
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);expect(f.scalar('SELECT count(*) FROM ad_runtime_policies')).toBe(0);
 });
 it('rejects changed source approvals and keeps published history after source retirement',async()=>{
  const f=await fixture();const p=await f.candidate();await f.publish(p.sourceDigest,'published');
  const original=await (await f.read(p.disclosureId)).json();
  f.sql.run("UPDATE offline_ad_policies SET config_json=? WHERE version='TEST_OFFLINE'",[JSON.stringify({...f.policy,weightsBps:[5000,5000,0]})]);
  expect((await f.publish(p.sourceDigest,'stale')).status).toBe(409);
  const next=await f.candidate();expect(next.disclosureId).not.toBe(p.disclosureId);expect((await f.publish(next.sourceDigest,'next')).status).toBe(200);
  f.sql.run("UPDATE offline_ad_policies SET status='RETIRED'");
  expect(await (await f.read(p.disclosureId)).json()).toEqual(original);
  expect((await f.publish(next.sourceDigest,'retired')).status).toBe(409);
 });
 it('rechecks the exact source within the publication batch',async()=>{
  const f=await fixture();const p=await f.candidate();
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify({...f.policy,weightsBps:[5000,5000,0]})]);};
  expect((await f.publish(p.sourceDigest)).status).toBe(409);expect((await f.read(p.disclosureId)).status).toBe(404);
 });
 it('does not allow client probability rows, unknown sources or mismatched environments',async()=>{
  const f=await fixture();const p=await f.candidate();
  for(const extra of [{distribution:[{multiplier:9,numerator:1}]},{consumer:'REROLL'},{environment:'DEV'}])expect((await f.publish(p.sourceDigest,'bad','OWNER',extra)).status).toBe(400);
  expect((await f.source('OFFLINE','missing')).status).toBe(404);
  f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify({...f.policy,environment:'DEV'})]);
  expect((await f.source()).status).toBe(503);
 });
 it('rolls back on audit failure and checks current OWNER inside the batch',async()=>{
  const f=await fixture();const p=await f.candidate();
  f.sql.run("CREATE TRIGGER test_disclosure_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.result='SUCCESS' BEGIN SELECT RAISE(ABORT,'TEST_AUDIT'); END");
  expect((await f.publish(p.sourceDigest)).status).toBe(503);expect((await f.read(p.disclosureId)).status).toBe(404);
  f.sql.run('DROP TRIGGER test_disclosure_audit');f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
  expect((await f.publish(p.sourceDigest)).status).toBe(403);expect(f.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
 });
 it('converges same-key concurrent publish and rejects other-key duplicate immutable IDs',async()=>{
  const f=await fixture();const p=await f.candidate();const results=await Promise.all([f.publish(p.sourceDigest,'same'),f.publish(p.sourceDigest,'same')]);
  expect(results.map(r=>r.status)).toEqual([200,200]);expect(await results[0]!.json()).toEqual(await results[1]!.json());
  expect((await f.publish(p.sourceDigest,'another')).status).toBe(409);
  expect((await f.publish(p.sourceDigest,'same','OWNER',{reason:'changed'})).status).toBe(409);
  expect(f.scalar('SELECT count(*) FROM probability_disclosures')).toBe(1);
  expect(()=>f.sql.run('DELETE FROM probability_disclosures')).toThrow('probability_disclosure_immutable');
 });
 it('requires active exact-version DEV tester access and does not bypass other gameplay admission',async()=>{
  const f=await fixture('DEV');const p=await f.candidate();await f.publish(p.sourceDigest);
  expect((await f.read(p.disclosureId)).status).toBe(401);
  const headers={...f.headers,'X-LastRail-App-Version-Code':'123'};expect((await f.read(p.disclosureId,headers)).status).toBe(403);
  f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)",[Date.now()+60000]);
  expect((await f.read(p.disclosureId,headers)).status).toBe(200);
  expect((await f.read(p.disclosureId,{...headers,'X-LastRail-App-Version-Code':'124'})).status).toBe(403);
  f.sql.run("UPDATE account_tester_entitlements SET revoked_at=1");expect((await f.read(p.disclosureId,headers)).status).toBe(403);
  expect((await game.request('/v1/profile',{headers},{...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'})).status).toBe(503);
  const prd=await game.request(`/v1/public/probability-disclosures/${p.disclosureId}`,{}, {...f.env,ENVIRONMENT:'PRD',CLIENT_ADMISSION_MODE:'ENFORCED'});expect(prd.status).toBe(404);
 });
 it('returns unavailable rather than success when stored disclosure data is corrupted',async()=>{
  const f=await fixture();const p=await f.candidate();await f.publish(p.sourceDigest);
  f.sql.run('DROP TRIGGER probability_disclosure_no_update');f.sql.run("UPDATE probability_disclosures SET payload_json='{}'");
  expect((await f.read(p.disclosureId)).status).toBe(503);
 });

 it.each(['retired','deleted','invalid'])('recovers a concurrent commit before a source becomes %s',async(change)=>{
  const f=await fixture();const p=await f.candidate();const prepare=f.env.DB.prepare.bind(f.env.DB);
  let intercept=true,winner:unknown;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT payload_hash,response_json FROM probability_disclosure_receipts')&&intercept){
    const first=statement.first.bind(statement);
    statement.first=async <T>()=>{const result=await first<T>();if(intercept){
      intercept=false;const r=await f.publish(p.sourceDigest,'same-race');expect(r.status).toBe(200);winner=await r.json();
      f.sql.run(change==='retired'?"UPDATE offline_ad_policies SET status='RETIRED'":change==='deleted'?'DELETE FROM offline_ad_policies':"UPDATE offline_ad_policies SET config_json='{}'");
    }return result;};
   }
   return statement;
  };
  const r=await f.publish(p.sourceDigest,'same-race');expect(r.status).toBe(200);expect(await r.json()).toEqual(winner);
 });
 it('reports an already published source as unavailable for another publication',async()=>{
  const f=await fixture();const p=await f.candidate();await f.publish(p.sourceDigest);
  expect(await (await f.source('OFFLINE','TEST_OFFLINE','OWNER')).json()).toMatchObject({canPublish:false,publicationStatus:'PUBLISHED'});
 });
});
describe('published probability history',()=>{
 const path='/v1/public/probability-disclosures';
 async function historyFixture(environment='PRD'){
  const f=await fixture(environment);
  const list=(query='',headers:Record<string,string>={})=>game.request(path+query,{headers},{...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'});
  const publishMany=async(count:number)=>{
   const ids:string[]=[];
   for(let i=0;i<count;i++){
    f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify({...f.policy,weightsBps:[i,0,10000-i]})]);
    const candidate=await f.candidate();
    const published=await f.publish(candidate.sourceDigest,'history-'+i);expect(published.status).toBe(200);ids.push(candidate.disclosureId);
   }
   return ids.sort();
  };
  return {...f,list,publishMany};
 }
 it('returns empty approved history without publishing the active source',async()=>{
  const f=await historyFixture();await f.candidate();
  const r=await f.list();expect(r.status).toBe(200);expect(r.headers.get('Cache-Control')).toBe('no-store');
  expect(await r.json()).toMatchObject({contractVersion:'public-probability-list-v1',scope:'PUBLISHED_HISTORY',environment:'PRD',items:[],nextCursor:null});
  expect(f.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
 });
 it('paginates immutable history in ID order including retired sources',async()=>{
  const f=await historyFixture();const ids=await f.publishMany(3);f.sql.run("UPDATE offline_ad_policies SET status='RETIRED'");
  const r=await f.list('?limit=2');expect(r.status).toBe(200);
  const first=await r.json() as {items:Array<{disclosureId:string}>;nextCursor:string};
  expect(first.items.map(x=>x.disclosureId)).toEqual(ids.slice(0,2));expect(first.nextCursor).toBeTruthy();
  for(const item of first.items)expect(item).toEqual(await (await f.read(item.disclosureId)).json());
  const last=await (await f.list('?limit=2&cursor='+encodeURIComponent(first.nextCursor))).json() as {items:Array<{disclosureId:string}>;nextCursor:null};
  expect(last.items.map(x=>x.disclosureId)).toEqual(ids.slice(2));expect(last.nextCursor).toBeNull();
  expect(JSON.stringify(first)).not.toMatch(/PRIVATE_|internal approval|actor|auditId|config_json/);
 });
 it('excludes another environment and rejects its cursor',async()=>{
  const f=await historyFixture();await f.publishMany(2);
  const prd=await (await f.list('?limit=1')).json() as {nextCursor:string};
  f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)",[Date.now()+60000]);
  const headers={...f.headers,'X-LastRail-App-Version-Code':'123'};
  const devEnv={...f.env,ENVIRONMENT:'DEV',CLIENT_ADMISSION_MODE:'ENFORCED'};
  const empty=await game.request(path,{headers},devEnv);expect(empty.status).toBe(200);expect(await empty.json()).toMatchObject({items:[],nextCursor:null});
  expect((await game.request(path+'?cursor='+encodeURIComponent(prd.nextCursor),{headers},devEnv)).status).toBe(400);
 });
 it.each(['?limit=0','?limit=51','?limit=01','?limit=1.5','?limit=2&limit=3','?cursor=','?cursor=bad','?consumer=OFFLINE','?environment=DEV','?after=x'])('rejects invalid pagination %s',async(query)=>{
  const f=await historyFixture();const r=await f.list(query);expect(r.status).toBe(400);expect(r.headers.get('Cache-Control')).toBe('no-store');
 });
 it('checks DEV identity, exact version, expiry and revocation on every page',async()=>{
  const f=await historyFixture('DEV');await f.publishMany(2);
  expect((await f.list()).status).toBe(401);
  const headers={...f.headers,'X-LastRail-App-Version-Code':'123'};
  expect((await f.list('',headers)).status).toBe(403);
  f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)",[Date.now()+60000]);
  const first=await f.list('?limit=1',headers);expect(first.status).toBe(200);
  const page=await first.json() as {nextCursor:string};
  expect((await f.list('',{...headers,'X-LastRail-App-Version-Code':'124'})).status).toBe(403);
  f.sql.run("UPDATE account_tester_entitlements SET expires_at=0");
  expect((await f.list('?cursor='+encodeURIComponent(page.nextCursor),headers)).status).toBe(403);
  f.sql.run("UPDATE account_tester_entitlements SET expires_at=?,revoked_at=1",[Date.now()+60000]);
  expect((await f.list('',headers)).status).toBe(403);
 });
 it('fails the entire page for returned or lookahead corruption',async()=>{
  const f=await historyFixture();const ids=await f.publishMany(2);
  f.sql.run('DROP TRIGGER probability_disclosure_no_update');
  f.sql.run("UPDATE probability_disclosures SET payload_json='{}' WHERE disclosure_id=?",[ids[1]!]);
  for(const limit of [1,2]){
   const r=await f.list('?limit='+limit);expect(r.status).toBe(503);expect(await r.json()).not.toHaveProperty('items');
  }
 });
 it('does not return empty success when storage is unavailable',async()=>{
  const f=await historyFixture();f.sql.run('DROP TABLE probability_disclosures');
  const r=await f.list();expect(r.status).toBe(503);expect(await r.json()).not.toHaveProperty('items');
 });
 it('preserves gameplay admission and public write protection',async()=>{
  const f=await historyFixture();
  for(const p of ['/v1/profile',path+'/unrecognized']){
   expect((await game.request(p,{headers:f.headers},{...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'})).status).toBe(503);
  }
  expect((await game.request(path,{method:'POST',headers:f.headers},{...f.env,CLIENT_ADMISSION_MODE:'ENFORCED'})).status).toBe(503);
  expect(f.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
 });
});

describe('purchase-policy probability disclosures',()=>{
 async function purchaseFixture(){
  const f=await fixture();
  const policy={...f.policy,weightsBps:[6000,3000,1000]};
  f.sql.run("UPDATE offline_ad_policies SET config_json=?",[JSON.stringify(policy)]);
  f.sql.run("INSERT INTO purchase_reward_policies VALUES('PRD','OFFLINE',?)",[JSON.stringify(policy)]);
  const source=(consumer='OFFLINE',version='TEST_OFFLINE',kind='PURCHASE_POLICY')=>admin.request('/v1/admin/probability-disclosures/source?consumer='+consumer+'&policyVersion='+version+'&sourceKind='+kind,{headers:{'Cf-Access-Subject':'OWNER'}},f.env);
  const publish=(digest:string,key=crypto.randomUUID(),extra:object={})=>f.publish(digest,key,'OWNER',{sourceKind:'PURCHASE_POLICY',...extra});
  const candidate=async()=>{const r=await source();expect(r.status).toBe(200);return await r.json() as {sourceDigest:string;disclosureId:string};};
  return {...f,policy,source,publish,candidate,adCandidate:f.candidate,adPublish:f.publish};
 }
 it('distinguishes purchase and ad provenance even with identical policy bytes',async()=>{
  const f=await purchaseFixture();const p=await f.candidate(),ad=await f.adCandidate();
  expect(p.sourceDigest).toBe(ad.sourceDigest);expect(p.disclosureId).not.toBe(ad.disclosureId);
  const preview=await (await f.source()).json();expect(preview).toMatchObject({publicSchemaVersion:2,sourceKind:'PURCHASE_POLICY',sourceStatus:'CONFIGURED',canPublish:true,distribution:[{multiplier:2,percentage:'60.00'},{multiplier:3,percentage:'30.00'},{multiplier:4,percentage:'10.00'}]});
  expect(JSON.stringify(preview)).not.toMatch(/PRIVATE_|adUnit|appId|rewardItem|config_json/);
  expect((await f.publish(p.sourceDigest)).status).toBe(200);expect((await f.adPublish(ad.sourceDigest)).status).toBe(200);
  expect(await (await f.read(p.disclosureId)).json()).toMatchObject({publicSchemaVersion:2,sourceKind:'PURCHASE_POLICY'});
  expect(await (await f.read(ad.disclosureId)).json()).toMatchObject({publicSchemaVersion:1});
  const list=await game.request('/v1/public/probability-disclosures',{},f.env);expect(list.status).toBe(200);
  const body=await list.json() as {items:Array<{publicSchemaVersion:number}>};expect(body.items.map(x=>x.publicSchemaVersion).sort()).toEqual([1,2]);
 });
 it('uses the existing purchase rule validator, including reduced COMBAT configuration',async()=>{
  const f=await purchaseFixture();
  const combat={version:'PURCHASE_COMBAT',environment:'PRD',placement:'COMBAT_CLEAR',weightsBps:[6000,3000,1000]};
  f.sql.run("INSERT INTO purchase_reward_policies VALUES('PRD','COMBAT',?)",[JSON.stringify(combat)]);
  const source=await f.source('COMBAT','PURCHASE_COMBAT');expect(source.status).toBe(200);
  const p=await source.json() as {sourceDigest:string;disclosureId:string};
  expect((await f.publish(p.sourceDigest,'combat',{consumer:'COMBAT',policyVersion:'PURCHASE_COMBAT'})).status).toBe(200);
  expect(await (await f.read(p.disclosureId)).json()).toMatchObject({consumer:'COMBAT',sourceKind:'PURCHASE_POLICY'});
  f.sql.run("UPDATE purchase_reward_policies SET config_json=? WHERE kind='OFFLINE'",[JSON.stringify({...f.policy,weightsBps:[5000,5000,0]})]);
  expect((await f.source()).status).toBe(503);
 });
 it('does not substitute ad policy when purchase source is missing or has another version',async()=>{
  const f=await purchaseFixture();
  expect((await f.source('OFFLINE','other')).status).toBe(404);
  f.sql.run('DELETE FROM purchase_reward_policies');
  expect((await f.source()).status).toBe(404);expect((await f.adCandidate()).disclosureId).toBeTruthy();
 });
 it('rejects unknown or duplicated source kinds and mismatched stored environments',async()=>{
  const f=await purchaseFixture();const p=await f.candidate();
  expect((await f.source('OFFLINE','TEST_OFFLINE','OTHER')).status).toBe(400);
  expect((await f.source('OFFLINE','TEST_OFFLINE','PURCHASE_POLICY&sourceKind=AD_POLICY')).status).toBe(400);
  expect((await f.publish(p.sourceDigest,'bad',{sourceKind:'OTHER'})).status).toBe(400);
  f.sql.run("UPDATE purchase_reward_policies SET config_json=?",[JSON.stringify({...f.policy,environment:'DEV'})]);
  expect((await f.source()).status).toBe(503);
 });
 it.each(['change','delete'])('atomically rejects purchase source %s during publication',async(change)=>{
  const f=await purchaseFixture();const p=await f.candidate();
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;
   if(change==='delete')f.sql.run('DELETE FROM purchase_reward_policies');
   else f.sql.run("UPDATE purchase_reward_policies SET config_json=?",[JSON.stringify({...f.policy,appId:'changed'})]);
  };
  expect((await f.publish(p.sourceDigest)).status).toBe(409);
  expect(f.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM probability_disclosure_receipts')).toBe(0);
 });
 it('preserves published purchase history and same-key receipt after source deletion',async()=>{
  const f=await purchaseFixture();const p=await f.candidate();
  const r=await f.publish(p.sourceDigest,'saved');expect(r.status).toBe(200);const receipt=await r.json();
  const original=await (await f.read(p.disclosureId)).json();f.sql.run('DELETE FROM purchase_reward_policies');
  expect(await (await f.publish(p.sourceDigest,'saved')).json()).toEqual(receipt);
  expect(await (await f.read(p.disclosureId)).json()).toEqual(original);
  expect((await f.publish(p.sourceDigest,'saved',{sourceKind:'AD_POLICY'})).status).toBe(409);
 });
 it('retains current OWNER and audit rollback checks for purchase sources',async()=>{
  const f=await purchaseFixture();const p=await f.candidate();
  expect((await f.publish(p.sourceDigest,'denied')).status).toBe(200);
  expect((await f.source()).status).toBe(200);
  expect(await (await f.source()).json()).toMatchObject({canPublish:false,publicationStatus:'PUBLISHED'});
  const other=await purchaseFixture(),q=await other.candidate();
  other.sql.run("CREATE TRIGGER test_purchase_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.result='SUCCESS' BEGIN SELECT RAISE(ABORT,'TEST_AUDIT'); END");
  expect((await other.publish(q.sourceDigest)).status).toBe(503);expect(other.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
  other.sql.run('DROP TRIGGER test_purchase_audit');
  other.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  other.adapter.beforeBatch=()=>{other.adapter.beforeBatch=undefined;other.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
  expect((await other.publish(q.sourceDigest)).status).toBe(403);
  expect(other.scalar('SELECT count(*) FROM probability_disclosures')).toBe(0);
 });
});

it('preserves the golden v1 ad permalink and receipts created before sourceKind existed',async()=>{
 const f=await fixture();
 const raw=JSON.stringify({version:'TEST_OFFLINE',environment:'PRD',placement:'OFFLINE_SUPPLY',weightsBps:[1,0,9999]});
 f.sql.run("UPDATE offline_ad_policies SET config_json=?",[raw]);
 const p=await f.candidate();
 expect(p.sourceDigest).toBe('sha256:120db8cbe537a9f4b227a58b27bcfb21bdee3a4a2c58d74cfe4fc494090db744');
 expect(p.disclosureId).toBe('pd_b2470b694e8efb2b7ee4dda5de5927c665377d62ce8cbc26ff0e851e09463576');
 expect((await f.publish(p.sourceDigest,'legacy-publish')).status).toBe(200);
 expect(await (await f.read(p.disclosureId)).json()).toMatchObject({publicSchemaVersion:1});
 const original={accepted:true,contractVersion:'admin-probability-publish-v1',environment:'PRD',disclosureId:p.disclosureId,auditId:'audit_before_source_kind',requestId:'request_before_source_kind'};
 const hash=await sha256Hex(canonicalJson({environment:'PRD',consumer:'OFFLINE',policyVersion:'TEST_OFFLINE',expectedSourceDigest:p.sourceDigest,reason:'TEST_ONLY internal approval'}));
 f.sql.run("INSERT INTO probability_disclosure_receipts VALUES('OWNER','PRD','legacy-receipt',?,?)",[hash,JSON.stringify(original)]);
 f.sql.run('DELETE FROM offline_ad_policies');
 const r=await f.publish(p.sourceDigest,'legacy-receipt');expect(r.status).toBe(200);expect(await r.json()).toEqual(original);
});
