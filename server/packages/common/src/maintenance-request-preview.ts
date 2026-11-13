import {z} from 'zod';
import {maintenanceDocumentSchema,maintenanceIdSchema,previewMaintenance} from './maintenance-plan-policy';

const timestamp=z.number().int().nonnegative().safe();
const draftSchema=z.strictObject({maintenanceId:maintenanceIdSchema,draftRevision:z.number().int().positive().safe(),
  planState:z.literal('DRAFT'),document:maintenanceDocumentSchema,updatedAt:timestamp});
const requestSchema=z.discriminatedUnion('operation',[
  z.strictObject({operation:z.enum(['NEW_RUN','NEW_AD','NEW_PURCHASE','GAME_WRITE','RESULT_READ','TRANSACTION_RECOVERY'])}),
  z.strictObject({operation:z.literal('EXISTING_RUN_WRITE'),admittedAt:timestamp}),
]);

/**
 * Pure, hypothetical GAME request preview; never a runtime admission capability.
 * The caller classifies the operation. In future runtime integration, admission
 * time must come from the stored run, and replay/recovery must be verified by its
 * existing owner, not inferred from a URL, request key, or client-provided label.
 * Runtime use requires an applied-policy owner, atomic write checks and proven
 * preservation/recovery. This draft helper intentionally supplies none of those.
 */
export function previewMaintenanceRequest(draftInput:unknown,atInput:unknown,requestInput:unknown){
  const draft=draftSchema.parse(draftInput),at=timestamp.parse(atInput),request=requestSchema.parse(requestInput);
  if(request.operation==='EXISTING_RUN_WRITE'&&request.admittedAt>at){
    throw new RangeError('Run admission cannot be later than the evaluation time');
  }
  const schedule=previewMaintenance(draft,at);
  const recovery=request.operation==='RESULT_READ'||request.operation==='TRANSACTION_RECOVERY';
  const newOperation=request.operation==='NEW_RUN'||request.operation==='NEW_AD'||request.operation==='NEW_PURCHASE';
  const lateRun=request.operation==='EXISTING_RUN_WRITE'&&draft.document.drainStartsAt!==null
    &&request.admittedAt>=draft.document.drainStartsAt;
  const blocked=!recovery&&(schedule.effectiveGameMode==='MAINTENANCE'
    ||(schedule.effectiveGameMode==='DRAINING'&&(newOperation||lateRun)));
  return {kind:'HYPOTHETICAL_REQUEST' as const,applied:false as const,maintenanceId:draft.maintenanceId,
    draftRevision:draft.draftRevision,at,operation:request.operation,effectiveGameMode:schedule.effectiveGameMode,
    decision:blocked?'BLOCKED_BY_MAINTENANCE' as const:'NOT_BLOCKED_BY_MAINTENANCE' as const,
    authorization:'EXISTING_CHECKS_REQUIRED' as const,automaticCompletion:false as const};
}
