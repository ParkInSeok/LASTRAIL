import { canonicalJson,sha256Hex } from '@lastrail/common';
import { CommunityContent,CommunityContentError } from '../../../packages/common/src/community-content';
import { z } from 'zod';
import { renderCommunityMarkdown,communityMarkdownVersion,CommunityMarkdownError } from './community-markdown';
export class CommunityStoreError extends Error {
 constructor(readonly status:400|401|403|404|409|503,readonly code:string){super(code);}
}
export const communityContentStoreVersion='community.content-store.v1.20261008' as const;
export const communityContentEditVersion='community.content-edit.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
export const communityDeleteRequestSchema=z.strictObject({commandId:id,expectedRevision:z.number().int().min(1).max(Number.MAX_SAFE_INTEGER-1)});
export const communityEditRequestSchema=communityDeleteRequestSchema.extend({title:z.string().max(200).nullable(),markdown:z.string().min(1).max(20000)});
const deleteCommandSchema=communityDeleteRequestSchema.extend({contentId:id});
const editCommandSchema=communityEditRequestSchema.extend({contentId:id});
const deleteReceiptSchema=z.strictObject({contractVersion:z.literal(communityContentStoreVersion),commandId:id,contentId:id,revision:z.number().int().min(2).safe(),visibility:z.literal('AUTHOR_DELETED')});
export const communityEditReceiptSchema=z.strictObject({contractVersion:z.literal(communityContentEditVersion),rendererVersion:z.literal(communityMarkdownVersion),commandId:id,contentId:id,revision:z.number().int().min(2).safe(),visibility:z.enum(['VISIBLE','HIDDEN','REVIEW_PENDING'])});
type Row={account_id:string;revision:number;state_json:string};
type Audit={content_id:string;previous_revision:number;next_revision:number;action:string;next_state_hash:string|null};
/** One active-content/CAS/receipt owner shared by author commands. State rules remain in CommunityContent.
 * Routes supply authenticated actors and authorization assertions; raw request HTML is never a trusted rendered body.
 * Receipt/audit history contains metadata/hashes only. Purge/search/cache/backup remain separate owners. */
