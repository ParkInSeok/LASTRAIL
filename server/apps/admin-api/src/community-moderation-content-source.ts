import {CommunityContent} from '../../../packages/common/src/community-content';
import {CommunityStoreError} from '../../game-api/src/community-content-store';
/** Shared current-content lookup/validation for moderator use cases; transitions stay in the domain model. */
export async function readModerationContent(db:D1Database,contentId:string){
 const row=await db.prepare('SELECT id,account_id,revision,state_json FROM community_contents WHERE id=?').bind(contentId).first<{id:string;account_id:string;revision:number;state_json:string}>();
 if(!row)throw new CommunityStoreError(404,'community_content_unavailable');
 try{const model=CommunityContent.restore(JSON.parse(row.state_json)),state=model.snapshot();if(state.id!==row.id||state.accountId!==row.account_id||state.revision!==row.revision)throw new Error();return {row,model,state};}catch{throw new CommunityStoreError(503,'community_moderation_storage_invalid');}
}