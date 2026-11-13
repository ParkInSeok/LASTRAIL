import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {CommunityContent} from '../../../packages/common/src/community-content';
import {CommunityStoreError} from './community-content-store';
import {CommunityBlockStore} from './community-block-store';
import {verifyCommunityRenderedContent} from './community-render-proof';
import {readCommunityPostDetails} from './community-post-projection';
import {communityPostCreateReceiptSchema} from './community-post-contract';
import {communityCommentCreateReceiptSchema} from './community-comment-creator';
import {communityMarkdownVersion} from './community-markdown';
import {decodeCommunityCursor,encodeCommunityCursor} from './community-own-list-source';
export const communityPublicReadVersion='community.public-read.v1.20261008' as const;
export const communityPublicScopeVersion='community.public-source.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),board=z.enum(['free','qna','guide_build','feedback']);
const listQuery=z.strictObject({limit:z.number().int().min(1).max(50).default(20),cursor:z.string().min(1).max(1024).nullable().optional(),boardId:board.optional()});
const cursorSchema=z.strictObject({v:z.literal(communityPublicReadVersion),scope:z.literal(communityPublicScopeVersion),viewer:z.string().regex(/^[0-9a-f]{64}$/u),kind:z.enum(['POST','COMMENT']),direction:z.enum(['ASC','DESC']),boardId:board.nullable(),parentId:id.nullable(),createdAt:z.number().int().nonnegative().safe(),id});
type Row={id:string;account_id:string;revision:number;state_json:string};
type Source={content_id:string;account_id:string;kind:string;parent_id:string|null;created_at:number;command_id:string;action:string;next_revision:number;audit_created_at:string;response_json:string;details_json:string|null};
type PageRow=Pick<Source,'content_id'|'account_id'|'kind'|'parent_id'|'created_at'|'details_json'>&{state_json:string;revision:number};
/** Indexed admitted creation source only, not an own-history scan or legacy publication/backfill.
 * Current visibility and the requesting viewer's directional relations are evaluated on every request.
 * Active content and relationships retain their existing single writers. */
