// Public login availability is an observation, never a client clock or a login command.
export class MaintenanceLoginModel {
 #api;#request=null;#generation=0;#disposed=false;#listeners=new Set();#off;#sessionKey=null;
 #state=Object.freeze({phase:'unknown',confirmed:null,snapshot:null,error:null});
 constructor(api,{session}={}){
  this.#api=api;
  if(session){this.#sessionKey=this.#key(session.state);this.#off=session.subscribe(state=>this.#sessionChanged(state));this.#sessionChanged(session.state);}
 }
 get state(){return this.#state;}
 subscribe(fn){if(this.#disposed)return ()=>{};this.#listeners.add(fn);return ()=>this.#listeners.delete(fn);}
 #publish(phase,{confirmed=null,snapshot=null,error=null}={}){this.#state=Object.freeze({phase,confirmed,snapshot,error});if(!this.#disposed)for(const fn of this.#listeners)fn(this.#state);}
 #cancel(){this.#generation++;this.#request?.abort();this.#request=null;}
 #key(state){return state.phase+':'+(state.accountId??'');}
 #sessionChanged(state){
  if(this.#disposed)return;
  const key=this.#key(state),changed=key!==this.#sessionKey;this.#sessionKey=key;
  if(changed||state.maintenanceConfirmed!==undefined&&state.maintenanceConfirmed!==null){
   this.#cancel();this.#publish(state.maintenanceConfirmed===true?'blocked':'unknown',{confirmed:state.maintenanceConfirmed??null,error:state.error??null});
  }
 }
 async refresh(){
  if(this.#disposed)return;
  this.#cancel();const generation=this.#generation,request=new AbortController();this.#request=request;
  this.#publish('loading');
  if(request.signal.aborted||generation!==this.#generation)return;
  try{
   const snapshot=await this.#api.maintenanceLogin({signal:request.signal});
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   this.#publish(snapshot.webLoginAllowed?'allowed':'blocked',{confirmed:!snapshot.webLoginAllowed,snapshot});
  }catch(error){
   if(this.#disposed||request.signal.aborted||generation!==this.#generation)return;
   this.#publish(error.maintenanceConfirmed===true?'blocked':'unknown',{confirmed:error.maintenanceConfirmed??null,error:error.kind??'failure'});
  }finally{if(this.#request===request)this.#request=null;}
 }
 dispose(){if(this.#disposed)return;this.#cancel();this.#off?.();this.#disposed=true;this.#listeners.clear();this.#publish('disposed');}
}

