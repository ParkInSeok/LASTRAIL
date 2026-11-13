import {readFileSync} from 'node:fs';
import {BResultConsistencyOwner} from '../../apps/game-api/src/combat-b-result-consistency-owner';
import app from '../../apps/game-api/src/index';
import {expect,it} from 'vitest';
import {bRouteFixture} from '../helpers/combat-b-route-fixture';
import {BResultReportStore,bResultReportVersion,type BResultReceipt} from '../../apps/game-api/src/combat-b-result-report';
const qa=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json','utf8'));
const input=()=>({version:bResultReportVersion,requestId:'original-export-1',reportOriginMapRevision:2,checkpointHash:qa.runtimeSources.checkpointHash,nodeSession:structuredClone(qa.runtimeSources.nodeSession)});
it('receives the exact original Unity export without upgrading LOCAL formation to departure authority',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const store=new BResultReportStore(f.env.DB),body=input(),r=await store.submit('a',f.runId,body);expect(r.combatTruth).toBe('NOT_PROVEN');expect(r.fullNativeReplayPerformed).toBe(false);expect(r.arrivalAccepted).toBe(false);expect(r.progressAuthorized).toBe(false);expect(r.missingSources).toContain('ORIGINAL_DEPARTURE_FORMATION');expect(r.completedWaves).toBe(2);expect(await store.submit('a',f.runId,body)).toEqual(r);expect((await store.read('a',f.runId,r.evidenceRef))?.receipt).toEqual(r);expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(f.originalSnapshot);f.close();});
it('rejects altered report source, digest, wave order and duplicate commands',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const store=new BResultReportStore(f.env.DB);for(const edit of [(b:ReturnType<typeof input>)=>b.nodeSession.node.sourceHash='0'.repeat(64),(b:ReturnType<typeof input>)=>b.checkpointHash='sha256:'+'0'.repeat(64),(b:ReturnType<typeof input>)=>b.nodeSession.runtime.waveEntry.events.reverse(),(b:ReturnType<typeof input>)=>b.nodeSession.runtime.commands.push(b.nodeSession.runtime.commands[0])]){const b=input();edit(b);await expect(store.submit('a',f.runId,b)).rejects.toThrow();}expect(f.scalar('SELECT count(*) FROM b_result_reports')).toBe(0);f.close();});
it('preserves immutable receipt and rejects changed idempotency body and wrong account',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const store=new BResultReportStore(f.env.DB),r=await store.submit('a',f.runId,input()),b=input();b.requestId='original-export-1';b.nodeSession.localRevision++;await expect(store.submit('a',f.runId,b)).rejects.toThrow('request_conflict');await expect(store.read('b',f.runId,r.evidenceRef)).rejects.toThrow();expect(()=>f.sql.run('DELETE FROM b_result_reports')).toThrow('immutable');f.close();});
it('rolls back a stale original map in the same report commit',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const store=new BResultReportStore(f.env.DB);f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run('UPDATE operation_map_states SET revision=revision+1');};await expect(store.submit('a',f.runId,input())).rejects.toThrow('state_conflict');expect(f.scalar('SELECT count(*) FROM b_result_reports')).toBe(0);f.close();});
it('provides the actual GAME authenticated HTTP registration and private recovery path',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const body=input(),path='/v1/runs/'+f.runId+'/b-result-reports';const headers={...f.headers,'X-B-Result-Version':bResultReportVersion,'Idempotency-Key':body.requestId};const response=await app.request(path,{method:'POST',headers,body:JSON.stringify(body)},f.env);expect(response.status).toBe(200);const receipt=await response.json() as BResultReceipt;expect(receipt.completedWaves).toBe(2);expect(receipt.progressAuthorized).toBe(false);const recovered=await app.request(path+'/'+receipt.evidenceRef,{headers},f.env);expect(recovered.status).toBe(200);expect((await recovered.json() as {receipt:BResultReceipt}).receipt).toEqual(receipt);expect((await app.request(path,{method:'POST',headers:{'Content-Type':'application/json','X-B-Result-Version':bResultReportVersion},body:JSON.stringify(body)},f.env)).status).toBe(401);expect((await app.request(path,{method:'POST',headers,body:JSON.stringify({...body,verified:true})},f.env)).status).toBe(400);f.close();});
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {readBNativeNodeSource} from '../../apps/game-api/src/combat-b-native-node-source';
import {BOriginalAcceptedBroker} from '../../apps/game-api/src/combat-b-original-accepted-broker';
import {BRouteMapVerifier,BRouteWaveVerifier,} from '../../apps/game-api/src/combat-b-route-adapter';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {BOperationProgressStore} from '../../apps/game-api/src/combat-b-operation-progress';
const admitted={formation:{operators:[{slot:0,operatorId:'op_kyle'}]},loadout:{trainId:'train',carriages:[{slot:0,carriageId:'car0'}]},ownedState:{train:{trainId:'train'},operators:[{operatorId:'op_kyle'}]}};
it.each(['MISSING_ENEMY','LIVING_ENEMY','REPLACEMENT_ID','MISSING_BOARDER','LIVING_BOARDER'] as const)('does not turn disengagement into completion: %s',async kind=>{
 const f=await bRouteFixture('NATIVE',admitted);
 try{
  await f.choose();const body=await compatibleLocalReport(f),r=body.nodeSession.runtime;
  const enemyId=Object.keys(r.state.direct.enemies)[0]!,life=r.boarding.lives.find((v:{id:string})=>v.id===enemyId)!;
  if(kind==='MISSING_ENEMY')delete r.state.direct.enemies[enemyId];
  if(kind==='LIVING_ENEMY')r.state.direct.enemies[enemyId].hp=1;
  if(kind==='REPLACEMENT_ID'){r.state.direct.enemies[enemyId+'-rejoined']=r.state.direct.enemies[enemyId];delete r.state.direct.enemies[enemyId];life.id=enemyId+'-rejoined';}
  if(kind==='MISSING_BOARDER')r.boarding.lives=r.boarding.lives.filter((v:{id:string})=>v.id!==enemyId);
  if(kind==='LIVING_BOARDER'){life.phase='Rooftop';life.registered=true;life.targetable=true;life.attackEligible=true;}
  body.checkpointHash='sha256:'+await sha256Hex(canonicalJson(r));
  await expect(new BResultReportStore(f.env.DB).submit('a',f.runId,body)).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM b_result_reports')).toBe(0);
  expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
  expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(f.originalSnapshot);
 }finally{f.close();}
});
async function compatibleLocalReport(f:Awaited<ReturnType<typeof bRouteFixture>>){const body=input(),p=await readBNativeNodeSource(f.env.DB,'a',f.runId);body.nodeSession.node=structuredClone(p.originalNodeSource);body.nodeSession.runtime.waveSourceHash=p.originalNodeSource.sourceHash;body.nodeSession.runtime.travel.nodeSourceHash=p.originalNodeSource.sourceHash;const r=body.nodeSession.runtime,instances=p.originalNodeSource.waves.flatMap(w=>w.instances);r.waveEntry.cursor=p.originalNodeSource.waves[1]!.instances.length;r.state.direct.enemies=Object.fromEntries(instances.map(i=>[i.instanceId,{hp:0,maxHp:Number(i.hp)}]));r.boarding.lives=instances.map(i=>({id:i.instanceId,phase:'Dead',registered:false,targetable:false,attackEligible:false}));for(const c of r.commands)if(c.kind==='WAVE_BASIC')c.targetId=instances[0]!.instanceId;body.checkpointHash='sha256:'+await sha256Hex(canonicalJson(r));return body;} // Explicit LOCAL compatible departure, adjusted source; NOT unchanged actual Unity execution evidence.
it('consumes server consistency receipt through original wave and node stores after a separate arrival receipt',async()=>{const f=await bRouteFixture('NATIVE',admitted);await f.choose();const body=await compatibleLocalReport(f),store=new BResultReportStore(f.env.DB),receipt=await store.submit('a',f.runId,body);expect(receipt.progressAuthorized).toBe(true);const broker=new BOriginalAcceptedBroker(f.env.DB,new BResultConsistencyOwner(f.env.DB)),map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,broker)),waves=new BOperationProgressStore(f.env.DB,new BRouteWaveVerifier(f.env.DB,broker));await expect(map.commit('a',f.runId,{kind:'ARRIVE',eventId:'no-arrival',expectedRevision:2,edgeId:'entry:n1-0',evidenceRef:receipt.evidenceRef})).rejects.toThrow('original_source_unavailable');await f.arrive();for(const ordinal of [1,2])await waves.completeWave('a',f.runId,{eventId:'result-wave'+ordinal,nodeId:'n1-0',waveOrdinal:ordinal,expectedMapRevision:3,evidenceRef:receipt.evidenceRef});const c={kind:'COMPLETE',eventId:'result-complete',expectedRevision:3,nodeId:'n1-0',evidenceRef:receipt.evidenceRef};expect((await map.commit('a',f.runId,c)).state.phase).toBe('READY');expect(await map.commit('a',f.runId,c)).toEqual(await map.lookup('a',f.runId,c.eventId));expect((await waves.read('a',f.runId)).completedWaves).toBe(2);f.close();});
it('does not authorize a departed actor replacement from a matching hash',async()=>{const f=await bRouteFixture('NATIVE',{...admitted,formation:{operators:[{slot:0,operatorId:'op_sable'}]}});await f.choose();const body=await compatibleLocalReport(f);await expect(new BResultReportStore(f.env.DB).submit('a',f.runId,body)).rejects.toThrow('departure_formation_conflict');f.close();});
it.each(['TIME','TRAIN','BUDGET','HP'] as const)('rejects inconsistent original report %s',async kind=>{const f=await bRouteFixture('NATIVE');await f.choose();const b=input(),r=b.nodeSession.runtime;if(kind==='TIME'){const i=r.commands.findIndex((c:{kind:string})=>c.kind==='WAVE_BASIC');r.results[i].tick++;}if(kind==='TRAIN')r.commands.find((c:{kind:string})=>c.kind==='WAVE_BASIC').sourceId='train';if(kind==='BUDGET')r.commands[0].maximumSteps=10001;if(kind==='HP')r.state.direct.enemies[Object.keys(r.state.direct.enemies)[0]!].maxHp=1;b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(r));await expect(new BResultReportStore(f.env.DB).submit('a',f.runId,b)).rejects.toThrow();f.close();});
it('accepts the native zero-step terminal no-op without advancing its tick',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const b=input();b.nodeSession.runtime.commands.at(-1).maximumSteps=0;b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(b.nodeSession.runtime));expect((await new BResultReportStore(f.env.DB).submit('a',f.runId,b)).completedWaves).toBe(2);f.close();});
it('accepts native terminal pause without changing victory tick',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const b=input(),r=b.nodeSession.runtime;r.commands.push({kind:'PAUSE',eventId:'terminal-pause',value:false});r.results.push(structuredClone(r.results.at(-1)));r.travel.commandRevision++;r.sequence++;r.travel.sequence=r.sequence;b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(r));expect((await new BResultReportStore(f.env.DB).submit('a',f.runId,b)).completedWaves).toBe(2);f.close();});
it('rejects native movement while paused even when tick and final pause state match',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const b=input(),r=b.nodeSession.runtime,i=r.commands.findIndex((c:{kind:string})=>c.kind==='ORDINARY_MOVE'),result=structuredClone(r.results[i]);r.commands.splice(i,0,{kind:'PAUSE',eventId:'illegal-paused-move',value:true});r.results.splice(i,0,result);r.commands.splice(i+2,0,{kind:'PAUSE',eventId:'resume-illegal-move',value:false});r.results.splice(i+2,0,structuredClone(result));r.travel.commandRevision+=2;r.sequence+=2;r.travel.sequence=r.sequence;b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(r));await expect(new BResultReportStore(f.env.DB).submit('a',f.runId,b)).rejects.toThrow('paused_movement');f.close();});
it('preserves original admission rejection of cooperative departure before solo reporting',async()=>{const {captureCombatAuthority}=await import('../../apps/game-api/src/combat-authority');await expect(bRouteFixture('NATIVE',{combatAuthority:captureCombatAuthority('ORDINARY_COOP')})).rejects.toThrow('mode_unsupported');});
it('rejects unknown or incomplete installed hazard owners',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();for(const kind of ['UNKNOWN','MISSING']){const b=input();if(kind==='UNKNOWN')b.nodeSession.observation.hazards.unknown=['UNSUPPORTED_OWNER'];else b.nodeSession.observation.hazards.owners.pop();await expect(new BResultReportStore(f.env.DB).submit('a',f.runId,b)).rejects.toThrow('hazard_registry_conflict');}f.close();});
it('rejects replacement of an accepted original event and command cursor under a new request key',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const store=new BResultReportStore(f.env.DB);await store.submit('a',f.runId,input());const b=input();b.requestId='changed-request';b.nodeSession.runtime.commands[0].eventId='changed-original-command';b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(b.nodeSession.runtime));await expect(store.submit('a',f.runId,b)).rejects.toThrow('original_event_conflict');expect(f.scalar('SELECT count(*) FROM b_result_reports')).toBe(1);f.close();});
it('replays an identical report when another commit wins during duplicate validation',async()=>{const f=await bRouteFixture('NATIVE');await f.choose();const originalPrepare=f.adapter.prepare.bind(f.adapter);let duplicateReads=0,release!:()=>void;const firstCommitted=new Promise<void>(resolve=>{release=resolve;});const originalBatch=f.env.DB.batch.bind(f.env.DB);f.env.DB.batch=async statements=>{const result=await originalBatch(statements);release();return result;};f.adapter.prepare=sql=>{const s=originalPrepare(sql);if(sql.startsWith('SELECT 1 FROM b_result_reports')){const read=s.first.bind(s);s.first=async<T>()=>{if(++duplicateReads===2)await firstCommitted;return read<T>();};}return s;};try{const store=new BResultReportStore(f.env.DB),first=store.submit('a',f.runId,input()).then(r=>{release();return r;},e=>{release();throw e;}),second=store.submit('a',f.runId,input()),results=await Promise.allSettled([first,second]);expect(results.map(r=>r.status)).toEqual(['fulfilled','fulfilled']);if(results[0]!.status==='fulfilled'&&results[1]!.status==='fulfilled')expect(results[0].value).toEqual(results[1].value);expect(f.scalar('SELECT count(*) FROM b_result_reports')).toBe(1);}finally{release();f.adapter.prepare=originalPrepare;f.env.DB.batch=originalBatch;f.close();}});

