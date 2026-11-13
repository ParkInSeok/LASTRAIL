import type {Context,Hono} from 'hono';
import type {ActivityAuthResult} from './activity-routes';
import {ActivityError} from './activity-policy';
import {createWebSessionService} from './web-session';
import {createD1WebSessionStore} from './web-session-store';
import {isPublicNoticeRead} from './public-notice-routes';
import {isPublicProbabilityRead} from './public-probability-routes';
import {isPublicReleaseRead} from './public-release-routes';
type Ctx=Context<{Bindings:Env}>;
type WebEnv=Env&{WEB_ALLOWED_ORIGIN?:string};
export function isPublicReadPreflight(method:string,path:string){
  return method==='OPTIONS'&&(isPublicNoticeRead('GET',path)||isPublicProbabilityRead('GET',path)||isPublicReleaseRead('GET',path));
}
export function registerPublicReadPreflight(app:Hono<{Bindings:Env}>){
  app.options('/v1/public/*',(c:Ctx)=>{
    c.header('Cache-Control','no-store');c.header('Vary','Origin');
    if(!isPublicReadPreflight(c.req.method,new URL(c.req.url).pathname))return c.json({error:'not_found'},404);
    const origin=(c.env as WebEnv).WEB_ALLOWED_ORIGIN;
    if(!origin)return c.json({error:'web_origin_unavailable'},503);
    if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
    if(c.req.header('Access-Control-Request-Method')!=='GET')return c.json({error:'contract_violation'},400);
    c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');
    c.header('Access-Control-Allow-Methods','GET, OPTIONS');
    c.header('Access-Control-Allow-Headers','X-LastRail-App-Version-Code');
    c.header('Access-Control-Max-Age','600');
    return c.body(null,204);
  });
}
/** Public DEV reads still require an active tester entitlement for this exact version. */
export async function publicReadAccess(c:Ctx,environment:'DEV'|'PRD',authenticate:(c:Ctx)=>Promise<ActivityAuthResult>){
  const origin=c.req.header('Origin');
  const allowedOrigin=(c.env as WebEnv).WEB_ALLOWED_ORIGIN;
  c.header('Vary','Origin');
  if(origin&&allowedOrigin&&origin===allowedOrigin){
    c.header('Access-Control-Allow-Origin',allowedOrigin);
    c.header('Access-Control-Allow-Credentials','true');
  }
  if(environment!=='DEV')return null;
  const cookie=c.req.header('Cookie');
  if(cookie&&!c.req.header('Authorization')){
    if(!allowedOrigin)throw new ActivityError(503,'web_origin_unavailable');
    if(origin!==allowedOrigin)throw new ActivityError(403,'web_origin_forbidden');
    const service=createWebSessionService({store:createD1WebSessionStore(c.env.DB),origin:allowedOrigin});
    if(!await service.verify(cookie))return c.json({error:'web_session_required'},401);
    if(!await service.verifyDevPublicRead(cookie,c.req.header('X-LastRail-App-Version-Code')))
      throw new ActivityError(403,'tester_forbidden');
    return null;
  }
  const auth=await authenticate(c);
  if('errorCode' in auth)return c.json({error:auth.errorCode},auth.status);
  const code=c.req.header('X-LastRail-App-Version-Code')??'';
  if(!/^[1-9][0-9]*$/u.test(code)||!Number.isSafeInteger(Number(code)))throw new ActivityError(403,'tester_forbidden');
  const tester=await c.env.DB.prepare(`SELECT account_id FROM account_tester_entitlements WHERE account_id=?
    AND environment='DEV' AND version_code=? AND status='ACTIVE' AND revoked_at IS NULL AND expires_at>unixepoch('subsec')*1000`)
    .bind(auth.accountId,code).first();
  if(!tester)throw new ActivityError(403,'tester_forbidden');
  return null;
}
