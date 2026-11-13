import {readFileSync,writeFileSync} from 'node:fs';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {bRouteFixture} from '../../tests/helpers/combat-b-route-fixture';
import {BResultReportStore,bResultReportVersion} from '../../apps/game-api/src/combat-b-result-report';
import {BResultConsistencyOwner} from '../../apps/game-api/src/combat-b-result-consistency-owner';
import {BOriginalAcceptedBroker} from '../../apps/game-api/src/combat-b-original-accepted-broker';
import {readBNativeNodeSource} from '../../apps/game-api/src/combat-b-native-node-source';
import {BRouteMapVerifier,BRouteWaveVerifier} from '../../apps/game-api/src/combat-b-route-adapter';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {BOperationProgressStore} from '../../apps/game-api/src/combat-b-operation-progress';
import {RunSafeMaintenanceStore} from '../../apps/game-api/src/equipment-safe-maintenance';
const qa=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json','utf8'));
const admission={formation:{operators:[{slot:0,operatorId:'op_kyle'}]},loadout:{trainId:'train',carriages:[{slot:0,carriageId:'car0'}]},ownedState:{train:{trainId:'train'},operators:[{operatorId:'op_kyle'}]}};
const f=await bRouteFixture('NATIVE',admission);
try{
 await f.choose();const p=await readBNativeNodeSource(f.env.DB,'a',f.runId),n=structuredClone(qa.runtimeSources.nodeSession),r=n.runtime,instances=p.originalNodeSource.waves.flatMap(w=>w.instances);n.node=structuredClone(p.originalNodeSource);r.waveSourceHash=p.originalNodeSource.sourceHash;r.travel.nodeSourceHash=p.originalNodeSource.sourceHash;r.waveEntry.cursor=p.originalNodeSource.waves[1]!.instances.length;
 r.state.direct.enemies=Object.fromEntries(instances.map(i=>[i.instanceId,{hp:0,maxHp:Number(i.hp)}]));r.boarding.lives=instances.map(i=>({id:i.instanceId,phase:'Dead',registered:false,targetable:false,attackEligible:false}));for(const c of r.commands)if(c.kind==='WAVE_BASIC')c.targetId=instances[0]!.instanceId;
 const report={version:bResultReportVersion,requestId:'LOCAL_SAFE_REPORT',reportOriginMapRevision:2,checkpointHash:'sha256:'+await sha256Hex(canonicalJson(r)),nodeSession:n};
 const receipt=await new BResultReportStore(f.env.DB).submit('a',f.runId,report);if(!receipt.progressAuthorized)throw new Error('local_report_blocked');
 const broker=new BOriginalAcceptedBroker(f.env.DB,new BResultConsistencyOwner(f.env.DB)),map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,broker)),progress=new BOperationProgressStore(f.env.DB,new BRouteWaveVerifier(f.env.DB,broker)),safe=new RunSafeMaintenanceStore(f.env.DB,broker);
 // Separate LOCAL original arrival fixture; no VICTORY -> ARRIVE conversion.
 await f.arrive();for(const ordinal of [1,2])await progress.completeWave('a',f.runId,{eventId:'LOCAL_SAFE_WAVE'+ordinal,nodeId:p.nodeId,waveOrdinal:ordinal,expectedMapRevision:3,evidenceRef:receipt.evidenceRef});
 const complete=await map.commit('a',f.runId,{kind:'COMPLETE',eventId:'LOCAL_SAFE_COMPLETE',nodeId:p.nodeId,expectedRevision:3,evidenceRef:receipt.evidenceRef});
 const entered=await safe.commit('a',f.runId,{kind:'ENTER',eventId:'LOCAL_SAFE_ENTER',nodeId:p.nodeId,expectedMapRevision:4,expectedRevision:0,reason:'BETWEEN_NODES'}),prepared=await safe.prepare('a',f.runId);if(entered.mode!=='SAFE'||prepared.assertions.length<2||f.scalar('SELECT snapshot_json FROM run_snapshots')!==f.originalSnapshot)throw new Error('safe_link_failed');
 writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-safe-link-local-20261006.json',JSON.stringify({provenance:'PRIVATE_LOCAL_COMPATIBLE_ADMISSION_ADJUSTED_REPORT_AND_SEPARATE_ARRIVAL_NOT_ACTUAL_UNITY_ACCEPTANCE',unityRestoreNotVerified:true,receipt,complete,entered,preparedAssertionCount:prepared.assertions.length,originalSnapshotPreserved:true,operatingStart:false,operatingDeploy:false,rewardsGranted:false},null,2));
 console.log('LOCAL original report -> two waves -> node completion link -> original safe ENTER/prepare PASS.');
}finally{f.close();}