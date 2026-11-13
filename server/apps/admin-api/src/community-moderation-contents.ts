import {canonicalJson,sha256Hex} from '@lastrail/common';
import {CommunityContentError} from '../../../packages/common/src/community-content';
import {communityModerateCommand,communityModerationVersion} from '../../../packages/common/src/community-moderation-contract';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
import {CommunityPublicReader} from '../../game-api/src/community-public-reader';
import {verifyCommunityRenderedContent,communityRenderSourceGuards} from '../../game-api/src/community-render-proof';
import {communityMarkdownVersion} from '../../game-api/src/community-markdown';
import {CommunityModerationCommands} from './community-moderation-commands';
import {readModerationContent} from './community-moderation-content-source';
/** Owns moderator content use cases; CommunityContent owns visibility rules and existing source owners supply proof. */
export class CommunityModerationContents{
 constructor(private readonly commands:CommunityModerationCommands){}
 private row(contentId:string){return readModerationContent(this.commands.db,contentId);}
 async read(actor:string,contentId:string){await this.commands.authorize(actor);const initial=await this.row(contentId),s=initial.state,current=await this.row(contentId);if(canonicalJson(current.row)!==canonicalJson(initial.row))throw new CommunityStoreError(409,'community_moderation_stale');await this.commands.authorize(actor);return {contractVersion:communityModerationVersion,environment:this.commands.environment,content:{id:s.id,kind:s.kind,parentId:s.parentId,revision:s.revision,visibility:s.visibility,commentLocked:s.commentLocked}};}
 async moderate(actor:string,contentId:string,key:string,raw:unknown){await this.commands.authorize(actor);const parsed=communityModerateCommand.safeParse(raw);if(!parsed.success)throw new CommunityStoreError(400,'contract_violation');const body=parsed.data;
  return this.commands.execute(actor,key,'CONTENT',contentId,body,async()=>{
   const initial=await this.row(contentId),{row,model,state}=initial;
   if(row.revision!==body.expectedRevision)throw new CommunityStoreError(409,'community_revision_conflict');if(state.visibility==='AUTHOR_DELETED')throw new CommunityStoreError(409,'community_deleted');
   if(body.action==='HIDE'&&!['VISIBLE','REVIEW_PENDING'].includes(state.visibility)||body.action==='RESTORE'&&state.visibility!=='HIDDEN')throw new CommunityStoreError(409,'community_moderation_transition_invalid');
   await verifyCommunityRenderedContent(this.commands.db,state,true);const proofGuards=await communityRenderSourceGuards(this.commands.db,state),reader=new CommunityPublicReader(this.commands.db);
   let parent:Awaited<ReturnType<CommunityModerationContents['row']>>|null=null,eligibility:D1PreparedStatement[]=[];
   const eligible=async()=>{const author=await this.commands.db.prepare("SELECT a.id FROM accounts a JOIN community_public_authors p ON p.account_id=a.id WHERE a.id=? AND a.status='ACTIVE' AND p.public_author_id=?").bind(state.accountId,state.publicAuthorId).first();if(!author)throw new CommunityStoreError(409,'community_restore_ineligible');};
   if(body.action==='RESTORE'){
    await eligible();eligibility=await reader.publicationSourceGuards(state);
    eligibility.push(this.commands.fence("EXISTS(SELECT 1 FROM accounts a JOIN community_public_authors p ON p.account_id=a.id WHERE a.id=? AND a.status='ACTIVE' AND p.public_author_id=?)",[state.accountId,state.publicAuthorId]));
    if(state.parentId){try{await reader.read(null,state.parentId);}catch(e){if(e instanceof CommunityStoreError&&e.status===404)throw new CommunityStoreError(409,'community_parent_unavailable');throw e;}parent=await this.row(state.parentId);if(parent.state.visibility!=='VISIBLE')throw new CommunityStoreError(409,'community_parent_unavailable');eligibility.push(...await reader.publicationSourceGuards(parent.state),...await communityRenderSourceGuards(this.commands.db,parent.state),this.commands.fence('EXISTS(SELECT 1 FROM community_contents WHERE id=? AND revision=? AND state_json=?)',[parent.row.id,parent.row.revision,parent.row.state_json]));}
   }
   try{model.moderate({principalId:actor,canModerate:true},body.expectedRevision,body.action,parent?.state??null);}catch(e){if(e instanceof CommunityContentError)throw new CommunityStoreError(409,e.message);throw e;}
   const next=model.snapshot(),at=new Date(),nextJson=canonicalJson(next),nextHash=await sha256Hex(nextJson);
   const assertCurrent=async()=>{const current=await this.row(contentId);if(canonicalJson(current.row)!==canonicalJson(row))throw new CommunityStoreError(409,'community_moderation_stale');await verifyCommunityRenderedContent(this.commands.db,current.state,true);if(body.action==='RESTORE'){await eligible();await reader.publicationSourceGuards(current.state);if(parent){try{const currentParent=await this.row(parent.row.id);if(canonicalJson(currentParent.row)!==canonicalJson(parent.row))throw new Error();await reader.read(null,parent.row.id);}catch{throw new CommunityStoreError(409,'community_parent_unavailable');}}}};
   return {receipt:{contractVersion:communityModerationVersion,environment:this.commands.environment,resourceType:'CONTENT' as const,resourceId:contentId,revision:next.revision,visibility:next.visibility as 'VISIBLE'|'HIDDEN',rendererVersion:communityMarkdownVersion},guards:[this.commands.fence('EXISTS(SELECT 1 FROM community_contents WHERE id=? AND account_id=? AND revision=? AND state_json=?)',[contentId,row.account_id,row.revision,row.state_json]),...proofGuards,...eligibility],writes:[this.commands.db.prepare('UPDATE community_contents SET revision=?,state_json=? WHERE id=? AND revision=? AND state_json=?').bind(next.revision,nextJson,contentId,row.revision,row.state_json),this.commands.db.prepare('INSERT INTO community_content_moderation_proof VALUES(?,?,?,?,?,?,?,?,?,?,?)').bind(this.commands.environment,actor,key,contentId,row.revision,next.revision,body.action,nextHash,at.toISOString(),communityMarkdownVersion,'community-retention-v3-20260930')],summary:{action:body.action,beforeRevision:row.revision,afterRevision:next.revision,beforeVisibility:state.visibility,afterVisibility:next.visibility},assertCurrent};
  });
 }
}