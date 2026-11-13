import {z} from "zod";
import {parseKst,type EventScheduleDraft,type EventRewardRow} from "../eventScheduleDraft";
export const EVENT_DEFINITION_PREVIEW_VERSION="admin.event-definition-preview.v1.20261020" as const;
const id=z.string().trim().min(1).max(128),positive=z.number().int().positive().safe(),time=z.number().int().nonnegative().max(8640000000000000),absolute=z.number().int().nonnegative().safe();
export const eventDefinitionDraftSchema=z.object({environment:z.enum(["DEV","PRD"]),roundId:id,revision:positive,policyVersion:id.nullable(),title:z.string().trim().min(1).max(200),theme:z.string().trim().min(1).max(200),schedule:z.object({publishAt:time,startAt:time,endAt:time,claimUntil:time.nullable(),giftOpenAt:time.nullable()}).strict(),rewards:z.array(z.object({rightId:id,kind:z.enum(["PARTICIPATION","STORY","CUMULATIVE","GIFT"]),condition:z.string().trim().min(1).max(1000),producerId:id.nullable(),threshold:positive.nullable(),itemId:id.nullable(),quantity:positive,perAccountCount:positive,policyVersion:id.nullable()}).strict()).min(1).max(100)}).strict();
export type EventDefinitionDraft=z.infer<typeof eventDefinitionDraftSchema>;
export const eventDefinitionPreviewSchema=z.object({
 contractVersion:z.literal(EVENT_DEFINITION_PREVIEW_VERSION),environment:z.enum(["DEV","PRD"]),roundId:id,revision:positive,definition:eventDefinitionDraftSchema,definitionHash:z.string().regex(/^[a-f0-9]{64}$/u),definitionValid:z.boolean(),issues:z.array(z.string()),blockers:z.array(z.string()).min(1),
 persisted:z.literal(false),reviewApproved:z.literal(false),scheduleSupported:z.literal(false),writesOnPreview:z.literal(false),overlapStatus:z.literal("NOT_OBSERVED_WARNING_ONLY"),runtimeState:z.literal("NOT_OBSERVED"),displayTimezone:z.literal("Asia/Seoul"),timestampUnit:z.literal("MILLISECONDS"),
 timePolicy:z.object({policyVersion:z.literal("event.close-claim-settlement.v1.20261020"),participationEndsAt:absolute,completionGraceEndsAt:absolute,manualClaimEndsAt:absolute,participationEndExclusive:z.literal(true),completionGraceEndExclusive:z.literal(true),manualClaimEndExclusive:z.literal(true),existingRunExpiryPreserved:z.literal(true),authorityEventTimeRequired:z.literal(true),lateOriginalReplayPreserved:z.literal(true),originalStartEventBindingRequired:z.literal(true),preEventStartNotAutomaticallyGraceEligible:z.literal(true),manualAndAutomaticShareOriginalRight:z.literal(true),automaticSettlementScope:z.literal("ACHIEVED_UNCLAIMED_RIGHTS_ONLY"),failedOrUnknownRightsPreserved:z.literal(true),settlementExecuted:z.literal(false)}),
 proposedTotals:z.array(z.object({itemId:id,quantity:z.string().regex(/^[1-9]\d*$/u)})),totalScope:z.literal("PER_ACCOUNT_PROPOSED_MAXIMUM_NOT_VALIDATED_ENTITLEMENT")
}).refine(value=>value.environment===value.definition.environment&&value.roundId===value.definition.roundId&&value.revision===value.definition.revision,"event_preview_identity_mismatch");
export type EventDefinitionPreview=z.infer<typeof eventDefinitionPreviewSchema>;
export const eventDefinitionKey=(value:EventDefinitionDraft)=>JSON.stringify(eventDefinitionDraftSchema.parse(value));
const kind:Record<EventRewardRow["kind"],EventDefinitionDraft["rewards"][number]["kind"]>={participation:"PARTICIPATION",story:"STORY",cumulative:"CUMULATIVE",anniversary:"GIFT"};
const optionalId=(value:string)=>value.trim()||null;
function requiredTime(value:string){const parsed=parseKst(value);if(parsed===null)throw new Error("EVENT_REQUIRED_SCHEDULE_INVALID");return parsed;}
function optionalTime(value:string){return value===""?null:requiredTime(value);}
/** Pure adapter only. Positive proposedRevision is explicit; a local review is never Server approval.
 * Local readiness booleans/policy notes are not proof of installed producer or execution support.
 */
export function eventPreviewInputFromDraft(snapshot:ReturnType<EventScheduleDraft["snapshot"]>,proposedRevision:number):EventDefinitionDraft {
 const d=snapshot.definition;
 return eventDefinitionDraftSchema.parse({environment:d.environment,roundId:d.roundId,revision:proposedRevision,policyVersion:optionalId(d.policyVersion),title:d.title,theme:d.theme,schedule:{publishAt:requiredTime(d.schedule.publicAt),startAt:requiredTime(d.schedule.start),endAt:requiredTime(d.schedule.end),claimUntil:optionalTime(d.schedule.claimEnd),giftOpenAt:optionalTime(d.schedule.giftOpen)},rewards:d.rows.map(row=>({rightId:row.id,kind:kind[row.kind],condition:row.condition,producerId:optionalId(row.sourceId),threshold:row.threshold??null,itemId:optionalId(row.itemId),quantity:row.quantity,perAccountCount:row.perAccount,policyVersion:optionalId(row.policySource)}))});
}
