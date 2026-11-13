import type {Context,Hono} from 'hono';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {bBoardingEventDefinition,bBoardingEventSourceVersion} from './combat-b-boarding-events';
export function registerBBoardingEventRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 app.get('/v2/combat/b-scout-gunner-definition',async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if((await readBoundedBody(c)).length||new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
   if(c.req.header('X-Combat-B-Scout-Gunner-Version')!==bBoardingEventSourceVersion)throw new ActivityError(409,'combat_b_boarding_version_conflict');
   const body={version:bBoardingEventSourceVersion,definition:await bBoardingEventDefinition(),definitionAvailable:true,runtimeSupported:false,canStart:false},packetHash=await sha256Hex(canonicalJson(z.json().parse(body)));
   const match=c.req.header('If-Match');if(match!==undefined&&match!==packetHash)throw new ActivityError(409,'combat_b_boarding_stale');
   const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_boarding_stale');
   c.header('ETag',packetHash);return c.json({...body,packetHash});
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}