import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureOperationMap,projectOperationMap} from './operation-map-plan';
import {readMapProgress} from './operation-map-state';
import {verifyPinnedOperationMapContent} from './operation-map-publisher';
import {readDepartureMap} from './operation-map-departure';
import type {AugmentSelectionGuard} from './solo-augment-store';
import {captureOwnedAugmentSelections} from './augment-owned-selection-source';
import {augmentUtilityDefinitionVersion,captureAugmentUtilityDefinitions} from './augment-utility-definition';
/** Uses the persisted authoritative map owner. Does not create an arrival,
 * issue a candidate, complete a node or accept client progress flags. */
export class AugmentNodeSelectionGuard implements AugmentSelectionGuard {
 constructor(private readonly db:D1Database){}
 async prepare(c:Parameters<AugmentSelectionGuard['prepare']>[0]){
  const prepared=await prepareAugmentNodeIssuance(this.db,c);
  if(c.catalogVersion===augmentUtilityDefinitionVersion){
   const catalog=await captureAugmentUtilityDefinitions();
   if(catalog.eligibility.singleOwnedPerRun.includes(c.candidateId)&&prepared.owned.some(item=>item.candidateId===c.candidateId))throw new ActivityError(409,'augment_single_owned_conflict');
   const other=catalog.eligibility.exclusivePair.filter(id=>id!==c.candidateId);
   if(catalog.eligibility.exclusivePair.includes(c.candidateId)&&prepared.owned.some(item=>other.includes(item.candidateId)))throw new ActivityError(409,'augment_exclusive_conflict');
  }
  return prepared.assertions;
 }
}
/** Shared original map/acquisition proof for issuance and selection. */
export async function prepareAugmentNodeIssuance(db:D1Database,c:{accountId:string;runId:string;stageId:string;startSnapshot:string;environment:string}){
  const fail=():never=>{throw new ActivityError(409,'augment_node_selection_unavailable');};
  const row=await db.prepare('SELECT revision,plan_json,state_json,start_snapshot_json FROM operation_map_states WHERE run_id=?').bind(c.runId)
   .first<{revision:number;plan_json:string;state_json:string;start_snapshot_json:string}>();
  if(!row||row.start_snapshot_json!==c.startSnapshot)return fail();
  const snapshot=JSON.parse(c.startSnapshot),plan=captureOperationMap(JSON.parse(row.plan_json));
  if(canonicalJson(readDepartureMap(snapshot))!==canonicalJson(plan))return fail();
  await verifyPinnedOperationMapContent(snapshot,plan);
  const state=readMapProgress(plan,JSON.parse(row.state_json)),node=projectOperationMap(plan,state.traversedEdgeIds).currentNodeId;
  if(state.phase!=='ACTIVE'||state.holdIds.length||node!==c.stageId||plan.nodes.find(n=>n.nodeId===node)?.eventKind!=='AUGMENT')return fail();
  const ownership=await captureOwnedAugmentSelections(db,c);
  return {revision:row.revision,owned:ownership.owned,assertions:[...ownership.assertions,db.prepare(`INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM operation_map_states m JOIN runs r ON r.id=m.run_id JOIN accounts a ON a.id=r.account_id
   WHERE m.run_id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
   AND m.revision=? AND m.plan_json=? AND m.state_json=? AND m.start_snapshot_json=?
   AND NOT EXISTS(SELECT 1 FROM combat_result_reviews review WHERE review.run_id=m.run_id)) THEN 1 ELSE 0 END`)
   .bind(c.runId,c.accountId,row.revision,row.plan_json,row.state_json,row.start_snapshot_json)]};
}
