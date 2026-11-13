import {canonicalJson,sha256Hex} from '@lastrail/common';
import {projectOperationMap,type OperationMapPlan} from './operation-map-plan';
import {ActivityError} from './activity-policy';
import {destinationResponseVersion,destinationResponseSchema,type mapReceipt} from './operation-map-state';
import type {z} from 'zod';
export {destinationResponseVersion};
/** Both HTTP projections reuse the one stored baseline, including original-key recovery. */
export function projectDestinationResponse(receipt:z.infer<typeof mapReceipt>,version?:typeof destinationResponseVersion){
 const initial=receipt.travelInitial?{travelInitial:receipt.travelInitial}:{};
 if(version===destinationResponseVersion)return destinationResponseSchema.parse({...receipt.destination,...initial});
 return {version:'operation.map-destination.v1',requestId:receipt.eventId,revision:receipt.revision,phase:receipt.state.phase,pendingEdgeId:receipt.state.pendingEdgeId,...initial};
}
/** Selected pinned references only. This confirms travel intent, never arrival,
 * combat completion, reward issuance or future random event candidates. The caller
 * persists this detached reply with the original map revision CAS and receipt. */
export async function prepareDestinationResponse(input:{accountId:string;runId:string;eventId:string;revision:number;plan:OperationMapPlan;startSnapshot:string;state:{traversedEdgeIds:string[];pendingEdgeId:string|null;phase:string;holdIds:string[]}}){
 const {plan,state}=input,view=projectOperationMap(plan,state.traversedEdgeIds);
 const edge=plan.edges.find(e=>e.edgeId===state.pendingEdgeId&&e.fromNodeId===view.currentNodeId),node=edge&&plan.nodes.find(n=>n.nodeId===edge.toNodeId);
 if(state.phase!=='TRAVELLING'||state.holdIds.length||!edge||!node)throw new ActivityError(409,'operation_map_destination_source_invalid');
 return destinationResponseSchema.parse({version:destinationResponseVersion,accountId:input.accountId,runId:input.runId,requestId:input.eventId,revision:input.revision,startSnapshotHash:await sha256Hex(input.startSnapshot),planHash:await sha256Hex(canonicalJson(plan)),map:{graphId:plan.graphId,graphRevision:plan.graphRevision,eventAssignmentVersion:plan.eventAssignmentVersion,definitionVersion:plan.definitionVersion},state:{phase:'TRAVELLING',currentNodeId:view.currentNodeId,pendingEdgeId:edge.edgeId},destination:{...edge,node:structuredClone(node)},nextAction:'AWAIT_VERIFIED_ARRIVAL'});
}
