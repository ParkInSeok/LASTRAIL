import {z} from 'zod';
export const bCompletedProgressVersion='operation.b-completed-progress.v1.20261004';
const progressSchema=z.strictObject({completedWaves:z.number().int().min(0).max(1536),completedNodes:z.number().int().min(0).max(12),fullCompletion:z.boolean()});
/** Receives only original, verified journal aggregates; never caller completion counts. */
export function qualifyBCompletedProgress(outcome:string,original:unknown){
 const progress=progressSchema.parse(original);
 if(outcome==='VICTORY'&&!progress.fullCompletion)throw new Error('B victory objectives incomplete');
 if(!['VICTORY','FAILURE','ABANDONED','INVALID','CANCELLED'].includes(outcome))throw new Error('B outcome invalid');
 const victory=outcome==='VICTORY',completedEligible=victory||outcome==='FAILURE';
 return {policyVersion:bCompletedProgressVersion,status:victory?'ELIGIBLE':completedEligible?'COMPLETED_PROGRESS_ELIGIBLE':'NOT_ELIGIBLE',ends:victory?1:0,completedWaves:completedEligible?progress.completedWaves:0,completedNodes:completedEligible?progress.completedNodes:0};
}