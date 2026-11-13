import type {Context,Hono} from 'hono';
import {MaintenancePlanError,maintenanceCommandSchema,maintenanceEnvironmentSchema,maintenanceIdSchema,
  maintenanceApplication,maintenancePermissions,previewMaintenance,type MaintenancePrincipal} from '../../../packages/common/src/maintenance-plan-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {MaintenancePlanStore} from './maintenance-plan-store';

type Ctx=Context<{Bindings:Env}>;
export function registerMaintenancePlanRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<MaintenancePrincipal|Response>){
  const handler=(mode:'list'|'detail'|'preview'|'command')=>async(c:Ctx)=>{
    c.header('Cache-Control','no-store');
    const actor=await authenticate(c);if(actor instanceof Response)return actor;
    try{
      const environment=maintenanceEnvironmentSchema.safeParse(c.env.ENVIRONMENT);
      if(!environment.success)throw new MaintenancePlanError(503,'maintenance_environment_unavailable');
      const params=new URL(c.req.url).searchParams;
      const id=c.req.param('maintenanceId');
      if(mode!=='list'&&!maintenanceIdSchema.safeParse(id).success)throw new MaintenancePlanError(400,'contract_violation');
      const raw=await readBoundedBody(c),store=new MaintenancePlanStore(c.env.DB,environment.data);
      if(mode==='command'){
        if(actor.role!=='OWNER')throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
        if(params.size)throw new MaintenancePlanError(400,'contract_violation');
        const key=c.req.header('Idempotency-Key')??'';
        if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new MaintenancePlanError(400,'idempotency_key_required');
        let body:unknown;try{body=JSON.parse(raw);}catch{throw new MaintenancePlanError(400,'contract_violation');}
        const parsed=maintenanceCommandSchema.safeParse(body);if(!parsed.success)throw new MaintenancePlanError(400,'contract_violation');
        return c.json(await store.command(actor,id!,parsed.data,key));
      }
      if(raw!=='')throw new MaintenancePlanError(400,'contract_violation');
      const allowed=mode==='list'?['after','limit']:mode==='preview'?['at']:[];
      for(const key of params.keys())if(!allowed.includes(key)||params.getAll(key).length!==1)throw new MaintenancePlanError(400,'contract_violation');
      const serverTime=Date.now(),base={environment:environment.data,serverTime,application:maintenanceApplication(),permissions:maintenancePermissions(actor)};
      if(mode==='list'){
        const after=params.get('after')??'',rawLimit=params.get('limit')??'20';
        if((after!==''&&!maintenanceIdSchema.safeParse(after).success)||!/^[1-9][0-9]*$/u.test(rawLimit)||Number(rawLimit)>50)
          throw new MaintenancePlanError(400,'contract_violation');
        const limit=Number(rawLimit),rows=await store.list(after,limit+1),items=rows.slice(0,limit);
        return c.json({...base,contractVersion:'admin-maintenance-plan-list-v1',items,nextAfter:rows.length>limit?items.at(-1)!.maintenanceId:null});
      }
      const at=params.get('at');
      if(at!==null&&(!/^(0|[1-9][0-9]*)$/u.test(at)||!Number.isSafeInteger(Number(at))))throw new MaintenancePlanError(400,'contract_violation');
      const plan=await store.read(id!);if(!plan)throw new MaintenancePlanError(404,'maintenance_plan_not_found');
      if(mode==='preview')return c.json({...base,contractVersion:'admin-maintenance-preview-v1',preview:previewMaintenance(plan,at===null?serverTime:Number(at))});
      return c.json({...base,contractVersion:'admin-maintenance-plan-v1',plan});
    }catch(error){
      if(error instanceof MaintenancePlanError||error instanceof ActivityError)return c.json({error:error.code},error.status);
      return c.json({error:'maintenance_plan_unavailable'},503);
    }
  };
  app.get('/v1/admin/maintenance-plans',handler('list'));
  app.get('/v1/admin/maintenance-plans/:maintenanceId',handler('detail'));
  app.get('/v1/admin/maintenance-plans/:maintenanceId/preview',handler('preview'));
  app.post('/v1/admin/maintenance-plans/:maintenanceId/commands',handler('command'));
}
