import {Hono,type Context} from 'hono';
import {z} from 'zod';
import {MaintenancePlanError,type MaintenancePrincipal} from '../../../packages/common/src/maintenance-plan-policy';
import {maintenanceKeySchema,maintenanceReasonSchema} from '../../../packages/common/src/maintenance-runtime-policy';
import {deploymentPausedResponse,type DevDeploymentEnvironment} from '../../../packages/common/src/dev-deployment';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {MaintenanceRuntimePolicyStore} from './maintenance-runtime-policy-store';

export const maintenanceInitializePath='/v1/admin/maintenance-runtime/initialize';
type Bindings=Env & DevDeploymentEnvironment;
const bodySchema=z.strictObject({reason:maintenanceReasonSchema});
/** A separate router: the paused entry must never dispatch the general Admin app. */
export function createMaintenanceInitializationApp(authenticate:(c:Context<{Bindings:Env}>)=>Promise<MaintenancePrincipal|Response>){
 const app=new Hono<{Bindings:Bindings}>();
 app.post(maintenanceInitializePath,async c=>{
  c.header('Cache-Control','no-store');
  if(c.env.ENVIRONMENT!=='DEV'||c.env.DEPLOYMENT_STATE!=='PAUSED'||c.env.ADMIN_AUTH_MODE!=='ACCESS_JWT_REQUIRED')return deploymentPausedResponse();
  try{
   const actor=await authenticate(c);if(actor instanceof Response)return actor;
   if(actor.role!=='OWNER')throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
   if(new URL(c.req.url).searchParams.size)throw new MaintenancePlanError(400,'contract_violation');
   const key=maintenanceKeySchema.safeParse(c.req.header('Idempotency-Key'));
   if(!key.success)throw new MaintenancePlanError(400,'idempotency_key_required');
   const raw=await readBoundedBody(c);
   let value:unknown;try{value=JSON.parse(raw);}catch{throw new MaintenancePlanError(400,'contract_violation');}
   const body=bodySchema.safeParse(value);if(!body.success)throw new MaintenancePlanError(400,'contract_violation');
   const store=new MaintenanceRuntimePolicyStore(c.env.DB,{environment:'DEV',applicationId:'LASTRAIL'});
   return c.json(await store.transition(actor,{command:'INITIALIZE',expectedRevision:null},body.data.reason,key.data));
  }catch(error){
   if(error instanceof MaintenancePlanError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'maintenance_policy_unavailable'},503);
  }
 });
 return app;
}
