import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityReadModel} from '../src/community-read-model.mjs';
const version='community.own-posts-list.v1.20261008',renderer='community.markdown.v1.20261008';
const row={kind:'POST',id:'p',parentId:null,revision:1,visibility:'VISIBLE',commentLocked:false,title:'제목',sanitizedBody:'<p>본문</p>',rendererVersion:renderer,createdAt:1,updatedAt:1,boardId:'free',spoiler:false,wikiRefs:[],sourceBalanceVersion:null};
const response=(v,status=200)=>new Response(JSON.stringify(v),{status});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('exact ownposts GET/header and opaque filters/cursor no body or csrf',async()=>{
 let sent;const result=await api(async(url,o)=>{sent={url,o};return response({contractVersion:version,items:[row],nextCursor:'opaque==/+'});}).ownCommunityPosts({boardId:'free',cursor:'opaque==/+'});
 const q=new URL(sent.url);assert.equal(q.pathname,'/v1/web/community/own/posts');assert.equal(q.searchParams.get('limit'),'20');assert.equal(q.searchParams.get('boardId'),'free');assert.equal(q.searchParams.get('cursor'),'opaque==/+');
 assert.equal(sent.o.headers.get('X-Community-Own-Posts-Version'),version);assert.equal(sent.o.headers.has('X-CSRF-Token'),false);assert.equal(sent.o.body,undefined);assert.equal(result.nextCursor,'opaque==/+');
});
test('unknown board/limit/fields invalid before fetch',async()=>{
 let calls=0;const c=api(async()=>{calls++;return response({});});
 for(const filters of [{boardId:'unknown'},{limit:51},{limit:1.5},{actor:'A'},{cursor:''}]){
 await assert.rejects(c.ownCommunityPosts(filters),e=>e.kind==='invalid');}assert.equal(calls,0);
});
test('DESC binary order and duplicate/source drift reject',async()=>{
 for(const items of [[{...row,id:'first',createdAt:1},{...row,id:'later',createdAt:2,updatedAt:2}],[{...row,id:'Z'},{...row,id:'a'}],[row,row],[{...row,createdAt:'not-source'}]]){
 await assert.rejects(api(async()=>response({contractVersion:version,items,nextCursor:null})).ownCommunityPosts(),e=>e.kind==='invalid-response');}
 const r=await api(async()=>response({contractVersion:version,items:[{...row,id:'a'},{...row,id:'Z'}],nextCursor:null})).ownCommunityPosts();assert.equal(r.items[0].id,'a');
});
test('masked rows require all sensitive fields null',async()=>{
 const hidden={...row,visibility:'HIDDEN',title:null,sanitizedBody:null,rendererVersion:null,updatedAt:null,spoiler:null,wikiRefs:null,sourceBalanceVersion:null};
 await api(async()=>response({contractVersion:version,items:[hidden],nextCursor:null})).ownCommunityPosts();
 for(const field of ['spoiler','wikiRefs','sourceBalanceVersion','title','sanitizedBody']){
 const v={...hidden,[field]:row[field]===null?'leak':row[field]};
 await assert.rejects(api(async()=>response({contractVersion:version,items:[v],nextCursor:null})).ownCommunityPosts(),e=>e.kind==='invalid-response');}
});
test('visible metadata keeps fixed refs and rejects extra owner/incorrect board/renderer',async()=>{
 const ref={snapshotId:'pcs_'+'b'.repeat(64),itemId:'fixedId'};
 const v=await api(async()=>response({contractVersion:version,items:[{...row,wikiRefs:[ref],sourceBalanceVersion:'balance-1'}],nextCursor:null})).ownCommunityPosts({boardId:'free'});
 assert.deepEqual(v.items[0].wikiRefs,[ref]);assert.equal(Object.isFrozen(v.items[0].wikiRefs[0]),true);
 for(const r of [{...row,boardId:'qna'},{...row,accountId:'private'},{...row,rendererVersion:'legacy'}]){
 await assert.rejects(api(async()=>response({contractVersion:version,items:[r],nextCursor:null})).ownCommunityPosts({boardId:'free'}),e=>e.kind==='invalid-response');}
});
test('board change starts fresh cursor and cancels previous page',async()=>{
 const calls=[];let finish;const m=new CommunityReadModel({ownCommunityPosts:(q,o)=>{calls.push({q,o});if(calls.length===2)return new Promise(r=>finish=r);return Promise.resolve({items:[row],nextCursor:'free-cursor'});}},session());
 await m.openPosts('free');const pending=m.nextPage();const changed=m.openPosts('qna');assert.equal(calls[1].o.signal.aborted,true);assert.equal(calls[2].q.cursor,null);assert.equal(calls[2].q.boardId,'qna');
 finish({items:[row],nextCursor:'old'});await pending;await changed;assert.equal(m.state.query.filters.boardId,'qna');assert.notEqual(m.state.nextCursor,'old');m.dispose();
});
test('account/logout clears cursor/body and drops late ownposts response',async()=>{
 let finish;const s=session(),m=new CommunityReadModel({ownCommunityPosts:()=>new Promise(r=>finish=r)},s);
 const p=m.openPosts('free');s.set({phase:'anonymous',accountId:null});finish({items:[row],nextCursor:'private-cursor'});await p;
 assert.equal(m.state.items.length,0);assert.equal(m.state.nextCursor,null);assert.equal(m.state.query,null);m.dispose();
});
test('creation/render source503 and stale409 cannot leave previous visible body',async()=>{
 let calls=0;const m=new CommunityReadModel(api(async()=>++calls===1?response({contractVersion:version,items:[row],nextCursor:'cursor'}):response({error:'community_creation_source_unavailable'},503)),session());
 await m.openPosts();await m.refresh();assert.equal(m.state.phase,'error');assert.equal(m.state.items.length,0);assert.equal(m.state.error.code,'community_creation_source_unavailable');m.dispose();
});
test('comments ASC and POST DESC retain independent query scopes',async()=>{
 const seen=[];const m=new CommunityReadModel({ownCommunityPosts:async f=>{seen.push(['POST',f]);return {items:[row],nextCursor:'p'};},ownCommunityComments:async f=>{seen.push(['COMMENT',f]);return {items:[],nextCursor:null};}},session());
 await m.openPosts('free');await m.openList();assert.equal(seen[1][0],'COMMENT');assert.equal(seen[1][1].cursor,null);assert.equal(Object.hasOwn(seen[1][1],'boardId'),false);m.dispose();
});
