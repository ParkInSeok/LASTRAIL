import type {Context,Hono} from 'hono';
import {CommunityModerationGrantService,CommunityGrantError,communityModerationGrantVersion} from '../../../packages/common/src/community-moderation-grant-service';
import type {SupportDatabase} from '../../../packages/common/src/support-ticket-service';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function registerCommunityModerationGrantRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 const handle=(write:boolean)=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   const environment=c.env.ENVIRONMENT;
   if(environment!=='DEV'&&environment!=='PRD')throw new CommunityGrantError(503,'community_grant_unavailable');
   const service=new CommunityModerationGrantService(c.env.DB as unknown as SupportDatabase,environment);
   if(c.req.header('X-Community-Moderation-Grant-Version')!==communityModerationGrantVersion)throw new CommunityGrantError(409,'community_grant_version_conflict');
   if(write&&!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))throw new CommunityGrantError(400,'contract_violation');
   const target=c.req.param('principalId');
   if(!target||!/^[A-Za-z0-9_-]{1,128}$/u.test(target)||new URL(c.req.url).searchParams.size)throw new CommunityGrantError(400,'contract_violation');
   const raw=await readBoundedBody(c);
   if(!write){if(raw!=='')throw new CommunityGrantError(400,'contract_violation');return c.json({contractVersion:communityModerationGrantVersion,environment,state:await service.read(actor.id,target)});}
   const key=c.req.header('Idempotency-Key')??'';
   if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new CommunityGrantError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(raw);}catch{throw new CommunityGrantError(400,'contract_violation');}
   return c.json(await service.change(actor.id,target,key,body));
  }catch(error){
   if(error instanceof CommunityGrantError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'community_grant_unavailable'},503);
  }
 };
 app.get('/v1/admin/community/moderator-grants/:principalId',handle(false));
 app.post('/v1/admin/community/moderator-grants/:principalId',handle(true));
}
