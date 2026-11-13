import {afterEach,expect,it} from 'vitest';
import initSqlJs from 'sql.js';
import {setup} from '../helpers/departure-fixture';

const dbs:initSqlJs.Database[]=[];
afterEach(()=>{for(const db of dbs.splice(0))db.close();});
async function fixture(){
 const f=await setup(true);dbs.push(f.sql);
 const q=await(await f.preview()).json(),run=await(await f.start(q.operationPreviewId)).json();
 const body={runId:run.runId,currentStage:'Complete',currentWaveNumber:0,completedWaveCount:5,outcome:'FAILURE',runtimeStateJson:'{}',tacticSelections:[],clientCompletedAt:'2026-10-19T00:00:00.000Z'};
 return {...f,body,finish:(request=body,key='failure-original')=>f.post(`/v1/runs/${run.runId}/finish`,request,key)};
}
it('rejects another outcome with an already committed finish key and preserves the original result',async()=>{
 const f=await fixture();expect((await(await f.finish()).json()).accepted).toBe(true);
 const r=await f.finish({...f.body,outcome:'ABANDONED'});
 expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:'run_finish_request_conflict'});
 expect(f.scalar('SELECT outcome FROM run_final_results')).toBe('FAILURE');
 expect(f.scalar('SELECT count(*) FROM run_reward_previews')).toBe(1);
});
it('returns the original finish after commit acknowledgment loss without another settlement',async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);
 f.env.DB.batch=async statements=>{const result=await batch(statements);f.env.DB.batch=batch;void result;throw new Error('lost-finish-ack');};
 const r=await f.finish();expect(r.status).toBe(200);expect(await r.json()).toMatchObject({accepted:false,resultStatus:'DUPLICATE_REPLAY',errorCode:'duplicate_replay'});
 expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM run_reward_previews')).toBe(1);
});
it('does not overwrite server expiry that wins before the finish commit',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='EXPIRED'");};
 const r=await f.finish();expect(r.status).toBe(409);
 expect(f.scalar('SELECT status FROM runs')).toBe('EXPIRED');
 expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
 expect(f.scalar('SELECT count(*) FROM run_reward_previews')).toBe(0);
});
it('converges simultaneous same-key finishes on one original result',async()=>{
 const f=await fixture(),responses=await Promise.all([f.finish(),f.finish()]);
 expect(responses.map(r=>r.status)).toEqual([200,200]);
 const replies=await Promise.all(responses.map(r=>r.json()));
 expect(new Set(replies.map(r=>r.rewardPreviewId)).size).toBe(1);
 expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(1);
});
it('retains already claimed permanent rewards when finish is replayed or a later abandon is rejected',async()=>{
 const f=await fixture(),finish=await(await f.finish()).json();
 const claim=await f.post(`/v1/runs/${f.body.runId}/reward-preview/${finish.rewardPreviewId}/claim`,{runId:f.body.runId,rewardPreviewId:finish.rewardPreviewId},'original-claim');
 expect(claim.status).toBe(200);expect((await claim.json()).status).toBe('CLAIMED');
 const transactions=f.scalar('SELECT count(*) FROM reward_transactions');
 expect((await f.finish()).status).toBe(200);
 expect((await(await f.finish({...f.body,outcome:'ABANDONED'},'later-abandon')).json()).accepted).toBe(false);
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(transactions);
 expect(f.scalar('SELECT status FROM run_reward_previews')).toBe('CLAIMED');
});
it('rolls back the finish when the account is revoked before commit',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='BANNED'");};
 expect((await f.finish()).status).toBe(409);
 expect(f.scalar('SELECT status FROM runs')).toBe('ACTIVE');
 expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
});
it('does not settle against a source snapshot replaced before commit',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.sourceGeneration','replaced')");};
 expect((await f.finish()).status).toBe(409);
 expect(f.scalar('SELECT status FROM runs')).toBe('ACTIVE');
 expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
 expect(f.scalar('SELECT count(*) FROM run_reward_previews')).toBe(0);
});
