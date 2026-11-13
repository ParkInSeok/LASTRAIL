import type {Context,Hono} from 'hono';
import {authenticateWebSupport} from './support-private-read-routes';
import {hasWebSessionCookie} from './web-session';
import {CommunityStoreError} from './community-content-store';
import {CommunityPublicReader,communityPublicReadVersion} from './community-public-reader';
type Ctx=Context<{Bindings:Env}>;
const posts='/v1/web/community/posts',detail='/v1/web/community/contents/:contentId',comments='/v1/web/community/contents/:postId/comments';
export function isCommunityPublicRoute(method:string,path:string){return ['GET','OPTIONS'].includes(method)&&(path===posts||/^\/v1\/web\/community\/contents\/[^/]+(?:\/comments)?$/u.test(path));}
/** Anonymous PRD and authenticated personal projections share no cached response.
 * Invalid/expired supplied web credentials never silently fall back to an anonymous viewer. */
async function viewer(c:Ctx):Promise<string|null>{
 c.header('Cache-Control','no-store');c.header('Vary','Origin, Cookie');
 const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
 if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))throw new CommunityStoreError(503,'community_unavailable');
 if(c.req.header('Origin')!==origin)throw new CommunityStoreError(403,'web_origin_forbidden');
 c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');
 if(c.env.ENVIRONMENT==='PRD'&&!hasWebSessionCookie(c.req.header('Cookie')))return null;
 const auth=await authenticateWebSupport(c,false);c.header('Vary','Origin, Cookie');
 if('errorCode' in auth)throw new CommunityStoreError(auth.status===500?503:auth.status,auth.errorCode);return auth.accountId;
}
export function registerCommunityPublicRoutes(app:Hono<{Bindings:Env}>){
 for(const path of [posts,detail,comments])app.options(path,async(c:Ctx,next)=>{
  // Creation owns POST preflight on shared URLs; this read owner does not replace it.
  if(path!==detail&&c.req.header('Access-Control-Request-Method')==='POST')return next();
  c.header('Cache-Control','no-store');c.header('Vary','Origin, Cookie');const origin=(c.env as Env&{WEB_ALLOWED_ORIGIN?:string}).WEB_ALLOWED_ORIGIN;
  if(!origin||!['DEV','PRD'].includes(c.env.ENVIRONMENT))return c.json({error:'community_unavailable'},503);
  if(c.req.header('Origin')!==origin)return c.json({error:'web_origin_forbidden'},403);
  if(c.req.header('Access-Control-Request-Method')!=='GET')return c.json({error:'contract_violation'},400);
  c.header('Access-Control-Allow-Origin',origin);c.header('Access-Control-Allow-Credentials','true');c.header('Access-Control-Allow-Methods','GET, OPTIONS');c.header('Access-Control-Allow-Headers','X-LastRail-App-Version-Code, X-Community-Public-Version');return c.body(null,204);
 });
 const handle=(kind:'DETAIL'|'POST'|'COMMENT')=>async(c:Ctx)=>{c.header('Cache-Control','no-store');c.header('Vary','Origin, Cookie');try{
  const accountId=await viewer(c),params=new URL(c.req.url).searchParams;
  if(c.req.raw.body!==null)return c.json({error:'contract_violation'},400);
  if(c.req.header('X-Community-Public-Version')!==communityPublicReadVersion)return c.json({error:'community_version_conflict'},409);
  const authorization=async()=>{if(await viewer(c)!==accountId)throw new CommunityStoreError(403,'community_forbidden');},reader=new CommunityPublicReader(c.env.DB,authorization);
  if(kind==='DETAIL'){if(params.size)return c.json({error:'contract_violation'},400);return c.json(await reader.read(accountId,c.req.param('contentId')??''));}
  const allowed=kind==='POST'?['limit','cursor','boardId']:['limit','cursor'];
  if([...params.keys()].some(k=>!allowed.includes(k))||allowed.some(k=>params.getAll(k).length>1))return c.json({error:'contract_violation'},400);
  const limit=params.get('limit');if(limit!==null&&!/^[1-9][0-9]?$/u.test(limit))return c.json({error:'contract_violation'},400);
  const query={limit:limit===null?20:Number(limit),cursor:params.get('cursor'),...(params.has('boardId')?{boardId:params.get('boardId')}:{})};
  return c.json(kind==='POST'?await reader.listPosts(accountId,query):await reader.listComments(accountId,c.req.param('postId')??'',query));
 }catch(e){if(e instanceof CommunityStoreError)return c.json({error:e.code},e.status);return c.json({error:'community_unavailable'},503);}};
 app.get(posts,handle('POST'));app.get(detail,handle('DETAIL'));app.get(comments,handle('COMMENT'));
}