import { z } from 'zod';
import { CommunityContent } from '../../../packages/common/src/community-content';
import { CommunityStoreError } from './community-content-store';
import {verifyCommunityRenderedContent} from './community-render-proof';
import { communityMarkdownVersion } from './community-markdown';
export const communityOwnReadVersion='community.own-content-read.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
type Row={account_id:string;revision:number;state_json:string};
/** Read-only projection. Active state and author mutations remain owned by CommunityContentStore. */
export class CommunityOwnContentReader {
 constructor(private readonly db:D1Database,private readonly authorize?:()=>Promise<void>){}
 private row(contentId:string){return this.db.prepare('SELECT account_id,revision,state_json FROM community_contents WHERE id=?').bind(contentId).first<Row>();}
 private model(contentId:string,row:Row){
  try{const model=CommunityContent.restore(JSON.parse(row.state_json)),state=model.snapshot();
   if(state.id!==contentId||state.accountId!==row.account_id||state.revision!==row.revision)throw new Error();return model;
  }catch{throw new CommunityStoreError(503,'community_storage_invalid');}
 }
 async read(accountId:string,contentId:string){
  if(!id.safeParse(accountId).success||!id.safeParse(contentId).success)throw new CommunityStoreError(400,'community_command_invalid');
  await this.authorize?.();
  const row=await this.row(contentId);
  if(!row||row.account_id!==accountId)throw new CommunityStoreError(404,'community_content_unavailable');
  const model=this.model(contentId,row),state=model.snapshot();
  const parentRow=state.parentId?await this.row(state.parentId):null;
  const parent=state.parentId&&parentRow?this.model(state.parentId,parentRow).snapshot():null;
  const projection=model.read({accountId,blockedAuthors:[],parent});
  if(!projection)throw new CommunityStoreError(404,'community_content_unavailable');
  const updatedAt=await verifyCommunityRenderedContent(this.db,state);
  await this.authorize?.();
  const current=await this.row(contentId),currentParent=state.parentId?await this.row(state.parentId):null;
  const equal=(a:Row|null,b:Row|null)=>a===null?b===null:!!b&&a.account_id===b.account_id&&a.revision===b.revision&&a.state_json===b.state_json;
  if(!equal(row,current)||!equal(parentRow,currentParent))throw new CommunityStoreError(409,'community_content_stale');
  if(await verifyCommunityRenderedContent(this.db,state)!==updatedAt)throw new CommunityStoreError(409,'community_content_stale');
  const {kind,id:contentKey,parentId,revision,visibility,commentLocked,title,sanitizedBody}=projection;
  return {contractVersion:communityOwnReadVersion,rendererVersion:visibility==='VISIBLE'?communityMarkdownVersion:null,
   content:{kind,id:contentKey,parentId,revision,visibility,commentLocked,title,sanitizedBody,updatedAt}};
 }
}