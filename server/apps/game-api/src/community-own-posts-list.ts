import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {CommunityStoreError} from './community-content-store';
import {readCommunityPostDetails} from './community-post-projection';
import {CommunityOwnContentReader} from './community-own-content-reader';
import {assertCommunityOwnCreationSources,decodeCommunityCursor,encodeCommunityCursor} from './community-own-list-source';
export const communityOwnPostsListVersion='community.own-posts-list.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),board=z.enum(['free','qna','guide_build','feedback']);
const querySchema=z.strictObject({limit:z.number().int().min(1).max(50).default(20),cursor:z.string().max(1024).nullable().optional(),boardId:board.optional()});
const cursorSchema=z.strictObject({v:z.literal(communityOwnPostsListVersion),owner:z.string().regex(/^[0-9a-f]{64}$/u),scope:z.literal('OWN_POST'),direction:z.literal('DESC'),boardId:board.nullable(),createdAt:z.number().int().nonnegative().safe(),id});
type Row={content_id:string;created_at:number;state_json:string;details_json:string;response_json:string};
/** Own-only paginated read. Model body proof and own source checks share existing owners. */
export class CommunityOwnPostsLister{
 constructor(private readonly db:D1Database,private readonly authorize?:()=>Promise<void>){}
 async list(accountId:string,raw:unknown){
  const parsed=querySchema.safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorize?.();const query=parsed.data,owner=await sha256Hex(accountId);let cursor:z.infer<typeof cursorSchema>|null=null;
  if(query.cursor){try{cursor=cursorSchema.parse(decodeCommunityCursor(query.cursor));if(cursor.owner!==owner||cursor.boardId!==(query.boardId??null))throw new Error();}catch{throw new CommunityStoreError(400,'community_cursor_invalid');}}
  await assertCommunityOwnCreationSources(this.db,accountId,'POST');
  const values:(string|number)[]=[accountId];if(query.boardId)values.push(query.boardId);if(cursor)values.push(cursor.createdAt,cursor.createdAt,cursor.id);values.push(query.limit+1);
  const statement=this.db.prepare(`SELECT m.content_id,m.created_at,c.state_json,d.details_json,r.response_json,e.created_at AS edit_created_at,mp.created_at AS moderation_created_at,clp.created_at AS comment_lock_created_at
   FROM community_content_creation m JOIN community_contents c ON c.id=m.content_id
   JOIN community_post_details d ON d.content_id=c.id JOIN community_post_create_audit a ON a.content_id=c.id AND a.account_id=m.account_id
   JOIN community_content_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id
   LEFT JOIN community_content_edit_audit e ON e.content_id=c.id AND e.account_id=c.account_id AND e.next_revision=c.revision LEFT JOIN community_content_moderation_proof mp ON mp.content_id=c.id AND mp.next_revision=c.revision LEFT JOIN community_content_comment_lock_proof clp ON clp.content_id=c.id AND clp.next_revision=c.revision
   WHERE m.account_id=? AND m.kind='POST' AND c.account_id=m.account_id
   ${query.boardId?"AND json_extract(d.details_json,'$.boardId')=?":''}
   ${cursor?'AND (m.created_at<? OR (m.created_at=? AND m.content_id COLLATE BINARY<?))':''}
   ORDER BY m.created_at DESC,m.content_id COLLATE BINARY DESC LIMIT ?`).bind(...values);
  const rows=await statement.all<Row>();if(!rows.success)throw new CommunityStoreError(503,'community_storage_unavailable');
  const reader=new CommunityOwnContentReader(this.db,this.authorize),items=[];
  for(const row of rows.results.slice(0,query.limit)){
   const details=await readCommunityPostDetails(row);
   const read=await reader.read(accountId,row.content_id);if(read.content.kind!=='POST')throw new CommunityStoreError(503,'community_storage_invalid');const visible=read.content.visibility==='VISIBLE';
   items.push({...read.content,rendererVersion:read.rendererVersion,createdAt:row.created_at,boardId:details.boardId,spoiler:visible?details.spoiler:null,wikiRefs:visible?details.wikiRefs:null,sourceBalanceVersion:visible?details.sourceBalanceVersion:null});
  }
  await this.authorize?.();const current=await statement.all<Row>();if(!current.success)throw new CommunityStoreError(503,'community_storage_unavailable');if(canonicalJson(current.results)!==canonicalJson(rows.results))throw new CommunityStoreError(409,'community_content_stale');
  const last=items.at(-1),nextCursor=rows.results.length>query.limit&&last?encodeCommunityCursor({v:communityOwnPostsListVersion,owner,scope:'OWN_POST',direction:'DESC',boardId:query.boardId??null,createdAt:last.createdAt,id:last.id}):null;
  return {contractVersion:communityOwnPostsListVersion,items,nextCursor};
 }
}