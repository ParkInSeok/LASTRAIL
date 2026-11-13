import type {Context,Hono} from 'hono';
import {SupportGrantService,SupportGrantError,supportGrantContractVersion} from '../../../packages/common/src/support-grant-service';
import type {SupportDatabase} from '../../../packages/common/src/support-ticket-service';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function registerSupportGrantRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 const handle=(write:boolean)=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   const environment=c.env.ENVIRONMENT;
   if(environment!=='DEV'&&environment!=='PRD')throw new SupportGrantError(503,'support_grant_unavailable');
   const service=new SupportGrantService(c.env.DB as unknown as SupportDatabase,environment);
   const target=c.req.param('principalId');
   if(!target||!/^[A-Za-z0-9_-]{1,128}$/u.test(target)||new URL(c.req.url).searchParams.size)throw new SupportGrantError(400,'contract_violation');
   const raw=await readBoundedBody(c);
   if(!write){if(raw!=='')throw new SupportGrantError(400,'contract_violation');return c.json({contractVersion:supportGrantContractVersion,environment,state:await service.read(actor.id,target)});}
   const key=c.req.header('Idempotency-Key')??'';
   if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new SupportGrantError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(raw);}catch{throw new SupportGrantError(400,'contract_violation');}
   return c.json(await service.change(actor.id,target,key,body));
  }catch(error){
   if(error instanceof SupportGrantError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'support_grant_unavailable'},503);
  }
 };
 app.get('/v1/admin/support/processor-grants/:principalId',handle(false));
 app.post('/v1/admin/support/processor-grants/:principalId',handle(true));
}
