import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {CommunityBlockModel,CommunityBlockReadModel} from '../src/community-block-model.mjs';
const version='community.personal-block.v1.20261008';
const command={commandId:'block-1',publicAuthorId:'opaque-author',blocked:true,expectedRevision:0};
const receipt={contractVersion:version,commandId:'block-1',publicAuthorId:'opaque-author',blocked:true,revision:1};
const response=(v,status=200)=>new Response(JSON.stringify(v),{status});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},csrfToken:async()=> 'csrf',expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('block exact actor-wide CAS/header and no consent or actor injection',async()=>{
 let sent;const r=await api(async(url,o)=>{sent={url,o};return response(receipt);}).setCommunityBlock(command,{csrfToken:'csrf'});
 assert.equal(sent.url,'https://api.test/v1/web/community/blocks');assert.equal(sent.o.headers.get('X-Community-Block-Version'),version);assert.deepEqual(JSON.parse(sent.o.body),command);assert.deepEqual(r,receipt);assert.equal(sent.o.cache,'no-store');
});
test('initial revision0/no-op revision and forged receipt reject',async()=>{
 await api(async()=>response({...receipt,revision:0})).setCommunityBlock(command,{csrfToken:'csrf'});
 for(const v of [{...receipt,revision:2},{...receipt,blocked:false},{...receipt,accountId:'private'},{...receipt,publicAuthorId:'other'}]){
 await assert.rejects(api(async()=>response(v)).setCommunityBlock(command,{csrfToken:'csrf'}),e=>e.kind==='invalid-response');}
});
test('invalid opaque ID/revision/extra source fields reject locally',async()=>{
 for(const c of [{...command,publicAuthorId:'email@example.test'},{...command,expectedRevision:-1},{...command,blocked:1},{...command,actorId:'A'},{...command,consentVersion:'invented'}]){
 await assert.rejects(api(async()=>response(receipt)).setCommunityBlock(c,{csrfToken:'csrf'}),e=>e.kind==='invalid');}
});
test('GET exact header/opaque cursor and safe minimal sorted projection',async()=>{
 let sent;const r=await api(async(url,o)=>{sent={url,o};return response({contractVersion:version,revision:3,items:[{publicAuthorId:'A'},{publicAuthorId:'a'}],nextCursor:'opaque/+=='});}).communityBlocks({cursor:'opaque/+=='});
 assert.equal(sent.o.headers.get('X-Community-Block-Version'),version);assert.equal(sent.o.headers.has('X-CSRF-Token'),false);assert.equal(sent.o.body,undefined);assert.equal(new URL(sent.url).searchParams.get('cursor'),'opaque/+==');assert.equal(r.revision,3);
});
test('minimal block list rejects leaked identity/order/duplicates/version/revision',async()=>{
 for(const v of [{contractVersion:version,revision:0,items:[{publicAuthorId:'a',email:'private'}],nextCursor:null},{contractVersion:version,revision:0,items:[{publicAuthorId:'a'},{publicAuthorId:'A'}],nextCursor:null},{contractVersion:version,revision:-1,items:[],nextCursor:null}]){
 await assert.rejects(api(async()=>response(v)).communityBlocks(),e=>e.kind==='invalid-response');}
});
test('shared in-flight/original key response loss retry',async()=>{
 let calls=0,finish;const m=new CommunityBlockModel({setCommunityBlock:()=>{calls++;return new Promise(r=>finish=r);}},session());
 const p=m.execute(command);assert.equal(m.execute({...command}),p);await assert.rejects(m.execute({...command,blocked:false}),e=>e.kind==='conflict');await Promise.resolve();finish(receipt);await p;assert.equal(calls,1);m.dispose();
 const sent=[];let n=0;const retry=new CommunityBlockModel({setCommunityBlock:async c=>{sent.push(c);if(++n===1)throw {kind:'network'};return receipt;}},session());
 await retry.execute(command);await retry.retry();assert.deepEqual(sent,[command,command]);retry.dispose();
});
test('self403/unknown404 and actor revision409 typed without automatic mutated retry',async()=>{
 for(const [status,code,reason] of [[403,'community_self_block_forbidden','self-block-forbidden'],[404,'community_author_unavailable','author-unavailable'],[409,'community_block_revision_conflict','block-revision-conflict']]){
 let calls=0;const m=new CommunityBlockModel(api(async()=>{calls++;return response({error:code},status);}),session());await m.execute(command);assert.equal(m.state.error.reason,reason);await m.retry();assert.equal(calls,1);m.dispose();}
});
test('revision-bound cursor conflict clears query cursor for explicit fresh refresh',async()=>{
 const cursors=[];const m=new CommunityBlockReadModel({communityBlocks:async q=>{cursors.push(q.cursor);if(cursors.length===2)throw {kind:'conflict',code:'community_block_revision_conflict',status:409};return {revision:cursors.length,items:[{publicAuthorId:'opaque-author'}],nextCursor:'old-revision-cursor'};}},session());
 await m.openList();assert.equal(m.state.revision,1);await m.nextPage();assert.equal(m.state.items.length,0);assert.equal(m.state.nextCursor,null);assert.equal(m.state.query.cursor,null);await m.refresh();assert.deepEqual(cursors,[null,'old-revision-cursor',null]);m.dispose();
});
test('historical blocked receipt does not replace current unblocked relationship',async()=>{
 const m=new CommunityBlockModel(api(async()=>response(receipt)),session());const read=new CommunityBlockReadModel(api(async()=>response({contractVersion:version,revision:2,items:[],nextCursor:null})),session());
 await m.execute(command);await read.openList();assert.equal(m.state.receipt.blocked,true);assert.equal(read.state.items.length,0);assert.equal(read.state.revision,2);m.dispose();read.dispose();
});
test('logout/account changes abort old command and clear relation revision/cursor',async()=>{
 let finish,signal;const s=session(),m=new CommunityBlockModel({setCommunityBlock:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=m.execute(command);await Promise.resolve();s.set({phase:'anonymous',accountId:null});assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(m.state.receipt,null);m.dispose();
 const owner=session(),r=new CommunityBlockReadModel({communityBlocks:async()=>({revision:4,items:[{publicAuthorId:'x'}],nextCursor:'old'})},owner);
 await r.openList();owner.set({phase:'authenticated',accountId:'B',expiresAt:'2099-01-01'});assert.equal(r.state.revision,null);assert.equal(r.state.nextCursor,null);assert.equal(r.state.items.length,0);r.dispose();
});
