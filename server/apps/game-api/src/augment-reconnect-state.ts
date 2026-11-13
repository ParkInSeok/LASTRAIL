import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {SoloAugmentSelection,soloSelectionIssueSchema,soloSelectionResultSchema,type SelectionSnapshot} from './solo-augment-selection';
import {readO22ApplicationOriginal} from './augment-lifesteal-original';
import {captureOperationMap,projectOperationMap} from './operation-map-plan';
import {readMapProgress} from './operation-map-state';
import {readDepartureMap} from './operation-map-departure';
import {verifyPinnedOperationMapContent} from './operation-map-publisher';
import {readCombatAuthority} from './combat-authority';

/** Authenticated lifecycle query supplies ONE consistent SQL snapshot and only
 * the grant at the current map node. Never accept this structure from a client.
 * Receipt is the selected command's original key, not the latest reroll.
 * Local pending commands, APPLIED/checkpoint and pause ownership are not in D1. */
export type AugmentReconnectInput={
 accountId:string;environment:string;runId:string;runStatus:string;startSnapshot:string|null;
 map:{revision:number;planJson:string;stateJson:string;startSnapshot:string}|null;
 grant:{accountId:string;environment:string;runId:string;eventId:string;stageId:string;
  initialJson:string;issueHash:string;checkpointJson:string;startSnapshot:string;writeRevision:number}|null;
 receipt:{requestId:string;requestHash:string;responseJson:string}|null;
 applicationOriginal:{originalJson:string;originalHash:string}|null;
};
type State='UNKNOWN'|'NO_CURRENT_GRANT'|'SELECTING'|'CONFIRMED_RUNTIME_CHECKPOINT_REQUIRED'|'NODE_EXITED'|'TERMINAL'|'REVIEW_REQUIRED';
type SavedReceipt=z.infer<typeof soloSelectionResultSchema>&{applicationOriginal?:unknown};
const receiptSchema=soloSelectionResultSchema.extend({applicationOriginal:z.unknown().optional()});
const id=z.string().min(1).max(200),revision=z.number().int().positive().safe();
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const check=(condition:boolean)=>{if(!condition)throw Error('reconnect_original_conflict');};
const readOnly={extraApproval:{authorize:async()=>{throw Error('read_only');}}};
type MapSummary={phase:ReturnType<typeof readMapProgress>['phase'];currentNodeId:string;holdIds:string[]};
function result(state:State,selection:SelectionSnapshot|null=null,selectionReceipt:SavedReceipt|null=null,map:MapSummary|null=null,reason:string|null=null){
 return {version:'combat.augment-reconnect-state.v1.20261020' as const,state,reason,selection,selectionReceipt,map,
  localCheckpointStatus:'NOT_OBSERVED' as const,pendingOriginalRequestMustBeRecovered:true as const,
  releaseSelectionHold:false as const,runtimeApplied:null,newCommandAuthorized:false as const};
}
/** Advisory server facts, not a UI or combat resumption command. The caller must
 * preserve terminal/reward routing. An unresolved LOCAL original request takes
 * priority over SELECTING/absence until recovered using that same request key.
 * No new grants, healing, costs, holds, migrations, or current-catalog lookups. */
