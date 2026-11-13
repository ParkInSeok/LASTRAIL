import { z } from "zod";
export const OPS_INVESTIGATION_VERSION = "ops-investigation.v1.20261019";
export const opsEventId = z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
export const opsRequestKey = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const count = z.number().int().nonnegative().safe();
const note = z.string().trim().min(1).max(500).refine(value => [...value].every(c => c.charCodeAt(0) >= 32 && c.charCodeAt(0) !== 127));
export const opsFixBuild = z.strictObject({
  appVersion: z.string().regex(/^[A-Za-z0-9.+_-]{1,64}$/u).nullable(),
  appVersionCode: z.string().regex(/^\d{1,19}$/u).nullable(),
  serverVersion: z.string().regex(/^[A-Za-z0-9._-]{1,100}$/u).nullable()
}).refine(value => Object.values(value).some(item => item !== null));
export const opsInvestigationCommandSchema = z.strictObject({
  status: z.enum(["acknowledged", "verifying", "resolved"]),
  expectedVersion: count.max(Number.MAX_SAFE_INTEGER - 1), reason: note,
  fixBuild: opsFixBuild.nullable(), evidence: note.nullable(), verificationResult: z.literal("PASS").nullable()
}).superRefine((value, ctx) => {
  if (value.status === "resolved" ? !value.fixBuild || !value.evidence || value.verificationResult !== "PASS" : value.fixBuild !== null || value.evidence !== null || value.verificationResult !== null) {
    ctx.addIssue({code: z.ZodIssueCode.custom, message: "Resolution evidence must match the transition"});
  }
});
export type OpsInvestigationCommand = z.infer<typeof opsInvestigationCommandSchema>;
export const opsInvestigationStateSchema = z.object({
  errorLogId: opsEventId, status: z.enum(["open","ignored","acknowledged","verifying","resolved"]), version: count,
  reason: note.nullable(), fixBuild: opsFixBuild.nullable(), evidence: note.nullable(), verificationResult: z.literal("PASS").nullable(),
  updatedBy: z.string().nullable(), updatedAt: count.nullable(), resolvedAt: count.nullable()
});
export type OpsInvestigationState = z.infer<typeof opsInvestigationStateSchema>;
const recurrence = z.discriminatedUnion("available", [
  z.object({available:z.literal(false), reason:z.enum(["NOT_RESOLVED_WITH_BUILD","ORIGINAL_EVENT_RETAINED_OUT"])}),
  z.object({available:z.literal(true), targetBuildRecurred:z.boolean(), targetBuildEventCount:count, otherBuildEventCount:count,
    unknownBuildEventCount:count, latestTargetEventId:opsEventId.nullable(), totalEventCount:count,
    items:z.array(z.object({id:opsEventId, occurredAt:count, appVersion:z.string().nullable(),appVersionCode:z.string().nullable(),serverVersion:z.string().nullable(),targetBuild:z.union([z.literal(0),z.literal(1),z.null()])})).max(50), hasMore:z.boolean()})
]);
export const opsInvestigationReadSchema = z.object({contractVersion:z.literal(OPS_INVESTIGATION_VERSION),investigation:opsInvestigationStateSchema,recurrence});
export type OpsInvestigationRead = z.infer<typeof opsInvestigationReadSchema>;
export const opsInvestigationReceiptSchema = z.object({accepted:z.literal(true),contractVersion:z.literal(OPS_INVESTIGATION_VERSION),permission:z.literal("OPS_ERROR_LOG_WRITE"),investigation:opsInvestigationStateSchema,auditId:z.string().min(1)});
export type OpsInvestigationReceipt = z.infer<typeof opsInvestigationReceiptSchema>;
const metrics = z.object({eventCount:count, occurrenceCount:count.nullable(), occurrenceUnknownEventCount:count, affectedAccountCount:count, unattributedEventCount:count,firstOccurredAt:z.string().datetime().nullable(),lastOccurredAt:z.string().datetime().nullable()});
const metadata = z.object({contractVersion:z.literal(OPS_INVESTIGATION_VERSION),collectionAvailable:z.literal(true),scope:z.literal("FILTERED_ALL"),statusScope:z.literal("ORIGINAL_EVENT"),filters:z.record(z.string()),period:z.object({occurredFrom:z.string().datetime().nullable(),occurredTo:z.string().datetime().nullable()}),serverCalculatedAt:z.string().datetime()});
export const opsGroupsSchema = metadata.extend({items:z.array(metrics.extend({environment:z.string(),service:z.string(),errorCode:z.string(),phase:z.string().nullable(),serverVersion:z.string().nullable(),appVersion:z.string().nullable(),appVersionCode:z.string().nullable(),contractVersion:z.string().nullable()})).max(50),totalGroupCount:count,pageItemCount:count,nextCursor:z.string().nullable()});
export type OpsGroups = z.infer<typeof opsGroupsSchema>;
export function assertOpsReceipt(raw:unknown,id:string,command:OpsInvestigationCommand):OpsInvestigationReceipt {
  const receipt=opsInvestigationReceiptSchema.parse(raw),state=receipt.investigation;
  if(state.errorLogId!==id || state.version!==command.expectedVersion+1 || state.status!==command.status || state.reason!==command.reason || JSON.stringify(state.fixBuild)!==JSON.stringify(command.fixBuild) || state.evidence!==command.evidence || state.verificationResult!==command.verificationResult)throw new Error("OPS_INVESTIGATION_RECEIPT_MISMATCH");
  return receipt;
}
