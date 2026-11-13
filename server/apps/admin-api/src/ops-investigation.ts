import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {opsDimensions,opsInvestigationVersion} from './ops-investigation-read';
import type {RuntimePolicyPrincipal} from './runtime-policy';

export const opsWritePermission='OPS_ERROR_LOG_WRITE';
export const canWriteOps=(actor:RuntimePolicyPrincipal)=>actor.role==='OWNER'||actor.role==='OPERATOR';
const safeText=z.string().trim().min(1).max(500).refine(v=>[...v].every(c=>c.charCodeAt(0)>=32&&c.charCodeAt(0)!==127));
const build=z.strictObject({appVersion:z.string().regex(/^[A-Za-z0-9.+_-]{1,64}$/u).nullable(),appVersionCode:z.string().regex(/^\d{1,19}$/u).nullable(),serverVersion:z.string().regex(/^[A-Za-z0-9._-]{1,100}$/u).nullable()}).refine(v=>Object.values(v).some(x=>x!==null));
export const opsCommand=z.strictObject({status:z.enum(['acknowledged','verifying','resolved']),expectedVersion:z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-1),reason:safeText,fixBuild:build.nullable(),evidence:safeText.nullable(),verificationResult:z.enum(['PASS']).nullable()});
type Command=z.infer<typeof opsCommand>;
type State={errorLogId:string;status:string;version:number;reason:string|null;fixBuild:Command['fixBuild'];evidence:string|null;verificationResult:'PASS'|null;updatedBy:string|null;updatedAt:number|null;resolvedAt:number|null};
export class OpsInvestigationError extends Error{
 constructor(readonly status:400|403|404|409|503,readonly code:string,readonly current?:State){super(code);}
}
export class OpsInvestigationStore{
 constructor(private readonly db:D1Database){}
 async read(id:string):Promise<State|null>{
  const saved=await this.db.prepare('SELECT state_json FROM ops_error_investigations WHERE error_log_id=?').bind(id).first<{state_json:string}>();
  if(saved)return JSON.parse(saved.state_json) as State;
  const log=await this.db.prepare('SELECT status FROM ops_error_logs WHERE id=?').bind(id).first<{status:string}>();
  return log?{errorLogId:id,status:log.status,version:0,reason:null,fixBuild:null,evidence:null,verificationResult:null,updatedBy:null,updatedAt:null,resolvedAt:null}:null;
 }
 async command(actor:RuntimePolicyPrincipal,id:string,command:Command,key:string){
  if(!canWriteOps(actor))throw new OpsInvestigationError(403,'ADMIN_PERMISSION_DENIED');
  const hash=await sha256Hex(canonicalJson({id,...command}));
  const replay=async()=>{
   const row=await this.db.prepare('SELECT payload_hash,response_json FROM ops_error_investigation_receipts WHERE principal_id=? AND request_key=?').bind(actor.id,key).first<{payload_hash:string;response_json:string}>();
   if(!row)return null;
   if(row.payload_hash!==hash)throw new OpsInvestigationError(409,'idempotency_conflict');
   return JSON.parse(row.response_json) as {accepted:true;contractVersion:string;investigation:State;auditId:string};
  };
  const prior=await replay();if(prior)return prior;
  const current=await this.read(id);
  if(!current||current.version!==command.expectedVersion){
   const committed=await replay();if(committed)return committed;
   if(!current)throw new OpsInvestigationError(404,'ops_error_log_not_found');
   throw new OpsInvestigationError(409,'ops_investigation_version_conflict',current);
  }
  const transitions:Record<string,string[]>={open:['acknowledged'],acknowledged:['verifying'],verifying:['acknowledged','resolved'],resolved:['acknowledged'],ignored:[]};
  if(!transitions[current.status]?.includes(command.status))throw new OpsInvestigationError(400,'ops_investigation_transition_invalid');
  if(command.status==='resolved'&&(!command.fixBuild||!command.evidence||command.verificationResult!=='PASS'))throw new OpsInvestigationError(400,'ops_resolution_evidence_required');
  if(command.status!=='resolved'&&(command.fixBuild!==null||command.evidence!==null||command.verificationResult!==null))throw new OpsInvestigationError(400,'ops_resolution_evidence_not_applicable');
  const now=Date.now(),auditId='audit_'+crypto.randomUUID();
  const state:State={errorLogId:id,status:command.status,version:current.version+1,reason:command.reason,fixBuild:command.fixBuild,evidence:command.evidence,verificationResult:command.verificationResult,updatedBy:actor.id,updatedAt:now,resolvedAt:command.status==='resolved'?now:null};
  const response={accepted:true as const,contractVersion:opsInvestigationVersion,investigation:state,auditId};
  try{
   await this.db.batch([
    this.db.prepare("INSERT INTO ops_error_investigation_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role=? AND role IN ('OWNER','OPERATOR') AND status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.id,actor.role),
    this.db.prepare(`INSERT INTO ops_error_investigation_guard SELECT CASE WHEN
     (?=0 AND NOT EXISTS(SELECT 1 FROM ops_error_investigations WHERE error_log_id=?) AND EXISTS(SELECT 1 FROM ops_error_logs WHERE id=? AND status=?))
     OR EXISTS(SELECT 1 FROM ops_error_investigations WHERE error_log_id=? AND version=?) THEN 1 ELSE 0 END`).bind(command.expectedVersion,id,id,current.status,id,command.expectedVersion),
    this.db.prepare(`INSERT INTO ops_error_investigations VALUES(?,?,?,?) ON CONFLICT(error_log_id) DO UPDATE SET version=excluded.version,state_json=excluded.state_json,updated_at=excluded.updated_at`).bind(id,state.version,canonicalJson(state),now),
    this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
     VALUES(?,?,?,'OPS_ERROR_LOG_WRITE','OPS_INVESTIGATION_TRANSITION','ops_error_investigation',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`).bind(auditId,actor.id,actor.role,id,crypto.randomUUID(),hash,canonicalJson({reason:command.reason,before:current,after:state}),now),
    this.db.prepare('INSERT INTO ops_error_investigation_receipts VALUES(?,?,?,?,?)').bind(actor.id,key,hash,canonicalJson(response),now),
    this.db.prepare('DELETE FROM ops_error_investigation_guard'),
   ]);
  }catch{
   const active=await this.db.prepare("SELECT id FROM admin_principals WHERE id=? AND role=? AND role IN ('OWNER','OPERATOR') AND status='ACTIVE'").bind(actor.id,actor.role).first();
   if(!active)throw new OpsInvestigationError(403,'ADMIN_PERMISSION_DENIED');
   const committed=await replay();if(committed)return committed;
   const latest=await this.read(id);
   if(latest&&latest.version!==command.expectedVersion)throw new OpsInvestigationError(409,'ops_investigation_version_conflict',latest);
   throw new OpsInvestigationError(503,'ops_investigation_outcome_unknown');
  }
  return response;
 }
 async detail(id:string){
  const investigation=await this.read(id);if(!investigation)throw new OpsInvestigationError(404,'ops_error_log_not_found');
  const anchor=await this.db.prepare(`SELECT environment,service,error_code,${opsDimensions.phase} AS phase,${opsDimensions.contractVersion} AS contractVersion FROM ops_error_logs WHERE id=?`).bind(id).first<{environment:string;service:string;error_code:string;phase:string|null;contractVersion:string|null}>();
  if(!anchor||investigation.resolvedAt===null||!investigation.fixBuild)return {contractVersion:opsInvestigationVersion,investigation,recurrence:{available:false,reason:anchor?'NOT_RESOLVED_WITH_BUILD':'ORIGINAL_EVENT_RETAINED_OUT'}};
  const fix=investigation.fixBuild;
  // Exact supplied target build identifiers, never guessed version ordering.
  const target=(Object.entries(fix) as Array<[keyof typeof fix,string|null]>).filter(([,v])=>v!==null);
  const match=target.map(([key])=>`${opsDimensions[key]} = ?`).join(' AND ');
  const unknown=target.map(([key])=>`${opsDimensions[key]} IS NULL`).join(' OR ');
  const row=await this.db.prepare(`WITH related AS (SELECT id,occurred_at,${opsDimensions.appVersion} AS appVersion,${opsDimensions.appVersionCode} AS appVersionCode,${opsDimensions.serverVersion} AS serverVersion,
    CASE WHEN ${unknown} THEN NULL WHEN ${match} THEN 1 ELSE 0 END AS targetBuild FROM ops_error_logs
    WHERE environment=? AND service=? AND error_code=? AND ${opsDimensions.phase} IS ? AND ${opsDimensions.contractVersion} IS ? AND occurred_at>? AND id!=?),
    page AS (SELECT * FROM related ORDER BY occurred_at,id LIMIT 50)
    SELECT COUNT(*) AS total,COALESCE(SUM(targetBuild),0) AS targets,COALESCE(SUM(targetBuild IS NULL),0) AS unknowns,
    (SELECT id FROM related WHERE targetBuild=1 ORDER BY occurred_at DESC,id LIMIT 1) AS latestTargetEventId,
    (SELECT json_group_array(json_object('id',id,'occurredAt',occurred_at,'appVersion',appVersion,'appVersionCode',appVersionCode,'serverVersion',serverVersion,'targetBuild',targetBuild)) FROM page) AS items FROM related`)
    .bind(...target.map(([,v])=>v!),anchor.environment,anchor.service,anchor.error_code,anchor.phase,anchor.contractVersion,investigation.resolvedAt,id).first<{total:number;targets:number;unknowns:number;latestTargetEventId:string|null;items:string}>();
  if(!row)throw new OpsInvestigationError(503,'ops_investigation_unavailable');
  return {contractVersion:opsInvestigationVersion,investigation,recurrence:{available:true,targetBuildRecurred:row.targets>0,targetBuildEventCount:row.targets,otherBuildEventCount:row.total-row.targets-row.unknowns,unknownBuildEventCount:row.unknowns,latestTargetEventId:row.latestTargetEventId,totalEventCount:row.total,items:JSON.parse(row.items) as unknown[],hasMore:row.total>50}};
 }
}
