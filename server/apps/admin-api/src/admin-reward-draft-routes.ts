import type {Context,Hono} from 'hono';
import {ActivityError} from '../../game-api/src/activity-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {AdminRewardDrafts,RewardDraftNotFound} from './admin-reward-drafts';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function registerAdminRewardDraftRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 app.get('/v1/admin/reward-tracking/drafts/:batchId',async c=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   if(new URL(c.req.url).search||await readBoundedBody(c))throw new ActivityError(400,'contract_violation');
   return c.json(await new AdminRewardDrafts(c.env.DB,c.env.ENVIRONMENT).read(actor.id,c.req.param('batchId')));
  }catch(error){
   if(error instanceof ActivityError||error instanceof RewardDraftNotFound)return c.json({error:error.code},error.status);
   return c.json({error:'reward_draft_unavailable'},503);
  }
 });
 app.post('/v1/admin/reward-tracking/drafts/:batchId/commands',async c=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   if(new URL(c.req.url).search||c.req.header('If-Match')!==undefined||
    c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   const text=await readBoundedBody(c);let input:unknown;
   try{input=JSON.parse(text);}catch{throw new ActivityError(400,'contract_violation');}
   return c.json(await new AdminRewardDrafts(c.env.DB,c.env.ENVIRONMENT).save(actor,c.req.param('batchId'),input,c.req.header('Idempotency-Key')??''));
  }catch(error){
   if(error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'reward_draft_unavailable'},503);
  }
 });
}
