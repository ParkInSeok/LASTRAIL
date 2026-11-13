import type {Context,Hono} from 'hono';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {bOperationBindingVersion,bOperationContentVersion,bOperationProfileVersion,bOperationRuntime} from './combat-b-operation-binding';
import {BOperationProgressStore} from './combat-b-operation-progress';
export function registerBOperationRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 for(const route of ['/v1/operations/b-story12-contract','/v1/runs/:runId/b-operation-progress'])app.get(route,async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{if(new URL(c.req.url).search||(await readBoundedBody(c)).length)throw new ActivityError(400,'contract_violation');if(c.req.header('X-B-Operation-Version')!==bOperationBindingVersion)throw new ActivityError(409,'combat_b_operation_version_conflict');
   const body=route.includes(':runId')?await new BOperationProgressStore(c.env.DB).read(auth.accountId,c.req.param('runId')!):{version:bOperationBindingVersion,contentVersion:bOperationContentVersion,profileVersion:bOperationProfileVersion,definitionAvailable:true,...bOperationRuntime};
   const packetHash=await sha256Hex(canonicalJson(z.json().parse(body)));if(c.req.header('If-Match')!==undefined&&c.req.header('If-Match')!==packetHash)throw new ActivityError(409,'combat_b_operation_stale');
   const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_operation_stale');c.header('ETag',packetHash);return c.json({...body,packetHash});
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}