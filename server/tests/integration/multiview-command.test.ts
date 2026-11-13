import {expect,it} from 'vitest';
import {z} from 'zod';
import {writeFileSync} from 'node:fs';
import {startDataLifecycleLoopback} from '../../scripts/local/data-lifecycle-loopback-http';
import {setup} from '../helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
it('keeps actual HTTP bulk acquired totals separate from previously granted rights and recovers the original list',async()=>{
 const trace:{method:string;path:string;status:number;bytes:number}[]=[];
 const host=await startDataLifecycleLoopback({lifetimeMs:60000,onRequest:e=>trace.push(e)});
 try{
  const b=host.bootstrap,send=(path:string,key:string,body?:object)=>fetch(b.baseUrl+path,{method:'POST',headers:{Authorization:'Bearer '+b.accessToken,'Content-Type':'application/json','Idempotency-Key':key},...(body?{body:JSON.stringify(body)}:{})});
  expect((await send('/v1/quests/trial-award/claim','individual')).status).toBe(200);
  const body={kind:'QUEST',awardIds:['trial-award','not-earned']};
  const response=await send('/v1/recurring/bulk-claims','bulk-original',body);expect(response.status).toBe(200);
  const original=z.object({resultContract:z.object({grantedCount:z.number(),alreadyGrantedCount:z.number(),unpaidCount:z.number(),unknownCount:z.number()}).passthrough(),items:z.array(z.object({includeInAcquiredTotal:z.boolean()}).passthrough())}).passthrough().parse(await response.json());
  expect(original.resultContract).toMatchObject({grantedCount:0,alreadyGrantedCount:1,unpaidCount:1,unknownCount:0});
  expect(original.items.every((i:{includeInAcquiredTotal:boolean})=>!i.includeInAcquiredTotal)).toBe(true);
  expect(await(await send('/v1/recurring/bulk-claims','bulk-original',body)).json()).toEqual(original);
  expect((await send('/v1/recurring/bulk-claims','bulk-original',{kind:'QUEST',awardIds:['trial-award']})).status).toBe(409);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-bulk-result-http-20261021.json',JSON.stringify({classification:'ACTUAL_TCP_SYNTHETIC_RIGHT_SERIALIZED_LOCAL_HOST_NOT_PLAYER',trace,acquiredTotal:0,originalListRecovered:true,normalResultGetCount:0,balanceRefresh:'LEGACY_RECEIPT_INSUFFICIENT_NOT_TESTED',player:'NOT_RUN',memory:'NOT_MEASURED'},null,2));
 }finally{await host.close();}
});
it('returns one original quest grant for two actual HTTP views with different keys',async()=>{
 const trace:{method:string;path:string;status:number;bytes:number}[]=[];
 const host=await startDataLifecycleLoopback({lifetimeMs:60000,onRequest:e=>trace.push(e)});
 try{
  const b=host.bootstrap,send=(key:string)=>fetch(b.baseUrl+'/v1/quests/trial-award/claim',{method:'POST',headers:{Authorization:'Bearer '+b.accessToken,'Idempotency-Key':key}});
  const pair=await Promise.all([send('view-a'),send('view-b')]);expect(pair.map(r=>r.status)).toEqual([200,200]);
  const receipts=await Promise.all(pair.map(r=>r.json()));expect(receipts[1]).toEqual(receipts[0]);
  expect(receipts[0]).toMatchObject({rightKey:'quest:trial-award',rewardLines:[expect.objectContaining({quantity:100})]});
  expect(receipts[0]).toMatchObject({stateContract:{version:'data.currency-display.v1.20261021',refreshRequired:false},owners:{account:{accountId:'a',kind:'PATCH',currencies:{CREDITS:100100}}}});
  expect(await(await send('view-a')).json()).toEqual(receipts[0]);
  const state=await(await fetch(b.baseUrl+'/v1/account/state',{headers:{Authorization:'Bearer '+b.accessToken}})).json();
  expect(state).toMatchObject({currencies:{CREDITS:100100}});
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-multiview-http-20261021.json',JSON.stringify({classification:'ACTUAL_TCP_SYNTHETIC_RIGHT_SERIALIZED_LOCAL_HOST_NOT_PLAYER',trace,sameOriginalGrant:true,creditDelta:100,player:'NOT_RUN',memory:'NOT_MEASURED'},null,2));
 }finally{await host.close();}
});
it('fences different-key offline claims and a double START in the existing server transaction owners',async()=>{
 const f=await setup();try{
  const claim=(key:string)=>app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':key,'X-Offline-Response-Version':'offline.claim.v2.20261021'}},f.env);
  const responses=await Promise.all([claim('different-a'),claim('different-b')]);expect(responses.map(r=>r.status).sort()).toEqual([200,409]);
  expect(f.scalar('SELECT COUNT(*) FROM reward_transactions')).toBe(1);
 }finally{f.sql.close();}
 const f2=await setup(true);try{
  const preview=z.object({operationPreviewId:z.string()}).parse(await(await f2.preview()).json());
  const responses=await Promise.all([f2.start(preview.operationPreviewId,'view-a'),f2.start(preview.operationPreviewId,'view-b')]);
  expect(responses.map(r=>r.status).sort()).toEqual([200,409]);expect(f2.scalar('SELECT COUNT(*) FROM runs')).toBe(1);expect(f2.scalar('SELECT fuel FROM fuel_state')).toBe(20);
 }finally{f2.sql.close();}
});
it('keeps sequential newly confirmed growth spending separate despite identical bodies',async()=>{
 const host=await startDataLifecycleLoopback({lifetimeMs:60000});try{
  const b=host.bootstrap,body=JSON.stringify({economyVersion:b.economyVersion,responseVersion:'growth.command.v2.20261021'});
  const call=(key:string)=>fetch(b.baseUrl+'/v1/trains/train_a/level-up',{method:'POST',headers:{Authorization:'Bearer '+b.accessToken,'Content-Type':'application/json','Idempotency-Key':key},body});
  const first=await call('new-confirmation-1');expect(first.status).toBe(200);const original=await first.json();
  const second=await call('new-confirmation-2');expect(second.status).toBe(200);expect(await second.json()).toMatchObject({owners:{account:{train:{level:3,revision:3}}}});
  expect(await(await call('new-confirmation-1')).json()).toEqual(original);
 }finally{await host.close();}
});
