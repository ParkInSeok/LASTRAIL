import {setup} from './departure-fixture';
import {plan} from './operation-map-fixture';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import {z} from 'zod';
/** Explicit LOCAL synthetic START/arrival and O22 eligibility only. No operating admission. */
export async function createAugmentSelectFixture(arrived=true,candidateId='O22',issue=true,catalogVersion=augmentUtilityDefinitionVersion){
 const f=await setup(true);
 try{
 const p=z.object({operationPreviewId:z.string().min(1)}).parse(await(await f.preview()).json());
 const {runId}=z.object({runId:z.string().min(1)}).parse(await(await f.start(p.operationPreviewId)).json());
 // Synthetic pinned map/arrival, exercising the real command route and stores.
 const map=plan();map.enabledEventKinds.push('AUGMENT');map.nodes.find(n=>n.nodeId==='n1-1')!.eventKind='AUGMENT';
 const snapshot=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));snapshot.encounterType='ORDINARY_12_NODE';snapshot.admission.operationMapPlan=map;
 f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(snapshot)]);
 const progress=new OperationMapStore(f.env.DB,{prepare:async c=>({proof:{kind:c.command.kind,verificationId:'synthetic-arrival',holdIds:[]},assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')]})});
 await progress.initialize('a',runId);await progress.commit('a',runId,{eventId:'travel',expectedRevision:1,kind:'CONFIRM',edgeId:'entry:n1-1'});
 if(arrived)await progress.commit('a',runId,{eventId:'arrive',expectedRevision:2,kind:'ARRIVE',edgeId:'entry:n1-1',evidenceRef:'synthetic'});
 const eventId='grant';if(issue)await new SoloAugmentStore(f.env.DB,()=>0).issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId,eventId,stageId:'n1-1',catalogVersion,eligibleIds:[candidateId]});
 const body={version:'combat.augment-select.v1.20261019',command:{eventId,expectedRevision:0,slotIndex:0,candidateId}};
 const url=`/v1/runs/${runId}/augment-grants/${eventId}/select`;
 return {...f,runId,body,url,select:(value:object=body,key='select')=>f.post(url,value,key)};
 }catch(error){f.sql.close();throw error;}
}
