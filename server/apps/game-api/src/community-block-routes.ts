import type {Context,Hono} from 'hono';
import {authenticateWebSupport} from './support-private-read-routes';
import {readBoundedBody} from './activity-routes';
import {ActivityError} from './activity-policy';
import {CommunityStoreError} from './community-content-store';
import {communityWriteAuthorization} from './community-write-authorization';
import {CommunityBlockStore,communityBlockVersion,communityBlockCommandSchema} from './community-block-store';
type Ctx=Context<{Bindings:Env}>;
const path='/v1/web/community/blocks';
export const isCommunityBlockRoute=(method:string,pathname:string)=>pathname===path&&['GET','POST','OPTIONS'].includes(method);
function authorization(c:Ctx,write:boolean){return async()=>{const auth=await authenticateWebSupport(c,write);if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};}
export function registerCommunityBlockRoutes(app:Hono<{Bindings:Env}>){
 app.options(path,(c:Ctx)=>{c.header('Cache-Control','no-store');c.header('Vary','Origin');const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
  if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
  if(!['GET','POST'].includes(c.req.header('Access-Control-Request-Method')??''))return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');c.header('Access-Control-Allow-Methods','GET, POST, OPTIONS');c.header('Access-Control-Allow-Headers','Content-Type, X-CSRF-Token, X-LastRail-App-Version-Code, X-Community-Block-Version');return c.body(null,204);
 });
 app.post(path,async(c:Ctx)=>{c.header('Cache-Control','no-store');try{
  const authorize=authorization(c,true),accountId=await authorize();
  if(new URL(c.req.url).search||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))return c.json({error:'contract_violation'},400);
  if(c.req.header('X-Community-Block-Version')!==communityBlockVersion)return c.json({error:'community_version_conflict'},409);
  let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c));}catch(e){if(e instanceof ActivityError)throw e;return c.json({error:'community_command_invalid'},400);}
  const parsed=communityBlockCommandSchema.safeParse(raw);if(!parsed.success)return c.json({error:'community_command_invalid'},400);
  const rights=await communityWriteAuthorization(c,accountId,authorize);
  return c.json(await new CommunityBlockStore(c.env.DB,rights).change(accountId,parsed.data));
 }catch(e){if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}});
 app.get(path,async(c:Ctx)=>{c.header('Cache-Control','no-store');try{
  const authorize=authorization(c,false),accountId=await authorize(),params=new URL(c.req.url).searchParams;
  if(c.req.raw.body!==null||[...params.keys()].some(k=>!['limit','cursor'].includes(k))||params.getAll('limit').length>1||params.getAll('cursor').length>1)return c.json({error:'contract_violation'},400);
  if(c.req.header('X-Community-Block-Version')!==communityBlockVersion)return c.json({error:'community_version_conflict'},409);
  const limit=params.get('limit');if(limit!==null&&!/^[1-9][0-9]?$/u.test(limit))return c.json({error:'contract_violation'},400);
  const rights=async()=>{if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');};
  return c.json(await new CommunityBlockStore(c.env.DB,undefined,rights).list(accountId,{limit:limit===null?20:Number(limit),cursor:params.get('cursor')}));
 }catch(e){if(e instanceof CommunityStoreError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}});
}