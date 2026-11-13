import {bRouteFixture} from './combat-b-route-fixture';
import {readBNativeNodeSource} from '../../apps/game-api/src/combat-b-native-node-source';
import {BOriginalAcceptedBroker,bOriginalObservationVersion,type BOriginalVerifiedObservationOwner} from '../../apps/game-api/src/combat-b-original-accepted-broker';
import {BRouteMapVerifier,BRouteWaveVerifier} from '../../apps/game-api/src/combat-b-route-adapter';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {BOperationProgressStore} from '../../apps/game-api/src/combat-b-operation-progress';
import {RunSafeMaintenanceStore} from '../../apps/game-api/src/equipment-safe-maintenance';
/** Explicit LOCAL trusted-owner SQL fixture; NOT an operating verification implementation. */
export async function brokerFixture(){
 const f=await bRouteFixture('NATIVE');await f.choose();f.sql.run('CREATE TABLE local_verified_native_source(id INTEGER PRIMARY KEY CHECK(id=1),record_json TEXT NOT NULL)');
 const source:BOriginalVerifiedObservationOwner={prepare:async c=>{const row=await f.env.DB.prepare('SELECT record_json FROM local_verified_native_source WHERE id=1').first<{record_json:string}>();if(!row)return {status:'UNSUPPORTED',missingSources:['LOCAL_SQL_SOURCE_ABSENT']};const table=c.kind==='SAFE_BETWEEN_NODES'?'equipment_safe_write_guard':'operation_map_write_guard';return {status:'VERIFIED',observation:JSON.parse(row.record_json),assertions:[f.env.DB.prepare(`INSERT INTO ${table}(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_verified_native_source WHERE id=1 AND record_json=?) THEN 1 ELSE 0 END`).bind(row.record_json)]};}};
 const broker=new BOriginalAcceptedBroker(f.env.DB,source),map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,broker)),progress=new BOperationProgressStore(f.env.DB,new BRouteWaveVerifier(f.env.DB,broker)),safe=new RunSafeMaintenanceStore(f.env.DB,broker);
 const hazards=['WAVE_ENTRY','ENEMY_ACTION','ENEMY_ADMISSION','GUNNER_PROJECTILE','INTERCEPTION'];
 async function observe(revision:number,fields:Record<string,unknown>={}){
 const p=await readBNativeNodeSource(f.env.DB,'a',f.runId);
 const o={version:bOriginalObservationVersion,accountId:'a',runId:f.runId,nodeId:'n1-0',mapRevision:revision,authorityMode:'ORDINARY_SOLO',sourceVerification:'ORIGINAL_OWNER_VERIFIED',verificationOwner:'SERVER',nativeContextId:p.nativeContextId,departurePinHash:p.departurePinHash,nodeSourceHash:p.originalNodeSource.sourceHash,operationBindingHash:p.routeBinding.bindingHash,nativeBindingHash:'sha256:'+'4'.repeat(64),originalEventId:'LOCAL_ORIGINAL_CHECKPOINT_EVENT',checkpointHash:'sha256:'+'5'.repeat(64),sourceRevision:507,sequence:1063,evidenceRef:'LOCAL_VERIFIED_REF',registeredHazardOwners:hazards,hazards:hazards.map(ownerId=>({ownerId,pending:false})),unknownHazards:[],combatStopped:true,travelStopped:true,arrival:{eventId:'LOCAL_ACCEPTED_ARRIVAL',sequence:1},waves:[{ordinal:1,eventId:'LOCAL_ACCEPTED_WAVE1',sequence:622,hazardsSettled:true},{ordinal:2,eventId:'LOCAL_ACCEPTED_WAVE2',sequence:1048,hazardsSettled:true}],completion:{eventId:'LOCAL_ACCEPTED_COMPLETE',sequence:1060},holdIds:[],holdSettlements:[],...fields};
 f.sql.run('INSERT INTO local_verified_native_source VALUES(1,?) ON CONFLICT(id) DO UPDATE SET record_json=excluded.record_json',[JSON.stringify(o)]);return o;
 }
 const arrival={kind:'ARRIVE' as const,eventId:'broker-arrive',expectedRevision:2,edgeId:'entry:n1-0',evidenceRef:'LOCAL_VERIFIED_REF'};
 async function arrive(){await observe(2);return map.commit('a',f.runId,arrival);}
 async function wave(ordinal:number){await observe(3);return progress.completeWave('a',f.runId,{eventId:'broker-wave'+ordinal,nodeId:'n1-0',waveOrdinal:ordinal,expectedMapRevision:3,evidenceRef:'LOCAL_VERIFIED_REF'});}
 return {...f,broker,map,progress,safe,observe,arrive,wave,arrival,source};
}
