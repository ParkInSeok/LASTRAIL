import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import {join} from 'node:path';
import {SqlJsD1Database} from '../helpers/growth-d1';
import {describe,it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
import {createOfflineAdIntent,readOfflineAdIntent} from '../../apps/game-api/src/offline-ad-service';
import {createCombatAdIntent} from '../../apps/game-api/src/combat-ad-service';
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
describe('new reward opportunity disclosure requirement',()=>{
 it.each([['OFFLINE',false],['COMBAT',false],['OFFLINE',true],['COMBAT',true]] as const)('requires approved %s purchase=%s before any new intent',async(consumer,purchase)=>{
  const f=await fixture(consumer,purchase);
  await expect(f.create()).rejects.toMatchObject({status:503,code:'reward_disclosure_unavailable'});
  expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(0);
  const p=await f.publish();const created=await f.create();
  expect(created).toMatchObject({probabilityDisclosure:{status:'PINNED',disclosureId:p.disclosureId,sourceDigest:p.sourceDigest,publicPath:'/v1/public/probability-disclosures/'+p.disclosureId}});
  f.sql.run('DELETE FROM '+f.sourceTable);
  expect(await f.create()).toMatchObject({intentId:created.intentId,probabilityDisclosure:created.probabilityDisclosure});
  expect(await f.create('another')).toMatchObject({intentId:created.intentId,probabilityDisclosure:created.probabilityDisclosure});
 });
 it.each(['OFFLINE','COMBAT'] as const)('rolls back %s on policy change during creation',async(consumer)=>{
  const f=await fixture(consumer);await f.publish();
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE '+f.sourceTable+' SET config_json=?',[JSON.stringify({...f.p,appId:'CHANGED'})]);};
  await expect(f.create()).rejects.toMatchObject({code:'reward_disclosure_policy_changed'});
  expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(0);
 });
 it('does not accept a caller policy different from the approved source',async()=>{
  const f=await fixture();await f.publish();
  await expect(createOfflineAdIntent(f.env.DB,'a','DEV',{...f.p,weightsBps:[10000,0,0]},'mismatch')).rejects.toMatchObject({code:'reward_disclosure_policy_changed'});
  expect(f.scalar('SELECT count(*) FROM offline_ad_intents')).toBe(0);
 });
 it('blocks missing public snapshots through real HTTP without consuming basic rewards',async()=>{
  const f=await fixture();
  const r=await game.request('/v1/offline-rewards/ad-intents',{method:'POST',headers:{...f.headers,'Idempotency-Key':'create'}},f.env);
  expect(r.status).toBe(503);expect(await r.json()).toMatchObject({errorCode:'reward_disclosure_unavailable'});
  expect((await game.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'basic'}},f.env)).status).toBe(200);
 });
 it('rejects corrupt snapshots and prevents a pinned policy from being rewritten',async()=>{
  const f=await fixture();await f.publish();const i=await f.create();
  expect(()=>f.sql.run("UPDATE offline_ad_intents SET probability_disclosure_id=NULL")).toThrow('reward_disclosure_pin_immutable');
  expect(()=>f.sql.run("UPDATE offline_ad_intents SET policy_json='{}'")).toThrow('reward_disclosure_pin_immutable');
  expect((await readOfflineAdIntent(f.env.DB,'a',i.intentId)).probabilityDisclosure.status).toBe('PINNED');
  const other=await fixture();await other.publish();
  other.sql.run('DROP TRIGGER probability_disclosure_no_update');other.sql.run("UPDATE probability_disclosures SET payload_json='{}'");
  await expect(other.create()).rejects.toMatchObject({code:'reward_disclosure_unavailable'});
 });
});

it('migrates old opportunities without inventing a public link and replays them without current policies',async()=>{
 const SQL=await initSqlJs(),sql=new SQL.Database();
 try{
  sql.create_function('unixepoch',()=>Date.now()/1000);sql.run('PRAGMA foreign_keys=ON');
  for(const name of readdirSync('packages/db/migrations').filter(n=>/^\d+_.+\.sql$/u.test(n)&&n<'0047').sort())
   sql.run(readFileSync(join('packages/db/migrations',name),'utf8'));
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
  sql.run("INSERT INTO offline_ad_intents(id,account_id,environment,create_key,baseline_ms,baseline_revision,created_at,policy_json,base_json,metadata_json,story_progress_id,narrative_version,status,origin,write_revision) VALUES('legacy','a','DEV','old',0,0,0,?,'[]','{}','none','v','AD_PENDING','PURCHASE_ONLY',1)",[JSON.stringify({version:'OLD',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000]})]);
  sql.run("INSERT INTO offline_ad_create_receipts VALUES('a','old','legacy')");
  sql.run(readFileSync('packages/db/migrations/0047_reward_disclosure_pins.sql','utf8'));
  const db=new SqlJsD1Database(sql) as unknown as D1Database;
  const replay=await createOfflineAdIntent(db,'a','DEV',null,'old');
  expect(replay.intentId).toBe('legacy');expect(replay.probabilityDisclosure).toEqual({status:'LEGACY_UNLINKED',disclosureId:null,sourceDigest:null,publicPath:null});
  expect(sql.exec('SELECT count(*) FROM probability_disclosures')[0]!.values[0]![0]).toBe(0);
 }finally{sql.close();}
});

it.each(['OFFLINE','COMBAT'] as const)('recovers a concurrent %s winner when source disappears after the last replay check',async(consumer)=>{
 const f=await fixture(consumer);await f.publish();const prepare=f.env.DB.prepare.bind(f.env.DB);
 let intercept=true,winner:Awaited<ReturnType<typeof f.create>>|undefined;
 f.env.DB.prepare=(sql:string)=>{
  const statement=prepare(sql);
  if(sql.startsWith(consumer==='OFFLINE'?'SELECT * FROM offline_ad_intents WHERE account_id=? AND baseline_ms=?':'SELECT * FROM combat_ad_intents WHERE preview_id=?')&&intercept){
   const first=statement.first.bind(statement);
   statement.first=async <T>()=>{const result=await first<T>();if(intercept){intercept=false;winner=await f.create('winner');f.sql.run('DELETE FROM '+f.sourceTable);}return result;};
  }
  return statement;
 };
 const result=await f.create('loser');
 expect(result.intentId).toBe(winner!.intentId);expect(result.probabilityDisclosure).toEqual(winner!.probabilityDisclosure);
 expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(1);
});
it.each(['OFFLINE','COMBAT'] as const)('recovers a concurrent %s winner after the disclosure check but before its write guard',async(consumer)=>{
 const f=await fixture(consumer);await f.publish();const prepare=f.env.DB.prepare.bind(f.env.DB);
 let intercept=true,winner:Awaited<ReturnType<typeof f.create>>|undefined;
 f.env.DB.prepare=(sql:string)=>{
  const statement=prepare(sql);
  if(sql.startsWith('SELECT disclosure_id,payload_json,payload_digest FROM probability_disclosures')&&intercept){
   const first=statement.first.bind(statement);
   statement.first=async <T>()=>{const result=await first<T>();if(intercept){intercept=false;winner=await f.create();f.sql.run('DELETE FROM '+f.sourceTable);}return result;};
  }
  return statement;
 };
 const result=await f.create();
 expect(result.intentId).toBe(winner!.intentId);expect(result.probabilityDisclosure).toEqual(winner!.probabilityDisclosure);
 expect(f.scalar('SELECT count(*) FROM '+f.table)).toBe(1);
});
