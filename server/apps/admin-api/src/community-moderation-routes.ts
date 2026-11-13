import type {Context,Hono} from 'hono';
import {communityModerationId,communityModerationVersion} from '../../../packages/common/src/community-moderation-contract';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {CommunityModerationCommands} from './community-moderation-commands';
import {CommunityModerationReports} from './community-moderation-reports';
import {CommunityModerationContents} from './community-moderation-contents';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function registerCommunityModerationRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 const handle=(kind:'LIST'|'REPORT'|'CONTENT'|'REVIEW'|'MODERATE')=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{if(c.env.ENVIRONMENT!=='DEV'&&c.env.ENVIRONMENT!=='PRD')throw new CommunityStoreError(503,'community_moderation_unavailable');const commands=new CommunityModerationCommands(c.env.DB,c.env.ENVIRONMENT);await commands.authorize(actor.id);
   if(c.req.header('X-Community-Moderation-Version')!==communityModerationVersion)throw new CommunityStoreError(409,'community_moderation_version_conflict');
   const write=kind==='REVIEW'||kind==='MODERATE',url=new URL(c.req.url),id=c.req.param('id');if(kind!=='LIST'&&(!communityModerationId.safeParse(id).success||url.searchParams.size))throw new CommunityStoreError(400,'contract_violation');
   if(write&&!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))throw new CommunityStoreError(400,'contract_violation');
   const raw=await readBoundedBody(c);if(!write&&raw!=='')throw new CommunityStoreError(400,'contract_violation');const reports=new CommunityModerationReports(commands),contents=new CommunityModerationContents(commands);
   if(kind==='LIST'){const entries=Array.from(url.searchParams.entries());if(entries.some(([k])=>!['status','limit','cursor'].includes(k))||new Set(entries.map(([k])=>k)).size!==entries.length)throw new CommunityStoreError(400,'contract_violation');const q=Object.fromEntries(entries);return c.json(await reports.list(actor.id,{...q,...(q.limit!==undefined?{limit:Number(q.limit)}:{})}));}
   if(kind==='REPORT')return c.json(await reports.read(actor.id,id!));if(kind==='CONTENT')return c.json(await contents.read(actor.id,id!));
   let body:unknown;try{body=JSON.parse(raw);}catch{throw new CommunityStoreError(400,'contract_violation');}const key=c.req.header('Idempotency-Key')??'';
   return c.json(kind==='REVIEW'?await reports.review(actor.id,id!,key,body):await contents.moderate(actor.id,id!,key,body));
  }catch(e){if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_moderation_unavailable'},503);}
 };
 app.get('/v1/admin/community/reports',handle('LIST'));app.get('/v1/admin/community/reports/:id',handle('REPORT'));app.post('/v1/admin/community/reports/:id/review',handle('REVIEW'));
 app.get('/v1/admin/community/contents/:id',handle('CONTENT'));app.post('/v1/admin/community/contents/:id/moderate',handle('MODERATE'));
}