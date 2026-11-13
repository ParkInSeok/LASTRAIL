import test from 'node:test';
import assert from 'node:assert/strict';
import * as models from '../src/web-session-model.mjs';
import * as support from '../src/support-model.mjs';
const future='2099-01-01T00:00:00.000Z';
const apiFor=(owner='a')=>({loginPassword:async()=>({csrfToken:'csrf',expiresAt:future}),me:async()=>({accountId:owner,expiresAt:future}),csrf:async()=>({csrfToken:'new-csrf'}),logout:async()=>({loggedOut:true})});
const signed=async api=>{const m=new models.WebSessionModel(api);await m.loginPassword('user','password');return m;};
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return {promise,resolve};};

test('session publishes verified account only after /me and never publishes CSRF',async()=>{
 const d=deferred();const api=apiFor();api.me=()=>d.promise;const m=new models.WebSessionModel(api);const p=m.loginPassword('user','password');assert.equal(m.state.phase,'authenticating');assert.equal(m.state.accountId,null);d.resolve({accountId:'a',expiresAt:future});await p;assert.equal(m.state.accountId,'a');assert.equal(m.state.csrfToken,undefined);assert.equal(await m.csrfToken(),'csrf');m.dispose();
});
test('late login after logout cannot revive identity or notify disposed consumers',async()=>{
 const d=deferred();const api=apiFor();api.loginPassword=()=>d.promise;const m=new models.WebSessionModel(api);const p=m.loginPassword('user','password');await m.logout();d.resolve({csrfToken:'late',expiresAt:future});await p;assert.equal(m.state.phase,'anonymous');m.dispose();assert.equal(m.state.accountId,null);
});
test('expired session requires verification rather than extending a client timestamp',async()=>{
 let now=0;const api=apiFor();api.me=async()=>({accountId:'a',expiresAt:new Date(1000).toISOString()});const m=new models.WebSessionModel(api,{now:()=>now});await m.restore();now=1000;await assert.rejects(m.csrfToken());assert.equal(m.state.phase,'expired');assert.equal(m.state.accountId,null);m.dispose();
});
test('failed logout clears local secrets and records unconfirmed server logout',async()=>{
 const api=apiFor();api.logout=async()=>{throw Object.assign(new Error('offline'),{kind:'network'});};const m=await signed(api);await m.logout();assert.equal(m.state.phase,'anonymous');assert.equal(m.state.logoutConfirmed,false);await assert.rejects(m.csrfToken());m.dispose();
});
test('same server account restores memory-only draft after expiry; different account discards it',async()=>{
 let owner='a';const api=apiFor();api.me=async()=>({accountId:owner,expiresAt:future});const session=await signed(api);const m=new support.SupportModel(api,session);m.edit({category:'BUG_OTHER',body:'original memory draft'});session.expire();assert.equal(m.state.draft,null);await session.loginPassword('user','password');assert.equal(m.state.draft.body,'original memory draft');session.expire();owner='b';await session.loginPassword('other','password');assert.equal(m.state.draft.body,'');m.dispose();session.dispose();
});
test('explicit logout discards memory draft even when the same account logs in again',async()=>{
 const api=apiFor();const session=await signed(api);const m=new support.SupportModel(api,session);m.edit({category:'BUG_OTHER',body:'original memory draft'});await session.logout();await session.loginPassword('user','password');assert.equal(m.state.draft.body,'');m.dispose();session.dispose();
});
test('single submission locks payload and receipt404 retains the original unresolved key',async()=>{
 const sent=[];const api=apiFor();api.submitTicket=async(input,o)=>{sent.push({input,o});throw Object.assign(new Error('lost'),{kind:'network'});};api.receipt=async key=>{sent.push({key});throw Object.assign(new Error('missing'),{kind:'not-found'});};const session=await signed(api);const m=new support.SupportModel(api,session,{newKey:()=> 'original-key-0123456789'});m.edit({category:'BUG_OTHER',body:'original inquiry body'});await m.submit();assert.equal(m.state.phase,'uncertain');assert.equal(sent.length,1);await m.recover();assert.equal(m.state.phase,'uncertain');assert.equal(sent[1].key,sent[0].o.key);assert.throws(()=>m.edit({category:'BUG_OTHER',body:'changed'}));await m.replay();assert.equal(sent[2].o.key,sent[0].o.key);assert.deepEqual(sent[2].input,sent[0].input);m.dispose();session.dispose();
});
test('double submit invokes original API once and accepted receipt is not a public answer',async()=>{
 const d=deferred();let calls=0;const api=apiFor();api.submitTicket=()=>{calls++;return d.promise;};const session=await signed(api);const m=new support.SupportModel(api,session,{newKey:()=> 'original-key-0123456789'});m.edit({category:'BUG_OTHER',body:'original inquiry body'});const p=m.submit();m.submit();await new Promise(r=>setImmediate(r));assert.equal(calls,1);d.resolve({accepted:true,ticketId:'ticket-a'});await p;assert.equal(m.state.phase,'accepted');assert.equal(m.state.ticketId,'ticket-a');assert.equal(m.state.publicReply,undefined);m.dispose();session.dispose();
});
test('account switch aborts old inquiry and suppresses even an uncancellable late success',async()=>{
 let owner='a';const d=deferred();let signal;const api=apiFor();api.me=async()=>({accountId:owner,expiresAt:future});api.submitTicket=(_i,o)=>{signal=o.signal;return d.promise;};const session=await signed(api);const m=new support.SupportModel(api,session,{newKey:()=> 'original-key-0123456789'});m.edit({category:'BUG_OTHER',body:'original inquiry body'});const p=m.submit();await new Promise(r=>setImmediate(r));session.expire();owner='b';await session.loginPassword('other','password');assert.equal(signal.aborted,true);d.resolve({accepted:true,ticketId:'old-ticket'});await p;assert.equal(m.state.ticketId,null);assert.equal(m.state.draft.body,'');m.dispose();session.dispose();
});
test('dispose releases session subscription and suppresses later notifications',async()=>{
 const api=apiFor();const session=await signed(api);const m=new support.SupportModel(api,session);let count=0;m.subscribe(()=>count++);m.dispose();session.expire();assert.equal(count,0);session.dispose();
});

