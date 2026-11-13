import {z} from 'zod';
import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import type {RuntimePolicyPrincipal} from './runtime-policy';
export const unityBuildVersion='admin.unity-build-management.v1.20261020';
export const unityBuildReadPermission='SERVER_DEPLOYMENT_READ',unityBuildWritePermission='UNITY_BUILD_WRITE';
const id=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/u),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const environment=z.enum(['DEV','PRD']);
export const unityBuildProfile=z.strictObject({id,platform:id,environment,unityVersion:id,configurationHash:hash,dependencyLockHash:hash});
export type UnityBuildProfile=z.infer<typeof unityBuildProfile>;
export const unityBuildRequest=z.strictObject({environment,profileId:id,platform:id,commit:z.string().regex(/^[a-f0-9]{40}$/u),
 contentVersion:id,appVersion:id,buildNumber:z.number().int().positive().safe(),note:z.string().trim().min(1).max(500)});
export const unityBuildCancel=z.strictObject({expectedUpdatedAt:z.number().int().nonnegative().safe(),reason:z.string().trim().min(1).max(500)});
export class UnityBuildError extends Error{constructor(readonly status:400|403|404|409|503,readonly code:string){super(code);}}
const fail=(status:400|403|404|409|503,code:string):never=>{throw new UnityBuildError(status,code);};
type JobRow={id:string;status:string;payload_json:string;progress_json:string|null;lease_owner:string|null;lease_expires_at:number|null;created_at:number;updated_at:number;profile_json:string};
function view(row:JobRow){
 const request=unityBuildRequest.parse(JSON.parse(row.payload_json)),profile=unityBuildProfile.parse(JSON.parse(row.profile_json));
 const progress=row.progress_json?z.object({stage:z.string().regex(/^[A-Z_]{1,64}$/u),cancelRequested:z.boolean()}).parse(JSON.parse(row.progress_json)):null;
 // No public raw progress, runner IDs, filesystem paths or signing references.
 return {jobId:row.id,status:row.status,stage:progress?.stage??(row.status==='PENDING'&&!row.lease_owner?'WAITING_FOR_RUNNER':row.status),cancelRequested:progress?.cancelRequested??false,
  runnerReady:false,request,profile,createdAt:row.created_at,updatedAt:row.updated_at,
  logsStatus:'UNAVAILABLE',artifactsStatus:'UNAVAILABLE',installationStatus:'NOT_VERIFIED',deploymentTriggered:false};
}
type Response={accepted:true;contractVersion:string;environment:string;job:ReturnType<typeof view>};
/** Server-owned profile definitions only; request JSON never installs a profile.
 * No runner transport is installed. PENDING requests cannot execute here.
 * Add dispatch/output integration only after its original Unity contract is supplied. */
