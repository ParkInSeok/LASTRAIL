import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {readHostContext} from "./combat-host-report";
const id=z.string().min(1).max(200),revision=z.number().int().positive().safe();
const command=z.strictObject({requestId:id,expectedHostRevision:revision});
const evidence=z.strictObject({version:z.literal("combat.review-reservations.v1"),runId:id,reconciliationId:id,verifiedAt:z.number().int().nonnegative().safe()});
const receipt=z.strictObject({version:z.literal("combat.review-release.v1"),runId:id,occupancyReleased:z.literal(true),combatInputsAllowed:z.literal(false),evidence});
const fail=(code:string):never=>{throw new ActivityError(409,`combat_review_${code}`);};
export interface ReviewReservationReconciler {
 /** Server-owned complete transaction inventory only. Resolve known originals or keep each
  * uncertain reservation protected by its resource owner. No client all-clear flags.
  * Return guards fencing ALL inspected journals/reservations until this batch commits.
  * Preparing must not release/spend resources. No production adapter is installed yet. */
 prepare(context:{runId:string;reviewJson:string;startSnapshotJson:string;participantIds:string[]}):Promise<{evidence:unknown;assertions:D1PreparedStatement[]}>;
}
/** Internal combat-occupancy release. Does not erase reservation rows or settle rewards. */
export class CombatReviewReleaseStore {
 constructor(private readonly db:D1Database,private readonly reconciler?:ReviewReservationReconciler){}
 async release(runId:string,input:unknown){
  id.parse(runId);const c=command.parse(input),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_id,request_hash,response_json FROM combat_review_releases WHERE run_id=?").bind(runId).first<{request_id:string;request_hash:string;response_json:string}>();if(!r)return null;if(r.request_id!==c.requestId)return fail("already_released");if(r.request_hash!==hash)return fail("release_request_conflict");return receipt.parse(JSON.parse(r.response_json));};
  const old=await replay();if(old)return old;
  const row=await this.db.prepare("SELECT v.state_json,h.revision,h.context_json,h.start_snapshot_json FROM combat_result_reviews v JOIN combat_host_report_states h ON h.run_id=v.run_id WHERE v.run_id=?").bind(runId).first<{state_json:string;revision:number;context_json:string;start_snapshot_json:string}>();
  if(!row)return fail("missing");if(row.revision!==c.expectedHostRevision)return fail("release_revision_conflict");
  if(!this.reconciler)return fail("reconciliation_unavailable");
  const prepared=await this.reconciler.prepare({runId,reviewJson:row.state_json,startSnapshotJson:row.start_snapshot_json,participantIds:readHostContext(JSON.parse(row.context_json)).participants.map(p=>p.accountId)});
  const proof=evidence.parse(prepared.evidence);if(proof.runId!==runId||proof.verifiedAt<JSON.parse(row.state_json).firstObservedAt||!prepared.assertions.length)return fail("reconciliation_invalid");
  const result=receipt.parse({version:"combat.review-release.v1",runId,occupancyReleased:true,combatInputsAllowed:false,evidence:proof});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_review_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_result_reviews v JOIN combat_host_report_states h ON h.run_id=v.run_id JOIN runs r ON r.id=v.run_id JOIN run_snapshots s ON s.run_id=r.id WHERE v.run_id=? AND v.state_json=? AND h.revision=? AND h.context_json=? AND h.start_snapshot_json=? AND s.snapshot_json=? AND r.status='ACTIVE') AND NOT EXISTS(SELECT 1 FROM combat_review_releases WHERE run_id=?) THEN 1 ELSE 0 END").bind(runId,row.state_json,row.revision,row.context_json,row.start_snapshot_json,row.start_snapshot_json,runId),
   ...prepared.assertions,
   this.db.prepare("INSERT INTO combat_review_releases VALUES(?,?,?,?)").bind(runId,c.requestId,hash,canonicalJson(result)),this.db.prepare("DELETE FROM combat_review_write_guard")
  ]);}catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("release_conflict");throw error;}
  return result;
 }
}
