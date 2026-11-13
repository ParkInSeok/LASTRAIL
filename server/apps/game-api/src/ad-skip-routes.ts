import type { Hono,Context } from "hono";
import { readBoundedBody,type ActivityAuthResult } from "./activity-routes";
import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readAdSkipCatalog } from "./ad-skip-policy";
import { readAdSkipEntitlement } from "./ad-skip-entitlement";
import { purchaseAdSkip,type AdSkipPurchaseVerifier } from "./ad-skip-purchase";
import {useAdSkip,skipOpportunitySchema} from './ad-skip-uses';
import {rewardContract} from './reward-opportunity-view';
import {readPurchaseRewardPolicy} from './purchase-reward-policy';
const provider=z.enum(['GOOGLE_PLAY','APP_STORE']);
const receipt=z.string().min(1).max(16384);
const checkoutSchema=z.strictObject({catalogVersion:z.string().min(1).max(200),provider});
const verifySchema=z.strictObject({checkoutIntentId:z.string().min(1).max(200),receipt});
const restoreSchema=z.strictObject({provider,receipt});
type Ctx=Context<{Bindings:Env}>;
function reject(c:Ctx,e:unknown){
  if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);
  // Never expose a receipt or DB exception in the response.
  return c.json({accepted:false,errorCode:'ad_skip_temporarily_unavailable'},503);
}
export function registerAdSkipRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
  app.get("/v1/ad-skip",async c=>{
    c.header("Cache-Control","no-store");
    const auth=await authenticate(c);if("errorCode" in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try{
      if(new URL(c.req.url).search||(await readBoundedBody(c))!=='')throw new ActivityError(400,'contract_violation');
      const catalog=await readAdSkipCatalog(c.env.DB,c.env.ENVIRONMENT);
      const e=await readAdSkipEntitlement({db:c.env.DB,accountId:auth.accountId,environment:c.env.ENVIRONMENT});
      const verifier=(c.env as Env & {AD_SKIP_PURCHASE_VERIFIER?:AdSkipPurchaseVerifier}).AD_SKIP_PURCHASE_VERIFIER;
      const offline=await readPurchaseRewardPolicy(c.env.DB,c.env.ENVIRONMENT,'OFFLINE'),combat=await readPurchaseRewardPolicy(c.env.DB,c.env.ENVIRONMENT,'COMBAT');
      return c.json({accepted:true,contractVersion:'ad-skip-v1',owned:e?.status==='ACTIVE',
        entitlement:e?{status:e.status,revision:e.revision,permanent:true}:null,
        catalog:catalog?{version:catalog.version,provider:catalog.provider,appId:catalog.appId,productId:catalog.productId,price:null,priceSource:"STORE_REQUIRED"}:null,
        purchaseAvailable:!!catalog&&!!verifier&&e?.status!=='ACTIVE',
        featureAvailability:{OFFLINE:offline?"AVAILABLE":"UNAVAILABLE",COMBAT:combat?"AVAILABLE":"UNAVAILABLE",REROLL:"UNSUPPORTED"},serverTime:Date.now()});
    }catch(e){return reject(c,e);}
  });
  const handler=(kind:'CHECKOUT'|'VERIFY'|'RESTORE')=>async(c:Ctx)=>{
    c.header('Cache-Control','no-store');
    const auth=await authenticate(c);if("errorCode" in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try{
      const key=c.req.header('Idempotency-Key')??'';
      if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
      if(new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
      let body:unknown;try{body=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'contract_violation');}
      const options={db:c.env.DB,accountId:auth.accountId,environment:c.env.ENVIRONMENT,
        verifier:(c.env as Env & {AD_SKIP_PURCHASE_VERIFIER?:AdSkipPurchaseVerifier}).AD_SKIP_PURCHASE_VERIFIER};
      if(kind==='CHECKOUT'){
        const p=checkoutSchema.safeParse(body);if(!p.success)throw new ActivityError(400,'contract_violation');
        return c.json(await purchaseAdSkip(options,{kind,...p.data},key));
      }
      if(kind==='VERIFY'){
        const p=verifySchema.safeParse(body);if(!p.success)throw new ActivityError(400,'contract_violation');
        return c.json(await purchaseAdSkip(options,{kind,...p.data},key));
      }
      const p=restoreSchema.safeParse(body);if(!p.success)throw new ActivityError(400,'contract_violation');
      return c.json(await purchaseAdSkip(options,{kind,...p.data},key));
    }catch(e){return reject(c,e);}
  };
  app.post('/v1/ad-skip/checkout-intents',handler('CHECKOUT'));
  app.post('/v1/ad-skip/purchases/verify',handler('VERIFY'));
  app.post('/v1/ad-skip/purchases/restore',handler('RESTORE'));
  app.post('/v1/ad-skip/uses/authorize',async c=>{
    c.header('Cache-Control','no-store');
    const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try{
      if(c.req.header('X-Reward-Contract')!==rewardContract)throw new ActivityError(409,'reward_contract_required');
      const key=c.req.header('Idempotency-Key')??'';
      if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
      if(new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
      let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'contract_violation');}
      const p=z.strictObject({opportunity:skipOpportunitySchema}).safeParse(raw);
      if(!p.success)throw new ActivityError(400,'contract_violation');
      return c.json(await useAdSkip({db:c.env.DB,accountId:auth.accountId,environment:c.env.ENVIRONMENT},p.data.opportunity,key));
    }catch(e){if(e instanceof Error&&e.message==='ad_reward_policy_unconfigured')return c.json({accepted:false,errorCode:e.message},503);return reject(c,e);}
  });
}
