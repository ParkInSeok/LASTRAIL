import type {Context,Hono} from 'hono';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {captureBNamedOperatorSource,bNamedOperatorSourceVersion} from './combat-b-named-operator-source';
export function registerBNamedOperatorSourceRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 app.post('/v1/combat/b-named-operator-source',async c=>{c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{if(new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');if(c.req.header('X-B-Named-Operator-Version')!==bNamedOperatorSourceVersion)throw new ActivityError(409,'combat_b_named_version_conflict');let input:unknown;try{input=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'combat_b_named_request_invalid');}
   const packet=await captureBNamedOperatorSource(c.env.DB,auth.accountId,input),match=c.req.header('If-Match');if(match!==undefined&&match!==packet.packetHash)throw new ActivityError(409,'combat_b_named_stale');const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_named_stale');c.header('ETag',packet.packetHash);return c.json(packet);
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}