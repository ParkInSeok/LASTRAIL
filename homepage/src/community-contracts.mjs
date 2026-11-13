import {requireInput,requireResponse} from './api-http.mjs';
export const deleteVersion='community.content-store.v1.20261008';
export const editVersion='community.content-edit.v1.20261008';
export const rendererVersion='community.markdown.v1.20261008';
const id=v=>typeof v==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(v);
export function communityCommand(input,operation){
 const keys=operation==='edit'?['contentId','commandId','expectedRevision','title','markdown']:['contentId','commandId','expectedRevision'];
 requireInput(input&&typeof input==='object'&&!Array.isArray(input)&&Object.keys(input).length===keys.length&&Object.keys(input).every(k=>keys.includes(k))&&id(input.contentId)&&id(input.commandId)&&Number.isSafeInteger(input.expectedRevision)&&input.expectedRevision>=1&&input.expectedRevision<Number.MAX_SAFE_INTEGER);
 if(operation==='edit'){
  requireInput(input.title===null||typeof input.title==='string'&&input.title.trim().length>0&&[...input.title.trim()].length<=100);
  requireInput(typeof input.markdown==='string'&&input.markdown.trim().length>0&&[...input.markdown].length<=(input.title===null?1000:10000));
 }
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,input[k]])));
}
export function communityReceipt(v,command,operation){
 const edit=operation==='edit',keys=edit?['contractVersion','rendererVersion','commandId','contentId','revision','visibility']:['contractVersion','commandId','contentId','revision','visibility'];
 requireResponse(v&&typeof v==='object'&&!Array.isArray(v)&&Object.keys(v).length===keys.length&&Object.keys(v).every(k=>keys.includes(k))&&v.contractVersion===(edit?editVersion:deleteVersion)&&v.commandId===command.commandId&&v.contentId===command.contentId&&v.revision===command.expectedRevision+1&&(edit?['VISIBLE','HIDDEN','REVIEW_PENDING'].includes(v.visibility)&&v.rendererVersion===rendererVersion:v.visibility==='AUTHOR_DELETED'));
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,v[k]])));
}

export const commentVersion='community.comment-create.v1.20261008';
export const ownReadVersion='community.own-content-read.v1.20261008';
export const ownCommentsVersion='community.own-comments-list.v1.20261008';
export function commentCommand(input){
 const keys=['postId','commandId','markdown'];
 requireInput(input&&typeof input==='object'&&!Array.isArray(input)&&Object.keys(input).length===3&&Object.keys(input).every(k=>keys.includes(k))&&id(input.postId)&&id(input.commandId)&&typeof input.markdown==='string'&&input.markdown.trim().length>0&&input.markdown.length<=2000&&[...input.markdown].length<=1000);
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,input[k]])));
}
export function commentReceipt(v,input){
 const keys=['contractVersion','rendererVersion','commandId','contentId','parentId','kind','revision','visibility','createdAt'];
 requireResponse(v&&Object.keys(v).length===keys.length&&Object.keys(v).every(k=>keys.includes(k))&&v.contractVersion===commentVersion&&v.rendererVersion===rendererVersion&&v.commandId===input.commandId&&id(v.contentId)&&v.parentId===input.postId&&v.kind==='COMMENT'&&v.revision===1&&['VISIBLE','REVIEW_PENDING'].includes(v.visibility)&&Number.isSafeInteger(v.createdAt)&&v.createdAt>=0);
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,v[k]])));
}
function updatedTime(v,createdAt){requireResponse(v.visibility==='VISIBLE'?Number.isSafeInteger(v.updatedAt)&&v.updatedAt>=0&&(createdAt===undefined||v.updatedAt>=createdAt&&(v.revision!==1||v.updatedAt===createdAt)):v.updatedAt===null);}
export function ownContent(v,renderer){
 const keys=['kind','id','parentId','revision','visibility','commentLocked','title','sanitizedBody','updatedAt'];
 requireResponse(v&&Object.keys(v).length===keys.length&&Object.keys(v).every(k=>keys.includes(k))&&['POST','COMMENT'].includes(v.kind)&&id(v.id)&&(v.kind==='POST'?v.parentId===null:id(v.parentId))&&Number.isSafeInteger(v.revision)&&v.revision>=1&&typeof v.commentLocked==='boolean'&&['VISIBLE','HIDDEN','REVIEW_PENDING','AUTHOR_DELETED'].includes(v.visibility));
 updatedTime(v);
 requireResponse(v.visibility==='VISIBLE'?renderer===rendererVersion&&typeof v.sanitizedBody==='string'&&(v.kind==='COMMENT'?v.title===null:typeof v.title==='string'):renderer===null&&v.title===null&&v.sanitizedBody===null);
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,v[k]])));
}
export function ownDetail(v,contentId){
 requireResponse(v&&Object.keys(v).length===3&&v.contractVersion===ownReadVersion&&v.content?.id===contentId);
 return Object.freeze({contractVersion:ownReadVersion,rendererVersion:v.rendererVersion,content:ownContent(v.content,v.rendererVersion)});
}
export function ownComments(v){
 requireResponse(v&&Object.keys(v).length===3&&v.contractVersion===ownCommentsVersion&&Array.isArray(v.items)&&v.items.length<=50&&(v.nextCursor===null||typeof v.nextCursor==='string'&&v.nextCursor.length>0&&v.nextCursor.length<=4096));
 const items=v.items.map(row=>{
  const keys=['kind','id','parentId','revision','visibility','commentLocked','title','sanitizedBody','rendererVersion','createdAt','updatedAt'];
  requireResponse(row&&Object.keys(row).length===keys.length&&Object.keys(row).every(k=>keys.includes(k))&&row.kind==='COMMENT'&&Number.isSafeInteger(row.createdAt)&&row.createdAt>=0);
  updatedTime(row,row.createdAt);
  const {rendererVersion,createdAt,...content}=row;
  return Object.freeze({...ownContent(content,rendererVersion),rendererVersion,createdAt});
 });
 requireResponse(new Set(items.map(row=>row.id)).size===items.length&&items.every((row,i)=>i===0||row.createdAt>items[i-1].createdAt||row.createdAt===items[i-1].createdAt&&row.id>items[i-1].id));
 return Object.freeze({contractVersion:ownCommentsVersion,items:Object.freeze(items),nextCursor:v.nextCursor});
}


