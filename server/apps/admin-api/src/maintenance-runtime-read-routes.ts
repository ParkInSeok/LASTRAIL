import type {Context,Hono} from 'hono';
import {MaintenancePlanError,maintenanceEnvironmentSchema,type MaintenancePrincipal} from '../../../packages/common/src/maintenance-plan-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {MaintenanceRuntimePolicyStore} from './maintenance-runtime-policy-store';

type Ctx=Context<{Bindings:Env}>;
/** Persisted applied policy only; do not claim unsupported login or web enforcement. */
export function registerMaintenanceRuntimeReadRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<MaintenancePrincipal|Response>){
 app.get('/v1/admin/maintenance-runtime',async c=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   const environment=maintenanceEnvironmentSchema.safeParse(c.env.ENVIRONMENT);
   if(!environment.success)throw new MaintenancePlanError(503,'maintenance_environment_unavailable');
   if(new URL(c.req.url).searchParams.size||(await readBoundedBody(c))!=='')throw new MaintenancePlanError(400,'contract_violation');
   const store=new MaintenanceRuntimePolicyStore(c.env.DB,{environment:environment.data,applicationId:'LASTRAIL'});
   const policy=await store.read(actor);
   return c.json({contractVersion:policy.state==='IMMEDIATE'?'admin-maintenance-runtime-v2':'admin-maintenance-runtime-v1',environment:environment.data,applicationId:'LASTRAIL',serverTime:Date.now(),
    policy,capabilities:{canTransition:false,gameLoginControl:false,webModeControl:false}});
  }catch(error){
   if(error instanceof MaintenancePlanError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'maintenance_policy_unavailable'},503);
  }
 });
}
