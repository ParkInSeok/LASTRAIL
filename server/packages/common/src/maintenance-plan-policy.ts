import {z} from 'zod';

export class MaintenancePlanError extends Error {
  constructor(readonly status:400|403|404|409|503,readonly code:string){super(code);}
}
export type MaintenancePrincipal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export const maintenanceIdSchema=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/u);
export const maintenanceEnvironmentSchema=z.enum(['DEV','PRD']);
const timestamp=z.number().int().nonnegative().safe();
const revision=z.number().int().positive().safe();
const seconds=z.number().int().positive().max(Math.floor(Number.MAX_SAFE_INTEGER/1000));
const text=(max:number)=>z.string().trim().min(1).max(max).refine(value=>!/[<>]/u.test(value)
  &&Array.from(value).every(c=>(c.charCodeAt(0)>=32&&c.charCodeAt(0)!==127)||'\t\n\r'.includes(c)),'Plain text required');

export const maintenanceDocumentSchema=z.strictObject({
  applicationId:maintenanceIdSchema,serviceScope:z.enum(['GAME','WEB','GAME_AND_WEB']),
  webMode:z.enum(['READ_ONLY','MAINTENANCE']).nullable(),
  title:text(200),summary:text(500),noticeId:z.null(),
  drainStartsAt:timestamp.nullable(),startsAt:timestamp,estimatedEndAt:timestamp.nullable(),
  existingRunPolicy:z.enum(['DRAIN_THEN_PRESERVE','EMERGENCY_PRESERVE']).nullable(),
  statusFreshnessSeconds:seconds,retryAfterSeconds:seconds,
}).superRefine((plan,ctx)=>{
  const invalid=(message:string)=>ctx.addIssue({code:'custom',message});
  if(plan.estimatedEndAt!==null&&plan.estimatedEndAt<=plan.startsAt)invalid('Estimated end must follow start');
  if(plan.serviceScope==='GAME'&&plan.webMode!==null)invalid('Game-only plan cannot affect web');
  if(plan.serviceScope!=='GAME'&&plan.webMode===null)invalid('Web scope requires an explicit mode');
  if(plan.serviceScope==='WEB'){
    if(plan.drainStartsAt!==null||plan.existingRunPolicy!==null)invalid('Web-only plan cannot change runs');
  }else if(plan.existingRunPolicy==='DRAIN_THEN_PRESERVE'){
    if(plan.drainStartsAt===null||plan.drainStartsAt>plan.startsAt)invalid('Planned game maintenance requires drain at or before start');
  }else if(plan.existingRunPolicy==='EMERGENCY_PRESERVE'){
    if(plan.drainStartsAt!==null)invalid('Emergency proposal has no drain');
  }else invalid('Game scope requires a preservation policy');
});
export type MaintenanceDocument=z.infer<typeof maintenanceDocumentSchema>;
const commandFields={environment:maintenanceEnvironmentSchema,expectedDraftRevision:revision.nullable(),reason:text(500)};
export const maintenanceCommandSchema=z.discriminatedUnion('command',[
  z.strictObject({...commandFields,command:z.literal('SAVE_DRAFT'),plan:maintenanceDocumentSchema}),
  z.strictObject({...commandFields,command:z.enum(['SCHEDULE','ACTIVATE','EXTEND','COMPLETE','CANCEL'])}),
]);
export type MaintenanceCommand=z.infer<typeof maintenanceCommandSchema>;
export type MaintenanceDraft={maintenanceId:string;draftRevision:number;planState:'DRAFT';document:MaintenanceDocument;updatedAt:number};

export function maintenanceApplication(){
  // No operational adapter is installed. Browser acknowledgements cannot override this.
  return {applied:false as const,status:'UNCONNECTED' as const,currentPolicyRevision:null,
    blockReasons:['request_gates_unconnected','run_preservation_unverified','callback_recovery_unverified','public_status_projection_unconnected']};
}
export function maintenancePermissions(principal:MaintenancePrincipal){
  return {canSaveDraft:principal.role==='OWNER',canApply:false};
}
export function previewMaintenance(draft:MaintenanceDraft,at:number){
  const plan=draft.document;
  const gameTarget=plan.serviceScope!=='WEB',webTarget=plan.serviceScope!=='GAME';
  const effectiveGameMode=!gameTarget?'NORMAL':at>=plan.startsAt?'MAINTENANCE'
    :plan.drainStartsAt!==null&&at>=plan.drainStartsAt?'DRAINING':'NORMAL';
  const effectiveWebMode=webTarget&&at>=plan.startsAt?plan.webMode!:'NORMAL';
  // This is a hypothetical schedule; neither its NORMAL nor MAINTENANCE grants or denies access.
  return {kind:'HYPOTHETICAL_SCHEDULE' as const,applied:false as const,maintenanceId:draft.maintenanceId,
    draftRevision:draft.draftRevision,at,effectiveGameMode,effectiveWebMode,
    estimatedEndAt:plan.estimatedEndAt,automaticCompletion:false,
    existingRunPolicy:plan.existingRunPolicy,title:plan.title,summary:plan.summary};
}
