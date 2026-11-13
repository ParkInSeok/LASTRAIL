import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {NoticeError,hasNoticePermission,noticeCommandSchema,type NoticePrincipal} from '../../../packages/common/src/notice-policy';
import {NoticeStore} from '../../game-api/src/notice-store';
import {saveNoticeCommand} from './notice-service';

export const liveopsVersion='admin-liveops-management.v1.20261020';
export const liveopsCommand=z.discriminatedUnion('kind',[
 z.strictObject({kind:z.literal('REVIEW'),expectedRevision:z.number().int().positive().safe(),reason:z.string().trim().min(1).max(500)}),
 z.strictObject({kind:z.literal('DOMAIN_COMMAND'),reviewId:z.string().min(1).max(100).nullable(),command:noticeCommandSchema})
]);
/** A management projection over existing domains, not a new entitlement owner. */
export function liveopsCapabilities(actor:NoticePrincipal){
 return {contractVersion:liveopsVersion,domains:{
  NOTICE:{supported:true,commands:['SAVE_DRAFT','REVIEW','PUBLISH','SCHEDULE','WITHDRAW'],canEdit:hasNoticePermission(actor,'NOTICE_EDIT'),canReview:hasNoticePermission(actor,'NOTICE_PUBLISH'),canExecute:hasNoticePermission(actor,'NOTICE_PUBLISH')},
  MAINTENANCE:{supported:false,reason:'USE_EXISTING_MAINTENANCE_DRAFT_AND_RUNTIME_OWNERS',path:'/v1/admin/maintenance-plans'},
  SUPPORT:{supported:false,reason:'USE_EXISTING_SUPPORT_PROCESSING_AND_GRANT_AUTHORITY'},
  OPS_INVESTIGATION:{supported:false,reason:'USE_EXISTING_OPS_INVESTIGATION_OWNER',path:'/v1/admin/ops/error-logs'},
  EVENT_REWARDS:{supported:false,reason:'ORIGINAL_EVENT_ENTITLEMENT_TARGET_POLICY_AND_EXECUTOR_NOT_SUPPLIED'},
  COSMETIC_REWARDS:{supported:false,reason:'ORIGINAL_ASSET_UNLOCK_AND_DUPLICATE_POLICY_NOT_SUPPLIED'}
 }};
}
export class LiveopsManagement{
 constructor(private readonly db:D1Database,private readonly environment:string){}
 async command(actor:NoticePrincipal,id:string,input:z.infer<typeof liveopsCommand>,key:string){
  if(input.kind==='DOMAIN_COMMAND'){
   const requestFence=async()=>{
    if(await this.db.prepare('SELECT 1 FROM liveops_notice_reviews WHERE principal_id=? AND environment=? AND request_key=?').bind(actor.id,this.environment,key).first())throw new NoticeError(409,'idempotency_conflict');
    return this.db.prepare('INSERT INTO notice_write_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM liveops_notice_reviews WHERE principal_id=? AND environment=? AND request_key=?) THEN 1 ELSE 0 END').bind(actor.id,this.environment,key);
   };
   if(input.command.command==='SAVE_DRAFT'){
    if(input.reviewId!==null)throw new NoticeError(400,'liveops_draft_review_forbidden');
    return saveNoticeCommand(this.db,this.environment,actor,id,input.command,key,{id:null,prepare:async()=>[await requestFence()]});
   }
   if(!input.reviewId)throw new NoticeError(409,'liveops_review_required');
   return saveNoticeCommand(this.db,this.environment,actor,id,input.command,key,{id:input.reviewId,prepare:async()=>{
    const review=await this.db.prepare('SELECT review_id FROM liveops_notice_reviews WHERE review_id=? AND environment=? AND notice_id=? AND revision=?')
     .bind(input.reviewId,this.environment,id,input.command.expectedRevision).first();
    if(!review)throw new NoticeError(409,'liveops_review_source_conflict');
    return [await requestFence(),this.db.prepare(`INSERT INTO notice_write_guard SELECT CASE WHEN EXISTS(
     SELECT 1 FROM liveops_notice_reviews v JOIN notice_revisions r ON r.environment=v.environment AND r.notice_id=v.notice_id AND r.revision=v.revision
     JOIN notice_heads h ON h.environment=r.environment AND h.notice_id=r.notice_id
     WHERE v.review_id=? AND v.environment=? AND v.notice_id=? AND v.revision=? AND h.revision=v.revision AND r.document_json=v.document_json
     ) THEN 1 ELSE 0 END`).bind(input.reviewId,this.environment,id,input.command.expectedRevision)];
   }});
  }
  if(!hasNoticePermission(actor,'NOTICE_PUBLISH'))throw new NoticeError(403,'ADMIN_PERMISSION_DENIED');
  const hash=await sha256Hex(canonicalJson({noticeId:id,environment:this.environment,...input}));
  const replay=async()=>{
   const row=await this.db.prepare('SELECT payload_hash,response_json FROM liveops_notice_reviews WHERE principal_id=? AND environment=? AND request_key=?')
    .bind(actor.id,this.environment,key).first<{payload_hash:string;response_json:string}>();
   if(!row)return null;if(row.payload_hash!==hash)throw new NoticeError(409,'idempotency_conflict');
   return JSON.parse(row.response_json) as Record<string,unknown>;
  };
  const old=await replay();if(old)return old;
  if(await this.db.prepare('SELECT 1 FROM notice_command_receipts WHERE principal_id=? AND environment=? AND request_key=?').bind(actor.id,this.environment,key).first())throw new NoticeError(409,'idempotency_conflict');
  const notice=await new NoticeStore(this.db,this.environment).read(id);
  if(!notice||notice.revision!==input.expectedRevision){
   // A concurrent identical review may have committed after the first lookup,
   // followed by a new draft. Recover its original receipt before reporting CAS.
   const committed=await replay();if(committed)return committed;
   throw new NoticeError(409,'notice_revision_conflict');
  }
  const now=Date.now(),reviewId='review_'+crypto.randomUUID(),auditId='audit_'+crypto.randomUUID();
  const response={accepted:true,contractVersion:liveopsVersion,environment:this.environment,reviewId,reviewedRevision:notice.revision,reviewedBy:actor.id,reviewedAt:now,notice,executed:false,auditId};
  try{await this.db.batch([
   this.db.prepare('INSERT INTO notice_write_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM notice_command_receipts WHERE principal_id=? AND environment=? AND request_key=?) THEN 1 ELSE 0 END').bind(actor.id,this.environment,key),
   this.db.prepare("INSERT INTO notice_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.id),
   this.db.prepare(`INSERT INTO notice_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM notice_heads h JOIN notice_revisions r ON r.environment=h.environment AND r.notice_id=h.notice_id AND r.revision=h.revision WHERE h.environment=? AND h.notice_id=? AND h.revision=? AND r.document_json=?) THEN 1 ELSE 0 END`).bind(this.environment,id,notice.revision,canonicalJson(notice.document)),
   this.db.prepare('INSERT INTO liveops_notice_reviews VALUES(?,?,?,?,?,?,?,?,?,?)').bind(reviewId,this.environment,id,notice.revision,canonicalJson(notice.document),actor.id,key,hash,canonicalJson(response),now),
   this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
    VALUES(?,?,?,'NOTICE_PUBLISH','LIVEOPS_NOTICE_REVIEW','notice',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`)
    .bind(auditId,actor.id,actor.role,id,reviewId,hash,canonicalJson({environment:this.environment,reviewId,revision:notice.revision,reason:input.reason,executed:false}),now),
   this.db.prepare('DELETE FROM notice_write_guard')
  ]);}catch(error){
   const recovered=await replay();if(recovered)return recovered;
   if(String(error).includes('CHECK constraint failed')){
    const active=await this.db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor.id).first();
    throw new NoticeError(active?409:403,active?'notice_revision_conflict':'ADMIN_PERMISSION_DENIED');
   }
   throw new NoticeError(503,'liveops_review_unavailable');
  }
  return response;
 }
 async history(id:string,before:number,limit:number){
  // Original immutable domain revisions and original command/audit receipts.
  const rows=await this.db.prepare(`SELECT r.revision,r.status,r.document_json,r.updated_at,a.principal_id,a.action,a.change_summary_json,c.response_json
   FROM notice_revisions r LEFT JOIN admin_audit_logs a ON a.resource_type='notice' AND a.resource_id=r.notice_id AND a.action='NOTICE_COMMAND'
    AND json_extract(a.change_summary_json,'$.environment')=r.environment AND json_extract(a.change_summary_json,'$.afterRevision')=r.revision
   LEFT JOIN notice_command_receipts c ON c.audit_id=a.id
   WHERE r.environment=? AND r.notice_id=? AND r.revision<? ORDER BY r.revision DESC LIMIT ?`).bind(this.environment,id,before,limit+1).all<{revision:number;status:string;document_json:string;updated_at:number;principal_id:string|null;action:string|null;change_summary_json:string|null;response_json:string|null}>();
  const items=rows.results.slice(0,limit).map(r=>({revision:r.revision,status:r.status,document:JSON.parse(r.document_json),updatedAt:r.updated_at,principalId:r.principal_id,change:r.change_summary_json?JSON.parse(r.change_summary_json):null,receipt:r.response_json?JSON.parse(r.response_json):null}));
  return {contractVersion:liveopsVersion,environment:this.environment,noticeId:id,items,nextBefore:rows.results.length>limit?items.at(-1)!.revision:null};
 }
}
