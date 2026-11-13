import {it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {plan} from '../helpers/operation-map-fixture';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import app from '../../apps/game-api/src/index';
import * as view from '../../apps/game-api/src/operation-map-view';
async function fixture(pin=true){
 const f=await setup(true),p=await(await f.preview()).json(),r=await(await f.start(p.operationPreviewId)).json();
 if(pin){const s=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));s.encounterType='ORDINARY_12_NODE';s.admission.operationMapPlan=plan();s.departureResponse.encounterType=s.encounterType;s.departureResponse.admission.operationMapPlan=plan();f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(s)]);await new OperationMapStore(f.env.DB).initialize('a',r.runId);}
 return {...f,runId:r.runId,url:`/v1/runs/${r.runId}/operation-map`};
}
it('serves the full pinned map without exposing private content or writing state',async()=>{
 const f=await fixture();expect((await app.request(f.url,{},f.env)).status).toBe(401);
 const before=f.sql.export(),response=await app.request(f.url,{headers:f.headers},f.env);
 expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');const body=await response.json();
 expect(body).toMatchObject({version:'operation.map-status.v1',runId:f.runId,runStatus:'ACTIVE',revision:1,progress:{phase:'READY',pendingEdgeId:null,blockedByPendingTransactions:false},view:{version:'operation.map-view.v1',currentNodeId:'entry'}});
 expect(body.view.nodes).toHaveLength(34);expect(body.view.nodes.some((n:{eventKind:string})=>n.eventKind==='BOSS')).toBe(true);
 expect(JSON.stringify(body)).not.toMatch(/encounterProfileRef|rewardPlanRef|start_snapshot|holdIds|accountId/);
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
});
it('rejects foreign ownership and unexpected query parameters',async()=>{
 const f=await fixture();await expect(view.readOperationMapView(f.env.DB,'b',f.runId)).rejects.toThrow('operation_map_run_not_found');
 expect((await app.request(f.url+'?edge=entry:n1-0',{headers:f.headers},f.env)).status).toBe(400);
});
it('shows pending travel without moving the current node and closes only run status on termination',async()=>{
 const f=await fixture();await new OperationMapStore(f.env.DB).commit('a',f.runId,{eventId:'choose',expectedRevision:1,kind:'CONFIRM',edgeId:'entry:n1-0'});
 expect(await view.readOperationMapView(f.env.DB,'a',f.runId)).toMatchObject({revision:2,progress:{phase:'TRAVELLING',pendingEdgeId:'entry:n1-0'},view:{currentNodeId:'entry'}});
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await view.readOperationMapView(f.env.DB,'a',f.runId)).toMatchObject({runStatus:'COMPLETED',revision:2});
});
it('does not generate a map for historical starts',async()=>{
 const f=await fixture(false);const response=await app.request(f.url,{headers:f.headers},f.env);expect(response.status).toBe(409);expect((await response.json()).errorCode).toBe('operation_map_state_missing');expect(f.scalar('SELECT count(*) FROM operation_map_states')).toBe(0);
});
it('rejects a changed original snapshot instead of mixing definitions',async()=>{
 const f=await fixture();f.sql.run("UPDATE run_snapshots SET snapshot_json='{}'");
 await expect(view.readOperationMapView(f.env.DB,'a',f.runId)).rejects.toThrow('operation_map_start_changed');
});
it('projects transaction holds without exposing their identities or settling them',async()=>{
 const f=await fixture();f.sql.run('UPDATE operation_map_states SET state_json=?',[JSON.stringify({version:'operation.map-progress.v1',traversedEdgeIds:['entry:n1-0'],pendingEdgeId:null,phase:'READY',holdIds:['private-reward-key']})]);
 const before=f.sql.export(),body=await view.readOperationMapView(f.env.DB,'a',f.runId);
 expect(body.progress.blockedByPendingTransactions).toBe(true);expect(JSON.stringify(body)).not.toContain('private-reward-key');expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
});

