import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {WebHttp} from '../src/api-http.mjs';
import {CommunityEditModel} from '../src/community-edit-model.mjs';
const version='community.content-edit.v1.20261008',renderer='community.markdown.v1.20261008';
const command={contentId:'p',commandId:'edit-1',expectedRevision:1,title:'제목',markdown:'**본문**'};
const receipt={contractVersion:version,rendererVersion:renderer,commandId:'edit-1',contentId:'p',revision:2,visibility:'VISIBLE'};
const response=(data,status=200)=>new Response(JSON.stringify(data),{status});
const api=fetch=>new WebApi({origin:'https://api.test',environment:'DEV',versionCode:123,fetch});
function session(){let state={phase:'authenticated',accountId:'A',expiresAt:'2099-01-01'},listeners=new Set();return {get state(){return state;},subscribe(f){listeners.add(f);return ()=>listeners.delete(f);},csrfToken:async()=> 'csrf',expire(){this.set({phase:'expired',accountId:null});},set(s){state=s;for(const f of listeners)f(s);}};}
test('edit exact wire/version and original metadata only',async()=>{
 let sent;const result=await api(async(url,o)=>{sent={url,o};return response(receipt);}).editCommunityContent(command,{csrfToken:'csrf'});
 assert.deepEqual(result,receipt);assert.equal(sent.url,'https://api.test/v1/web/community/contents/p/edit');
 assert.equal(sent.o.headers.get('X-Community-Content-Version'),version);assert.equal(sent.o.headers.has('Idempotency-Key'),false);
 assert.deepEqual(JSON.parse(sent.o.body),{commandId:'edit-1',expectedRevision:1,title:'제목',markdown:'**본문**'});
 assert.equal(sent.o.credentials,'include');assert.equal(sent.o.cache,'no-store');assert.equal(Object.hasOwn(result,'markdown'),false);
});
test('post Unicode max accepted without UTF16 undercount/overcount',async()=>{
 const v={...command,title:'😀'.repeat(100),markdown:'😀'.repeat(10000)};let calls=0;
 const client=api(async()=>{calls++;return response(receipt);});await client.editCommunityContent(v,{csrfToken:'csrf'});
 await assert.rejects(client.editCommunityContent({...v,markdown:v.markdown+'😀'},{csrfToken:'csrf'}),e=>e.kind==='invalid');
 await assert.rejects(client.editCommunityContent({...v,title:v.title+'😀'},{csrfToken:'csrf'}),e=>e.kind==='invalid');assert.equal(calls,1);
});
test('comments use null title and max1000 submitted codepoints',async()=>{
 const client=api(async()=>response(receipt));await client.editCommunityContent({...command,title:null,markdown:'😀'.repeat(1000)},{csrfToken:'csrf'});
 await assert.rejects(client.editCommunityContent({...command,title:null,markdown:'😀'.repeat(1001)},{csrfToken:'csrf'}),e=>e.kind==='invalid');
 await assert.rejects(client.editCommunityContent({...command,title:null,markdown:'x'+' '.repeat(1000)},{csrfToken:'csrf'}),e=>e.kind==='invalid');
});
test('whitespace and injected server-owned fields rejected',async()=>{
 const client=api(async()=>response(receipt));
 for(const c of [{...command,title:'  '},{...command,markdown:' \n '},{...command,sanitizedBody:'<p>fake</p>'},{...command,kind:'POST'},{...command,approvedOrigins:['https://evil.test']}]){
 await assert.rejects(client.editCommunityContent(c,{csrfToken:'csrf'}),e=>e.kind==='invalid');}
});
test('receipt metadata mismatch and body/private fields fail closed',async()=>{
 for(const v of [{...receipt,rendererVersion:'unknown'},{...receipt,revision:3},{...receipt,markdown:'body'},{...receipt,visibility:'AUTHOR_DELETED'},{...receipt,contentId:'other'}]){
 await assert.rejects(api(async()=>response(v)).editCommunityContent(command,{csrfToken:'csrf'}),e=>e.kind==='invalid-response');}
});
test('historical hidden/review receipt remains metadata, never current body',async()=>{
 for(const visibility of ['HIDDEN','REVIEW_PENDING']){const m=new CommunityEditModel(api(async()=>response({...receipt,visibility})),session());await m.execute(command);assert.equal(m.state.phase,'edited');assert.equal(m.state.receipt.visibility,visibility);assert.equal(Object.hasOwn(m.state,'currentContent'),false);assert.equal(Object.hasOwn(m.state.receipt,'title'),false);m.dispose();}
});
test('in-flight same edit shared, changed markdown/key rejected',async()=>{
 let finish,calls=0;const m=new CommunityEditModel({editCommunityContent:()=>{calls++;return new Promise(r=>finish=r);}},session());
 const p=m.execute(command);assert.equal(m.execute({...command}),p);await assert.rejects(m.execute({...command,markdown:'changed'}),e=>e.kind==='conflict');
 await Promise.resolve();finish(receipt);await p;assert.equal(calls,1);m.dispose();
});
test('response loss retries exact immutable input even caller changes object',async()=>{
 let calls=0,sent=[];const c={...command};const m=new CommunityEditModel({editCommunityContent:async(v)=>{sent.push({...v});if(++calls===1)throw {kind:'network'};return receipt;}},session());
 await m.execute(c);c.markdown='later';await m.retry();assert.deepEqual(sent,[command,command]);m.dispose();
});
test('edit session/account switch excludes late receipts',async()=>{
 for(const next of [{phase:'anonymous',accountId:null},{phase:'authenticated',accountId:'B',expiresAt:'2099-01-01'}]){
 let finish,signal;const s=session(),m=new CommunityEditModel({editCommunityContent:(_,o)=>{signal=o.signal;return new Promise(r=>finish=r);}},s);
 const p=m.execute(command);await Promise.resolve();s.set(next);assert.equal(signal.aborted,true);finish(receipt);await p;assert.equal(m.state.receipt,null);m.dispose();}
});
test('link rejection is typed; literal HTML in code sends unchanged and no allowlist invented',async()=>{
 const m=new CommunityEditModel(api(async()=>response({error:'community_link_forbidden'},400)),session());
 await m.execute({...command,markdown:'[link](https://example.test)'});assert.equal(m.state.phase,'error');assert.equal(m.state.error.reason,'link-forbidden');m.dispose();
 const c={...command,markdown:'\u0060<div>literal</div>\u0060'};let body;await api(async(_,o)=>{body=JSON.parse(o.body);return response(receipt);}).editCommunityContent(c,{csrfToken:'csrf'});assert.equal(body.markdown,c.markdown);
});
test('exact 128000 byte edit transport limit and default deletion18000',async()=>{
 let calls=0;const http=new WebHttp({origin:'https://api.test',environment:'DEV',versionCode:123,fetch:async()=>{calls++;return response({ok:true});}});
 await http.request('/v1/web/community/contents/p/edit',{method:'POST',communityVersion:version,body:{x:'a'.repeat(127992)}});
 await assert.rejects(http.request('/v1/web/community/contents/p/edit',{method:'POST',communityVersion:version,body:{x:'a'.repeat(127993)}}),e=>e.kind==='invalid');
 await assert.rejects(http.request('/v1/web/community/contents/p/delete',{method:'POST',communityVersion:'community.content-store.v1.20261008',body:{x:'a'.repeat(17993)}}),e=>e.kind==='invalid');assert.equal(calls,1);
});
test('subsecond expiry checked with no artificial 1second grace',async()=>{
 const s=session();s.set({phase:'authenticated',accountId:'A',expiresAt:'2026-10-08T00:00:00.500Z'});let calls=0;
 const m=new CommunityEditModel({editCommunityContent:()=>calls++},s,{now:()=>Date.parse('2026-10-08T00:00:00.501Z')});
 await assert.rejects(m.execute(command),e=>e.kind==='unauthenticated');assert.equal(calls,0);m.dispose();
});
test('delete nonpublic404 remains hidden unavailable not ownership assertion',async()=>{
 const client=api(async()=>response({error:'community_content_unavailable'},404));
 await assert.rejects(client.deleteCommunityContent({contentId:'hidden',commandId:'del',expectedRevision:1},{csrfToken:'csrf'}),e=>e.kind==='not-found'&&e.code==='community_content_unavailable');
});

