import {z} from 'zod';
import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {eventDefinitionDraftSchema,previewEventDefinition} from './admin-event-definition';
import type {RuntimePolicyPrincipal} from './runtime-policy';
export const eventManagementVersion='admin.event-draft-management.v1.20261020';
export const eventConfigWritePermission='EVENT_CONFIG_WRITE',eventConfigReadPermission='SUPPORT_READ';
export const eventDraftSaveSchema=z.strictObject({operation:z.literal('SAVE_DRAFT'),
 reason:z.string().trim().min(1).max(1000),expectedRevision:z.number().int().positive().safe().nullable(),definition:eventDefinitionDraftSchema});
export class AdminEventManagementError extends Error{constructor(readonly status:400|403|404|409|503,readonly code:string){super(code);}}
const fail=(status:400|403|404|409|503,code:string):never=>{throw new AdminEventManagementError(status,code);};
type Validation=Awaited<ReturnType<typeof previewEventDefinition>>;
type Saved=Omit<Validation,'contractVersion'|'persisted'|'writesOnPreview'>&{
 contractVersion:typeof eventManagementVersion;validationVersion:string;persisted:true;state:'DRAFT';savedAt:number;savedBy:string;reason:string;
};
type Row={environment:string;round_id:string;revision:number;definition_hash:string;response_json:string};
export class AdminEventManagement{
 constructor(private readonly db:D1Database,private readonly environment:string){
  if(environment!=='DEV'&&environment!=='PRD')fail(503,'event_environment_unavailable');
 }
 async authorize(actor:RuntimePolicyPrincipal,write=false){
  if(write&&!['OWNER','OPERATOR'].includes(actor.role))return fail(403,'admin_permission_denied');
  if(!await this.db.prepare("SELECT 1 FROM admin_principals WHERE id=? AND role=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR','VIEWER')").bind(actor.id,actor.role).first())return fail(403,'admin_permission_denied');
 }
 private async latest(roundId:string){
  return this.db.prepare('SELECT * FROM admin_event_drafts WHERE environment=? AND round_id=? ORDER BY revision DESC LIMIT 1').bind(this.environment,roundId).first<Row>();
 }
 private async decode(row:Row){
  const result=JSON.parse(row.response_json) as Saved;
  if(result.contractVersion!==eventManagementVersion||result.environment!==row.environment||result.environment!==this.environment||
   result.roundId!==row.round_id||result.revision!==row.revision||result.state!=='DRAFT'||result.persisted!==true||result.reviewApproved!==false||result.scheduleSupported!==false||
   result.definitionHash!==row.definition_hash||await sha256Hex(canonicalJson({contractVersion:result.validationVersion,definition:result.definition}))!==row.definition_hash)
   return fail(503,'event_draft_source_invalid');
  return result;
 }
 async read(roundId:string){
  const id=eventDefinitionDraftSchema.shape.roundId.parse(roundId),row=await this.latest(id);
  if(!row)return fail(404,'event_draft_not_found');return this.decode(row);
 }
 async list(after='',limit=20){
  if(after!==''&&eventDefinitionDraftSchema.shape.roundId.parse(after)!==after)return fail(400,'contract_violation');
  if(!Number.isSafeInteger(limit)||limit<1||limit>50)return fail(400,'contract_violation');
  const rows=await this.db.prepare(`SELECT d.* FROM admin_event_drafts d WHERE d.environment=? AND d.round_id>?
   AND NOT EXISTS(SELECT 1 FROM admin_event_drafts newer WHERE newer.environment=d.environment AND newer.round_id=d.round_id AND newer.revision>d.revision)
   ORDER BY d.round_id LIMIT ?`).bind(this.environment,after,limit+1).all<Row>();
  const items=await Promise.all(rows.results.slice(0,limit).map(async row=>{
   const saved=await this.decode(row);
   return {roundId:saved.roundId,title:saved.definition.title,theme:saved.definition.theme,revision:saved.revision,definitionHash:saved.definitionHash,
    schedule:saved.definition.schedule,state:saved.state,definitionValid:saved.definitionValid,issueCount:saved.issues.length,blockerCount:saved.blockers.length,
    savedAt:saved.savedAt,savedBy:saved.savedBy};
  }));
  return {contractVersion:eventManagementVersion,environment:this.environment,state:'DRAFT',items,nextAfter:rows.results.length>limit?items.at(-1)!.roundId:null};
 }
 private async replay(actor:RuntimePolicyPrincipal,key:string,digest:string){
  const row=await this.db.prepare('SELECT payload_hash,response_json FROM admin_event_draft_receipts WHERE environment=? AND principal_id=? AND request_key=?')
   .bind(this.environment,actor.id,key).first<{payload_hash:string;response_json:string}>();
  if(!row)return null;if(row.payload_hash!==digest)return fail(409,'idempotency_conflict');return JSON.parse(row.response_json) as Saved;
 }
 async save(actor:RuntimePolicyPrincipal,roundId:string,input:unknown,key:string):Promise<Saved>{
  await this.authorize(actor,true);
  if(!/^[\x21-\x7e]{1,128}$/u.test(key))return fail(400,'idempotency_key_required');
  const parsed=eventDraftSaveSchema.safeParse(input);if(!parsed.success)return fail(400,'contract_violation');
  const command=parsed.data,definition=command.definition;
  if(definition.environment!==this.environment)return fail(409,'event_environment_conflict');
  if(roundId!==definition.roundId)return fail(409,'event_round_conflict');
  const digest=await sha256Hex(canonicalJson({roundId,command}));
  const old=await this.replay(actor,key,digest);if(old)return old;
  const conflict=async()=>{
   await this.authorize(actor,true);
   const won=await this.replay(actor,key,digest);if(won)return won;
   return fail(409,'event_revision_conflict');
  };
  if((command.expectedRevision??0)>=Number.MAX_SAFE_INTEGER||definition.revision!==(command.expectedRevision??0)+1)return conflict();
  const before=await this.latest(roundId);
  if((before?.revision??null)!==command.expectedRevision)return conflict();
  const validation=await previewEventDefinition(this.environment,definition);
  const {contractVersion:validationVersion,persisted:_persisted,writesOnPreview:_writes,...details}=validation;
  void _persisted;void _writes;
  // Structural drafts may retain policy issues. Saving never approves a review
  // or publishes clock-driven state, reward rights, extensions or settlements.
  const response:Saved={...details,contractVersion:eventManagementVersion,validationVersion,persisted:true,state:'DRAFT',savedAt:Date.now(),savedBy:actor.id,reason:command.reason,
   blockers:details.blockers};
  const serialized=canonicalJson(response),auditId=createRequestId(),requestId=createRequestId();
  try{
   await this.db.batch([
    this.db.prepare(`INSERT INTO admin_event_draft_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR'))
     AND (SELECT MAX(revision) FROM admin_event_drafts WHERE environment=? AND round_id=?) IS ? THEN 1 ELSE 0 END`).bind(actor.id,actor.role,this.environment,roundId,command.expectedRevision),
    this.db.prepare('INSERT INTO admin_event_drafts VALUES(?,?,?,?,?,?,?)').bind(this.environment,roundId,definition.revision,response.definitionHash,serialized,actor.id,response.savedAt),
    this.db.prepare('INSERT INTO admin_event_draft_receipts VALUES(?,?,?,?,?)').bind(this.environment,actor.id,key,digest,serialized),
    this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,created_at)
     VALUES(?,?,?,?,'EVENT_SAVE_DRAFT','EVENT_DRAFT',?,?,?,?,'SUCCESS',?)`).bind(auditId,actor.id,actor.role,eventConfigWritePermission,roundId,requestId,digest,
      canonicalJson({environment:this.environment,reason:command.reason,previousRevision:command.expectedRevision,revision:definition.revision,definitionHash:response.definitionHash,definitionValid:response.definitionValid,
       state:'DRAFT',reviewApproved:false,scheduleSupported:false}),response.savedAt),
    this.db.prepare('DELETE FROM admin_event_draft_write_guard')]);
  }catch(error){
   await this.authorize(actor,true);
   const won=await this.replay(actor,key,digest);if(won)return won;
   if(((await this.latest(roundId))?.revision??null)!==command.expectedRevision)return fail(409,'event_revision_conflict');
   throw error;
  }
  return response;
 }
}
