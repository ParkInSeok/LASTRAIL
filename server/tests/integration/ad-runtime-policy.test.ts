import {createPublishedOfflineForTest as createOfflineAdIntent,createPublishedCombatForTest as createCombatAdIntent} from '../helpers/published-disclosure-fixture';
import {publishConfiguredDisclosuresForTest} from '../helpers/published-disclosure-fixture';
import {describe,it,expect} from 'vitest';
import app from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
import {recordOfflineAdVerification,spinOfflineAd} from '../../apps/game-api/src/offline-ad-service';
import {approveAdStart} from '../../apps/game-api/src/ad-start-approval';
import {recordCombatAdVerification,spinCombatAd} from '../../apps/game-api/src/combat-ad-service';

const policy={version:'TEST_AD',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[10000,0,0],challengeLifetimeMs:60000,callbackGraceMs:60000,maxAdsPerDay:5,dayStartUtcMinutes:0,adUnit:'test-unit',appId:'test-app',rewardItem:'test',rewardAmount:'1'};
async function fixture(){const f=await setup(false,true,null);return f;}
async function combatFixture(){
 const f=await setup();
 f.sql.run("INSERT INTO combat_ad_policies VALUES('TEST_COMBAT','DEV','ACTIVE',?)",[JSON.stringify({...policy,version:'TEST_COMBAT',placement:'COMBAT_CLEAR',weightsBps:[6000,3000,1000]})]);
 f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('m','a',100,'TEST',0)");
 f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES('r','a','m',1,'COMPLETED',0,1)");
 f.sql.run("INSERT INTO run_final_results VALUES('r','VICTORY',15,'{}',1)");
 const plan={calculation:{operationId:'opn_01_last_departure',outcome:'VICTORY'},rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:240,source:'RUN_CLEAR'}]};
 f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES('p','a','r',1,'ACTIVE',?,?,?)",[JSON.stringify(plan),Date.now()+3600000,Date.now()]);
 return f;
}
describe('runtime ad policy',()=>{
 it('rejects malformed policy revisions at the database boundary',async()=>{
  const f=await fixture();
  for(const value of [1.5,'invalid',9007199254740992])expect(()=>f.sql.run("INSERT INTO ad_runtime_policies VALUES('DEV',?,1)",[value])).toThrow();
 });
 it('the atomic intent gate denies a corrupted UNKNOWN policy even after an ON precheck',async()=>{
  const f=await setup();
  f.env.DB.beforeBatch=()=>{
   f.env.DB.beforeBatch=undefined;
   f.sql.run("DELETE FROM ad_runtime_policies; PRAGMA ignore_check_constraints=ON; INSERT INTO ad_runtime_policies VALUES('DEV',1.5,1); PRAGMA ignore_check_constraints=OFF;");
  };
  await expect(createOfflineAdIntent(f.env.DB,'a','DEV',policy,'corrupt')).rejects.toMatchObject({code:'ad_runtime_policy_changed'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
 });
 it('reads unknown explicitly, requires auth, and rejects client policy input',async()=>{
  const f=await fixture();
  const r=await app.request('/v1/runtime-policy',{headers:f.headers},f.env);
  expect(r.status).toBe(200);
  expect(await r.json()).toMatchObject({contractVersion:'runtime-policy-v1',policyStatus:'UNKNOWN',adsEnabled:null,runtimePolicyRevision:null,environment:'DEV'});
  expect((await app.request('/v1/runtime-policy',{},f.env)).status).toBe(401);
  expect((await app.request('/v1/runtime-policy?adsEnabled=true',{headers:f.headers},f.env)).status).toBe(400);
 });
 it('unknown denies a new advertisement without disabling basic collection',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO offline_ad_policies VALUES('TEST_AD','DEV','ACTIVE',?,0,'test')",[JSON.stringify(policy)]);
  const response=await app.request('/v1/offline-rewards/ad-intents',{method:'POST',headers:{...f.headers,'Idempotency-Key':'new'}},f.env);
  expect(response.status).toBe(503);expect(await response.json()).toMatchObject({errorCode:'ad_runtime_policy_unknown'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
  expect((await app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'basic'}},f.env)).status).toBe(200);
 });
 it('requires start-protocol negotiation before legacy clients receive a new ad challenge',async()=>{
  const f=await setup();f.sql.run("INSERT INTO offline_ad_policies VALUES('TEST_AD','DEV','ACTIVE',?,0,'test')",[JSON.stringify(policy)]);
  const legacyHeaders={Authorization:f.headers.Authorization,'Idempotency-Key':'legacy'};
  const r=await app.request('/v1/offline-rewards/ad-intents',{method:'POST',headers:legacyHeaders},f.env);
  expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:'ad_start_contract_required'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
 });
 it('OFF is a known policy, scoped to environment, and blocks new ad service calls',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO ad_runtime_policies VALUES('DEV',1,0),('PRD',5,1)");
  expect(await (await app.request('/v1/runtime-policy',{headers:f.headers},f.env)).json()).toMatchObject({policyStatus:'CONFIGURED',adsEnabled:false,runtimePolicyRevision:1,environment:'DEV'});
  await expect(createOfflineAdIntent(f.env.DB,'a','DEV',policy,'off')).rejects.toMatchObject({code:'ads_disabled_by_policy'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
 });
 it('a switch between read and write rolls back the entire new intent batch',async()=>{
  const f=await setup();
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;f.sql.run("UPDATE ad_runtime_policies SET revision=revision+1,ads_enabled=0 WHERE environment='DEV'");};
  await expect(createOfflineAdIntent(f.env.DB,'a','DEV',policy,'race')).rejects.toMatchObject({code:'ad_runtime_policy_changed'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
  expect(f.scalar("SELECT count(*) FROM account_mutations WHERE mutation_type='OFFLINE_AD_INTENT'")).toBe(0);
 });
 it('preserves a verified approval and its creation/spin replay after OFF',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'original');
  await approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start');
  await recordOfflineAdVerification(f.env.DB,i.intentId,{provider:'ADMOB',transactionId:'test-runtime-tx',timestamp:Date.now(),userId:i.userId!,customData:i.customData!,adUnit:'test-unit'});
  f.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");
  const read=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'original');expect(read.intentId).toBe(i.intentId);expect(read.canSpin).toBe(true);
  const paid=await spinOfflineAd(f.env.DB,'a',i.intentId,'spin');expect(await spinOfflineAd(f.env.DB,'a',i.intentId,'spin')).toEqual(paid);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
 });
 it('retains ACTIVE providerless benefit authorization in OFF and never enables REROLL',async()=>{
  const f=await setup(false,true,false);
  f.sql.run("INSERT INTO verified_store_orders VALUES('order','a','GOOGLE_PLAY','test.app','test.permanent','test-tx','DEV','AD_SKIP_PERMANENT','fixture',0,0,NULL)");
  f.sql.run("INSERT INTO account_ad_skip_entitlements VALUES('a','DEV','ACTIVE',1,'order',0)");
  f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','OFFLINE',?)",[JSON.stringify({version:'TEST_GAME',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000]})]);
  await publishConfiguredDisclosuresForTest(f.env.DB);
  const state=await (await app.request('/v1/ad-skip',{headers:f.headers},f.env)).json();
  expect(state).toMatchObject({owned:true,featureAvailability:{OFFLINE:'AVAILABLE',REROLL:'UNSUPPORTED'}});
  const result=await app.request('/v1/ad-skip/uses/authorize',{method:'POST',headers:{...f.headers,'Idempotency-Key':'use','X-Reward-Contract':'reward-opportunity-v2'},body:JSON.stringify({opportunity:{kind:'OFFLINE'}})},f.env);
  expect(result.status).toBe(200);expect(await result.json()).toMatchObject({approval:{cause:'PURCHASE_ENTITLEMENT'},adContext:null});
 });
 it('does not approve a created but unstarted ad after OFF',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'create');
  f.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");
  await expect(approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start')).rejects.toMatchObject({code:'ads_disabled_by_policy'});
  await expect(recordOfflineAdVerification(f.env.DB,i.intentId,{provider:'ADMOB',transactionId:'unstarted',timestamp:Date.now(),userId:i.userId!,customData:i.customData!,adUnit:'test-unit'})).rejects.toMatchObject({code:'ad_start_not_approved'});
 });
 it('retains a start approved before OFF through signed completion, without new start authorization on replay',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'create');
  const started=await approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start');
  f.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");
  const replay=await approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start');
  expect(started).toMatchObject({startStatus:'APPROVED',mayShow:true,runtimePolicyRevision:1});
  expect(replay).toMatchObject({startStatus:'ALREADY_APPROVED',mayShow:false,runtimePolicyRevision:1});
  await recordOfflineAdVerification(f.env.DB,i.intentId,{provider:'ADMOB',transactionId:'started',timestamp:Date.now(),userId:i.userId!,customData:i.customData!,adUnit:'test-unit'});
  expect((await spinOfflineAd(f.env.DB,'a',i.intentId,'spin')).accepted).toBe(true);
 });
 it('atomically rejects OFF between start read and start approval write',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'create');
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;f.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");};
  await expect(approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start')).rejects.toMatchObject({code:'ad_runtime_policy_changed'});
  expect(f.scalar('SELECT count(*) FROM ad_start_approvals')).toBe(0);
 });
 it('exposes an authenticated start endpoint with key binding and no client policy override',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'create');
  const path=`/v1/ad-opportunities/OFFLINE/${i.intentId}/start`;
  const headers={...f.headers,'Idempotency-Key':'start'};
  expect((await app.request(path,{method:'POST'},f.env)).status).toBe(401);
  expect((await app.request(path,{method:'POST',headers,body:'{"adsEnabled":true}'},f.env)).status).toBe(400);
  const started=await app.request(path,{method:'POST',headers},f.env);expect(started.status).toBe(200);
  expect(await started.json()).toMatchObject({contractVersion:'ad-start-v1',mayShow:true,startStatus:'APPROVED'});
  expect(await (await app.request(path,{method:'POST',headers},f.env)).json()).toMatchObject({mayShow:false,startStatus:'ALREADY_APPROVED'});
  await expect(approveAdStart(f.env.DB,'a','DEV','COMBAT','other','start')).rejects.toMatchObject({code:'idempotency_conflict'});
  await expect(approveAdStart(f.env.DB,'b','DEV','OFFLINE',i.intentId,'other')).rejects.toMatchObject({code:'ad_intent_not_found'});
  await expect(approveAdStart(f.env.DB,'a','PRD','OFFLINE',i.intentId,'other')).rejects.toMatchObject({code:'ad_intent_not_found'});
 });
 it('rejects start when basic supply was already consumed, including batch race',async()=>{
  const f=await setup();const i=await createOfflineAdIntent(f.env.DB,'a','DEV',policy,'create');
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;f.sql.run("UPDATE accounts SET created_at=123 WHERE id='a'");};
  await expect(approveAdStart(f.env.DB,'a','DEV','OFFLINE',i.intentId,'start')).rejects.toMatchObject({code:'ad_start_not_available'});
  expect(f.scalar('SELECT count(*) FROM ad_start_approvals')).toBe(0);
 });
 it('preserves combat start-before-OFF completion and rejects combat creation while OFF',async()=>{
  const f=await combatFixture();const i=await createCombatAdIntent(f.env.DB,'a','DEV','r','p','create');
  await approveAdStart(f.env.DB,'a','DEV','COMBAT',i.intentId,'start');
  f.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");
  await recordCombatAdVerification(f.env.DB,i.intentId,{provider:'ADMOB',transactionId:'combat-runtime',timestamp:Date.now(),userId:i.userId!,customData:i.customData!,adUnit:'test-unit'});
  const paid=await spinCombatAd(f.env.DB,'a','DEV','r','p',i.intentId,'spin');
  expect(await spinCombatAd(f.env.DB,'a','DEV','r','p',i.intentId,'spin')).toEqual(paid);
  const other=await combatFixture();other.sql.run("UPDATE ad_runtime_policies SET ads_enabled=0,revision=2 WHERE environment='DEV'");
  await expect(createCombatAdIntent(other.env.DB,'a','DEV','r','p','off')).rejects.toMatchObject({code:'ads_disabled_by_policy'});
  expect(other.scalar('SELECT count(*) FROM combat_ad_intents')).toBe(0);
 });
});