export class CommunityContentStore {
 constructor(private readonly db:D1Database,private readonly now:()=>Date=()=>new Date(),private readonly authorization?:()=>Promise<D1PreparedStatement[]>) {}
 async deleteOwned(accountId:string,raw:unknown){return deleteReceiptSchema.parse(await this.changeOwned('DELETE',accountId,raw));}
 async editOwned(accountId:string,raw:unknown){return communityEditReceiptSchema.parse(await this.changeOwned('EDIT',accountId,raw));}
 private async changeOwned(kind:'DELETE'|'EDIT',accountId:string,raw:unknown){
  const parsed=kind==='DELETE'?deleteCommandSchema.safeParse(raw):editCommandSchema.safeParse(raw);
  if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');
  const command=parsed.data;
  // Preserve v1 deletion hashes. New edit keys are distinguished by operation/version and exact raw Markdown.
  const hash=await sha256Hex(canonicalJson(kind==='DELETE'?command:{...command,operation:'AUTHOR_EDIT',rendererVersion:communityMarkdownVersion}));
  const auditTable=kind==='DELETE'?'community_content_audit':'community_content_edit_audit',action=kind==='DELETE'?'AUTHOR_DELETE':'AUTHOR_EDIT';
  const readModel=(row:Row)=>{try{const model=CommunityContent.restore(JSON.parse(row.state_json)),state=model.snapshot();if(state.accountId!==row.account_id||state.id!==command.contentId||state.revision!==row.revision)throw new Error();return model;}catch{throw new CommunityStoreError(503,'community_storage_invalid');}};
  const replay=async()=>{
   const stored=await this.db.prepare('SELECT request_hash,response_json FROM community_content_receipts WHERE account_id=? AND command_id=?').bind(accountId,command.commandId).first<{request_hash:string;response_json:string}>();
   if(!stored)return null;
   if(stored.request_hash!==hash)throw new CommunityStoreError(409,'community_request_conflict');
   try{
    const receipt=(kind==='DELETE'?deleteReceiptSchema:communityEditReceiptSchema).parse(JSON.parse(stored.response_json));
    const audit=await this.db.prepare(`SELECT content_id,previous_revision,next_revision,action,${kind==='DELETE'?'NULL AS next_state_hash':'next_state_hash'} FROM ${auditTable} WHERE account_id=? AND command_id=?`).bind(accountId,command.commandId).first<Audit>();
    if(receipt.commandId!==command.commandId||receipt.contentId!==command.contentId||receipt.revision!==command.expectedRevision+1||!audit||audit.content_id!==command.contentId||audit.previous_revision!==command.expectedRevision||audit.next_revision!==receipt.revision||audit.action!==action)throw new Error();
    const current=await this.db.prepare('SELECT account_id,revision,state_json FROM community_contents WHERE id=?').bind(command.contentId).first<Row>();
    if(!current||current.account_id!==accountId)throw new Error();
    const state=readModel(current).snapshot();
    if(kind==='DELETE'){
     if(state.revision!==receipt.revision||state.visibility!=='AUTHOR_DELETED'||state.title!==null||state.sanitizedBody!==null)throw new Error();
    }else{
     if(!audit.next_state_hash||!/^[0-9a-f]{64}$/u.test(audit.next_state_hash)||state.revision<receipt.revision)throw new Error();
     // Later legitimate edit/hide/delete never re-applies this historical body or visibility.
     if(state.revision===receipt.revision&&await sha256Hex(canonicalJson(state))!==audit.next_state_hash)throw new Error();
    }
    return receipt;
   }catch{throw new CommunityStoreError(503,'community_storage_invalid');}
  };
  const authorization=await this.authorization?.()??[],original=await replay();if(original)return original;
  const row=await this.db.prepare('SELECT account_id,revision,state_json FROM community_contents WHERE id=?').bind(command.contentId).first<Row>();
  if(!row)throw new CommunityStoreError(404,'community_content_unavailable');
  const model=readModel(row);
  if(row.account_id!==accountId){if(model.snapshot().visibility!=='VISIBLE')throw new CommunityStoreError(404,'community_content_unavailable');throw new CommunityStoreError(403,'community_forbidden');}
  try{
   if(kind==='EDIT'){const edit=editCommandSchema.parse(command),rendered=renderCommunityMarkdown(model.snapshot().kind,edit.markdown);model.edit(accountId,edit.expectedRevision,{title:edit.title,sanitizedBody:rendered.sanitizedBody});}
   else model.delete(accountId,command.expectedRevision);
  }catch(error){
   if(error instanceof CommunityMarkdownError)throw new CommunityStoreError(400,error.message);
   if(error instanceof CommunityContentError)throw new CommunityStoreError(error.message==='community_content_invalid'?400:error.message==='community_forbidden'?403:409,error.message);
   throw error;
  }
  const next=model.snapshot(),receipt=kind==='DELETE'?deleteReceiptSchema.parse({contractVersion:communityContentStoreVersion,commandId:command.commandId,contentId:command.contentId,revision:next.revision,visibility:next.visibility}):communityEditReceiptSchema.parse({contractVersion:communityContentEditVersion,rendererVersion:communityMarkdownVersion,commandId:command.commandId,contentId:command.contentId,revision:next.revision,visibility:next.visibility});
  const at=this.now(),retainUntil=new Date(at.getTime()+180*86400000).toISOString(),stateJson=canonicalJson(next);
  const audit=kind==='DELETE'?this.db.prepare(`INSERT INTO community_content_audit(account_id,command_id,content_id,action,previous_revision,next_revision,created_at,retain_until,retention_policy_version) VALUES(?,?,?,'AUTHOR_DELETE',?,?,?,?,?)`).bind(accountId,command.commandId,command.contentId,row.revision,receipt.revision,at.toISOString(),retainUntil,'community-retention-v3-20260930'):
   this.db.prepare(`INSERT INTO community_content_edit_audit(account_id,command_id,content_id,action,previous_revision,next_revision,next_state_hash,created_at,retain_until,retention_policy_version) VALUES(?,?,?,'AUTHOR_EDIT',?,?,?,?,?,?)`).bind(accountId,command.commandId,command.contentId,row.revision,receipt.revision,await sha256Hex(stateJson),at.toISOString(),retainUntil,'community-retention-v3-20260930');
  try{
   await this.db.batch([
    ...authorization,
    this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_contents WHERE id=? AND account_id=? AND revision=? AND state_json=?) THEN 1 ELSE 0 END`).bind(command.contentId,accountId,row.revision,row.state_json),
    this.db.prepare('UPDATE community_contents SET revision=?,state_json=? WHERE id=? AND account_id=? AND revision=? AND state_json=?').bind(receipt.revision,stateJson,command.contentId,accountId,row.revision,row.state_json),
    this.db.prepare('INSERT INTO community_content_receipts(account_id,command_id,content_id,request_hash,response_json) VALUES(?,?,?,?,?)').bind(accountId,command.commandId,command.contentId,hash,canonicalJson(receipt)),
    audit,this.db.prepare('DELETE FROM community_content_write_guard'),
   ]);
  }catch{
   await this.authorization?.();const concurrent=await replay();if(concurrent)return concurrent;
   const latest=await this.db.prepare('SELECT revision,state_json FROM community_contents WHERE id=?').bind(command.contentId).first<{revision:number;state_json:string}>();
   if(!latest||latest.revision!==row.revision||latest.state_json!==row.state_json)throw new CommunityStoreError(409,'community_revision_conflict');
   throw new CommunityStoreError(503,'community_storage_unavailable');
  }
  return receipt;
 }
}



