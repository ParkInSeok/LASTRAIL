import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readCombatAuthority} from './combat-authority';
import {readHostContext} from './combat-host-report';
import {requireRunControl} from './combat-coop-controls';
import {hostRetention} from './combat-host-retention';
import {SoloAugmentSelection,soloSelectionIssueSchema} from './solo-augment-selection';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {captureAugmentTargetReadout} from './augment-target-readout';
const id=z.string().min(1).max(200);
/** Display-only projection from one DB snapshot. Does not authorize a command,
 * start an ad, expire a session, restore combat, or reveal private approval evidence. */
export async function readAugmentView(db:D1Database,sender:string,environment:string,runId:string,eventId:string,serverNow:number){
 [sender,environment,runId,eventId].forEach(value=>id.parse(value));z.number().int().nonnegative().safe().parse(serverNow);
 const row=await db.prepare(`SELECT g.account_id,g.environment,g.stage_id,g.issue_hash,g.checkpoint_json,g.start_snapshot_json,r.status,
 h.context_json,h.recovery_json,h.retention_json,v.run_id AS review_id,a.session_json,a.callback_deadline
 FROM solo_augment_grants g JOIN runs r ON r.id=g.run_id
 LEFT JOIN combat_host_report_states h ON h.run_id=g.run_id
 LEFT JOIN combat_result_reviews v ON v.run_id=g.run_id
 LEFT JOIN augment_ad_sessions a ON a.run_id=g.run_id AND a.event_id=g.event_id AND a.status='AD_PENDING'
 WHERE g.run_id=? AND g.event_id=?`).bind(runId,eventId).first<{account_id:string;environment:string;stage_id:string;issue_hash:string;checkpoint_json:string;start_snapshot_json:string;status:string;context_json:string|null;recovery_json:string|null;retention_json:string|null;review_id:string|null;session_json:string|null;callback_deadline:number|null}>();
 if(!row)throw new ActivityError(409,'augment_view_missing');
 const deny=()=>{throw new ActivityError(403,'augment_view_forbidden');};
 if(row.environment!==environment)deny();
 const pinned=z.object({admission:z.object({combatAuthority:z.unknown()})}).parse(JSON.parse(row.start_snapshot_json));
 const mode=readCombatAuthority(pinned.admission.combatAuthority).contract?.mode;
 const context=mode==='ORDINARY_COOP'&&row.context_json?readHostContext(JSON.parse(row.context_json)):null;
 if(mode==='ORDINARY_SOLO'){if(row.account_id!==sender)deny();}
 else if(mode!=='ORDINARY_COOP'||!context?.participants.some(p=>p.accountId===sender))deny();
 let grant;
 try{
  const saved=z.object({issue:soloSelectionIssueSchema}).parse(JSON.parse(row.checkpoint_json)),issue=saved.issue;
  if(issue.runId!==runId||issue.eventId!==eventId||issue.stageId!==row.stage_id||issue.accountId!==row.account_id
    ||issue.environment!==row.environment||await sha256Hex(canonicalJson(issue))!==row.issue_hash)throw Error('source mismatch');
  grant=SoloAugmentSelection.restore(row.checkpoint_json,{extraApproval:{authorize:async()=>{throw Error('read_only');}}}).read();
 }catch{throw new ActivityError(503,'augment_view_source_conflict');}
 const pending=row.session_json?z.object({target:z.object({accountId:id,slotIndex:z.number().int().min(0).max(2)})}).parse(JSON.parse(row.session_json)):null;
 const active=!context||(context.activeAccountIds??context.participants.map(p=>p.accountId)).includes(sender);
 let selectionControl:'SELF'|'PARTNER'|'PAUSED'|'CLOSED'='SELF';
 if(row.status!=='ACTIVE'||grant.state==='CONFIRMED'||!active)selectionControl='CLOSED';
 else if(row.review_id||row.recovery_json||row.retention_json&&hostRetention.parse(JSON.parse(row.retention_json)).suspended||pending)selectionControl='PAUSED';
 else if(context){try{requireRunControl(context,sender,{kind:'SHARED_GRANT',ownerAccountId:row.account_id});}catch(error){if(error instanceof ActivityError&&error.status===403)selectionControl='PARTNER';else throw error;}}
 return {version:'combat.augment-view.v1' as const,runId,eventId,serverNow,grant,selectionControl,targetReadout:await captureAugmentTargetReadout(grant),
 pendingAd:pending?{slotIndex:pending.target.slotIndex,callbackDeadlineAt:row.callback_deadline,ownedByViewer:pending.target.accountId===sender,phase:serverNow>(row.callback_deadline??0)?'AWAITING_RESOLUTION':'PENDING'}:null};
}
