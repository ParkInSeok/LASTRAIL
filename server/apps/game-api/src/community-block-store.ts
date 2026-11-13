import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {CommunityStoreError} from './community-content-store';
export const communityBlockVersion='community.personal-block.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),revision=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-1);
export const communityBlockCommandSchema=z.strictObject({commandId:id,publicAuthorId:id,blocked:z.boolean(),expectedRevision:revision});
const receiptSchema=communityBlockCommandSchema.omit({expectedRevision:true}).extend({contractVersion:z.literal(communityBlockVersion),revision});
const cursorSchema=z.strictObject({v:z.literal(communityBlockVersion),owner:z.string().regex(/^[a-f0-9]{64}$/u),revision,after:id});
/** Sole owner of personal relations and their actor revision. Receipts are historical metadata,
 * never commands to restore old relationships. Public readers consume isBlocked independently.
 * Does not change content, reciprocal rights, community sanctions, or any game state. */
export class CommunityBlockStore {
 constructor(private readonly db:D1Database,private readonly authorize?:()=>Promise<D1PreparedStatement[]>,private readonly authorizeRead?:()=>Promise<void>){}
 private async revision(accountId:string){const row=await this.db.prepare('SELECT revision FROM community_block_state WHERE account_id=?').bind(accountId).first<{revision:number}>();if(row&&!revision.safeParse(row.revision).success)throw new CommunityStoreError(503,'community_storage_invalid');return row?.revision??0;}
 async isBlocked(accountId:string,publicAuthorId:string){if(!id.safeParse(accountId).success||!id.safeParse(publicAuthorId).success)throw new CommunityStoreError(400,'community_command_invalid');return !!await this.db.prepare('SELECT 1 FROM community_blocks WHERE account_id=? AND public_author_id=?').bind(accountId,publicAuthorId).first();}
 async change(accountId:string,raw:unknown){
  const parsed=communityBlockCommandSchema.safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  const command=parsed.data,hash=await sha256Hex(canonicalJson({version:communityBlockVersion,...command}));
  const replay=async()=>{const row=await this.db.prepare('SELECT request_hash,response_json FROM community_block_receipts WHERE account_id=? AND command_id=?').bind(accountId,command.commandId).first<{request_hash:string;response_json:string}>();if(!row)return null;if(row.request_hash!==hash)throw new CommunityStoreError(409,'community_request_conflict');try{const receipt=receiptSchema.parse(JSON.parse(row.response_json));if(receipt.commandId!==command.commandId||receipt.publicAuthorId!==command.publicAuthorId||receipt.blocked!==command.blocked||receipt.revision<command.expectedRevision||receipt.revision>command.expectedRevision+1)throw new Error();return receipt;}catch{throw new CommunityStoreError(503,'community_storage_invalid');}};
  await this.authorize?.();const original=await replay();if(original)return original;
  const target=await this.db.prepare('SELECT account_id FROM community_public_authors WHERE public_author_id=?').bind(command.publicAuthorId).first<{account_id:string}>();
  if(target?.account_id===accountId)throw new CommunityStoreError(403,'community_self_block_forbidden');
  const wasBlocked=await this.isBlocked(accountId,command.publicAuthorId);
  // Retired public identities can be removed from an existing relation; new blocks require a real public identity.
  if(!target&&(command.blocked||!wasBlocked))throw new CommunityStoreError(404,'community_author_unavailable');
  if(await this.revision(accountId)!==command.expectedRevision)throw new CommunityStoreError(409,'community_block_revision_conflict');
  const next=command.expectedRevision+(wasBlocked===command.blocked?0:1),receipt=receiptSchema.parse({contractVersion:communityBlockVersion,commandId:command.commandId,publicAuthorId:command.publicAuthorId,blocked:command.blocked,revision:next});
  const rights=await this.authorize?.()??[];
  try{await this.db.batch([...rights,
   this.db.prepare('INSERT OR IGNORE INTO community_block_state(account_id,revision) VALUES(?,0)').bind(accountId),
   this.db.prepare('INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_block_state WHERE account_id=? AND revision=?) THEN 1 ELSE 0 END').bind(accountId,command.expectedRevision),
   ...(target?[this.db.prepare('INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_public_authors WHERE public_author_id=? AND account_id=? AND account_id<>?) THEN 1 ELSE 0 END').bind(command.publicAuthorId,target.account_id,accountId)]:[]),
   this.db.prepare('INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_blocks WHERE account_id=? AND public_author_id=?)=? THEN 1 ELSE 0 END').bind(accountId,command.publicAuthorId,wasBlocked?1:0),
   command.blocked?this.db.prepare('INSERT OR IGNORE INTO community_blocks(account_id,public_author_id) VALUES(?,?)').bind(accountId,command.publicAuthorId):this.db.prepare('DELETE FROM community_blocks WHERE account_id=? AND public_author_id=?').bind(accountId,command.publicAuthorId),
   this.db.prepare('UPDATE community_block_state SET revision=? WHERE account_id=? AND revision=?').bind(next,accountId,command.expectedRevision),
   this.db.prepare('INSERT INTO community_block_receipts(account_id,command_id,request_hash,response_json) VALUES(?,?,?,?)').bind(accountId,command.commandId,hash,canonicalJson(receipt)),
   this.db.prepare('DELETE FROM community_content_write_guard')]);
  }catch{await this.authorize?.();const result=await replay();if(result)return result;if(await this.revision(accountId)!==command.expectedRevision)throw new CommunityStoreError(409,'community_block_revision_conflict');throw new CommunityStoreError(503,'community_storage_unavailable');}
  return receipt;
 }
 async list(accountId:string,raw:unknown){
  const parsed=z.strictObject({limit:z.number().int().min(1).max(50).default(20),cursor:z.string().min(1).max(1024).nullable().optional()}).safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorizeRead?.();const current=await this.revision(accountId),owner=await sha256Hex(accountId);let after='';
  if(parsed.data.cursor){let cursor:z.infer<typeof cursorSchema>;try{if(!/^[A-Za-z0-9_-]+$/u.test(parsed.data.cursor))throw new Error();cursor=cursorSchema.parse(JSON.parse(atob(parsed.data.cursor.replaceAll('-','+').replaceAll('_','/'))));if(cursor.owner!==owner)throw new Error();}catch{throw new CommunityStoreError(400,'community_cursor_invalid');}if(cursor.revision!==current)throw new CommunityStoreError(409,'community_block_revision_conflict');after=cursor.after;}
  const rows=await this.db.prepare('SELECT public_author_id FROM community_blocks WHERE account_id=? AND public_author_id COLLATE BINARY>? ORDER BY public_author_id COLLATE BINARY ASC LIMIT ?').bind(accountId,after,parsed.data.limit+1).all<{public_author_id:string}>();if(!rows.success)throw new CommunityStoreError(503,'community_storage_unavailable');if(rows.results.some(row=>!id.safeParse(row.public_author_id).success))throw new CommunityStoreError(503,'community_storage_invalid');
  await this.authorizeRead?.();if(await this.revision(accountId)!==current)throw new CommunityStoreError(409,'community_block_revision_conflict');
  const items=rows.results.slice(0,parsed.data.limit).map(row=>({publicAuthorId:row.public_author_id}));
  return {contractVersion:communityBlockVersion,revision:current,items,nextCursor:rows.results.length>parsed.data.limit?btoa(JSON.stringify({v:communityBlockVersion,owner,revision:current,after:items.at(-1)!.publicAuthorId})).replaceAll('+','-').replaceAll('/','_').replaceAll('=',''):null};
 }
}