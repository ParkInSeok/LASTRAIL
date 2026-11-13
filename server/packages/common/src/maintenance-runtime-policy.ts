import {z} from 'zod';
import {maintenanceIdSchema} from './maintenance-plan-policy';
export const maintenanceContextSchema=z.strictObject({environment:z.enum(['DEV','PRD']),applicationId:maintenanceIdSchema});
export type MaintenanceContext=z.infer<typeof maintenanceContextSchema>;
const revision=z.number().int().positive().safe();
export const maintenanceTransitionSchema=z.discriminatedUnion('command',[
 z.strictObject({command:z.literal('INITIALIZE'),expectedRevision:z.null()}),
 z.strictObject({command:z.enum(['SCHEDULE','ACTIVATE']),expectedRevision:revision,maintenanceId:maintenanceIdSchema,draftRevision:revision}),
 z.strictObject({command:z.enum(['COMPLETE','CANCEL']),expectedRevision:revision}),
]);
export type MaintenanceTransition=z.infer<typeof maintenanceTransitionSchema>;
export type MaintenanceState='NORMAL'|'SCHEDULED'|'ACTIVE'|'COMPLETED'|'CANCELLED'|'IMMEDIATE';
export const maintenanceReceiptSchema=z.strictObject({accepted:z.literal(true),environment:z.enum(['DEV','PRD']),applicationId:maintenanceIdSchema,policyRevision:revision});
export type MaintenanceReceipt=z.infer<typeof maintenanceReceiptSchema>;
export const maintenanceReasonSchema=z.string().trim().min(1).max(500);
export const maintenanceKeySchema=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
export const maintenanceImmediateCommandSchema=z.strictObject({expectedRevision:revision,
 gameMode:z.enum(['NORMAL','DRAINING','MAINTENANCE']),webMode:z.enum(['NORMAL','READ_ONLY','MAINTENANCE']),
 reason:maintenanceReasonSchema,estimatedEndAt:z.number().int().nonnegative().safe().nullable().default(null)});
export const maintenanceImmediateReceiptSchema=z.strictObject({accepted:z.literal(true),applied:z.literal(true),
 contractVersion:z.literal('admin-maintenance-immediate-command-v1'),environment:z.enum(['DEV','PRD']),applicationId:maintenanceIdSchema,
 policyRevision:revision,gameMode:z.enum(['NORMAL','DRAINING','MAINTENANCE']),webMode:z.literal('NORMAL'),
 appliedAt:z.number().int().nonnegative().safe(),auditId:z.string().min(1),requestId:z.string().min(1),automaticCompletion:z.literal(false)});
