import test from 'node:test';
import assert from 'node:assert/strict';
import {WebApi} from '../src/web-api.mjs';
import {WebSessionModel} from '../src/web-session-model.mjs';
import * as module from '../src/support-read-model.mjs';
const response=(data,status=200)=>new Response(JSON.stringify(data),{status});
const ticket=(id='ticket-a',revision=1)=>({ticketId:id,category:'BUG_OTHER',status:'IN_REVIEW',bodyPreview:'original inquiry',serverReceivedAt:'2026-10-01T00:00:00.000Z',revision,publicReply:'latest reply'});
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return {promise,resolve};};
async function setup(read,options={}){
 let owner='account-a';let calls=[];
 const api=new WebApi({origin:'https://api.example.test',environment:'DEV',versionCode:123,fetch:async(url,options)=>{
  const path=new URL(url).pathname;
  if(path==='/v1/web/sessions/me')return response({contractVersion:'web-session-v1',accountId:owner,expiresAt:'2099-01-01T00:00:00.000Z'});
  calls.push({url,options});return read(url,options);
 }});
 const session=new WebSessionModel(api);await session.restore();
 const reader=new module.SupportReadModel(api,session,options);
 return {reader,session,calls,switchOwner:async next=>{session.expire();owner=next;await session.restore();},dispose:()=>{reader.dispose();session.dispose();}};
}
test('list exposes loading then authenticated data with latest public reply only',async()=>{
 const d=deferred();const context=await setup(()=>d.promise);const p=context.reader.openList();assert.equal(context.reader.state.phase,'loading');d.resolve(response({contractVersion:'private-support-list-v1',items:[{...ticket(),internalNote:'private'}],nextCursor:'opaque-page'}));await p;
 assert.equal(context.reader.state.phase,'ready');assert.equal(context.reader.state.items[0].publicReply,'latest reply');assert.equal(context.reader.state.items[0].internalNote,undefined);assert.equal(context.reader.state.nextCursor,'opaque-page');assert.equal(Object.isFrozen(context.reader.state.items),true);context.dispose();
});
test('confirmed empty list and server failure stay distinct',async()=>{
 const empty=await setup(()=>response({contractVersion:'private-support-list-v1',items:[],nextCursor:null}));await empty.reader.openList();assert.equal(empty.reader.state.phase,'empty');empty.dispose();
 const failed=await setup(()=>response({error:'support_ticket_unavailable'},503));await failed.reader.openList();assert.equal(failed.reader.state.phase,'error');assert.equal(failed.reader.state.error.kind,'unavailable');assert.equal(failed.reader.state.items.length,0);failed.dispose();
});
test('next page uses original server cursor; fresh list navigation drops it',async()=>{
 let count=0;const c=await setup(()=>response({contractVersion:'private-support-list-v1',items:[ticket('ticket-'+(++count))],nextCursor:count===1?'opaque+/=':null}));await c.reader.openList();await c.reader.nextPage();assert.equal(new URL(c.calls[1].url).searchParams.get('cursor'),'opaque+/=');await c.reader.openList();assert.equal(new URL(c.calls[2].url).searchParams.has('cursor'),false);c.dispose();
});
test('new detail navigation cancels older list and ignores its late response',async()=>{
 const d=deferred();const c=await setup(u=>new URL(u).pathname.endsWith('/ticket-a')?response({contractVersion:'private-support-ticket-v1',ticket:{...ticket(),body:'original full inquiry'}}):d.promise);
 const old=c.reader.openList();await c.reader.openTicket('ticket-a');assert.equal(c.calls[0].options.signal.aborted,true);d.resolve(response({contractVersion:'private-support-list-v1',items:[],nextCursor:null}));await old;assert.equal(c.reader.state.query.kind,'detail');assert.equal(c.reader.state.ticket.body,'original full inquiry');c.dispose();
});
test('missing or other-account ticket 404 never retains previously viewed content',async()=>{
 let count=0;const c=await setup(()=>++count===1?response({contractVersion:'private-support-ticket-v1',ticket:{...ticket(),body:'original full inquiry'}}):response({error:'support_ticket_not_found'},404));await c.reader.openTicket('ticket-a');await c.reader.openTicket('ticket-b');assert.equal(c.reader.state.phase,'not-found');assert.equal(c.reader.state.ticket,null);assert.equal(c.reader.state.items.length,0);c.dispose();
});
test('account switch removes old data and cursor and aborts pending account read',async()=>{
 const d=deferred();let count=0;const c=await setup(()=>++count===1?response({contractVersion:'private-support-list-v1',items:[ticket()],nextCursor:'old-owner-cursor'}):d.promise);await c.reader.openList();const old=c.reader.nextPage();await c.switchOwner('account-b');assert.equal(c.calls[1].options.signal.aborted,true);assert.equal(c.reader.state.items.length,0);assert.equal(c.reader.state.nextCursor,null);d.resolve(response({contractVersion:'private-support-list-v1',items:[ticket()],nextCursor:'old-owner-cursor'}));await old;assert.equal(c.reader.state.items.length,0);assert.equal(c.reader.state.query,null);c.dispose();
});
test('401 expires session and hides content, 403 remains a distinct permission error',async()=>{
 const c=await setup(()=>response({error:'web_session_required'},401));await c.reader.openList();assert.equal(c.session.state.phase,'expired');assert.equal(c.reader.state.phase,'authentication-required');assert.equal(c.reader.state.items.length,0);c.dispose();
 const denied=await setup(()=>response({error:'tester_forbidden'},403));await denied.reader.openList();assert.equal(denied.reader.state.error.kind,'forbidden');assert.equal(denied.session.state.phase,'authenticated');await denied.reader.retry();assert.equal(denied.calls.length,1);denied.dispose();
});
test('explicit retry is single-flight and rereads the exact detail ID',async()=>{
 const d=deferred();let count=0;const c=await setup(()=>++count===1?response({error:'support_ticket_unavailable'},503):d.promise);await c.reader.openTicket('ticket-a');const p=c.reader.retry();c.reader.retry();assert.equal(c.calls.length,2);assert.equal(new URL(c.calls[1].url).pathname,'/v1/web/support/tickets/ticket-a');d.resolve(response({contractVersion:'private-support-ticket-v1',ticket:{...ticket('ticket-a',2),body:'original full inquiry'}}));await p;assert.equal(c.reader.state.ticket.revision,2);c.dispose();
});
test('dispose aborts request, drops data and removes owned subscriptions',async()=>{
 const d=deferred();const c=await setup(()=>d.promise);let changes=0;c.reader.subscribe(()=>changes++);const p=c.reader.openList();c.reader.dispose();const settled=changes;assert.equal(c.calls[0].options.signal.aborted,true);d.resolve(response({contractVersion:'private-support-list-v1',items:[ticket()],nextCursor:null}));await p;c.session.expire();assert.equal(changes,settled);assert.equal(c.reader.state.phase,'disposed');assert.equal(c.reader.state.items.length,0);c.session.dispose();
});


test('explicit refresh rereads latest revision without changing selected ticket',async()=>{
 let revision=0;const c=await setup(()=>response({contractVersion:'private-support-ticket-v1',ticket:{...ticket('ticket-a',++revision),body:'original full inquiry'}}));await c.reader.openTicket('ticket-a');await c.reader.refresh();assert.equal(c.reader.state.ticket.revision,2);assert.equal(c.reader.state.query.ticketId,'ticket-a');assert.equal(c.calls.length,2);c.dispose();
});
test('server rate delay blocks both retry and refresh until due',async()=>{
 let now=1000,count=0;const c=await setup(()=>++count===1?response({errorCode:'support_ticket_rate_limited',retryAfterSeconds:60},429):response({contractVersion:'private-support-list-v1',items:[],nextCursor:null}),{now:()=>now});await c.reader.openList();assert.equal(c.reader.state.retryAt,61000);await c.reader.retry();await c.reader.refresh();assert.equal(c.calls.length,1);now=61000;await c.reader.retry();assert.equal(c.calls.length,2);assert.equal(c.reader.state.phase,'empty');c.dispose();
});
