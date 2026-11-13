import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityPostModel} from '../src/community-post-model.mjs';
const version='community.post-create.v1.20261008',renderer='community.markdown.v1.20261008';
const input={commandId:'post-1',boardId:'free',title:'제목',markdown:'본문'};
const receipt={contractVersion:version,rendererVersion:renderer,commandId:'post-1',contentId:'new-post',parentId:null,boardId:'free',detailsHash:'a'.repeat(64),kind:'POST',revision:1,visibility:'VISIBLE',createdAt:1};
const response=(v,status=200,headers={})=>new Response(JSON.stringify(v),{status,headers});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},csrfToken:async()=> 'csrf',expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('four stable boards exact POST/header and semantic defaults',async()=>{
 for(const boardId of ['free','qna','guide_build','feedback']){
 let sent;const v=await api(async(url,o)=>{sent={url,o};return response({...receipt,boardId});}).createCommunityPost({...input,boardId},{csrfToken:'csrf'});
 assert.equal(sent.url,'https://api.test/v1/web/community/posts');assert.equal(sent.o.headers.get('X-Community-Content-Version'),version);
 assert.deepEqual(JSON.parse(sent.o.body),{...input,boardId,spoiler:false,wikiRefs:[],sourceBalanceVersion:null});
 assert.equal(v.kind,'POST');assert.equal(Object.hasOwn(v,'markdown'),false);assert.equal(sent.o.cache,'no-store');}
});
test('post Unicode limits and server-owned input fields rejected',async()=>{
 const c=api(async()=>response(receipt));await c.createCommunityPost({...input,title:'😀'.repeat(100),markdown:'😀'.repeat(10000)},{csrfToken:'csrf'});
 for(const v of [{...input,boardId:'自由'},{...input,title:' '},{...input,markdown:' '},{...input,title:'😀'.repeat(101)},{...input,markdown:'😀'.repeat(10001)},{...input,actor:'A'}]){
 await assert.rejects(c.createCommunityPost(v,{csrfToken:'csrf'}),e=>e.kind==='invalid');}
});
test('fixed references preserved and never invented',async()=>{
 const ref={snapshotId:'pcs_'+'b'.repeat(64),itemId:'actualPublicId'};let sent;
 await api(async(_,o)=>{sent=JSON.parse(o.body);return response(receipt);}).createCommunityPost({...input,wikiRefs:[ref],sourceBalanceVersion:'balance-1'},{csrfToken:'csrf'});
 assert.deepEqual(sent.wikiRefs,[ref]);assert.equal(sent.sourceBalanceVersion,'balance-1');
 for(const refs of [[{snapshotId:'current',itemId:'name'}],[ref,ref],[{...ref,name:'invented'}]]){
 await assert.rejects(api(async()=>response(receipt)).createCommunityPost({...input,wikiRefs:refs},{csrfToken:'csrf'}),e=>e.kind==='invalid');}
});
test('body-free receipt metadata/schema fails closed',async()=>{
 for(const v of [{...receipt,boardId:'other'},{...receipt,parentId:'parent'},{...receipt,detailsHash:'unknown'},{...receipt,title:'private'},{...receipt,revision:2}]){
 await assert.rejects(api(async()=>response(v)).createCommunityPost(input,{csrfToken:'csrf'}),e=>e.kind==='invalid-response');}
});
test('omitted and explicit defaults share pending command promise',async()=>{
 let finish,calls=0;const m=new CommunityPostModel({createCommunityPost:()=>{calls++;return new Promise(r=>finish=r);}},session());
 const p=m.execute(input);assert.equal(m.execute({...input,spoiler:false,wikiRefs:[],sourceBalanceVersion:null}),p);
 await Promise.resolve();finish(receipt);await p;assert.equal(calls,1);m.dispose();
});
test('response loss freezes nested wiki refs and original key',async()=>{
 const ref={snapshotId:'pcs_'+'b'.repeat(64),itemId:'original'},v={...input,wikiRefs:[ref]};let calls=0,sent=[];
 const m=new CommunityPostModel({createCommunityPost:async c=>{sent.push(c);if(++calls===1)throw {kind:'network'};return receipt;}},session());
 await m.execute(v);ref.itemId='changed';await m.retry();assert.equal(sent[1].wikiRefs[0].itemId,'original');assert.equal(sent[1].commandId,input.commandId);m.dispose();
});
test('post429 delay and wiki-specific failures do not block unrelated plaintext',async()=>{
 let now=0,calls=0;const m=new CommunityPostModel(api(async()=>{if(++calls===1)return response({error:'community_rate_limited',retryAfter:60},429,{'Retry-After':'60'});return response(receipt);}),session(),{now:()=>now});
 await m.execute(input);assert.equal(m.state.retryAt,60000);await m.retry();assert.equal(calls,1);now=60000;await m.retry();assert.equal(m.state.phase,'created');m.dispose();
 for(const [status,code,reason] of [[400,'community_wiki_reference_unavailable','wiki-reference-unavailable'],[503,'community_wiki_source_unavailable','wiki-source-unavailable']]){
 const bad=new CommunityPostModel(api(async()=>response({error:code},status)),session());await bad.execute(input);assert.equal(bad.state.error.reason,reason);assert.equal(bad.state.receipt,null);bad.dispose();}
 const plain=new CommunityPostModel(api(async()=>response(receipt)),session());await plain.execute(input);assert.equal(plain.state.phase,'created');plain.dispose();
});
test('post account transition aborts late original receipt',async()=>{
 let signal,finish;const s=session(),m=new CommunityPostModel({createCommunityPost:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=m.execute(input);await Promise.resolve();s.set({phase:'anonymous',accountId:null});assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(m.state.receipt,null);m.dispose();
});
test('receipt does not fill current read body or consent state',async()=>{
 const m=new CommunityPostModel(api(async()=>response({...receipt,visibility:'REVIEW_PENDING'})),session());
 await m.execute(input);assert.equal(Object.hasOwn(m.state,'currentContent'),false);assert.equal(Object.hasOwn(m.state,'consent'),false);assert.equal(Object.hasOwn(m.state.receipt,'sanitizedBody'),false);m.dispose();
});

test('post128000 byte budget cannot expand via large reference envelope',async()=>{
 let calls=0;const c=api(async()=>{calls++;return response(receipt);});
 const wikiRefs=Array.from({length:700},(_,i)=>({snapshotId:'pcs_'+'b'.repeat(64),itemId:('id'+i).padEnd(256,'a')}));
 await assert.rejects(c.createCommunityPost({...input,wikiRefs},{csrfToken:'csrf'}),e=>e.kind==='invalid');assert.equal(calls,0);
});
test('after original receipt, ownGET is separately current and may be deleted',async()=>{
 const c=api(async(url)=>url.endsWith('/posts')?response(receipt):response({contractVersion:'community.own-content-read.v1.20261008',rendererVersion:null,content:{kind:'POST',id:'new-post',parentId:null,revision:3,visibility:'AUTHOR_DELETED',commentLocked:true,title:null,sanitizedBody:null,updatedAt:null}}));
 const m=new CommunityPostModel(c,session());await m.execute(input);const current=await c.ownCommunityContent(m.state.receipt.contentId);
 assert.equal(m.state.receipt.visibility,'VISIBLE');assert.equal(current.content.visibility,'AUTHOR_DELETED');assert.equal(current.content.sanitizedBody,null);m.dispose();
});
