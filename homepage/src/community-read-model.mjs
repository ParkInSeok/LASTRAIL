import {requireInput} from './api-http.mjs';
import {SupportReadModel} from './support-read-model.mjs';
// Reuses authenticated read lifecycle; creation receipts are never applied to this current-read owner.
export class CommunityReadModel {
 #read;
 constructor(api,session,options={}){this.#read=new SupportReadModel(api,session,{...options,list:({scope,boardId,...filters},options)=>scope==='posts'?api.ownCommunityPosts({...filters,...(boardId===null?{}:{boardId})},options):api.ownCommunityComments(filters,options),detail:async(id,options)=>{const result=await api.ownCommunityContent(id,options);return {ticket:Object.freeze({...result.content,rendererVersion:result.rendererVersion})};}});}
 get state(){const {ticket,...state}=this.#read.state;return Object.freeze({...state,content:ticket});}
 subscribe(fn){return this.#read.subscribe(()=>fn(this.state));}
 openList(){return this.#read.openList();}
 openPosts(boardId=null){requireInput(boardId===null||['free','qna','guide_build','feedback'].includes(boardId));return this.#read.openList({scope:'posts',boardId});}
 openContent(id){return this.#read.openTicket(id);}
 nextPage(){return this.#read.nextPage();}
 refresh(){return this.#read.refresh();}
 retry(){return this.#read.retry();}
 dispose(){this.#read.dispose();}
}


