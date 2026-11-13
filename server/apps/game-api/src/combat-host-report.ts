import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readCombatAuthority } from "./combat-authority";
const id=z.string().min(1).max(200);
const natural=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const participant=z.strictObject({accountId:id,role:z.enum(["TRAIN","OPERATOR"])});
const contextSchema=z.strictObject({authority:z.unknown(),runId:id,participants:z.array(participant).length(2),activeAccountIds:z.array(id).min(1).max(2).optional(),hostAccountId:id,
 hostGeneration:natural.min(1),definitionVersion:id,snapshotRevision:natural.min(1),lastStateSequence:natural,logicalTimeMs:natural})
 .refine(c=>new Set(c.participants.map(p=>p.accountId)).size===2 && new Set(c.participants.map(p=>p.role)).size===2 && c.participants.some(p=>p.accountId===c.hostAccountId))
 .refine(c=>c.activeAccountIds===undefined || (new Set(c.activeAccountIds).size===c.activeAccountIds.length && c.activeAccountIds.every(id=>c.participants.some(p=>p.accountId===id)) && c.activeAccountIds.includes(c.hostAccountId)));
export const hostReportVersion="combat.host-report.v1";
const reportSchema=z.strictObject({version:z.literal(hostReportVersion),runId:id,hostGeneration:natural.min(1),definitionVersion:id,
 baseSnapshotRevision:natural.min(1),stateSequence:natural.min(1),logicalTimeMs:natural});
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** Internal contiguous-report envelope validator, not a public route or combat/result verifier.
 * Context comes from server-owned pinned state; sender comes from authentication, never report fields.
 * No persistence or replay here. The transport owner must resolve original receipts before validation,
 * then atomically CAS this context with accepted state/receipt. Gaps require a separate recovery path.
 */
export function readHostContext(serverContext:unknown){
 const c=contextSchema.parse(serverContext);
 const authority=readCombatAuthority(c.authority);
 if(authority.status!=="PINNED"||authority.contract.mode!=="ORDINARY_COOP")return fail("combat_host_mode_unsupported");
 return {...c,authority:authority.contract};
}
export function validateHostReport(serverContext:unknown,authenticatedAccountId:string,input:unknown){
 const c=readHostContext(serverContext),sender=id.parse(authenticatedAccountId);
 if(sender!==c.hostAccountId||!c.participants.some(p=>p.accountId===sender))throw new ActivityError(403,"combat_host_sender_forbidden");
 const report=reportSchema.parse(input);
 if(report.runId!==c.runId)return fail("combat_host_run_mismatch");
 if(report.hostGeneration!==c.hostGeneration)return fail("combat_host_generation_mismatch");
 if(report.definitionVersion!==c.definitionVersion)return fail("combat_host_definition_mismatch");
 if(report.baseSnapshotRevision!==c.snapshotRevision)return fail("combat_host_snapshot_mismatch");
 if(c.lastStateSequence===Number.MAX_SAFE_INTEGER||report.stateSequence!==c.lastStateSequence+1)return fail("combat_host_sequence_invalid");
 if(report.logicalTimeMs<c.logicalTimeMs)return fail("combat_host_time_regressed");
 return {status:"ENVELOPE_VALID",combatVerification:"NOT_PERFORMED",report} as const;
}
