import {adminApi} from '../api/client';
import type {LiveOpsReadPort} from './LiveOpsWorkspace';
/** Existing typed adapters keep their original environment/schema/identity validation and server permissions. */
export function liveOpsReadPort(environment:'DEV'|'PRD'):LiveOpsReadPort{return {
 list:(kind,after)=>kind==='NOTICE'?adminApi.notices(environment,after):adminApi.maintenancePlans(environment,after),
 detail:(kind,id)=>kind==='NOTICE'?adminApi.notice(environment,id):adminApi.maintenancePlan(environment,id)
};}
