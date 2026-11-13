import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { readHostContext } from "./combat-host-report";
import { classifyFinalReport, finalReportSchema } from "./combat-final-report";
const id=z.string().min(1).max(200);
const command=z.strictObject({requestId:id,report:finalReportSchema});
const participationEvidence=z.strictObject({originActiveAccountIds:z.array(id).min(1).max(2),observedActiveAccountIds:z.array(id).min(1).max(2),originHostGeneration:z.number().int().positive().safe(),observedHostGeneration:z.number().int().positive().safe()});
const response=z.strictObject({version:z.literal("combat.final-evidence-receipt.v1"),status:z.enum(["REQUIRES_VERIFICATION","HISTORICAL_EVIDENCE_ONLY"]),sourceVerification:z.literal("NOT_PERFORMED"),combatVerification:z.literal("NOT_PERFORMED"),rewardsAuthorized:z.literal(false),report:finalReportSchema,participationEvidence:participationEvidence.optional()});
const storedRecordResponse=z.strictObject({version:z.literal("combat.final-record-storage.v1"),recordId:id,recordSha256:z.string().regex(/^[a-f0-9]{64}$/u),combatVerification:z.literal("NOT_PERFORMED"),sourceVerification:z.literal("NOT_PERFORMED"),rewardsAuthorized:z.literal(false)});
const recordText=z.string().min(1).refine(value=>new TextDecoder("utf-8",{ignoreBOM:true,fatal:true}).decode(new TextEncoder().encode(value))===value,"invalid_utf8_text");
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** Immutable unverified references and original text; no combat validation, finalization or reward mutation. */
export class CombatFinalReportStore {
 constructor(private readonly db:D1Database,private readonly serverNow:()=>number=Date.now){}
 private async access(sender:string,runId:string){
  id.parse(sender);id.parse(runId);
  const row=await this.db.prepare("SELECT revision,context_json,start_snapshot_json FROM combat_host_report_states WHERE run_id=?").bind(runId).first<{revision:number;context_json:string;start_snapshot_json:string}>();
  if(!row)return fail("combat_host_state_missing");const context=readHostContext(JSON.parse(row.context_json));
  if(!context.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_final_sender_forbidden");
  return {row,context};
 }
 private original(sender:string,runId:string,requestId:string){
  return this.db.prepare("SELECT s.request_hash,e.response_json FROM combat_final_submissions s JOIN combat_final_evidence e ON e.run_id=s.run_id AND e.record_id=s.record_id WHERE s.run_id=? AND s.sender_id=? AND s.request_id=?").bind(runId,sender,requestId).first<{request_hash:string;response_json:string}>();
 }
 async lookup(sender:string,runId:string,requestId:string){
  id.parse(requestId);await this.access(sender,runId);const r=await this.original(sender,runId,requestId);return r?response.parse(JSON.parse(r.response_json)):null;
 }
 private body(runId:string,recordId:string){
  return this.db.prepare("SELECT record_text,response_json FROM combat_final_record_bodies WHERE run_id=? AND record_id=?").bind(runId,recordId).first<{record_text:string;response_json:string}>();
 }
 /** Original participants may recover the exact stored evidence, including after termination. */
 async readRecord(sender:string,runId:string,recordId:string){
  id.parse(recordId);await this.access(sender,runId);const body=await this.body(runId,recordId);
  return body?{...storedRecordResponse.parse(JSON.parse(body.response_json)),recordText:recordText.parse(JSON.parse(body.record_text))}:null;
 }
 /** Internal evidence ingestion only. SHA equality is not origin authentication or combat proof.
  * Text is exact UTF-8, never JSON-normalized. Public payload schema/transport limits remain unissued.
  */
 async attachRecord(sender:string,runId:string,recordId:string,input:unknown){
  id.parse(recordId);const text=recordText.parse(input),{row}=await this.access(sender,runId);
  const evidence=await this.db.prepare("SELECT report_json FROM combat_final_evidence WHERE run_id=? AND record_id=?").bind(runId,recordId).first<{report_json:string}>();
  if(!evidence)return fail("combat_final_evidence_missing");
  const report=finalReportSchema.parse(JSON.parse(evidence.report_json));
  if(await sha256Hex(text)!==report.recordSha256)return fail("combat_final_record_hash_mismatch");
  const replay=async()=>{const stored=await this.body(runId,recordId);if(!stored)return null;if(recordText.parse(JSON.parse(stored.record_text))!==text)return fail("combat_final_record_conflict");return storedRecordResponse.parse(JSON.parse(stored.response_json));};
  const original=await replay();if(original)return original;
  const result=storedRecordResponse.parse({version:"combat.final-record-storage.v1",recordId,recordSha256:report.recordSha256,combatVerification:"NOT_PERFORMED",sourceVerification:"NOT_PERFORMED",rewardsAuthorized:false});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_final_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_host_report_states h JOIN run_snapshots s ON s.run_id=h.run_id WHERE h.run_id=? AND h.revision=? AND h.context_json=? AND s.snapshot_json=? AND h.start_snapshot_json=?) THEN 1 ELSE 0 END").bind(runId,row.revision,row.context_json,row.start_snapshot_json,row.start_snapshot_json),
   this.db.prepare("INSERT OR IGNORE INTO combat_final_record_bodies VALUES(?,?,?,?)").bind(runId,recordId,JSON.stringify(text),canonicalJson(result)),
   this.db.prepare("DELETE FROM combat_final_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_final_state_conflict");throw error;}
  return (await replay())!;
 }
 async submit(sender:string,runId:string,input:unknown){
  const c=command.parse(input),{row,context}=await this.access(sender,runId),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.original(sender,runId,c.requestId);if(!r)return null;if(r.request_hash!==hash)return fail("combat_final_request_conflict");return response.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  const classified=classifyFinalReport(context,sender,c.report),reportJson=canonicalJson(classified.report);
  const existing=await this.db.prepare("SELECT report_json,response_json FROM combat_final_evidence WHERE run_id=? AND record_id=?").bind(runId,c.report.finalRecordId).first<{report_json:string;response_json:string}>();
  if(existing&&existing.report_json!==reportJson)return fail("combat_final_evidence_conflict");
  // Prior immutable evidence remains replayable. New references require an observed assignment.
  let basis:z.infer<typeof participationEvidence>|undefined;
  if(!existing){
   const lineage=await this.db.prepare("SELECT context_json FROM combat_host_lineage WHERE run_id=? AND host_generation=?").bind(runId,c.report.hostGeneration).first<{context_json:string}>();
   if(!lineage)return fail("combat_final_lineage_unavailable");
   const origin=readHostContext(JSON.parse(lineage.context_json));
   if(origin.runId!==runId||origin.hostGeneration!==c.report.hostGeneration||origin.definitionVersion!==c.report.definitionVersion||origin.hostAccountId!==c.report.originHostAccountId)return fail("combat_final_origin_mismatch");
   if(origin.participants.some(p=>!context.participants.some(current=>current.accountId===p.accountId&&current.role===p.role)))return fail("combat_final_participants_mismatch");
   basis=participationEvidence.parse({originActiveAccountIds:origin.activeAccountIds??origin.participants.map(p=>p.accountId),observedActiveAccountIds:context.activeAccountIds??context.participants.map(p=>p.accountId),originHostGeneration:origin.hostGeneration,observedHostGeneration:context.hostGeneration});
  }
  const result=existing?response.parse(JSON.parse(existing.response_json)):response.parse({version:"combat.final-evidence-receipt.v1",...classified,participationEvidence:basis});
  const {participationEvidence:storedBasis,...baseResult}=result;
  const resultJson=canonicalJson(storedBasis===undefined?baseResult:{...baseResult,participationEvidence:storedBasis});
  const receivedAt=!existing&&classified.status==="REQUIRES_VERIFICATION"?z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-86400000).parse(this.serverNow()):null;
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_final_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_host_report_states h JOIN run_snapshots s ON s.run_id=h.run_id WHERE h.run_id=? AND h.revision=? AND h.context_json=? AND s.snapshot_json=? AND h.start_snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM combat_final_evidence WHERE run_id=? AND record_id=? AND report_json<>?) THEN 1 ELSE 0 END").bind(runId,row.revision,row.context_json,row.start_snapshot_json,row.start_snapshot_json,runId,c.report.finalRecordId,reportJson),
   this.db.prepare("INSERT OR IGNORE INTO combat_final_evidence VALUES(?,?,?,?)").bind(runId,c.report.finalRecordId,reportJson,resultJson),
   ...(receivedAt===null?[]:[this.db.prepare("INSERT OR IGNORE INTO combat_final_recovery_origins VALUES(?,?)").bind(runId,receivedAt)]),
   this.db.prepare("INSERT INTO combat_final_submissions VALUES(?,?,?,?,?)").bind(runId,sender,c.requestId,hash,c.report.finalRecordId),
   this.db.prepare("DELETE FROM combat_final_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_final_state_conflict");throw error;}
  // A concurrent identical reference may have been inserted first; return its immutable receipt.
  return (await replay())!;
 }
}