test('expiry while waiting for csrf prevents dispatch',async()=>{
 let now=Date.parse('2026-10-08T00:00:00.499Z'),finish,calls=0;
 const s=session();s.set({phase:'authenticated',accountId:'A',expiresAt:'2026-10-08T00:00:00.500Z'});s.csrfToken=()=>new Promise(r=>finish=r);
 const m=new CommunityEditModel({editCommunityContent:async()=>{calls++;return receipt;}},s,{now:()=>now});
 const p=m.execute(command);now+=2;finish('csrf');await p;assert.equal(calls,0);assert.equal(m.state.phase,'authentication-required');m.dispose();
});
test('UTF8 byte cap differs from JS code units',async()=>{
 let calls=0;const http=new WebHttp({origin:'https://api.test',environment:'DEV',versionCode:123,fetch:async()=>{calls++;return response({ok:true});}});
 await assert.rejects(http.request('/v1/web/community/contents/p/edit',{method:'POST',communityVersion:version,body:{x:'😀'.repeat(32000)}}),e=>e.kind==='invalid');assert.equal(calls,0);
});
test('edit rejects link/html/image errors without rendering failure as success',async()=>{
 for(const [code,reason] of [['community_html_forbidden','html-forbidden'],['community_image_forbidden','image-forbidden'],['community_markdown_invalid','invalid-markdown']]){
 const m=new CommunityEditModel(api(async()=>response({error:code},400)),session());await m.execute(command);assert.equal(m.state.error.reason,reason);assert.equal(m.state.receipt,null);m.dispose();}
});
