import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {UnityBuildError,unityBuildProfile,unityBuildRequest} from './unity-build-management';
const id=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/u);
const stages=z.enum(['PREFLIGHT','SOURCE_PREPARATION','UNITY_BUILD','ARTIFACT_REGISTRATION','REGISTRATION_RECOVERY_REQUIRED','RESULT_UNKNOWN','SUCCEEDED','FAILED','CANCELLED']);
const progress=z.strictObject({version:z.literal('unity-build-attempt-v1'),attemptId:id,revision:z.number().int().positive().safe(),
 stage:stages,resumeStage:stages.nullable(),cancelRequested:z.boolean()});
const command=z.strictObject({expectedRevision:z.number().int().positive().safe(),
 kind:z.enum(['PREFLIGHT_PASSED','SOURCE_READY','BUILD_SUCCEEDED','ARTIFACTS_VERIFIED','REGISTRATION_FAILED','CONNECTION_LOST','RESUME','CANCEL_REQUESTED','CANCEL_CONFIRMED','FAILED']),
 evidenceRef:id.optional()});
type Progress=z.infer<typeof progress>;
type Row={id:string;status:string;payload_json:string;profile_json:string;progress_json:string|null;lease_owner:string|null;lease_expires_at:number|null};
type Attempt=Row&{runner_id:string;attempt_id:string};
const fail=(code:string):never=>{throw new UnityBuildError(409,code);};
/** Internal authenticated runner authority, not a client JSON readiness flag.
 * Claim proof must cover approved entry/scene/profile, isolated D source/output,
 * original commit/locks/config, modules/license/capacity and runner identity.
 * Result proof independently verifies the original process/BuildReport or all
 * required artifact bytes, manifest checksums and completion. FAILED and
 * CANCEL_CONFIRMED must prove the original job process is stopped; a transport
 * exception is not termination evidence (never an exit-code
 * or runner-supplied success boolean alone). Every mutable proof supplies CAS. */
export interface UnityBuildRunnerEvidence {
 prepare(input:{runnerId:string;jobId:string;attemptId:string;environment:string;request:z.infer<typeof unityBuildRequest>;profile:z.infer<typeof unityBuildProfile>}):Promise<readonly D1PreparedStatement[]>;
 verify?(input:{runnerId:string;jobId:string;attemptId:string;kind:'BUILD_SUCCEEDED'|'ARTIFACTS_VERIFIED'|'CANCEL_CONFIRMED'|'FAILED';evidenceRef:string}):Promise<readonly D1PreparedStatement[]>;
}
/** No HTTP endpoint, executable, secret, or timer. The runner transport must
 * authenticate its identity before calling. Expiration NEVER reassigns a job:
 * disconnected/expired work keeps the same attempt and runner until reconciled.
 * admin_jobs owns state; immutable attempt and receipt tables only pin identity. */
