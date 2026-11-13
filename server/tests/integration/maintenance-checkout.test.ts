import {describe,it,expect,vi,beforeEach,afterEach} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {testMaintenancePolicy} from '../helpers/maintenance-policy-fixture';
import {purchaseAdSkip} from '../../apps/game-api/src/ad-skip-purchase';
import {purchasePass50,type Pass50Verifier} from '../../apps/game-api/src/pass50-purchase';
async function checkoutFixture(){
 const f=await setup();
 f.sql.run("INSERT INTO ad_skip_catalogs VALUES('DEV',?)",[JSON.stringify({version:'TEST_ONLY',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',environment:'DEV',checkoutLifetimeMs:60000})]);
 f.sql.run("INSERT INTO recurring_definitions VALUES('pass-test','PASS50',?,?,?)",[Date.parse('2026-08-31T19:00:00Z'),Date.parse('2026-09-30T19:00:00Z'),JSON.stringify({version:'recurring-v1',cosmetics:{FRAME:'TEST_FRAME',OPERATOR_SKIN:'TEST_SKIN',TRAIN_SKIN:'TEST_TRAIN'},purchase:{provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.pass'}})]);
 const skip={db:f.env.DB,accountId:'a',environment:'DEV' as const,verifier:async(input:{receipt:string})=>JSON.parse(input.receipt)};
 const verifier:Pass50Verifier=async input=>({...input,transactionId:'TEST_PASS',purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false});
 return {...f,skip,pass:{db:f.env.DB,accountId:'a',environment:'DEV' as const,verifier}};
}
describe('maintenance checkout admission and recovery',()=>{
 // Test maintenance during the original valid season, preserving the calendar-boundary contract.
 beforeEach(()=>{vi.useFakeTimers({toFake:['Date']});vi.setSystemTime(new Date('2026-09-15T00:00:00Z'));});
 afterEach(()=>vi.useRealTimers());
 it.each(['skip','pass'] as const)('blocks new %s checkout at the commit boundary',async kind=>{
  const f=await checkoutFixture();f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;testMaintenancePolicy(f.sql,true);};
  const request=kind==='skip'?purchaseAdSkip(f.skip,{kind:'CHECKOUT',catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'},'new'):purchasePass50(f.pass,{kind:'INTENT',policyId:'pass-test'},'new');
  await expect(request).rejects.toMatchObject({status:503,code:'maintenance_new_request_blocked'});
  expect(f.scalar('SELECT count(*) FROM '+(kind==='skip'?'ad_skip_checkout_intents':'pass_checkout_intents'))).toBe(0);
 });
 it('keeps verified skip purchase recovery and checkout replay after maintenance',async()=>{
  const f=await checkoutFixture(),action={kind:'CHECKOUT' as const,catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY' as const};
  const first=await purchaseAdSkip(f.skip,action,'same');testMaintenancePolicy(f.sql,true);
  expect(await purchaseAdSkip(f.skip,action,'same')).toEqual(first);
  const receipt=JSON.stringify({accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'TEST_SKIP',purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:first.storeAccountBinding});
  const verify={kind:'VERIFY' as const,checkoutIntentId:String(first.checkoutIntentId),receipt};
  expect(await purchaseAdSkip(f.skip,verify,'verify')).toMatchObject({accepted:true});
  expect(await purchaseAdSkip(f.skip,verify,'verify')).toMatchObject({accepted:true});expect(f.scalar('SELECT count(*) FROM verified_store_orders')).toBe(1);
 });
 it('keeps pass reconciliation and original intent replay after maintenance',async()=>{
  const f=await checkoutFixture(),action={kind:'INTENT' as const,policyId:'pass-test'};
  const first=await purchasePass50(f.pass,action,'same');testMaintenancePolicy(f.sql,true);
  expect(await purchasePass50(f.pass,action,'same')).toEqual(first);
  const verify={kind:'RECONCILE' as const,policyId:'pass-test',intentId:String(first.intentId),receipt:'TEST_ONLY'};
  expect(await purchasePass50(f.pass,verify,'verify')).toMatchObject({accepted:true});
  expect(await purchasePass50(f.pass,verify,'verify')).toMatchObject({accepted:true});expect(f.scalar('SELECT count(*) FROM verified_store_orders')).toBe(1);
 });
 it.each(['skip','pass'] as const)('recovers the committed %s checkout after maintenance and a lost DB response',async kind=>{
  const f=await checkoutFixture(),batch=f.env.DB.batch.bind(f.env.DB);let once=true;
  f.env.DB.batch=async statements=>{const result=await batch(statements);if(once){once=false;testMaintenancePolicy(f.sql,true);throw new Error('maintenance_new_request_blocked');}return result;};
  const result=kind==='skip'?await purchaseAdSkip(f.skip,{kind:'CHECKOUT',catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'},'lost'):await purchasePass50(f.pass,{kind:'INTENT',policyId:'pass-test'},'lost');
  expect(result.accepted).toBe(true);expect(f.scalar('SELECT count(*) FROM '+(kind==='skip'?'ad_skip_checkout_intents':'pass_checkout_intents'))).toBe(1);
 });

});
