import {prepareLegacyRestoreBoundary} from "./combat-legacy-restore";
import {prepareAugmentRestore} from "./combat-augment-restore";
import {hostRetention,suspendCommand,suspensionReceipt,suspendHost,resumeStoredHost} from "./combat-host-retention";
import { requireRunControl } from "./combat-coop-controls";
import { planHostResume, type HostRestoreVerifier } from "./combat-host-resume";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { readHostContext, validateHostReport } from "./combat-host-report";
import { readCombatAuthority } from "./combat-authority";
const id=z.string().min(1).max(200),revision=z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const command=z.strictObject({requestId:id,expectedRevision:revision.max(Number.MAX_SAFE_INTEGER-1),report:z.unknown()});
const receipt=z.strictObject({version:z.literal("combat.host-report-receipt.v1"),requestId:id,revision,
 stateSequence:revision,combatVerification:z.literal("NOT_PERFORMED")});
function encodeContext(c:ReturnType<typeof readHostContext>){
 const {activeAccountIds,...required}=c;
 return activeAccountIds===undefined?canonicalJson(required):canonicalJson({...required,activeAccountIds});
}
const recoveryWindowMs=60000;
const timestamp=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-recoveryWindowMs);
export const recoverySchema=z.strictObject({startedAt:timestamp,deadlineAt:z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),disconnectedAccountIds:z.array(id).min(1).max(2)})
 .refine(r=>r.deadlineAt===r.startedAt+recoveryWindowMs && new Set(r.disconnectedAccountIds).size===r.disconnectedAccountIds.length);
