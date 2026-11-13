import type { Context,Hono } from 'hono';
import {communityWriteAuthorization} from './community-write-authorization';
import {registerCommunityBlockRoutes,isCommunityBlockRoute} from './community-block-routes';
import {registerCommunityPublicRoutes,isCommunityPublicRoute} from './community-public-routes';
import {CommunityConsentService,communityCreationEligibilityVersion} from './community-consent';
import {registerCommunityConsentRoutes,isCommunityConsentRoute} from './community-consent-routes';
import {registerCommunityReportRoutes,isCommunityReportRoute} from './community-report-routes';
import { readBoundedBody } from './activity-routes';
import { ActivityError } from './activity-policy';
import { authenticateWebSupport } from './support-private-read-routes';
import { CommunityContentStore,CommunityStoreError,communityContentStoreVersion,communityContentEditVersion,
 communityDeleteRequestSchema,communityEditRequestSchema } from './community-content-store';
import { communityPostReferenceVerifier } from './community-post-references';
import { communityPostCreateRequestSchema,communityPostCreateVersion } from './community-post-contract';
import { CommunityContentCreator,CommunityRateError,communityCommentCreateVersion,communityCommentCreateRequestSchema } from './community-comment-creator';
import { CommunityOwnPostsLister,communityOwnPostsListVersion } from './community-own-posts-list';
import { CommunityOwnCommentsLister,communityOwnCommentsListVersion } from './community-own-comments-list';
import { CommunityOwnContentReader,communityOwnReadVersion } from './community-own-content-reader';
type Ctx=Context<{Bindings:Env}>;
// 10,000 Unicode points can occupy 120,000 bytes when JSON-escaped surrogate pairs are used.
// Includes the approved title and bounded command envelope. A transport cap, not a new content limit.
export const communityEditBodyByteLimit=128000;
export function isCommunityContentRoute(method:string,pathname:string):boolean {
 return isCommunityReportRoute(method,pathname)||isCommunityConsentRoute(method,pathname)||isCommunityPublicRoute(method,pathname)||isCommunityBlockRoute(method,pathname)||['POST','OPTIONS'].includes(method)&&pathname==='/v1/web/community/posts'||['GET','OPTIONS'].includes(method)&&['/v1/web/community/own/comments','/v1/web/community/own/posts'].includes(pathname)||['GET','OPTIONS'].includes(method)&&/^\/v1\/web\/community\/own\/contents\/[^/]+$/u.test(pathname)||['POST','OPTIONS'].includes(method)&&/^\/v1\/web\/community\/contents\/[^/]+\/(?:delete|edit|comments)$/u.test(pathname);
}
export function registerCommunityContentRoutes(app:Hono<{Bindings:Env}>){
 registerCommunityBlockRoutes(app);
 registerCommunityPublicRoutes(app);
 registerCommunityConsentRoutes(app);
 registerCommunityReportRoutes(app);
 const postPath='/v1/web/community/posts';
 app.options(postPath,(c:Ctx)=>{
  c.header('Cache-Control','no-store');c.header('Vary','Origin');const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
  if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
  if(c.req.header('Access-Control-Request-Method')!=='POST')return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');c.header('Access-Control-Allow-Methods','POST, OPTIONS');
  c.header('Access-Control-Allow-Headers','Content-Type, X-CSRF-Token, X-LastRail-App-Version-Code, X-Community-Content-Version, X-Community-Creation-Eligibility-Version');return c.body(null,204);
 });
 app.post(postPath,async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  try{
   const authorize=async()=>{const auth=await authenticateWebSupport(c,true);if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};
   const accountId=await authorize();
   if(new URL(c.req.url).search||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))return c.json({error:'contract_violation'},400);
   if(c.req.header('X-Community-Creation-Eligibility-Version')!==communityCreationEligibilityVersion)return c.json({error:'community_version_conflict'},409);
   if(c.req.header('X-Community-Content-Version')!==communityPostCreateVersion)return c.json({error:'community_version_conflict'},409);
   let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c,communityEditBodyByteLimit));}catch(e){if(e instanceof ActivityError)throw e;return c.json({error:'community_command_invalid'},400);}
   const parsed=communityPostCreateRequestSchema.safeParse(raw);if(!parsed.success)return c.json({error:'community_command_invalid'},400);
   const authorization=await communityWriteAuthorization(c,accountId,authorize);
   const mode=(c.env as Env&{COMMUNITY_REVIEW_POLICY_MODE?:string}).COMMUNITY_REVIEW_POLICY_MODE;
   // Initial approved implementation has no extra classifier. Explicitly required source cannot be fabricated.
   const review=mode===undefined||mode==='NONE'?undefined:async():Promise<'NONE'>=>{throw new Error('review source unavailable');};
   return c.json(await new CommunityContentCreator(c.env.DB,()=>Date.now(),authorization,review,communityPostReferenceVerifier(c.env.DB,c.env.ENVIRONMENT as 'DEV'|'PRD',c.req.header('X-LastRail-App-Version-Code')),actor=>new CommunityConsentService(c.env.DB,c.env.ENVIRONMENT as 'DEV'|'PRD','ko-KR').creationEligibility(actor)).createPost(accountId,parsed.data));
  }catch(e){if(e instanceof CommunityRateError){c.header('Retry-After',String(e.retryAfter));return c.json({error:e.code,retryAfter:e.retryAfter},429);}if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}
 });

 const commentPath='/v1/web/community/contents/:postId/comments';
 app.options(commentPath,(c:Ctx)=>{
  c.header('Cache-Control','no-store');c.header('Vary','Origin');const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
  if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
  if(c.req.header('Access-Control-Request-Method')!=='POST')return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');c.header('Access-Control-Allow-Methods','POST, OPTIONS');
  c.header('Access-Control-Allow-Headers','Content-Type, X-CSRF-Token, X-LastRail-App-Version-Code, X-Community-Content-Version, X-Community-Creation-Eligibility-Version');return c.body(null,204);
 });
 app.post(commentPath,async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  try{
   const authorize=async()=>{const auth=await authenticateWebSupport(c,true);if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};
   const accountId=await authorize();
   if(new URL(c.req.url).search||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))return c.json({error:'contract_violation'},400);
   if(c.req.header('X-Community-Creation-Eligibility-Version')!==communityCreationEligibilityVersion)return c.json({error:'community_version_conflict'},409);
   if(c.req.header('X-Community-Content-Version')!==communityCommentCreateVersion)return c.json({error:'community_version_conflict'},409);
   let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c));}catch(e){if(e instanceof ActivityError)throw e;return c.json({error:'community_command_invalid'},400);}
   const parsed=communityCommentCreateRequestSchema.safeParse(raw);if(!parsed.success)return c.json({error:'community_command_invalid'},400);
   const authorization=await communityWriteAuthorization(c,accountId,authorize);
   const mode=(c.env as Env&{COMMUNITY_REVIEW_POLICY_MODE?:string}).COMMUNITY_REVIEW_POLICY_MODE;
   // Initial approved implementation has no extra classifier. Explicitly required source cannot be fabricated.
   const review=mode===undefined||mode==='NONE'?undefined:async():Promise<'NONE'>=>{throw new Error('review source unavailable');};
   return c.json(await new CommunityContentCreator(c.env.DB,()=>Date.now(),authorization,review,undefined,actor=>new CommunityConsentService(c.env.DB,c.env.ENVIRONMENT as 'DEV'|'PRD','ko-KR').creationEligibility(actor)).create(accountId,{...parsed.data,parentId:c.req.param('postId')}));
  }catch(e){if(e instanceof CommunityRateError){c.header('Retry-After',String(e.retryAfter));return c.json({error:e.code,retryAfter:e.retryAfter},429);}if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}
 });
 const ownPath='/v1/web/community/own/contents/:contentId';
 const ownPreflight=(c:Ctx)=>{
  c.header('Cache-Control','no-store');c.header('Vary','Origin');
  const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
  if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
  if(c.req.header('Access-Control-Request-Method')!=='GET')return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');
  c.header('Access-Control-Allow-Methods','GET, OPTIONS');
  c.header('Access-Control-Allow-Headers','X-LastRail-App-Version-Code, X-Community-Own-Read-Version, X-Community-Own-Comments-Version, X-Community-Own-Posts-Version');
  return c.body(null,204);
 };
 app.options(ownPath,ownPreflight);
 app.options('/v1/web/community/own/comments',ownPreflight);
 app.options('/v1/web/community/own/posts',ownPreflight);
 app.get('/v1/web/community/own/comments',async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  try{const authorize=async()=>{const auth=await authenticateWebSupport(c,false);if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};
   const accountId=await authorize(),params=new URL(c.req.url).searchParams;
   if(c.req.raw.body!==null||[...params.keys()].some(k=>!['limit','cursor'].includes(k))||params.getAll('limit').length>1||params.getAll('cursor').length>1)return c.json({error:'contract_violation'},400);
   if(c.req.header('X-Community-Own-Comments-Version')!==communityOwnCommentsListVersion)return c.json({error:'community_version_conflict'},409);
   const rawLimit=params.get('limit');if(rawLimit!==null&&!/^[1-9][0-9]?$/u.test(rawLimit))return c.json({error:'contract_violation'},400);
   const lister=new CommunityOwnCommentsLister(c.env.DB,async()=>{if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');});
   return c.json(await lister.list(accountId,{limit:rawLimit===null?20:Number(rawLimit),cursor:params.get('cursor')}));
  }catch(e){if(e instanceof CommunityStoreError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}
 });
 app.get('/v1/web/community/own/posts',async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  try{const authorize=async()=>{const auth=await authenticateWebSupport(c,false);if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};
   const accountId=await authorize(),params=new URL(c.req.url).searchParams;
   if(c.req.raw.body!==null||[...params.keys()].some(k=>!['limit','cursor','boardId'].includes(k))||params.getAll('limit').length>1||params.getAll('cursor').length>1||params.getAll('boardId').length>1)return c.json({error:'contract_violation'},400);
   if(c.req.header('X-Community-Own-Posts-Version')!==communityOwnPostsListVersion)return c.json({error:'community_version_conflict'},409);
   const rawLimit=params.get('limit');if(rawLimit!==null&&!/^[1-9][0-9]?$/u.test(rawLimit))return c.json({error:'contract_violation'},400);
   const lister=new CommunityOwnPostsLister(c.env.DB,async()=>{if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');});
   return c.json(await lister.list(accountId,{limit:rawLimit===null?20:Number(rawLimit),cursor:params.get('cursor'),...(params.has('boardId')?{boardId:params.get('boardId')}: {})}));
  }catch(e){if(e instanceof CommunityStoreError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}
 });

 app.get(ownPath,async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  try{
   const authorize=async()=>{const auth=await authenticateWebSupport(c,false);
    if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;};
   const accountId=await authorize();
   if(new URL(c.req.url).search||c.req.raw.body!==null)return c.json({error:'contract_violation'},400);
   if(c.req.header('X-Community-Own-Read-Version')!==communityOwnReadVersion)return c.json({error:'community_version_conflict'},409);
   const reader=new CommunityOwnContentReader(c.env.DB,async()=>{if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');});
   return c.json(await reader.read(accountId,c.req.param('contentId')??''));
  }catch(error){if(error instanceof CommunityStoreError)return c.json({error:error.code},error.status);return c.json({error:'community_unavailable'},503);}
 });
 for(const action of ['delete','edit'] as const){
  const path='/v1/web/community/contents/:contentId/'+action;
  app.options(path,(c:Ctx)=>{
   c.header('Cache-Control','no-store');c.header('Vary','Origin');
   const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
   if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
   if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
   if(c.req.header('Access-Control-Request-Method')!=='POST')return c.json({error:'contract_violation'},400);
   c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');
   c.header('Access-Control-Allow-Methods','POST, OPTIONS');
   c.header('Access-Control-Allow-Headers','Content-Type, X-CSRF-Token, X-LastRail-App-Version-Code, X-Community-Content-Version, X-Community-Creation-Eligibility-Version');
   return c.body(null,204);
  });
  app.post(path,async(c:Ctx)=>{
   c.header('Cache-Control','no-store');
   try{
    const authorize=async()=>{
     const auth=await authenticateWebSupport(c,true);
     if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);
     return auth.accountId;
    };
    const accountId=await authorize();
    if(new URL(c.req.url).search||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))return c.json({error:'contract_violation'},400);
    const version=action==='delete'?communityContentStoreVersion:communityContentEditVersion;
    if(c.req.header('X-Community-Content-Version')!==version)return c.json({error:'community_version_conflict'},409);
    let raw:unknown;try{raw=JSON.parse(await readBoundedBody(c,action==='edit'?communityEditBodyByteLimit:18000));}
    catch(error){if(error instanceof ActivityError)throw error;return c.json({error:'community_command_invalid'},400);}
    const parsed=(action==='delete'?communityDeleteRequestSchema:communityEditRequestSchema).safeParse(raw);
    if(!parsed.success)return c.json({error:'community_command_invalid'},400);
    const authorization=await communityWriteAuthorization(c,accountId,authorize);
    const store=new CommunityContentStore(c.env.DB,()=>new Date(),authorization),command={...parsed.data,contentId:c.req.param('contentId')};
    return c.json(action==='delete'?await store.deleteOwned(accountId,command):await store.editOwned(accountId,command));
   }catch(error){
    if(error instanceof ActivityError||error instanceof CommunityStoreError)return c.json({error:error.code},error.status);
    return c.json({error:'community_unavailable'},503);
   }
  });
 }
}

