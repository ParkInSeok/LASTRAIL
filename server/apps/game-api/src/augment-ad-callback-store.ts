import {readCombatAuthority} from './combat-authority';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {augmentAdSessionSchema,verifyAugmentAdSession} from './augment-ad-session';
import {verifyAdmobCallback} from './admob-ssv';
import {providerReceiptStatement} from './ad-provider-receipts';
import {SoloAugmentSelection,soloSelectionResultSchema,type ExtraApproval} from './solo-augment-selection';
const fail=(code:string):never=>{throw new ActivityError(409,`augment_ad_${code}`);};
/** Internal callback application. Issuer/start authorization and HTTP routing remain separate.
 * The original pending owner's valid evidence can finish during coop recovery; no new
 * input authority is granted to that owner. Review/terminal runs cannot accept new effects. */
export class AugmentAdCallbackStore {
 constructor(private readonly db:D1Database,private readonly trustedKey:(id:string)=>Promise<string|null>,private readonly drawIndex?:(maximum:number)=>number){}
 async apply(intentId:string,environment:string,rawQuery:string){
  const stored=await this.db.prepare('SELECT session_json,status FROM augment_ad_sessions WHERE id=?').bind(intentId).first<{session_json:string;status:string}>();
  if(!stored)return fail('session_missing');
  const session=augmentAdSessionSchema.parse(JSON.parse(stored.session_json)),t=session.target;
  if(session.intentId!==intentId||t.environment!==environment)return fail('environment_mismatch');
  // Authenticate even replays; original accepted results survive expiry/run changes.
  const callback=await verifyAdmobCallback(rawQuery,this.trustedKey,session.binding);
  const replay=async()=>{const old=await this.db.prepare('SELECT provider_transaction_id,result_json FROM augment_ad_results WHERE intent_id=?').bind(intentId).first<{provider_transaction_id:string;result_json:string}>();if(!old)return null;if(old.provider_transaction_id!==callback.transactionId)return fail('callback_conflict');return soloSelectionResultSchema.parse(JSON.parse(old.result_json));};
  const old=await replay();if(old)return old;
  if(stored.status!=='AD_PENDING')return fail('session_closed');
  // Reuse the exact target/time rules, never substitute a client-provided session.
  await verifyAugmentAdSession(rawQuery,this.trustedKey,session,t,environment,Date.now());
  const row=await this.db.prepare('SELECT account_id,environment,checkpoint_json,start_snapshot_json,write_revision FROM solo_augment_grants WHERE run_id=? AND event_id=?').bind(t.runId,t.eventId).first<{account_id:string;environment:string;checkpoint_json:string;start_snapshot_json:string;write_revision:number}>();
  if(!row||row.environment!==environment)return fail('owner_mismatch');
  const mode=readCombatAuthority(JSON.parse(row.start_snapshot_json).admission.combatAuthority).contract?.mode;
  if(mode!=='ORDINARY_COOP'&&(mode!=='ORDINARY_SOLO'||row.account_id!==t.accountId))return fail('owner_mismatch');
  if(row.write_revision>=Number.MAX_SAFE_INTEGER)return fail('revision_exhausted');
  const approval:ExtraApproval={target:t,approvalId:'augment_approval_'+crypto.randomUUID(),cause:'AD_SSV'};
  const model=SoloAugmentSelection.restore(row.checkpoint_json,{approvalAccountId:t.accountId,...(this.drawIndex?{drawIndex:this.drawIndex}:{}),extraApproval:{authorize:async target=>{if(canonicalJson(target)!==canonicalJson(t))return fail('target_mismatch');return approval;}}});
  const result=await model.reroll({accountId:row.account_id,environment},'ad_'+await sha256Hex(intentId),{eventId:t.eventId,expectedRevision:t.grantRevision,slotIndex:t.slotIndex,candidateId:t.candidateId,mode:'EXTRA'});
  if(result.outcome!=='REROLLED')return fail('target_unavailable');
  try{await this.db.batch([
   this.db.prepare("INSERT INTO solo_augment_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM augment_ad_sessions WHERE id=? AND status='AD_PENDING' AND session_json=? AND callback_deadline>=CAST(ROUND((julianday('now')-2440587.5)*86400000) AS INTEGER)) THEN 1 ELSE 0 END").bind(intentId,stored.session_json),
   this.db.prepare("INSERT INTO solo_augment_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM solo_augment_grants g JOIN runs r ON r.id=g.run_id JOIN run_snapshots s ON s.run_id=r.id WHERE g.run_id=? AND g.event_id=? AND g.checkpoint_json=? AND g.write_revision=? AND s.snapshot_json=? AND r.status='ACTIVE') AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) THEN 1 ELSE 0 END").bind(t.runId,t.eventId,row.checkpoint_json,row.write_revision,row.start_snapshot_json,t.runId),
   providerReceiptStatement(this.db,callback.transactionId,'AUGMENT',intentId,t.accountId),
   this.db.prepare("INSERT INTO augment_extra_approvals(id,run_id,event_id,slot_index,account_id,environment,target_json,cause,approved_at,ad_intent_id,provider_transaction_id) VALUES(?,?,?,?,?,?,?,'AD_SSV',?,?,?)").bind(approval.approvalId,t.runId,t.eventId,t.slotIndex,t.accountId,environment,canonicalJson(t),Date.now(),intentId,callback.transactionId),
   this.db.prepare("UPDATE augment_ad_sessions SET status='APPLIED' WHERE id=?").bind(intentId),
   this.db.prepare('UPDATE solo_augment_grants SET checkpoint_json=?,write_revision=write_revision+1 WHERE run_id=? AND event_id=?').bind(model.checkpoint(),t.runId,t.eventId),
   this.db.prepare('INSERT INTO augment_ad_results VALUES(?,?,?)').bind(intentId,callback.transactionId,canonicalJson(result)),
   this.db.prepare('DELETE FROM solo_augment_write_guard')
  ]);}catch(error){const completed=await replay();if(completed)return completed;throw error;}
  return result;
 }
}