const disconnectCommand=z.strictObject({requestId:id,expectedRevision:revision.max(Number.MAX_SAFE_INTEGER-1),disconnectedAccountId:id,observedAt:timestamp});
const recoveryReceipt=z.strictObject({version:z.literal("combat.host-recovery-receipt.v1"),requestId:id,revision,recovery:recoverySchema});
const resumeCommand=z.strictObject({requestId:id,expectedRevision:revision.max(Number.MAX_SAFE_INTEGER-1),mode:z.enum(["REJOIN","SOLO"])});
const restoreEvidence=z.strictObject({checkpointId:id,transactionCursor:id,augmentCursor:id.optional(),verifiedAt:z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),restoredAccountIds:z.array(id).min(1).max(2)});
const resumeReceipt=z.strictObject({version:z.literal("combat.host-resume-receipt.v1"),requestId:id,revision,mode:z.enum(["REJOIN","SOLO"]),hostAccountId:id,hostGeneration:revision,activeAccountIds:z.array(id).min(1).max(2),evidence:restoreEvidence});
type Row={revision:number;initial_hash:string;start_snapshot_json:string;context_json:string;recovery_json:string|null;retention_json:string|null};
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** Internal server-composed context only. No client bootstrap, host selection, payload checkpoint or reward API. */
export class CombatHostReportStore {
 constructor(private readonly db:D1Database,private readonly restoreVerifier?:HostRestoreVerifier){}
 private row(runId:string){id.parse(runId);return this.db.prepare("SELECT revision,initial_hash,start_snapshot_json,context_json,recovery_json,retention_json FROM combat_host_report_states WHERE run_id=?").bind(runId).first<Row>();}
 private writeGuard(runId:string,row:Row){return this.db.prepare("INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN combat_host_report_states h ON h.run_id=r.id WHERE r.id=? AND r.status='ACTIVE' AND s.snapshot_json=? AND h.revision=? AND h.context_json=? AND h.recovery_json IS ? AND h.retention_json IS ?) AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) THEN 1 ELSE 0 END").bind(runId,row.start_snapshot_json,row.revision,row.context_json,row.recovery_json,row.retention_json,runId);}
 private async run(runId:string){
  const r=await this.db.prepare("SELECT r.account_id,r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=?").bind(runId).first<{account_id:string;status:string;snapshot_json:string}>();
  if(!r)return fail("combat_host_run_missing");
  if(await this.db.prepare("SELECT 1 FROM combat_result_reviews WHERE run_id=?").bind(runId).first())return fail("combat_host_result_review");return r;
 }
 private async access(sender:string,runId:string){
  id.parse(sender);const row=await this.row(runId);if(!row)return fail("combat_host_state_missing");
  const context=readHostContext(JSON.parse(row.context_json));
  if(!context.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_host_access_forbidden");
  return {row,context};
 }
 async read(sender:string,runId:string){const {row,context}=await this.access(sender,runId);return {revision:row.revision,context,...(row.retention_json?{retention:hostRetention.parse(JSON.parse(row.retention_json))}:{}),...(row.recovery_json?{recovery:recoverySchema.parse(JSON.parse(row.recovery_json))}:{})};}
 async lookup(sender:string,runId:string,requestId:string){
  id.parse(requestId);await this.access(sender,runId);
  const r=await this.db.prepare("SELECT response_json FROM combat_host_report_receipts WHERE run_id=? AND request_id=? AND sender_id=?").bind(runId,requestId,sender).first<{response_json:string}>();
  return r?receipt.parse(JSON.parse(r.response_json)):null;
 }
 async initialize(input:unknown){
  const context=readHostContext(input),run=await this.run(context.runId);
  const snapshot=z.object({admission:z.object({combatAuthority:z.unknown(),combatPolicyRequirements:z.object({version:z.string()})})}).safeParse(JSON.parse(run.snapshot_json));
  if(!snapshot.success||readCombatAuthority(snapshot.data.admission.combatAuthority).contract?.mode!=="ORDINARY_COOP"
   ||snapshot.data.admission.combatPolicyRequirements.version!==context.definitionVersion||!context.participants.some(p=>p.accountId===run.account_id))return fail("combat_host_start_mismatch");
  const initialHash=await sha256Hex(canonicalJson({contextJson:encodeContext(context),startSnapshot:run.snapshot_json}));
  const existing=await this.row(context.runId);
  if(existing){if(existing.initial_hash!==initialHash)return fail("combat_host_initialization_conflict");return this.read(run.account_id,context.runId);}
  if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.status='ACTIVE' AND s.snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM combat_host_report_states WHERE run_id=?) AND NOT EXISTS(SELECT 1 FROM active_combat_runs WHERE id<>? AND account_id IN (SELECT value FROM json_each(?))) AND NOT EXISTS(SELECT 1 FROM json_each(?) p WHERE NOT EXISTS(SELECT 1 FROM accounts WHERE id=p.value)) THEN 1 ELSE 0 END").bind(context.runId,run.snapshot_json,context.runId,context.runId,JSON.stringify(context.activeAccountIds??context.participants.map(p=>p.accountId)),JSON.stringify(context.participants.map(p=>p.accountId))),
   this.db.prepare("INSERT INTO combat_host_report_states(run_id,revision,initial_hash,start_snapshot_json,context_json) VALUES(?,1,?,?,?)").bind(context.runId,initialHash,run.snapshot_json,encodeContext(context)),
   this.db.prepare("INSERT INTO combat_host_lineage VALUES(?,?,NULL,'INITIAL',?)").bind(context.runId,context.hostGeneration,encodeContext(context)),
   this.db.prepare("DELETE FROM combat_host_write_guard")]);
  }catch(error){if((await this.row(context.runId))?.initial_hash!==initialHash)throw error;}
  return this.read(run.account_id,context.runId);
 }
 async commit(sender:string,runId:string,input:unknown){
  const c=command.parse(input),{row,context}=await this.access(sender,runId),hash=await sha256Hex(canonicalJson({sender,command:c} as Parameters<typeof canonicalJson>[0]));
  const replay=async()=>{
   const r=await this.db.prepare("SELECT sender_id,request_hash,response_json FROM combat_host_report_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{sender_id:string;request_hash:string;response_json:string}>();
   if(!r)return null;if(r.sender_id!==sender||r.request_hash!==hash)return fail("combat_host_request_conflict");return receipt.parse(JSON.parse(r.response_json));
  };
  const original=await replay();if(original)return original;
  const run=await this.run(runId);if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  if(run.snapshot_json!==row.start_snapshot_json)return fail("combat_host_start_mismatch");
  if(row.revision!==c.expectedRevision)return fail("combat_host_revision_conflict");
  if(row.recovery_json!==null||(row.retention_json&&hostRetention.parse(JSON.parse(row.retention_json)).suspended))return fail("combat_host_recovery_required");
  const validated=validateHostReport(context,sender,c.report);
  const next={...context,lastStateSequence:validated.report.stateSequence,logicalTimeMs:validated.report.logicalTimeMs};
  const result=receipt.parse({version:"combat.host-report-receipt.v1",requestId:c.requestId,revision:row.revision+1,stateSequence:next.lastStateSequence,combatVerification:"NOT_PERFORMED"});
  try{await this.db.batch([
   this.writeGuard(runId,row),
   this.db.prepare("UPDATE combat_host_report_states SET revision=?,context_json=? WHERE run_id=? AND revision=?").bind(result.revision,encodeContext(next),runId,row.revision),
   this.db.prepare("INSERT INTO combat_host_report_receipts VALUES(?,?,?,?,?,?)").bind(runId,c.requestId,sender,hash,canonicalJson(validated.report),canonicalJson(result)),
   this.db.prepare("DELETE FROM combat_host_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_host_state_conflict");throw error;}
  return result;
 }
 /** Trusted server connectivity observation only. Never accept peer disconnect claims or client clocks. */
 async recordDisconnect(runId:string,input:unknown){
  const c=disconnectCommand.parse(input),row=await this.row(runId);if(!row)return fail("combat_host_state_missing");
  const context=readHostContext(JSON.parse(row.context_json)),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM combat_host_recovery_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{request_hash:string;response_json:string}>();
   if(!r)return null;if(r.request_hash!==hash)return fail("combat_host_request_conflict");return recoveryReceipt.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  if(!(context.activeAccountIds??context.participants.map(p=>p.accountId)).includes(c.disconnectedAccountId))return fail("combat_host_participant_invalid");
  const run=await this.run(runId);if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  if(run.snapshot_json!==row.start_snapshot_json)return fail("combat_host_start_mismatch");
  if(row.revision!==c.expectedRevision)return fail("combat_host_revision_conflict");
  const old=row.recovery_json?recoverySchema.parse(JSON.parse(row.recovery_json)):null;
  if(old && c.observedAt<old.startedAt)return fail("combat_host_observation_regressed");
  const recovery=recoverySchema.parse({startedAt:old?.startedAt??c.observedAt,deadlineAt:old?.deadlineAt??c.observedAt+recoveryWindowMs,
   disconnectedAccountIds:[...new Set([...(old?.disconnectedAccountIds??[]),c.disconnectedAccountId])].sort()});
  const result=recoveryReceipt.parse({version:"combat.host-recovery-receipt.v1",requestId:c.requestId,revision:row.revision+1,recovery});
  try{await this.db.batch([
   this.writeGuard(runId,row),
   this.db.prepare("UPDATE combat_host_report_states SET revision=?,recovery_json=? WHERE run_id=? AND revision=?").bind(result.revision,canonicalJson(recovery),runId,row.revision),
   this.db.prepare("INSERT INTO combat_host_recovery_receipts VALUES(?,?,?,?)").bind(runId,c.requestId,hash,canonicalJson(result)),
   this.db.prepare("DELETE FROM combat_host_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_host_state_conflict");throw error;}
  return result;
 }

 /** Trusted server-confirmed actual storage, not a raw client request or disconnect timer.
  * Does not expire/settle a run or release transaction reservations. */
 async recordSuspend(runId:string,input:unknown){
  const c=suspendCommand.parse(input),row=await this.row(runId);if(!row)return fail("combat_host_state_missing");
  const hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM combat_host_retention_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return fail("combat_host_request_conflict");return suspensionReceipt.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  const run=await this.run(runId);if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  if(run.snapshot_json!==row.start_snapshot_json)return fail("combat_host_start_mismatch");
  if(row.revision!==c.expectedRevision)return fail("combat_host_revision_conflict");
  if(!row.recovery_json)return fail("combat_host_recovery_missing");
  if(c.observedAt<recoverySchema.parse(JSON.parse(row.recovery_json)).startedAt)return fail("combat_host_observation_regressed");
  const retention=suspendHost(row.retention_json?JSON.parse(row.retention_json):null,c.observedAt);
  const result=suspensionReceipt.parse({version:"combat.host-suspend-receipt.v1",requestId:c.requestId,revision:row.revision+1,retention});
  try{await this.db.batch([this.writeGuard(runId,row),
   this.db.prepare("UPDATE combat_host_report_states SET revision=?,retention_json=? WHERE run_id=? AND revision=?").bind(result.revision,canonicalJson(retention),runId,row.revision),
   this.db.prepare("INSERT INTO combat_host_retention_receipts VALUES(?,?,?,?)").bind(runId,c.requestId,hash,canonicalJson(result)),this.db.prepare("DELETE FROM combat_host_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_host_state_conflict");throw error;}
  return result;
 }

 /** Read only; the original sender can recover a committed transition even after termination. */
 async lookupResume(sender:string,runId:string,requestId:string){
  id.parse(requestId);await this.access(sender,runId);
  const r=await this.db.prepare("SELECT response_json FROM combat_host_resume_receipts WHERE run_id=? AND request_id=? AND sender_id=?").bind(runId,requestId,sender).first<{response_json:string}>();
  return r?resumeReceipt.parse(JSON.parse(r.response_json)):null;
 }
 async resolveRecovery(sender:string,runId:string,input:unknown){
  const c=resumeCommand.parse(input),{row,context}=await this.access(sender,runId),hash=await sha256Hex(canonicalJson({sender,command:c}));
  const replay=async()=>{const r=await this.db.prepare("SELECT sender_id,request_hash,response_json FROM combat_host_resume_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{sender_id:string;request_hash:string;response_json:string}>();
   if(!r)return null;if(r.sender_id!==sender||r.request_hash!==hash)return fail("combat_host_request_conflict");return resumeReceipt.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  const run=await this.run(runId);if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  if(run.snapshot_json!==row.start_snapshot_json)return fail("combat_host_start_mismatch");
  if(row.revision!==c.expectedRevision)return fail("combat_host_revision_conflict");
  if(!row.recovery_json)return fail("combat_host_recovery_missing");
  const recovery=recoverySchema.parse(JSON.parse(row.recovery_json));
  const next=planHostResume(context,recovery.disconnectedAccountIds,sender,c.mode);
  if(!this.restoreVerifier)return fail("combat_host_restore_unavailable");
  const legacyAssertion=await prepareLegacyRestoreBoundary(this.db,runId);
  const augment=await prepareAugmentRestore(this.db,runId);
  const prepared=await this.restoreVerifier.prepare({runId,revision:row.revision,requestId:c.requestId,startSnapshotJson:row.start_snapshot_json,currentContextJson:row.context_json,nextContextJson:encodeContext(next),augmentJournal:augment.journal});
  if(!Array.isArray(prepared.assertions)||!prepared.assertions.length)return fail("combat_host_restore_guards_missing");
  const evidence=restoreEvidence.parse(prepared.evidence);
  const checkpoint=await this.db.prepare("SELECT checkpoint_text,sha256 FROM combat_checkpoints WHERE run_id=? AND checkpoint_id=?").bind(runId,evidence.checkpointId).first<{checkpoint_text:string;sha256:string}>();
  if(checkpoint&&await sha256Hex(checkpoint.checkpoint_text)!==checkpoint.sha256)return fail("combat_host_checkpoint_hash_mismatch");
  if(augment.journal.hasEntries&&evidence.augmentCursor!==augment.journal.cursor)return fail("combat_host_augment_restore_incomplete");
  if(evidence.verifiedAt<recovery.startedAt)return fail("combat_host_observation_regressed");
  const retained=row.retention_json?hostRetention.parse(JSON.parse(row.retention_json)):null;
  const nextRetention=retained?resumeStoredHost(retained,evidence.verifiedAt):null;
  if(!retained?.suspended&&c.mode==="REJOIN"&&evidence.verifiedAt>=recovery.deadlineAt)return fail("combat_host_rejoin_expired");
  if(evidence.restoredAccountIds.length!==next.activeAccountIds.length||new Set(evidence.restoredAccountIds).size!==evidence.restoredAccountIds.length||!next.activeAccountIds.every(id=>evidence.restoredAccountIds.includes(id)))return fail("combat_host_restore_incomplete");
  // Check the database clock at publication, not only the adapter's earlier verification time.
  const publicationDeadline=retained?.suspended?retained.deadlineAt:c.mode==="REJOIN"?recovery.deadlineAt:null;
  const result=resumeReceipt.parse({version:"combat.host-resume-receipt.v1",requestId:c.requestId,revision:row.revision+1,mode:c.mode,hostAccountId:next.hostAccountId,hostGeneration:next.hostGeneration,activeAccountIds:next.activeAccountIds,evidence});
  const {augmentCursor,...baseEvidence}=result.evidence;
  const storedResult=canonicalJson({...result,evidence:augmentCursor===undefined?baseEvidence:{...baseEvidence,augmentCursor}});
  try{await this.db.batch([
   this.writeGuard(runId,row),
   this.db.prepare("INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN now_ms>=? AND (? IS NULL OR now_ms<?) THEN 1 ELSE 0 END FROM (SELECT CAST(strftime('%s','now') AS INTEGER)*1000+CAST(substr(strftime('%f','now'),4,3) AS INTEGER) AS now_ms)").bind(evidence.verifiedAt,publicationDeadline,publicationDeadline),
   legacyAssertion,
   ...prepared.assertions,
   augment.assertion,
   this.db.prepare("INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_checkpoints cp WHERE run_id=? AND checkpoint_id=? AND checkpoint_text=? AND sha256=? AND start_snapshot_json=? AND json_extract(context_json,'$.hostGeneration')<=? AND EXISTS(SELECT 1 FROM combat_host_lineage l WHERE l.run_id=cp.run_id AND l.host_generation=json_extract(cp.context_json,'$.hostGeneration') AND json_extract(l.context_json,'$.hostAccountId')=json_extract(cp.context_json,'$.hostAccountId')) AND json_extract(context_json,'$.definitionVersion')=? AND json_extract(context_json,'$.snapshotRevision')=? AND json_extract(context_json,'$.lastStateSequence')<=? AND json_extract(context_json,'$.logicalTimeMs')<=?) THEN 1 ELSE 0 END").bind(runId,evidence.checkpointId,checkpoint?.checkpoint_text??null,checkpoint?.sha256??null,row.start_snapshot_json,context.hostGeneration,context.definitionVersion,context.snapshotRevision,context.lastStateSequence,context.logicalTimeMs),
   this.db.prepare("UPDATE combat_host_report_states SET revision=?,context_json=?,recovery_json=NULL,retention_json=? WHERE run_id=? AND revision=?").bind(result.revision,encodeContext(next),nextRetention?canonicalJson(nextRetention):null,runId,row.revision),
   this.db.prepare("INSERT INTO combat_host_resume_receipts VALUES(?,?,?,?,?)").bind(runId,c.requestId,sender,hash,storedResult),
   this.db.prepare("INSERT INTO combat_host_lineage VALUES(?,?,?,'RESUME',?)").bind(runId,next.hostGeneration,context.hostGeneration,encodeContext(next)),
   this.db.prepare("DELETE FROM combat_host_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_host_state_conflict");throw error;}
  return result;
 }

 /** Internal composition: use assertion + existing action checks/effects + cleanup in ONE D1 batch.
  * Preparing is not execution authorization outside that batch, nor an idempotency receipt.
  * Retain the action service's ownership, economic, grant and target checks.
  */
 async prepareControl(sender:string,runId:string,action:unknown){
  const {row,context}=await this.access(sender,runId),run=await this.run(runId);
  if(run.status!=="ACTIVE")return fail("combat_host_run_not_active");
  if(run.snapshot_json!==row.start_snapshot_json)return fail("combat_host_start_mismatch");
  if(row.recovery_json!==null||(row.retention_json&&hostRetention.parse(JSON.parse(row.retention_json)).suspended))return fail("combat_host_recovery_required");
  requireRunControl(context,sender,action);
  return {revision:row.revision,sourceOwnership:"UNCHANGED" as const,assertion:this.writeGuard(runId,row),cleanup:this.db.prepare("DELETE FROM combat_host_write_guard")};
 }

}