export const postVersion='community.post-create.v1.20261008';
const boards=Object.freeze(['free','qna','guide_build','feedback']);
const sourceId=v=>typeof v==='string'&&/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/.test(v);
export function postCommand(input){
 const required=['commandId','boardId','title','markdown'],keys=[...required,'spoiler','wikiRefs','sourceBalanceVersion'];
 requireInput(input&&typeof input==='object'&&!Array.isArray(input)&&required.every(k=>Object.hasOwn(input,k))&&Object.keys(input).every(k=>keys.includes(k))&&id(input.commandId)&&boards.includes(input.boardId)&&typeof input.title==='string'&&input.title.length<=200&&input.title.trim().length>0&&[...input.title.trim()].length<=100&&typeof input.markdown==='string'&&input.markdown.length<=20000&&input.markdown.trim().length>0&&[...input.markdown].length<=10000);
 const spoiler=input.spoiler===undefined?false:input.spoiler,wikiRefs=input.wikiRefs===undefined?[]:input.wikiRefs,sourceBalanceVersion=input.sourceBalanceVersion===undefined?null:input.sourceBalanceVersion;
 requireInput(typeof spoiler==='boolean'&&Array.isArray(wikiRefs)&&(sourceBalanceVersion===null||sourceId(sourceBalanceVersion)));
 const refs=wikiReferences(wikiRefs);
 return Object.freeze({commandId:input.commandId,boardId:input.boardId,title:input.title,markdown:input.markdown,spoiler,wikiRefs:Object.freeze(refs),sourceBalanceVersion});
}
export function postReceipt(v,input){
 const keys=['contractVersion','rendererVersion','commandId','contentId','parentId','boardId','detailsHash','kind','revision','visibility','createdAt'];
 requireResponse(v&&Object.keys(v).length===keys.length&&Object.keys(v).every(k=>keys.includes(k))&&v.contractVersion===postVersion&&v.rendererVersion===rendererVersion&&v.commandId===input.commandId&&id(v.contentId)&&v.parentId===null&&v.boardId===input.boardId&&typeof v.detailsHash==='string'&&/^[a-f0-9]{64}$/.test(v.detailsHash)&&v.kind==='POST'&&v.revision===1&&['VISIBLE','REVIEW_PENDING'].includes(v.visibility)&&Number.isSafeInteger(v.createdAt)&&v.createdAt>=0);
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,v[k]])));
}


