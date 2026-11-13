import type {Context,Hono} from 'hono';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import type {BNamedOriginalRefitOwner} from './combat-b-named-operator-source';
import {bNamedEffectiveSourceVersion,captureBNamedEffectiveSource} from './combat-b-named-effective-source';
type Ctx=Context<{Bindings:Env}>;
/** Server-composed original owner factory only. No client/provider flag or LOCAL fallback. */
export interface BNamedEffectiveRefitProvider { forAuthenticatedContext(c:Ctx,accountId:string):Promise<BNamedOriginalRefitOwner>; }
export function registerBNamedEffectiveSourceRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<ActivityAuthResult>,provider?:BNamedEffectiveRefitProvider){
 app.post('/v1/combat/b-named-effective-source',async c=>{c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{if(new URL(c.req.url).search||c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');if(c.req.header('X-B-Named-Effective-Version')!==bNamedEffectiveSourceVersion)throw new ActivityError(409,'combat_b_named_effective_version_conflict');let input:unknown;try{input=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'combat_b_named_effective_request_invalid');}
   const owner=provider?await provider.forAuthenticatedContext(c,auth.accountId):undefined,packet=await captureBNamedEffectiveSource(c.env.DB,auth.accountId,input,owner),match=c.req.header('If-Match');if(match!==undefined&&match!==packet.packetHash)throw new ActivityError(409,'combat_b_named_effective_stale');const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_named_effective_stale');c.header('ETag',packet.packetHash);return c.json(packet);
  }catch(e){if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);throw e;}
 });
}