import type {Context,Hono} from 'hono';
import {ZodError} from 'zod';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {bDepartureCombatPinVersion,readBDepartureCombatPinForRun} from './combat-b-departure-combat-pin';
export function registerBDepartureCombatPinRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){app.get('/v1/runs/:runId/b-combat-source',async c=>{
 c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
 try{
  if((await readBoundedBody(c)).length||new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
  if(c.req.header('X-Combat-B-Departure-Pin-Version')!==bDepartureCombatPinVersion)throw new ActivityError(409,'combat_b_departure_version_conflict');
  const packet=await readBDepartureCombatPinForRun(c.env.DB,auth.accountId,c.req.param('runId')),expected=c.req.header('If-Match');if(expected!==undefined&&expected!==packet.packetHash)throw new ActivityError(409,'combat_b_departure_stale');
  const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_departure_stale');
  const fenced=await readBDepartureCombatPinForRun(c.env.DB,auth.accountId,c.req.param('runId'));if(fenced.packetHash!==packet.packetHash)throw new ActivityError(409,'combat_b_departure_stale');c.header('ETag',fenced.packetHash);return c.json(fenced);
 }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);if(error instanceof ZodError||error instanceof SyntaxError)return c.json({accepted:false,errorCode:'combat_b_departure_source_invalid'},409);throw error;}
});}