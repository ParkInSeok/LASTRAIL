import {z} from 'zod';
import type {Context,Hono} from 'hono';
import type {RuntimePolicyPrincipal} from './runtime-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {AdminEventManagement,AdminEventManagementError,eventConfigReadPermission,eventConfigWritePermission} from './admin-event-management';
type Ctx=Context<{Bindings:Env}>;
export function registerAdminEventManagementRoutes(app:Hono<{Bindings:Env}>,
 authenticate:(c:Ctx)=>Promise<RuntimePolicyPrincipal|Response>,hasPermission:(actor:RuntimePolicyPrincipal,permission:string)=>boolean){
 const handler=(mode:'list'|'read'|'save')=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');const actor=await authenticate(c);if(actor instanceof Response)return actor;
  if(!hasPermission(actor,mode==='save'?eventConfigWritePermission:eventConfigReadPermission))return c.json({error:'admin_permission_denied'},403);
  try{
   const service=new AdminEventManagement(c.env.DB,c.env.ENVIRONMENT);await service.authorize(actor,mode==='save');
   const params=new URL(c.req.url).searchParams,allowed=mode==='list'?['after','limit']:[];
   for(const key of params.keys())if(!allowed.includes(key)||params.getAll(key).length!==1)throw new AdminEventManagementError(400,'contract_violation');
   const raw=await readBoundedBody(c),roundId=c.req.param('roundId')??'';
   if(mode==='save'){
    if(c.req.header('Content-Type')?.split(';')[0]?.trim()!=='application/json'||c.req.header('If-Match')!==undefined)throw new AdminEventManagementError(400,'contract_violation');
    let input:unknown;try{input=JSON.parse(raw);}catch{throw new AdminEventManagementError(400,'contract_violation');}
    return c.json(await service.save(actor,roundId,input,c.req.header('Idempotency-Key')??''));
   }
   if(raw!=='')throw new AdminEventManagementError(400,'contract_violation');
   if(mode==='read')return c.json(await service.read(roundId));
   const limit=params.get('limit')??'20';if(!/^[1-9][0-9]*$/u.test(limit))throw new AdminEventManagementError(400,'contract_violation');
   return c.json(await service.list(params.get('after')??'',Number(limit)));
  }catch(error){
   if(error instanceof AdminEventManagementError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   if(error instanceof z.ZodError)return c.json({error:'contract_violation'},400);
   return c.json({error:'event_draft_management_unavailable'},503);
  }
 };
 app.get('/v1/admin/events/definitions',handler('list'));
 app.get('/v1/admin/events/definitions/:roundId',handler('read'));
 app.post('/v1/admin/events/definitions/:roundId/commands',handler('save'));
}
