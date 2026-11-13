import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityPublicReadModel} from '../src/community-public-read-model.mjs';
const version='community.public-read.v1.20261008',scope='community.public-source.v1.20261008',renderer='community.markdown.v1.20261008';
const post={kind:'POST',id:'p',parentId:null,publicAuthorId:'opaque-author',revision:1,visibility:'VISIBLE',commentLocked:false,title:'title',sanitizedBody:'<p>body</p>',createdAt:1,updatedAt:1,boardId:'free',spoiler:false,wikiRefs:[],sourceBalanceVersion:null};
const comment={kind:'COMMENT',id:'c',parentId:'p',publicAuthorId:'opaque-author',revision:1,visibility:'VISIBLE',commentLocked:false,title:null,sanitizedBody:'<p>comment</p>',createdAt:1,updatedAt:1};
const page=items=>({contractVersion:version,publicScopeVersion:scope,items:items.map(v=>({...v,rendererVersion:renderer})),nextCursor:null});
const response=(v,status=200)=>new Response(JSON.stringify(v),{status,headers:{'Cache-Control':'no-store',Vary:'Origin, Cookie'}});
const api=(fetch,environment='DEV')=>new WebApi({origin:'https://api.test',environment,...(environment==='DEV'?{versionCode:123}:{}),fetch});
function session(phase='authenticated'){let state={phase,accountId:phase==='authenticated'?'A':null,expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('public detail exact header credentials include noCSRF and scope',async()=>{
 let sent;const v=await api(async(url,o)=>{sent={url,o};return response({contractVersion:version,publicScopeVersion:scope,rendererVersion:renderer,content:post});}).publicCommunityContent('p');
 assert.equal(sent.url,'https://api.test/v1/web/community/contents/p');assert.equal(sent.o.headers.get('X-Community-Public-Version'),version);assert.equal(sent.o.credentials,'include');assert.equal(sent.o.cache,'no-store');assert.equal(sent.o.headers.has('X-CSRF-Token'),false);assert.equal(v.publicScopeVersion,scope);
});
test('posts DESC/comments ASC binary and exact parent/board filters',async()=>{
 let sent;const c=api(async(url,o)=>{sent={url,o};return response(page([post]));});
 await c.publicCommunityPosts({boardId:'free',cursor:'opaque'});
 assert.equal(new URL(sent.url).searchParams.get('boardId'),'free');
 for(const items of [[{...post,createdAt:1},{...post,id:'second',createdAt:2,updatedAt:2}],[{...post,id:'Z'},{...post,id:'a'}]]){
 await assert.rejects(api(async()=>response(page(items))).publicCommunityPosts(),e=>e.kind==='invalid-response');}
 const comments=await api(async()=>response(page([comment,{...comment,id:'d',createdAt:2,updatedAt:2}]))).publicCommunityComments('p');assert.equal(comments.items.length,2);
 await assert.rejects(api(async()=>response(page([{...comment,parentId:'wrong'}]))).publicCommunityComments('p'),e=>e.kind==='invalid-response');
});
test('public onlyVISIBLE renderer/provenance/version/private fields strict',async()=>{
 for(const v of [{contractVersion:version,publicScopeVersion:'wrong',rendererVersion:renderer,content:post},{contractVersion:version,publicScopeVersion:scope,rendererVersion:'legacy',content:post},{contractVersion:version,publicScopeVersion:scope,rendererVersion:renderer,content:{...post,visibility:'HIDDEN'}},{contractVersion:version,publicScopeVersion:scope,rendererVersion:renderer,content:{...post,accountId:'private'}}]){
 await assert.rejects(api(async()=>response(v)).publicCommunityContent('p'),e=>e.kind==='invalid-response');}
});
test('public bounded1024 cursor/max50 unknownfilters/body/query scope reject',async()=>{
 const c=api(async()=>response(page([post])));
 for(const f of [{cursor:'x'.repeat(1025)},{limit:51},{actor:'A'}])await assert.rejects(c.publicCommunityPosts(f),e=>e.kind==='invalid');
 await assert.rejects(c.publicCommunityComments('p',{boardId:'free'}),e=>e.kind==='invalid');
});
test('PRD explicit anonymous read allowed; DEV requires session',async()=>{
 const anonymous=new CommunityPublicReadModel(api(async()=>response(page([post])),'PRD'),session('anonymous'));await anonymous.openPosts();assert.equal(anonymous.state.phase,'ready');anonymous.dispose();
 const dev=new CommunityPublicReadModel(api(async()=>response(page([post]))),session('anonymous'));assert.throws(()=>dev.openPosts(),e=>e.kind==='unauthenticated');dev.dispose();
});
test('expired cookie401 never falls back anonymous or resends',async()=>{
 let calls=0;const s=session(),m=new CommunityPublicReadModel(api(async()=>{calls++;return response({error:'web_session_required'},401);},'PRD'),s);
 await m.openPosts();assert.equal(calls,1);assert.equal(s.state.phase,'expired');assert.equal(m.state.phase,'authentication-required');await m.retry();assert.equal(calls,1);m.dispose();
});
test('parent/board/viewer changes discard cursor and late response',async()=>{
 const calls=[];let finish;const s=session(),m=new CommunityPublicReadModel({environment:'DEV',publicCommunityComments:(id,q,o)=>{calls.push({id,q,o});if(calls.length===2)return new Promise(r=>finish=r);return Promise.resolve({...page([comment]),nextCursor:'p-cursor'});}},s);
 await m.openComments('p');const old=m.nextPage();await m.openComments('other');assert.equal(calls[1].o.signal.aborted,true);assert.equal(calls[2].q.cursor,null);finish({...page([comment]),nextCursor:'old'});await old;assert.notEqual(m.state.nextCursor,'old');s.set({phase:'anonymous',accountId:null});assert.equal(m.state.items.length,0);m.dispose();
});
test('stale409 and source503 clear existing body instead of empty success',async()=>{
 for(const [status,code] of [[409,'community_content_stale'],[503,'community_render_source_unavailable'],[404,'community_content_unavailable']]){
 let calls=0;const m=new CommunityPublicReadModel(api(async()=>++calls===1?response({contractVersion:version,publicScopeVersion:scope,rendererVersion:renderer,content:post}):response({error:code},status)),session());await m.openContent('p');await m.refresh();assert.equal(m.state.content,null);assert.equal(m.state.error.code,code);m.dispose();}
});
test('block completion refreshes current public via server, not historical receipt',async()=>{
 let notify,calls=0;const blocks={subscribe(f){notify=f;return ()=>{notify=null;};}};
 const m=new CommunityPublicReadModel(api(async()=>{calls++;return response(page(calls===1?[post]:[]));}),session(),{blocks});
 await m.openPosts();notify({phase:'applied',receipt:{blocked:true}});await new Promise(r=>setTimeout(r,0));assert.equal(calls,2);assert.equal(m.state.phase,'empty');assert.equal(m.state.items.length,0);m.dispose();assert.equal(notify,null);
});
test('public scope cache never retains former authenticated data for anonymous viewer',async()=>{
 const s=session(),m=new CommunityPublicReadModel(api(async()=>response(page([post])),'PRD'),s);await m.openPosts();s.set({phase:'anonymous',accountId:null});assert.equal(m.state.items.length,0);assert.equal(m.state.nextCursor,null);assert.equal(m.state.query,null);m.dispose();
});

test('block update aborts in-flight page and prevents late body resurrection',async()=>{
 let notify,finish,calls=0;const blocks={subscribe(f){notify=f;return ()=>{};}};
 const m=new CommunityPublicReadModel(api(async()=>{if(++calls===1)return new Promise(r=>finish=r);return response(page([]));}),session(),{blocks});
 const old=m.openPosts();notify({phase:'applied',receipt:{blocked:false}});await new Promise(r=>setTimeout(r,0));finish(response(page([post])));await old;assert.equal(calls,2);assert.equal(m.state.items.length,0);assert.equal(m.state.phase,'empty');m.dispose();
});
test('public fixed references and opaque author are preserved as server data',async()=>{
 const ref={snapshotId:'pcs_'+'b'.repeat(64),itemId:'fixed-public-id'};
 const v=await api(async()=>response({contractVersion:version,publicScopeVersion:scope,rendererVersion:renderer,content:{...post,wikiRefs:[ref],sourceBalanceVersion:'balance-v1'}})).publicCommunityContent('p');
 assert.deepEqual(v.content.wikiRefs,[ref]);assert.equal(v.content.publicAuthorId,'opaque-author');assert.equal(v.content.sourceBalanceVersion,'balance-v1');assert.equal(Object.hasOwn(v.content,'accountId'),false);
});
