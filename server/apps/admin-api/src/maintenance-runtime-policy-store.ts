import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {MaintenancePlanError,type MaintenancePrincipal} from '../../../packages/common/src/maintenance-plan-policy';
import {maintenanceContextSchema,maintenanceTransitionSchema,maintenanceReceiptSchema,maintenanceReasonSchema,maintenanceKeySchema,type MaintenanceContext,type MaintenanceTransition,type MaintenanceReceipt,type MaintenanceState} from '../../../packages/common/src/maintenance-runtime-policy';
import {MaintenancePlanStore} from './maintenance-plan-store';
import {z} from 'zod';
import {maintenanceImmediateCommandSchema,maintenanceImmediateReceiptSchema} from '../../../packages/common/src/maintenance-runtime-policy';
import {maintenanceLoginReadStatement} from '../../game-api/src/maintenance-admission';
const timestamp=z.number().int().nonnegative().safe();
const positive=z.number().int().positive().safe();
const storedPolicySchema=z.object({revision:positive,state:z.enum(['NORMAL','SCHEDULED','ACTIVE','COMPLETED','CANCELLED','IMMEDIATE']),
 maintenance_id:z.string().min(1).nullable(),draft_revision:positive.nullable(),drain_starts_at:timestamp.nullable(),
 starts_at:timestamp.nullable(),estimated_end_at:timestamp.nullable(),ended_at:timestamp.nullable(),
 actor_id:z.string().min(1),reason:maintenanceReasonSchema,created_at:timestamp,
}).refine(row=>row.state==='NORMAL'
 ? row.revision===1&&[row.maintenance_id,row.draft_revision,row.drain_starts_at,row.starts_at,row.estimated_end_at,row.ended_at].every(value=>value===null)
 : row.state==='IMMEDIATE'
 ? row.revision>1&&[row.maintenance_id,row.draft_revision,row.drain_starts_at,row.starts_at,row.ended_at].every(value=>value===null)
   &&(row.estimated_end_at===null||row.estimated_end_at>row.created_at)
 : row.maintenance_id!==null&&row.draft_revision!==null&&row.starts_at!==null
   &&(row.drain_starts_at===null||row.drain_starts_at<=row.starts_at)
   &&(row.estimated_end_at===null||row.estimated_end_at>row.starts_at)
   &&(['SCHEDULED','ACTIVE'].includes(row.state)?row.ended_at===null:row.ended_at!==null));
const clock="CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)";
type Row={revision:number;state:MaintenanceState;maintenance_id:string|null;draft_revision:number|null;drain_starts_at:number|null;starts_at:number|null;estimated_end_at:number|null};
/** Single runtime policy owner. Dedicated HTTP supports immediate GAME modes with
 * WEB NORMAL; legacy scheduled ACTIVATE/CANCEL HTTP remains unavailable. */
