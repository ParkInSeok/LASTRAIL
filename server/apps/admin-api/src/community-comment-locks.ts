import {canonicalJson,sha256Hex} from '@lastrail/common';
import {CommunityContentError} from '../../../packages/common/src/community-content';
import {communityCommentLockCommand,communityCommentLockVersion} from '../../../packages/common/src/community-moderation-contract';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
import {verifyCommunityRenderedContent,communityRenderSourceGuards} from '../../game-api/src/community-render-proof';
import {communityMarkdownVersion} from '../../game-api/src/community-markdown';
import {CommunityModerationCommands} from './community-moderation-commands';
import {readModerationContent} from './community-moderation-content-source';
/** POST comment-lock use case only; visibility and creation eligibility remain owned by their existing rules. */
export class CommunityCommentLocks{
 constructor(private readonly commands:CommunityModerationCommands){}
 async change(actor:string,contentId:string,key:string,raw:unknown){
  await this.commands.authorize(actor);const parsed=communityCommentLockCommand.safeParse(raw);if(!parsed.success)throw new CommunityStoreError(400,'contract_violation');const body=parsed.data;
  return this.commands.execute(actor,key,'CONTENT',contentId,body,async()=>{
   const {row,model,state}=await readModerationContent(this.commands.db,contentId);
   if(row.revision!==body.expectedRevision)throw new CommunityStoreError(409,'community_revision_conflict');
   if(state.kind!=='POST')throw new CommunityStoreError(409,'community_comment_lock_target_invalid');if(state.visibility==='AUTHOR_DELETED')throw new CommunityStoreError(409,'community_deleted');
   const locked=body.action==='LOCK_COMMENTS';if(locked===state.commentLocked)throw new CommunityStoreError(409,'community_comment_lock_transition_invalid');
   await verifyCommunityRenderedContent(this.commands.db,state,true);const source=await communityRenderSourceGuards(this.commands.db,state);
   try{model.moderate({principalId:actor,canModerate:true},body.expectedRevision,body.action);}catch(e){if(e instanceof CommunityContentError)throw new CommunityStoreError(409,e.message);throw e;}
   const next=model.snapshot(),json=canonicalJson(next),hash=await sha256Hex(json),at=new Date().toISOString();
   const assertCurrent=async()=>{const current=await readModerationContent(this.commands.db,contentId);if(canonicalJson(current.row)!==canonicalJson(row))throw new CommunityStoreError(409,'community_moderation_stale');await verifyCommunityRenderedContent(this.commands.db,current.state,true);};
   return {receipt:{contractVersion:communityCommentLockVersion,environment:this.commands.environment,resourceType:'CONTENT' as const,resourceId:contentId,kind:'POST' as const,revision:next.revision,visibility:next.visibility as 'VISIBLE'|'HIDDEN'|'REVIEW_PENDING',commentLocked:next.commentLocked,rendererVersion:communityMarkdownVersion},guards:[this.commands.fence('EXISTS(SELECT 1 FROM community_contents WHERE id=? AND account_id=? AND revision=? AND state_json=?)',[contentId,row.account_id,row.revision,row.state_json]),...source],writes:[this.commands.db.prepare('UPDATE community_contents SET revision=?,state_json=? WHERE id=? AND account_id=? AND revision=? AND state_json=?').bind(next.revision,json,contentId,row.account_id,row.revision,row.state_json),this.commands.db.prepare('INSERT INTO community_content_comment_lock_proof VALUES(?,?,?,?,?,?,?,?,?,?,?)').bind(this.commands.environment,actor,key,contentId,row.revision,next.revision,body.action,hash,at,communityMarkdownVersion,'community-retention-v3-20260930')],summary:{action:body.action,beforeRevision:row.revision,afterRevision:next.revision,beforeLocked:state.commentLocked?1:0,afterLocked:next.commentLocked?1:0,visibility:next.visibility},assertCurrent};
  },communityCommentLockVersion);
 }
}