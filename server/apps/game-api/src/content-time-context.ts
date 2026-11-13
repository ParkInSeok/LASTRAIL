import {objectivePeriods} from './recurring-policy';

type Boundary = {kind:'SERVICE_DAY_END'|'SERVICE_WEEK_END'|'CYCLE_END'|'PARTICIPATION_START'|'PARTICIPATION_END'|'CLAIM_END'|'PURCHASE_END';at:number};
/** Display/invalidation evidence, never an admission or entitlement decision.
 * Observation time is not a state revision. Receipts retain their original time. */
export function contentTimeContext(owner:string,sourceVersion:string|null,serverTime:number,boundaries:Boundary[]){
 return {version:'data.time-resume.v1.20261021' as const,owner,sourceVersion,serverTime,boundaries};
}
export function monthlyTimeContext(now:number,sourceVersion:string|null=null){
 const period=objectivePeriods(now);
 return contentTimeContext('MONTHLY_ATTENDANCE',sourceVersion,now,[{kind:'SERVICE_DAY_END',at:period.dayEnd},{kind:'CYCLE_END',at:period.endAt}]);
}
