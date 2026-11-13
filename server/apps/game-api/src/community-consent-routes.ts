import type {Context,Hono} from 'hono';
import {CommunityConsentService,communityConsentVersion,communityConsentAcceptSchema} from './community-consent';
import {CommunityStoreError} from './community-content-store';
import {authenticateWebSupport} from './support-private-read-routes';
import {communityWriteAuthorization} from './community-write-authorization';
import {readBoundedBody} from './activity-routes';
import {ActivityError} from './activity-policy';
type Ctx=Context<{Bindings:Env}>;
const read='/v1/web/community/consent',accept='/v1/web/community/consent/accept';
export const isCommunityConsentRoute=(method:string,path:string)=>path===read&&['GET','OPTIONS'].includes(method)||path===accept&&['POST','OPTIONS'].includes(method);
export function registerCommunityConsentRoutes(app:Hono<{Bindings:Env}>){
 for(const [path,method] of [[read,'GET'],[accept,'POST']])app.options(path!, (c:Ctx)=>{c.header('Cache-Control','no-store');c.header('Vary','Origin');const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);if(c.req.header('Access-Control-Request-Method')!==method)return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');c.header('Access-Control-Allow-Methods',method+', OPTIONS');c.header('Access-Control-Allow-Headers','Content-Type, X-CSRF-Token, X-LastRail-App-Version-Code, X-Community-Consent-Version');return c.body(null,204);
 });
 const auth=(c:Ctx,write:boolean)=>async()=>{const result=await authenticateWebSupport(c,write);if('errorCode' in result)throw new CommunityStoreError(result.status===500?503:result.status,result.errorCode);return result.accountId;};
 app.get(read,async(c:Ctx)=>{c.header('Cache-Control','no-store');try{const authorize=auth(c,false),accountId=await authorize();if(new URL(c.req.url).search||c.req.raw.body!==null)return c.json({error:'contract_violation'},400);if(c.req.header('X-Community-Consent-Version')!==communityConsentVersion)return c.json({error:'community_version_conflict'},409);
  const rights=async()=>{if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');return [];};return c.json(await new CommunityConsentService(c.env.DB,c.env.ENVIRONMENT as 'DEV'|'PRD','ko-KR',rights).current(accountId));
 }catch(e){if(e instanceof CommunityStoreError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}});
 app.post(accept,async(c:Ctx)=>{c.header('Cache-Control','no-store');try{const authorize=auth(c,true),accountId=await authorize();if(new URL(c.req.url).search||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))return c.json({error:'contract_violation'},400);if(c.req.header('X-Community-Consent-Version')!==communityConsentVersion)return c.json({error:'community_version_conflict'},409);
  let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c));}catch(e){if(e instanceof ActivityError)throw e;return c.json({error:'community_command_invalid'},400);}const parsed=communityConsentAcceptSchema.safeParse(raw);if(!parsed.success)return c.json({error:'community_command_invalid'},400);
  const rights=await communityWriteAuthorization(c,accountId,authorize);return c.json(await new CommunityConsentService(c.env.DB,c.env.ENVIRONMENT as 'DEV'|'PRD','ko-KR',rights).accept(accountId,parsed.data));
 }catch(e){if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}});
}