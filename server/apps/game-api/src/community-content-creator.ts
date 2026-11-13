import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ulid} from 'ulid';
import {z} from 'zod';
import {CommunityContent,CommunityContentError} from '../../../packages/common/src/community-content';
import {CommunityStoreError} from './community-content-store';
import type {CommunityCreationConsent} from './community-consent';
import {renderCommunityMarkdown,communityMarkdownVersion,CommunityMarkdownError} from './community-markdown';
import {evaluateCommunityCreationRate,communityCreationLimits} from './community-creation-rate';
import {communityPostCreateRequestSchema,communityPostCreateReceiptSchema,communityPostCreateVersion,communityPostDetails,type CommunityPostCreateRequest} from './community-post-contract';
export const communityCommentCreateVersion='community.comment-create.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
export const communityCommentCreateRequestSchema=z.strictObject({commandId:id,markdown:z.string().min(1).max(2000)});
const commentCommand=communityCommentCreateRequestSchema.extend({parentId:id});
export const communityCommentCreateReceiptSchema=z.strictObject({contractVersion:z.literal(communityCommentCreateVersion),rendererVersion:z.literal(communityMarkdownVersion),commandId:id,contentId:id,parentId:id,kind:z.literal('COMMENT'),revision:z.literal(1),visibility:z.enum(['VISIBLE','REVIEW_PENDING']),createdAt:z.number().int().nonnegative().safe()});
export class CommunityRateError extends Error{readonly status=429;readonly code='community_rate_limited';constructor(readonly retryAfter:number){super('community_rate_limited');}}
type Row={account_id:string;revision:number;state_json:string};
type Kind='POST'|'COMMENT';
/** One creation transaction owner. Existing domain/renderer/WEB boundaries supply rules and rights. */
export class CommunityContentCreator{
 constructor(private readonly db:D1Database,private readonly now:()=>number=()=>Date.now(),private readonly authorize?:()=>Promise<D1PreparedStatement[]>,private readonly reviewPolicy?:()=>Promise<'NONE'|'REVIEW_PENDING'>,private readonly postReferences?:(post:CommunityPostCreateRequest)=>Promise<D1PreparedStatement[]>,private readonly creationConsent:(accountId:string)=>Promise<CommunityCreationConsent>=async()=>{throw new CommunityStoreError(503,'community_policy_source_unavailable');}){}
 private row(id:string){return this.db.prepare('SELECT account_id,revision,state_json FROM community_contents WHERE id=?').bind(id).first<Row>();}
 private model(id:string,row:Row){try{const m=CommunityContent.restore(JSON.parse(row.state_json)),s=m.snapshot();if(s.id!==id||s.accountId!==row.account_id||s.revision!==row.revision)throw new Error();return m;}catch{throw new CommunityStoreError(503,'community_storage_invalid');}}
 private async rate(accountId:string,kind:Kind,at:number){const r=await this.db.prepare('SELECT created_at FROM community_content_creation WHERE account_id=? AND kind=? AND created_at>? ORDER BY created_at ASC').bind(accountId,kind,at-3600000).all<{created_at:number}>();if(!r.success)throw new CommunityStoreError(503,'community_storage_unavailable');try{const limited=evaluateCommunityCreationRate(kind,r.results.map(row=>row.created_at),at);if(limited)throw new CommunityRateError(limited.retryAfter);}catch(e){if(e instanceof CommunityRateError)throw e;throw new CommunityStoreError(503,'community_rate_source_invalid');}}
 async create(accountId:string,raw:unknown){return communityCommentCreateReceiptSchema.parse(await this.createContent('COMMENT',accountId,raw));}
 async createPost(accountId:string,raw:unknown){return communityPostCreateReceiptSchema.parse(await this.createContent('POST',accountId,raw));}
 private async createContent(kind:Kind,accountId:string,raw:unknown){
  const parsed=(kind==='POST'?communityPostCreateRequestSchema:commentCommand).safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  const cmd=parsed.data,post=kind==='POST'?communityPostCreateRequestSchema.parse(cmd):null,parentId='parentId' in cmd?cmd.parentId:null;
  const action=kind==='POST'?'CREATE_POST':'CREATE_COMMENT',auditTable=kind==='POST'?'community_post_create_audit':'community_content_create_audit';
  const receiptSchema=kind==='POST'?communityPostCreateReceiptSchema:communityCommentCreateReceiptSchema;
  const hash=await sha256Hex(canonicalJson({...cmd,operation:action,rendererVersion:communityMarkdownVersion}));
  const replay=async()=>{
   const stored=await this.db.prepare('SELECT request_hash,response_json FROM community_content_receipts WHERE account_id=? AND command_id=?').bind(accountId,cmd.commandId).first<{request_hash:string;response_json:string}>();if(!stored)return null;if(stored.request_hash!==hash)throw new CommunityStoreError(409,'community_request_conflict');
   try{const r=receiptSchema.parse(JSON.parse(stored.response_json));const audit=await this.db.prepare(`SELECT content_id,action,next_revision,next_state_hash FROM ${auditTable} WHERE account_id=? AND command_id=?`).bind(accountId,cmd.commandId).first<{content_id:string;action:string;next_revision:number;next_state_hash:string}>(),meta=await this.db.prepare('SELECT account_id,kind,parent_id,created_at FROM community_content_creation WHERE content_id=?').bind(r.contentId).first<{account_id:string;kind:string;parent_id:string|null;created_at:number}>(),row=await this.row(r.contentId);
    if(r.commandId!==cmd.commandId||r.kind!==kind||r.parentId!==parentId||!audit||audit.content_id!==r.contentId||audit.action!==action||audit.next_revision!==1||!/^[0-9a-f]{64}$/u.test(audit.next_state_hash)||!meta||meta.account_id!==accountId||meta.kind!==kind||meta.parent_id!==parentId||meta.created_at!==r.createdAt||!row||row.account_id!==accountId)throw new Error();
    const state=this.model(r.contentId,row).snapshot();if(state.kind!==kind||state.parentId!==parentId||state.revision<1||state.revision===1&&await sha256Hex(canonicalJson(state))!==audit.next_state_hash)throw new Error();
    if(post){const details=await this.db.prepare('SELECT details_json FROM community_post_details WHERE content_id=?').bind(r.contentId).first<{details_json:string}>();if(!details||!('detailsHash' in r)||r.boardId!==post.boardId||await sha256Hex(details.details_json)!==r.detailsHash||details.details_json!==canonicalJson(communityPostDetails(post)))throw new Error();}return r;
   }catch{throw new CommunityStoreError(503,'community_storage_invalid');}
  };
  const rights=await this.authorize?.()??[],original=await replay();if(original)return original;
  const consent=await this.creationConsent(accountId);
  const parent=parentId?await this.row(parentId):null;
  if(kind==='COMMENT'){if(!parent||!parentId)throw new CommunityStoreError(404,'community_parent_unavailable');try{this.model(parentId,parent).requireCommentCreation();}catch(e){if(e instanceof CommunityContentError)throw new CommunityStoreError(e.message==='community_parent_unavailable'?404:409,e.message);throw e;}}
  let referenceGuards:D1PreparedStatement[]=[];if(post){if(this.postReferences)referenceGuards=await this.postReferences(post);else if(post.wikiRefs.length||post.sourceBalanceVersion!==null)throw new CommunityStoreError(503,'community_wiki_source_unavailable');}
  let visibility:'VISIBLE'|'REVIEW_PENDING'='VISIBLE';if(this.reviewPolicy){try{const review=await this.reviewPolicy();if(review==='REVIEW_PENDING')visibility='REVIEW_PENDING';else if(review!=='NONE')throw new Error();}catch{throw new CommunityStoreError(503,'community_review_policy_unavailable');}}
  let rendered:ReturnType<typeof renderCommunityMarkdown>;try{rendered=renderCommunityMarkdown(kind,cmd.markdown);}catch(e){if(e instanceof CommunityMarkdownError)throw new CommunityStoreError(400,e.message);throw e;}
  const at=this.now();await this.rate(accountId,kind,at);
  const existing=await this.db.prepare('SELECT public_author_id FROM community_public_authors WHERE account_id=?').bind(accountId).first<{public_author_id:string}>(),author=existing?.public_author_id??'author_'+ulid(),contentId=ulid();
  let state:ReturnType<CommunityContent['snapshot']>;try{state=CommunityContent.restore({kind,id:contentId,parentId,accountId,publicAuthorId:author,visibility,commentLocked:false,revision:1,title:post?.title??null,sanitizedBody:rendered.sanitizedBody}).snapshot();}catch{throw new CommunityStoreError(400,'community_command_invalid');}
  const json=canonicalJson(state),detailsJson=post?canonicalJson(communityPostDetails(post)):null;
  const receipt=receiptSchema.parse({contractVersion:post?communityPostCreateVersion:communityCommentCreateVersion,rendererVersion:communityMarkdownVersion,commandId:cmd.commandId,contentId,parentId,kind,revision:1,visibility,createdAt:at,...(post?{boardId:post.boardId,detailsHash:await sha256Hex(detailsJson!)}:{})});
  const freshRights=await this.authorize?.()??rights,limits=communityCreationLimits[kind];
  await consent.assertCurrent();
  try{await this.db.batch([...freshRights,...consent.guards,...referenceGuards,
   ...(parent&&parentId?[this.db.prepare('INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_contents WHERE id=? AND account_id=? AND revision=? AND state_json=?) THEN 1 ELSE 0 END').bind(parentId,parent.account_id,parent.revision,parent.state_json)]:[]),
   this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN
    (SELECT COUNT(*) FROM community_content_creation WHERE account_id=? AND kind=? AND created_at>?)<?
    AND NOT EXISTS(SELECT 1 FROM community_content_creation WHERE account_id=? AND kind=? AND created_at>?) THEN 1 ELSE 0 END`).bind(accountId,kind,at-3600000,limits.hourly,accountId,kind,at-limits.minimumInterval),
   this.db.prepare('INSERT INTO community_public_authors(account_id,public_author_id) SELECT ?,? WHERE NOT EXISTS(SELECT 1 FROM community_public_authors WHERE account_id=?)').bind(accountId,author,accountId),
   this.db.prepare('INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_public_authors WHERE account_id=? AND public_author_id=?) THEN 1 ELSE 0 END').bind(accountId,author),
   this.db.prepare('INSERT INTO community_contents(id,account_id,revision,state_json) VALUES(?,?,1,?)').bind(contentId,accountId,json),
   this.db.prepare('INSERT INTO community_content_creation(content_id,account_id,kind,parent_id,created_at) VALUES(?,?,?,?,?)').bind(contentId,accountId,kind,parentId,at),
   ...(detailsJson?[this.db.prepare('INSERT INTO community_post_details(content_id,details_json) VALUES(?,?)').bind(contentId,detailsJson)]:[]),
   this.db.prepare('INSERT INTO community_content_receipts(account_id,command_id,content_id,request_hash,response_json) VALUES(?,?,?,?,?)').bind(accountId,cmd.commandId,contentId,hash,canonicalJson(receipt)),
   this.db.prepare(`INSERT INTO ${auditTable}(account_id,command_id,content_id,action,next_revision,next_state_hash,created_at,retain_until,retention_policy_version) VALUES(?,?,?,?,1,?,?,?,'community-retention-v3-20260930')`).bind(accountId,cmd.commandId,contentId,action,await sha256Hex(json),new Date(at).toISOString(),new Date(at+180*86400000).toISOString()),
   this.db.prepare('DELETE FROM community_content_write_guard')]);
  }catch{await this.authorize?.();const result=await replay();if(result)return result;await consent.assertCurrent();if(parent&&parentId){const latest=await this.row(parentId);if(!latest||latest.state_json!==parent.state_json||latest.revision!==parent.revision||latest.account_id!==parent.account_id)throw new CommunityStoreError(409,'community_revision_conflict');}await this.rate(accountId,kind,this.now());throw new CommunityStoreError(503,'community_storage_unavailable');}
  return receipt;
 }
}