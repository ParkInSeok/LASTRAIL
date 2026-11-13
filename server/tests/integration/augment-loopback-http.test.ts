import {it,expect} from 'vitest';
import {mkdtempSync,mkdirSync,rmdirSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {startAugmentLoopback} from '../../scripts/local/augment-loopback-http';
const root='D:/Project/LASTRAIL/QA/CommanderS1-20260922/';
it('serves an actual free reroll NO_CANDIDATE receipt and restart recovery without consuming the singleton budget',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-');let host=await startAugmentLoopback({directory,lifetimeMs:60000});
 const body={version:host.bootstrap.rerollVersion,command:{eventId:host.bootstrap.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22',mode:'FREE'}};
 const send=()=>fetch(host.bootstrap.baseUrl+host.bootstrap.rerollPath,{method:'POST',headers:{Authorization:'Bearer '+host.bootstrap.accessToken,'Content-Type':'application/json','Idempotency-Key':'singleton-roll'},body:JSON.stringify(body)});
 try{
  const response=await send();expect(response.status).toBe(200);const original=await response.json();
  expect(original).toMatchObject({result:{outcome:'NO_CANDIDATE',snapshot:{grantRevision:0,slots:[{freeRemaining:3,extraRemaining:1}]}}});
  await host.close();host=await startAugmentLoopback({directory,lifetimeMs:60000});
  expect(await(await send()).json()).toEqual(original);expect(host.inspect()).toEqual({receipts:1,originals:0});expect(host.requests().every(r=>r.method==='POST')).toBe(true);
 }finally{await host.close();}
});
it('does not depend on stale lock files from an abruptly terminated old process',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-');writeFileSync(join(directory,'owner.lock'),'old crashed owner');
 const host=await startAugmentLoopback({directory,lifetimeMs:60000});
 try{expect(host.bootstrap.initialSelection.state).toBe('SELECTING');}finally{await host.close();}
});
it('uses real authenticated HTTP SELECT and recovers the identical persisted original after restart without GET',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-');
 let host=await startAugmentLoopback({directory,lifetimeMs:60000});
 const send=()=>fetch(host.bootstrap.baseUrl+host.bootstrap.selectPath,{method:'POST',headers:{Authorization:'Bearer '+host.bootstrap.accessToken,'Content-Type':'application/json','Idempotency-Key':'original-choice'},body:JSON.stringify(host.bootstrap.selectBody)});
 try{
  expect(host.bootstrap.targetReadout).toMatchObject({grantRevision:host.bootstrap.initialSelection.grantRevision,cards:[{candidateId:'O22',scope:'OPERATOR',targets:null}]});
  const first=await send();expect(first.status).toBe(200);const receipt=await first.json();
  expect(receipt).toMatchObject({committed:true,result:{applicationOriginal:{effectId:'O22',applied:false}}});
  await host.close();host=await startAugmentLoopback({directory,lifetimeMs:60000});
  expect(host.bootstrap.targetReadout.grantRevision).toBe(host.bootstrap.initialSelection.grantRevision);
  expect(host.bootstrap.targetReadout.cards.map(c=>c.candidateId)).toEqual(['O22']);
  expect(await(await send()).json()).toEqual(receipt);
  expect(host.inspect()).toMatchObject({receipts:1,originals:1});
  expect(host.requests().some(r=>r.method==='GET')).toBe(false);
 }finally{await host.close();}
});
it('never acknowledges RAM-only selection when disk replacement fails; original-key restart recovers once',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-');let host=await startAugmentLoopback({directory,lifetimeMs:60000});
 const send=()=>fetch(host.bootstrap.baseUrl+host.bootstrap.selectPath,{method:'POST',headers:{Authorization:'Bearer '+host.bootstrap.accessToken,'Content-Type':'application/json','Idempotency-Key':'persist-choice'},body:JSON.stringify(host.bootstrap.selectBody)});
 const blocked=join(directory,'fixture.sqlite.next');
 try{
  mkdirSync(blocked);
  expect((await send()).status).toBe(503);
  expect((await send()).status).toBe(503);
  await host.close();rmdirSync(blocked);
  host=await startAugmentLoopback({directory,lifetimeMs:60000});
  expect(host.bootstrap.initialSelection.state).toBe('SELECTING');
  expect((await send()).status).toBe(200);
  expect(host.inspect()).toEqual({receipts:1,originals:1});
 }finally{await host.close();}
});
it('converges real duplicate network requests and rejects an altered request under the original key',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-'),host=await startAugmentLoopback({directory,lifetimeMs:60000});
 const send=(body:unknown)=>fetch(host.bootstrap.baseUrl+host.bootstrap.selectPath,{method:'POST',headers:{Authorization:'Bearer '+host.bootstrap.accessToken,'Content-Type':'application/json','Idempotency-Key':'same-choice'},body:JSON.stringify(body)});
 try{
  const responses=await Promise.all([send(host.bootstrap.selectBody),send(host.bootstrap.selectBody)]);
  expect(responses.map(r=>r.status)).toEqual([200,200]);expect(await responses[0]!.json()).toEqual(await responses[1]!.json());
  const altered=structuredClone(host.bootstrap.selectBody) as {command:{candidateId:string}};altered.command.candidateId='O23';
  expect((await send(altered)).status).toBe(409);expect(host.inspect()).toEqual({receipts:1,originals:1});
 }finally{await host.close();}
});
it('rejects missing auth and non-test routes and enforces a single durable owner',async()=>{
 const directory=mkdtempSync(root+'augment-http-test-'),host=await startAugmentLoopback({directory,lifetimeMs:60000});
 try{
  expect((await fetch(host.bootstrap.baseUrl+host.bootstrap.selectPath,{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':'x'},body:JSON.stringify(host.bootstrap.selectBody)})).status).toBe(401);
  expect((await fetch(host.bootstrap.baseUrl+'/v1/runs/start',{method:'POST'})).status).toBe(404);
  await expect(startAugmentLoopback({directory,lifetimeMs:60000})).rejects.toThrow();
  expect(host.inspect()).toMatchObject({receipts:0,originals:0});
 }finally{await host.close();}
 await expect(fetch(host.bootstrap.baseUrl+host.bootstrap.selectPath)).rejects.toThrow();
});
