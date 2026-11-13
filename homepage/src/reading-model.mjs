import {validReadingQuery} from './reading-route.mjs';
// Internal UI port: read(query, {signal}) -> unavailable | empty | not-found | failure.
// No endpoint/DTO/publication fields are guessed. A real public-data adapter is
// intentionally absent until Server supplies its reviewed contract and validation.
export class UnavailableReadingPort {
 async read(_query,{signal}={}) {signal?.throwIfAborted();return {kind:'unavailable'};}
}
export class ReadingModel {
 #port; #listeners=new Set(); #request; #generation=0; #disposed=false;
 #state=Object.freeze({phase:'idle',query:null,retryAt:null}); #now;
 constructor(port,{now=Date.now}={}){this.#port=port;this.#now=now;}
 get state(){return this.#state;}
 subscribe(callback){if(this.#disposed)return ()=>{};this.#listeners.add(callback);return ()=>this.#listeners.delete(callback);}
 #publish(next){this.#state=Object.freeze(next);for(const fn of this.#listeners)fn(this.#state);}
 async load(query,{invalid=false}={}) {
  if(this.#disposed)return;
  this.cancel();
  const generation=this.#generation;
  const selection=Object.freeze({...query});
  if(invalid||!validReadingQuery(query)){this.#publish({phase:'invalid',query:selection,retryAt:null});return;}
  const request=new AbortController();this.#request=request;
  this.#publish({phase:'loading',query:selection,retryAt:null});
  let result;
  try {
   result=await this.#port.read(selection,{signal:request.signal});
   if(!result||typeof result!=='object'||!['unavailable','empty','not-found','failure'].includes(result.kind))throw new TypeError('Invalid adapter outcome');
   const allowed=result.kind==='failure'?['kind','retryAfterSeconds']:['kind'];
   if(Object.keys(result).some(k=>!allowed.includes(k)))throw new TypeError('Unexpected adapter data');
   if(result.kind==='failure'&&result.retryAfterSeconds!==undefined&&(!Number.isSafeInteger(result.retryAfterSeconds)||result.retryAfterSeconds<0||result.retryAfterSeconds>86400))throw new TypeError('Invalid retry delay');
  }catch{
   if(request.signal.aborted)return;
   result={kind:'failure'};
  }
  if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
  this.#request=null;
  const retryAt=result.kind==='failure'?this.#now()+(result.retryAfterSeconds??0)*1000:null;
  this.#publish({phase:result.kind==='failure'?'error':result.kind,query:selection,retryAt});
 }
 retry(){
  if(this.#disposed||this.#state.phase!=='error'||this.#now()<this.#state.retryAt)return Promise.resolve();
  return this.load(this.#state.query);
 }
 cancel(){this.#generation++;this.#request?.abort();this.#request=null;}
 dispose(){this.cancel();this.#disposed=true;this.#listeners.clear();}
}

