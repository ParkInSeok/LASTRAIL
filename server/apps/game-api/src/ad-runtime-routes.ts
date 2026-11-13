import type {Context,Hono} from 'hono';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {readAdRuntimePolicy} from './ad-runtime-policy';
import {approveAdStart} from './ad-start-approval';
import {ActivityError,activityId} from './activity-policy';

export function registerAdRuntimeRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>) {
  app.post('/v1/ad-opportunities/:consumer/:intentId/start',async c=>{
    c.header('Cache-Control','no-store');
    const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try {
      const consumer=c.req.param('consumer'),intent=c.req.param('intentId'),key=c.req.header('Idempotency-Key')??'';
      if((consumer!=='OFFLINE'&&consumer!=='COMBAT')||!activityId.safeParse(intent).success||new URL(c.req.url).search||(await readBoundedBody(c))!=='')throw new ActivityError(400,'contract_violation');
      if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
      return c.json(await approveAdStart(c.env.DB,auth.accountId,c.env.ENVIRONMENT,consumer,intent,key));
    }catch(e){if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);
      return c.json({accepted:false,errorCode:'ad_start_unavailable'},503);}
  });
  app.get('/v1/runtime-policy',async c=>{
    c.header('Cache-Control','no-store');
    const auth=await authenticate(c);
    if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try {
      if(new URL(c.req.url).search||(await readBoundedBody(c))!=='')return c.json({accepted:false,errorCode:'contract_violation'},400);
      return c.json(await readAdRuntimePolicy(c.env.DB,c.env.ENVIRONMENT));
    } catch {return c.json({accepted:false,errorCode:'ad_runtime_policy_unavailable'},503);}
  });
}
