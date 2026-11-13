import {createServer} from 'node:http';
import {once} from 'node:events';
import {isDeepStrictEqual} from 'node:util';
import {writeFileSync} from 'node:fs';
import app from '../../apps/game-api/src/index';
import {bRouteFixture} from '../../tests/helpers/combat-b-route-fixture';
import {setupOriginalBDepartureFixture} from '../../tests/helpers/combat-b-departure-pin-fixture';
import {v2Report,rehashV2} from '../../tests/helpers/combat-b-result-v2-input';
import {bResultV2BodyLimit,type BResultReceipt} from '../../apps/game-api/src/combat-b-result-report';
const body=await v2Report(),f=await bRouteFixture('NATIVE');await f.choose();let env=f.env;
const server=createServer(async(req,res)=>{try{const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));const r=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method??'GET',headers:req.headers as Record<string,string>,...(req.method==='GET'?{}:{body:Buffer.concat(chunks)})}),env);res.writeHead(r.status,Object.fromEntries(r.headers.entries()));res.end(Buffer.from(await r.arrayBuffer()));}catch{res.writeHead(500);res.end('local_test_bridge_error');}});
server.listen(0,'127.0.0.1');await once(server,'listening');const a=server.address();if(!a||typeof a==='string')throw new Error('local_port');const origin='http://127.0.0.1:'+a.port,base='/v1/runs/'+f.runId+'/b-result-reports',checks:{name:string;status:number}[]=[];
async function call(name:string,path:string,status:number,data?:unknown,headers:Record<string,string>={}){const r=await fetch(origin+path,{method:data===undefined?'GET':'POST',headers:{...f.headers,'X-B-Result-Version':body.version,'Idempotency-Key':body.requestId,...headers},...(data===undefined?{}:{body:JSON.stringify(data)})});if(r.status!==status)throw new Error(name+': '+r.status+' '+await r.text());checks.push({name,status});return r;}
try{
 const receipt=await(await call('v2_original_journal_derived_transport',base,200,body)).json() as BResultReceipt;
 if(receipt.progressAuthorized||receipt.arrivalAccepted||receipt.combatTruth!=='NOT_PROVEN'||receipt.originalJournal?.terminal.commandId!=='resume-basic-361'||receipt.originalJournal.waves[1]?.rawSequence!==1048||receipt.originalJournal.waves[1]?.pointSequence!==1063)throw new Error('verification_level_or_cursor_changed');
 if(!isDeepStrictEqual(receipt,await(await call('same_receipt_retry',base,200,body)).json()))throw new Error('receipt_changed');
 const recovered=await(await call('complete_original_report_recovery',base+'/'+receipt.evidenceRef,200)).json() as {receipt:BResultReceipt;report:typeof body;storage?:unknown};
 if(!isDeepStrictEqual(body,recovered.report)||!isDeepStrictEqual(receipt,recovered.receipt)||recovered.storage!==undefined)throw new Error('report_changed');
 await call('invalid_game_bearer',base,401,body,{Authorization:'Bearer invalid'});
 await call('header_body_version_conflict',base,409,{version:body.version,requestId:body.requestId},{'X-B-Result-Version':'combat.b-result-report.v1.20261006'});
 await call('client_verified_flag',base,400,{...body,verified:true});
 await call('idempotency_body_conflict',base,409,{...body,nodeSession:{...body.nodeSession,localRevision:3}});
 await call('unknown_receipt',base+'/unknown',404);
 await call('query_override',base+'?nodeId=n2-0',400,body);
 await call('v2_body_budget',base,413,{padding:'x'.repeat(bResultV2BodyLimit+1)});
 const bad=await v2Report();bad.requestId='bad-journal';bad.nodeSession.runtime.originalEvents.records[1].binding.originalCommand.eventId='substitute';await rehashV2(bad,true);await call('rehashed_original_command_substitution',base,409,bad,{'Idempotency-Key':bad.requestId});
 const original=await setupOriginalBDepartureFixture();let input:unknown;try{const start=await original.start();env=original.env;input=await(await call('actual_original_producer_frozen_input','/v1/runs/'+start.runId+'/b-departure-runtime-input',200,undefined,{...original.headers,'X-B-Departure-Input-Version':'combat.b-departure-runtime-input.v1.20261006'})).json();}finally{env=f.env;original.close();}
 if((await f.map.read('a',f.runId)).state.phase!=='TRAVELLING'||f.scalar('SELECT snapshot_json FROM run_snapshots')!==f.originalSnapshot)throw new Error('map_or_snapshot_changed');
 writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-v2-tcp-20261006.json',JSON.stringify({contract:body.version,networkEvidence:'REAL_LOOPBACK_TCP_EXISTING_GAME_AUTH',inputEvidence:'DERIVED_V2_TRANSPORT_FROM_PRESERVED_V1_CAPTURE_PLUS_UNCHANGED_ORIGINAL_V2_JOURNAL_NOT_FRESH_FULL_UNITY_V2_EXPORT',originalJournalUnchanged:true,requestBytes:Buffer.byteLength(JSON.stringify(body)),checks,receipt,originalDepartureInput:input,originalReportRecovered:true,snapshotPreserved:true,mapPhase:'TRAVELLING',actualUnityRerun:false,operatingStart:false,operatingDeploy:false},null,2));
 console.log('Actual GAME authenticated TCP: '+checks.length+' PASS; original v2 journal recovered; progress remains blocked.');
}finally{server.close();server.closeAllConnections();await once(server,'close');f.close();}
