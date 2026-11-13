import {requireResponse as check} from './api-http.mjs';
export const text=(v,max=2000)=>typeof v==='string'&&v.length<=max;
export const integer=(v,min=0)=>Number.isSafeInteger(v)&&v>=min;
const object=v=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const iso=v=>typeof v==='string'&&Number.isFinite(Date.parse(v));
const id=(v,prefix)=>typeof v==='string'&&new RegExp('^'+prefix+'_[a-f0-9]{64}$').test(v);
const nullable=(v,valid)=>v===null||valid(v);
export const categories=['ACCOUNT_LOGIN','RUN_REWARD','GACHA_RECRUITMENT','STORY_PROGRESS','BUG_OTHER'];
export function sessionDto(v,action){
 check(v.contractVersion==='web-session-v1');
 if(action==='login'){check(text(v.csrfToken,256)&&v.csrfToken.length>0&&iso(v.expiresAt));return Object.freeze({contractVersion:v.contractVersion,csrfToken:v.csrfToken,expiresAt:v.expiresAt});}
 if(action==='me'){check(text(v.accountId,256)&&v.accountId.length>0&&iso(v.expiresAt));return Object.freeze({contractVersion:v.contractVersion,accountId:v.accountId,expiresAt:v.expiresAt});}
 if(action==='csrf'){check(text(v.csrfToken,256)&&v.csrfToken.length>0);return Object.freeze({contractVersion:v.contractVersion,csrfToken:v.csrfToken});}
 check(v.loggedOut===true);return Object.freeze({contractVersion:v.contractVersion,loggedOut:true});
}
export function ticketDto(v,detail=false){
 check(object(v)&&text(v.ticketId,256)&&v.ticketId.length>0&&categories.includes(v.category)&&['OPEN','IN_REVIEW','CLOSED'].includes(v.status)&&text(v.bodyPreview)&&iso(v.serverReceivedAt)&&integer(v.revision)&&nullable(v.publicReply,text));
 if(detail)check(text(v.body));
 return Object.freeze({ticketId:v.ticketId,category:v.category,status:v.status,bodyPreview:v.bodyPreview,serverReceivedAt:v.serverReceivedAt,revision:v.revision,publicReply:v.publicReply,...(detail?{body:v.body}:{})});
}
export function pageDto(v,contract,map,{environment,scope}={}){
 check(v.contractVersion===contract&&Array.isArray(v.items)&&v.items.length<=50&&nullable(v.nextCursor,c=>text(c,4096)&&c.length>0));
 if(environment!==undefined)check(v.environment===environment&&integer(v.serverTime));
 if(scope!==undefined)check(v.scope===scope);
 return Object.freeze({contractVersion:contract,...(environment===undefined?{}:{environment,serverTime:v.serverTime}),...(scope===undefined?{}:{scope}),items:Object.freeze(v.items.map(map)),nextCursor:v.nextCursor});
}
export function noticeDto(v){
 check(object(v)&&typeof v.noticeId==='string'&&/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/.test(v.noticeId)&&integer(v.revision,1)&&integer(v.presentationRevision,1)&&text(v.title,200)&&text(v.summary,500)&&text(v.body,6000)&&text(v.locale,32)&&v.bodyFormat==='PLAIN_TEXT'&&integer(v.updatedAt)&&integer(v.startsAt)&&nullable(v.endsAt,integer)&&Number.isSafeInteger(v.priority)&&v.priority>=-1000&&v.priority<=1000&&typeof v.pinned==='boolean');
 // Display consumes text; never execute body as HTML or server-supplied actions.
 return Object.freeze({noticeId:v.noticeId,revision:v.revision,presentationRevision:v.presentationRevision,type:v.type,title:v.title,summary:v.summary,body:v.body,locale:v.locale,bodyFormat:v.bodyFormat,startsAt:v.startsAt,endsAt:v.endsAt,updatedAt:v.updatedAt,priority:v.priority,pinned:v.pinned});
}
export function disclosureDto(v,environment){
 check(object(v)&&v.contractVersion==='probability-disclosure-v1'&&v.environment===environment&&id(v.disclosureId,'pd')&&integer(v.publishedAt)&&[1,2].includes(v.publicSchemaVersion)&&['OFFLINE','COMBAT'].includes(v.consumer)&&text(v.policyVersion,256)&&/^sha256:[a-f0-9]{64}$/.test(v.sourceDigest)&&v.scope==='MULTIPLIER_DISTRIBUTION'&&Array.isArray(v.distribution)&&v.distribution.length===3);
 check(v.publicSchemaVersion===1?v.sourceKind===undefined:v.sourceKind==='PURCHASE_POLICY');
 let sum=0;const distribution=v.distribution.map((row,i)=>{check(object(row)&&row.multiplier===i+2&&integer(row.numerator)&&row.numerator<=10000&&row.denominator===10000&&row.percentage===Math.floor(row.numerator/100)+'.'+String(row.numerator%100).padStart(2,'0'));sum+=row.numerator;return Object.freeze({multiplier:row.multiplier,numerator:row.numerator,denominator:10000,percentage:row.percentage});});check(sum===10000);
 return Object.freeze({contractVersion:v.contractVersion,environment,consumer:v.consumer,policyVersion:v.policyVersion,scope:v.scope,sourceDigest:v.sourceDigest,publicSchemaVersion:v.publicSchemaVersion,...(v.publicSchemaVersion===2?{sourceKind:v.sourceKind}:{}),distribution:Object.freeze(distribution),disclosureId:v.disclosureId,publishedAt:v.publishedAt});
}
export function indexDto(v,environment,versionCode){
 check(object(v)&&v.contractVersion==='public-release-index-v1'&&v.environment===environment&&text(v.channel,64)&&v.channel!=='INTERNAL'&&/^[1-9][0-9]{0,9}$/.test(v.appVersionCode)&&Number(v.appVersionCode)<=2147483647&&(environment!=='DEV'||v.appVersionCode===String(versionCode))&&integer(v.revision,1)&&text(v.releaseId,256)&&id(v.snapshotId,'pcs')&&id(v.indexId,'pri')&&nullable(v.previousIndexId,x=>id(x,'pri'))&&integer(v.publishedAt)&&Array.isArray(v.disclosureIds)&&v.disclosureIds.length<=100&&v.disclosureIds.every(x=>id(x,'pd'))&&new Set(v.disclosureIds).size===v.disclosureIds.length);
 return Object.freeze({contractVersion:v.contractVersion,environment,channel:v.channel,appVersionCode:v.appVersionCode,revision:v.revision,releaseId:v.releaseId,snapshotId:v.snapshotId,disclosureIds:Object.freeze([...v.disclosureIds]),publishedAt:v.publishedAt,previousIndexId:v.previousIndexId,indexId:v.indexId});
}
export function catalogDto(v,environment,versionCode){
 check(object(v)&&v.contractVersion==='public-catalog-snapshot-v1'&&v.environment===environment&&v.publicSchemaVersion===1&&id(v.snapshotId,'pcs')&&text(v.channel,64)&&v.channel!=='INTERNAL'&&/^[1-9][0-9]{0,9}$/.test(v.appVersionCode)&&(environment!=='DEV'||v.appVersionCode===String(versionCode))&&text(v.sourceBalanceVersion,256)&&/^sha256:[a-f0-9]{64}$/.test(v.sourceDigest)&&text(v.contentReleaseId,256)&&text(v.templateVersion,256)&&text(v.localeVersion,256)&&Array.isArray(v.items)&&v.items.length>0&&v.items.length<=10000);
 const items=v.items.map(row=>{check(object(row)&&text(row.id,256)&&text(row.name)&&text(row.description)&&text(row.category)&&text(row.unit)&&['AUGMENT','EQUIPMENT','UNIQUE_EFFECT','SET','ENCHANTMENT','OPERATOR','TURRET','COMMANDER','MAP','COMBAT'].includes(row.kind)&&nullable(row.stackLimit,integer)&&nullable(row.iconPath,p=>typeof p==='string'&&/^\/assets\/[A-Za-z0-9/_-]+\.(png|webp|svg)$/.test(p)));
  for(const field of ['values','conditions','compatibleWith','acquisition'])check(Array.isArray(row[field])&&row[field].length<=(field==='compatibleWith'?256:64));
  check(row.conditions.every(x=>text(x))&&row.compatibleWith.every(x=>text(x,256))&&row.acquisition.every(x=>text(x)));
  const values=row.values.map(value=>{check(object(value)&&text(value.label)&&text(value.value));return Object.freeze({label:value.label,value:value.value});});
  return Object.freeze({id:row.id,kind:row.kind,name:row.name,description:row.description,category:row.category,unit:row.unit,values:Object.freeze(values),conditions:Object.freeze([...row.conditions]),stackLimit:row.stackLimit,compatibleWith:Object.freeze([...row.compatibleWith]),acquisition:Object.freeze([...row.acquisition]),iconPath:row.iconPath});});
 check(new Set(items.map(row=>row.id)).size===items.length);
 return Object.freeze({contractVersion:v.contractVersion,snapshotId:v.snapshotId,environment,channel:v.channel,appVersionCode:v.appVersionCode,publicSchemaVersion:1,sourceBalanceVersion:v.sourceBalanceVersion,sourceDigest:v.sourceDigest,contentReleaseId:v.contentReleaseId,templateVersion:v.templateVersion,localeVersion:v.localeVersion,items:Object.freeze(items)});
}
export function submissionDto(v){
 check(v.accepted===true&&['RECEIVED','DUPLICATE_REPLAY'].includes(v.ticketStatus)&&text(v.ticketId,256)&&categories.includes(v.category)&&text(v.bodyPreview)&&nullable(v.appVersion,x=>text(x,64))&&nullable(v.appVersionCode,x=>typeof x==='string'&&/^\d{1,19}$/.test(x))&&nullable(v.clientComposedAt,iso)&&nullable(v.clientSubmittedAt,iso)&&iso(v.serverReceivedAt)&&nullable(v.retryAfterSeconds,integer)&&nullable(v.errorCode,x=>text(x,100))&&nullable(v.message,text));
 return Object.freeze(Object.fromEntries(['accepted','ticketStatus','ticketId','category','bodyPreview','appVersion','appVersionCode','clientComposedAt','clientSubmittedAt','serverReceivedAt','retryAfterSeconds','errorCode','message'].map(k=>[k,v[k]])));
}

