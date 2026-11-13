import {z} from 'zod';
import type {SupportDatabase} from './support-ticket-service';
const policies={
 SUPPORT:{permission:'SUPPORT_TICKET_PROCESS',contractVersion:'support-grant-management-v1',grantTable:'support_ticket_processor_grants',revisionTable:'support_grant_revisions',commandTable:'support_grant_commands',actionPrefix:'SUPPORT_GRANT',errorPrefix:'support_grant'},
 COMMUNITY:{permission:'COMMUNITY_MODERATE',contractVersion:'community.moderation-grants.v1.20261008',grantTable:'community_moderation_grants',revisionTable:'community_grant_revisions',commandTable:'community_grant_commands',actionPrefix:'COMMUNITY_GRANT',errorPrefix:'community_grant'},
} as const;
export const grantCommandSchema=(permission:string)=>z.strictObject({action:z.enum(['GRANT','REVOKE']),permission:z.literal(permission),expectedRevision:z.number().int().nonnegative().safe(),reason:z.string().trim().min(1).max(500)});
const grantReceiptSchema=(version:string,permission:string)=>z.strictObject({contractVersion:z.literal(version),environment:z.enum(['DEV','PRD']),result:z.strictObject({principalId:z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),permission:z.literal(permission),granted:z.boolean(),revision:z.number().int().nonnegative().safe(),outcome:z.enum(['APPLIED','NO_CHANGE'])})});
type Command=z.infer<ReturnType<typeof grantCommandSchema>>;
type Environment='DEV'|'PRD';
type LastChange={actorId:string|null;at:number;reason:string|null;outcome:string;environment:string};
type State={principalId:string;displayName:string|null;role:string;status:string;permission:string;granted:boolean;revision:number;lastChange:LastChange|null};
type Receipt={contractVersion:string;environment:Environment;result:{principalId:string;permission:string;granted:boolean;revision:number;outcome:'APPLIED'|'NO_CHANGE'}};
type Target={id:string;display_name:string|null;role:string;status:string;revision:number;granted:number};
type Recorded={request_hash:string;response_json:string};
export class PrincipalGrantError extends Error{
 constructor(readonly status:400|403|404|409|503,readonly code:string){super(code);}
}
/** Only the server binding selects the environment; each deployment owns a separate DB. */
export class PrincipalGrantService{
 private readonly policy:typeof policies[keyof typeof policies];
 constructor(private readonly db:SupportDatabase,private readonly environment:Environment,scope:keyof typeof policies){this.policy=policies[scope];}
 async canManage(actor:string){return !!await this.db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor).first();}
 private async target(actor:string,target:string):Promise<Target|null>{
  return this.db.prepare(`SELECT p.id,p.display_name,p.role,p.status,COALESCE(r.revision,0) revision,
   EXISTS(SELECT 1 FROM ${this.policy.grantTable} g WHERE g.principal_id=p.id) granted
   FROM admin_principals p LEFT JOIN ${this.policy.revisionTable} r ON r.principal_id=p.id
   WHERE p.id=? AND EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE')`).bind(target,actor).first<Target>();
 }
 async read(actor:string,target:string):Promise<State>{
  if(!await this.canManage(actor))throw new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');
  const row=await this.target(actor,target);
  if(!row){if(!await this.canManage(actor))throw new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');throw new PrincipalGrantError(404,this.policy.errorPrefix+'_target_not_found');}
  const last=await this.db.prepare(`SELECT COALESCE(json_extract(change_summary_json,'$.actorId'),principal_id) actorId,
   created_at at,json_extract(change_summary_json,'$.reason') reason,
   COALESCE(json_extract(change_summary_json,'$.outcome'),json_extract(change_summary_json,'$.cause')) outcome,
   COALESCE(json_extract(change_summary_json,'$.environment'),'LEGACY_DATABASE_BOUND') environment
   FROM admin_audit_logs WHERE resource_type='admin_principal' AND resource_id=? AND result='SUCCESS'
   AND action IN ('${this.policy.actionPrefix}_CHANGE','${this.policy.actionPrefix}_INVALIDATE')
   AND json_extract(change_summary_json,'$.afterRevision')=?
   AND EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE')
   ORDER BY created_at DESC,rowid DESC LIMIT 1`).bind(target,row.revision,actor).first<LastChange>();
  if(!await this.canManage(actor))throw new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');
  const current=await this.target(actor,target);
  if(!current){if(!await this.canManage(actor))throw new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');throw new PrincipalGrantError(404,this.policy.errorPrefix+'_target_not_found');}
  if(JSON.stringify(current)!==JSON.stringify(row))throw new PrincipalGrantError(409,this.policy.errorPrefix+'_revision_conflict');
  return {principalId:row.id,displayName:row.display_name,role:row.role,status:row.status,permission:this.policy.permission,granted:row.granted===1,revision:row.revision,lastChange:last};
 }
 private async recorded(actor:string,key:string):Promise<Recorded|null>{
  return this.db.prepare(`SELECT c.request_hash,c.response_json FROM ${this.policy.commandTable} c
   JOIN admin_principals p ON p.id=c.actor_id WHERE c.environment=? AND c.actor_id=? AND c.request_key=?
   AND p.role='OWNER' AND p.status='ACTIVE'`).bind(this.environment,actor,key).first<Recorded>();
 }
 private async auditFailure(actor:string,target:string,key:string,hash:string,code:string,command:Command,state:State|null){
  await this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,error_code,created_at)
   SELECT ?,id,role,'${this.policy.permission}','${this.policy.actionPrefix}_REJECT','admin_principal',?,?,?,?,'FAILURE',?,? FROM admin_principals WHERE id=?`)
   .bind('audit_'+crypto.randomUUID(),target,key,hash,JSON.stringify({environment:this.environment,reason:command.reason,action:command.action,beforeGranted:state?.granted??null,afterGranted:state?.granted??null,beforeRevision:state?.revision??null,afterRevision:state?.revision??null}),code,Date.now(),actor).run();
 }
 async change(actor:string,target:string,key:string,value:unknown):Promise<Receipt>{
  const initiallyAllowed=await this.canManage(actor);
  const parsed=grantCommandSchema(this.policy.permission).safeParse(value);
  if(!parsed.success||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new PrincipalGrantError(initiallyAllowed?400:403,initiallyAllowed?'contract_violation':'ADMIN_PERMISSION_DENIED');
  const command=parsed.data;
  const bytes=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(JSON.stringify([this.environment,target,command.action,command.permission,command.expectedRevision,command.reason])));
  const hash=Array.from(new Uint8Array(bytes),b=>b.toString(16).padStart(2,'0')).join('');
  let state:State|null=null;
  const replay=async()=>{const old=await this.recorded(actor,key);if(!old)return null;
   if(old.request_hash!==hash)throw new PrincipalGrantError(409,'idempotency_conflict');
   const receipt=grantReceiptSchema(this.policy.contractVersion,this.policy.permission).parse(JSON.parse(old.response_json));
   if(receipt.environment!==this.environment||receipt.result.principalId!==target)throw new PrincipalGrantError(503,this.policy.errorPrefix+'_unavailable');
   return receipt;};
  try{
   if(!initiallyAllowed||!await this.canManage(actor))throw new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');
   const old=await replay();if(old)return old;
   state=await this.read(actor,target);
   if(command.action==='GRANT'&&(state.status!=='ACTIVE'||!['OWNER','OPERATOR'].includes(state.role)))throw new PrincipalGrantError(409,this.policy.errorPrefix+'_target_ineligible');
   if(state.revision!==command.expectedRevision)throw new PrincipalGrantError(409,this.policy.errorPrefix+'_revision_conflict');
   const granted=command.action==='GRANT',changed=granted!==state.granted;
   if(changed&&state.revision===Number.MAX_SAFE_INTEGER)throw new PrincipalGrantError(409,this.policy.errorPrefix+'_revision_conflict');
   const revision=state.revision+(changed?1:0),now=Date.now(),auditId='audit_'+crypto.randomUUID();
   const result:Receipt={contractVersion:this.policy.contractVersion,environment:this.environment,
    result:{principalId:target,permission:this.policy.permission,granted,revision,outcome:changed?'APPLIED':'NO_CHANGE'}};
   await this.db.batch([
    this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,created_at)
     VALUES(?,?,'OWNER','${this.policy.permission}','${this.policy.actionPrefix}_CHANGE','admin_principal',?,?,?,?,'SUCCESS',?)`)
     .bind(auditId,actor,target,key,hash,JSON.stringify({environment:this.environment,actorId:actor,reason:command.reason,action:command.action,beforeGranted:state.granted,afterGranted:granted,beforeRevision:state.revision,afterRevision:revision,outcome:result.result.outcome}),now),
    this.db.prepare(`INSERT INTO ${this.policy.commandTable} VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?)`)
     .bind(this.environment,actor,target,key,hash,command.action,command.reason,state.granted?1:0,granted?1:0,state.revision,revision,auditId,JSON.stringify(result),now),
   ]);
   return result;
  }catch(error){
   let cause:unknown=error;
   if(!initiallyAllowed||!await this.canManage(actor))cause=new PrincipalGrantError(403,'ADMIN_PERMISSION_DENIED');
   else{
    // A matching command may commit between the receipt and target-state reads.
    // Recover it even when the stale preflight raised a typed revision error.
    try{const old=await replay();if(old)return old;}catch(replayError){cause=replayError;}
    const message=String(cause);
    if(!(cause instanceof PrincipalGrantError)){
     if(message.includes(this.policy.errorPrefix+'_revision_conflict'))cause=new PrincipalGrantError(409,this.policy.errorPrefix+'_revision_conflict');
     else if(message.includes(this.policy.errorPrefix+'_target_ineligible'))cause=new PrincipalGrantError(409,this.policy.errorPrefix+'_target_ineligible');
     else if(message.includes(this.policy.errorPrefix+'_target_missing'))cause=new PrincipalGrantError(404,this.policy.errorPrefix+'_target_not_found');
     else cause=new PrincipalGrantError(503,this.policy.errorPrefix+'_unavailable');
    }
   }
   const failure=cause as PrincipalGrantError;
   await this.auditFailure(actor,target,key,hash,failure.code,command,state);
   throw failure;
  }
 }
}
