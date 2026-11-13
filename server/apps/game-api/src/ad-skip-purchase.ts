import {maintenanceAdmissionStatement,maintenanceContext,maintenanceAdmissionError} from './maintenance-admission';
import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readAdSkipCatalog,adSkipCatalogSchema } from "./ad-skip-policy";
import { verifiedStoreProof,storeOrderInsert,type StoreIdentity } from "./crystal-orders";
import { readAdSkipEntitlement } from "./ad-skip-entitlement";
import { adSkipHash,adSkipReplay,adSkipReceipt,adSkipMutation,adSkipRetry,type AdSkipActor } from "./ad-skip-storage";
export type AdSkipPurchaseVerifier=(input:Pick<StoreIdentity,'accountId'|'environment'|'provider'> & Partial<Pick<StoreIdentity,'appId'|'productId'>> & {receipt:string;binding?:string;now:number})=>Promise<unknown>;
export type AdSkipOptions=AdSkipActor & {verifier?:AdSkipPurchaseVerifier|undefined};
export type AdSkipPurchaseAction={kind:"CHECKOUT";catalogVersion:string;provider:"GOOGLE_PLAY"|"APP_STORE"}
  |{kind:"VERIFY";checkoutIntentId:string;receipt:string}|{kind:"RESTORE";provider:"GOOGLE_PLAY"|"APP_STORE";receipt:string};
