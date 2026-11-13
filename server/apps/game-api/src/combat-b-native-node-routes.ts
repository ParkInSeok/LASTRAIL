import {readBDepartureSourceForRun,bDepartureSourceBundleVersion} from './combat-b-departure-source-bundle';
import {bDepartureAssignmentVersion,readBDepartureAssignmentForRun} from './combat-b-departure-assignment';
import {bDepartureInputVersion,readBDepartureRuntimeInput} from './combat-b-departure-runtime-input';
import type {Hono,Context} from 'hono';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {bNativeNodeVersion,readBNativeNodeSource} from './combat-b-native-node-source';
export function registerBNativeNodeRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 for(const route of [{path:'b-departure-source',header:'X-B-Departure-Source-Version',version:bDepartureSourceBundleVersion,read:readBDepartureSourceForRun},{path:'b-native-node-source',header:'X-B-Native-Node-Version',version:bNativeNodeVersion,read:readBNativeNodeSource},{path:'b-departure-runtime-input',header:'X-B-Departure-Input-Version',version:bDepartureInputVersion,read:readBDepartureRuntimeInput},{path:'b-departure-assignment',header:'X-B-Assignment-Version',version:bDepartureAssignmentVersion,read:readBDepartureAssignmentForRun}])app.get('/v1/runs/:runId/'+route.path,async c=>{c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
 try{const runId=c.req.param('runId');if(!runId||runId.length>200||new URL(c.req.url).search||(await readBoundedBody(c)).length)throw new ActivityError(400,'contract_violation');
 if(c.req.header(route.header)!==route.version)throw new ActivityError(409,'combat_b_native_version_conflict');
 const p=await route.read(c.env.DB,auth.accountId,runId);if(c.req.header('If-Match')!==undefined&&c.req.header('If-Match')!==p.packetHash)throw new ActivityError(409,'combat_b_native_source_stale');
 const latestAuth=await authenticate(c);if('errorCode' in latestAuth)return c.json({accepted:false,errorCode:latestAuth.errorCode},latestAuth.status);if(latestAuth.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_native_auth_changed');
 const latest=await route.read(c.env.DB,auth.accountId,runId);if(latest.packetHash!==p.packetHash)throw new ActivityError(409,'combat_b_native_source_stale');
 c.header('ETag',p.packetHash);return c.json(p);
 }catch(e){if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);throw e;}});
}
