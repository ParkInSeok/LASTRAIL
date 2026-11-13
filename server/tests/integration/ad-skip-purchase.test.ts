import { describe, it, expect } from "vitest";
import { setup } from "../helpers/departure-fixture";
import app from "../../apps/game-api/src/index";
import * as entitlement from "../../apps/game-api/src/ad-skip-entitlement";
import {reconcileCrystalOrder} from '../../apps/game-api/src/crystal-refunds';
const catalog={version:"TEST_ONLY",provider:"GOOGLE_PLAY",appId:"test.app",productId:"test.permanent",environment:"DEV",checkoutLifetimeMs:60000};
async function purchaseFixture(){
  const f=await setup();
  f.sql.run("INSERT INTO ad_skip_catalogs VALUES('DEV',?)",[JSON.stringify(catalog)]);
  const env={...f.env,AD_SKIP_PURCHASE_VERIFIER:async(input:{receipt:string})=>JSON.parse(input.receipt)};
  const request=async(path:string,body?:object,key=crypto.randomUUID())=>app.request('/v1/ad-skip'+path,{method:body?'POST':'GET',headers:{...f.headers,'Idempotency-Key':key},...(body?{body:JSON.stringify(body)}:{})},env);
  return {...f,env,request};
}

describe("permanent ad skip purchase boundary", () => {
  it('does not activate a delayed proof when its transaction was revoked before first verification',async()=>{
    const {request,env,scalar}=await purchaseFixture();
    const c=await (await request('/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'})).json() as {checkoutIntentId:string;storeAccountBinding:string};
    await entitlement.revokeAdSkipOrder({db:env.DB,accountId:'a',environment:'DEV',verificationToken:'test',verifier:async()=>({accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',transactionId:'cancelled-first',status:'REFUNDED',verifiedAt:Date.now()})});
    const proof={accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'cancelled-first',purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:c.storeAccountBinding};
    expect((await request('/purchases/verify',{checkoutIntentId:c.checkoutIntentId,receipt:JSON.stringify(proof)})).status).toBe(403);
    expect(scalar('SELECT count(*) FROM account_ad_skip_entitlements')).toBe(0);
    expect(scalar('SELECT count(*) FROM verified_store_orders')).toBe(0);
  });
  it('restores an unfinished historical checkout after catalog removal and excludes generic crystal refunds',async()=>{
    const {request,sql,env,scalar}=await purchaseFixture();
    const c=await (await request('/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'})).json() as {checkoutIntentId:string;storeAccountBinding:string};
    const proof={accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'historical',purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:c.storeAccountBinding};
    sql.run('DELETE FROM ad_skip_catalogs');
    const restored=await request('/purchases/restore',{provider:'GOOGLE_PLAY',receipt:JSON.stringify(proof)});
    expect(restored.status).toBe(200);
    const orderId=String(scalar('SELECT id FROM verified_store_orders'));
    expect(()=>sql.run('UPDATE verified_store_orders SET revoked_at=1 WHERE id=?',[orderId])).toThrow('ad_skip_revocation_required');
    await expect(reconcileCrystalOrder({db:env.DB,orderId,environment:'DEV',verificationToken:'test',
      verifier:async input=>({...input,status:'REVOKED' as const,verifiedAt:Date.now()})})).rejects.toThrow('refund_product_kind_mismatch');
    expect(await (await request('')).json()).toMatchObject({owned:true,catalog:null});
  });
  it("preserves revocation across restore and does not revoke a newer replacement order",async()=>{
    const {request,env,scalar}=await purchaseFixture();
    const buy=async(tx:string)=>{
      const c=await (await request('/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'})).json() as {checkoutIntentId:string;storeAccountBinding:string};
      const proof={accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:tx,purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:c.storeAccountBinding};
      const r=await request('/purchases/verify',{checkoutIntentId:c.checkoutIntentId,receipt:JSON.stringify(proof)});
      expect(r.status).toBe(200);return proof;
    };
    const first=await buy('first');
    expect(entitlement.revokeAdSkipOrder).toBeTypeOf('function');
    const revoke=()=>entitlement.revokeAdSkipOrder({db:env.DB,accountId:'a',environment:'DEV',verificationToken:'opaque',
      verifier:async()=>({accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',transactionId:'first',status:'REVOKED',verifiedAt:Date.now()})});
    await revoke();
    expect((await request('/purchases/restore',{provider:'GOOGLE_PLAY',receipt:JSON.stringify(first)})).status).toBe(403);
    expect(await (await request('')).json()).toMatchObject({owned:false,entitlement:{status:'REVOKED',revision:2}});
    await buy('second');await revoke();
    expect(await (await request('')).json()).toMatchObject({owned:true,entitlement:{status:'ACTIVE',revision:3}});
    expect(scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
  });
  it("binds a verified purchase and restores the same order without a reward grant",async()=>{
    const {request,scalar}=await purchaseFixture();
    const checkout=await request('/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'},'checkout');
    expect(checkout.status).toBe(200);
    const c=await checkout.json() as {checkoutIntentId:string;storeAccountBinding:string};
    const proof={accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'test-tx',
      purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:c.storeAccountBinding};
    const body={checkoutIntentId:c.checkoutIntentId,receipt:JSON.stringify(proof)};
    const verified=await request('/purchases/verify',body,'verify');expect(verified.status).toBe(200);
    const original=await verified.json();
    expect(await (await request('/purchases/verify',body,'verify')).json()).toEqual(original);
    expect((await request('/purchases/restore',{provider:'GOOGLE_PLAY',receipt:JSON.stringify(proof)},'restore')).status).toBe(200);
    expect(await (await request('')).json()).toMatchObject({owned:true,entitlement:{status:'ACTIVE',revision:1,permanent:true},purchaseAvailable:false});
    expect(scalar('SELECT count(*) FROM verified_store_orders')).toBe(1);
    expect(scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
  });
  it("rejects another account's proof and requires a real configured verification adapter",async()=>{
    const {request,env,headers}=await purchaseFixture();
    const c=await (await request('/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'})).json() as {checkoutIntentId:string;storeAccountBinding:string};
    const body={checkoutIntentId:c.checkoutIntentId,receipt:JSON.stringify({accountId:'b',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'bad',purchasedAt:Date.now(),verifiedAt:Date.now(),revoked:false,state:'PURCHASED',binding:c.storeAccountBinding})};
    expect((await request('/purchases/verify',body)).status).toBe(403);
    const response=await app.request('/v1/ad-skip/checkout-intents',{method:'POST',headers:{...headers,'Idempotency-Key':'none'},body:JSON.stringify({catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'})},{...env,AD_SKIP_PURCHASE_VERIFIER:undefined});
    expect(response.status).toBe(503);
  });
  it("provides a distinct permanent entitlement store without any seeded rights", async () => {
    const {scalar}=await setup();
    expect(scalar("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='account_ad_skip_entitlements'")).toBe(1);
    expect(scalar("SELECT count(*) FROM account_ad_skip_entitlements")).toBe(0);
  });
  it("reports no ownership and disabled purchase when catalog and verifier are absent", async () => {
    const {env,headers,scalar}=await setup();
    const response=await app.request("/v1/ad-skip",{headers},env);
    expect(response.status).toBe(200);
    expect(await response.json()).toMatchObject({accepted:true,owned:false,entitlement:null,catalog:null,
      purchaseAvailable:false,featureAvailability:{REROLL:"UNSUPPORTED"}});
    const purchase=await app.request("/v1/ad-skip/checkout-intents",{method:"POST",headers:{...headers,"Idempotency-Key":"test"},
      body:JSON.stringify({catalogVersion:"TEST_ONLY",provider:"GOOGLE_PLAY"})},env);
    expect(purchase.status).toBe(503);
    expect(await purchase.json()).toMatchObject({errorCode:"ad_skip_purchase_unconfigured"});
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
  });
});
