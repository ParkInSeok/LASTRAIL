import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readHostContext } from "./combat-host-report";
const id=z.string().min(1).max(200);
export const finalReportVersion="combat.final-report.v1";
export const finalReportSchema=z.strictObject({version:z.literal(finalReportVersion),runId:id,
 hostGeneration:z.number().int().min(1).max(Number.MAX_SAFE_INTEGER),definitionVersion:id,
 originHostAccountId:id,finalRecordId:id,recordSha256:z.string().regex(/^[a-f0-9]{64}$/u)});
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** A record reference, not a verified final record. Fetch and authenticate complete evidence before
 * checking lineage/combat/outcome and issuing any rights. Hash equality is never proof of truth.
 * Original participants may relay records; relay identity does not determine beneficiaries.
 * Historical epochs are evidence for review only, never current-state writes or a second payout.
 */
export function classifyFinalReport(serverContext:unknown,authenticatedAccountId:string,input:unknown){
 const c=readHostContext(serverContext),sender=id.parse(authenticatedAccountId);
 if(!c.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_final_sender_forbidden");
 const report=finalReportSchema.parse(input);
 if(report.runId!==c.runId)return fail("combat_final_run_mismatch");
 if(report.definitionVersion!==c.definitionVersion)return fail("combat_final_definition_mismatch");
 if(report.hostGeneration>c.hostGeneration)return fail("combat_final_generation_invalid");
 if(!c.participants.some(p=>p.accountId===report.originHostAccountId))return fail("combat_final_origin_invalid");
 if(report.hostGeneration===c.hostGeneration && report.originHostAccountId!==c.hostAccountId)return fail("combat_final_origin_invalid");
 return {status:report.hostGeneration<c.hostGeneration?"HISTORICAL_EVIDENCE_ONLY":"REQUIRES_VERIFICATION",
  sourceVerification:"NOT_PERFORMED",combatVerification:"NOT_PERFORMED",rewardsAuthorized:false,report} as const;
}
