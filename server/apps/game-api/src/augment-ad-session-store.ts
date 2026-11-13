import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {augmentAdSessionSchema} from './augment-ad-session';
import {SoloAugmentSelection} from './solo-augment-selection';
import {readCombatAuthority} from './combat-authority';
import {CombatHostReportStore} from './combat-host-report-store';
import {maintenanceAdmissionStatement,maintenanceContext} from './maintenance-admission';
const sqlNow="CAST(ROUND((julianday('now')-2440587.5)*86400000) AS INTEGER)";
/** Trusted issuer only: reservation does not authorize an SDK show. Caller must supply
 * an approved start/configuration; public admission and cumulative ad budget are not wired.
 * Future callback application must resolve this lock and consume its grant in one batch. */
export class AugmentAdSessionStore {
 constructor(private readonly db:D1Database){}
 async reserve(input:unknown){
  const session=augmentAdSessionSchema.parse(input),t=session.target,json=canonicalJson(session);
  const replay=async()=>{const row=await this.db.prepare('SELECT session_json FROM augment_ad_sessions WHERE id=?').bind(session.intentId).first<{session_json:string}>();if(!row)return null;if(row.session_json!==json)throw new ActivityError(409,'augment_ad_session_conflict');return augmentAdSessionSchema.parse(JSON.parse(row.session_json));};
  const prior=await replay();if(prior)return prior;
  const row=await this.db.prepare('SELECT account_id,environment,checkpoint_json,start_snapshot_json,write_revision FROM solo_augment_grants WHERE run_id=? AND event_id=?').bind(t.runId,t.eventId).first<{account_id:string;environment:string;checkpoint_json:string;start_snapshot_json:string;write_revision:number}>();
  if(!row||row.environment!==t.environment)throw new ActivityError(403,'augment_ad_owner_mismatch');
  const mode=readCombatAuthority(JSON.parse(row.start_snapshot_json).admission.combatAuthority).contract?.mode;
  if(mode!=='ORDINARY_SOLO'&&mode!=='ORDINARY_COOP')throw new ActivityError(409,'augment_ad_mode_unsupported');
  if(mode==='ORDINARY_SOLO'&&row.account_id!==t.accountId)throw new ActivityError(403,'augment_ad_owner_mismatch');
  const model=SoloAugmentSelection.restore(row.checkpoint_json,{approvalAccountId:t.accountId,extraApproval:{authorize:async()=>{throw Error('unavailable');}}});
  const target=model.previewExtra({accountId:row.account_id,environment:t.environment},{eventId:t.eventId,expectedRevision:t.grantRevision,slotIndex:t.slotIndex,candidateId:t.candidateId});
  if(!target||canonicalJson(target)!==canonicalJson(t))throw new ActivityError(409,'augment_ad_target_mismatch');
  const control=mode==='ORDINARY_COOP'?await new CombatHostReportStore(this.db).prepareControl(t.accountId,t.runId,{kind:'SHARED_GRANT',ownerAccountId:row.account_id}):null;
  try{await this.db.batch([
   maintenanceAdmissionStatement(this.db,maintenanceContext(t.environment)),
   this.db.prepare(`INSERT INTO solo_augment_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM ad_runtime_policies WHERE environment=? AND ads_enabled=1) AND EXISTS(SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE') AND ?<=${sqlNow} AND ?>${sqlNow} THEN 1 ELSE 0 END`).bind(t.environment,t.accountId,session.startedAt,session.expiresAt),
   this.db.prepare("INSERT INTO solo_augment_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM solo_augment_grants g JOIN runs r ON r.id=g.run_id JOIN run_snapshots s ON s.run_id=r.id WHERE g.run_id=? AND g.event_id=? AND g.checkpoint_json=? AND g.write_revision=? AND s.snapshot_json=? AND r.status='ACTIVE') AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) THEN 1 ELSE 0 END").bind(t.runId,t.eventId,row.checkpoint_json,row.write_revision,row.start_snapshot_json,t.runId),
   ...(control?[control.assertion]:[]),
   this.db.prepare("INSERT INTO augment_ad_sessions VALUES(?,?,?,?,?,'AD_PENDING')").bind(session.intentId,t.runId,t.eventId,json,session.callbackDeadline),
   this.db.prepare('DELETE FROM solo_augment_write_guard'),...(control?[control.cleanup]:[])
  ]);}catch(error){const committed=await replay();if(committed)return committed;throw error;}
  return session;
 }
 /** Only a terminal timeout; never restarts the original session or consumes EXTRA. */
 async expire(intentId:string){await this.db.batch([this.db.prepare(`UPDATE augment_ad_sessions SET status='EXPIRED' WHERE id=? AND status='AD_PENDING' AND callback_deadline<${sqlNow}`).bind(intentId)]);}
}
