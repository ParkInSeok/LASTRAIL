import {publishConfiguredDisclosuresForTest} from '../helpers/published-disclosure-fixture';
import {createStartedOfflineAdIntent as createOfflineAdIntent} from '../helpers/started-ad-fixture';
import {describe,it,expect,vi} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
import {recordOfflineAdVerification,spinOfflineAd} from '../../apps/game-api/src/offline-ad-service';
import {revokeAdSkipOrder} from '../../apps/game-api/src/ad-skip-entitlement';
const adPolicy={version:'TEST_AD',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[10000,0,0],challengeLifetimeMs:60000,callbackGraceMs:60000,maxAdsPerDay:1,dayStartUtcMinutes:0,adUnit:'test-unit',appId:'test-app',rewardItem:'test',rewardAmount:'1'};
const gamePolicy={version:'TEST_ONLY',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000]};
const revoke=(db:D1Database)=>revokeAdSkipOrder({db,accountId:'a',environment:'DEV',verificationToken:'test',verifier:async()=>({accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',transactionId:'skip-tx',status:'REVOKED',verifiedAt:Date.now()})});
async function fixture(){
 const f=await setup();
 f.sql.run("INSERT INTO verified_store_orders VALUES('skip-order','a','GOOGLE_PLAY','test.app','test.permanent','skip-tx','DEV','AD_SKIP_PERMANENT','fixture',0,0,NULL)");
 f.sql.run("INSERT INTO account_ad_skip_entitlements VALUES('a','DEV','ACTIVE',1,'skip-order',0)");
 const headers={...f.headers,'X-Reward-Contract':'reward-opportunity-v2'};
 const authorize=async(opportunity:object,key='authorize')=>{await publishConfiguredDisclosuresForTest(f.env.DB);return app.request('/v1/ad-skip/uses/authorize',{method:'POST',headers:{...headers,'Idempotency-Key':key},body:JSON.stringify({opportunity})},f.env);};
 return {...f,headers,authorize};
}
describe('purchase-approved reward opportunities',()=>{
 it('reports a consumed supply explicitly when basic collection wins before purchase approval',async()=>{
  const f=await fixture(),i=await createOfflineAdIntent(f.env.DB,'a','DEV',adPolicy,'ad');
  expect((await app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'basic'}},f.env)).status).toBe(200);
  const r=await f.authorize({kind:'OFFLINE',intentId:i.intentId});expect(r.status).toBe(409);
  expect(await r.json()).toMatchObject({errorCode:'ad_supply_already_consumed'});
  expect(f.scalar('SELECT count(*) FROM reward_opportunity_approvals')).toBe(0);
 });
 it('merges concurrent SSV and purchase approval before basic/spin compete for one payout',async()=>{
  const f=await fixture(),i=await createOfflineAdIntent(f.env.DB,'a','DEV',adPolicy,'ad');
  const callback={provider:'ADMOB' as const,transactionId:'race-ssv',timestamp:i.createdAt,userId:i.userId!,customData:i.customData!,adUnit:'test-unit'};
  const [purchase]=await Promise.all([f.authorize({kind:'OFFLINE',intentId:i.intentId}),recordOfflineAdVerification(f.env.DB,i.intentId,callback)]);
  expect(purchase.status).toBe(200);
  expect(f.scalar('SELECT count(*) FROM reward_opportunity_approvals')).toBe(1);
  const requests=await Promise.all([
   app.request(`/v1/offline-rewards/ad-intents/${i.intentId}/spin`,{method:'POST',headers:{...f.headers,'Idempotency-Key':'one'}},f.env),
   app.request(`/v1/offline-rewards/ad-intents/${i.intentId}/spin`,{method:'POST',headers:{...f.headers,'Idempotency-Key':'two'}},f.env),
   app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'basic'}},f.env)]);
  expect(requests.some(r=>r.status===200)).toBe(true);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(1);
 });
 it('pins a request key to one opportunity and denies reuse with another kind',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','OFFLINE',?)",[JSON.stringify(gamePolicy)]);
  expect((await f.authorize({kind:'OFFLINE'},'one-key')).status).toBe(200);
  const r=await f.authorize({kind:'COMBAT',runId:'other',rewardPreviewId:'other'},'one-key');
  expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:'idempotency_conflict'});
  expect(f.scalar('SELECT count(*) FROM reward_opportunity_approvals')).toBe(1);
 });
 it('preserves the v2 serializer when an ad creation request is replayed',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO offline_ad_policies VALUES('TEST_AD','DEV','ACTIVE',?,0,'test')",[JSON.stringify(adPolicy)]);
  await publishConfiguredDisclosuresForTest(f.env.DB);
  const create=()=>app.request('/v1/offline-rewards/ad-intents',{method:'POST',headers:{...f.headers,'Idempotency-Key':'v2-create'}},f.env);
  const first=await (await create()).json();
  expect(first).toMatchObject({contractVersion:'reward-opportunity-v2'});
  expect(await (await create()).json()).toEqual(first);
 });
 it('pays combat through the same settlement with no provider after entitlement revocation',async()=>{
  const f=await fixture();
  f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','COMBAT',?)",[JSON.stringify({...gamePolicy,placement:'COMBAT_CLEAR'})]);
  f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('m','a',100,'TEST',0)");
  f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES('r','a','m',1,'COMPLETED',0,1)");
  f.sql.run("INSERT INTO run_final_results VALUES('r','VICTORY',15,'{}',1)");
  const plan={calculation:{operationId:'opn_01_last_departure',outcome:'VICTORY'},rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:240,source:'RUN_CLEAR'},{kind:'CURRENCY',id:'cur_credits',quantity:10,source:'FIRST_CLEAR'}]};
  f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES('p','a','r',1,'ACTIVE',?,?,?)",[JSON.stringify(plan),Date.now()+3600000,Date.now()]);
  const response=await f.authorize({kind:'COMBAT',runId:'r',rewardPreviewId:'p'});expect(response.status).toBe(200);
  const i=await response.json() as {intentId:string};
  await revoke(f.env.DB);
  const spin=await app.request(`/v1/runs/r/reward-preview/p/ad-intents/${i.intentId}/spin`,{method:'POST',headers:{...f.headers,'Idempotency-Key':'spin'}},f.env);
  expect(spin.status).toBe(200);
  const paid=await spin.json() as {multiplier:number;rewardLines:Array<{quantity:number;source:string}>};
  expect(paid.rewardLines.find(l=>l.source==='RUN_CLEAR')?.quantity).toBe(240*paid.multiplier);
  expect(paid.rewardLines.find(l=>l.source==='FIRST_CLEAR')?.quantity).toBe(10);
  expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
 });
 it('recovers a durable supply approval after revocation but refuses any new opportunity',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','OFFLINE',?)",[JSON.stringify(gamePolicy)]);
  const i=await (await f.authorize({kind:'OFFLINE'})).json() as {intentId:string};
  await revoke(f.env.DB);
  expect((await f.authorize({kind:'OFFLINE'})).status).toBe(200);
  const paid=await app.request(`/v1/offline-rewards/ad-intents/${i.intentId}/spin`,{method:'POST',headers:{...f.headers,'Idempotency-Key':'spin'}},f.env);
  expect(paid.status).toBe(200);
  expect((await f.authorize({kind:'OFFLINE'},'new')).status).toBe(403);
 });
 it('converts an expired AD_PENDING without changing its policy and observes only timely genuine SSV',async()=>{
  const f=await fixture();
  const i=await createOfflineAdIntent(f.env.DB,'a','DEV',adPolicy,'old-ad');
  const clock=vi.spyOn(Date,'now').mockReturnValue(i.expiresAt!+60001);
  try{
   // Test the domain directly because the test authentication session has a shorter TTL.
   const {useAdSkip}=await import('../../apps/game-api/src/ad-skip-uses');
   const view=await useAdSkip({db:f.env.DB,accountId:'a',environment:'DEV'},{kind:'OFFLINE',intentId:i.intentId},'convert');
   expect(view).toMatchObject({canSpin:true,probabilities:[{multiplier:2,weightBps:10000},{multiplier:3,weightBps:0},{multiplier:4,weightBps:0}],approval:{cause:'PURCHASE_ENTITLEMENT'}});
   await expect(recordOfflineAdVerification(f.env.DB,i.intentId,{provider:'ADMOB',transactionId:'late',timestamp:i.createdAt,userId:i.userId!,customData:i.customData!,adUnit:'test-unit'})).rejects.toThrow('ad_verification_expired');
   expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(0);
   expect((await spinOfflineAd(f.env.DB,'a',i.intentId,'after-deadline')).multiplier).toBe(2);
  }finally{clock.mockRestore();}
 });
 it('records a timely late SSV once without replacing purchase approval or granting',async()=>{
  const f=await fixture(),i=await createOfflineAdIntent(f.env.DB,'a','DEV',adPolicy,'ad');
  expect((await f.authorize({kind:'OFFLINE',intentId:i.intentId})).status).toBe(200);
  const callback={provider:'ADMOB' as const,transactionId:'late-valid',timestamp:i.createdAt,userId:i.userId!,customData:i.customData!,adUnit:'test-unit'};
  await recordOfflineAdVerification(f.env.DB,i.intentId,callback);await recordOfflineAdVerification(f.env.DB,i.intentId,callback);
  expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(1);
  expect(f.scalar('SELECT cause FROM reward_opportunity_approvals')).toBe('PURCHASE_ENTITLEMENT');
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
  expect(f.scalar("SELECT count(*) FROM offline_ad_intents WHERE origin='AD_REQUEST'")).toBe(1);
 });
 it('rejects old writers, old product clients and unsupported reroll',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','OFFLINE',?)",[JSON.stringify(gamePolicy)]);
  const i=await (await f.authorize({kind:'OFFLINE'})).json() as {intentId:string};
  expect(()=>f.sql.run("UPDATE offline_ad_intents SET status='AD_VERIFIED' WHERE id=?",[i.intentId])).toThrow('reward_writer_upgrade_required');
  const old=await app.request(`/v1/offline-rewards/ad-intents/${i.intentId}`,{headers:{Authorization:f.headers.Authorization}},f.env);
  expect(old.status).toBe(409);
  const unsupported=await f.authorize({kind:'REROLL'},'reroll');expect(unsupported.status).toBe(503);
  expect(await unsupported.json()).toMatchObject({errorCode:'reroll_contract_unavailable'});
 });
 it('does not approve when revocation commits at the approval batch boundary',async()=>{
  const f=await fixture(),i=await createOfflineAdIntent(f.env.DB,'a','DEV',adPolicy,'ad');
  let fired=false;
  f.adapter.beforeBatch=()=>{if(!fired){fired=true;f.sql.run("UPDATE account_ad_skip_entitlements SET status='REVOKED',revision=revision+1 WHERE account_id='a'");}};
  const r=await f.authorize({kind:'OFFLINE',intentId:i.intentId});
  expect(r.status).toBe(403);
  expect(f.scalar('SELECT count(*) FROM reward_opportunity_approvals')).toBe(0);
 });
 it('rejects missing game policy and never grants merely for owning a purchase',async()=>{
  const f=await fixture();
  const r=await f.authorize({kind:'OFFLINE'});expect(r.status).toBe(503);
  expect(await r.json()).toMatchObject({errorCode:'ad_reward_policy_unconfigured'});
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
 });
 it('creates a providerless pinned supply and pays once through the existing spin',async()=>{
  const f=await fixture();
  // Failure is an absent schema/API, not an invented provider or live configuration.
  expect(f.scalar("SELECT count(*) FROM sqlite_master WHERE name='purchase_reward_policies'")).toBe(1);
  f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV','OFFLINE',?)",[JSON.stringify({version:'TEST_ONLY',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000]})]);
  expect(await (await app.request('/v1/ad-skip',{headers:f.headers},f.env)).json()).toMatchObject({featureAvailability:{OFFLINE:'AVAILABLE',COMBAT:'UNAVAILABLE',REROLL:'UNSUPPORTED'}});
  const r=await f.authorize({kind:'OFFLINE'});expect(r.status).toBe(200);
  const intent=await r.json() as {intentId:string;canSpin:boolean};
  expect(intent).toMatchObject({contractVersion:'reward-opportunity-v2',canSpin:true,adContext:null,approval:{cause:'PURCHASE_ENTITLEMENT'}});
  const path=`/v1/offline-rewards/ad-intents/${intent.intentId}/spin`;
  const spin=()=>app.request(path,{method:'POST',headers:{...f.headers,'Idempotency-Key':'spin'}},f.env);
  const paid=await spin();expect(paid.status).toBe(200);
  expect(await (await spin()).json()).toEqual(await paid.json());
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(0);
 });
});
