import {Hono} from 'hono';
import {resolveAuthContext} from './index';
import {verifyAccessToken} from './auth';
import {ActivityError} from './activity-policy';
import {createBActiveProjectileSourceHttp} from './combat-b-active-projectile-http';
import type {LocalReference} from './combat-b-local-flow-source';
import type {BActiveProjectileSource} from './combat-b-active-projectile-source';
import {BTargetLifecycleRegistry,targetLifecycleVersion} from './combat-b-target-lifecycle';
class LocalHttpError extends Error{constructor(readonly status:400|401|403|404|409|500|503,readonly code:string){super(code);}}
export function createBTargetLifecycleHttp(reference:LocalReference,registry:BTargetLifecycleRegistry){
 const original=createBActiveProjectileSourceHttp(reference),app=new Hono<{Bindings:Env}>();
 app.all('*',async c=>{const url=new URL(c.req.url);if(url.pathname!=='/_local/combat-b/target-lifecycle')return original.fetch(c.req.raw,c.env);
 c.header('Cache-Control','no-store');c.header('X-Combat-B-Lifecycle-Version',targetLifecycleVersion);
 try{
 if(c.env.ENVIRONMENT!=='LOCAL'||url.protocol!=='http:'||!['127.0.0.1','localhost','[::1]'].includes(url.hostname))throw new LocalHttpError(404,'combat_b_lifecycle_local_disabled');
 if(c.req.header('Origin')&&c.req.header('Origin')!==url.origin)throw new ActivityError(403,'combat_b_lifecycle_origin_forbidden');
 if(c.req.method!=='GET'||url.search||c.req.header('Content-Length')&&c.req.header('Content-Length')!=='0')throw new ActivityError(400,'combat_b_lifecycle_request_invalid');
 const authorize=async()=>{const auth=await resolveAuthContext(c);if('errorCode' in auth)throw new LocalHttpError(auth.status,auth.errorCode);const verified=await verifyAccessToken({secret:(c.env as Env&{AUTH_SIGNING_KEY_CURRENT?:string}).AUTH_SIGNING_KEY_CURRENT??'',token:(c.req.header('Authorization')??'').slice(7).trim(),nowMs:Date.now()});if(!verified||verified.accountId!==auth.accountId||verified.sessionId!==auth.sessionId)throw new LocalHttpError(401,'access_token_invalid');return auth;};
 const auth=await authorize();if(c.req.header('X-Combat-B-Lifecycle-Version')!==targetLifecycleVersion)throw new ActivityError(409,'combat_b_lifecycle_version_conflict');
 let fixture=registry.find(auth.accountId);
 if(!fixture){const raw=await original.fetch(new Request(new URL('/_local/combat-b/active-projectile-source',url),{headers:c.req.raw.headers}),c.env);if(raw.status!==200)return raw;const source=await raw.json() as BActiveProjectileSource;if(source.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_lifecycle_source_conflict');await authorize();fixture=await registry.bind(source);}
 const packet=await fixture.read();await authorize();return c.json(packet);
 }catch(e){if(e instanceof ActivityError||e instanceof LocalHttpError)return c.json({accepted:false,errorCode:e.code},e.status);console.error('combat_b_lifecycle_unavailable');return c.json({accepted:false,errorCode:'combat_b_lifecycle_unavailable'},503);}});return app;
}
