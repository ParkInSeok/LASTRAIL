import {expect,it,vi} from 'vitest';
import {createServer} from 'node:http';
import {writeFileSync,existsSync} from 'node:fs';
import {setup} from '../helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
import {readAccountState} from '../../apps/game-api/src/account-state';
import {sha256Hex} from '@lastrail/common';
import economy from '../../packages/balance/src/launch-economy-v1.json';
import {z} from 'zod';
const qa='D:/Project/LASTRAIL/QA/CommanderS1-20260922/';
it('compares real authenticated HTTP legacy refresh with v2 committed patches without another GET',async()=>{
 const f=await setup(),trace:{method:string;path:string;status:number;bytes:number}[]=[];
 const server=createServer(async(req,res)=>{try{
  const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));
  const response=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method??'GET',headers:new Headers(Object.entries(req.headers).filter(([,v])=>typeof v==='string') as [string,string][]),...(req.method==='GET'?{}:{body:Buffer.concat(chunks)})}),f.env);
  const bytes=Buffer.from(await response.arrayBuffer());trace.push({method:req.method!,path:req.url!,status:response.status,bytes:bytes.length});res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
 }catch{res.writeHead(500);res.end();}});
 await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',ok);});
 const address=server.address();if(!address||typeof address==='string')throw Error('bind');const base='http://127.0.0.1:'+address.port;
 const operators=Array.from({length:5},(_,slot)=>({slot,operatorId:'op'+slot}));
 const post=(value:object,key:string)=>fetch(base+'/v1/formation/presets/p/save',{method:'POST',headers:{...f.headers,'Idempotency-Key':key},body:JSON.stringify(value)});
 try{
  const legacy=await post({name:'before',expectedRevision:1,operators},'baseline');expect(legacy.status).toBe(200);expect(await legacy.json()).toMatchObject({refreshRequired:true});
  const read=await fetch(base+'/v1/account/state',{headers:f.headers});expect(read.status).toBe(200);await read.arrayBuffer();const baseline=trace.splice(0);
  const command={name:'after',expectedRevision:2,operators,responseVersion:'formation.command.v2.20261021'};
  const response=await post(command,'after');expect(response.status).toBe(200);const result=await response.json();
  const state=await readAccountState(f.env.DB,'a');
  expect(result).toMatchObject({refreshRequired:false,owners:{account:{mutationSequence:state.mutationSequence,preset:{id:'p',name:'after',revision:3,operators}}}});
  const normal=trace.splice(0);expect(normal.map(r=>r.method)).toEqual(['POST']);
  expect(await(await post(command,'after')).json()).toEqual(result);const recovery=trace.splice(0);
  const config=JSON.stringify(economy);f.sql.run('INSERT INTO economy_versions VALUES(?,?,?,0)',[economy.version,await sha256Hex(config),config]);f.sql.run('INSERT INTO economy_version_bindings VALUES(1,?)',[economy.version]);
  for(const currency of ['CREDITS','MACHINE_PARTS','SPECIAL_CORE'])f.sql.run('INSERT INTO account_currencies(account_id,currency,balance,updated_at) VALUES(?,?,100000,0)',['a',currency]);
  const growthBody={economyVersion:economy.version,responseVersion:'growth.command.v2.20261021'};
  const growth=await fetch(base+'/v1/trains/train_a/level-up',{method:'POST',headers:{...f.headers,'Idempotency-Key':'tcp-growth'},body:JSON.stringify(growthBody)});
  expect(growth.status).toBe(200);expect(await growth.json()).toMatchObject({refreshRequired:false,owners:{account:{train:{trainId:'train_a',level:2,revision:2}}}});
  const offline=()=>fetch(base+'/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'tcp-offline','X-Offline-Response-Version':'offline.claim.v2.20261021'}});
  const claimed=await offline();expect(claimed.status).toBe(200);const original=await claimed.json();expect(original).toMatchObject({contractVersion:'offline.claim.v2.20261021',refreshRequired:false});
  expect(await(await offline()).json()).toEqual(original);const supported=trace.splice(0);expect(supported.every(r=>r.method==='POST')).toBe(true);
  for(const path of ['/v1/recurring/state','/v1/attendance/monthly/current']){
   const timed=await fetch(base+path,{headers:{...f.headers,'X-Client-Time':'2099-01-01T00:00:00Z'}});expect(timed.status).toBe(200);
   expect(await timed.json()).toMatchObject({timeContext:{version:'data.time-resume.v1.20261021',serverTime:expect.any(Number),boundaries:expect.arrayContaining([{kind:'SERVICE_DAY_END',at:expect.any(Number)}])}});
  }
  const timeReads=trace.splice(0);
  const time=Date.now();
  f.sql.run('INSERT INTO activity_definitions VALUES(?,?)',['time_trial',JSON.stringify({kind:'ATTENDANCE',startAt:time-10000,endAt:time+1000,claimUntil:time+10000,resetOffsetMinutes:1140,counting:'DISTINCT_DAYS',dailyRewards:[],milestones:[{days:1,rewards:[]}],recovery:null})]);
  const activityPost=(path:string,key:string,body:object)=>fetch(base+'/v1/attendance/time_trial/'+path,{method:'POST',headers:{...f.headers,'Idempotency-Key':key},...(path==='check-in'?{}:{body:JSON.stringify(body)})});
  expect((await activityPost('check-in','time-attend',{})).status).toBe(200);
  const clock=vi.spyOn(Date,'now').mockReturnValue(time+2000);
  try{
   const state=await fetch(base+'/v1/attendance/time_trial',{headers:f.headers});expect(state.status).toBe(200);
   expect(await state.json()).toMatchObject({active:false,claimable:true,timeContext:{boundaries:expect.arrayContaining([{kind:'PARTICIPATION_END',at:time+1000},{kind:'CLAIM_END',at:time+10000}])}});
   const claim=await activityPost('milestones/claim','time-claim',{days:1});expect(claim.status).toBe(200);const receipt=await claim.json();
   clock.mockReturnValue(time+12000);
   expect(await(await activityPost('milestones/claim','time-claim',{days:1})).json()).toEqual(receipt);
   expect((await activityPost('milestones/claim','time-new',{days:1})).status).toBe(409);
   expect(f.scalar("SELECT COUNT(*) FROM activity_claims WHERE activity_id='time_trial' AND claim_key='milestone:1'")).toBe(1);
  }finally{clock.mockRestore();}
  const timeRecovery=trace.splice(0);
  writeFileSync(qa+'data-lifecycle-http-20261021.json',JSON.stringify({version:'data.lifecycle.v1.20261021',classification:'LOCAL_SYNTHETIC_DB_ACTUAL_AUTHENTICATED_TCP_NOT_PLAYER',scenario:'formation preset save controlled consumer comparison',baseline,normal,recovery,supported,timeReads,timeRecovery,timeBoundaryClock:'CONTROLLED_SERVER_CLOCK',memory:'NOT_MEASURED',unityPlayer:'NOT_RUN',note:'Baseline uses one account GET for the missing saved preset; not a measurement of Unity shared-three-GET flow.'},null,2));
 }finally{server.closeAllConnections();await new Promise<void>(ok=>server.close(()=>ok()));f.sql.close();}
});
it('captures the existing offline claim response baseline before completing its owner patch',async()=>{
 const f=await setup();try{
  const response=await app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'offline-baseline'}},f.env);
  const body=z.record(z.string(),z.unknown()).parse(await response.json());if(!existsSync(qa+'data-lifecycle-offline-baseline-20261021.json'))writeFileSync(qa+'data-lifecycle-offline-baseline-20261021.json',JSON.stringify({status:response.status,fields:Object.keys(body),errorCode:body.errorCode},null,2));
  expect(response.status).toBe(200);
 }finally{f.sql.close();}
});
it('completes offline v2 from the original receipt without granting twice or fetching current balances',async()=>{
 const f=await setup();try{
  const request=()=>app.request('/v1/offline-rewards/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':'offline-v2','X-Offline-Response-Version':'offline.claim.v2.20261021'}},f.env);
  const response=await request();expect(response.status).toBe(200);const original=z.object({serverClaimedAt:z.string(),rewardTransactionId:z.string()}).passthrough().parse(await response.json());
  expect(original).toMatchObject({accepted:true,contractVersion:'offline.claim.v2.20261021',refreshRequired:false,owners:{account:{kind:'PATCH',coverage:['CURRENCIES'],mutationSequence:expect.any(Number)},offline:{kind:'PATCH',lastClaimAt:original.serverClaimedAt}}});
  f.sql.run("UPDATE account_currencies SET balance=balance+7 WHERE account_id='a'");
  expect(await(await request()).json()).toEqual(original);expect(f.scalar('SELECT COUNT(*) FROM reward_transactions')).toBe(1);
 }finally{f.sql.close();}
});
it('pins the committed fuel patch in the supported local START core without admitting public combat',async()=>{
 const f=await setup(true);try{
  const preview=z.object({operationPreviewId:z.string()}).parse(await(await f.preview()).json()),response=await f.start(preview.operationPreviewId,'state-start');expect(response.status).toBe(200);const start=await response.json();
  expect(start).toMatchObject({stateContractVersion:'departure.state.v1.20261021',owners:{account:{kind:'PATCH',coverage:['FUEL'],fuel:{current:20},beforeMutationSequence:expect.any(Number),mutationSequence:expect.any(Number)}}});
  expect(await(await f.start(preview.operationPreviewId,'state-start')).json()).toEqual(start);
  expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(20);
 }finally{f.sql.close();}
});
