import {assertCommunityOwnCreationSources,decodeCommunityCursor,encodeCommunityCursor} from './community-own-list-source';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {CommunityStoreError} from './community-content-store';
import {communityCommentCreateReceiptSchema} from './community-comment-creator';
import {CommunityOwnContentReader} from './community-own-content-reader';
export const communityOwnCommentsListVersion='community.own-comments-list.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
const querySchema=z.strictObject({limit:z.number().int().min(1).max(50).default(20),cursor:z.string().max(1024).nullable().optional()});
const cursorSchema=z.strictObject({v:z.literal(communityOwnCommentsListVersion),owner:z.string().regex(/^[0-9a-f]{64}$/u),scope:z.literal('OWN_COMMENT'),direction:z.literal('ASC'),createdAt:z.number().int().nonnegative().safe(),id});
type Row={content_id:string;created_at:number;state_json:string;parent_state:string;parent_id:string;response_json:string|null;audit_at:string|null;audit_action:string|null};
/** Own-only list uses actual creation source; cursor never grants access or supplies synthetic dates. */
export class CommunityOwnCommentsLister{
 constructor(private readonly db:D1Database,private readonly authorize?:()=>Promise<void>){}
 async list(accountId:string,raw:unknown){
  const parsed=querySchema.safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorize?.();const query=parsed.data,owner=await sha256Hex(accountId);let cursor:z.infer<typeof cursorSchema>|null=null;
  if(query.cursor){try{if(!/^[A-Za-z0-9_-]+$/u.test(query.cursor))throw new Error();cursor=cursorSchema.parse(decodeCommunityCursor(query.cursor));if(cursor.owner!==owner)throw new Error();}catch{throw new CommunityStoreError(400,'community_cursor_invalid');}}
  await assertCommunityOwnCreationSources(this.db,accountId,'COMMENT');
  const values:(string|number)[]=[accountId];if(cursor)values.push(cursor.createdAt,cursor.createdAt,cursor.id);values.push(query.limit+1);
  const statement=this.db.prepare(`SELECT m.content_id,m.created_at,m.parent_id,c.state_json,p.state_json AS parent_state,r.response_json,a.created_at AS audit_at,a.action AS audit_action,e.created_at AS edit_created_at,mp.created_at AS moderation_created_at,clp.created_at AS comment_lock_created_at
   FROM community_content_creation m JOIN community_contents c ON c.id=m.content_id JOIN community_contents p ON p.id=m.parent_id
   LEFT JOIN community_content_create_audit a ON a.content_id=m.content_id AND a.account_id=m.account_id
   LEFT JOIN community_content_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id
   LEFT JOIN community_content_edit_audit e ON e.content_id=c.id AND e.account_id=c.account_id AND e.next_revision=c.revision LEFT JOIN community_content_moderation_proof mp ON mp.content_id=c.id AND mp.next_revision=c.revision LEFT JOIN community_content_comment_lock_proof clp ON clp.content_id=c.id AND clp.next_revision=c.revision
   WHERE m.account_id=? AND m.kind='COMMENT' AND c.account_id=m.account_id
   AND json_extract(p.state_json,'$.kind')='POST' AND json_extract(p.state_json,'$.visibility')='VISIBLE'
   ${cursor?'AND (m.created_at>? OR (m.created_at=? AND m.content_id COLLATE BINARY>?))':''}
   ORDER BY m.created_at ASC,m.content_id COLLATE BINARY ASC LIMIT ?`).bind(...values);
  const rows=await statement.all<Row>();if(!rows.success)throw new CommunityStoreError(503,'community_storage_unavailable');
  if(rows.results.some(r=>!Number.isSafeInteger(r.created_at)||r.created_at<0||!id.safeParse(r.content_id).success))throw new CommunityStoreError(503,'community_storage_invalid');
  try{const ids=new Set<string>();for(const row of rows.results){if(ids.has(row.content_id)||!row.response_json||row.audit_action!=='CREATE_COMMENT')throw new Error();ids.add(row.content_id);const receipt=communityCommentCreateReceiptSchema.parse(JSON.parse(row.response_json));if(receipt.contentId!==row.content_id||receipt.parentId!==row.parent_id||receipt.createdAt!==row.created_at||row.audit_at!==new Date(row.created_at).toISOString())throw new Error();}}catch{throw new CommunityStoreError(503,'community_storage_invalid');}
  const reader=new CommunityOwnContentReader(this.db,this.authorize),items=[];
  for(const row of rows.results.slice(0,query.limit)){const read=await reader.read(accountId,row.content_id);if(read.content.kind!=='COMMENT')throw new CommunityStoreError(503,'community_storage_invalid');items.push({...read.content,rendererVersion:read.rendererVersion,createdAt:row.created_at});}
  await this.authorize?.();const current=await statement.all<Row>();if(!current.success)throw new CommunityStoreError(503,'community_storage_unavailable');
  if(canonicalJson(current.results)!==canonicalJson(rows.results))throw new CommunityStoreError(409,'community_content_stale');
  const last=items.at(-1),nextCursor=rows.results.length>query.limit&&last?encodeCommunityCursor({v:communityOwnCommentsListVersion,owner,scope:'OWN_COMMENT',direction:'ASC',createdAt:last.createdAt,id:last.id}):null;
  return {contractVersion:communityOwnCommentsListVersion,items,nextCursor};
 }
}