async function project(input:AugmentReconnectInput){
 try{
  id.parse(input.accountId);id.parse(input.environment);id.parse(input.runId);
  if(['COMPLETED','FAILED','ABANDONED','FINISH_PENDING'].includes(input.runStatus))return result('TERMINAL');
  if(input.runStatus!=='ACTIVE')return result('REVIEW_REQUIRED');
  if(!input.startSnapshot)return result('UNKNOWN',null,null,null,'START_UNAVAILABLE');
  if(input.startSnapshot.length>2*1024*1024)return result('REVIEW_REQUIRED');
  const snapshot=JSON.parse(input.startSnapshot);
  const authority=readCombatAuthority(snapshot?.admission?.combatAuthority);
  if(authority.status==='INVALID')return result('REVIEW_REQUIRED',null,null,null,'AUTHORITY_INVALID');
  if(!authority.contract)return result('UNKNOWN',null,null,null,'AUTHORITY_NOT_PINNED');
  if(authority.contract.mode!=='ORDINARY_SOLO')return result('UNKNOWN',null,null,null,'MODE_NOT_APPLICABLE');
  if(!input.map)return result('UNKNOWN',null,null,null,'MAP_UNAVAILABLE');
  const m=input.map;
  for(const text of [input.startSnapshot,m.planJson,m.stateJson,input.grant?.initialJson,input.grant?.checkpointJson,input.receipt?.responseJson,input.applicationOriginal?.originalJson])
   if(text!==undefined&&text.length>2*1024*1024)return result('REVIEW_REQUIRED');
  revision.parse(m.revision);
  check(m.startSnapshot===input.startSnapshot);
  const plan=captureOperationMap(JSON.parse(m.planJson));
  check(json(readDepartureMap(snapshot))===json(plan));
  await verifyPinnedOperationMapContent(snapshot,plan);
  const state=readMapProgress(plan,JSON.parse(m.stateJson));
  const nodeId=projectOperationMap(plan,state.traversedEdgeIds).currentNodeId,g=input.grant;
  const map:MapSummary={phase:state.phase,currentNodeId:nodeId,holdIds:[...state.holdIds]};
  if(state.phase!=='ACTIVE')return result('NODE_EXITED',null,null,map);
  if(!g){check(input.receipt===null&&input.applicationOriginal===null);return result('NO_CURRENT_GRANT',null,null,map);}
  check(plan.nodes.find(n=>n.nodeId===nodeId)?.eventKind==='AUGMENT');
  check(g.accountId===input.accountId&&g.environment===input.environment&&g.runId===input.runId&&g.stageId===nodeId&&g.startSnapshot===input.startSnapshot);
  revision.parse(g.writeRevision);id.parse(g.eventId);
  const initial=SoloAugmentSelection.restore(g.initialJson,readOnly).read();
  const selection=SoloAugmentSelection.restore(g.checkpointJson,readOnly).read();
  const initialIssue=soloSelectionIssueSchema.parse(JSON.parse(g.initialJson).issue);
  const checkpoint=JSON.parse(g.checkpointJson),issue=soloSelectionIssueSchema.parse(checkpoint.issue);
  check(json(initialIssue)===json(issue)&&initial.state==='SELECTING'&&initial.grantRevision===0);
  check(issue.accountId===input.accountId&&issue.selectionOwner===input.accountId&&issue.environment===input.environment&&issue.runId===input.runId&&issue.eventId===g.eventId&&issue.stageId===nodeId);
  check(await sha256Hex(json(issue))===g.issueHash);
  if(selection.state==='SELECTING'){
   check(input.receipt===null&&input.applicationOriginal===null);
   return result('SELECTING',selection,null,map);
  }
  const receipt=input.receipt;check(receipt!==null);
  if(!receipt)return result('REVIEW_REQUIRED');
  const saved=receiptSchema.parse(JSON.parse(receipt.responseJson));
  check(saved.outcome==='SELECTED'&&json(saved.snapshot)===json(selection));
  const entries=z.array(z.object({key:id,body:z.string(),result:soloSelectionResultSchema})).parse(checkpoint.receipts);
  const selected=entries.filter(e=>e.result.outcome==='SELECTED');
  check(selected.length===1);
  const entry=selected[0]!;
  check(entry.key===receipt.requestId&&json(entry.result)===json({outcome:saved.outcome,snapshot:saved.snapshot,approval:saved.approval}));
  const action=JSON.parse(entry.body);
  check(action.operation==='SELECT');
  check(await sha256Hex(json({actor:{accountId:input.accountId,environment:input.environment},action}))===receipt.requestHash);
  if(selection.selectedCandidateId==='O22'){
   const original=input.applicationOriginal;check(original!==null&&saved.applicationOriginal!==undefined);
   if(!original)return result('REVIEW_REQUIRED');
   check(await sha256Hex(original.originalJson)===original.originalHash);
   check(json(JSON.parse(original.originalJson))===json(saved.applicationOriginal));
   const checked=await readO22ApplicationOriginal(saved.applicationOriginal,{accountId:input.accountId,environment:input.environment,runId:input.runId,eventId:g.eventId},selection);
   check(checked.definition.startSnapshotHash===await sha256Hex(input.startSnapshot)
    &&checked.definition.selectionSource.checkpointHash===await sha256Hex(g.checkpointJson)
    &&checked.definition.selectionSource.storeRevision===g.writeRevision);
  }else check(input.applicationOriginal===null&&saved.applicationOriginal===undefined);
  return result('CONFIRMED_RUNTIME_CHECKPOINT_REQUIRED',selection,saved,map);
 }catch{
  // A malformed original cannot silently route to combat or a fresh selection.
  return result('REVIEW_REQUIRED');
 }
}
export async function projectAugmentReconnectState(input:AugmentReconnectInput){
 const projection=await project(input);
 return {...projection,accountId:input.accountId,environment:input.environment,runId:input.runId,
  startSnapshotHash:input.startSnapshot&&input.startSnapshot.length<=2*1024*1024?await sha256Hex(input.startSnapshot):null,
  mapRevision:projection.map?input.map!.revision:null,storeRevision:projection.selection?input.grant!.writeRevision:null};
}