import {EquipmentRewardEffectSource} from '../../apps/game-api/src/equipment-reward-effects';
import {EquipmentRewardBundleStore} from '../../apps/game-api/src/equipment-reward-bundles';
import {equipmentInventoryStartStatements} from '../../apps/game-api/src/equipment-inventory-start';
async function rewardHoldFixture(){
 const f=await bRouteFixture('NATIVE',admitted,{equipmentInventoryCapacity:{version:'equipment.inventory-capacity.v18',do05Rank:0,slots:30}});
 await f.env.DB.batch(equipmentInventoryStartStatements(f.env.DB,'a',f.runId,f.originalSnapshot));
 await f.choose();const report=await new BResultReportStore(f.env.DB).submit('a',f.runId,await compatibleLocalReport(f));await f.arrive();
 const broker=new BOriginalAcceptedBroker(f.env.DB,new BResultConsistencyOwner(f.env.DB));
 const map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,broker));
 const waves=new BOperationProgressStore(f.env.DB,new BRouteWaveVerifier(f.env.DB,broker));
 for(const ordinal of [1,2])await waves.completeWave('a',f.runId,{eventId:'reward-wave'+ordinal,nodeId:'n1-0',waveOrdinal:ordinal,expectedMapRevision:3,evidenceRef:report.evidenceRef});
 // Issuance is explicitly synthetic; completion and resolution use real stores.
 const rewards=new EquipmentRewardBundleStore(f.env.DB,{prepare:async()=>({sourceVersion:'LOCAL_REWARD',items:[1,2].map(i=>({itemId:'reward-'+i,instanceId:'instance-'+i,definitionId:'gear',definitionVersion:'LOCAL',categoryId:'weapon',setTag:'SET01' as const,star:1})),assertions:[f.env.DB.prepare('INSERT INTO equipment_reward_write_guard VALUES(1)')]})},{prepare:async()=>({assertions:[f.env.DB.prepare('INSERT INTO equipment_reward_write_guard VALUES(1)')]})},new EquipmentRewardEffectSource(f.env.DB));
 const bundle=await rewards.publishBase('a',f.runId,'n1-0',report.evidenceRef);
 const complete={kind:'COMPLETE' as const,eventId:'reward-node-complete',nodeId:'n1-0',expectedRevision:3,evidenceRef:report.evidenceRef};
 return {...f,map,rewards,bundle,complete};
}
it('binds an already fixed pending reward to completion and keeps its original identities on replay',async()=>{
 const f=await rewardHoldFixture();try{
 const completed=await f.map.commit('a',f.runId,f.complete);
 expect(completed.state.holdIds).toEqual([f.bundle.bundleId]);
 expect(await f.map.commit('a',f.runId,f.complete)).toEqual(completed);
 expect((await f.rewards.read('a',f.runId,f.bundle.bundleId)).items).toEqual(f.bundle.items);
 }finally{f.close();}
});
it('releases only the resolved reward hold from its original decline receipt and replays after termination',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 const command={kind:'RELEASE_HOLD' as const,eventId:'release-reward',holdId:f.bundle.bundleId,evidenceRef:'decline-original',expectedRevision:4};
 await expect(f.map.commit('a',f.runId,command)).rejects.toThrow();
 await f.rewards.declineRemaining('a',f.runId,{commandId:'decline-original',bundleId:f.bundle.bundleId,itemIds:['reward-1','reward-2'],expectedInventoryRevision:1});
 const released=await f.map.commit('a',f.runId,command);
 expect(released.state.holdIds).toEqual([]);expect(released.verificationId).toBe('decline-original');
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect(await f.map.commit('a',f.runId,command)).toEqual(released);
 expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
 }finally{f.close();}
});
it('keeps partial claims held and preserves another hold when the remaining original reward is declined',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 await f.rewards.claimItems('a',f.runId,{commandId:'claim-first',bundleId:f.bundle.bundleId,itemIds:['reward-1'],expectedInventoryRevision:1});
 const command={kind:'RELEASE_HOLD' as const,eventId:'release-partial',holdId:f.bundle.bundleId,evidenceRef:'claim-first',expectedRevision:4};
 await expect(f.map.commit('a',f.runId,command)).rejects.toThrow('equipment_reward_hold_pending');
 expect((await f.map.read('a',f.runId)).state.holdIds).toEqual([f.bundle.bundleId]);
 await f.rewards.declineRemaining('a',f.runId,{commandId:'decline-rest',bundleId:f.bundle.bundleId,itemIds:['reward-2'],expectedInventoryRevision:2});
 const current=await f.map.read('a',f.runId);
 f.sql.run('UPDATE operation_map_states SET state_json=?',[canonicalJson({...current.state,holdIds:[f.bundle.bundleId,'OTHER_OWNER_HOLD']})]);
 const released=await f.map.commit('a',f.runId,{...command,evidenceRef:'decline-rest'});
 expect(released.state.holdIds).toEqual(['OTHER_OWNER_HOLD']);
 expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
 expect((await f.rewards.read('a',f.runId,f.bundle.bundleId)).items.map(i=>i.status)).toEqual(['CLAIMED','FORFEITED']);
 }finally{f.close();}
});
it('rejects a changed reward observation before completion commits and leaves no completion link',async()=>{
 const f=await rewardHoldFixture();try{
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE run_equipment_reward_items SET status='FORFEITED' WHERE item_id='reward-1'");};
 await expect(f.map.commit('a',f.runId,f.complete)).rejects.toThrow('operation_map_state_conflict');
 expect((await f.map.read('a',f.runId)).state.phase).toBe('ACTIVE');
 expect(f.scalar('SELECT count(*) FROM b_result_node_links')).toBe(0);
 expect(await f.map.lookup('a',f.runId,f.complete.eventId)).toBeNull();
 }finally{f.close();}
});
it('does not accept resolved item flags without the original equipment receipt',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 f.sql.run("UPDATE run_equipment_reward_items SET status='FORFEITED'");
 await expect(f.map.commit('a',f.runId,{kind:'RELEASE_HOLD',eventId:'no-receipt',holdId:f.bundle.bundleId,evidenceRef:'fake-receipt',expectedRevision:4})).rejects.toThrow('equipment_reward_hold_receipt_missing');
 expect((await f.map.read('a',f.runId)).revision).toBe(4);
 }finally{f.close();}
});
it('rolls back a reward release on account revocation during commit and recovers lost acknowledgements',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 await f.rewards.declineRemaining('a',f.runId,{commandId:'decline-all',bundleId:f.bundle.bundleId,itemIds:['reward-1','reward-2'],expectedInventoryRevision:1});
 const command={kind:'RELEASE_HOLD' as const,eventId:'release-race',holdId:f.bundle.bundleId,evidenceRef:'decline-all',expectedRevision:4};
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");};
 await expect(f.map.commit('a',f.runId,command)).rejects.toThrow('operation_map_state_conflict');
 expect((await f.map.read('a',f.runId)).revision).toBe(4);expect(await f.map.lookup('a',f.runId,command.eventId)).toBeNull();
 f.sql.run("UPDATE accounts SET status='ACTIVE' WHERE id='a'");
 const batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const result=await batch(statements);if(lose){lose=false;throw Error('lost acknowledgement');}return result;};
 const released=await f.map.commit('a',f.runId,command);expect(released.state.holdIds).toEqual([]);
 expect(await f.map.commit('a',f.runId,command)).toEqual(released);
 }finally{f.close();}
});
import {EquipmentRewardMapHolds} from '../../apps/game-api/src/equipment-reward-map-holds';
it('accepts a settled held reward in the final-node phase at the reward verifier boundary',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 await f.rewards.declineRemaining('a',f.runId,{commandId:'final-decline',bundleId:f.bundle.bundleId,itemIds:['reward-1','reward-2'],expectedInventoryRevision:1});
 const current=await f.map.read('a',f.runId);
 // Component phase regression only, not a simulated 12-node completion or boss drop issuance.
 const prepared=await new EquipmentRewardMapHolds(f.env.DB).release({accountId:'a',runId:f.runId,startSnapshot:f.originalSnapshot,plan:f.p,state:{...current.state,phase:'FINAL_NODE_COMPLETE'},command:{kind:'RELEASE_HOLD',eventId:'final-release',holdId:f.bundle.bundleId,evidenceRef:'final-decline',expectedRevision:4}},'n1-0');
 expect(prepared?.proof.verificationId).toBe('final-decline');
 }finally{f.close();}
});
it('uses the mounted authenticated transition for fixed reward release without accepting a client settlement flag',async()=>{
 const f=await rewardHoldFixture();try{
 await f.map.commit('a',f.runId,f.complete);
 await f.rewards.declineRemaining('a',f.runId,{commandId:'http-decline',bundleId:f.bundle.bundleId,itemIds:['reward-1','reward-2'],expectedInventoryRevision:1});
 const command={kind:'RELEASE_HOLD',eventId:'http-release',holdId:f.bundle.bundleId,evidenceRef:'http-decline',expectedRevision:4};
 const path='/v1/runs/'+f.runId+'/b-route/transition',version='operation.b-route.v1.20261005';
 const headers={...f.headers,'X-B-Route-Version':version,'Idempotency-Key':command.eventId};
 expect((await app.request(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({version,command})},f.env)).status).toBe(401);
 expect((await app.request(path,{method:'POST',headers,body:JSON.stringify({version,command:{...command,settled:true}})},f.env)).status).toBe(400);
 const result=await app.request(path,{method:'POST',headers,body:JSON.stringify({version,command})},f.env);
 expect(result.status).toBe(200);expect(result.headers.get('Cache-Control')).toBe('no-store');
 const response=await result.json();expect(response).toMatchObject({version,committed:true,receipt:{verificationId:'http-decline',state:{holdIds:[]}}});
 expect(await(await app.request(path,{method:'POST',headers,body:JSON.stringify({version,command})},f.env)).json()).toEqual(response);
 }finally{f.close();}
});
