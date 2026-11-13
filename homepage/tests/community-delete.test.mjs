import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityDeleteModel} from '../src/community-delete-model.mjs';
const version='community.content-store.v1.20261008';
const input={contentId:'post-1',commandId:'delete-1',expectedRevision:1};
const receipt={contractVersion:version,commandId:'delete-1',contentId:'post-1',revision:2,visibility:'AUTHOR_DELETED'};
const response=(v,status=200)=>new Response(JSON.stringify(v),{status});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01T00:00:00Z'},listeners=new Set();return {get state(){return state;},subscribe(fn){listeners.add(fn);return ()=>listeners.delete(fn);},csrfToken:async()=> 'csrf',expire(){this.set({phase:'expired',accountId:null});},set(v){state=v;for(const fn of listeners)fn(v);},get subscribers(){return listeners.size;}};}
test('exact delete wire/version header and receipt projection',async()=>{
 let sent;const client=api(async(url,o)=>{sent={url,o};return response(receipt);});
 assert.deepEqual(await client.deleteCommunityContent(input,{csrfToken:'csrf'}),receipt);
 assert.equal(sent.url,'https://api.test/v1/web/community/contents/post-1/delete');
 assert.equal(sent.o.headers.get('X-Community-Content-Version'),version);
 assert.equal(sent.o.headers.get('X-CSRF-Token'),'csrf');assert.equal(sent.o.headers.get('X-LastRail-App-Version-Code'),'123');
 assert.equal(sent.o.headers.has('Idempotency-Key'),false);
 assert.deepEqual(JSON.parse(sent.o.body),{commandId:'delete-1',expectedRevision:1});
 assert.equal(sent.o.credentials,'include');assert.equal(sent.o.cache,'no-store');
});
test('invalid IDs revision and unknown input rejected before transport',async()=>{
 let calls=0;const client=api(async()=>{calls++;return response(receipt);});
 for(const v of [{...input,contentId:'../x'},{...input,commandId:'bad x'},{...input,expectedRevision:0},{...input,extra:1}]){
  await assert.rejects(client.deleteCommunityContent(v,{csrfToken:'csrf'}),e=>e.kind==='invalid');
 }assert.equal(calls,0);
});
test('receipt mismatch/private fields fail closed',async()=>{
 for(const v of [{...receipt,revision:1},{...receipt,commandId:'other'},{...receipt,contentId:'other'},{...receipt,visibility:'PUBLIC'},{...receipt,accountId:'secret'}]){
 await assert.rejects(api(async()=>response(v)).deleteCommunityContent(input,{csrfToken:'csrf'}),e=>e.kind==='invalid-response');}
});
test('same in-flight command reuses promise and receipt; changed payload rejected',async()=>{
 let finish,calls=0;const model=new CommunityDeleteModel({deleteCommunityContent:()=>{calls++;return new Promise(r=>finish=r);}},session());
 const first=model.execute(input);const second=model.execute({...input});assert.equal(first,second);
 await assert.rejects(model.execute({...input,contentId:'other'}),e=>e.kind==='conflict');
 await Promise.resolve();finish(receipt);await first;assert.equal(calls,1);assert.equal(model.state.phase,'deleted');
 await model.execute(input);assert.equal(calls,1);model.dispose();
});
test('response loss permits only explicit original command retry',async()=>{
 let calls=0;const sent=[];const model=new CommunityDeleteModel({deleteCommunityContent:async(v)=>{sent.push(v);if(++calls===1)throw Object.assign(Error(),{kind:'network'});return receipt;}},session());
 await model.execute(input);assert.equal(model.state.phase,'uncertain');assert.equal(calls,1);
 await assert.rejects(model.execute({...input,commandId:'new'}),e=>e.kind==='conflict');
 await model.retry();assert.deepEqual(sent,[input,input]);assert.equal(model.state.phase,'deleted');model.dispose();
});
test('account change aborts and excludes late response',async()=>{
 let finish,signal;const s=session(),model=new CommunityDeleteModel({deleteCommunityContent:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=model.execute(input);await Promise.resolve();s.set({phase:'authenticated',accountId:'B',expiresAt:'2099-01-01'});
 assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(model.state.phase,'idle');assert.equal(model.state.receipt,null);model.dispose();
});
test('dispose during csrf does not send and unsubscribes',async()=>{
 const s=session();let finish,calls=0;s.csrfToken=()=>new Promise(r=>finish=r);
 const m=new CommunityDeleteModel({deleteCommunityContent:async()=>{calls++;return receipt;}},s);const p=m.execute(input);m.dispose();finish('csrf');await p;
 assert.equal(calls,0);assert.equal(s.subscribers,0);assert.equal(m.state.phase,'disposed');
});
test('typed known errors do not replay blindly',async()=>{
 for(const [code,status,kind,reason] of [['community_revision_conflict',409,'conflict','revision-conflict'],['web_csrf_forbidden',403,'forbidden','csrf-forbidden'],['community_forbidden',403,'forbidden','not-owner'],['community_content_unavailable',404,'not-found','content-unavailable'],['tester_forbidden',403,'forbidden','tester-forbidden']]){
  let calls=0;const m=new CommunityDeleteModel({deleteCommunityContent:async()=>{calls++;throw {code,status,kind};}},session());
  await m.execute(input);assert.equal(m.state.phase,'error');assert.equal(m.state.error.reason,reason);await m.retry();assert.equal(calls,1);m.dispose();
 }
});
test('storage failure remains uncertain and never reports deletion',async()=>{
 const m=new CommunityDeleteModel({deleteCommunityContent:async()=>{throw {kind:'unavailable',code:'community_storage_invalid',status:503};}},session());
 await m.execute(input);assert.equal(m.state.phase,'uncertain');assert.equal(m.state.receipt,null);assert.equal(m.state.error.reason,'storage-invalid');m.dispose();
});
test('expired identity prevents mutation',async()=>{
 const s=session();s.set({phase:'authenticated',accountId:'A',expiresAt:'2000-01-01'});let calls=0;
 const m=new CommunityDeleteModel({deleteCommunityContent:()=>calls++},s);
 await assert.rejects(m.execute(input),e=>e.kind==='unauthenticated');assert.equal(calls,0);m.dispose();
});

test('listener reentry cannot duplicate command dispatch',async()=>{
 let finish,calls=0;const s=session(),m=new CommunityDeleteModel({deleteCommunityContent:()=>{calls++;return new Promise(r=>finish=r);}},s);
 let nested;m.subscribe(state=>{if(state.phase==='deleting'&&!nested)nested=m.execute(input);});
 const p=m.execute(input);await Promise.resolve();assert.equal(calls,1);finish(receipt);await p;m.dispose();
});
test('real WebApi plus Model composes storage errors and original retry',async()=>{
 let calls=0;const sent=[];const m=new CommunityDeleteModel(api(async(url,o)=>{sent.push(JSON.parse(o.body));if(++calls===1)return response({error:'community_storage_unavailable'},503);return response(receipt);}),session());
 await m.execute(input);assert.equal(m.state.error.reason,'storage-unavailable');assert.equal(m.clear(),false);await m.retry();assert.equal(m.state.phase,'deleted');assert.deepEqual(sent,[{commandId:'delete-1',expectedRevision:1},{commandId:'delete-1',expectedRevision:1}]);m.dispose();
});

test('session termination aborts mutation and discards late receipt',async()=>{
 let finish,signal;const s=session(),m=new CommunityDeleteModel({deleteCommunityContent:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=m.execute(input);await Promise.resolve();s.set({phase:'anonymous',accountId:null});assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(m.state.phase,'authentication-required');assert.equal(m.state.receipt,null);m.dispose();
});
test('401 expires existing session without replay',async()=>{
 const s=session();let calls=0;const m=new CommunityDeleteModel({deleteCommunityContent:async()=>{calls++;throw {kind:'unauthenticated',status:401};}},s);
 await m.execute(input);assert.equal(s.state.phase,'expired');assert.equal(m.state.phase,'authentication-required');await m.retry();assert.equal(calls,1);m.dispose();
});
