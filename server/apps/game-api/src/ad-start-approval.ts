import {maintenanceAdmissionStatement,maintenanceContext,maintenanceAdmissionError} from './maintenance-admission';
import {ActivityError} from './activity-policy';
import {readAdRuntimePolicy,requireNewAd} from './ad-runtime-policy';
type Consumer='OFFLINE'|'COMBAT';
export const adStartContract='ad-start-v1';
type StartRow={consumer:Consumer;intent_id:string;account_id:string;environment:string;request_key:string;
 runtime_policy_revision:number;approved_at:number;expires_at:number};
function result(row:StartRow,replay:boolean) {
 return {accepted:true,contractVersion:adStartContract,consumer:row.consumer,intentId:row.intent_id,
  startStatus:replay?'ALREADY_APPROVED':'APPROVED',mayShow:!replay,
  runtimePolicyRevision:row.runtime_policy_revision,approvedAt:row.approved_at,expiresAt:row.expires_at};
}
/** A start grant permits at most the first explicit SDK show. Replays are recovery,
 * never a second show grant. No callback deadline is extended by this operation. */
export async function approveAdStart(db:D1Database,account:string,environment:string,consumer:Consumer,intent:string,key:string) {
 const prior=await db.prepare('SELECT * FROM ad_start_approvals WHERE account_id=? AND environment=? AND request_key=?')
  .bind(account,environment,key).first<StartRow>();
 if(prior){if(prior.consumer!==consumer||prior.intent_id!==intent)throw new ActivityError(409,'idempotency_conflict');return result(prior,true);}
 const table=consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents';
 const row=await db.prepare(`SELECT origin,status,created_at,expires_at FROM ${table} WHERE id=? AND account_id=? AND environment=?`)
  .bind(intent,account,environment).first<{origin:string;status:string;created_at:number;expires_at:number|null}>();
 if(!row)throw new ActivityError(403,'ad_intent_not_found');
 if(row.origin!=='AD_REQUEST'||row.status!=='AD_PENDING'||row.expires_at===null||row.expires_at<Date.now())throw new ActivityError(409,'ad_start_not_available');
 await requireNewAd(db,environment);
 const policy=await readAdRuntimePolicy(db,environment);
 const approved:StartRow={consumer,intent_id:intent,account_id:account,environment,request_key:key,
  runtime_policy_revision:policy.runtimePolicyRevision??0,approved_at:Date.now(),expires_at:row.expires_at};
 try {
  await db.batch([maintenanceAdmissionStatement(db,maintenanceContext(environment)),db.prepare('INSERT INTO ad_start_approvals VALUES(?,?,?,?,?,?,?,?)')
   .bind(consumer,intent,account,environment,key,approved.runtime_policy_revision,approved.approved_at,approved.expires_at)]);
 }catch(error){
  const committed=await db.prepare('SELECT * FROM ad_start_approvals WHERE account_id=? AND environment=? AND request_key=?')
   .bind(account,environment,key).first<StartRow>();
  if(committed){if(committed.consumer!==consumer||committed.intent_id!==intent)throw new ActivityError(409,'idempotency_conflict');return result(committed,true);}
  const maintenanceError=maintenanceAdmissionError(error);if(maintenanceError)throw maintenanceError;
  if(String(error).includes('ad_runtime_policy_blocked'))throw new ActivityError(409,'ad_runtime_policy_changed');
  if(/ad_start_not_available|UNIQUE constraint failed/u.test(String(error)))throw new ActivityError(409,'ad_start_not_available');
  throw error;
 }
 return result(approved,false);
}
/** Caller has already cryptographically verified the original challenge/binding. */
export async function requireAdStartForCallback(db:D1Database,consumer:Consumer,intent:string,account:string,environment:string,timestamp:number) {
 const start=await db.prepare('SELECT approved_at,expires_at FROM ad_start_approvals WHERE consumer=? AND intent_id=? AND account_id=? AND environment=?')
  .bind(consumer,intent,account,environment).first<{approved_at:number;expires_at:number}>();
 if(!start)throw new ActivityError(409,'ad_start_not_approved');
 if(timestamp<start.approved_at||timestamp>start.expires_at)throw new ActivityError(409,'ad_verification_expired');
}
