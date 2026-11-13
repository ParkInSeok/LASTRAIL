import {WebApiError,requireInput} from './api-http.mjs';
import {categories,text} from './api-contracts.mjs';
const blank=()=>Object.freeze({category:'BUG_OTHER',body:''});
// Sole owner of memory-only draft and original submission. Identity is borrowed from SessionModel.
export class SupportModel {
 #api;#session;#off;#newKey;#now;#retryAt=null;#owner=null;#draft=blank();#pending=null;#ticketId=null;#phase='composing';#error=null;#request=null;#generation=0;#disposed=false;#listeners=new Set();#state;
 constructor(api,session,{newKey=()=>crypto.randomUUID(),now=Date.now}={}){this.#api=api;this.#session=session;this.#newKey=newKey;this.#now=now;this.#off=session.subscribe(state=>this.#sessionChanged(state));this.#sessionChanged(session.state);}
 get state(){return this.#state;}
 subscribe(fn){if(this.#disposed)return ()=>{};this.#listeners.add(fn);return ()=>this.#listeners.delete(fn);}
 #cancel(){this.#generation++;this.#request?.abort();this.#request=null;}
 #clear(){this.#cancel();this.#draft=blank();this.#pending=null;this.#ticketId=null;this.#phase='composing';this.#error=null;this.#retryAt=null;}
 #publish(){
  const visible=!this.#disposed&&this.#session.state.phase==='authenticated'&&this.#session.state.accountId===this.#owner;
  this.#state=Object.freeze({phase:visible?this.#phase:'authentication-required',draft:visible?this.#draft:null,ticketId:visible?this.#ticketId:null,error:visible?this.#error:null,retryAt:visible?this.#retryAt:null});
  if(!this.#disposed)for(const fn of this.#listeners)fn(this.#state);
 }
 #sessionChanged(state){
  if(this.#disposed)return;
  if(state.phase==='authenticated'){
   if(state.accountId!==this.#owner){this.#clear();this.#owner=state.accountId;}
  }else{
   this.#cancel();if(this.#phase==='submitting'||this.#phase==='checking')this.#phase='uncertain';
   if(state.phase==='anonymous'||state.phase==='disposed'){this.#clear();this.#owner=null;}
  }
  this.#publish();
 }
 #requireOwner(){if(this.#disposed||this.#session.state.phase!=='authenticated'||this.#owner!==this.#session.state.accountId)throw new WebApiError('unauthenticated');}
 edit({category,body}){this.#requireOwner();requireInput(!this.#pending&&categories.includes(category)&&text(body));this.#draft=Object.freeze({category,body});this.#publish();}
 async submit(){
  this.#requireOwner();if(this.#pending)return;
  requireInput(this.#draft.body.trim().length>=10);
  const key=this.#newKey();requireInput(typeof key==='string'&&/^[\x21-\x7e]{16,128}$/.test(key));
  this.#pending=Object.freeze({key,payload:this.#draft});return this.#run(false);
 }
 recover(){this.#requireOwner();requireInput(this.#pending!==null);return this.#run(true);}
 async replay(){this.#requireOwner();requireInput(this.#pending!==null);return this.#run(false);}
 async #run(receipt){
  if(this.#request||this.#phase==='accepted')return;
  if(this.#retryAt!==null&&this.#now()<this.#retryAt)throw new WebApiError('rate-limited',{retryAfterSeconds:Math.ceil((this.#retryAt-this.#now())/1000)});
  const request=new AbortController();this.#request=request;const generation=this.#generation;const pending=this.#pending;
  this.#phase=receipt?'checking':'submitting';this.#error=null;this.#retryAt=null;this.#publish();
  try{
   let result;
   if(receipt)result=await this.#api.receipt(pending.key,{signal:request.signal});
   else{const csrfToken=await this.#session.csrfToken({signal:request.signal});if(request.signal.aborted)return;result=await this.#api.submitTicket(pending.payload,{key:pending.key,csrfToken,signal:request.signal});}
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   if(result.accepted!==true||typeof result.ticketId!=='string')throw new WebApiError('invalid-response');
   this.#phase='accepted';this.#ticketId=result.ticketId;
  }catch(error){
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   this.#phase='uncertain';this.#error=error.kind??'failure';
   this.#retryAt=Number.isSafeInteger(error.retryAfterSeconds)&&error.retryAfterSeconds>=0&&error.retryAfterSeconds<=86400?this.#now()+error.retryAfterSeconds*1000:null;
   if(error.kind==='unauthenticated')this.#session.expire();
  }finally{if(this.#request===request)this.#request=null;if(!this.#disposed&&generation===this.#generation)this.#publish();}
 }
 newTicket(){this.#requireOwner();requireInput(!this.#pending||this.#phase==='accepted');this.#clear();this.#publish();}
 dispose(){if(this.#disposed)return;this.#off();this.#clear();this.#owner=null;this.#disposed=true;this.#publish();this.#listeners.clear();}
}

