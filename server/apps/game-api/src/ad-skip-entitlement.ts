import { adSkipMutation,adSkipRetry,type AdSkipActor } from "./ad-skip-storage";
import { ActivityError } from "./activity-policy";
import { z } from "zod";
export type AdSkipEntitlement={status:"ACTIVE"|"REVOKED";revision:number;order_id:string};
export async function readAdSkipEntitlement(o:AdSkipActor){
  return o.db.prepare(`SELECT e.status,e.revision,e.order_id FROM account_ad_skip_entitlements e
    JOIN verified_store_orders s ON s.id=e.order_id AND s.account_id=e.account_id AND s.environment=e.environment
    WHERE e.account_id=? AND e.environment=?`).bind(o.accountId,o.environment).first<AdSkipEntitlement>();
}
const revocation=z.object({accountId:z.string().min(1),environment:z.enum(['LOCAL','DEV','PRD']),provider:z.enum(['GOOGLE_PLAY','APP_STORE']),
  appId:z.string().min(1).max(200),transactionId:z.string().min(1).max(512),status:z.enum(['REVOKED','REFUNDED']),verifiedAt:z.number().int().nonnegative()});
export type AdSkipRevocationVerifier=(input:{accountId:string;environment:string;verificationToken:string;now:number})=>Promise<unknown>;
/** Server-only authenticated Store adapter boundary, never a player route. */
export async function revokeAdSkipOrder(o:AdSkipActor & {verificationToken:string;verifier?:AdSkipRevocationVerifier}){
  if(!o.verifier)throw new ActivityError(503,'refund_verifier_unconfigured');
  let raw:unknown;try{raw=await o.verifier({accountId:o.accountId,environment:o.environment,verificationToken:o.verificationToken,now:Date.now()});}
  catch{throw new ActivityError(503,'refund_verification_unavailable');}
  const parsed=revocation.safeParse(raw),now=Date.now();
  if(!parsed.success||parsed.data.accountId!==o.accountId||parsed.data.environment!==o.environment||parsed.data.verifiedAt>now||parsed.data.verifiedAt<now-60000)
    throw new ActivityError(403,'refund_not_verified');
  const p=parsed.data;
  return adSkipRetry(async()=>{
    const mutation=await adSkipMutation(o,'AD_SKIP_REVOKE');
    const prior=await o.db.prepare('SELECT account_id,environment FROM ad_skip_revocations WHERE provider=? AND app_id=? AND transaction_id=?')
      .bind(p.provider,p.appId,p.transactionId).first<{account_id:string;environment:string}>();
    if(prior){if(prior.account_id!==o.accountId||prior.environment!==o.environment)throw new ActivityError(403,'refund_order_invalid');return {accepted:true};}
    const order=await o.db.prepare('SELECT id,account_id,environment,product_kind FROM verified_store_orders WHERE provider=? AND app_id=? AND transaction_id=?')
      .bind(p.provider,p.appId,p.transactionId).first<{id:string;account_id:string;environment:string;product_kind:string}>();
    if(order&&(order.account_id!==o.accountId||order.environment!==o.environment||order.product_kind!=='AD_SKIP_PERMANENT'))throw new ActivityError(403,'refund_order_invalid');
    await o.db.batch([mutation,o.db.prepare('INSERT INTO ad_skip_revocations VALUES(?,?,?,?,?,?)').bind(p.provider,p.appId,p.transactionId,o.accountId,o.environment,p.verifiedAt),
      o.db.prepare(`UPDATE verified_store_orders SET revoked_at=COALESCE(revoked_at,?) WHERE provider=? AND app_id=? AND transaction_id=? AND account_id=? AND environment=? AND product_kind='AD_SKIP_PERMANENT'`)
        .bind(p.verifiedAt,p.provider,p.appId,p.transactionId,o.accountId,o.environment),
      o.db.prepare(`UPDATE account_ad_skip_entitlements SET status='REVOKED',revision=revision+1,updated_at=? WHERE account_id=? AND environment=? AND status='ACTIVE'
        AND order_id IN (SELECT id FROM verified_store_orders WHERE provider=? AND app_id=? AND transaction_id=?)`)
        .bind(p.verifiedAt,o.accountId,o.environment,p.provider,p.appId,p.transactionId)]);
    return {accepted:true};
  });
}