export class UnityBuildRunnerCoordinator{
 constructor(private readonly db:D1Database,private readonly environment:'DEV'|'PRD',private readonly evidence?:UnityBuildRunnerEvidence){}
 private guards(values:readonly D1PreparedStatement[]){
  if(!values.length||values.length>32||values.some(s=>typeof s.bind!=='function'))return fail('unity_build_evidence_missing');return [...values];
 }
 private async job(jobId:string){return this.db.prepare(`SELECT j.*,b.profile_json FROM admin_jobs j JOIN unity_build_requests b ON b.job_id=j.id
  WHERE j.id=? AND j.job_type='UNITY_CLIENT_BUILD' AND b.environment=?`).bind(jobId,this.environment).first<Row>();}
 async claim(runnerId:string,jobId:string,attemptId:string,leaseExpiresAt:number):Promise<Progress>{
  for(const value of [runnerId,jobId,attemptId])id.parse(value);
  if(!Number.isSafeInteger(leaseExpiresAt)||leaseExpiresAt<=Date.now())return fail('unity_build_lease_invalid');
  const existing=await this.db.prepare('SELECT job_id,runner_id FROM unity_build_attempts WHERE attempt_id=? AND environment=?').bind(attemptId,this.environment).first<{job_id:string;runner_id:string}>();
  const row=await this.job(jobId);if(!row)return fail('unity_build_not_found');
  if(existing){
   if(existing.job_id!==jobId||existing.runner_id!==runnerId||!row.progress_json)return fail('unity_build_attempt_conflict');
   return progress.parse(JSON.parse(row.progress_json));
  }
  if(!this.evidence)return fail('unity_build_runner_not_ready');
  if(!await this.db.prepare("SELECT 1 FROM admin_jobs j JOIN admin_principals p ON p.id=j.created_by WHERE j.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR')").bind(jobId).first())return fail('unity_build_creator_permission_revoked');
  const recoverClaim=async()=>{
   const won=await this.db.prepare('SELECT job_id,runner_id FROM unity_build_attempts WHERE attempt_id=? AND environment=?').bind(attemptId,this.environment).first<{job_id:string;runner_id:string}>();
   if(!won)return null;if(won.job_id!==jobId||won.runner_id!==runnerId)return fail('unity_build_attempt_conflict');
   const current=await this.job(jobId);if(!current?.progress_json)return fail('unity_build_attempt_conflict');
   const p=progress.parse(JSON.parse(current.progress_json));if(p.attemptId!==attemptId)return fail('unity_build_attempt_conflict');return p;
  };
  if(row.status!=='PENDING'||row.lease_owner!==null){const won=await recoverClaim();if(won)return won;return fail('unity_build_attempt_conflict');}
  if(await this.db.prepare("SELECT 1 FROM admin_jobs WHERE job_type='UNITY_CLIENT_BUILD' AND lease_owner=? AND status IN ('PENDING','RUNNING')").bind(runnerId).first()){
   const won=await recoverClaim();if(won)return won;return fail('unity_build_runner_busy');
  }
  const request=unityBuildRequest.parse(JSON.parse(row.payload_json)),profile=unityBuildProfile.parse(JSON.parse(row.profile_json));
  const assertions=this.guards(await this.evidence.prepare({runnerId,jobId,attemptId,environment:this.environment,request,profile}));
  const next:Progress={version:'unity-build-attempt-v1',attemptId,revision:1,stage:'PREFLIGHT',resumeStage:null,cancelRequested:false};
  try{await this.db.batch([
   this.db.prepare(`INSERT INTO unity_build_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_jobs j JOIN admin_principals p ON p.id=j.created_by WHERE j.id=? AND j.status='PENDING' AND j.lease_owner IS NULL AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR'))
    AND NOT EXISTS(SELECT 1 FROM admin_jobs WHERE job_type='UNITY_CLIENT_BUILD' AND lease_owner=? AND status IN ('PENDING','RUNNING')) THEN 1 ELSE 0 END`).bind(jobId,runnerId),
   ...assertions,
   this.db.prepare('INSERT INTO unity_build_attempts VALUES(?,?,?,?,?,?,?)').bind(attemptId,jobId,runnerId,this.environment,Date.now(),request.commit,await sha256Hex(canonicalJson(profile))),
   this.db.prepare("UPDATE admin_jobs SET status='RUNNING',progress_json=?,lease_owner=?,lease_expires_at=?,updated_at=MAX(updated_at+1,?) WHERE id=?").bind(canonicalJson(next),runnerId,leaseExpiresAt,Date.now(),jobId),
   this.db.prepare('DELETE FROM unity_build_write_guard')]);}
  catch(error){
   const winner=await recoverClaim();if(winner)return winner;
   if(!await this.db.prepare("SELECT 1 FROM admin_jobs j JOIN admin_principals p ON p.id=j.created_by WHERE j.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR')").bind(jobId).first())return fail('unity_build_creator_permission_revoked');
   throw error;
  }
  return next;
 }
 async report(runnerId:string,attemptId:string,eventId:string,input:unknown):Promise<Progress>{
  for(const value of [runnerId,attemptId,eventId])id.parse(value);
  const c=command.parse(input);
  const row=await this.db.prepare(`SELECT j.*,b.profile_json,a.runner_id,a.attempt_id FROM unity_build_attempts a JOIN admin_jobs j ON j.id=a.job_id
   JOIN unity_build_requests b ON b.job_id=j.id WHERE a.attempt_id=? AND a.environment=? AND b.environment=?`).bind(attemptId,this.environment,this.environment).first<Attempt>();
  if(!row||row.runner_id!==runnerId||!row.progress_json)return fail('unity_build_attempt_conflict');
  const digest=await sha256Hex(canonicalJson(z.json().parse(c)));
  const replay=async()=>{
   const prior=await this.db.prepare('SELECT payload_hash,response_json FROM unity_build_runner_receipts WHERE attempt_id=? AND event_id=?').bind(attemptId,eventId).first<{payload_hash:string;response_json:string}>();
   if(!prior)return null;if(prior.payload_hash!==digest)return fail('idempotency_conflict');return progress.parse(JSON.parse(prior.response_json));
  };
  const prior=await replay();if(prior)return prior;
  if(row.status!=='RUNNING'||row.lease_owner!==runnerId){const winner=await replay();if(winner)return winner;return fail('unity_build_attempt_conflict');}
  const before=progress.parse(JSON.parse(row.progress_json));
  if(before.attemptId!==attemptId||before.revision!==c.expectedRevision){const winner=await replay();if(winner)return winner;return fail('unity_build_revision_conflict');}
  const next:Progress={...before,revision:before.revision+1};let status='RUNNING';
  const assertions:D1PreparedStatement[]=[];
  if(c.kind==='CONNECTION_LOST'){if(before.stage==='RESULT_UNKNOWN')return fail('unity_build_transition_invalid');next.resumeStage=before.stage;next.stage='RESULT_UNKNOWN';}
  else if(c.kind==='RESUME'){if(before.stage!=='RESULT_UNKNOWN'||!before.resumeStage)return fail('unity_build_transition_invalid');next.stage=before.resumeStage;next.resumeStage=null;}
  else if(c.kind==='CANCEL_REQUESTED'){next.cancelRequested=true;}
  else if(c.kind==='CANCEL_CONFIRMED'){
   if(!before.cancelRequested)return fail('unity_build_transition_invalid');next.stage='CANCELLED';status='CANCELLED';
  }else if(c.kind==='FAILED'){if(before.stage==='RESULT_UNKNOWN')return fail('unity_build_reconcile_required');next.stage='FAILED';status='FAILED';}
  else {
   const transitions={PREFLIGHT_PASSED:['PREFLIGHT','SOURCE_PREPARATION'],SOURCE_READY:['SOURCE_PREPARATION','UNITY_BUILD'],
    BUILD_SUCCEEDED:['UNITY_BUILD','ARTIFACT_REGISTRATION'],REGISTRATION_FAILED:['ARTIFACT_REGISTRATION','REGISTRATION_RECOVERY_REQUIRED'],
    ARTIFACTS_VERIFIED:['ARTIFACT_REGISTRATION','SUCCEEDED']} as const;
   const pair=transitions[c.kind];
   if(before.stage!==pair[0]&&!(c.kind==='ARTIFACTS_VERIFIED'&&before.stage==='REGISTRATION_RECOVERY_REQUIRED'))return fail('unity_build_transition_invalid');
   next.stage=pair[1];if(next.stage==='SUCCEEDED')status='SUCCEEDED';
  }
  if(c.kind==='BUILD_SUCCEEDED'||c.kind==='ARTIFACTS_VERIFIED'||c.kind==='CANCEL_CONFIRMED'||c.kind==='FAILED'){
   if(!c.evidenceRef||!this.evidence?.verify)return fail('unity_build_result_evidence_unavailable');
   assertions.push(...this.guards(await this.evidence.verify({runnerId,jobId:row.id,attemptId,kind:c.kind,evidenceRef:c.evidenceRef})));
  }
  try{await this.db.batch([
   this.db.prepare("INSERT INTO unity_build_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_jobs WHERE id=? AND status='RUNNING' AND lease_owner=? AND progress_json=?) THEN 1 ELSE 0 END").bind(row.id,runnerId,row.progress_json),
   ...assertions,
   this.db.prepare("UPDATE admin_jobs SET status=?,progress_json=?,updated_at=MAX(updated_at+1,?) WHERE id=?").bind(status,canonicalJson(next),Date.now(),row.id),
   this.db.prepare('INSERT INTO unity_build_runner_receipts VALUES(?,?,?,?)').bind(attemptId,eventId,digest,canonicalJson(next)),
   this.db.prepare('DELETE FROM unity_build_write_guard')]);}
  catch(error){const winner=await replay();if(winner)return winner;throw error;}
  return next;
 }
}
