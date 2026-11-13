import type {Context,Hono} from 'hono';
import {communityModerationId,communityCommentLockVersion} from '../../../packages/common/src/community-moderation-contract';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {CommunityModerationCommands} from './community-moderation-commands';
import {CommunityCommentLocks} from './community-comment-locks';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function registerCommunityCommentLockRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 app.post('/v1/admin/community/contents/:id/comment-lock',async(c:Ctx)=>{
  c.header('Cache-Control','no-store');const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{if(c.env.ENVIRONMENT!=='DEV'&&c.env.ENVIRONMENT!=='PRD')throw new CommunityStoreError(503,'community_moderation_unavailable');const commands=new CommunityModerationCommands(c.env.DB,c.env.ENVIRONMENT);await commands.authorize(actor.id);
   if(c.req.header('X-Community-Comment-Lock-Version')!==communityCommentLockVersion)throw new CommunityStoreError(409,'community_comment_lock_version_conflict');
   const id=c.req.param('id');if(!communityModerationId.safeParse(id).success||new URL(c.req.url).searchParams.size||!/^application\/json(?:\s*;.*)?$/iu.test(c.req.header('Content-Type')??''))throw new CommunityStoreError(400,'contract_violation');
   const key=c.req.header('Idempotency-Key')??'',raw=await readBoundedBody(c);let body:unknown;try{body=JSON.parse(raw);}catch{throw new CommunityStoreError(400,'contract_violation');}
   return c.json(await new CommunityCommentLocks(commands).change(actor.id,id!,key,body));
  }catch(e){if(e instanceof CommunityStoreError||e instanceof ActivityError)return c.json({error:e.code},e.status);return c.json({error:'community_moderation_unavailable'},503);}
 });
}