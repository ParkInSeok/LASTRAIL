import test from 'node:test';
import assert from 'node:assert/strict';
import * as api from '../src/web-api.mjs';
const response=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'Content-Type':'application/json'}});
const setup=(handler)=>new api.WebApi({origin:'https://api.example.test',environment:'DEV',versionCode:123,fetch:handler});
const ticket={ticketId:'ticket-a',category:'BUG_OTHER',status:'CLOSED',bodyPreview:'a valid inquiry',serverReceivedAt:'2026-10-01T00:00:00.000Z',revision:2,publicReply:'latest public reply'};
const session={contractVersion:'web-session-v1',csrfToken:'csrf-test',expiresAt:'2026-10-01T01:00:00.000Z'};


test('configuration rejects HTTP, credential URLs, paths and invalid DEV version',()=>{
 for(const origin of ['http://api.example.test','https://user:pass@api.example.test','https://api.example.test/path'])assert.throws(()=>new api.WebApi({origin,environment:'DEV',versionCode:123}));
 assert.throws(()=>new api.WebApi({origin:'https://api.example.test',environment:'DEV',versionCode:0}));
});
test('password login uses cookie credentials and keeps no bearer or password copy',async()=>{
 let request;const client=setup(async(url,options)=>{request={url,options};return response(session);});
 assert.deepEqual(await client.loginPassword('user','password'),session);
 assert.equal(request.url,'https://api.example.test/v1/web/sessions/password');
 assert.equal(request.options.credentials,'include');assert.equal(request.options.cache,'no-store');
 assert.equal(request.options.headers.has('Authorization'),false);
 assert.deepEqual(JSON.parse(request.options.body),{loginId:'user',password:'password'});
});
test('public history has explicit scope, opaque cursor and exact DEV header',async()=>{
 let sent;const client=setup(async(url,o)=>{sent={url,o};return response({contractVersion:'public-probability-list-v1',environment:'DEV',scope:'PUBLISHED_HISTORY',serverTime:1,items:[],nextCursor:'DEV.pd_cursor'});});
 const result=await client.probabilities({cursor:'DEV.pd_previous'});
 assert.equal(result.scope,'PUBLISHED_HISTORY');assert.equal(new URL(sent.url).searchParams.get('cursor'),'DEV.pd_previous');
 assert.equal(sent.o.headers.get('X-LastRail-App-Version-Code'),'123');
});
test('fixed probability ID receives no unsupported query',async()=>{
 const id='pd_'+'a'.repeat(64);let url;const client=setup(async u=>{url=u;return response({error:'probability_disclosure_not_found'},404);});
 await assert.rejects(client.probability(id),e=>e.kind==='not-found'&&e.status===404);
 assert.equal(url,'https://api.example.test/v1/public/probability-disclosures/'+id);
});
test('HTTP errors retain safe code and actionable kind without raw response content',async()=>{
 for(const [status,kind] of [[400,'invalid'],[401,'unauthenticated'],[403,'forbidden'],[404,'not-found'],[409,'conflict'],[429,'rate-limited'],[503,'unavailable']]){
 const client=setup(async()=>response({error:'web_csrf_forbidden',message:'private body'},status));
 await assert.rejects(client.me(),e=>e.kind===kind&&e.status===status&&e.code==='web_csrf_forbidden'&&!e.message.includes('private body'));
 }
});
test('wrong contracts and environment are rejected before consumption',async()=>{
 for(const value of [{contractVersion:'future-version'},{contractVersion:'public-notice-list-v1',environment:'PRD',serverTime:1,items:[],nextCursor:null}]){
 await assert.rejects(setup(async()=>response(value)).notices(),e=>e.kind==='invalid-response');
 }
});
test('public notice cursor is used only with explicitly supplied filter selection',async()=>{
 let url;const client=setup(async u=>{url=u;return response({contractVersion:'public-notice-list-v1',environment:'DEV',serverTime:1,items:[],nextCursor:null});});
 await client.notices({locale:'ko',channel:'PUBLIC',cursor:'opaque+/='});
 assert.equal(new URL(url).searchParams.get('cursor'),'opaque+/=');assert.equal(new URL(url).searchParams.get('locale'),'ko');
 await client.notices({locale:'ko',channel:'OTHER'});assert.equal(new URL(url).searchParams.has('cursor'),false);
});
test('support projection preserves latest reply and CLOSED without claiming resolution; internal fields discarded',async()=>{
 const client=setup(async()=>response({contractVersion:'private-support-ticket-v1',ticket:{...ticket,body:'full inquiry body',internalNote:'PRIVATE',assigneePrincipalId:'PRIVATE'}}));
 const value=await client.ticket('ticket-a');assert.equal(value.ticket.status,'CLOSED');assert.equal(value.ticket.publicReply,'latest public reply');
 assert.equal(value.ticket.internalNote,undefined);assert.equal(value.ticket.assigneePrincipalId,undefined);assert.equal(value.ticket.resolved,undefined);
});
test('historical CLOSED ticket without reply remains readable',async()=>{
 const client=setup(async()=>response({contractVersion:'private-support-list-v1',items:[{...ticket,publicReply:null}],nextCursor:null}));
 assert.equal((await client.tickets()).items[0].publicReply,null);
});
test('submission and receipt reuse the same caller-owned original key without automatic retries',async()=>{
 const sent=[];const client=setup(async(url,o)=>{sent.push({url,o});if(sent.length===1)throw new TypeError('lost');return response({accepted:true,receiptStatus:'FOUND',accountId:'owner-a',ticketId:'ticket-a',category:'BUG_OTHER',bodyPreview:'a valid inquiry',appVersion:null,appVersionCode:null,clientComposedAt:null,clientSubmittedAt:null,serverReceivedAt:'2026-10-01T00:00:00.000Z',retryAfterSeconds:null,errorCode:null,message:null});});
 const key='original-key-0123456789';
 await assert.rejects(client.submitTicket({category:'BUG_OTHER',body:'a valid inquiry'}, {key,csrfToken:'csrf-test'}),e=>e.kind==='network');
 assert.equal(sent.length,1);const receipt=await client.receipt(key);assert.equal(receipt.ticketId,'ticket-a');
 assert.equal(sent[0].o.headers.get('Idempotency-Key'),key);assert.equal(sent[1].o.headers.get('Idempotency-Key'),key);
 assert.equal(sent[0].o.headers.get('X-CSRF-Token'),'csrf-test');assert.equal(sent[1].o.body,undefined);
});
test('invalid support inputs and missing CSRF never reach network',async()=>{
 let count=0;const client=setup(async()=>{count++;return response({});});
 for(const input of [{category:'ADMIN',body:'a valid inquiry'},{category:'BUG_OTHER',body:'short'},{category:'BUG_OTHER',body:'a valid inquiry',accountId:'other'}])await assert.rejects(client.submitTicket(input,{key:'original-key-0123456789',csrfToken:'csrf-test'}));
 await assert.rejects(client.logout(''));assert.equal(count,0);
});
test('caller abort cancels owned request, no retry',async()=>{
 let signal;const controller=new AbortController();const client=setup(async(_u,o)=>{signal=o.signal;return new Promise((_r,reject)=>o.signal.addEventListener('abort',()=>reject(o.signal.reason),{once:true}));});
 const pending=client.me({signal:controller.signal});controller.abort();await assert.rejects(pending,e=>e.name==='AbortError');assert.equal(signal.aborted,true);
});
test('malformed JSON is distinct from network failure',async()=>{
 await assert.rejects(setup(async()=>new Response('<html>',{status:200})).me(),e=>e.kind==='invalid-response');
});

