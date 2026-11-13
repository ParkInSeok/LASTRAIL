import {CommunityStoreError} from './community-content-store';
import {communityCommentCreateVersion} from './community-content-creator';
import {communityPostCreateVersion} from './community-post-contract';
import {communityMarkdownVersion} from './community-markdown';
/** Own-history integrity check only. Public pagination must not expand this actor-history scan. */
export async function assertCommunityOwnCreationSources(db:D1Database,accountId:string,kind:'POST'|'COMMENT'){
 const missing=await db.prepare(`SELECT 1 FROM community_contents c WHERE c.account_id=? AND json_extract(c.state_json,'$.kind')=?
  AND NOT EXISTS(SELECT 1 FROM community_content_creation m WHERE m.content_id=c.id AND m.account_id=c.account_id AND m.kind=? AND m.parent_id IS json_extract(c.state_json,'$.parentId')) LIMIT 1`).bind(accountId,kind,kind).first();
 if(missing)throw new CommunityStoreError(503,'community_creation_source_unavailable');
 const post=kind==='POST',audit=post?'community_post_create_audit':'community_content_create_audit';
 const invalid=await db.prepare(`SELECT 1 FROM community_contents c JOIN community_content_creation m ON m.content_id=c.id
  LEFT JOIN ${audit} a ON a.content_id=c.id AND a.account_id=c.account_id
  LEFT JOIN community_content_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id
  ${post?'LEFT JOIN community_post_details d ON d.content_id=c.id':''}
  WHERE c.account_id=? AND json_extract(c.state_json,'$.kind')=? AND (
   m.account_id IS NOT c.account_id OR m.kind IS NOT ? OR m.parent_id IS NOT json_extract(c.state_json,'$.parentId')
   OR a.action IS NOT ? OR a.next_revision IS NOT 1 OR a.created_at IS NOT strftime('%Y-%m-%dT%H:%M:%fZ',m.created_at/1000.0,'unixepoch')
   OR json_extract(r.response_json,'$.createdAt') IS NOT m.created_at OR json_extract(r.response_json,'$.contentId') IS NOT c.id
   OR json_extract(r.response_json,'$.parentId') IS NOT m.parent_id OR json_extract(r.response_json,'$.commandId') IS NOT a.command_id
   OR json_extract(r.response_json,'$.contractVersion') IS NOT ? OR json_extract(r.response_json,'$.rendererVersion') IS NOT ?
   ${post?"OR d.content_id IS NULL OR json_extract(d.details_json,'$.boardId') IS NOT json_extract(r.response_json,'$.boardId')":''}) LIMIT 1`).bind(accountId,kind,kind,post?'CREATE_POST':'CREATE_COMMENT',post?communityPostCreateVersion:communityCommentCreateVersion,communityMarkdownVersion).first();
 if(invalid)throw new CommunityStoreError(503,'community_storage_invalid');
}
export function decodeCommunityCursor(raw:string):unknown{try{if(raw.length>1024||!/^[A-Za-z0-9_-]+$/u.test(raw))throw new Error();return JSON.parse(atob(raw.replaceAll('-','+').replaceAll('_','/')));}catch{throw new CommunityStoreError(400,'community_cursor_invalid');}}
export function encodeCommunityCursor(value:unknown):string{return btoa(JSON.stringify(value)).replaceAll('+','-').replaceAll('/','_').replace(/=+$/u,'');}