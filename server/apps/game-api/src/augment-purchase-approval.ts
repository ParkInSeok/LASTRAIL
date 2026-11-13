import {z} from "zod";
import {canonicalJson} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {readAdSkipEntitlement} from "./ad-skip-entitlement";
import {adSkipMutation} from "./ad-skip-storage";
import type {ExtraTarget,ExtraApproval} from "./solo-augment-selection";
/** Existing verified purchase only; does not purchase, grant ownership or execute a batch.
 * Caller must atomically guard the grant/run and save these statements with its command receipt. */
export async function prepareAugmentPurchase(db:D1Database,actor:{accountId:string;environment:string},target:ExtraTarget){
 const environment=z.enum(["LOCAL","DEV","PRD"]).parse(actor.environment);
 if(actor.accountId!==target.accountId||environment!==target.environment)throw new ActivityError(403,"augment_purchase_owner_mismatch");
 const owner={db,accountId:actor.accountId,environment},e=await readAdSkipEntitlement(owner);
 if(!e)throw new ActivityError(403,"augment_purchase_entitlement_required");
 if(e.status!=="ACTIVE")throw new ActivityError(403,"augment_purchase_entitlement_revoked");
 const approval:ExtraApproval={target,approvalId:"augment_approval_"+crypto.randomUUID(),cause:"PURCHASE_ENTITLEMENT"};
 const assertions=[db.prepare("INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM account_ad_skip_entitlements e JOIN verified_store_orders o ON o.id=e.order_id JOIN accounts a ON a.id=e.account_id WHERE e.account_id=? AND e.environment=? AND e.status='ACTIVE' AND e.revision=? AND e.order_id=? AND o.account_id=e.account_id AND o.environment=e.environment AND o.product_kind='AD_SKIP_PERMANENT' AND o.revoked_at IS NULL AND a.status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.accountId,environment,e.revision,e.order_id),
  await adSkipMutation(owner,"AUGMENT_PURCHASE_APPROVAL"),
  db.prepare("INSERT INTO augment_extra_approvals(id,run_id,event_id,slot_index,account_id,environment,target_json,cause,order_id,entitlement_revision,approved_at) VALUES(?,?,?,?,?,?,?,'PURCHASE_ENTITLEMENT',?,?,?)").bind(approval.approvalId,target.runId,target.eventId,target.slotIndex,actor.accountId,environment,canonicalJson(target),e.order_id,e.revision,Date.now())];
 return {approval,assertions};
}
