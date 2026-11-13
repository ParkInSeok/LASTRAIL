import {Hono} from 'hono';
import {z} from 'zod';
import {resolveAuthContext} from './index';
import {ActivityError} from './activity-policy';
import {readBoundedBody} from './activity-routes';
import {railCommandSchema,railVersion} from './world-boss-rail-contract';
import {WorldBossRailLocalStore,LocalRailError} from './world-boss-rail-local-store';
export const localRailHttpVersion='world-boss.rail-local-http.v1.20261002';
export type LocalRailEnv=Env&{LOCAL_WORLD_RAIL_PROFILE:'LOCAL_TEST_PROFILE'};
const localUrl=(url:string)=>{try{const u=new URL(url);return u.protocol==='http:'&&['localhost','127.0.0.1','[::1]'].includes(u.hostname);}catch{return false;}};
/** Deliberately not registered on the game worker. The factory serves LOCAL loopback trials only. */
export function createWorldBossRailLocalHttp(){
 const app=new Hono<{Bindings:LocalRailEnv}>();
 app.use('*',async(c,next)=>{
  c.header('Cache-Control','no-store');c.header('X-World-Rail-Transport',localRailHttpVersion);
  if(c.env.ENVIRONMENT!=='LOCAL'||c.env.LOCAL_WORLD_RAIL_PROFILE!=='LOCAL_TEST_PROFILE'||!localUrl(c.req.url))return c.json({accepted:false,errorCode:'rail_local_transport_disabled'},404);
  const origin=c.req.header('Origin');if(origin&&!localUrl(origin))return c.json({accepted:false,errorCode:'rail_local_origin_forbidden'},403);
  return next();
 });
 const handler=(write:boolean)=>async(c:Parameters<typeof resolveAuthContext>[0])=>{
  try{
   const auth=await resolveAuthContext(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
   const attemptId=c.req.param('attemptId')??'';
   if(!attemptId||attemptId.length>200||new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
   if(c.req.header('X-World-Rail-Version')!==railVersion)throw new ActivityError(409,'rail_version_conflict');
   const contentVersion=c.req.header('X-World-Content-Version');if(!contentVersion||contentVersion.length>200)throw new ActivityError(400,'rail_content_version_required');
   const store=new WorldBossRailLocalStore(c.env.DB);
   if(!write){if((await readBoundedBody(c)).length)throw new ActivityError(400,'contract_violation');return c.json({transportVersion:localRailHttpVersion,...await store.read(auth.accountId,attemptId,contentVersion)});}
   if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   const key=c.req.header('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
   let input:unknown;try{input=JSON.parse(await readBoundedBody(c));}catch(error){if(error instanceof ActivityError)throw error;throw new ActivityError(400,'contract_violation');}
   const command=railCommandSchema.parse(input);
   if(command.attemptId!==attemptId||command.contentVersion!==contentVersion)throw new ActivityError(409,'rail_definition_conflict');
   if(command.commandId!==key)throw new ActivityError(400,'rail_command_key_mismatch');
   const receipt=await store.command(auth.accountId,command,Date.now());
   return c.json({accepted:true,transportVersion:localRailHttpVersion,version:railVersion,status:'LOCAL_TEST_PROFILE',receipt});
  }catch(error){
   if(error instanceof ActivityError||error instanceof LocalRailError)return c.json({accepted:false,errorCode:error.code},error.status);
   if(error instanceof z.ZodError)return c.json({accepted:false,errorCode:'contract_violation'},400);
   const message=error instanceof Error?error.message:'';
   if(message==='rail_role_forbidden')return c.json({accepted:false,errorCode:message},403);
   if(['rail_definition_conflict','rail_command_key_conflict','rail_revision_conflict','rail_attempt_ended','rail_round_not_found','rail_round_not_open','rail_window_closed','rail_route_already_confirmed','rail_actor_unavailable','rail_internal_already_accepted'].includes(message))return c.json({accepted:false,errorCode:message},409);
   return c.json({accepted:false,errorCode:'rail_local_temporarily_unavailable'},503);
  }
 };
 app.get('/_local/world-boss/attempts/:attemptId/rail',handler(false));
 app.post('/_local/world-boss/attempts/:attemptId/rail/commands',handler(true));
 return app;
}