export class MaintenanceRuntimePolicyStore{
 private readonly context:MaintenanceContext;
 constructor(private readonly db:D1Database,context:MaintenanceContext){this.context=maintenanceContextSchema.parse(context);}
 async read(actor:MaintenancePrincipal){
  const {environment,applicationId}=this.context;
  const active=()=>this.db.prepare("SELECT id FROM admin_principals WHERE id=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR','VIEWER')").bind(actor.id).first();
  if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
  const raw=await this.db.prepare(`SELECT r.* FROM maintenance_runtime_heads h
   JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
   JOIN admin_principals reader ON reader.id=? AND reader.status='ACTIVE' AND reader.role IN ('OWNER','OPERATOR','VIEWER')
   WHERE h.environment=? AND h.application_id=?`).bind(actor.id,environment,applicationId).first();
  if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
  const parsed=storedPolicySchema.safeParse(raw);
  if(!parsed.success)throw new MaintenancePlanError(503,'maintenance_policy_unavailable');
  const row=parsed.data;
  const modes=row.state==='IMMEDIATE'?await this.db.prepare('SELECT game_mode,web_mode FROM maintenance_immediate_modes WHERE environment=? AND application_id=? AND revision=?').bind(environment,applicationId,row.revision).first():null;
  if(row.state==='IMMEDIATE'&&!z.object({game_mode:z.enum(['NORMAL','DRAINING','MAINTENANCE']),web_mode:z.literal('NORMAL')}).safeParse(modes).success)
   throw new MaintenancePlanError(503,'maintenance_policy_unavailable');
  if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
  return {revision:row.revision,state:row.state,maintenanceId:row.maintenance_id,draftRevision:row.draft_revision,
   drainStartsAt:row.drain_starts_at,startsAt:row.starts_at,estimatedEndAt:row.estimated_end_at,endedAt:row.ended_at,
   lastChange:{principalId:row.actor_id,reason:row.reason,at:row.created_at}};
 }
 async changeImmediately(actor:MaintenancePrincipal,raw:unknown,keyInput:string){
  const db=this.db,{environment,applicationId}=this.context;
  const active=()=>db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor.id).first();
  if(actor.role!=='OWNER'||!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
  const parsed=maintenanceImmediateCommandSchema.safeParse(raw),key=maintenanceKeySchema.safeParse(keyInput);
  if(!parsed.success||!key.success)throw new MaintenancePlanError(400,'contract_violation');
  const command=parsed.data,hash=await sha256Hex(canonicalJson({context:this.context,command}));
  const replay=async()=>{
   const row=await db.prepare('SELECT payload_hash,response_json FROM maintenance_runtime_receipts WHERE principal_id=? AND environment=? AND request_key=?').bind(actor.id,environment,key.data).first<{payload_hash:string;response_json:string}>();
   if(!row)return null;
   if(row.payload_hash!==hash)throw new MaintenancePlanError(409,'idempotency_conflict');
   if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
   try{return maintenanceImmediateReceiptSchema.parse(JSON.parse(row.response_json));}
   catch{throw new MaintenancePlanError(503,'maintenance_policy_unavailable');}
  };
  const prior=await replay();if(prior)return prior;
  if(command.webMode!=='NORMAL')throw new MaintenancePlanError(409,'maintenance_mode_unsupported');
  try{
   const snapshot=await maintenanceLoginReadStatement(db,this.context).first();
   const parsedCurrent=z.object({revision:positive,stored_state:z.enum(['NORMAL','SCHEDULED','ACTIVE','COMPLETED','CANCELLED','IMMEDIATE']),
    game_code:z.enum(['OK','BLOCKED']),web_code:z.enum(['OK','BLOCKED']),checked_at:timestamp,
    document_json:z.string().nullable()}).safeParse(snapshot);
   if(!parsedCurrent.success)throw new MaintenancePlanError(503,'maintenance_policy_unavailable');
   const current=parsedCurrent.data;
   if(current.revision!==command.expectedRevision||current.revision===Number.MAX_SAFE_INTEGER)throw new MaintenancePlanError(409,'maintenance_revision_conflict');
   if(current.stored_state==='SCHEDULED'||(current.stored_state==='ACTIVE'&&current.document_json!==null&&JSON.parse(current.document_json).startsAt>current.checked_at))
    throw new MaintenancePlanError(409,'maintenance_schedule_conflict');
   if(command.estimatedEndAt!==null&&command.estimatedEndAt<=current.checked_at)throw new MaintenancePlanError(400,'contract_violation');
   const revision=current.revision+1,now=current.checked_at,auditId='audit_'+crypto.randomUUID(),requestId=createRequestId();
   const response=maintenanceImmediateReceiptSchema.parse({accepted:true,applied:true,contractVersion:'admin-maintenance-immediate-command-v1',
    environment,applicationId,policyRevision:revision,gameMode:command.gameMode,webMode:command.webMode,appliedAt:now,auditId,requestId,automaticCompletion:false});
   await db.batch([
    db.prepare("INSERT INTO maintenance_runtime_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.id),
    db.prepare(`INSERT INTO maintenance_runtime_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM maintenance_runtime_heads h JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
      WHERE h.environment=? AND h.application_id=? AND h.revision=? AND r.state<>'SCHEDULED'
      AND (r.state<>'ACTIVE' OR r.starts_at<=${clock})) AND (? IS NULL OR ?>${clock}) THEN 1 ELSE 0 END`).bind(environment,applicationId,command.expectedRevision,command.estimatedEndAt,command.estimatedEndAt),
    db.prepare("INSERT INTO maintenance_runtime_revisions VALUES(?,?,?,'IMMEDIATE',NULL,NULL,NULL,NULL,?,NULL,?,?,?)").bind(environment,applicationId,revision,command.estimatedEndAt,actor.id,command.reason,now),
    db.prepare('INSERT INTO maintenance_immediate_modes VALUES(?,?,?,?,?)').bind(environment,applicationId,revision,command.gameMode,command.webMode),
    db.prepare('UPDATE maintenance_runtime_heads SET revision=? WHERE environment=? AND application_id=?').bind(revision,environment,applicationId),
    db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
      VALUES(?,?,?,'MAINTENANCE_RUNTIME_WRITE','MAINTENANCE_IMMEDIATE_CHANGE','maintenance_runtime',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`)
      .bind(auditId,actor.id,actor.role,applicationId,requestId,hash,canonicalJson({environment,reason:command.reason,beforeRevision:current.revision,afterRevision:revision,gameMode:command.gameMode,webMode:command.webMode,estimatedEndAt:command.estimatedEndAt}),now),
    db.prepare('INSERT INTO maintenance_runtime_receipts VALUES(?,?,?,?,?,?)').bind(actor.id,environment,key.data,hash,auditId,canonicalJson(response)),
   ]);
   return response;
  }catch(error){
   const committed=await replay();if(committed)return committed;
   if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
   if(error instanceof MaintenancePlanError)throw error;
   if(/CHECK constraint failed: valid=1|maintenance_revision_conflict/u.test(String(error)))throw new MaintenancePlanError(409,'maintenance_revision_conflict');
   throw new MaintenancePlanError(503,'maintenance_policy_unavailable');
  }
 }
 async transition(actor:MaintenancePrincipal,input:MaintenanceTransition,reasonInput:string,keyInput:string):Promise<MaintenanceReceipt>{
  const command=maintenanceTransitionSchema.parse(input),reason=maintenanceReasonSchema.parse(reasonInput),key=maintenanceKeySchema.parse(keyInput);
  const db=this.db,{environment,applicationId}=this.context;
  const active=()=>db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor.id).first();
  if(actor.role!=='OWNER'||!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
  const hash=await sha256Hex(canonicalJson({context:this.context,command,reason}));
  const replay=async()=>{
   const r=await db.prepare('SELECT payload_hash,response_json FROM maintenance_runtime_receipts WHERE principal_id=? AND environment=? AND request_key=?').bind(actor.id,environment,key).first<{payload_hash:string;response_json:string}>();
   if(!r)return null;
   if(r.payload_hash!==hash)throw new MaintenancePlanError(409,'idempotency_conflict');
   return maintenanceReceiptSchema.parse(JSON.parse(r.response_json));
  };
  const prior=await replay();if(prior)return prior;
  try {
  const current=await db.prepare(`SELECT r.* FROM maintenance_runtime_heads h JOIN maintenance_runtime_revisions r USING(environment,application_id,revision) WHERE h.environment=? AND h.application_id=?`).bind(environment,applicationId).first<Row>();
  if((current?.revision??null)!==command.expectedRevision||current?.revision===Number.MAX_SAFE_INTEGER)throw new MaintenancePlanError(409,'maintenance_revision_conflict');
  let state:MaintenanceState,source:Row={revision:0,state:'NORMAL',maintenance_id:null,draft_revision:null,drain_starts_at:null,starts_at:null,estimated_end_at:null};
  const checks:D1PreparedStatement[]=[];
  if(command.command==='INITIALIZE'){state='NORMAL';}
  else if(command.command==='SCHEDULE'||command.command==='ACTIVATE'){
   const immediateNormal=current?.state==='IMMEDIATE'&&await db.prepare("SELECT revision FROM maintenance_immediate_modes WHERE environment=? AND application_id=? AND revision=? AND game_mode='NORMAL' AND web_mode='NORMAL'").bind(environment,applicationId,current.revision).first();
   if(!current||(!['NORMAL','COMPLETED','CANCELLED'].includes(current.state)&&!immediateNormal))throw new MaintenancePlanError(409,'maintenance_transition_conflict');
   if(immediateNormal)checks.push(db.prepare("INSERT INTO maintenance_runtime_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM maintenance_immediate_modes WHERE environment=? AND application_id=? AND revision=? AND game_mode='NORMAL' AND web_mode='NORMAL') THEN 1 ELSE 0 END").bind(environment,applicationId,current.revision));
   const draft=await new MaintenancePlanStore(db,environment).read(command.maintenanceId);
   if(!draft||draft.draftRevision!==command.draftRevision||draft.document.serviceScope==='WEB'||draft.document.applicationId!==applicationId)throw new MaintenancePlanError(409,'maintenance_source_conflict');
   const p=draft.document;state=command.command==='SCHEDULE'?'SCHEDULED':'ACTIVE';
   source={...source,maintenance_id:draft.maintenanceId,draft_revision:draft.draftRevision,drain_starts_at:p.drainStartsAt,starts_at:p.startsAt,estimated_end_at:p.estimatedEndAt};
   checks.push(db.prepare(`INSERT INTO maintenance_runtime_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM maintenance_plan_heads h JOIN maintenance_plan_revisions r USING(environment,maintenance_id,draft_revision) WHERE h.environment=? AND h.maintenance_id=? AND h.draft_revision=? AND r.document_json=?) AND (?='SCHEDULE' OR ?<=${clock}) THEN 1 ELSE 0 END`).bind(environment,draft.maintenanceId,draft.draftRevision,canonicalJson(p),command.command,p.startsAt));
  }else{
   if(!current||(command.command==='CANCEL'?current.state!=='SCHEDULED':!['SCHEDULED','ACTIVE'].includes(current.state)))throw new MaintenancePlanError(409,'maintenance_transition_conflict');
   state=command.command==='CANCEL'?'CANCELLED':'COMPLETED';source=current;
  }
  const revision=(current?.revision??0)+1,now=Date.now(),auditId='audit_'+crypto.randomUUID(),requestId=createRequestId();
  const response:MaintenanceReceipt={accepted:true,environment,applicationId,policyRevision:revision};
   await db.batch([
    db.prepare("INSERT INTO maintenance_runtime_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.id),
    db.prepare(`INSERT INTO maintenance_runtime_guard SELECT CASE WHEN (? IS NULL AND NOT EXISTS(SELECT 1 FROM maintenance_runtime_heads WHERE environment=? AND application_id=?)) OR EXISTS(SELECT 1 FROM maintenance_runtime_heads WHERE environment=? AND application_id=? AND revision=?) THEN 1 ELSE 0 END`).bind(command.expectedRevision,environment,applicationId,environment,applicationId,command.expectedRevision),
    ...checks,
    db.prepare('INSERT INTO maintenance_runtime_revisions VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?)').bind(environment,applicationId,revision,state,source.maintenance_id,source.draft_revision,source.drain_starts_at,source.starts_at,source.estimated_end_at,state==='COMPLETED'||state==='CANCELLED'?now:null,actor.id,reason,now),
    db.prepare('INSERT INTO maintenance_runtime_heads VALUES(?,?,?) ON CONFLICT(environment,application_id) DO UPDATE SET revision=excluded.revision').bind(environment,applicationId,revision),
    db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at) VALUES(?,?,?,'MAINTENANCE_RUNTIME_WRITE','MAINTENANCE_RUNTIME_TRANSITION','maintenance_runtime',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`).bind(auditId,actor.id,actor.role,applicationId,requestId,hash,canonicalJson({environment,reason,beforeRevision:current?.revision??null,afterRevision:revision,state}),now),
    db.prepare('INSERT INTO maintenance_runtime_receipts VALUES(?,?,?,?,?,?)').bind(actor.id,environment,key,hash,auditId,canonicalJson(response)),
   ]);
  return response;
  }catch(error){
   const committed=await replay();if(committed)return committed;
   if(error instanceof MaintenancePlanError)throw error;
   if(!await active())throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
   if(/CHECK constraint failed: valid=1|maintenance_revision_conflict/u.test(String(error)))throw new MaintenancePlanError(409,'maintenance_revision_conflict');
   throw new MaintenancePlanError(503,'maintenance_policy_unavailable');
  }
 }
}