const destination=(f:Awaited<ReturnType<typeof fixture>>,body:unknown={expectedRevision:1,edgeId:'entry:n1-0'},key='destination')=>app.request(f.url+'/destination',{method:'POST',headers:{...f.headers,'Idempotency-Key':key,'Content-Type':'application/json'},body:JSON.stringify(body)},f.env);
it('confirms a direct destination once, replays the original and keeps arrival separate',async()=>{
 const f=await fixture(),response=await destination(f);expect(response.status).toBe(200);const result=await response.json();
 expect(result).toMatchObject({version:'operation.map-destination.v1',requestId:'destination',revision:2,phase:'TRAVELLING',pendingEdgeId:'entry:n1-0'});
 expect(await(await destination(f)).json()).toEqual(result);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);
 expect((await view.readOperationMapView(f.env.DB,'a',f.runId)).view.currentNodeId).toBe('entry');
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await(await destination(f)).json()).toEqual(result);
});
it('rejects distant destinations, changed idempotent commands, and false completion fields',async()=>{
 const f=await fixture();expect((await destination(f,{expectedRevision:1,edgeId:'n5-0:n6-0'})).status).toBe(409);
 expect((await destination(f,{expectedRevision:1,edgeId:'entry:n1-0',kind:'COMPLETE'})).status).toBe(400);
 expect((await destination(f)).status).toBe(200);
 expect((await destination(f,{expectedRevision:1,edgeId:'entry:n1-1'})).status).toBe(409);
 expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);
});
it('requires authentication and an idempotency key, rejects query and oversized input',async()=>{
 const f=await fixture(),url=f.url+'/destination';
 expect((await app.request(url,{method:'POST',body:'{}'},f.env)).status).toBe(401);
 expect((await app.request(url,{method:'POST',headers:f.headers,body:'{}'},f.env)).status).toBe(400);
 expect((await app.request(url+'?override=1',{method:'POST',headers:{...f.headers,'Idempotency-Key':'query'},body:'{}'},f.env)).status).toBe(400);
 expect((await destination(f,{expectedRevision:1,edgeId:'x'.repeat(19000)})).status).toBe(400);
 expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
});
it('keeps unresolved holds and rejects stale revisions without accepting a destination',async()=>{
 const f=await fixture();expect((await destination(f,{expectedRevision:2,edgeId:'entry:n1-0'})).status).toBe(409);
 f.sql.run('UPDATE operation_map_states SET state_json=?',[JSON.stringify({version:'operation.map-progress.v1',traversedEdgeIds:['entry:n1-0'],pendingEdgeId:null,phase:'READY',holdIds:['pending']})]);
 const response=await destination(f,{expectedRevision:1,edgeId:'n1-0:n2-0'});expect((await response.json()).errorCode).toBe('operation_map_hold_active');
 expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
});
it('rolls back destination on terminal races and recovers a lost committed response',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 expect((await destination(f)).status).toBe(409);expect(f.scalar('SELECT revision FROM operation_map_states')).toBe(1);
 const g=await fixture(),batch=g.env.DB.batch.bind(g.env.DB);let lost=true;g.env.DB.batch=async statements=>{const result=await batch(statements);if(lost){lost=false;throw new Error('lost response');}return result;};
 const result=await(await destination(g)).json();expect(result.revision).toBe(2);expect(await(await destination(g)).json()).toEqual(result);
 expect(g.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(1);
});

it('returns a domain conflict at exhausted map revision without persisting unsafe numbers',async()=>{
 const f=await fixture();f.sql.run('UPDATE operation_map_states SET revision=?',[Number.MAX_SAFE_INTEGER]);
 const response=await destination(f,{expectedRevision:Number.MAX_SAFE_INTEGER,edgeId:'entry:n1-0'});
 expect(response.status).toBe(409);expect((await response.json()).errorCode).toBe('operation_map_revision_exhausted');
 expect(f.scalar('SELECT revision FROM operation_map_states')).toBe(Number.MAX_SAFE_INTEGER);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
});
it('does not publish destination if the authenticated account is disabled before the batch',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");};
 const response=await destination(f);expect(response.status).toBe(409);expect((await response.json()).errorCode).toBe('operation_map_state_conflict');
 expect(f.scalar('SELECT revision FROM operation_map_states')).toBe(1);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
});
it('denies a foreign run owner and already disabled account at the public boundary',async()=>{
 const f=await fixture();f.sql.run("UPDATE runs SET account_id='b'");
 expect((await destination(f)).status).toBe(409);expect(f.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
 const g=await fixture();g.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
 expect((await destination(g)).status).toBe(403);expect(g.scalar('SELECT count(*) FROM operation_map_receipts')).toBe(0);
});
