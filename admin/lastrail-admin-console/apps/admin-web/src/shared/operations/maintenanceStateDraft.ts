import {z} from 'zod';
import {maintenanceRuntimeSchema} from './maintenanceRuntimeAdapter';
const draftSchema=z.strictObject({game:z.enum(['NORMAL','DRAINING','MAINTENANCE']),web:z.enum(['NORMAL','READ_ONLY','MAINTENANCE']),reason:z.string().trim().min(1).max(500)});
export type MaintenanceStateDraft=z.infer<typeof draftSchema>;
/** Preparation only: no supported effective-mode or atomic two-axis write DTO exists yet. */
export const maintenanceStateOptions=Object.freeze({game:Object.freeze(['NORMAL','DRAINING','MAINTENANCE'].map(value=>Object.freeze({value,disabled:true}))),web:Object.freeze(['NORMAL','READ_ONLY','MAINTENANCE'].map(value=>Object.freeze({value,disabled:true})))});
export function prepareMaintenanceStateDraft(runtimeInput:unknown,baseRevision:number,draftInput:MaintenanceStateDraft){
 const runtime=maintenanceRuntimeSchema.parse(runtimeInput),draft=Object.freeze(draftSchema.parse(draftInput));
 z.number().int().positive().safe().parse(baseRevision);
 const policy=runtime.policy,blockers=['EFFECTIVE_MODES_UNAVAILABLE','TRANSITION_UNSUPPORTED','ATOMIC_TWO_AXIS_COMMAND_UNAVAILABLE'];
 if(baseRevision!==policy.revision)blockers.push('REVISION_CONFLICT');
 if(policy.state==='SCHEDULED')blockers.push('SCHEDULE_RESOLUTION_REQUIRED');
 const schedule=policy.state==='SCHEDULED'?Object.freeze({maintenanceId:policy.maintenanceId,draftRevision:policy.draftRevision,drainStartsAt:policy.drainStartsAt,startsAt:policy.startsAt,estimatedEndAt:policy.estimatedEndAt}):null;
 return Object.freeze({draft,baseRevision,currentRevision:policy.revision,storedPolicyState:policy.state,currentEffective:Object.freeze({game:null,web:null}),schedule,canApply:false,blockers:Object.freeze(blockers)});
}
