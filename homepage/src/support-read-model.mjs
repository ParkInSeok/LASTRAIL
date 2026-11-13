import {WebApiError,requireInput} from './api-http.mjs';
const retryable=new Set(['network','timeout','unavailable','failure','rate-limited']);
const emptyItems=Object.freeze([]);
// Owns only authenticated read selection/results. Session and submission are separate owners.
export class SupportReadModel {
 #allowAnonymous;#anonymous=Symbol("anonymous");#list;#detail;#restartCodes;#session;#off;#owner=null;#request=null;#generation=0;#listeners=new Set();#disposed=false;#now;
 #state;
 constructor(api,session,{now=Date.now,allowAnonymous=false,restartListOnConflict=[],list=(filters,options)=>api.tickets(filters,options),detail=(id,options)=>api.ticket(id,options)}={}){
  this.#allowAnonymous=allowAnonymous;this.#restartCodes=new Set(restartListOnConflict);this.#list=list;this.#detail=detail;this.#session=session;this.#now=now;
  this.#off=session.subscribe(state=>this.#sessionChanged(state));
  this.#sessionChanged(session.state);
 }
 get state(){return this.#state;}
 subscribe(fn){if(this.#disposed)return ()=>{};this.#listeners.add(fn);return ()=>this.#listeners.delete(fn);}
 #publish(phase,{query=null,items=emptyItems,ticket=null,nextCursor=null,error=null,retryAt=null,revision=null}={}){
  this.#state=Object.freeze({phase,query,items,ticket,nextCursor,error,retryAt,revision});
  for(const fn of this.#listeners)fn(this.#state);
 }
 #cancel(){this.#generation++;this.#request?.abort();this.#request=null;}
 #viewer(state){return state.phase==="authenticated"?state.accountId:this.#allowAnonymous&&state.phase==="anonymous"?this.#anonymous:null;}
 #sessionChanged(state){
  if(this.#disposed)return;
  this.#cancel();this.#owner=this.#viewer(state);
  this.#publish(this.#owner===null?'authentication-required':'idle');
 }
 #requireOwner(){if(this.#disposed||this.#owner===null||this.#viewer(this.#session.state)!==this.#owner)throw new WebApiError('unauthenticated');}
 openList(filters={}){this.#requireOwner();requireInput(filters&&typeof filters==='object'&&!Array.isArray(filters));return this.#load(Object.freeze({kind:'list',cursor:null,filters:Object.freeze({...filters})}));}
 openTicket(ticketId){this.#requireOwner();requireInput(typeof ticketId==='string'&&ticketId.length>0&&ticketId.length<=256);return this.#load(Object.freeze({kind:'detail',ticketId}));}
 nextPage(){
  this.#requireOwner();if(this.#state.phase!=='ready'||this.#state.query?.kind!=='list'||this.#state.nextCursor===null)return Promise.resolve();
  return this.#load(Object.freeze({...this.#state.query,cursor:this.#state.nextCursor}));
 }
 retry(){
  if(this.#disposed||this.#state.phase!=='error'||!retryable.has(this.#state.error?.kind)||(this.#state.retryAt!==null&&this.#now()<this.#state.retryAt))return Promise.resolve();
  this.#requireOwner();return this.#load(this.#state.query);
 }
 refresh(){this.#requireOwner();if(!this.#state.query||this.#state.phase==='loading'||(this.#state.retryAt!==null&&this.#now()<this.#state.retryAt))return Promise.resolve();return this.#load(this.#state.query);}
 async #load(query){
  this.#cancel();const generation=this.#generation,owner=this.#owner;
  const request=new AbortController();this.#request=request;
  this.#publish('loading',{query});
  if(request.signal.aborted||generation!==this.#generation)return;
  try{
   const result=query.kind==='list'?await this.#list({...query.filters,limit:20,cursor:query.cursor},{signal:request.signal}):await this.#detail(query.ticketId,{signal:request.signal});
   if(this.#disposed||request.signal.aborted||generation!==this.#generation||owner!==this.#viewer(this.#session.state))return;
   // WebApi already validates and freezes the server DTO at the external boundary.
   if(query.kind==='list')this.#publish(result.items.length?'ready':'empty',{query,items:result.items,nextCursor:result.nextCursor,revision:result.revision??null});
   else this.#publish('ready',{query,ticket:result.ticket});
  }catch(error){
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   if(error.kind==='unauthenticated'){this.#session.expire();return;}
   if(query.kind==='list'&&this.#restartCodes.has(error.code))query=Object.freeze({...query,cursor:null});
   const kind=error.kind??'failure';
   const retryAt=Number.isSafeInteger(error.retryAfterSeconds)&&error.retryAfterSeconds>=0&&error.retryAfterSeconds<=86400?this.#now()+error.retryAfterSeconds*1000:null;
   this.#publish(kind==='not-found'?'not-found':'error',{query,error:Object.freeze({kind,status:error.status??null,code:error.code??null}),retryAt});
  }finally{if(this.#request===request)this.#request=null;}
 }
 invalidate(){if(this.#disposed)return;const query=this.#state.query;this.#cancel();this.#publish(this.#owner===null?"authentication-required":"idle",{query:query?.kind==="list"?Object.freeze({...query,cursor:null}):query});}
 dispose(){if(this.#disposed)return;this.#off();this.#cancel();this.#owner=null;this.#disposed=true;this.#listeners.clear();this.#publish('disposed');}
}