test('restored session logout obtains CSRF and actually confirms revocation',async()=>{
 let calls=0;const api=apiFor();api.logout=async token=>{assert.equal(token,'new-csrf');calls++;return {loggedOut:true};};const m=new models.WebSessionModel(api);await m.restore();await m.logout();assert.equal(calls,1);assert.equal(m.state.logoutConfirmed,true);m.dispose();
});
test('dispose cancels pending CSRF refresh and prevents secret revival',async()=>{
 let signal;const api=apiFor();api.csrf=o=>{signal=o.signal;return new Promise((_r,reject)=>signal.addEventListener('abort',()=>reject(signal.reason),{once:true}));};const m=new models.WebSessionModel(api);await m.restore();const pending=m.csrfToken();await new Promise(r=>setImmediate(r));m.dispose();await assert.rejects(pending);assert.equal(signal.aborted,true);
});

test('server rate delay blocks manual replay until due, keeps original request key',async()=>{
 let now=1000,calls=0;const api=apiFor();api.submitTicket=async()=>{calls++;throw Object.assign(new Error('limited'),{kind:'rate-limited',retryAfterSeconds:60});};const session=await signed(api);const m=new support.SupportModel(api,session,{now:()=>now,newKey:()=> 'original-key-0123456789'});m.edit({category:'BUG_OTHER',body:'original inquiry body'});await m.submit();assert.equal(m.state.retryAt,61000);await assert.rejects(m.replay(),e=>e.kind==='rate-limited');assert.equal(calls,1);now=61000;await m.replay();assert.equal(calls,2);m.dispose();session.dispose();
});

