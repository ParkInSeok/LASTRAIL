import {describe,it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {testMaintenancePolicy} from '../helpers/maintenance-policy-fixture';
import admin from '../../apps/admin-api/src/index';
import {createOfflineAdIntent,recordOfflineAdVerification} from '../../apps/game-api/src/offline-ad-service';
import {createCombatAdIntent,recordCombatAdVerification} from '../../apps/game-api/src/combat-ad-service';
import {approveAdStart} from '../../apps/game-api/src/ad-start-approval';
const policy={version:'PIN_TEST',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000],
 challengeLifetimeMs:60000,callbackGraceMs:60000,maxAdsPerDay:10,dayStartUtcMinutes:0,
 adUnit:'TEST_UNIT',appId:'TEST_APP',rewardItem:'TEST_ITEM',rewardAmount:'1'};
async function fixture(consumer:'OFFLINE'|'COMBAT'='OFFLINE',purchase=false){
 const f=await setup();const p={...policy,placement:consumer==='OFFLINE'?'OFFLINE_SUPPLY':'COMBAT_CLEAR'};
 f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 if(purchase)f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV',?,?)",[consumer,JSON.stringify(p)]);
 else if(consumer==='OFFLINE')f.sql.run("INSERT INTO offline_ad_policies VALUES(?,'DEV','ACTIVE',?,0,'TEST_ONLY')",[p.version,JSON.stringify(p)]);
 else f.sql.run("INSERT INTO combat_ad_policies VALUES(?,'DEV','ACTIVE',?)",[p.version,JSON.stringify(p)]);
 if(consumer==='COMBAT'){
  f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('m','a',100,'TEST',0)");
  f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES('r','a','m',1,'COMPLETED',0,1)");
  f.sql.run("INSERT INTO run_final_results VALUES('r','VICTORY',15,'{}',1)");
  f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES('p','a','r',1,'ACTIVE',?,?,0)",[JSON.stringify({calculation:{operationId:'opn_01_last_departure',outcome:'VICTORY'},rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:240,source:'RUN_CLEAR'}]}),Date.now()+3600000]);
 }
 const kind=purchase?'PURCHASE_POLICY':'AD_POLICY';
 const publish=async()=>{
  const r=await admin.request('/v1/admin/probability-disclosures/source?consumer='+consumer+'&policyVersion='+p.version+'&sourceKind='+kind,{headers:{'Cf-Access-Subject':'OWNER'}},env);expect(r.status).toBe(200);
  const source=await r.json() as {disclosureId:string;sourceDigest:string};
  const saved=await admin.request('/v1/admin/probability-disclosures',{method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':crypto.randomUUID()},body:JSON.stringify({environment:'DEV',consumer,sourceKind:kind,policyVersion:p.version,expectedSourceDigest:source.sourceDigest,reason:'TEST_ONLY pin readiness'})},env);
  expect(saved.status).toBe(200);return source;
 };
 const create=(key='create')=>consumer==='OFFLINE'?createOfflineAdIntent(f.env.DB,'a','DEV',p,key,purchase?'PURCHASE_ONLY':'AD_REQUEST'):
  createCombatAdIntent(f.env.DB,'a','DEV','r','p',key,purchase?p:undefined);
 return {...f,p,env,publish,create,table:consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents',sourceTable:purchase?'purchase_reward_policies':consumer==='OFFLINE'?'offline_ad_policies':'combat_ad_policies'};
}

describe('maintenance reward admission',()=>{
 it.each([['OFFLINE',false],['COMBAT',false],['OFFLINE',true],['COMBAT',true]] as const)('blocks new %s purchase=%s and preserves replay',async(consumer,purchase)=>{
  const f=await fixture(consumer,purchase);await f.publish();
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;testMaintenancePolicy(f.sql,true);};
  await expect(f.create()).rejects.toMatchObject({status:503,code:'maintenance_new_request_blocked'});
  expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(0);
 });
 it.each(['OFFLINE','COMBAT'] as const)('preserves %s opportunity replay but blocks a new ad start',async consumer=>{
  const f=await fixture(consumer);await f.publish();const first=await f.create();testMaintenancePolicy(f.sql,true);
  expect(await f.create()).toMatchObject({intentId:first.intentId,probabilityDisclosure:first.probabilityDisclosure,status:first.status});
  await expect(approveAdStart(f.env.DB,'a','DEV',consumer,first.intentId,'start')).rejects.toMatchObject({code:'maintenance_new_request_blocked'});
  expect(f.scalar('SELECT count(*) FROM ad_start_approvals')).toBe(0);
 });
 it('replays a pre-maintenance ad start without a second SDK show',async()=>{
  const f=await fixture();await f.publish();const first=await f.create();
  await approveAdStart(f.env.DB,'a','DEV','OFFLINE',first.intentId,'start');testMaintenancePolicy(f.sql,true);
  expect(await approveAdStart(f.env.DB,'a','DEV','OFFLINE',first.intentId,'start')).toMatchObject({mayShow:false,startStatus:'ALREADY_APPROVED'});
 });
 it.each(['OFFLINE','COMBAT'] as const)('retains verified %s callbacks under maintenance without duplicate approval',async consumer=>{
  const f=await fixture(consumer);await f.publish();const i=await f.create();
  await approveAdStart(f.env.DB,'a','DEV',consumer,i.intentId,'start');testMaintenancePolicy(f.sql,true);
  const record=consumer==='OFFLINE'?recordOfflineAdVerification:recordCombatAdVerification;
  const callback={provider:'ADMOB' as const,transactionId:'TEST_CALLBACK',timestamp:Date.now(),userId:i.userId!,customData:i.customData!,adUnit:'TEST_UNIT'};
  await record(f.env.DB,i.intentId,callback);await record(f.env.DB,i.intentId,callback);
  expect(f.scalar('SELECT count(*) FROM reward_opportunity_approvals')).toBe(1);
 });
 it.each([['OFFLINE',false],['COMBAT',false],['OFFLINE',true],['COMBAT',true]] as const)('recovers committed %s purchase=%s after a lost DB response and maintenance',async(consumer,purchase)=>{
  const f=await fixture(consumer,purchase);await f.publish();const batch=f.env.DB.batch.bind(f.env.DB);let once=true;
  f.env.DB.batch=async statements=>{const result=await batch(statements);if(once){once=false;testMaintenancePolicy(f.sql,true);throw new Error('maintenance_new_request_blocked');}return result;};
  const result=await f.create();expect(result.accepted).toBe(true);expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(1);
 });

});
