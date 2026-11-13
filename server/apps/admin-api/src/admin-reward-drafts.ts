import {z} from 'zod';
import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {ActivityError} from '../../game-api/src/activity-policy';
import {AdminRewardTracking,rewardTrackingProposalSchema,rewardManagementWritePermission,rewardDraftContractVersion} from './admin-reward-tracking';
export const rewardDraftPermission=rewardManagementWritePermission;
export const rewardDraftVersion=rewardDraftContractVersion;
export class RewardDraftNotFound extends Error{
 readonly status=404;readonly code='reward_draft_not_found';
 constructor(){super('reward_draft_not_found');}
}
type Actor={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
const id=z.string().trim().min(1).max(128);
const saveCommand=z.strictObject({command:z.literal('SAVE_DRAFT'),expectedRevision:z.number().int().positive().safe().nullable(),
 reason:z.string().trim().min(1).max(500),proposal:rewardTrackingProposalSchema});
type Preview=Awaited<ReturnType<AdminRewardTracking['preview']>>;
type DraftResponse={accepted:true;contractVersion:string;environment:string;batchId:string;revision:number;status:'DRAFT';
 reviewApproved:false;executable:false;preview:Preview;updatedAt:number;auditId:string;requestId:string};
/** A proposal owner only. Current account observations remain explicitly unconfirmed;
 * saving them does not approve targets, review a payout or mint an entitlement. */
export class AdminRewardDrafts{
 constructor(private readonly db:D1Database,private readonly environment:string){
  if(!['DEV','PRD'].includes(environment))throw new ActivityError(503,'reward_tracking_environment_unavailable');
 }
 private async authorize(actor:Actor){
  if(!['OWNER','OPERATOR'].includes(actor.role)||!await this.db.prepare(
   "SELECT 1 FROM admin_principals WHERE id=? AND role=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR')")
   .bind(actor.id,actor.role).first())throw new ActivityError(403,'ADMIN_PERMISSION_DENIED');
 }
 private async revision(batchId:string){
  return (await this.db.prepare('SELECT revision FROM admin_reward_draft_heads WHERE environment=? AND batch_id=?')
   .bind(this.environment,batchId).first<{revision:number}>())?.revision??null;
 }
 async read(principalId:string,batchId:string){
  if(!id.safeParse(batchId).success)throw new ActivityError(400,'contract_violation');
  const row=await this.db.prepare(`SELECT r.revision,r.document_json,r.created_at FROM admin_principals p
   LEFT JOIN admin_reward_draft_heads h ON h.environment=? AND h.batch_id=?
   LEFT JOIN admin_reward_draft_revisions r ON r.environment=h.environment AND r.batch_id=h.batch_id AND r.revision=h.revision
   WHERE p.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR','VIEWER')`)
   .bind(this.environment,batchId,principalId).first<{revision:number|null;document_json:string|null;created_at:number|null}>();
  if(!row)throw new ActivityError(403,'ADMIN_PERMISSION_DENIED');
  if(row.document_json===null||row.revision===null)throw new RewardDraftNotFound();
  const preview=JSON.parse(row.document_json) as Preview,proposal=rewardTrackingProposalSchema.parse(preview.proposal);
  if(proposal.batchId!==batchId||proposal.environment!==this.environment||proposal.revision!==row.revision||
   preview.reviewApproved!==false||preview.executable!==false||
   await sha256Hex(canonicalJson({contractVersion:preview.contractVersion,proposal,observations:preview.observations}))!==preview.reviewHash)
   throw new ActivityError(503,'reward_draft_original_invalid');
  return {contractVersion:rewardDraftVersion,environment:this.environment,batchId,revision:row.revision,status:'DRAFT',
   preview,updatedAt:row.created_at,observationsCurrent:false,reviewApproved:false,executable:false};
 }
 private async replay(actor:Actor,key:string,hash:string):Promise<DraftResponse|null>{
  const row=await this.db.prepare('SELECT payload_hash,response_json FROM admin_reward_draft_receipts WHERE principal_id=? AND environment=? AND request_key=?')
   .bind(actor.id,this.environment,key).first<{payload_hash:string;response_json:string}>();
  if(!row)return null;
  if(row.payload_hash!==hash)throw new ActivityError(409,'idempotency_conflict');
  return JSON.parse(row.response_json) as DraftResponse;
 }
 async save(actor:Actor,batchId:string,input:unknown,key:string):Promise<DraftResponse>{
  if(!id.safeParse(batchId).success||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'contract_violation');
  await this.authorize(actor);
  const parsed=saveCommand.safeParse(input);if(!parsed.success)throw new ActivityError(400,'contract_violation');
  const command=parsed.data;
  if(command.proposal.environment!==this.environment||command.proposal.batchId!==batchId)throw new ActivityError(409,'reward_draft_scope_conflict');
  const hash=await sha256Hex(canonicalJson({batchId,command})),old=await this.replay(actor,key,hash);if(old)return old;
  const current=await this.revision(batchId),next=(current??0)+1;
  if(current!==command.expectedRevision||!Number.isSafeInteger(next)||command.proposal.revision!==next){
   const committed=await this.replay(actor,key,hash);if(committed)return committed;
   throw new ActivityError(409,'reward_draft_revision_conflict');
  }
  const preview=await new AdminRewardTracking(this.db,this.environment).preview(actor.id,command.proposal);
  const now=Date.now(),auditId=createRequestId(),requestId=createRequestId();
  const response:DraftResponse={accepted:true,contractVersion:rewardDraftVersion,environment:this.environment,batchId,revision:next,
   status:'DRAFT',reviewApproved:false,executable:false,preview,updatedAt:now,auditId,requestId};
  try{
   await this.db.batch([
    this.db.prepare("INSERT INTO admin_reward_draft_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR')) THEN 1 ELSE 0 END").bind(actor.id,actor.role),
    this.db.prepare(`INSERT INTO admin_reward_draft_guard SELECT CASE WHEN
     (? IS NULL AND NOT EXISTS(SELECT 1 FROM admin_reward_draft_heads WHERE environment=? AND batch_id=?))
     OR EXISTS(SELECT 1 FROM admin_reward_draft_heads WHERE environment=? AND batch_id=? AND revision=?) THEN 1 ELSE 0 END`)
     .bind(command.expectedRevision,this.environment,batchId,this.environment,batchId,command.expectedRevision),
    this.db.prepare("INSERT INTO admin_reward_draft_revisions VALUES(?,?,?,'DRAFT',?,?,?)")
     .bind(this.environment,batchId,next,canonicalJson(preview),actor.id,now),
    this.db.prepare('INSERT INTO admin_reward_draft_heads VALUES(?,?,?) ON CONFLICT(environment,batch_id) DO UPDATE SET revision=excluded.revision')
     .bind(this.environment,batchId,next),
    this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,created_at)
     VALUES(?,?,?,?,'SAVE_DRAFT','REWARD_DRAFT',?,?,?,?,'SUCCESS',?)`)
     .bind(auditId,actor.id,actor.role,rewardDraftPermission,batchId,requestId,hash,
      canonicalJson({environment:this.environment,beforeRevision:current,afterRevision:next,reason:command.reason,status:'DRAFT',payoutExecuted:false}),now),
    this.db.prepare('INSERT INTO admin_reward_draft_receipts VALUES(?,?,?,?,?)').bind(actor.id,this.environment,key,hash,canonicalJson(response)),
    this.db.prepare('DELETE FROM admin_reward_draft_guard')
   ]);
  }catch{
   await this.authorize(actor);
   const winner=await this.replay(actor,key,hash);if(winner)return winner;
   if(await this.revision(batchId)!==current)throw new ActivityError(409,'reward_draft_revision_conflict');
   throw new ActivityError(503,'reward_draft_unavailable');
  }
  return response;
 }
}
