import {writeFileSync} from 'node:fs';
import {expect,it} from 'vitest';
import app from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
import {plan} from '../helpers/operation-map-fixture';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
const version='operation.map-destination.v2.20261019';
async function fixture(){
 const f=await setup(true),p=await(await f.preview()).json(),r=await(await f.start(p.operationPreviewId)).json();
 const snapshot=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
 snapshot.encounterType='ORDINARY_12_NODE';snapshot.admission.operationMapPlan=plan();snapshot.departureResponse.encounterType=snapshot.encounterType;snapshot.departureResponse.admission.operationMapPlan=plan();
 f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(snapshot)]);
 const store=new OperationMapStore(f.env.DB);await store.initialize('a',r.runId);
 const send=(edgeId='entry:n1-0',key='destination-key',v:string|null=version)=>app.request(`/v1/runs/${r.runId}/operation-map/destination`,{method:'POST',headers:{...f.headers,'Idempotency-Key':key,...(v===null?{}:{'X-Map-Destination-Version':v})},body:JSON.stringify({edgeId,expectedRevision:1})},f.env);
 return {...f,store,runId:r.runId,send};
}
it('returns the pinned destination and original map versions in the same authenticated command response',async()=>{
 const f=await fixture();try{const reply=await f.send();expect(reply.status).toBe(200);expect(reply.headers.get('Cache-Control')).toBe('no-store');const body=await reply.json(),p=plan(),n=p.nodes.find(n=>n.nodeId==='n1-0')!;
 expect(body).toMatchObject({version,accountId:'a',runId:f.runId,requestId:'destination-key',revision:2,map:{graphId:p.graphId,graphRevision:p.graphRevision,eventAssignmentVersion:p.eventAssignmentVersion,definitionVersion:p.definitionVersion},state:{phase:'TRAVELLING',currentNodeId:'entry',pendingEdgeId:'entry:n1-0'},destination:{edgeId:'entry:n1-0',fromNodeId:'entry',toNodeId:'n1-0',node:n},nextAction:'AWAIT_VERIFIED_ARRIVAL'});
 expect(body.planHash).toMatch(/^[a-f0-9]{64}$/u);expect(body.startSnapshotHash).toMatch(/^[a-f0-9]{64}$/u);expect(body).not.toHaveProperty('seed');expect(body).not.toHaveProperty('canStart');expect(body).not.toHaveProperty('nodes');
 if(process.env.LASTRAIL_EXPORT_DESTINATION_EVIDENCE==='1')writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/node-destination-example-20261019.json',JSON.stringify({scope:'IN_PROCESS_HONO_SQLJS_FIXTURE_NOT_EXTERNAL_NETWORK_OR_UNITY',response:body},null,2));
 expect((await f.store.lookup('a',f.runId,'destination-key'))?.destination).toEqual(body);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);
 }finally{f.sql.close();}
});
it('replays the exact destination after termination and rejects another edge or reply contract for the same key',async()=>{
 const f=await fixture();try{const first=await(await f.send()).json();f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await(await f.send()).json()).toEqual(first);expect((await f.send('entry:n1-1')).status).toBe(409);expect((await f.send('entry:n1-0','destination-key',null)).status).toBe(409);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);}finally{f.sql.close();}
});
it('preserves the legacy response and rejects unknown opt-in versions without writing',async()=>{
 const f=await fixture();try{expect((await f.send('entry:n1-0','wrong','future')).status).toBe(409);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);const legacy=await(await f.send('entry:n1-0','old',null)).json();expect(legacy).toEqual({version:'operation.map-destination.v1',requestId:'old',revision:2,phase:'TRAVELLING',pendingEdgeId:'entry:n1-0'});expect((await f.store.lookup('a',f.runId,'old'))).not.toHaveProperty('destination');}finally{f.sql.close();}
});
it('rolls the enriched response and progress back together when receipt storage fails',async()=>{
 const f=await fixture();try{f.sql.run("CREATE TRIGGER fail_destination BEFORE INSERT ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");expect((await f.send()).status).toBe(500);expect((await f.store.read('a',f.runId)).revision).toBe(1);expect(await f.store.lookup('a',f.runId,'destination-key')).toBeNull();}finally{f.sql.close();}
});
it('rejects an owner revocation before the original atomic publication',async()=>{
 const f=await fixture();try{f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");};expect((await f.send()).status).toBe(409);expect((await f.store.read('a',f.runId)).revision).toBe(1);expect(await f.store.lookup('a',f.runId,'destination-key')).toBeNull();}finally{f.sql.close();}
});
it('recovers a lost acknowledgement using the exact enriched receipt',async()=>{
 const f=await fixture();try{const batch=f.env.DB.batch.bind(f.env.DB);let lose=true;f.env.DB.batch=async statements=>{const result=await batch(statements);if(lose){lose=false;throw Error('lost acknowledgement');}return result;};const reply=await f.send();expect(reply.status).toBe(200);const body=await reply.json();expect(body.version).toBe(version);expect(await(await f.send()).json()).toEqual(body);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);}finally{f.sql.close();}
});