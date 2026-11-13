import {z} from "zod";
import {ActivityError} from "./activity-policy";
import {readHostContext} from "./combat-host-report";
import {recoverySchema} from "./combat-host-report-store";
import {hostRetention} from "./combat-host-retention";
import {resultReviewState} from "./combat-result-review";
const id=z.string().min(1).max(200);
/** A single DB snapshot projected for display. No restore, settlement or command authorization. */
export async function readCoopStatus(db:D1Database,sender:string,runId:string,serverNow:number){
 id.parse(sender);id.parse(runId);z.number().int().nonnegative().safe().parse(serverNow);
 const row=await db.prepare(`SELECT r.status,h.revision,h.context_json,h.recovery_json,h.retention_json,v.state_json,
 EXISTS(SELECT 1 FROM active_combat_runs a WHERE a.id=r.id AND a.account_id=?) AS occupied
 FROM runs r JOIN combat_host_report_states h ON h.run_id=r.id
 LEFT JOIN combat_result_reviews v ON v.run_id=r.id WHERE r.id=?`).bind(sender,runId).first<{
 status:string;revision:number;context_json:string;recovery_json:string|null;retention_json:string|null;state_json:string|null;occupied:number}>();
 if(!row)throw new ActivityError(409,"combat_coop_state_missing");
 const context=readHostContext(JSON.parse(row.context_json));
 if(!context.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_coop_access_forbidden");
 const recovery=row.recovery_json?recoverySchema.parse(JSON.parse(row.recovery_json)):null;
 const retention=row.retention_json?hostRetention.parse(JSON.parse(row.retention_json)):null;
 const review=row.state_json?resultReviewState.parse(JSON.parse(row.state_json)):null;
 const active=(context.activeAccountIds??context.participants.map(p=>p.accountId)).includes(sender);
 // Review remains independent of run lifecycle: expiry never implies a payout or denial.
 const phase=review?(serverNow<review.deadlineAt?"RESULT_RECOVERY":"NEEDS_REVIEW"):
 row.status==="FINISH_PENDING"?"FINISH_PENDING":row.status==="QUARANTINED"?"QUARANTINED":row.status!=="ACTIVE"?"CLOSED":retention?.suspended?(serverNow<retention.deadlineAt?"SUSPENDED":"RESUME_EXPIRED"):
 recovery?(serverNow<recovery.deadlineAt?"RECOVERING":"RECOVERY_ELAPSED"):"ACTIVE";
 return {version:"combat.coop-status.v1" as const,runId,serverNow,revision:row.revision,phase,runStatus:row.status,
 participation:active?"ACTIVE":"DEPARTED",occupancyHeld:Boolean(row.occupied),
 hostGeneration:context.hostGeneration,definitionVersion:context.definitionVersion,
 recoveryDeadlineAt:recovery?.deadlineAt??null,retentionDeadlineAt:retention?.deadlineAt??null,
 resultRecoveryDeadlineAt:review?.deadlineAt??null};
}
