import { ActivityError } from './activity-policy';
import { readAdSkipEntitlement } from './ad-skip-entitlement';
import { adSkipMutation,adSkipRetry,type AdSkipActor } from './ad-skip-storage';
export type RewardConsumer='OFFLINE'|'COMBAT';
export type RewardApproval={cause:'AD_SSV'|'PURCHASE_ENTITLEMENT';approved_at:number;order_id:string|null;entitlement_revision:number|null;provider_transaction_id:string|null};
export async function readRewardApproval(db:D1Database,consumer:RewardConsumer,intent:string){
 return db.prepare('SELECT * FROM reward_opportunity_approvals WHERE consumer=? AND intent_id=?').bind(consumer,intent).first<RewardApproval>();
}
export function ssvApprovalStatement(db:D1Database,consumer:RewardConsumer,intent:string,account:string,environment:string,transaction:string){
 return db.prepare(`INSERT INTO reward_opportunity_approvals VALUES(?,?,?,?,'AD_SSV',?,NULL,NULL,?)`)
  .bind(consumer,intent,account,environment,Date.now(),transaction);
}
export async function authorizeRewardOpportunity(o:AdSkipActor,consumer:RewardConsumer,intent:string){
 return adSkipRetry(async()=>{
  const table=consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents';
  const row=await o.db.prepare(`SELECT * FROM ${table} WHERE id=? AND account_id=? AND environment=?`).bind(intent,o.accountId,o.environment)
    .first<{status:string;baseline_ms:number;baseline_revision:number;base_json:string;balance_version:number;preview_id:string}>();
  if(!row)throw new ActivityError(403,'ad_intent_not_found');
  const prior=await readRewardApproval(o.db,consumer,intent);if(prior)return prior;
  if(consumer==='OFFLINE'){
   const base=await o.db.prepare(`SELECT COALESCE(s.last_offline_reward_claim_at,a.created_at) AS baseline,COALESCE(s.revision,0) AS revision
    FROM accounts a LEFT JOIN account_offline_reward_state s ON s.account_id=a.id WHERE a.id=? AND a.status='ACTIVE'`).bind(o.accountId).first<{baseline:number;revision:number}>();
   if(!base||base.baseline!==row.baseline_ms||base.revision!==row.baseline_revision)throw new ActivityError(409,'ad_supply_already_consumed');
  }
  const mutation=await adSkipMutation(o,'PURCHASE_REWARD_APPROVAL'),e=await readAdSkipEntitlement(o);
  if(!e||e.status!=='ACTIVE')throw new ActivityError(403,e?'ad_skip_entitlement_revoked':'ad_skip_entitlement_required');
  const eligible=consumer==='OFFLINE'?o.db.prepare(`INSERT INTO ad_skip_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts a
   LEFT JOIN account_offline_reward_state s ON s.account_id=a.id WHERE a.id=? AND a.status='ACTIVE'
   AND COALESCE(s.last_offline_reward_claim_at,a.created_at)=? AND COALESCE(s.revision,0)=?) THEN 1 ELSE 0 END`).bind(o.accountId,row.baseline_ms,row.baseline_revision)
   :o.db.prepare(`INSERT INTO ad_skip_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM run_reward_previews p JOIN runs r ON r.id=p.run_id
    JOIN run_final_results f ON f.run_id=r.id WHERE p.id=? AND p.account_id=? AND p.status='ACTIVE' AND r.status='COMPLETED' AND f.outcome='VICTORY'
    AND p.reward_plan_json=? AND p.balance_config_version=? AND p.expires_at>CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)) THEN 1 ELSE 0 END`)
     .bind(row.preview_id,o.accountId,row.base_json,row.balance_version);
  await o.db.batch([mutation,eligible,o.db.prepare(`INSERT INTO ad_skip_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM account_ad_skip_entitlements e
    JOIN verified_store_orders s ON s.id=e.order_id AND s.account_id=e.account_id AND s.environment=e.environment
    WHERE e.account_id=? AND e.environment=? AND e.status='ACTIVE' AND e.revision=? AND e.order_id=? AND s.revoked_at IS NULL)
    AND EXISTS(SELECT 1 FROM ${table} WHERE id=? AND status='AD_PENDING') THEN 1 ELSE 0 END`).bind(o.accountId,o.environment,e.revision,e.order_id,intent),
   o.db.prepare(`INSERT INTO reward_opportunity_approvals VALUES(?,?,?,?,'PURCHASE_ENTITLEMENT',?,?,?,NULL)`).bind(consumer,intent,o.accountId,o.environment,Date.now(),e.order_id,e.revision),
   o.db.prepare(`UPDATE ${table} SET status='AD_VERIFIED',write_revision=write_revision+1${consumer==='OFFLINE'?',verified_at=?':''} WHERE id=?`)
    .bind(...(consumer==='OFFLINE'?[Date.now(),intent]:[intent])),o.db.prepare('DELETE FROM ad_skip_guard')]);
  return (await readRewardApproval(o.db,consumer,intent))!;
 });
}
