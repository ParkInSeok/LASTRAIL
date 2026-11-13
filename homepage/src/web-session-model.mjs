import {WebApiError} from './api-http.mjs';
// Sole owner of verified WEB identity and in-memory CSRF. Views receive no secret.
export class WebSessionModel {
 #api;#now;#csrf=null;#request=null;#generation=0;#disposed=false;#listeners=new Set();#known=false;#csrfRequest=null;
 #state=Object.freeze({phase:'anonymous',accountId:null,expiresAt:null,error:null,logoutConfirmed:null});
 constructor(api,{now=Date.now}={}){this.#api=api;this.#now=now;}
 get state(){return this.#state;}
 subscribe(fn){if(this.#disposed)return ()=>{};this.#listeners.add(fn);return ()=>this.#listeners.delete(fn);}
 #publish(state){if(this.#disposed)return;this.#state=Object.freeze(state);for(const fn of this.#listeners)fn(this.#state);}
 #cancel(){this.#generation++;this.#request?.abort();this.#request=null;this.#csrfRequest?.abort();this.#csrfRequest=null;}
 async #authenticate(issue){
  if(this.#disposed)return;const previous=this.#state,previousCsrf=this.#csrf;this.#cancel();this.#csrf=null;const generation=this.#generation;const request=new AbortController();this.#request=request;
  this.#publish({phase:'authenticating',accountId:null,expiresAt:null,error:null,logoutConfirmed:null});
  try{
   const issued=issue?await issue({signal:request.signal}):null;
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   const verified=await this.#api.me({signal:request.signal});
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   if(Date.parse(verified.expiresAt)<=this.#now())throw new WebApiError('unauthenticated');
   this.#csrf=issued?.csrfToken??null;this.#known=true;
   this.#publish({phase:'authenticated',accountId:verified.accountId,expiresAt:verified.expiresAt,error:null,logoutConfirmed:null});
  }catch(error){
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   const maintenanceConfirmed=error.maintenanceConfirmed??null;
   // A maintenance protocol refusal commits no new session and leaves the verified one intact.
   if(issue&&error.maintenanceProtocol===true&&previous.phase==='authenticated'&&Date.parse(previous.expiresAt)>this.#now()){
    this.#csrf=previousCsrf;this.#publish({...previous,error:error.kind,maintenanceConfirmed});
   }else this.#publish({phase:this.#known?'expired':'anonymous',accountId:null,expiresAt:null,error:error.kind??'failure',logoutConfirmed:null,maintenanceConfirmed});
  }
  finally{if(this.#request===request)this.#request=null;}
 }
 restore(){return this.#authenticate(null);}
 loginPassword(loginId,password){return this.#authenticate(options=>this.#api.loginPassword(loginId,password,options));}
 loginGoogle(idToken){return this.#authenticate(options=>this.#api.loginGoogle(idToken,options));}
 expire(){if(this.#disposed)return;this.#cancel();this.#csrf=null;this.#publish({phase:'expired',accountId:null,expiresAt:null,error:'unauthenticated',logoutConfirmed:null});}
 async csrfToken({signal}={}){
  if(this.#disposed||this.#state.phase!=='authenticated')throw new WebApiError('unauthenticated');
  if(Date.parse(this.#state.expiresAt)<=this.#now()){this.expire();throw new WebApiError('unauthenticated');}
  signal?.throwIfAborted();if(this.#csrf)return this.#csrf;
  const generation=this.#generation;
  const request=new AbortController();this.#csrfRequest?.abort();this.#csrfRequest=request;
  const abort=()=>request.abort(signal.reason);signal?.addEventListener('abort',abort,{once:true});
  try{const result=await this.#api.csrf({signal:request.signal});request.signal.throwIfAborted();if(this.#disposed||generation!==this.#generation||this.#state.phase!=='authenticated')throw new WebApiError('unauthenticated');this.#csrf=result.csrfToken;return this.#csrf;}
  catch(error){if(error.kind==='unauthenticated'&&generation===this.#generation)this.expire();throw error;}
  finally{signal?.removeEventListener('abort',abort);if(this.#csrfRequest===request)this.#csrfRequest=null;}
 }
 async logout(){
  if(this.#disposed)return;const token=this.#csrf;this.#cancel();this.#csrf=null;this.#known=false;
  const generation=this.#generation;
  this.#publish({phase:'anonymous',accountId:null,expiresAt:null,error:null,logoutConfirmed:false});

  const request=new AbortController();this.#request=request;
  try{const csrfToken=token??(await this.#api.csrf({signal:request.signal})).csrfToken;if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;await this.#api.logout(csrfToken,{signal:request.signal});if(!this.#disposed&&generation===this.#generation)this.#publish({...this.#state,logoutConfirmed:true});}
  catch(error){if(!this.#disposed&&generation===this.#generation)this.#publish({...this.#state,error:error.kind??'failure',logoutConfirmed:false});}
  finally{if(this.#request===request)this.#request=null;}
 }
 dispose(){if(this.#disposed)return;this.#cancel();this.#csrf=null;this.#known=false;this.#publish({phase:'disposed',accountId:null,expiresAt:null,error:null,logoutConfirmed:null});this.#disposed=true;this.#listeners.clear();}
}


