import {requireInput} from './api-http.mjs';
import {SupportReadModel} from './support-read-model.mjs';
// Public current projections only; PRD anonymous is explicit, expired identity never becomes anonymous.
export class CommunityPublicReadModel {
 #read;#off;
 constructor(api,session,{blocks,...options}={}){
  this.#read=new SupportReadModel(api,session,{...options,allowAnonymous:api.environment==='PRD',list:({scope,boardId,parentId,...filters},options)=>scope==='comments'?api.publicCommunityComments(parentId,filters,options):api.publicCommunityPosts({...filters,...(boardId===null?{}:{boardId})},options),detail:async(id,options)=>{const v=await api.publicCommunityContent(id,options);return {ticket:Object.freeze({...v.content,rendererVersion:v.rendererVersion,publicScopeVersion:v.publicScopeVersion})};},restartListOnConflict:['community_content_stale','community_cursor_invalid']});
  this.#off=blocks?.subscribe(state=>{if(state.phase!=='applied')return;this.#read.invalidate();if(this.#read.state.query&&this.#read.state.phase!=='authentication-required')this.#read.refresh();});
 }
 get state(){const {ticket,...state}=this.#read.state;return Object.freeze({...state,content:ticket});}
 subscribe(fn){return this.#read.subscribe(()=>fn(this.state));}
 openPosts(boardId=null){requireInput(boardId===null||['free','qna','guide_build','feedback'].includes(boardId));return this.#read.openList({scope:'posts',boardId});}
 openComments(parentId){requireInput(typeof parentId==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(parentId));return this.#read.openList({scope:'comments',parentId});}
 openContent(id){return this.#read.openTicket(id);}
 nextPage(){return this.#read.nextPage();}
 refresh(){return this.#read.refresh();}
 retry(){return this.#read.retry();}
 dispose(){this.#off?.();this.#read.dispose();}
}