export const ownPostsVersion='community.own-posts-list.v1.20261008';
function wikiReferences(refs){
 requireInput(Array.isArray(refs));
 const normalized=refs.map(ref=>{
  requireInput(ref&&typeof ref==='object'&&Object.keys(ref).length===2&&Object.keys(ref).every(k=>['snapshotId','itemId'].includes(k))&&typeof ref.snapshotId==='string'&&/^pcs_[a-f0-9]{64}$/.test(ref.snapshotId)&&sourceId(ref.itemId));
  return Object.freeze({snapshotId:ref.snapshotId,itemId:ref.itemId});
 });
 requireInput(new Set(normalized.map(ref=>ref.snapshotId+':'+ref.itemId)).size===normalized.length);
 return Object.freeze(normalized);
}
export function ownPosts(v,boardId){
 requireResponse(v&&Object.keys(v).length===3&&v.contractVersion===ownPostsVersion&&Array.isArray(v.items)&&v.items.length<=50&&(v.nextCursor===null||typeof v.nextCursor==='string'&&v.nextCursor.length>0&&v.nextCursor.length<=4096));
 const items=v.items.map(row=>{
  const keys=['kind','id','parentId','revision','visibility','commentLocked','title','sanitizedBody','rendererVersion','createdAt','updatedAt','boardId','spoiler','wikiRefs','sourceBalanceVersion'];
  requireResponse(row&&Object.keys(row).length===keys.length&&Object.keys(row).every(k=>keys.includes(k))&&row.kind==='POST'&&Number.isSafeInteger(row.createdAt)&&row.createdAt>=0&&boards.includes(row.boardId)&&(boardId===undefined||row.boardId===boardId));
  updatedTime(row,row.createdAt);
  const {rendererVersion,createdAt,boardId:board,spoiler,wikiRefs,sourceBalanceVersion,...content}=row;
  const base=ownContent(content,rendererVersion);
  let refs=null;
  if(row.visibility==='VISIBLE'){
   requireResponse(typeof spoiler==='boolean'&&Array.isArray(wikiRefs)&&(sourceBalanceVersion===null||sourceId(sourceBalanceVersion)));
   try{refs=wikiReferences(wikiRefs);}catch{requireResponse(false);}
  }else requireResponse(spoiler===null&&wikiRefs===null&&sourceBalanceVersion===null);
  return Object.freeze({...base,rendererVersion,createdAt,boardId:board,spoiler,wikiRefs:refs,sourceBalanceVersion});
 });
 requireResponse(new Set(items.map(row=>row.id)).size===items.length&&items.every((row,i)=>i===0||row.createdAt<items[i-1].createdAt||row.createdAt===items[i-1].createdAt&&row.id<items[i-1].id));
 return Object.freeze({contractVersion:ownPostsVersion,items:Object.freeze(items),nextCursor:v.nextCursor});
}

export const blockVersion='community.personal-block.v1.20261008';
export function blockCommand(input){
 const keys=['commandId','publicAuthorId','blocked','expectedRevision'];
 requireInput(input&&typeof input==='object'&&!Array.isArray(input)&&Object.keys(input).length===4&&Object.keys(input).every(k=>keys.includes(k))&&id(input.commandId)&&id(input.publicAuthorId)&&typeof input.blocked==='boolean'&&Number.isSafeInteger(input.expectedRevision)&&input.expectedRevision>=0&&input.expectedRevision<Number.MAX_SAFE_INTEGER);
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,input[k]])));
}
export function blockReceipt(v,input){
 const keys=['contractVersion','commandId','publicAuthorId','blocked','revision'];
 requireResponse(v&&Object.keys(v).length===5&&Object.keys(v).every(k=>keys.includes(k))&&v.contractVersion===blockVersion&&v.commandId===input.commandId&&v.publicAuthorId===input.publicAuthorId&&v.blocked===input.blocked&&Number.isSafeInteger(v.revision)&&v.revision<Number.MAX_SAFE_INTEGER&&[input.expectedRevision,input.expectedRevision+1].includes(v.revision));
 return Object.freeze(Object.fromEntries(keys.map(k=>[k,v[k]])));
}
export function blockList(v){
 requireResponse(v&&Object.keys(v).length===4&&v.contractVersion===blockVersion&&Number.isSafeInteger(v.revision)&&v.revision>=0&&v.revision<Number.MAX_SAFE_INTEGER&&Array.isArray(v.items)&&v.items.length<=50&&(v.nextCursor===null||typeof v.nextCursor==='string'&&v.nextCursor.length>0&&v.nextCursor.length<=4096));
 const items=v.items.map(row=>{requireResponse(row&&Object.keys(row).length===1&&id(row.publicAuthorId));return Object.freeze({publicAuthorId:row.publicAuthorId});});
 requireResponse(items.every((row,i)=>i===0||row.publicAuthorId>items[i-1].publicAuthorId));
 return Object.freeze({contractVersion:blockVersion,revision:v.revision,items:Object.freeze(items),nextCursor:v.nextCursor});
}