const notice={noticeId:'notice-a',revision:1,presentationRevision:1,type:'GENERAL',title:'Public title',summary:'Public summary',body:'Plain text',locale:'ko',bodyFormat:'PLAIN_TEXT',startsAt:0,endsAt:null,updatedAt:1,priority:-1000,pinned:false,imageAssetId:null,action:null};
test('real server notice accepts negative priority and refuses unsupported rich body',async()=>{
 const envelope=n=>({contractVersion:'public-notice-v1',environment:'DEV',serverTime:1,notice:n});
 assert.equal((await setup(async()=>response(envelope(notice))).notice('notice-a')).notice.priority,-1000);
 await assert.rejects(setup(async()=>response(envelope({...notice,bodyFormat:'MARKDOWN'}))).notice('notice-a'),e=>e.kind==='invalid-response');
});
test('real receipt FOUND projection differs from POST, preserves string version and hides account',async()=>{
 const raw={accepted:true,receiptStatus:'FOUND',ticketId:'ticket-a',accountId:'owner-a',category:'BUG_OTHER',bodyPreview:'a valid inquiry',appVersion:null,appVersionCode:'123',clientComposedAt:null,clientSubmittedAt:null,serverReceivedAt:'2026-10-01T00:00:00.000Z',errorCode:null,message:null};
 const result=await setup(async()=>response(raw)).receipt('original-key-0123456789');
 assert.equal(result.receiptStatus,'FOUND');assert.equal(result.appVersionCode,'123');assert.equal(result.accountId,undefined);assert.equal(result.ticketStatus,undefined);
});
test('support rate limit uses server body delay and code without an invented public Retry-After',async()=>{
 const client=setup(async()=>response({errorCode:'support_ticket_rate_limited',retryAfterSeconds:60,message:'private'},429));
 await assert.rejects(client.submitTicket({category:'BUG_OTHER',body:'a valid inquiry'},{key:'original-key-0123456789',csrfToken:'csrf-test'}),e=>e.kind==='rate-limited'&&e.code==='support_ticket_rate_limited'&&e.retryAfterSeconds===60);
});
test('missing original key cannot silently submit a fresh ticket',async()=>{
 let calls=0;const client=setup(async()=>{calls++;return response({});});await assert.rejects(client.submitTicket({category:'BUG_OTHER',body:'a valid inquiry'},{csrfToken:'csrf-test'}));assert.equal(calls,0);
});


