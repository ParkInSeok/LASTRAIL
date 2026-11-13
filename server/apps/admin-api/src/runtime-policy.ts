import {z} from 'zod';
import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {readAdRuntimePolicy} from '../../game-api/src/ad-runtime-policy';

export const runtimePolicyReadPermission='RUNTIME_POLICY_READ';
export const runtimePolicyWritePermission='RUNTIME_POLICY_WRITE';
export type RuntimePolicyPrincipal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export const runtimePolicyWriteSchema=z.strictObject({environment:z.enum(['DEV','PRD']),
 expectedRuntimePolicyRevision:z.number().int().positive().safe().nullable(),adsEnabled:z.boolean(),reason:z.string().trim().min(1).max(500)});
type Command=z.infer<typeof runtimePolicyWriteSchema>;
export class RuntimePolicyError extends Error {
 constructor(readonly status:400|403|409|503,readonly code:string){super(code);}
}
type Receipt={payload_hash:string;response_json:string};
type StoredPolicy={revision:number;ads_enabled:number};
const validStored=z.strictObject({revision:z.number().int().positive().safe(),ads_enabled:z.union([z.literal(0),z.literal(1)])});

export async function readAdminRuntimePolicy(db:D1Database,environment:string,principal:RuntimePolicyPrincipal) {
 const policy=await readAdRuntimePolicy(db,environment);
 return {...policy,contractVersion:'admin-runtime-policy-v1',permission:runtimePolicyReadPermission,permissionMode:'ROLE_PERMISSION',
  canWrite:principal.role==='OWNER',canEnable:false,enableBlockReason:'ad_enable_readiness_unavailable'};
}
async function prior(db:D1Database,principal:string,environment:string,key:string,hash:string) {
 const receipt=await db.prepare('SELECT payload_hash,response_json FROM admin_runtime_policy_receipts WHERE principal_id=? AND environment=? AND request_key=?')
  .bind(principal,environment,key).first<Receipt>();
 if(!receipt)return null;
 if(receipt.payload_hash!==hash)throw new RuntimePolicyError(409,'idempotency_conflict');
 return JSON.parse(receipt.response_json) as Record<string,unknown>;
}
function audit(db:D1Database,principal:RuntimePolicyPrincipal,id:string,request:string,hash:string,summary:Parameters<typeof canonicalJson>[0] & {environment:string},result:'SUCCESS'|'FAILURE',code:string|null) {
 return db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,
 confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
 VALUES(?,?,?,?,'RUNTIME_POLICY_UPDATE','ad_runtime_policy',?,?,NULL,?,?,NULL,?,?,?)`)
 .bind(id,principal.id,principal.role,runtimePolicyWritePermission,summary.environment,request,hash,
  canonicalJson(summary),result,code,Date.now());
}
/** Actor/env already authenticated by the existing Admin boundary. The OWNER check
 * is repeated in the same D1 batch as policy, success audit, and immutable receipt. */
export async function saveAdminRuntimePolicy(db:D1Database,environment:string,principal:RuntimePolicyPrincipal,command:Command,key:string) {
 if(command.environment!==environment)throw new RuntimePolicyError(400,'runtime_policy_environment_mismatch');
 if(principal.role!=='OWNER')throw new RuntimePolicyError(403,'ADMIN_PERMISSION_DENIED');
 const hash=await sha256Hex(canonicalJson(command)),requestId=createRequestId(),auditId='audit_'+crypto.randomUUID();
 try {
  const replay=await prior(db,principal.id,environment,key,hash);if(replay)return replay;
  // No trusted readiness evidence owner exists yet. Never accept a browser-ready bool.
  if(command.adsEnabled)throw new RuntimePolicyError(503,'ad_enable_readiness_unavailable');
  const before=await db.prepare('SELECT revision,ads_enabled FROM ad_runtime_policies WHERE environment=?').bind(environment).first<StoredPolicy>();
  if((before&&!validStored.safeParse(before).success)||(before?.revision??null)!==command.expectedRuntimePolicyRevision || before?.revision===Number.MAX_SAFE_INTEGER){
   // Another identical request may have committed after the first receipt read.
   const replay=await prior(db,principal.id,environment,key,hash);if(replay)return replay;
   throw new RuntimePolicyError(409,'runtime_policy_revision_conflict');
  }
  const next=(before?.revision??0)+1;
  const response={accepted:true,contractVersion:'admin-runtime-policy-write-v1',environment,adsEnabled:command.adsEnabled,
   runtimePolicyRevision:next,auditId,requestId};
  try {
   await db.batch([
    db.prepare(`INSERT INTO admin_runtime_policy_guard SELECT CASE WHEN EXISTS(
      SELECT 1 FROM admin_principals WHERE id=? AND status='ACTIVE' AND role='OWNER') THEN 1 ELSE 0 END`).bind(principal.id),
    db.prepare(`INSERT INTO admin_runtime_policy_guard SELECT CASE WHEN
      (? IS NULL AND NOT EXISTS(SELECT 1 FROM ad_runtime_policies WHERE environment=?))
      OR EXISTS(SELECT 1 FROM ad_runtime_policies WHERE environment=? AND revision=? AND typeof(revision)='integer'
        AND revision BETWEEN 1 AND 9007199254740990 AND ads_enabled IN (0,1)) THEN 1 ELSE 0 END`)
      .bind(command.expectedRuntimePolicyRevision,environment,environment,command.expectedRuntimePolicyRevision),
    db.prepare(`INSERT INTO ad_runtime_policies VALUES(?,?,?) ON CONFLICT(environment)
      DO UPDATE SET revision=excluded.revision,ads_enabled=excluded.ads_enabled`).bind(environment,next,command.adsEnabled?1:0),
    audit(db,principal,auditId,requestId,hash,{environment,reason:command.reason,before:before?{adsEnabled:before.ads_enabled===1,runtimePolicyRevision:before.revision}:null,
      after:{adsEnabled:command.adsEnabled,runtimePolicyRevision:next}},'SUCCESS',null),
    db.prepare('INSERT INTO admin_runtime_policy_receipts VALUES(?,?,?,?,?,?)').bind(principal.id,environment,key,hash,auditId,canonicalJson(response)),
    db.prepare('DELETE FROM admin_runtime_policy_guard')
   ]);
  }catch(error){
   const replay=await prior(db,principal.id,environment,key,hash);if(replay)return replay;
   if(/CHECK constraint failed: valid=1|ad_runtime_policy_revision_conflict/u.test(String(error))) {
    const actor=await db.prepare("SELECT id FROM admin_principals WHERE id=? AND status='ACTIVE' AND role='OWNER'").bind(principal.id).first();
    if(!actor)throw new RuntimePolicyError(403,'ADMIN_PERMISSION_DENIED');
    throw new RuntimePolicyError(409,'runtime_policy_revision_conflict');
   }
   throw error;
  }
  return response;
 }catch(error){
  const failure=error instanceof RuntimePolicyError?error:new RuntimePolicyError(503,'runtime_policy_unavailable');
  await audit(db,principal,auditId,requestId,hash,{environment,reason:command.reason,requestedAdsEnabled:command.adsEnabled,
   expectedRuntimePolicyRevision:command.expectedRuntimePolicyRevision},'FAILURE',failure.code).run();
  throw failure;
 }
}