export const publicVersion='community.public-read.v1.20261008';
export const publicScopeVersion='community.public-source.v1.20261008';
function publicContent(v,renderer){
 const baseKeys=['kind','id','parentId','publicAuthorId','revision','visibility','commentLocked','title','sanitizedBody','createdAt','updatedAt'];
 const keys=v?.kind==='POST'?[...baseKeys,'boardId','spoiler','wikiRefs','sourceBalanceVersion']:baseKeys;
 requireResponse(v&&Object.keys(v).length===keys.length&&Object.keys(v).every(k=>keys.includes(k))&&v.visibility==='VISIBLE'&&id(v.publicAuthorId)&&Number.isSafeInteger(v.createdAt)&&v.createdAt>=0);
 updatedTime(v,v.createdAt);
 const {publicAuthorId,createdAt,boardId,spoiler,wikiRefs,sourceBalanceVersion,...base}=v;
 const content=ownContent(base,renderer);
 if(v.kind==='POST'){
  requireResponse(boards.includes(boardId)&&typeof spoiler==='boolean'&&Array.isArray(wikiRefs)&&(sourceBalanceVersion===null||sourceId(sourceBalanceVersion)));
  let refs;try{refs=wikiReferences(wikiRefs);}catch{requireResponse(false);}
  return Object.freeze({...content,publicAuthorId,createdAt,boardId,spoiler,wikiRefs:refs,sourceBalanceVersion});
 }
 return Object.freeze({...content,publicAuthorId,createdAt});
}
export function publicDetail(v,id){
 requireResponse(v&&Object.keys(v).length===4&&Object.keys(v).every(k=>['contractVersion','publicScopeVersion','rendererVersion','content'].includes(k))&&v.contractVersion===publicVersion&&v.publicScopeVersion===publicScopeVersion&&v.content?.id===id);
 return Object.freeze({contractVersion:publicVersion,publicScopeVersion,rendererVersion:v.rendererVersion,content:publicContent(v.content,v.rendererVersion)});
}
export function publicPage(v,{kind,boardId,parentId}){
 requireResponse(v&&Object.keys(v).length===4&&Object.keys(v).every(k=>['contractVersion','publicScopeVersion','items','nextCursor'].includes(k))&&v.contractVersion===publicVersion&&v.publicScopeVersion===publicScopeVersion&&Array.isArray(v.items)&&v.items.length<=50&&(v.nextCursor===null||typeof v.nextCursor==='string'&&v.nextCursor.length>0&&v.nextCursor.length<=1024));
 const items=v.items.map(row=>{
  const {rendererVersion,...content}=row;const item=publicContent(content,rendererVersion);
  requireResponse(item.kind===kind&&(boardId===undefined||item.boardId===boardId)&&(parentId===undefined||item.parentId===parentId));
  return Object.freeze({...item,rendererVersion});
 });
 requireResponse(new Set(items.map(row=>row.id)).size===items.length&&items.every((row,i)=>i===0||(kind==='POST'?row.createdAt<items[i-1].createdAt||row.createdAt===items[i-1].createdAt&&row.id<items[i-1].id:row.createdAt>items[i-1].createdAt||row.createdAt===items[i-1].createdAt&&row.id>items[i-1].id)));
 return Object.freeze({contractVersion:publicVersion,publicScopeVersion,items:Object.freeze(items),nextCursor:v.nextCursor});
}
