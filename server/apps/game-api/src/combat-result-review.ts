import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {finalReportSchema} from "./combat-final-report";
import {readHostContext} from "./combat-host-report";
const id=z.string().min(1).max(200),time=z.number().int().nonnegative().safe(),windowMs=86400000;
const command=z.strictObject({requestId:id,recordId:id,expectedRevision:z.number().int().positive().safe(),observedAt:time.max(Number.MAX_SAFE_INTEGER-windowMs)});
const reviewFields={recordId:id,firstObservedAt:time,deadlineAt:time,hostGeneration:z.number().int().positive().safe(),rewardsAuthorized:z.literal(false),occupancyReleased:z.literal(false)};
export const resultReviewState=z.discriminatedUnion("version",[
 z.strictObject({...reviewFields,version:z.literal("combat.result-review.v1"),reason:z.literal("MISSING_RECORD")}),
 z.strictObject({...reviewFields,version:z.literal("combat.result-review.v2"),reason:z.literal("CONFLICTING_RECORDS"),otherRecordId:id})
]).refine(s=>s.deadlineAt===s.firstObservedAt+windowMs && (s.version!=="combat.result-review.v2"||s.recordId!==s.otherRecordId));
const fail=(code:string):never=>{throw new ActivityError(409,`combat_review_${code}`);};
/** Trusted server review entry, not a client failure claim. Current scope: missing original body or conflicting current-generation references. ACK absence never qualifies. No polling or auto-settlement.
 * Active-run occupancy remains protected until reservation reconciliation is integrated. */
export class CombatResultReviewStore {
 constructor(private readonly db:D1Database){}
 private async host(runId:string){id.parse(runId);const r=await this.db.prepare("SELECT revision,context_json,start_snapshot_json FROM combat_host_report_states WHERE run_id=?").bind(runId).first<{revision:number;context_json:string;start_snapshot_json:string}>();if(!r)return fail("host_missing");return r;}
 private async saved(runId:string){const r=await this.db.prepare("SELECT state_json FROM combat_result_reviews WHERE run_id=?").bind(runId).first<{state_json:string}>();return r?resultReviewState.parse(JSON.parse(r.state_json)):null;}
 async read(sender:string,runId:string,serverNow:number){id.parse(sender);time.parse(serverNow);const h=await this.host(runId);if(!readHostContext(JSON.parse(h.context_json)).participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_review_access_forbidden");const s=await this.saved(runId);const released=s?await this.db.prepare("SELECT 1 FROM combat_review_releases WHERE run_id=?").bind(runId).first():null;return s?{...s,occupancyReleased:Boolean(released),phase:serverNow<s.deadlineAt?"AUTO_RECOVERY" as const:"NEEDS_REVIEW" as const,combatInputsAllowed:false,lateEvidenceAllowed:true}:null;}
 async beginMissingRecord(runId:string,input:unknown){return this.beginReview(runId,input,false);}
 async beginConflictingRecords(runId:string,input:unknown){return this.beginReview(runId,input,true);}
 private async beginReview(runId:string,input:unknown,conflicting:boolean){
  const c=conflicting?command.extend({otherRecordId:id}).parse(input):command.parse(input);
  const otherRecordId="otherRecordId" in c?c.otherRecordId:null;
  const h=await this.host(runId),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM combat_result_review_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return fail("request_conflict");return resultReviewState.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  const old=await this.saved(runId);
  if(old&&(old.recordId!==c.recordId||(old.version==="combat.result-review.v2"?old.otherRecordId:null)!==otherRecordId))return fail("origin_conflict");
  if(old&&c.observedAt<old.firstObservedAt)return fail("clock_regressed");
  const e=await this.db.prepare("SELECT report_json,response_json FROM combat_final_evidence WHERE run_id=? AND record_id=?").bind(runId,c.recordId).first<{report_json:string;response_json:string}>();if(!e)return fail("evidence_missing");
  if(!old&&(JSON.parse(e.response_json).status!=="REQUIRES_VERIFICATION"||finalReportSchema.parse(JSON.parse(e.report_json)).hostGeneration!==readHostContext(JSON.parse(h.context_json)).hostGeneration))return fail("evidence_not_current");
  if(!old&&!conflicting&&await this.db.prepare("SELECT 1 FROM combat_final_record_bodies WHERE run_id=? AND record_id=?").bind(runId,c.recordId).first())return fail("record_not_missing");
  let other: {report_json:string;response_json:string}|null=null;
  if(otherRecordId!==null){
   other=await this.db.prepare("SELECT report_json,response_json FROM combat_final_evidence WHERE run_id=? AND record_id=?").bind(runId,otherRecordId).first<{report_json:string;response_json:string}>();
   if(!other)return fail("evidence_missing");
   const firstReport=finalReportSchema.parse(JSON.parse(e.report_json)),secondReport=finalReportSchema.parse(JSON.parse(other.report_json));
   if(!old&&(JSON.parse(other.response_json).status!=="REQUIRES_VERIFICATION"||secondReport.hostGeneration!==readHostContext(JSON.parse(h.context_json)).hostGeneration))return fail("evidence_not_current");
   if(c.recordId===otherRecordId||firstReport.recordSha256===secondReport.recordSha256)return fail("records_not_conflicting");
  }
  if(h.revision!==c.expectedRevision)return fail("revision_conflict");
  const origin=await this.db.prepare("SELECT first_received_at FROM combat_final_recovery_origins WHERE run_id=?").bind(runId).first<{first_received_at:number}>();
  if(!origin)return fail("timing_missing");if(c.observedAt<origin.first_received_at)return fail("clock_regressed");
  const result=old??resultReviewState.parse({...(otherRecordId===null?{version:"combat.result-review.v1",reason:"MISSING_RECORD"}:{version:"combat.result-review.v2",reason:"CONFLICTING_RECORDS",otherRecordId}),recordId:c.recordId,firstObservedAt:origin.first_received_at,deadlineAt:origin.first_received_at+windowMs,hostGeneration:readHostContext(JSON.parse(h.context_json)).hostGeneration,rewardsAuthorized:false,occupancyReleased:false});
  const json=canonicalJson(result);
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_review_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN combat_host_report_states h ON h.run_id=r.id WHERE r.id=? AND r.status='ACTIVE' AND s.snapshot_json=? AND h.start_snapshot_json=? AND h.revision=? AND h.context_json=?) AND EXISTS(SELECT 1 FROM combat_final_evidence WHERE run_id=? AND record_id=? AND report_json=?) AND (? IS NULL OR EXISTS(SELECT 1 FROM combat_final_evidence WHERE run_id=? AND record_id=? AND report_json=?)) AND ((?=0 AND (?=1 OR NOT EXISTS(SELECT 1 FROM combat_final_record_bodies WHERE run_id=? AND record_id=?)) AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?)) OR (?=1 AND EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=? AND state_json=?))) THEN 1 ELSE 0 END").bind(runId,h.start_snapshot_json,h.start_snapshot_json,h.revision,h.context_json,runId,c.recordId,e.report_json,otherRecordId,runId,otherRecordId,other?.report_json??null,old?1:0,conflicting?1:0,runId,c.recordId,runId,old?1:0,runId,json),
   this.db.prepare("INSERT OR IGNORE INTO combat_result_reviews VALUES(?,?)").bind(runId,json),
   this.db.prepare("INSERT INTO combat_result_review_receipts VALUES(?,?,?,?)").bind(runId,c.requestId,hash,json),this.db.prepare("DELETE FROM combat_review_write_guard")
  ]);}catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("state_conflict");throw error;}
  return result;
 }
}