export function receiptDto(v){
 check(v.accepted===true&&v.receiptStatus==='FOUND'&&text(v.accountId,256)&&v.accountId.length>0);
 const submission=submissionDto({...v,ticketStatus:'RECEIVED',retryAfterSeconds:null});
 const {ticketStatus,retryAfterSeconds,...rest}=submission;
 return Object.freeze({...rest,receiptStatus:'FOUND'});
}


export function maintenanceLoginDto(v){
 const keys=['contractVersion','serverTime','policyRevision','gameLoginAllowed','webLoginAllowed','title','summary','startsAt','estimatedEndAt','retryAfterSeconds'];
 check(object(v)&&Object.keys(v).sort().join(',')===[...keys].sort().join(',')&&v.contractVersion==='maintenance-login-v1'&&integer(v.serverTime)&&integer(v.policyRevision,1)&&typeof v.gameLoginAllowed==='boolean'&&typeof v.webLoginAllowed==='boolean');
 const plain=(value,max)=>text(value,max)&&value.trim().length>0&&!/[<>\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(value);
 const normal=v.title===null&&v.summary===null&&v.startsAt===null&&v.estimatedEndAt===null&&v.retryAfterSeconds===null;
 check(normal?v.gameLoginAllowed&&v.webLoginAllowed:plain(v.title,200)&&plain(v.summary,500)&&integer(v.startsAt)&&nullable(v.estimatedEndAt,x=>integer(x)&&x>v.startsAt)&&integer(v.retryAfterSeconds,1)&&v.retryAfterSeconds<=9007199254740);
 return Object.freeze(Object.fromEntries(keys.map(key=>[key,v[key]])));
}
