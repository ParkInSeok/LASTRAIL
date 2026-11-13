import {z} from "zod";
import {eventDefinitionDraftSchema,eventDefinitionPreviewSchema,EVENT_DEFINITION_PREVIEW_VERSION,eventDefinitionKey,type EventDefinitionDraft} from "../eventPreview/eventPreviewContract";
export const EVENT_DRAFT_VERSION="admin.event-draft-management.v1.20261020" as const;
const positive=z.number().int().positive().safe(),count=z.number().int().nonnegative().safe(),id=eventDefinitionDraftSchema.shape.roundId;
export const eventDraftSaveSchema=z.object({operation:z.literal("SAVE_DRAFT"),reason:z.string().trim().min(1).max(1000),expectedRevision:positive.nullable(),definition:eventDefinitionDraftSchema}).strict();
export type EventDraftSave=z.infer<typeof eventDraftSaveSchema>;
export const eventDraftSavedSchema=eventDefinitionPreviewSchema.innerType().omit({contractVersion:true,persisted:true,writesOnPreview:true}).extend({contractVersion:z.literal(EVENT_DRAFT_VERSION),validationVersion:z.literal(EVENT_DEFINITION_PREVIEW_VERSION),persisted:z.literal(true),state:z.literal("DRAFT"),savedAt:count,savedBy:z.string().min(1),reason:z.string().trim().min(1).max(1000)}).refine(value=>value.environment===value.definition.environment&&value.roundId===value.definition.roundId&&value.revision===value.definition.revision,"event_draft_identity_mismatch");
export type EventDraftSaved=z.infer<typeof eventDraftSavedSchema>;
export const eventDraftSummarySchema=z.object({roundId:id,title:z.string(),theme:z.string(),revision:positive,definitionHash:z.string().regex(/^[a-f0-9]{64}$/u),schedule:eventDefinitionDraftSchema.shape.schedule,state:z.literal("DRAFT"),definitionValid:z.boolean(),issueCount:count,blockerCount:count,savedAt:count,savedBy:z.string().min(1)});
export const eventDraftListSchema=z.object({contractVersion:z.literal(EVENT_DRAFT_VERSION),environment:z.enum(["DEV","PRD"]),state:z.literal("DRAFT"),items:z.array(eventDraftSummarySchema).max(50),nextAfter:id.nullable()});
export type EventDraftList=z.infer<typeof eventDraftListSchema>;
export const eventDraftRequestKey=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
export interface EventDraftPort{list(after?:string,limit?:number):Promise<unknown>;read(roundId:string):Promise<unknown>;save(roundId:string,command:EventDraftSave,key:string):Promise<unknown>;}
export function assertEventDraftReceipt(raw:unknown,command:EventDraftSave,principalId:string):EventDraftSaved{
 const saved=eventDraftSavedSchema.parse(raw);if(eventDefinitionKey(saved.definition)!==eventDefinitionKey(command.definition)||saved.reason!==command.reason||saved.savedBy!==principalId||saved.revision!==(command.expectedRevision??0)+1)throw new Error("EVENT_DRAFT_RECEIPT_MISMATCH");return saved;
}
export type {EventDefinitionDraft};
