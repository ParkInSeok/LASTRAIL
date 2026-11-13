import {CommunityCommandModel} from './community-command-model.mjs';
import {SupportReadModel} from './support-read-model.mjs';
import {requireInput} from './api-http.mjs';
import {reportCommand} from './community-report-contracts.mjs';
// Receipt is immutable history; current own projection has a separate read owner.
export class CommunityReportModel {
 #command;#off;
 constructor(api,session,{targetRead,...options}={}){this.#command=new CommunityCommandModel(session,{...options,normalize:reportCommand,send:(v,o)=>api.submitCommunityReport(v,o),progress:'submitting',complete:'submitted'});this.#off=targetRead?this.#command.subscribe(s=>{if(!['disposed','authentication-required'].includes(targetRead.state?.phase)&&s.phase==='error'&&['community_report_stale','community_report_revision_conflict'].includes(s.error?.code))targetRead.openContent(s.command.targetId);}):()=>{};}
 get state(){const s=this.#command.state;return s.error?.code==='community_forbidden'?Object.freeze({...s,error:Object.freeze({...s.error,reason:'permission-forbidden'})}):s;}
 subscribe(fn){return this.#command.subscribe(()=>fn(this.state));}
 execute(v){return this.#command.execute(v);}
 retry(){return this.#command.retry();}
 clear(){return this.#command.clear();}
 dispose(){this.#off();this.#command.dispose();}
}
export class CommunityReportReadModel {
 #read;
 constructor(api,session,options={}){this.#read=new SupportReadModel(api,session,{...options,detail:async(id,o)=>({ticket:(await api.ownCommunityReport(id,o)).report})});}
 get state(){const {ticket,...s}=this.#read.state;return Object.freeze({...s,report:ticket});}
 subscribe(fn){return this.#read.subscribe(()=>fn(this.state));}
 openReport(id){requireInput(typeof id==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(id));return this.#read.openTicket(id);}
 refresh(){return this.#read.refresh();}
 retry(){return this.#read.retry();}
 dispose(){this.#read.dispose();}
}
