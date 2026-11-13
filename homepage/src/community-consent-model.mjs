import {CommunityCommandModel} from './community-command-model.mjs';
import {SupportReadModel} from './support-read-model.mjs';
import {consentCommand} from './community-consent-contracts.mjs';
// Current provenance belongs to the read owner; acceptance receipts remain historical.
export class CommunityConsentModel {
 #read;#command;#off;
 constructor(api,session,{creationModels=[],...options}={}){
  this.#read=new SupportReadModel(api,session,{...options,detail:async(_,o)=>({ticket:await api.communityConsent(o)})});
  this.#command=new CommunityCommandModel(session,{...options,normalize:v=>consentCommand(v,api.environment??v?.environment),send:(v,o)=>api.acceptCommunityConsent(v,o),progress:'accepting',complete:'accepted'});
  const refreshCurrent=()=>{this.#read.invalidate();if(this.#read.state.phase!=='authentication-required')this.#read.openTicket('current');};
  const changed=s=>s.phase==='error'&&['community_policy_changed','community_consent_changed','community_consent_required'].includes(s.error?.code);
  const subscriptions=[this.#command.subscribe(s=>{if(s.phase==='accepted'||changed(s))refreshCurrent();}),...creationModels.map(model=>model.subscribe(s=>{if(changed(s))refreshCurrent();}))];
  this.#off=()=>subscriptions.forEach(off=>off());
 }
 get state(){return this.#command.state;}
 get current(){const {ticket,...s}=this.#read.state;return Object.freeze({...s,consent:ticket});}
 subscribe(fn){const a=this.#command.subscribe(()=>fn({acceptance:this.state,current:this.current})),b=this.#read.subscribe(()=>fn({acceptance:this.state,current:this.current}));return ()=>{a();b();};}
 open(){return this.#read.openTicket('current');}
 refresh(){return this.#read.refresh();}
 accept(input){return this.#command.execute(input);}
 retry(){return this.#command.retry();}
 clear(){return this.#command.clear();}
 dispose(){this.#off();this.#command.dispose();this.#read.dispose();}
}
