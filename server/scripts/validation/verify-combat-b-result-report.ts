import {createServer} from 'node:http';
import {once} from 'node:events';
import {isDeepStrictEqual} from 'node:util';
import {readFileSync,writeFileSync} from 'node:fs';
import app from '../../apps/game-api/src/index';
import {bRouteFixture} from '../../tests/helpers/combat-b-route-fixture';
import {bResultReportVersion,type BResultReceipt} from '../../apps/game-api/src/combat-b-result-report';
const qaFile='D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json',qa=JSON.parse(readFileSync(qaFile,'utf8'));
const body={version:bResultReportVersion,requestId:'ORIGINAL_EXPORT_TCP',reportOriginMapRevision:2,checkpointHash:qa.runtimeSources.checkpointHash,nodeSession:qa.runtimeSources.nodeSession};
const f=await bRouteFixture('NATIVE');await f.choose();
const server=createServer(async(req,res)=>{try{const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));const r=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method??'GET',headers:req.headers as Record<string,string>,...(req.method==='GET'?{}:{body:Buffer.concat(chunks)})}),f.env);res.writeHead(r.status,Object.fromEntries(r.headers.entries()));res.end(Buffer.from(await r.arrayBuffer()));}catch{res.writeHead(500);res.end('local_test_bridge_error');}});
server.listen(0,'127.0.0.1');await once(server,'listening');const address=server.address();if(!address||typeof address==='string')throw new Error('local_port');const origin='http://127.0.0.1:'+address.port,base='/v1/runs/'+f.runId+'/b-result-reports',checks:{name:string;status:number}[]=[];
async function call(name:string,path:string,status:number,data?:unknown,headers:Record<string,string>={}){const r=await fetch(origin+path,{method:data===undefined?'GET':'POST',headers:{...f.headers,'X-B-Result-Version':bResultReportVersion,'Idempotency-Key':body.requestId,...headers},...(data===undefined?{}:{body:JSON.stringify(data)})});if(r.status!==status)throw new Error(name+': '+r.status+' '+await r.text());checks.push({name,status});return r;}
try{
 const receipt=await(await call('original_unmodified_native_export',base,200,body)).json() as BResultReceipt;
 if(receipt.progressAuthorized||receipt.arrivalAccepted||receipt.combatTruth!=='NOT_PROVEN'||receipt.completedWaves!==2)throw new Error('verification_level_changed');
 if(!isDeepStrictEqual(receipt,await(await call('same_receipt_retry',base,200,body)).json()))throw new Error('replay_changed');
 const recovered=await(await call('original_report_recovery',base+'/'+receipt.evidenceRef,200)).json() as {receipt:BResultReceipt;report:typeof body};if(!isDeepStrictEqual(body,recovered.report)||!isDeepStrictEqual(receipt,recovered.receipt))throw new Error('report_changed');
 await call('invalid_game_bearer',base,401,body,{Authorization:'Bearer invalid'});
 await call('version_conflict',base,409,body,{'X-B-Result-Version':'old'});
 await call('client_verified_flag',base,400,{...body,verified:true});
 await call('idempotency_body_conflict',base,409,{...body,nodeSession:{...body.nodeSession,localRevision:3}});
 await call('unknown_receipt',base+'/unknown',404);
 await call('query_override',base+'?nodeId=n2-0',400,body);
 await call('body_budget',base,413,{padding:'x'.repeat(2*1024*1024)});
 if((await f.map.read('a',f.runId)).state.phase!=='TRAVELLING'||f.scalar('SELECT snapshot_json FROM run_snapshots')!==f.originalSnapshot)throw new Error('map_or_snapshot_changed');
 writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-report-tcp-20261006.json',JSON.stringify({contract:bResultReportVersion,networkEvidence:'REAL_LOOPBACK_TCP_EXISTING_GAME_AUTH',inputEvidence:'UNCHANGED_ORIGINAL_UNITY_NODE_SESSION_EXPORT',qaFile,requestBytes:Buffer.byteLength(JSON.stringify(body)),checks,receipt,originalReportRecovered:true,snapshotPreserved:true,mapPhase:'TRAVELLING',actualUnityRerun:false,operatingStart:false,operatingDeploy:false},null,2));
 console.log('Actual GAME authenticated TCP: '+checks.length+' PASS; unchanged original Unity export received, progress remains blocked.');
}finally{server.close();server.closeAllConnections();await once(server,'close');f.close();}