import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readBDepartureAssignmentForRun} from './combat-b-departure-assignment';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {readBNativeNodeSource} from './combat-b-native-node-source';
export const bDepartureSourceBundleVersion='combat.b-departure-source-bundle.v1.20261006';
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_departure_bundle_'+code);};
/** Supplies original SAME-run immutable bytes, not runtime approval/consumption.
 * Reuses existing B preparation/pin/native provider; no reconstruction from hashes,
 * sample rebind, mutable catalog read, new simulation or START support registry. */
export async function readBDepartureSourceForRun(db:D1Database,accountId:string,runId:string){
 const x=await readOriginalBRouteSource(db,accountId,runId);if(!x.pin)return fail('source_unavailable');
 const assignment=await readBDepartureAssignmentForRun(db,accountId,runId),pin=x.pin,startSnapshotHash=await sha256Hex(x.row.snapshot_json);
 if(assignment.startSnapshotHash!==startSnapshotHash||assignment.departurePinHash!==pin.pinHash||assignment.runtimeProfileHash!==pin.runtimeProfileHash||json(assignment.origin)!==json(pin.origin))return fail('source_stale');
 const missingSources=['ACTUAL_B_ASSIGNMENT_EXECUTOR_BINDING','APPROVED_B_EFFECTIVE_PROFILE','ORIGINAL_B_GEOMETRY_AND_CHASSIS'];
 let nativeNodeSource:Awaited<ReturnType<typeof readBNativeNodeSource>>|null=null;
 if(pin.source.profile.definition.bNativeNodeOrigin===undefined)missingSources.push('ORIGINAL_NATIVE_NODE_ORIGIN');
 else{try{nativeNodeSource=await readBNativeNodeSource(db,accountId,runId);}catch(e){if(e instanceof ActivityError&&e.code==='combat_b_native_node_unsupported')missingSources.push('SELECTED_COMBAT_NODE');else throw e;}}
 if(nativeNodeSource&&(nativeNodeSource.accountId!==accountId||nativeNodeSource.runId!==runId||nativeNodeSource.departurePinHash!==pin.pinHash||nativeNodeSource.startSnapshotHash!==startSnapshotHash||nativeNodeSource.nativeContextId!==pin.source.contextId||json(nativeNodeSource.originalNativeOrigin)!==json(pin.origin)||json(nativeNodeSource.originalNodeSource.origin)!==json(pin.origin)||nativeNodeSource.originalNodeSource.nativeContextId!==pin.source.contextId))return fail('source_conflict');
 const body={version:bDepartureSourceBundleVersion,accountId,runId,startSnapshotHash,departurePinHash:pin.pinHash,originalOrigin:pin.origin,assignment,originalCombatSource:pin.source,combatSourceHash:pin.sourceHash,runtimeProfileHash:pin.runtimeProfileHash,originalRiskDefinition:pin.riskDefinition,nativeNodeSource,mapRevision:x.row.revision,phase:x.state.phase,pendingEdgeId:x.state.pendingEdgeId,sourceStatus:'ORIGINAL_FROZEN_SOURCE_NOT_RUNTIME_CONSUMPTION' as const,predepartureStatsIncluded:true as const,effectiveCombatStatsIncluded:false as const,runtimeBindingIncluded:false as const,canStart:false as const,missingSources};
 const packet={...body,packetHash:await sha256Hex(json(body))};
 const latest=await readOriginalBRouteSource(db,accountId,runId);if(json(latest.row)!==json(x.row))return fail('source_stale');
 return packet;
}