type Checkout={id:string;catalog_json:string;binding:string;created_at:number;purchase_start_before:number};
const verifiedExtra=z.object({state:z.literal("PURCHASED"),binding:z.string().min(1).max(256)});
export async function purchaseAdSkip(o:AdSkipOptions,action:AdSkipPurchaseAction,key:string){
  const hash=await adSkipHash(action);
  return adSkipRetry(async()=>{
    const replay=await adSkipReplay(o,action.kind,key,hash);if(replay)return replay;
    if(!o.verifier)throw new ActivityError(503,"ad_skip_purchase_unconfigured");
    const mutation=await adSkipMutation(o,'AD_SKIP_'+action.kind);
    const current=await readAdSkipEntitlement(o),now=Date.now();
    if(action.kind==='CHECKOUT'){
      const catalog=await readAdSkipCatalog(o.db,o.environment);
      if(!catalog)throw new ActivityError(503,"ad_skip_purchase_unconfigured");
      if(catalog.version!==action.catalogVersion||catalog.provider!==action.provider)throw new ActivityError(409,"ad_skip_catalog_changed");
      if(current?.status==='ACTIVE')throw new ActivityError(409,"ad_skip_already_owned");
      const before=now+catalog.checkoutLifetimeMs;if(!Number.isSafeInteger(before))throw new ActivityError(503,"ad_skip_purchase_unconfigured");
      const id='ad_skip_checkout_'+crypto.randomUUID(),binding=crypto.randomUUID();
      const response={accepted:true,checkoutIntentId:id,storeAccountBinding:binding,productId:catalog.productId,appId:catalog.appId,provider:catalog.provider,purchaseStartBefore:before};
      try { await o.db.batch([maintenanceAdmissionStatement(o.db,maintenanceContext(o.environment)),mutation,o.db.prepare(`INSERT INTO ad_skip_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM account_ad_skip_entitlements WHERE account_id=? AND environment=? AND status='ACTIVE') THEN 1 ELSE 0 END`).bind(o.accountId,o.environment),
        o.db.prepare("INSERT INTO ad_skip_checkout_intents VALUES(?,?,?,?,?,?,?)").bind(id,o.accountId,o.environment,JSON.stringify(catalog),binding,now,before),
        adSkipReceipt(o,action.kind,key,hash,response),o.db.prepare('DELETE FROM ad_skip_guard')]);
      }catch(error){const committed=await adSkipReplay(o,action.kind,key,hash);if(committed)return committed;throw maintenanceAdmissionError(error)??error;}
      return response;
    }
    let checkout:Checkout|null=null;
    if(action.kind==='VERIFY'){
      checkout=await o.db.prepare("SELECT * FROM ad_skip_checkout_intents WHERE id=? AND account_id=? AND environment=?").bind(action.checkoutIntentId,o.accountId,o.environment).first<Checkout>();
      if(!checkout)throw new ActivityError(403,"purchase_binding_mismatch");
    }
    let catalog=checkout?adSkipCatalogSchema.parse(JSON.parse(checkout.catalog_json)):null;
    const verificationInput={accountId:o.accountId,environment:o.environment,
      provider:action.kind==='RESTORE'?action.provider:catalog!.provider,
      ...(catalog?{appId:catalog.appId,productId:catalog.productId}:{}),receipt:action.receipt,...(checkout?{binding:checkout.binding}:{}),now:Date.now()};
    // Restore adapter cryptographically verifies the receipt first. Its returned binding
    // resolves a server-owned historical checkout, never a client-supplied SKU/catalog.
    let raw:unknown;try{raw=await o.verifier(verificationInput);}
    catch{throw new ActivityError(503,"purchase_verification_unavailable");}
    const extra=verifiedExtra.safeParse(raw);
    if(!extra.success)throw new ActivityError(403,"purchase_not_verified");
    if(!checkout)checkout=await o.db.prepare("SELECT * FROM ad_skip_checkout_intents WHERE binding=? AND account_id=? AND environment=?")
      .bind(extra.data.binding,o.accountId,o.environment).first<Checkout>();
    if(!checkout||checkout.binding!==extra.data.binding)throw new ActivityError(403,'purchase_binding_mismatch');
    catalog=adSkipCatalogSchema.parse(JSON.parse(checkout.catalog_json));
    if(action.kind==='RESTORE'&&action.provider!==catalog.provider)throw new ActivityError(403,'purchase_binding_mismatch');
    const expected={accountId:o.accountId,environment:o.environment,provider:catalog.provider,appId:catalog.appId,productId:catalog.productId};
    const proof=verifiedStoreProof(raw,expected,Date.now());
    if(proof.purchasedAt<checkout.created_at||proof.purchasedAt>=checkout.purchase_start_before)
      throw new ActivityError(403,"purchase_binding_mismatch");
    const existing=await o.db.prepare("SELECT * FROM verified_store_orders WHERE provider=? AND app_id=? AND transaction_id=?")
      .bind(proof.provider,proof.appId,proof.transactionId).first<{id:string;account_id:string;environment:string;product_kind:string;product_id:string;revoked_at:number|null}>();
    if(existing&&(existing.account_id!==o.accountId||existing.environment!==o.environment||existing.product_kind!=='AD_SKIP_PERMANENT'||existing.product_id!==proof.productId))throw new ActivityError(403,"purchase_binding_mismatch");
    const revoked=await o.db.prepare("SELECT 1 FROM ad_skip_revocations WHERE provider=? AND app_id=? AND transaction_id=?").bind(proof.provider,proof.appId,proof.transactionId).first();
    if(revoked||existing?.revoked_at!=null)throw new ActivityError(403,"ad_skip_entitlement_revoked");
    if(current?.status==='ACTIVE'&&current.order_id!==existing?.id)throw new ActivityError(409,"ad_skip_already_owned");
    const order=existing?null:storeOrderInsert(o.db,proof,'AD_SKIP_PERMANENT',checkout.id),orderId=existing?.id??order!.orderId;
    const revision=current?.order_id===orderId&&current.status==='ACTIVE'?current.revision:(current?.revision??0)+1;
    const response={accepted:true,orderRef:orderId,entitlementRevision:revision,operation:action.kind};
    await o.db.batch([mutation,o.db.prepare(`INSERT INTO ad_skip_guard SELECT CASE WHEN
      NOT EXISTS(SELECT 1 FROM ad_skip_revocations WHERE provider=? AND app_id=? AND transaction_id=?)
      AND NOT EXISTS(SELECT 1 FROM verified_store_orders WHERE id=? AND revoked_at IS NOT NULL)
      AND COALESCE((SELECT revision FROM account_ad_skip_entitlements WHERE account_id=? AND environment=?),0)=?
      THEN 1 ELSE 0 END`).bind(proof.provider,proof.appId,proof.transactionId,orderId,o.accountId,o.environment,current?.revision??0),
      ...(order?[order.statement]:[]),o.db.prepare(`INSERT INTO account_ad_skip_entitlements VALUES(?,?,'ACTIVE',?,?,?)
        ON CONFLICT(account_id,environment) DO UPDATE SET status='ACTIVE',revision=excluded.revision,order_id=excluded.order_id,updated_at=excluded.updated_at`)
        .bind(o.accountId,o.environment,revision,orderId,Date.now()),adSkipReceipt(o,action.kind,key,hash,response),o.db.prepare('DELETE FROM ad_skip_guard')]);
    return response;
  });
}