const pd='pd_'+'a'.repeat(64),pri='pri_'+'b'.repeat(64),pcs='pcs_'+'c'.repeat(64);
const disclosure={contractVersion:'probability-disclosure-v1',environment:'DEV',consumer:'OFFLINE',policyVersion:'policy-v1',scope:'MULTIPLIER_DISTRIBUTION',sourceDigest:'sha256:'+'d'.repeat(64),publicSchemaVersion:1,distribution:[{multiplier:2,numerator:6000,denominator:10000,percentage:'60.00'},{multiplier:3,numerator:3000,denominator:10000,percentage:'30.00'},{multiplier:4,numerator:1000,denominator:10000,percentage:'10.00'}],disclosureId:pd,publishedAt:1};
const index={contractVersion:'public-release-index-v1',environment:'DEV',channel:'PUBLIC',appVersionCode:'123',revision:1,releaseId:'release-v1',snapshotId:pcs,disclosureIds:[pd],publishedAt:1,previousIndexId:null,indexId:pri};
const snapshot={contractVersion:'public-catalog-snapshot-v1',environment:'DEV',channel:'PUBLIC',appVersionCode:'123',snapshotId:pcs,publicSchemaVersion:1,sourceBalanceVersion:'balance-v1',sourceDigest:'sha256:'+'d'.repeat(64),contentReleaseId:'release-v1',templateVersion:'template-v1',localeVersion:'ko-v1',items:[{id:'operator-a',kind:'OPERATOR',name:'Public operator',description:'Public description',category:'operator',unit:'none',values:[{label:'Public stat',value:'1'}],conditions:[],stackLimit:null,compatibleWith:[],acquisition:[],iconPath:null}]};
test('probability detail checks supported schema and exact distribution',async()=>{
 assert.equal((await setup(async()=>response(disclosure)).probability(pd)).distribution.length,3);
 for(const bad of [{...disclosure,publicSchemaVersion:3},{...disclosure,distribution:disclosure.distribution.map((r,i)=>({...r,numerator:i===0?6001:r.numerator}))},{...disclosure,disclosureId:'pd_'+'f'.repeat(64)}])await assert.rejects(setup(async()=>response(bad)).probability(pd),e=>e.kind==='invalid-response');
});
test('current index keeps disclosure links separate from history and rejects another app version',async()=>{
 let url;const result=await setup(async u=>{url=u;return response({index});}).currentRelease();assert.equal(result.index.disclosureIds[0],pd);assert.equal(new URL(url).searchParams.get('versionCode'),'123');
 await assert.rejects(setup(async()=>response({index:{...index,appVersionCode:'124'}})).currentRelease(),e=>e.kind==='invalid-response');
 await assert.rejects(setup(async()=>response({index})).currentRelease({versionCode:124}),e=>e.kind==='invalid');
});
test('fixed catalog projection is immutable and rejects missing fields or duplicate IDs',async()=>{
 const result=await setup(async()=>response({snapshot})).catalog(pcs);assert.equal(result.snapshot.contentReleaseId,'release-v1');assert.equal(Object.isFrozen(result.snapshot.items[0].values),true);
 for(const bad of [{...snapshot,publicSchemaVersion:2},{...snapshot,items:[...snapshot.items,...snapshot.items]},{...snapshot,items:[{...snapshot.items[0],values:[{label:'Public stat'}]}]}])await assert.rejects(setup(async()=>response({snapshot:bad})).catalog(pcs),e=>e.kind==='invalid-response');
});
test('real POST acceptance is distinct from receipt, rejected success-shaped response is not accepted',async()=>{
 const raw={accepted:true,ticketStatus:'RECEIVED',ticketId:'ticket-a',category:'BUG_OTHER',bodyPreview:'a valid inquiry',appVersion:null,appVersionCode:'123',clientComposedAt:null,clientSubmittedAt:null,serverReceivedAt:'2026-10-01T00:00:00.000Z',retryAfterSeconds:null,errorCode:null,message:null};
 const value=await setup(async()=>response(raw)).submitTicket({category:'BUG_OTHER',body:'a valid inquiry'},{key:'original-key-0123456789',csrfToken:'csrf-test'});assert.equal(value.ticketStatus,'RECEIVED');assert.equal(value.receiptStatus,undefined);
 await assert.rejects(setup(async()=>response({...raw,accepted:false})).submitTicket({category:'BUG_OTHER',body:'a valid inquiry'},{key:'original-key-0123456789',csrfToken:'csrf-test'}),e=>e.kind==='invalid-response');
});
test('bounded timeout aborts request and does not initiate another send',async()=>{
 let calls=0;let signal;const client=new api.WebApi({origin:'https://api.example.test',environment:'DEV',versionCode:123,timeoutMs:5,fetch:async(_u,o)=>{calls++;signal=o.signal;return new Promise((_r,reject)=>o.signal.addEventListener('abort',()=>reject(o.signal.reason),{once:true}));}});
 await assert.rejects(client.me(),e=>e.kind==='timeout');assert.equal(calls,1);assert.equal(signal.aborted,true);
});

test('session CORS contract receives no DEV tester header; public and support reads do',async()=>{
 const sent=[];const client=setup(async(u,o)=>{sent.push({u,o});return response(u.endsWith('/me')?{contractVersion:'web-session-v1',accountId:'a',expiresAt:'2099-01-01T00:00:00.000Z'}:session);});
 await client.loginPassword('user','password');await client.me();assert.equal(sent[0].o.headers.has('X-LastRail-App-Version-Code'),false);assert.equal(sent[1].o.headers.has('X-LastRail-App-Version-Code'),false);
});

