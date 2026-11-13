import {expect,it} from 'vitest';
import {startDataLifecycleLoopback} from '../../scripts/local/data-lifecycle-loopback-http';
import {z} from 'zod';
import {writeFileSync} from 'node:fs';
it('serves the actual authenticated lifecycle routes, replays within the session, and closes',async()=>{
 const host=await startDataLifecycleLoopback({lifetimeMs:60000});const {baseUrl,accessToken}=host.bootstrap;
 try{
  expect((await fetch(baseUrl+'/v1/account/state')).status).toBe(401);
  expect((await fetch(baseUrl+'/v1/operations/start',{method:'POST'})).status).toBe(404);
  const headers={'Authorization':'Bearer '+accessToken,'Content-Type':'application/json','Idempotency-Key':'same'};
  for(const path of host.bootstrap.allowedGetPaths)expect((await fetch(baseUrl+path,{headers})).status,path).toBe(200);
  const command=host.bootstrap.formationCommand;
  const post=()=>fetch(baseUrl+command.path,{method:'POST',headers,body:JSON.stringify(command.body)});
  const first=await post();expect(first.status).toBe(200);const result=await first.json();expect(result).toMatchObject({refreshRequired:false});expect(await(await post()).json()).toEqual(result);
  const read=await fetch(baseUrl+'/v1/recurring/state',{headers});expect(read.status).toBe(200);expect(await read.json()).toMatchObject({timeContext:{version:'data.time-resume.v1.20261021'}});
 }finally{await host.close();}await expect(fetch(baseUrl+'/v1/account/state')).rejects.toThrow();
});
it('recovers an actually interrupted HTTP response with one original-key attempt and one committed effect',async()=>{
 const controller=new AbortController(),trace:{method:string;path:string;status:number;bytes:number}[]=[];
 let interrupt=true;
 const host=await startDataLifecycleLoopback({lifetimeMs:60000,onRequest:entry=>{trace.push(entry);if(interrupt&&entry.path==='/v1/offline-rewards/claim'){interrupt=false;controller.abort();}}});
 try{
  const b=host.bootstrap,headers={Authorization:'Bearer '+b.accessToken,'Idempotency-Key':'network-original-key','X-Offline-Response-Version':'offline.claim.v2.20261021'};
  await expect(fetch(b.baseUrl+'/v1/offline-rewards/claim',{method:'POST',headers,signal:controller.signal})).rejects.toThrow();
  const recovery=await fetch(b.baseUrl+'/v1/offline-rewards/claim',{method:'POST',headers});expect(recovery.status).toBe(200);
  const receipt=z.object({owners:z.object({account:z.object({mutationSequence:z.number()})}),refreshRequired:z.literal(false)}).parse(await recovery.json());
  expect(trace.filter(r=>r.method==='POST')).toHaveLength(2);
  // Verification GET only: never a normal consumer post-success refresh.
  const state=z.object({mutationSequence:z.number()}).parse(await(await fetch(b.baseUrl+'/v1/account/state',{headers})).json());
  expect(state.mutationSequence).toBe(receipt.owners.account.mutationSequence);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-network-retry-http-20261021.json',JSON.stringify({classification:'ACTUAL_TCP_ABORT_AFTER_SERVER_COMMIT_SYNTHETIC_DB_NOT_PLAYER',initialRequests:1,recoveryAttempts:1,normalFollowupGets:0,verificationGets:1,trace,unchangedMutationSequence:true,unityRetryBudget:'NOT_TESTED',memory:'NOT_MEASURED'},null,2));
 }finally{await host.close();}
});
it('uses real HTTP for rotation/logout and preserves successful receipts across simulated local save failures',async()=>{
 const trace:{method:string;path:string;status:number;bytes:number}[]=[];
 const host=await startDataLifecycleLoopback({lifetimeMs:60000,onRequest:entry=>trace.push(entry)});
 try{
  const b=host.bootstrap,headers={Authorization:'Bearer '+b.accessToken,'Content-Type':'application/json','Idempotency-Key':'save-failure-original-key'};
  // Harness injection is a local writer failure, not a Unity disk/memory measurement.
  const persist=(fail:boolean)=>{if(fail)throw Error('synthetic local write failure');};
  expect(()=>persist(true)).toThrow();expect(trace).toHaveLength(0);
  const send=()=>fetch(b.baseUrl+b.formationCommand.path,{method:'POST',headers,body:JSON.stringify(b.formationCommand.body)});
  const response=await send();expect(response.status).toBe(200);const receipt=await response.json();
  expect(()=>persist(true)).toThrow();persist(false);expect(trace).toHaveLength(1);
  expect(await(await send()).json()).toEqual(receipt);expect(trace).toHaveLength(2);
  expect(host.bootstrap.secondaryAccount?.accountId).toBe('b');
  const other=await fetch(b.baseUrl+'/v1/account/state',{headers:{Authorization:'Bearer '+host.bootstrap.secondaryAccount.accessToken}});
  expect(other.status).toBe(200);expect(await other.json()).toMatchObject({operators:[],formation:{presets:[]}});
  const refresh=await fetch(b.baseUrl+'/v1/auth/refresh',{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':'trial-refresh-same-account-0001'},body:JSON.stringify({refreshToken:b.refreshToken})});
  expect(refresh.status).toBe(200);const renewed=z.object({account:z.object({id:z.string()}),accessToken:z.string()}).parse(await refresh.json());expect(renewed.account.id).toBe('a');
  const logout=()=>fetch(b.baseUrl+'/v1/auth/logout',{method:'POST',headers:{Authorization:'Bearer '+renewed.accessToken}});
  const ended=await logout();expect(ended.status).toBe(200);const endedReceipt=await ended.json();expect(await(await logout()).json()).toEqual(endedReceipt);
  expect((await send()).status).toBe(401);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-account-exit-save-http-20261021.json',JSON.stringify({classification:'ACTUAL_TCP_SYNTHETIC_DB_LOCAL_WRITER_FAILURE_HARNESS_NOT_PLAYER',beforeSendFailureRequests:0,localReceiptSaveRetryAdditionalRequests:0,trace,receiptReplayEqual:true,player:'NOT_RUN',memory:'NOT_MEASURED',processRestartRecovery:'NOT_SUPPORTED_BY_MEMORY_TRIAL'},null,2));
 }finally{await host.close();}
});
