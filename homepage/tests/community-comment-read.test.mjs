import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityCommentModel} from '../src/community-comment-model.mjs';
import {CommunityReadModel} from '../src/community-read-model.mjs';
const version='community.comment-create.v1.20261008',renderer='community.markdown.v1.20261008',listVersion='community.own-comments-list.v1.20261008',readVersion='community.own-content-read.v1.20261008';
const input={postId:'post',commandId:'comment-1',markdown:'댓글'};
const receipt={contractVersion:version,rendererVersion:renderer,commandId:'comment-1',contentId:'new-comment',parentId:'post',kind:'COMMENT',revision:1,visibility:'VISIBLE',createdAt:1791417600000};
const content={kind:'COMMENT',id:'new-comment',parentId:'post',revision:1,visibility:'VISIBLE',commentLocked:false,title:null,sanitizedBody:'<p>댓글</p>',updatedAt:receipt.createdAt};
const row={...content,rendererVersion:renderer,createdAt:receipt.createdAt};
const response=(v,status=200,headers={})=>new Response(JSON.stringify(v),{status,headers});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},csrfToken:async()=> 'csrf',expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('comment exact typed request and body-free original receipt',async()=>{
 let sent;const v=await api(async(url,o)=>{sent={url,o};return response(receipt);}).createCommunityComment(input,{csrfToken:'csrf'});
 assert.deepEqual(v,receipt);assert.equal(sent.url,'https://api.test/v1/web/community/contents/post/comments');assert.equal(sent.o.headers.get('X-Community-Content-Version'),version);
 assert.deepEqual(JSON.parse(sent.o.body),{commandId:'comment-1',markdown:'댓글'});assert.equal(sent.o.cache,'no-store');assert.equal(Object.hasOwn(v,'markdown'),false);
});
test('COMMENT1000 Unicode input and no actor/kind injection',async()=>{
 const c=api(async()=>response(receipt));await c.createCommunityComment({...input,markdown:'😀'.repeat(1000)},{csrfToken:'csrf'});
 for(const v of [{...input,markdown:'😀'.repeat(1001)},{...input,markdown:'  '},{...input,actorId:'A'},{...input,postId:'../post'}]){
 await assert.rejects(c.createCommunityComment(v,{csrfToken:'csrf'}),e=>e.kind==='invalid');}
});
test('forged creation receipt parent/version/renderer/metadata rejected',async()=>{
 for(const v of [{...receipt,parentId:'other'},{...receipt,revision:2},{...receipt,rendererVersion:'wrong'},{...receipt,markdown:'body'},{...receipt,createdAt:'not-date'}]){
 await assert.rejects(api(async()=>response(v)).createCommunityComment(input,{csrfToken:'csrf'}),e=>e.kind==='invalid-response');}
});
test('rate limited retry uses server seconds and exact original command',async()=>{
 let calls=0,now=1000;const m=new CommunityCommentModel(api(async()=>{if(++calls===1)return response({error:'community_rate_limited',retryAfter:10},429,{'Retry-After':'10'});return response(receipt);}),session(),{now:()=>now});
 await m.execute(input);assert.equal(m.state.phase,'rate-limited');assert.equal(m.state.retryAt,11000);await m.retry();assert.equal(calls,1);
 await assert.rejects(m.execute({...input,commandId:'new'}),e=>e.kind==='conflict');now=11000;await m.retry();assert.equal(calls,2);assert.equal(m.state.phase,'created');m.dispose();
});
test('rate fields mismatch fails closed and no automatic resend',async()=>{
 let calls=0;const m=new CommunityCommentModel(api(async()=>{calls++;return response({error:'community_rate_limited',retryAfter:1},429,{'Retry-After':'2'});}),session());
 await m.execute(input);assert.equal(m.state.retryAt,null);await m.retry();assert.equal(calls,1);m.dispose();
});
test('comment in-flight/account switch ignores original late receipt',async()=>{
 let finish,signal;const s=session(),m=new CommunityCommentModel({createCommunityComment:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=m.execute(input);assert.equal(m.execute({...input}),p);await Promise.resolve();s.set({phase:'authenticated',accountId:'B',expiresAt:'2099-01-01'});
 assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(m.state.receipt,null);m.dispose();
});
test('own list exact GET header, opaque cursor and noCSRF',async()=>{
 let sent;const c=api(async(url,o)=>{sent={url,o};return response({contractVersion:listVersion,items:[row],nextCursor:'opaque/+=cursor'});});
 const v=await c.ownCommunityComments({limit:20,cursor:'opaque/+=cursor'});
 assert.equal(sent.o.headers.get('X-Community-Own-Comments-Version'),listVersion);assert.equal(sent.o.headers.has('X-CSRF-Token'),false);
 assert.equal(new URL(sent.url).searchParams.get('cursor'),'opaque/+=cursor');assert.equal(v.nextCursor,'opaque/+=cursor');
});
test('own detail exact version and current body without mutationreceipt',async()=>{
 let sent;const v=await api(async(url,o)=>{sent={url,o};return response({contractVersion:readVersion,rendererVersion:renderer,content});}).ownCommunityContent('new-comment');
 assert.equal(sent.o.headers.get('X-Community-Own-Read-Version'),readVersion);assert.equal(sent.o.body,undefined);assert.equal(v.content.sanitizedBody,content.sanitizedBody);
});
test('hidden/deleted/pending ownrows have null title/body/renderer',async()=>{
 for(const visibility of ['HIDDEN','REVIEW_PENDING','AUTHOR_DELETED']){
 const hidden={...row,visibility,title:null,sanitizedBody:null,rendererVersion:null,updatedAt:null};
 const v=await api(async()=>response({contractVersion:listVersion,items:[hidden],nextCursor:null})).ownCommunityComments();assert.equal(v.items[0].sanitizedBody,null);
 await assert.rejects(api(async()=>response({contractVersion:listVersion,items:[{...hidden,sanitizedBody:'leak'}],nextCursor:null})).ownCommunityComments(),e=>e.kind==='invalid-response');}
});
test('unsupported renderer/private fields/unbounded page rejected',async()=>{
 for(const v of [{contractVersion:readVersion,rendererVersion:'legacy',content},{contractVersion:readVersion,rendererVersion:renderer,content:{...content,accountId:'A'}}]){
 await assert.rejects(api(async()=>response(v)).ownCommunityContent('new-comment'),e=>e.kind==='invalid-response');}
 await assert.rejects(api(async()=>response({})).ownCommunityComments({limit:51}),e=>e.kind==='invalid');
});
test('read model cursor chain resets and account change clears result',async()=>{
 const cursors=[];const s=session(),m=new CommunityReadModel({ownCommunityComments:async f=>{cursors.push(f.cursor);return {items:[row],nextCursor:f.cursor===null?'opaque':null};}},s);
 await m.openList();await m.nextPage();assert.deepEqual(cursors,[null,'opaque']);await m.openList();assert.equal(cursors.at(-1),null);
 s.set({phase:'anonymous',accountId:null});assert.equal(m.state.items.length,0);assert.equal(m.state.nextCursor,null);m.dispose();
});
test('read late response and source failure cannot expose oldbody',async()=>{
 let finish;const s=session(),m=new CommunityReadModel({ownCommunityContent:()=>new Promise(r=>finish=r)},s);
 const p=m.openContent('new-comment');s.set({phase:'anonymous',accountId:null});finish({content});await p;assert.equal(m.state.content,null);m.dispose();
 const bad=new CommunityReadModel({ownCommunityContent:async()=>{throw {kind:'unavailable',code:'community_render_source_unavailable',status:503};}},session());
 await bad.openContent('new-comment');assert.equal(bad.state.phase,'error');assert.equal(bad.state.content,null);bad.dispose();
});
test('parent unavailability and review policy unavailable are failures not publication',async()=>{
 for(const [status,code] of [[404,'community_parent_unavailable'],[409,'community_comments_locked'],[503,'community_review_policy_unavailable']]){
 const m=new CommunityCommentModel(api(async()=>response({error:code},status)),session());await m.execute(input);assert.equal(m.state.receipt,null);assert.equal(m.state.error.code,code);m.dispose();}
});


test('comment response loss retries only original key/body',async()=>{
 let calls=0,sent=[];const m=new CommunityCommentModel({createCommunityComment:async v=>{sent.push({...v});if(++calls===1)throw {kind:'network'};return receipt;}},session());
 await m.execute(input);assert.equal(m.state.phase,'uncertain');await assert.rejects(m.execute({...input,markdown:'changed'}),e=>e.kind==='conflict');await m.retry();assert.deepEqual(sent,[input,input]);m.dispose();
});
test('read current detail carries validated renderer separately from receipt',async()=>{
 const m=new CommunityReadModel(api(async()=>response({contractVersion:readVersion,rendererVersion:renderer,content})),session());
 await m.openContent('new-comment');assert.equal(m.state.content.rendererVersion,renderer);m.dispose();
});
test('ownlist duplicate ids and descending time fail closed',async()=>{
 for(const items of [[row,row],[{...row,id:'second',createdAt:2,updatedAt:2},{...row,id:'first',createdAt:1,updatedAt:1}]]){
 await assert.rejects(api(async()=>response({contractVersion:listVersion,items,nextCursor:null})).ownCommunityComments(),e=>e.kind==='invalid-response');}
});
test('comment dispose during pending csrf sends nothing',async()=>{
 let finish,calls=0;const s=session();s.csrfToken=()=>new Promise(r=>finish=r);
 const m=new CommunityCommentModel({createCommunityComment:()=>calls++},s);const p=m.execute(input);m.dispose();finish('csrf');await p;assert.equal(calls,0);assert.equal(m.state.phase,'disposed');
});
