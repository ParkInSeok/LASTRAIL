import type {Context,Hono} from 'hono';
import {ZodError} from 'zod';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {bWaveManifestVersion,readBWaveManifestForRun} from './combat-b-wave-manifest';
export function registerBWaveManifestRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){app.get('/v1/runs/:runId/b-wave-manifest',async c=>{
 c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
 try{
  if((await readBoundedBody(c)).length||new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
  if(c.req.header('X-Combat-B-Wave-Manifest-Version')!==bWaveManifestVersion)throw new ActivityError(409,'combat_b_wave_version_conflict');
  const packet=await readBWaveManifestForRun(c.env.DB,auth.accountId,c.req.param('runId')),expected=c.req.header('If-Match');if(expected!==undefined&&expected!==packet.packetHash)throw new ActivityError(409,'combat_b_wave_stale');
  const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_wave_stale');
  const fenced=await readBWaveManifestForRun(c.env.DB,auth.accountId,c.req.param('runId'));if(fenced.packetHash!==packet.packetHash)throw new ActivityError(409,'combat_b_wave_stale');c.header('ETag',fenced.packetHash);return c.json(fenced);
 }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);if(error instanceof ZodError||error instanceof SyntaxError)return c.json({accepted:false,errorCode:'combat_b_wave_source_invalid'},409);throw error;}
});}