export class CommunityPublicReader{
 private readonly blocks:CommunityBlockStore;
 constructor(private readonly db:D1Database,private readonly authorize?:()=>Promise<void>){this.blocks=new CommunityBlockStore(db);}
 private row(contentId:string){return this.db.prepare('SELECT id,account_id,revision,state_json FROM community_contents WHERE id=?').bind(contentId).first<Row>();}
 private model(row:Row){try{const m=CommunityContent.restore(JSON.parse(row.state_json)),s=m.snapshot();if(s.id!==row.id||s.accountId!==row.account_id||s.revision!==row.revision)throw new Error();return m;}catch{throw new CommunityStoreError(503,'community_storage_invalid');}}
 private async publicState(viewer:string|null,contentId:string){
  const row=await this.row(contentId);if(!row)throw new CommunityStoreError(404,'community_content_unavailable');const model=this.model(row),state=model.snapshot();
  if(state.visibility!=='VISIBLE'||viewer&&await this.blocks.isBlocked(viewer,state.publicAuthorId))throw new CommunityStoreError(404,'community_content_unavailable');
  const identity=await this.db.prepare('SELECT public_author_id FROM community_public_authors WHERE account_id=?').bind(state.accountId).first<{public_author_id:string}>();if(identity?.public_author_id!==state.publicAuthorId)throw new CommunityStoreError(503,'community_public_author_source_unavailable');
  return {row,model,state};
 }
 private source(state:ReturnType<CommunityContent['snapshot']>){const post=state.kind==='POST',table=post?'community_post_create_audit':'community_content_create_audit';return this.db.prepare(`SELECT m.content_id,m.account_id,m.kind,m.parent_id,m.created_at,a.command_id,a.action,a.next_revision,a.created_at AS audit_created_at,r.response_json,${post?'d.details_json':'NULL AS details_json'}
   FROM community_content_creation m LEFT JOIN ${table} a ON a.content_id=m.content_id AND a.account_id=m.account_id
   LEFT JOIN community_content_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id
   ${post?'LEFT JOIN community_post_details d ON d.content_id=m.content_id':''} WHERE m.content_id=?`).bind(state.id).first<Source>();}
 async creationAnchor(state:ReturnType<CommunityContent['snapshot']>){
  const row=await this.source(state);if(!row)throw new CommunityStoreError(503,'community_creation_source_unavailable');
  try{const receipt=(state.kind==='POST'?communityPostCreateReceiptSchema:communityCommentCreateReceiptSchema).parse(JSON.parse(row.response_json));
   if(row.account_id!==state.accountId||row.kind!==state.kind||row.parent_id!==state.parentId||row.content_id!==state.id||row.action!==(state.kind==='POST'?'CREATE_POST':'CREATE_COMMENT')||row.next_revision!==1||row.command_id!==receipt.commandId||receipt.contentId!==state.id||receipt.parentId!==state.parentId||receipt.kind!==state.kind||receipt.createdAt!==row.created_at||!Number.isSafeInteger(row.created_at)||row.created_at<0||row.audit_created_at!==new Date(row.created_at).toISOString())throw new Error();
  }catch{throw new CommunityStoreError(503,'community_storage_invalid');}
  const details=state.kind==='POST'?await readCommunityPostDetails({response_json:row.response_json,details_json:row.details_json!,content_id:row.content_id,created_at:row.created_at}):null;
  return {row,details};
 }
 async publicationSourceGuards(state:ReturnType<CommunityContent['snapshot']>,expectedSource?:Source){
  const meta=await this.creationAnchor(state);if(expectedSource&&canonicalJson(meta.row)!==canonicalJson(expectedSource))throw new CommunityStoreError(409,'community_content_stale');const post=state.kind==='POST',table=post?'community_post_create_audit':'community_content_create_audit';
  const author=await this.db.prepare("SELECT a.id FROM accounts a JOIN community_public_authors p ON p.account_id=a.id WHERE a.id=? AND a.status='ACTIVE' AND p.public_author_id=?").bind(state.accountId,state.publicAuthorId).first();if(!author)throw new CommunityStoreError(409,'community_restore_ineligible');
  const sql=`EXISTS(SELECT 1 FROM community_content_creation m JOIN ${table} a ON a.content_id=m.content_id AND a.account_id=m.account_id JOIN community_content_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id ${post?'JOIN community_post_details d ON d.content_id=m.content_id':''} WHERE m.content_id=? AND m.account_id=? AND m.kind=? AND m.parent_id IS ? AND m.created_at=? AND a.command_id=? AND a.action=? AND a.next_revision=1 AND a.created_at=? AND r.response_json=? ${post?'AND d.details_json=?':''})`;
  return [this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN ${sql} THEN 1 ELSE 0 END`).bind(state.id,state.accountId,state.kind,state.parentId,meta.row.created_at,meta.row.command_id,meta.row.action,meta.row.audit_created_at,meta.row.response_json,...(post?[meta.row.details_json]:[])),this.db.prepare("INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts a JOIN community_public_authors p ON p.account_id=a.id WHERE a.id=? AND a.status='ACTIVE' AND p.public_author_id=?) THEN 1 ELSE 0 END").bind(state.accountId,state.publicAuthorId)];
 }
 async read(viewer:string|null,contentId:string){
  if(viewer!==null&&!id.safeParse(viewer).success||!id.safeParse(contentId).success)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorize?.();const initial=await this.publicState(viewer,contentId),meta=await this.creationAnchor(initial.state);
  const parent=initial.state.parentId?await this.publicState(viewer,initial.state.parentId):null;
  if(parent){if(parent.state.kind!=='POST')throw new CommunityStoreError(404,'community_content_unavailable');await this.creationAnchor(parent.state);}
  const projection=initial.model.read({accountId:null,blockedAuthors:[],parent:parent?.state??null});if(!projection)throw new CommunityStoreError(404,'community_content_unavailable');
  const updatedAt=await verifyCommunityRenderedContent(this.db,initial.state);
  await this.authorize?.();const current=await this.publicState(viewer,contentId),currentParent=parent?await this.publicState(viewer,parent.state.id):null;
  if(canonicalJson(current.row)!==canonicalJson(initial.row)||canonicalJson(currentParent?.row??null)!==canonicalJson(parent?.row??null)||canonicalJson(await this.source(initial.state))!==canonicalJson(meta.row))throw new CommunityStoreError(409,'community_content_stale');
  if(await verifyCommunityRenderedContent(this.db,current.state)!==updatedAt)throw new CommunityStoreError(409,'community_content_stale');
  const {kind,id:contentKey,parentId,publicAuthorId,revision,visibility,commentLocked,title,sanitizedBody}=projection;
  return {contractVersion:communityPublicReadVersion,publicScopeVersion:communityPublicScopeVersion,rendererVersion:communityMarkdownVersion,
   content:{kind,id:contentKey,parentId,publicAuthorId,revision,visibility,commentLocked,title,sanitizedBody,createdAt:meta.row.created_at,updatedAt,...meta.details}};
 }
 async listPosts(viewer:string|null,raw:unknown){return this.list(viewer,'POST',null,raw);}
 async listComments(viewer:string|null,parentId:string,raw:unknown){if(!id.safeParse(parentId).success)throw new CommunityStoreError(400,'community_command_invalid');return this.list(viewer,'COMMENT',parentId,raw);}
 private async list(viewer:string|null,kind:'POST'|'COMMENT',parentId:string|null,raw:unknown){
  const parsed=listQuery.safeParse(raw);if(!parsed.success||viewer!==null&&!id.safeParse(viewer).success||kind==='COMMENT'&&parsed.data.boardId!==undefined)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorize?.();const query=parsed.data,direction=kind==='POST'?'DESC':'ASC',viewerHash=await sha256Hex(canonicalJson({viewer}));let cursor:z.infer<typeof cursorSchema>|null=null;
  if(query.cursor){try{cursor=cursorSchema.parse(decodeCommunityCursor(query.cursor));if(cursor.viewer!==viewerHash||cursor.kind!==kind||cursor.direction!==direction||cursor.boardId!==(query.boardId??null)||cursor.parentId!==parentId)throw new Error();}catch{throw new CommunityStoreError(400,'community_cursor_invalid');}}
  const parent=parentId?await this.publicState(viewer,parentId):null;if(parent){if(parent.state.kind!=='POST')throw new CommunityStoreError(404,'community_content_unavailable');await this.creationAnchor(parent.state);}
  const values:(string|number)[]=[kind];if(parentId)values.push(parentId);if(viewer)values.push(viewer);if(query.boardId)values.push(query.boardId);if(cursor)values.push(cursor.createdAt,cursor.createdAt,cursor.id);values.push(query.limit+1);
  const statement=this.db.prepare(`SELECT m.content_id,m.account_id,m.kind,m.parent_id,m.created_at,c.state_json,c.revision,d.details_json,e.created_at AS edit_created_at,mp.created_at AS moderation_created_at,clp.created_at AS comment_lock_created_at
   FROM community_content_creation m JOIN community_contents c ON c.id=m.content_id LEFT JOIN community_post_details d ON d.content_id=c.id
   LEFT JOIN community_content_edit_audit e ON e.content_id=c.id AND e.account_id=c.account_id AND e.next_revision=c.revision LEFT JOIN community_content_moderation_proof mp ON mp.content_id=c.id AND mp.next_revision=c.revision LEFT JOIN community_content_comment_lock_proof clp ON clp.content_id=c.id AND clp.next_revision=c.revision
   WHERE m.kind=? AND json_extract(c.state_json,'$.visibility')='VISIBLE'
   ${parentId?'AND m.parent_id=?':''}
   ${viewer?"AND NOT EXISTS(SELECT 1 FROM community_blocks b WHERE b.account_id=? AND b.public_author_id=json_extract(c.state_json,'$.publicAuthorId'))":''}
   ${query.boardId?"AND json_extract(d.details_json,'$.boardId')=?":''}
   ${cursor?`AND (m.created_at${direction==='DESC'?'<':'>'}? OR (m.created_at=? AND m.content_id COLLATE BINARY${direction==='DESC'?'<':'>'}?))`:''}
   ORDER BY m.created_at ${direction},m.content_id COLLATE BINARY ${direction} LIMIT ?`).bind(...values);
  const rows=await statement.all<PageRow>();if(!rows.success)throw new CommunityStoreError(503,'community_storage_unavailable');const items=[];
  for(const row of rows.results.slice(0,query.limit)){const r=await this.read(viewer,row.content_id);if(r.content.kind!==kind||r.content.parentId!==parentId||r.content.createdAt!==row.created_at||query.boardId&&('boardId' in r.content?r.content.boardId:null)!==query.boardId)throw new CommunityStoreError(503,'community_storage_invalid');items.push({...r.content,rendererVersion:r.rendererVersion});}
  await this.authorize?.();if(parent){const now=await this.publicState(viewer,parent.state.id);if(canonicalJson(now.row)!==canonicalJson(parent.row))throw new CommunityStoreError(409,'community_content_stale');}
  const current=await statement.all<PageRow>();if(!current.success)throw new CommunityStoreError(503,'community_storage_unavailable');if(canonicalJson(current.results)!==canonicalJson(rows.results))throw new CommunityStoreError(409,'community_content_stale');
  const last=items.at(-1),nextCursor=rows.results.length>query.limit&&last?encodeCommunityCursor({v:communityPublicReadVersion,scope:communityPublicScopeVersion,viewer:viewerHash,kind,direction,boardId:query.boardId??null,parentId,createdAt:last.createdAt,id:last.id}):null;
  return {contractVersion:communityPublicReadVersion,publicScopeVersion:communityPublicScopeVersion,items,nextCursor};
 }
}