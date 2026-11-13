import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {hostRetention} from "./combat-host-retention";
import {readHostContext} from "./combat-host-report";
const id=z.string().min(1).max(200),revision=z.number().int().positive().safe();
const command=z.strictObject({requestId:id,checkpointId:id,expectedRevision:revision.max(Number.MAX_SAFE_INTEGER-1),checkpointText:z.string().min(1),sha256:z.string().regex(/^[0-9a-f]{64}$/u)});
const receipt=z.strictObject({version:z.literal("combat.checkpoint-storage.v1"),checkpointId:id,revision,status:z.literal("STORED_UNVERIFIED"),combatVerification:z.literal("NOT_PERFORMED")});
const fail=(code:string):never=>{throw new ActivityError(409,`combat_checkpoint_${code}`);};
/** Internal exact-text storage, not a complete-state validator, restore selector or public upload.
 * No checkpoint becomes trusted for combat/transactions merely because its bytes were saved. */
export class CombatCheckpointStore {
 constructor(private readonly db:D1Database){}
 private async access(sender:string,runId:string){
  id.parse(sender);id.parse(runId);
  const row=await this.db.prepare("SELECT revision,context_json,start_snapshot_json,recovery_json,retention_json FROM combat_host_report_states WHERE run_id=?").bind(runId).first<{revision:number;context_json:string;start_snapshot_json:string;recovery_json:string|null;retention_json:string|null}>();
  if(!row)return fail("state_missing");const context=readHostContext(JSON.parse(row.context_json));
  if(!context.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_checkpoint_access_forbidden");
  return {row,context};
 }
 async read(sender:string,runId:string,checkpointId:string){
  id.parse(checkpointId);await this.access(sender,runId);
  const r=await this.db.prepare("SELECT checkpoint_text,sha256,context_json FROM combat_checkpoints WHERE run_id=? AND checkpoint_id=?").bind(runId,checkpointId).first<{checkpoint_text:string;sha256:string;context_json:string}>();
  if(!r)return null;
  if(await sha256Hex(r.checkpoint_text)!==r.sha256)return fail("stored_hash_mismatch");
  return {checkpointId,checkpointText:r.checkpoint_text,sha256:r.sha256,context:readHostContext(JSON.parse(r.context_json)),status:"STORED_UNVERIFIED" as const,combatVerification:"NOT_PERFORMED" as const};
 }
 async save(sender:string,runId:string,input:unknown){
  const c=command.parse(input),{row,context}=await this.access(sender,runId),hash=await sha256Hex(canonicalJson({sender,command:c}));
  const replay=async()=>{const r=await this.db.prepare("SELECT sender_id,request_hash,response_json FROM combat_checkpoint_receipts WHERE run_id=? AND request_id=?").bind(runId,c.requestId).first<{sender_id:string;request_hash:string;response_json:string}>();if(!r)return null;if(r.sender_id!==sender||r.request_hash!==hash)return fail("request_conflict");return receipt.parse(JSON.parse(r.response_json));};
  const old=await replay();if(old)return old;
  if(context.hostAccountId!==sender)throw new ActivityError(403,"combat_checkpoint_sender_forbidden");
  if(row.revision!==c.expectedRevision)return fail("revision_conflict");
  if(row.recovery_json!==null||row.retention_json&&hostRetention.parse(JSON.parse(row.retention_json)).suspended)return fail("not_running");
  if(await sha256Hex(c.checkpointText)!==c.sha256)return fail("hash_mismatch");
  if(await this.db.prepare("SELECT 1 FROM combat_checkpoints WHERE run_id=? AND checkpoint_id=?").bind(runId,c.checkpointId).first())return fail("id_conflict");
  const result=receipt.parse({version:"combat.checkpoint-storage.v1",checkpointId:c.checkpointId,revision:row.revision+1,status:"STORED_UNVERIFIED",combatVerification:"NOT_PERFORMED"});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN combat_host_report_states h ON h.run_id=r.id WHERE r.id=? AND r.status='ACTIVE' AND s.snapshot_json=? AND h.start_snapshot_json=? AND h.revision=? AND h.context_json=? AND h.recovery_json IS NULL AND h.retention_json IS ?) AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) AND NOT EXISTS(SELECT 1 FROM combat_checkpoints WHERE run_id=? AND checkpoint_id=?) THEN 1 ELSE 0 END").bind(runId,row.start_snapshot_json,row.start_snapshot_json,row.revision,row.context_json,row.retention_json,runId,runId,c.checkpointId),
   this.db.prepare("INSERT INTO combat_checkpoints VALUES(?,?,?,?,?,?)").bind(runId,c.checkpointId,c.checkpointText,c.sha256,row.context_json,row.start_snapshot_json),
   this.db.prepare("UPDATE combat_host_report_states SET revision=? WHERE run_id=? AND revision=?").bind(result.revision,runId,row.revision),
   this.db.prepare("INSERT INTO combat_checkpoint_receipts VALUES(?,?,?,?,?)").bind(runId,c.requestId,sender,hash,canonicalJson(result)),
   this.db.prepare("DELETE FROM combat_host_write_guard")
  ]);}catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("state_conflict");throw error;}
  return result;
 }
}
