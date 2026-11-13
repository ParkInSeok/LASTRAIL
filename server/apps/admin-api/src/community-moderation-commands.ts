import {canonicalJson,sha256Hex} from '@lastrail/common';
import {communityAdminCommandReceipt,communityModerationVersion,communityCommentLockVersion,type CommunityAdminCommandReceipt} from '../../../packages/common/src/community-moderation-contract';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
type Actor={role:string;revision:number};
export type ModerationPlan={receipt:CommunityAdminCommandReceipt;guards:D1PreparedStatement[];writes:D1PreparedStatement[];summary:Record<string,string|number|null>;assertCurrent:()=>Promise<void>};
/** Owns current moderator authority and atomic command/receipt/audit coordination; domain plans own their state transitions. */
export class CommunityModerationCommands{
 constructor(readonly db:D1Database,readonly environment:'DEV'|'PRD'){}
 async authorize(actor:string):Promise<Actor>{
  const row=await this.db.prepare(`SELECT p.role,r.revision FROM admin_principals p JOIN community_moderation_grants g ON g.principal_id=p.id AND g.permission='COMMUNITY_MODERATE' JOIN community_grant_revisions r ON r.principal_id=p.id WHERE p.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR')`).bind(actor).first<Actor>();
  if(!row)throw new CommunityStoreError(403,'ADMIN_PERMISSION_DENIED');return row;
 }
 fence(sql:string,values:(string|number)[]=[]){return this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN ${sql} THEN 1 ELSE 0 END`).bind(...values);}
 async execute(actor:string,key:string,type:'REPORT'|'CONTENT',resourceId:string,body:{reason:string},prepare:()=>Promise<ModerationPlan>,contractVersion:typeof communityModerationVersion|typeof communityCommentLockVersion=communityModerationVersion){
  const initial=await this.authorize(actor);if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new CommunityStoreError(400,'idempotency_key_required');
  const hash=await sha256Hex(canonicalJson({contractVersion,environment:this.environment,resourceType:type,resourceId,body}));
  const replay=async()=>{await this.authorize(actor);const old=await this.db.prepare('SELECT request_hash,response_json,resource_type,resource_id,audit_id FROM community_moderation_commands WHERE environment=? AND actor_id=? AND request_key=?').bind(this.environment,actor,key).first<{request_hash:string;response_json:string;resource_type:string;resource_id:string;audit_id:string}>();
   if(!old)return null;if(old.request_hash!==hash)throw new CommunityStoreError(409,'idempotency_conflict');let receipt:CommunityAdminCommandReceipt;
   try{receipt=communityAdminCommandReceipt.parse(JSON.parse(old.response_json));if(receipt.contractVersion!==contractVersion||receipt.environment!==this.environment||receipt.resourceType!==type||receipt.resourceId!==resourceId||old.resource_type!==type||old.resource_id!==resourceId)throw new Error();}catch{throw new CommunityStoreError(503,'community_moderation_storage_invalid');}
   const audit=await this.db.prepare("SELECT id FROM admin_audit_logs WHERE id=? AND principal_id=? AND payload_hash=? AND result='SUCCESS' AND action=?").bind(old.audit_id,actor,hash,'COMMUNITY_MODERATION_'+type).first();if(!audit)throw new CommunityStoreError(503,'community_moderation_storage_invalid');await this.authorize(actor);return receipt;};
  let plan:ModerationPlan|undefined;
  try{const old=await replay();if(old)return old;plan=await prepare();const receipt=communityAdminCommandReceipt.parse(plan.receipt);if(receipt.contractVersion!==contractVersion||receipt.environment!==this.environment||receipt.resourceType!==type||receipt.resourceId!==resourceId)throw new CommunityStoreError(503,'community_moderation_storage_invalid');const auditId='audit_'+crypto.randomUUID(),at=Date.now();
   await this.db.batch([
    this.fence(`EXISTS(SELECT 1 FROM admin_principals p JOIN community_moderation_grants g ON g.principal_id=p.id AND g.permission='COMMUNITY_MODERATE' JOIN community_grant_revisions r ON r.principal_id=p.id WHERE p.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR') AND r.revision=?)`,[actor,initial.revision]),...plan.guards,
    this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,created_at) SELECT ?,id,role,'COMMUNITY_MODERATE',?,?,?,?,?,?,'SUCCESS',? FROM admin_principals WHERE id=?`).bind(auditId,'COMMUNITY_MODERATION_'+type,type==='REPORT'?'community_report':'community_content',resourceId,key,hash,canonicalJson({environment:this.environment,reason:body.reason,...plan.summary}),at,actor),
    this.db.prepare('INSERT INTO community_moderation_commands VALUES(?,?,?,?,?,?,?,?)').bind(this.environment,actor,key,type,resourceId,hash,canonicalJson(receipt),auditId),...plan.writes,this.db.prepare('DELETE FROM community_content_write_guard')
   ]);return receipt;
  }catch(error){const current=await this.authorize(actor);const old=await replay();if(old)return old;if(current.revision!==initial.revision)throw new CommunityStoreError(409,'community_moderation_authority_stale');await plan?.assertCurrent();if(error instanceof CommunityStoreError)throw error;throw new CommunityStoreError(503,'community_moderation_unavailable');}
 }
}