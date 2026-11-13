import {canonicalJson} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {OperationMapStore} from './operation-map-store';
import {captureOperationMap} from './operation-map-plan';
import {readDepartureMap} from './operation-map-departure';
import {verifyPinnedOperationMapContent} from './operation-map-publisher';
import {mapReceipt} from './operation-map-state';
import {AugmentNodeIssuer,type AugmentEligibleCatalogSource} from './augment-node-issuer';
/** Called after original ARRIVE commits, also on its retry. Arrival and issuance
 * have distinct existing owners. Failed issuance never pretends arrival failed;
 * the original arrival request can recover the one node grant without a GET. */
export async function issueAugmentForArrival(db:D1Database,accountId:string,runId:string,environment:string,input:unknown,catalog:AugmentEligibleCatalogSource){
 const receipt=mapReceipt.parse(input),original=await new OperationMapStore(db).lookup(accountId,runId,receipt.eventId);
 if(!original||canonicalJson(z.json().parse(original))!==canonicalJson(z.json().parse(receipt))||receipt.state.phase!=='ACTIVE'||receipt.state.holdIds.length)throw new ActivityError(409,'augment_arrival_receipt_invalid');
 const row=await db.prepare('SELECT plan_json,start_snapshot_json FROM operation_map_states WHERE run_id=?').bind(runId).first<{plan_json:string;start_snapshot_json:string}>();
 if(!row)throw new ActivityError(409,'augment_arrival_map_missing');
 const plan=captureOperationMap(JSON.parse(row.plan_json)),snapshot=JSON.parse(row.start_snapshot_json);
 if(canonicalJson(readDepartureMap(snapshot))!==canonicalJson(plan))throw new ActivityError(409,'augment_arrival_map_conflict');
 await verifyPinnedOperationMapContent(snapshot,plan);
 const edge=plan.edges.find(e=>e.edgeId===receipt.state.traversedEdgeIds.at(-1)),node=plan.nodes.find(n=>n.nodeId===edge?.toNodeId);
 if(!node)throw new ActivityError(409,'augment_arrival_node_missing');
 if(node.eventKind!=='AUGMENT')return undefined;
 return new AugmentNodeIssuer(db,catalog).issue({accountId,runId,environment,stageId:node.nodeId});
}
