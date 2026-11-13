import {destinationResponseVersion,projectDestinationResponse} from './operation-map-destination-response';
import type {Context,Hono} from 'hono';
import {z} from 'zod';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {ActivityError} from './activity-policy';
import {OperationMapStore} from './operation-map-store';
import type {FirstObjectiveEvidenceOwner} from './combat-first-objective-verifier';
import {OrdinaryObjectiveResultStore} from './ordinary-objective-result-store';
import {OrdinaryNodeAttemptStore,nodeRestartCommand} from './ordinary-node-attempt';
const command=z.strictObject({expectedRevision:z.number().int().positive().safe(),edgeId:z.string().min(1).max(200)});
const completionCommand=command.omit({edgeId:true}).extend({nodeId:z.string().min(1).max(200),evidenceRef:z.string().min(1).max(200),attemptGeneration:z.number().int().positive().safe().optional()});
/** Public commands carry references only; objective and hold proofs stay server-owned. */
export function registerOperationMapCommandRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>,
 createEvidenceOwner?:(db:D1Database)=>FirstObjectiveEvidenceOwner){
 app.post('/v1/runs/:runId/operation-map/restart',async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);
  if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if(new URL(c.req.url).search||c.req.param('runId').length>200||c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   const key=c.req.header('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(await readBoundedBody(c,4096));}catch{throw new ActivityError(400,'contract_violation');}
   const parsed=nodeRestartCommand.omit({requestId:true}).safeParse(body);if(!parsed.success)throw new ActivityError(400,'contract_violation');
   return c.json({accepted:true,...await new OrdinaryNodeAttemptStore(c.env.DB).restart(auth.accountId,c.req.param('runId'),{...parsed.data,requestId:key})});
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);return c.json({accepted:false,errorCode:'ordinary_node_restart_outcome_unknown'},503);}
 });
 app.post('/v1/runs/:runId/operation-map/completion',async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);
  if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if(new URL(c.req.url).search||c.req.param('runId').length>200
    ||c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   const key=c.req.header('Idempotency-Key');
   if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(await readBoundedBody(c,4096));}catch{throw new ActivityError(400,'contract_violation');}
   const parsed=completionCommand.safeParse(body);if(!parsed.success)throw new ActivityError(400,'contract_violation');
   // Resolve authority only for a new commit; original receipt replay must survive owner outages.
   const store=new OperationMapStore(c.env.DB,{prepare:context=>new OrdinaryObjectiveResultStore(c.env.DB,createEvidenceOwner?.(c.env.DB)).prepare(context)});
   const receipt=await store.commit(auth.accountId,c.req.param('runId'),{...parsed.data,eventId:key,kind:'COMPLETE'});
   return c.json({accepted:true,receipt});
  }catch(error){
   if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);
   return c.json({accepted:false,errorCode:'ordinary_completion_outcome_unknown'},503);
  }
 });
 app.post('/v1/runs/:runId/operation-map/destination',async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);
  if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if(new URL(c.req.url).search||c.req.param('runId').length>200)throw new ActivityError(400,'contract_violation');
   const requestedVersion=c.req.header('X-Map-Destination-Version');if(requestedVersion!==undefined&&requestedVersion!==destinationResponseVersion)throw new ActivityError(409,'operation_map_destination_version_conflict');
   const key=c.req.header('Idempotency-Key');
   if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'contract_violation');}
   const parsed=command.safeParse(body);if(!parsed.success)throw new ActivityError(400,'contract_violation');
   const result=await new OperationMapStore(c.env.DB).commit(auth.accountId,c.req.param('runId'),{...parsed.data,eventId:key,kind:'CONFIRM'},requestedVersion);
   return c.json(projectDestinationResponse(result,requestedVersion));
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}