export class UnityBuildManagement{
 private readonly profiles:UnityBuildProfile[];
 constructor(private readonly db:D1Database,private readonly target:string,profiles:readonly UnityBuildProfile[]=[]){
  environment.parse(target);this.profiles=profiles.map(p=>unityBuildProfile.parse(structuredClone(p)));
  if(new Set(this.profiles.map(p=>p.environment+':'+p.id)).size!==this.profiles.length)throw Error('duplicate_unity_build_profile');
 }
 capabilities(){return {contractVersion:unityBuildVersion,environment:this.target,readiness:'NOT_READY',runnerReady:false,
  blockingReasons:['unity_build_runner_unavailable',...(!this.profiles.some(p=>p.environment===this.target)?['unity_build_profile_not_ready']:[])],
  profiles:this.profiles.filter(p=>p.environment===this.target).map(p=>({...p,canExecute:false})),
  approvedSequence:['WINDOWS_DEV','ANDROID_DEV_APK','ANDROID_PRD_AAB'],maximumConcurrentJobsPerRunner:1,
  mutationPermission:unityBuildWritePermission,mutationPermissionMapping:'OWNER_OPERATOR',canExecute:false,canDeploy:false};}
 private async row(jobId:string){
  id.parse(jobId);
  return this.db.prepare(`SELECT j.*,b.profile_json FROM admin_jobs j JOIN unity_build_requests b ON b.job_id=j.id
   WHERE j.id=? AND j.job_type='UNITY_CLIENT_BUILD' AND b.environment=?`).bind(jobId,this.target).first<JobRow>();
 }
 async read(jobId:string){const row=await this.row(jobId);if(!row)return fail(404,'unity_build_not_found');return {contractVersion:unityBuildVersion,environment:this.target,job:view(row)};}
 async list(after='',limit=20){
  if(after)id.parse(after);if(!Number.isInteger(limit)||limit<1||limit>50)return fail(400,'contract_violation');
  const rows=await this.db.prepare(`SELECT j.*,b.profile_json FROM admin_jobs j JOIN unity_build_requests b ON b.job_id=j.id
   WHERE j.job_type='UNITY_CLIENT_BUILD' AND b.environment=? AND j.id>? ORDER BY j.id LIMIT ?`).bind(this.target,after,limit+1).all<JobRow>();
  const items=rows.results.slice(0,limit).map(view);
  return {contractVersion:unityBuildVersion,environment:this.target,items,nextAfter:rows.results.length>limit?items.at(-1)!.jobId:null};
 }
 private async authorize(actor:RuntimePolicyPrincipal,key:string){
  if(!['OWNER','OPERATOR'].includes(actor.role))return fail(403,'admin_permission_denied');
  if(!/^[\x21-\x7e]{1,128}$/u.test(key))return fail(400,'idempotency_key_required');
  if(!await this.db.prepare("SELECT 1 FROM admin_principals WHERE id=? AND role=? AND role IN ('OWNER','OPERATOR') AND status='ACTIVE'").bind(actor.id,actor.role).first())return fail(403,'admin_permission_denied');
 }
 private guard(actor:RuntimePolicyPrincipal){return this.db.prepare("INSERT INTO unity_build_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role=? AND role IN ('OWNER','OPERATOR') AND status='ACTIVE') THEN 1 ELSE 0 END").bind(actor.id,actor.role);}
 private async prior(actor:RuntimePolicyPrincipal,key:string,digest:string){
  const row=await this.db.prepare('SELECT payload_hash,response_json FROM unity_build_receipts WHERE principal_id=? AND environment=? AND request_key=?').bind(actor.id,this.target,key).first<{payload_hash:string;response_json:string}>();
  if(!row)return null;if(row.payload_hash!==digest)return fail(409,'idempotency_conflict');return JSON.parse(row.response_json) as Response;
 }
 private finish(actor:RuntimePolicyPrincipal,key:string,digest:string,response:Response,action:string){
  return [
   this.db.prepare('INSERT INTO unity_build_receipts VALUES(?,?,?,?,?)').bind(actor.id,this.target,key,digest,canonicalJson(response)),
   this.db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,payload_hash,change_summary_json,result,created_at)
    VALUES(?,?,?,?,?,'UNITY_CLIENT_BUILD',?,?,?,?,'SUCCESS',?)`).bind(createRequestId(),actor.id,actor.role,unityBuildWritePermission,action,response.job.jobId,createRequestId(),digest,
     canonicalJson({environment:this.target,status:response.job.status,externalExecution:false}),Date.now()),
   this.db.prepare('DELETE FROM unity_build_write_guard')];
 }
 async request(actor:RuntimePolicyPrincipal,input:unknown,key:string):Promise<Response>{
  await this.authorize(actor,key);
  const parsed=unityBuildRequest.safeParse(input);if(!parsed.success)return fail(400,'contract_violation');const request=parsed.data;
  if(request.environment!==this.target)return fail(400,'unity_build_environment_conflict');
  const digest=await sha256Hex(canonicalJson({operation:'REQUEST',request}));
  const prior=await this.prior(actor,key,digest);if(prior)return prior;
  const profile=this.profiles.find(p=>p.environment===this.target&&p.id===request.profileId&&p.platform===request.platform);
  if(!profile)return fail(409,'unity_build_profile_not_ready');
  const jobId=createRequestId(),now=Date.now(),payload=canonicalJson(request),profileJson=canonicalJson(profile);
  const response:Response={accepted:true,contractVersion:unityBuildVersion,environment:this.target,job:view({id:jobId,status:'PENDING',payload_json:payload,profile_json:profileJson,progress_json:null,lease_owner:null,lease_expires_at:null,created_at:now,updated_at:now})};
  try{await this.db.batch([this.guard(actor),
   this.db.prepare("INSERT INTO admin_jobs(id,job_type,status,payload_json,progress_json,created_by,created_at,updated_at) VALUES(?,'UNITY_CLIENT_BUILD','PENDING',?,NULL,?,?,?)").bind(jobId,payload,actor.id,now,now),
   this.db.prepare('INSERT INTO unity_build_requests VALUES(?,?,?,?,?)').bind(jobId,this.target,request.platform,request.buildNumber,profileJson),
   ...this.finish(actor,key,digest,response,'UNITY_BUILD_REQUEST')]);}
  catch(error){
   await this.authorize(actor,key);
   const winner=await this.prior(actor,key,digest);if(winner)return winner;
   if(await this.db.prepare('SELECT 1 FROM unity_build_requests WHERE environment=? AND platform=? AND build_number=?').bind(this.target,request.platform,request.buildNumber).first())return fail(409,'unity_build_number_conflict');
   throw error;
  }
  return response;
 }
 async cancel(actor:RuntimePolicyPrincipal,jobId:string,input:unknown,key:string):Promise<Response>{
  await this.authorize(actor,key);
  const parsed=unityBuildCancel.safeParse(input);if(!parsed.success)return fail(400,'contract_violation');
  const command=parsed.data,digest=await sha256Hex(canonicalJson({operation:'CANCEL',jobId,command}));
  const prior=await this.prior(actor,key,digest);if(prior)return prior;
  const conflict=async(status:404|409,code:string):Promise<Response>=>{
   await this.authorize(actor,key);
   const winner=await this.prior(actor,key,digest);if(winner)return winner;
   return fail(status,code);
  };
  const row=await this.row(jobId);if(!row)return conflict(404,'unity_build_not_found');
  const running=row.status==='RUNNING'&&row.lease_owner!==null&&row.progress_json!==null;
  if(!running&&(row.status!=='PENDING'||row.lease_owner!==null||row.lease_expires_at!==null))return conflict(409,'unity_build_cancel_requires_runner');
  if(row.updated_at!==command.expectedUpdatedAt)return conflict(409,'unity_build_revision_conflict');
  const updatedAt=Math.max(Date.now(),row.updated_at+1);
  // A running process remains RUNNING until the original runner independently
  // confirms that this job's process stopped; a request is not completion.
  const p=running?z.object({version:z.literal('unity-build-attempt-v1'),revision:z.number().int().positive().safe(),cancelRequested:z.boolean()}).passthrough().parse(JSON.parse(row.progress_json!)):null;
  const nextProgress=p?canonicalJson({...p,revision:p.revision+1,cancelRequested:true}):row.progress_json;
  const response:Response={accepted:true,contractVersion:unityBuildVersion,environment:this.target,job:{...view({...row,status:running?'RUNNING':'CANCELLED',progress_json:nextProgress,updated_at:updatedAt}),...{cancelRequested:running}}};
  try{await this.db.batch([this.guard(actor),
   this.db.prepare(`INSERT INTO unity_build_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_jobs WHERE id=? AND status=? AND updated_at=? AND lease_owner IS ? AND lease_expires_at IS ? AND progress_json IS ?) THEN 1 ELSE 0 END`).bind(jobId,row.status,row.updated_at,row.lease_owner,row.lease_expires_at,row.progress_json),
   this.db.prepare("UPDATE admin_jobs SET status=?,progress_json=?,updated_at=? WHERE id=?").bind(running?'RUNNING':'CANCELLED',nextProgress,updatedAt,jobId),
   ...this.finish(actor,key,digest,response,'UNITY_BUILD_CANCEL')]);}
  catch(error){
   await this.authorize(actor,key);
   const winner=await this.prior(actor,key,digest);if(winner)return winner;
   const latest=await this.row(jobId);
   if(!latest||latest.updated_at!==row.updated_at||latest.status!==row.status||latest.progress_json!==row.progress_json||latest.lease_owner!==row.lease_owner||latest.lease_expires_at!==row.lease_expires_at)return fail(409,'unity_build_revision_conflict');
   throw error;
  }
  return response;
 }
 async logs(jobId:string,after:number){await this.read(jobId);if(!Number.isSafeInteger(after)||after<0)return fail(400,'contract_violation');
  return {contractVersion:unityBuildVersion,jobId,status:'UNAVAILABLE',reason:'unity_build_log_source_unavailable',after,nextAfter:null,entries:[]};}
 async artifacts(jobId:string){await this.read(jobId);return {contractVersion:unityBuildVersion,jobId,status:'UNAVAILABLE',reason:'unity_build_artifact_source_unavailable',items:[]};}
 async download(jobId:string,artifactId:string):Promise<never>{id.parse(artifactId);await this.read(jobId);return fail(503,'unity_build_artifact_source_unavailable');}
}
