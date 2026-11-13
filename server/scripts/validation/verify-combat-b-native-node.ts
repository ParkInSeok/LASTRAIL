import {isDeepStrictEqual} from 'node:util';
import {z} from 'zod';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {writeFileSync} from 'node:fs';
import app from '../../apps/game-api/src/index';
import {bRouteFixture} from '../../tests/helpers/combat-b-route-fixture';
import {bNativeNodeVersion} from '../../apps/game-api/src/combat-b-native-node-source';
const packet=(raw:unknown)=>z.object({packetHash:z.string().regex(/^[a-f0-9]{64}$/u),originalNodeSource:z.object({sourceHash:z.string(),priorSourceHash:z.string().nullable()}).passthrough()}).passthrough().parse(raw);
const f=await bRouteFixture('NATIVE'),server=createServer(async(req,res)=>{try{const r=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method??'GET',headers:req.headers as Record<string,string>,...(req.method==='GET'?{}:{body:await (async()=>{const chunks:Buffer[]=[];for await(const c of req)chunks.push(Buffer.from(c));return Buffer.concat(chunks);})()})}),f.env);res.writeHead(r.status,Object.fromEntries(r.headers.entries()));res.end(Buffer.from(await r.arrayBuffer()));}catch(e){res.writeHead(500);res.end(String(e));}});
server.listen(0,'127.0.0.1');await once(server,'listening');const addr=server.address();if(!addr||typeof addr==='string')throw new Error('port');const origin='http://127.0.0.1:'+addr.port,base='/v1/runs/'+f.runId,checks:{name:string;status:number}[]=[];
async function call(name:string,path:string,status:number,body?:unknown,headers:Record<string,string>={}){const r=await fetch(origin+path,{method:body===undefined?'GET':'POST',headers:{...f.headers,'X-B-Native-Node-Version':bNativeNodeVersion,...headers},...(body===undefined?{}:{body:JSON.stringify(body)})});if(r.status!==status)throw new Error(name+' '+r.status+' '+await r.text());checks.push({name,status:r.status});return r;}
try{
 await call('entry_has_no_ordinary_node',base+'/b-native-node-source',409);
 await call('original_destination_select',base+'/operation-map/destination',200,{expectedRevision:1,edgeId:'entry:n1-0'},{'Idempotency-Key':'SELECT_NATIVE_TCP'});
 const first=packet(await(await call('actual_first_native_source',base+'/b-native-node-source',200)).json());
 const replay=await(await call('original_source_replay',base+'/b-native-node-source',200,undefined,{'If-Match':first.packetHash})).json();if(!isDeepStrictEqual(first,replay))throw new Error('replay changed');
 await call('invalid_game_bearer',base+'/b-native-node-source',401,undefined,{Authorization:'Bearer invalid'});
 await call('version_conflict',base+'/b-native-node-source',409,undefined,{'X-B-Native-Node-Version':'old'});
 await call('etag_conflict',base+'/b-native-node-source',409,undefined,{'If-Match':'0'.repeat(64)});
 await call('client_node_override',base+'/b-native-node-source?nodeId=n2-0',400);
 // Private LOCAL accepted events only to advance original map; NOT actual Unity runtime.
 await f.arrive();await f.wave(1);await f.wave(2);await f.record('COMPLETE','complete','n1-0',3);await f.map.commit('a',f.runId,{kind:'COMPLETE',eventId:'complete',expectedRevision:3,nodeId:'n1-0',evidenceRef:'LOCAL_ACCEPTED_ORIGINAL_REF'});
 await call('original_later_destination_select',base+'/operation-map/destination',200,{expectedRevision:4,edgeId:'n1-0:n2-0'},{'Idempotency-Key':'SELECT_NATIVE_2_TCP'});
 const later=packet(await(await call('actual_later_native_source',base+'/b-native-node-source',200)).json());if(later.originalNodeSource.priorSourceHash!==first.originalNodeSource.sourceHash)throw new Error('prior source');
 const laterReplay=await(await call('later_source_restore_read',base+'/b-native-node-source',200)).json();if(!isDeepStrictEqual(later,laterReplay)||f.scalar('SELECT snapshot_json FROM run_snapshots')!==f.originalSnapshot)throw new Error('snapshot/replay changed');
 writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-native-node-sample-20261005.json',JSON.stringify({version:bNativeNodeVersion,networkEvidence:'REAL_LOOPBACK_TCP_EXISTING_GAME_AUTH',mapAdvanceEvidence:'PRIVATE_LOCAL_ACCEPTED_EVENT_SQL_FIXTURE_NOT_ACTUAL_UNITY_RUNTIME',origin,checks,first,replay,later,laterReplay,originalSnapshotPreserved:true,actualUnityFactoryExecuted:false},null,2));
 console.log('TCP '+checks.length+' PASS; exact original v1 wave/v2 boarding/v3 intercept sources supplied.');
}finally{server.close();await once(server,'close');f.close();}
