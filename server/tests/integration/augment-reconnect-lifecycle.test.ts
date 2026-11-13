import {afterEach,expect,it} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {plan} from '../helpers/operation-map-fixture';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import app from '../../apps/game-api/src/index';
const cleanup:(()=>void)[]=[];afterEach(()=>{for(const close of cleanup.splice(0))close();});
async function fixture(){
 const f=await setup(true);cleanup.push(()=>f.sql.close());
 const preview=await(await f.preview()).json(),runId=(await(await f.start(preview.operationPreviewId)).json()).runId as string;
 // Explicit LOCAL map/arrival fixture, not operating combat permission.
 const map=plan();map.enabledEventKinds.push('AUGMENT');map.nodes.find(n=>n.nodeId==='n1-1')!.eventKind='AUGMENT';
 const snapshot=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
 snapshot.encounterType='ORDINARY_12_NODE';snapshot.admission.operationMapPlan=map;
 f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(snapshot)]);
 const progress=new OperationMapStore(f.env.DB,{prepare:async c=>({proof:{kind:c.command.kind,verificationId:'synthetic',holdIds:[]},assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard VALUES(1)')]})});
 await progress.initialize('a',runId);
 await progress.commit('a',runId,{kind:'CONFIRM',eventId:'travel',expectedRevision:1,edgeId:'entry:n1-1'});
 await progress.commit('a',runId,{kind:'ARRIVE',eventId:'arrive',expectedRevision:2,edgeId:'entry:n1-1',evidenceRef:'synthetic'});
 const grants=new SoloAugmentStore(f.env.DB,()=>0),actor={accountId:'a',environment:'DEV'};
 await grants.issue({...actor,selectionOwner:'a',runId,eventId:'grant',stageId:'n1-1',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22']});
 const read=()=>app.request(`/v1/runs/${runId}/lifecycle`,{headers:f.headers},f.env);
 const select=()=>grants.execute(actor,runId,'grant','original-selection',{operation:'SELECT',command:{eventId:'grant',expectedRevision:0,slotIndex:0,candidateId:'O22'}});
 return {...f,runId,grants,actor,progress,read,select};
}
it('includes current original selection in the existing lifecycle response without writes',async()=>{
 const f=await fixture(),before=f.sql.export();
 const response=await f.read();expect(response.status).toBe(200);
 const body=await response.json();
 expect(body.augmentRecovery).toMatchObject({state:'SELECTING',runId:f.runId});
 expect(JSON.stringify(body.augmentRecovery)).toContain('O22');
 expect(await f.grants.read(f.actor,f.runId,'grant')).toMatchObject({state:'SELECTING',slots:[{freeRemaining:3,extraRemaining:1}]});
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
});
it('returns confirmed original in one read but never claims local checkpoint application',async()=>{
 const f=await fixture(),selected=await f.select(),before=f.sql.export();
 const body=await(await f.read()).json();
 expect(body.augmentRecovery).toMatchObject({state:'CONFIRMED_RUNTIME_CHECKPOINT_REQUIRED',runId:f.runId});
 expect(JSON.stringify(body.augmentRecovery)).toContain(selected.applicationOriginal!.applicationId);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
});
it('does not reopen an old selection on terminal or exited-node state',async()=>{
 const f=await fixture();await f.select();
 await f.progress.commit('a',f.runId,{kind:'COMPLETE',eventId:'complete',expectedRevision:3,nodeId:'n1-1',evidenceRef:'synthetic'});
 expect((await(await f.read()).json()).augmentRecovery.state).toBe('NODE_EXITED');
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect((await(await f.read()).json()).augmentRecovery.state).toBe('TERMINAL');
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
});
it('isolates foreign runs and reports invalid saved grant scope as review required',async()=>{
 const f=await fixture();
 f.sql.run("UPDATE solo_augment_grants SET account_id='b'");
 const body=await(await f.read()).json();
 expect(body.augmentRecovery.state).toBe('REVIEW_REQUIRED');
 expect(body.nextAction).toBe('REVIEW_REQUIRED');
 f.sql.run("UPDATE runs SET account_id='b'");
 expect((await f.read()).status).toBe(404);
});
it('reports valid JSON with malformed stored receipt elements without failing the lifecycle query',async()=>{
 const f=await fixture(),checkpoint=JSON.parse(String(f.scalar('SELECT checkpoint_json FROM solo_augment_grants')));
 checkpoint.receipts=['bad'];
 f.sql.run('UPDATE solo_augment_grants SET checkpoint_json=?',[JSON.stringify(checkpoint)]);
 const response=await f.read();expect(response.status).toBe(200);
 expect(await response.json()).toMatchObject({nextAction:'REVIEW_REQUIRED',augmentRecovery:{state:'REVIEW_REQUIRED'}});
});
