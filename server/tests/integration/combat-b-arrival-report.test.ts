import {readFileSync} from 'node:fs';
import {expect,it} from 'vitest';
import {Hono} from 'hono';
import app from '../../apps/game-api/src/index';
import {bRouteVersion} from '../../apps/game-api/src/combat-b-route-adapter';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {bRouteFixture} from '../helpers/combat-b-route-fixture';
import {BArrivalReportStore,BArrivalConsistencyOwner,bArrivalReportVersion,bArrivalBodyLimit} from '../../apps/game-api/src/combat-b-arrival-report';
import {registerBArrivalReportRoutes} from '../../apps/game-api/src/combat-b-arrival-report-routes';
const qa=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json','utf8'));
const input=()=>({version:bArrivalReportVersion,requestId:'arrival-original-1',destinationEventId:'select',reportOriginMapRevision:2,checkpointHash:qa.runtimeSources.checkpointHash,nodeSession:structuredClone(qa.runtimeSources.nodeSession)});
it('actual authenticated app stores original capture but refuses ARRIVE and issuer effects without arrival producer',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();const base='/v1/runs/'+f.runId;
 const response=await app.request(base+'/b-arrival-reports',{method:'POST',headers:{...f.headers,'X-B-Arrival-Version':bArrivalReportVersion,'Idempotency-Key':input().requestId},body:JSON.stringify(input())},f.env);
 expect(response.status).toBe(200);const r=await response.json() as {evidenceRef:string};
 const result=await app.request(base+'/b-route/transition',{method:'POST',headers:{...f.headers,'X-B-Route-Version':bRouteVersion,'Idempotency-Key':'arrival-unproven'},body:JSON.stringify({version:bRouteVersion,command:{kind:'ARRIVE',eventId:'arrival-unproven',expectedRevision:2,edgeId:'entry:n1-0',evidenceRef:r.evidenceRef}})},f.env);
 expect(result.status).toBe(409);expect(await result.json()).toMatchObject({accepted:false,errorCode:'combat_b_route_original_source_unavailable'});expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE event_id='arrival-unproven'")).toBe(0);
 }finally{f.close();}
});
it('validates actual native capture and durable destination without manufacturing an arrival',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();const store=new BArrivalReportStore(f.env.DB),body=input(),r=await store.submit('a',f.runId,body);
 expect(r.arrivalAccepted).toBe(false);expect(r.combatTruth).toBe('NOT_PROVEN');expect(r.progressAuthorized).toBe(false);expect(f.scalar('SELECT count(*) FROM operation_map_write_guard')).toBe(0);
 expect(await store.submit('a',f.runId,body)).toEqual(r);expect((await store.read('a',f.runId,r.evidenceRef))?.receipt).toEqual(r);
 expect(await new BArrivalConsistencyOwner(f.env.DB).prepare({accountId:'a',runId:f.runId,nodeId:r.nodeId,mapRevision:2,kind:'ARRIVE',evidenceRef:r.evidenceRef})).toEqual({status:'UNSUPPORTED',missingSources:r.missingSources});
 expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(f.originalSnapshot);
 }finally{f.close();}
});
it('rejects wrong destination, changed source, impossible sequence, forged support and hash-only evidence',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();const store=new BArrivalReportStore(f.env.DB);
 for(const edit of [(b:ReturnType<typeof input>)=>b.destinationEventId='absent',(b:ReturnType<typeof input>)=>b.nodeSession.runtime.travel.nodeId='wrong',(b:ReturnType<typeof input>)=>b.nodeSession.runtime.travel.sequence++,(b:ReturnType<typeof input>)=>b.nodeSession.runtime.travel.routeSelectionSupported=true]){const b=input();edit(b);b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(b.nodeSession.runtime));await expect(store.submit('a',f.runId,b)).rejects.toThrow();}
 await expect(store.submit('a',f.runId,{version:bArrivalReportVersion,requestId:'forged',checkpointHash:input().checkpointHash,arrivalAccepted:true})).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);
 }finally{f.close();}
});
it('fences original map and destination receipt in the same storage batch',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run('UPDATE operation_map_states SET revision=revision+1');};await expect(new BArrivalReportStore(f.env.DB).submit('a',f.runId,input())).rejects.toThrow('state_conflict');expect(f.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);}finally{f.close();}
});
it('preserves original bytes and rejects changed replay, cross-account access and revoked replay',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();const store=new BArrivalReportStore(f.env.DB),r=await store.submit('a',f.runId,input()),changed=input();changed.nodeSession.localRevision++;
 await expect(store.submit('a',f.runId,changed)).rejects.toThrow('request_conflict');await expect(store.read('b',f.runId,r.evidenceRef)).rejects.toThrow();expect(()=>f.sql.run('DELETE FROM b_arrival_reports')).toThrow('immutable');
 f.sql.run('INSERT INTO b_arrival_report_revocations VALUES(?,?,?)',[f.runId,r.evidenceRef,'original source withdrawn']);await expect(store.submit('a',f.runId,input())).rejects.toThrow('revoked');await expect(store.read('a',f.runId,r.evidenceRef)).rejects.toThrow('revoked');
 }finally{f.close();}
});
it('authenticates bounded transport, checks version and idempotency, and recovers original receipt',async()=>{
 const f=await bRouteFixture('NATIVE');try{await f.choose();const app=new Hono<{Bindings:Env}>();registerBArrivalReportRoutes(app,async c=>c.req.header('Authorization')==='Bearer test'?{accountId:'a'}:{errorCode:'unauthorized',status:401,message:'unauthorized'});
 const path='/v1/runs/'+f.runId+'/b-arrival-reports',headers={'Authorization':'Bearer test','Content-Type':'application/json','X-B-Arrival-Version':bArrivalReportVersion,'Idempotency-Key':'arrival-original-1'};
 expect((await app.request(path,{method:'POST',body:JSON.stringify(input())},f.env)).status).toBe(401);
 expect((await app.request(path,{method:'POST',headers,body:' '.repeat(bArrivalBodyLimit+1)},f.env)).status).toBe(413);
 const response=await app.request(path,{method:'POST',headers,body:JSON.stringify(input())},f.env);expect(response.status).toBe(200);const r=await response.json() as {evidenceRef:string;arrivalAccepted:boolean};expect(r.arrivalAccepted).toBe(false);
 expect((await app.request(path+'/'+r.evidenceRef,{headers},f.env)).status).toBe(200);
 expect((await app.request(path,{method:'POST',headers:{...headers,'Idempotency-Key':'wrong'},body:JSON.stringify(input())},f.env)).status).toBe(400);
 }finally{f.close();}
});
