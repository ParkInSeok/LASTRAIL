import {Hono} from 'hono';
import {resolveAuthContext} from './index';
import {verifyAccessToken} from './auth';
import {readBoundedBody} from './activity-routes';
import {ActivityError} from './activity-policy';
import {BSelectionStore,BSelectionAuthError,selectionVersion} from './combat-b-selection-store';
import {UnavailableBSelectionCatalog,type BSelectionCatalogProvider} from './combat-b-selection-catalog';
export {selectionVersion};
type LocalEnv=Env&{LOCAL_B_SELECTION?:'ISOLATED_LOCAL_FIXTURE';AUTH_SIGNING_KEY_CURRENT?:string};
/** Real auth and SQLite persistence factory; unregistered, loopback LOCAL only. */
export function createCombatBSelectionHttp(provider:BSelectionCatalogProvider=new UnavailableBSelectionCatalog()){
 const app=new Hono<{Bindings:Env}>();
 app.use('*',async(c,next)=>{
  c.header('Cache-Control','no-store');c.header('X-Combat-B-Selection-Version',selectionVersion);
  const u=new URL(c.req.url);
  if(c.env.ENVIRONMENT!=='LOCAL'||(c.env as LocalEnv).LOCAL_B_SELECTION!=='ISOLATED_LOCAL_FIXTURE'||u.protocol!=='http:'||!['127.0.0.1','localhost','[::1]'].includes(u.hostname))return c.json({accepted:false,errorCode:'combat_b_local_disabled'},404);
  if(c.req.header('Origin')&&c.req.header('Origin')!==u.origin)return c.json({accepted:false,errorCode:'combat_b_origin_forbidden'},403);
  return next();
 });
 app.on(['GET','POST'],'/_local/combat-b/selection',async c=>{
  try{
   const auth=await resolveAuthContext(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
   if(new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
   if(c.req.header('X-Combat-B-Selection-Version')!==selectionVersion)throw new ActivityError(409,'combat_b_transport_version_conflict');
   const verified=await verifyAccessToken({secret:(c.env as LocalEnv).AUTH_SIGNING_KEY_CURRENT??'',token:(c.req.header('Authorization')??'').slice(7).trim(),nowMs:Date.now()});
   if(!verified||verified.accountId!==auth.accountId||verified.sessionId!==auth.sessionId)throw new BSelectionAuthError('access_token_invalid');
   const principal={accountId:auth.accountId,sessionId:auth.sessionId,accessExpiresAt:verified.expiresAt};
   const store=new BSelectionStore(c.env.DB,provider),text=await readBoundedBody(c);
   if(c.req.method==='GET'){if(text.length)throw new ActivityError(400,'contract_violation');return c.json(await store.read(principal));}
   if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   const key=c.req.header('Idempotency-Key')??'';if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
   let input:unknown;try{input=JSON.parse(text);}catch{throw new ActivityError(400,'combat_b_request_invalid');}
   return c.json(await store.save(principal,input,key));
  }catch(e){
   if(e instanceof ActivityError||e instanceof BSelectionAuthError)return c.json({accepted:false,errorCode:e.code},e.status);
   console.error('combat_b_selection_storage_unavailable');
   return c.json({accepted:false,errorCode:'combat_b_selection_storage_unavailable'},503);
  }
 });
 app.notFound(c=>c.json({accepted:false,errorCode:'combat_b_selection_route_not_found'},404));
 return app;
}