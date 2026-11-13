import {WebApiError,requireInput} from './api-http.mjs';
const same=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
const reasons=Object.freeze({community_report_unavailable:'report-unavailable',community_report_stale:'report-stale',community_report_revision_conflict:'report-revision-conflict',community_report_rate_limited:'report-rate-limited',community_report_storage_invalid:'report-storage-invalid',community_report_storage_unavailable:'report-storage-unavailable',community_report_rate_source_invalid:'report-rate-source-invalid',community_policy_changed:'policy-changed',community_consent_changed:'consent-changed',community_consent_required:'consent-required',community_policy_source_unavailable:'policy-source-unavailable',community_policy_source_invalid:'policy-source-invalid',community_consent_storage_invalid:'consent-storage-invalid',community_consent_storage_unavailable:'consent-storage-unavailable',community_command_invalid:'invalid-command',community_self_block_forbidden:'self-block-forbidden',community_author_unavailable:'author-unavailable',community_block_revision_conflict:'block-revision-conflict',community_wiki_reference_unavailable:'wiki-reference-unavailable',community_wiki_source_unavailable:'wiki-source-unavailable',community_parent_unavailable:'parent-unavailable',community_comments_locked:'comments-locked',community_rate_limited:'rate-limited',community_review_policy_unavailable:'review-policy-unavailable',community_content_stale:'content-stale',community_content_invalid:'invalid-content',community_body_invalid:'invalid-body',community_markdown_invalid:'invalid-markdown',community_html_forbidden:'html-forbidden',community_image_forbidden:'image-forbidden',community_link_forbidden:'link-forbidden',contract_violation:'invalid-command',web_origin_forbidden:'origin-forbidden',web_csrf_forbidden:'csrf-forbidden',tester_forbidden:'tester-forbidden',community_forbidden:'not-owner',community_content_unavailable:'content-unavailable',community_version_conflict:'version-conflict',community_request_conflict:'request-conflict',community_revision_conflict:'revision-conflict',community_deleted:'already-deleted',community_storage_invalid:'storage-invalid',community_storage_unavailable:'storage-unavailable',community_unavailable:'unavailable',web_support_unavailable:'environment-unavailable'});
// Shared author-command lifecycle only; operation adapters own typed input and wire dispatch.
export class CommunityCommandModel {
 #send;#normalize;#progress;#complete;#session;#off;#owner=null;#generation=0;#request=null;#pending=null;#disposed=false;#listeners=new Set();#now;
 #state=Object.freeze({phase:'idle',command:null,receipt:null,error:null,retryAt:null});
 constructor(session,{send,normalize,progress,complete,now=Date.now}){this.#send=send;this.#normalize=normalize;this.#progress=progress;this.#complete=complete;this.#session=session;this.#now=now;this.#off=session.subscribe(s=>this.#sessionChanged(s));this.#sessionChanged(session.state);}
 get state(){return this.#state;}
 subscribe(fn){if(this.#disposed)return ()=>{};this.#listeners.add(fn);return ()=>this.#listeners.delete(fn);}
 #publish(phase,{command=null,receipt=null,error=null,retryAt=null}={}){this.#state=Object.freeze({phase,command,receipt,error,retryAt});if(!this.#disposed)for(const fn of this.#listeners)fn(this.#state);}
 #cancel(){this.#generation++;this.#request?.abort();this.#request=null;this.#pending=null;}
 #sessionChanged(s){if(this.#disposed)return;const owner=s.phase==='authenticated'?s.accountId:null;if(owner!==this.#owner){this.#cancel();this.#owner=owner;this.#publish(owner===null?'authentication-required':'idle');}}
 #requireOwner(){if(this.#disposed||this.#owner===null||this.#session.state.phase!=='authenticated'||this.#session.state.accountId!==this.#owner)throw new WebApiError('unauthenticated');if(Date.parse(this.#session.state.expiresAt)<=this.#now()){this.#session.expire();throw new WebApiError('unauthenticated');}}
 execute(input){
 try{this.#requireOwner();const next=this.#normalize(input);
 if(this.#state.command&&!same(this.#state.command,next))throw new WebApiError('conflict');
 if(this.#pending)return this.#pending;
 if(this.#state.phase===this.#complete)return Promise.resolve(this.#state.receipt);
 if(this.#state.phase==='error'||this.#state.phase==='rate-limited'&&(this.#state.retryAt===null||this.#now()<this.#state.retryAt))return Promise.resolve();
 const generation=this.#generation,owner=this.#owner,request=new AbortController();this.#request=request;
 let settle;const pending=new Promise(resolve=>{settle=resolve;});this.#pending=pending;
 this.#publish(this.#progress,{command:next});
 if(this.#disposed||request.signal.aborted||generation!==this.#generation){settle();return pending;}
 this.#run(next,{generation,owner,request}).then(settle,settle);return pending;
 }catch(error){return Promise.reject(error);}
 }
 retry(){return ['uncertain','rate-limited'].includes(this.#state.phase)?this.execute(this.#state.command):Promise.resolve();}
 clear(){if(this.#pending||['uncertain','rate-limited'].includes(this.#state.phase)||this.#disposed)return false;this.#publish(this.#owner===null?'authentication-required':'idle');return true;}
 async #run(next,{generation,owner,request}){
 const current=()=>!this.#disposed&&!request.signal.aborted&&generation===this.#generation&&owner===this.#session.state.accountId&&this.#session.state.phase==='authenticated';
 try{
  const csrfToken=await this.#session.csrfToken({signal:request.signal});if(!current())return;this.#requireOwner();
  const receipt=await this.#send(next,{csrfToken,signal:request.signal});if(!current())return;
  this.#publish(this.#complete,{command:next,receipt});
 }catch(error){
  if(!current())return;
  if(error.kind==='unauthenticated'){this.#session.expire();return;}
  const kind=error.kind??'failure',uncertain=['network','timeout','invalid-response','unavailable','failure'].includes(kind);
  const limited=kind==='rate-limited';const retryAt=limited&&Number.isSafeInteger(error.retryAfterSeconds)&&error.retryAfterSeconds>=1?this.#now()+error.retryAfterSeconds*1000:null;
  this.#publish(limited?'rate-limited':uncertain?'uncertain':'error',{command:next,retryAt,error:Object.freeze({kind,status:error.status??null,code:error.code??null,reason:reasons[error.code]??kind})});
 }finally{if(this.#request===request){this.#request=null;this.#pending=null;}}
 }
 dispose(){if(this.#disposed)return;this.#cancel();this.#off();this.#disposed=true;this.#listeners.clear();this.#publish('disposed');}
}






