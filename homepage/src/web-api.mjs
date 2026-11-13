import {reportCommand,reportReceipt,ownReport} from './community-report-contracts.mjs';
import {consentCurrent,verifiedConsentCommand,consentReceipt} from './community-consent-contracts.mjs';
import {communityCommand,communityReceipt,deleteVersion,editVersion,commentCommand,commentReceipt,commentVersion,ownDetail,ownComments,ownReadVersion,ownCommentsVersion,postCommand,postReceipt,postVersion,ownPosts,blockCommand,blockReceipt,blockList,publicDetail,publicPage} from './community-contracts.mjs';
import {WebHttp,requireInput,requireResponse} from './api-http.mjs';
import {sessionDto,pageDto,ticketDto,receiptDto,submissionDto,noticeDto,disclosureDto,indexDto,catalogDto,maintenanceLoginDto,categories,text,integer} from './api-contracts.mjs';
// Protocol facade: state and user flow belong to consumer Models/Controllers.
export class WebApi {
 #http; #environment; #version;
 constructor(options){this.#http=new WebHttp(options);this.#environment=options.environment;this.#version=options.versionCode;}
 get environment(){return this.#environment;}
 async #session(path,action,options){return sessionDto(await this.#http.request('/v1/web/sessions/'+path,options),action);}
 loginPassword(loginId,password,options={}){return this.#session('password','login',{...options,method:'POST',body:{loginId,password}});}
 loginGoogle(idToken,options={}){return this.#session('google','login',{...options,method:'POST',body:{idToken}});}
 async #community(input,operation,{csrfToken,signal}={}){
  const command=communityCommand(input,operation);
  requireInput(text(csrfToken,256)&&csrfToken.length>0);
  const {contentId,...body}=command;
  const v=await this.#http.request('/v1/web/community/contents/'+contentId+'/'+operation,{method:'POST',body,csrfToken,signal,communityVersion:operation==='edit'?editVersion:deleteVersion});
  return communityReceipt(v,command,operation);
 }
 deleteCommunityContent(input,options){return this.#community(input,'delete',options);}
 editCommunityContent(input,options){return this.#community(input,'edit',options);}
 async createCommunityComment(input,{csrfToken,signal}={}){
  const command=commentCommand(input);requireInput(text(csrfToken,256)&&csrfToken.length>0);
  const v=await this.#http.request('/v1/web/community/contents/'+command.postId+'/comments',{method:'POST',body:{commandId:command.commandId,markdown:command.markdown},csrfToken,signal,communityVersion:commentVersion});
  return commentReceipt(v,command);
 }
 async ownCommunityContent(contentId,options={}){
  requireInput(typeof contentId==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(contentId));
  return ownDetail(await this.#http.request('/v1/web/community/own/contents/'+contentId,{signal:options.signal,communityRead:'detail'}),contentId);
 }
 async ownCommunityComments(filters={},options={}){
  return ownComments(await this.#http.request('/v1/web/community/own/comments?'+this.#page(filters),{signal:options.signal,communityRead:'list'}));
 }
 async createCommunityPost(input,{csrfToken,signal}={}){
  const command=postCommand(input);requireInput(text(csrfToken,256)&&csrfToken.length>0);
  const v=await this.#http.request('/v1/web/community/posts',{method:'POST',body:command,csrfToken,signal,communityVersion:postVersion});
  return postReceipt(v,command);
 }
 async ownCommunityPosts(filters={},options={}){
  requireInput(filters&&typeof filters==='object'&&!Array.isArray(filters)&&Object.keys(filters).every(k=>['limit','cursor','boardId'].includes(k))&&(filters.boardId===undefined||['free','qna','guide_build','feedback'].includes(filters.boardId)));
  const params=this.#page(filters);if(filters.boardId!==undefined)params.set('boardId',filters.boardId);
  return ownPosts(await this.#http.request('/v1/web/community/own/posts?'+params,{signal:options.signal,communityRead:'posts'}),filters.boardId);
 }
 async setCommunityBlock(input,{csrfToken,signal}={}){
  const command=blockCommand(input);requireInput(text(csrfToken,256)&&csrfToken.length>0);
  return blockReceipt(await this.#http.request('/v1/web/community/blocks',{method:'POST',body:command,csrfToken,signal,communityBlock:true}),command);
 }
 async communityBlocks(filters={},options={}){
  requireInput(filters&&typeof filters==='object'&&!Array.isArray(filters)&&Object.keys(filters).every(k=>['limit','cursor'].includes(k)));
  return blockList(await this.#http.request('/v1/web/community/blocks?'+this.#page(filters),{signal:options.signal,communityBlock:true}));
 }
 #publicFilters(filters,posts){
  requireInput(filters&&typeof filters==='object'&&!Array.isArray(filters)&&Object.keys(filters).every(k=>(posts?['limit','cursor','boardId']:['limit','cursor']).includes(k))&&(filters.cursor===undefined||filters.cursor===null||typeof filters.cursor==='string'&&filters.cursor.length<=1024)&&(filters.boardId===undefined||['free','qna','guide_build','feedback'].includes(filters.boardId)));
  const params=this.#page(filters);if(filters.boardId!==undefined)params.set('boardId',filters.boardId);return params;
 }
 async publicCommunityContent(id,options={}){
  requireInput(typeof id==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(id));
  return publicDetail(await this.#http.request('/v1/web/community/contents/'+id,{signal:options.signal,communityPublic:true}),id);
 }
 async publicCommunityPosts(filters={},options={}){
  return publicPage(await this.#http.request('/v1/web/community/posts?'+this.#publicFilters(filters,true),{signal:options.signal,communityPublic:true}),{kind:'POST',boardId:filters.boardId});
 }
 async publicCommunityComments(postId,filters={},options={}){
  requireInput(typeof postId==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(postId));
  return publicPage(await this.#http.request('/v1/web/community/contents/'+postId+'/comments?'+this.#publicFilters(filters,false),{signal:options.signal,communityPublic:true}),{kind:'COMMENT',parentId:postId});
 }
 async communityConsent(options={}){return consentCurrent(await this.#http.request('/v1/web/community/consent',{signal:options.signal,communityConsent:true}),this.#environment);}
 async acceptCommunityConsent(input,{csrfToken,signal}={}){const command=await verifiedConsentCommand(input,this.#environment);requireInput(text(csrfToken,256)&&csrfToken.length>0);signal?.throwIfAborted();return consentReceipt(await this.#http.request('/v1/web/community/consent/accept',{method:'POST',body:command,csrfToken,signal,communityConsent:true}),command);}
 async submitCommunityReport(input,{csrfToken,signal}={}){const command=reportCommand(input);requireInput(text(csrfToken,256)&&csrfToken.length>0);return reportReceipt(await this.#http.request('/v1/web/community/reports',{method:'POST',body:command,csrfToken,signal,communityReport:'submit'}),command);}
 async ownCommunityReport(caseId,options={}){requireInput(typeof caseId==='string'&&/^[A-Za-z0-9_-]{1,128}$/.test(caseId));return ownReport(await this.#http.request('/v1/web/community/own/reports/'+caseId,{signal:options.signal,communityReport:'read'}),caseId);}
 async maintenanceLogin(options){return maintenanceLoginDto(await this.#http.request('/v1/public/maintenance/login',{signal:options?.signal,anonymous:true}));}
 me(options){return this.#session('me','me',options);}
 csrf(options){return this.#session('csrf','csrf',{...options,method:'POST'});}
 async logout(csrfToken,options){requireInput(text(csrfToken,256)&&csrfToken.length>0);return this.#session('logout','logout',{...options,method:'POST',csrfToken});}
 #page({limit=20,cursor=null}={}){requireInput(integer(limit,1)&&limit<=50&&(cursor===null||text(cursor,4096)&&cursor.length>0));const params=new URLSearchParams({limit:String(limit)});if(cursor!==null)params.set('cursor',cursor);return params;}
 #id(value,prefix){requireInput(typeof value==='string'&&new RegExp('^'+prefix+'_[a-f0-9]{64}$').test(value));return value;}
 #noticeFilters({locale='ko',channel,platform,versionCode}={}){requireInput(text(locale,32)&&locale.length>0);const p=new URLSearchParams({locale});for(const [key,value] of Object.entries({channel,platform,versionCode})){if(value!==undefined){requireInput(key==='versionCode'?integer(value,1)&&value<=2147483647:text(value,64)&&value.length>0);p.set(key,String(value));}}return p;}
 async notices(filters={},options){const params=this.#noticeFilters(filters);for(const [k,v] of this.#page(filters))params.set(k,v);return pageDto(await this.#http.request('/v1/public/notices?'+params,options),'public-notice-list-v1',noticeDto,{environment:this.#environment});}
 async notice(noticeId,filters={},options){requireInput(typeof noticeId==='string'&&/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/.test(noticeId));const v=await this.#http.request('/v1/public/notices/'+noticeId+'?'+this.#noticeFilters(filters),options);requireResponse(v.contractVersion==='public-notice-v1'&&v.environment===this.#environment&&integer(v.serverTime));const notice=noticeDto(v.notice);requireResponse(notice.noticeId===noticeId);return Object.freeze({contractVersion:v.contractVersion,environment:v.environment,serverTime:v.serverTime,notice});}
 async probabilities(filters,options){return pageDto(await this.#http.request('/v1/public/probability-disclosures?'+this.#page(filters),options),'public-probability-list-v1',v=>disclosureDto(v,this.#environment),{environment:this.#environment,scope:'PUBLISHED_HISTORY'});}
 async probability(id,options){const dto=disclosureDto(await this.#http.request('/v1/public/probability-disclosures/'+this.#id(id,'pd'),options),this.#environment);requireResponse(dto.disclosureId===id);return dto;}
 async currentRelease({channel='PUBLIC',versionCode=this.#version}={},options){requireInput(text(channel,64)&&channel.length>0&&channel!=='INTERNAL'&&integer(versionCode,1)&&versionCode<=2147483647&&(this.#environment!=='DEV'||versionCode===this.#version));const p=new URLSearchParams({channel,versionCode:String(versionCode)});const v=await this.#http.request('/v1/public/releases/current?'+p,options);const index=indexDto(v.index,this.#environment,this.#version);requireResponse(index.channel===channel&&index.appVersionCode===String(versionCode));return Object.freeze({index});}
 async release(id,options){const v=await this.#http.request('/v1/public/releases/'+this.#id(id,'pri'),options);const index=indexDto(v.index,this.#environment,this.#version);requireResponse(index.indexId===id);return Object.freeze({index});}
 async catalog(id,options){const v=await this.#http.request('/v1/public/catalogs/'+this.#id(id,'pcs'),options);const snapshot=catalogDto(v.snapshot,this.#environment,this.#version);requireResponse(snapshot.snapshotId===id);return Object.freeze({snapshot});}
 async tickets(filters,options){return pageDto(await this.#http.request('/v1/web/support/tickets?'+this.#page(filters),options),'private-support-list-v1',v=>ticketDto(v));}
 async ticket(id,options){requireInput(text(id,256)&&id.length>0);const v=await this.#http.request('/v1/web/support/tickets/'+encodeURIComponent(id),options);requireResponse(v.contractVersion==='private-support-ticket-v1');const ticket=ticketDto(v.ticket,true);requireResponse(ticket.ticketId===id);return Object.freeze({contractVersion:v.contractVersion,ticket});}
 async submitTicket(input,{key,csrfToken,signal}={}){
  requireInput(input&&typeof input==='object'&&!Array.isArray(input)&&Object.keys(input).every(k=>['category','body','appVersion','appVersionCode','clientComposedAt','clientSubmittedAt'].includes(k))&&categories.includes(input.category)&&text(input.body)&&input.body.trim().length>=10&&text(csrfToken,256)&&csrfToken.length>0);
  requireInput(typeof key==='string'&&/^[\x21-\x7e]{16,128}$/.test(key));
  return submissionDto(await this.#http.request('/v1/web/support/tickets',{method:'POST',body:input,key,csrfToken,signal}));
 }
 async receipt(key,options){requireInput(typeof key==='string'&&/^[\x21-\x7e]{16,128}$/.test(key));return receiptDto(await this.#http.request('/v1/web/support/tickets/receipt',{...options,key}));}
}









