import type {Context,Hono} from 'hono';
import {ActivityError} from '../../game-api/src/activity-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {AdminRewardTracking} from './admin-reward-tracking';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
/** Existing SUPPORT_READ only; POST is a bounded read-only preview, not approval.
 * No execution/stop/delta route is installed without the original command owner. */
export function registerAdminRewardTrackingRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 const handle=(preview:boolean)=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const principal=await authenticate(c);if(principal instanceof Response)return principal;
  try{
   if(new URL(c.req.url).search||c.req.header('If-Match')!==undefined)throw new ActivityError(400,'contract_violation');
   const service=new AdminRewardTracking(c.env.DB,c.env.ENVIRONMENT),text=await readBoundedBody(c);
   if(!preview){
    if(text)throw new ActivityError(400,'contract_violation');
    return c.json(await service.capabilities(principal.id));
   }
   if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   let input:unknown;try{input=JSON.parse(text);}catch{throw new ActivityError(400,'contract_violation');}
   return c.json(await service.preview(principal.id,input));
  }catch(error){
   if(error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'reward_tracking_unavailable'},503);
  }
 };
 app.get('/v1/admin/reward-tracking/capabilities',handle(false));
 app.post('/v1/admin/reward-tracking/review-preview',handle(true));
}
