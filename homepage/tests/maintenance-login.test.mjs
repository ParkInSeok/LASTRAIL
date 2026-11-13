import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {WebSessionModel} from '../src/web-session-model.mjs';
import * as modelModule from '../src/maintenance-login-model.mjs';
const response=(v,status=200)=>new Response(JSON.stringify(v),{status});
const normal={contractVersion:'maintenance-login-v1',serverTime:1000,policyRevision:2,gameLoginAllowed:true,webLoginAllowed:true,title:null,summary:null,startsAt:null,estimatedEndAt:null,retryAfterSeconds:null};
const blocked={...normal,gameLoginAllowed:false,webLoginAllowed:false,title:'Public maintenance',summary:'Public summary',startsAt:100,estimatedEndAt:500,retryAfterSeconds:60};
const make=fetch=>new WebApi({origin:'https://api.example.test',environment:'DEV',versionCode:123,fetch});
const future='2099-01-01T00:00:00.000Z';
const sessionDto={contractVersion:'web-session-v1',accountId:'account-a',expiresAt:future};
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return {promise,resolve};};
test('confirmed login maintenance is distinct from generic and policy-unavailable503',async()=>{
 for(const [data,kind,confirmed] of [[{error:'maintenance_login_blocked',contractVersion:'maintenance-login-v1',confirmed:true},'maintenance',true],[{error:'maintenance_policy_unavailable',contractVersion:'maintenance-login-v1',confirmed:false},'unavailable',false],[{error:'service_unavailable'},'unavailable',null],[{error:'maintenance_login_blocked',confirmed:true},'unavailable',null],[{error:'maintenance_login_blocked',contractVersion:'future',confirmed:true},'unavailable',null]]){
 await assert.rejects(make(async()=>response(data,503)).loginPassword('user','password'),e=>e.kind===kind&&e.maintenanceConfirmed===confirmed);
 }
});
test('public maintenance uses exact anonymous no-store GET with no query/body/tester or credential header',async()=>{
 let sent;const api=make(async(url,options)=>{sent={url,options};return response(normal);});const result=await api.maintenanceLogin();assert.deepEqual(result,normal);assert.equal(sent.url,'https://api.example.test/v1/public/maintenance/login');assert.equal(sent.options.method,'GET');assert.equal(sent.options.credentials,'omit');assert.equal(sent.options.cache,'no-store');assert.equal(sent.options.body,undefined);for(const header of ['X-LastRail-App-Version-Code','X-CSRF-Token','Authorization','Idempotency-Key'])assert.equal(sent.options.headers.has(header),false);
});
test('malformed or private-shaped status is never interpreted as an allowed login',async()=>{
 for(const value of [{...normal,webLoginAllowed:'true'},{...normal,contractVersion:'future'},{...normal,policyRevision:null},{...normal,internalReason:'PRIVATE'},{...normal,webLoginAllowed:false},{...blocked,estimatedEndAt:99}])await assert.rejects(make(async()=>response(value)).maintenanceLogin(),e=>e.kind==='invalid-response');
});
test('public policy-unavailable without version is unconfirmed, not confirmed maintenance',async()=>{
 await assert.rejects(make(async()=>response({error:'maintenance_policy_unavailable',confirmed:false},503)).maintenanceLogin(),e=>e.kind==='unavailable'&&e.maintenanceConfirmed===false);
});
test('blocked replacement login preserves existing verified account and CSRF without automatic logout',async()=>{
 const sent=[];const api=make(async(url)=>{sent.push(new URL(url).pathname);if(url.endsWith('/me'))return response(sessionDto);if(url.endsWith('/csrf'))return response({contractVersion:'web-session-v1',csrfToken:'original-csrf'});return response({error:'maintenance_login_blocked',contractVersion:'maintenance-login-v1',confirmed:true},503);});const session=new WebSessionModel(api);await session.restore();await session.csrfToken();await session.loginPassword('other','password');assert.equal(session.state.phase,'authenticated');assert.equal(session.state.accountId,'account-a');assert.equal(session.state.error,'maintenance');assert.equal(session.state.maintenanceConfirmed,true);assert.equal(await session.csrfToken(),'original-csrf');assert.equal(sent.filter(p=>p.endsWith('/password')).length,1);assert.equal(sent.some(p=>p.endsWith('/logout')),false);session.dispose();
});
test('status model consumes server flags; elapsed estimated end never auto-normalizes or resends credentials',async()=>{
 let calls=0;const api=make(async()=>{calls++;return response(blocked);});const m=new modelModule.MaintenanceLoginModel(api);await m.refresh();assert.equal(m.state.phase,'blocked');assert.equal(m.state.confirmed,true);assert.equal(m.state.snapshot.estimatedEndAt,500);await new Promise(r=>setImmediate(r));assert.equal(m.state.phase,'blocked');assert.equal(calls,1);m.dispose();
});
test('GAME-only maintenance still permits WEB login based on server flag',async()=>{
 const m=new modelModule.MaintenanceLoginModel(make(async()=>response({...blocked,webLoginAllowed:true})));await m.refresh();assert.equal(m.state.phase,'allowed');assert.equal(m.state.snapshot.gameLoginAllowed,false);m.dispose();
});
test('failed recheck removes current allowed claim and distinguishes unconfirmed policy from ordinary503',async()=>{
 let count=0;const m=new modelModule.MaintenanceLoginModel(make(async()=>++count===1?response(normal):response({error:'maintenance_policy_unavailable',confirmed:false},503)));await m.refresh();assert.equal(m.state.phase,'allowed');await m.refresh();assert.equal(m.state.phase,'unknown');assert.equal(m.state.confirmed,false);assert.equal(m.state.snapshot,null);m.dispose();
});
test('explicit state refresh releases block only on new server answer, without login call',async()=>{
 const paths=[];let count=0;const m=new modelModule.MaintenanceLoginModel(make(async url=>{paths.push(new URL(url).pathname);return response(++count===1?blocked:normal);}));await m.refresh();await m.refresh();assert.equal(m.state.phase,'allowed');assert.equal(paths.length,2);assert.equal(paths.every(p=>p==='/v1/public/maintenance/login'),true);m.dispose();
});
test('superseded status request is aborted; late response cannot replace explicit new check',async()=>{
 const d=deferred();let count=0,oldSignal;const api=make(async(_u,o)=>{if(++count===1){oldSignal=o.signal;return d.promise;}return response(normal);});const m=new modelModule.MaintenanceLoginModel(api);const first=m.refresh();await m.refresh();d.resolve(response(blocked));await first;assert.equal(oldSignal.aborted,true);assert.equal(m.state.phase,'allowed');m.dispose();
});
test('account transition cancels status read and suppresses late result',async()=>{
 const d=deferred();let owner='account-a',signal;const api=make(async(url,o)=>{if(url.endsWith('/me'))return response({...sessionDto,accountId:owner});signal=o.signal;return d.promise;});const session=new WebSessionModel(api);await session.restore();const m=new modelModule.MaintenanceLoginModel(api,{session});const p=m.refresh();session.expire();owner='account-b';await session.restore();d.resolve(response(blocked));await p;assert.equal(signal.aborted,true);assert.equal(m.state.phase,'unknown');assert.equal(m.state.snapshot,null);m.dispose();session.dispose();
});
test('disposal aborts and removes observers; raw error or late status cannot be published',async()=>{
 const d=deferred();let signal;const m=new modelModule.MaintenanceLoginModel(make(async(_u,o)=>{signal=o.signal;return d.promise;}));let calls=0;m.subscribe(()=>calls++);const p=m.refresh();m.dispose();const count=calls;d.resolve(response(blocked));await p;assert.equal(signal.aborted,true);assert.equal(calls,count);assert.equal(m.state.phase,'disposed');assert.equal(m.state.snapshot,null);
});


test('WEB-allowed observation does not set confirmed maintenance flag',async()=>{
 const m=new modelModule.MaintenanceLoginModel(make(async()=>response({...blocked,webLoginAllowed:true})));await m.refresh();assert.equal(m.state.phase,'allowed');assert.equal(m.state.confirmed,false);m.dispose();
});
test('policy-unavailable login refusal preserves old session but reports unconfirmed status',async()=>{
 const api=make(async url=>url.endsWith('/me')?response(sessionDto):url.endsWith('/csrf')?response({contractVersion:'web-session-v1',csrfToken:'old-csrf'}):response({error:'maintenance_policy_unavailable',contractVersion:'maintenance-login-v1',confirmed:false},503));const session=new WebSessionModel(api);await session.restore();await session.csrfToken();await session.loginGoogle('test-id-token');assert.equal(session.state.phase,'authenticated');assert.equal(session.state.accountId,'account-a');assert.equal(session.state.maintenanceConfirmed,false);assert.equal(session.state.error,'unavailable');assert.equal(await session.csrfToken(),'old-csrf');session.dispose();
});
