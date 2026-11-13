import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {BScoutGunnerSource} from './combat-b-scout-gunner-source';
import {BBoardingEventSourceOwner,bBoardingEventDefinition,readPinnedBBoardingEventDefinition} from './combat-b-boarding-events';
import {bInterceptAbilityDefinition,readPinnedBInterceptAbilityDefinition,bInterceptAbilitySourceVersion} from './combat-b-intercept-ability';
import {readOriginalBRouteSource,readBRouteBinding} from './combat-b-route-adapter';
import type {BDepartureCombatSourceOwner,BDepartureCombatContext} from './combat-b-departure-combat-pin';
export const bNativeNodeVersion='combat.b-native-node-source.v1.20261005';
export const bNativeDepartureVersion='combat.b-native-node-origin.v1.20261005';
const hash=z.string().regex(/^[a-f0-9]{64}$/u);
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),digest=(v:unknown)=>sha256Hex(json(v));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_native_'+code);};
const originSchema=z.strictObject({version:z.literal(bNativeDepartureVersion),seed:hash,seedHash:hash,boardingDefinition:z.json(),interceptDefinition:z.json(),originHash:hash});
/** Called only while preparing a NEW original departure. Never upgrades an old snapshot. */
export async function prepareBNativeNodeOrigin(context:BDepartureCombatContext,nativeContextId:string,seed:string,plan:unknown){
 await BScoutGunnerSource.create({origin:context,nativeContextId,seed,plan});
 const body={version:bNativeDepartureVersion,seed,seedHash:await sha256Hex(seed),boardingDefinition:await bBoardingEventDefinition(),interceptDefinition:await bInterceptAbilityDefinition()};
 return originSchema.parse({...body,originHash:await digest(body)});
}
/** Trusted parent decides actual runtime support; adding metadata never grants support. */
export function createBNativeNodeSourceOwner(parent:BDepartureCombatSourceOwner,originalSeed:{prepare(context:Readonly<BDepartureCombatContext>):Promise<{seed:string;assertions:readonly D1PreparedStatement[]}>},plan:unknown):BDepartureCombatSourceOwner{
 return {prepare:async context=>{const p=await parent.prepare(structuredClone(context));const source=z.object({contextId:z.string(),profile:z.object({definition:z.record(z.string(),z.json())}).passthrough()}).passthrough().parse(p.source);
 if(Object.hasOwn(source.profile.definition,'bNativeNodeOrigin'))return fail('already_pinned');
 const s=await originalSeed.prepare(structuredClone(context)),guards=[...p.assertions,...s.assertions];if(!p.assertions.length||!s.assertions.length||guards.length>128||guards.some(g=>typeof g.bind!=='function'))return fail('source_fence_missing');
 const frozen=await prepareBNativeNodeOrigin(context,source.contextId,s.seed,plan);
 return {source:{...source,profile:{...source.profile,definition:{...source.profile.definition,bNativeNodeOrigin:frozen}}},assertions:guards};
 },supports:(source,context)=>parent.supports(source,context)};
}
/** Source generation is pure from the original SQL departure and actual traversed path.
 * No node parameter, mutable catalog, accepted event, runtime speed, or DB writes. */
export async function readBNativeNodeSource(db:D1Database,accountId:string,runId:string){
 const x=await readOriginalBRouteSource(db,accountId,runId);if(!x.pin)return fail('native_origin_unavailable');
 const raw=x.pin.source.profile.definition.bNativeNodeOrigin;if(raw===undefined)return fail('native_origin_unavailable');
 const p=originSchema.parse(raw),{originHash,...originBody}=p;if(originHash!==await digest(originBody)||p.seedHash!==await sha256Hex(p.seed))return fail('origin_conflict');
 const bd=z.object({definitionHash:hash,originalDefinition:z.json()}).parse(p.boardingDefinition),intercept=z.object({definitionHash:hash}).parse(p.interceptDefinition);
 const boardingDefinition=await readPinnedBBoardingEventDefinition(p.boardingDefinition,bd.definitionHash),interceptDefinition=await readPinnedBInterceptAbilityDefinition(p.interceptDefinition,intercept.definitionHash);
 if(json(interceptDefinition.originalDefinition)!==json(boardingDefinition))return fail('definition_conflict');
 const generator=await BScoutGunnerSource.fromFrozenDeparture({origin:x.pin.origin,nativeContextId:x.pin.source.contextId,seed:p.seed,plan:x.plan},boardingDefinition.originalDefinition,x.pin.riskDefinition);
 const ids=x.state.traversedEdgeIds.map(edgeId=>{const e=x.plan.edges.find(e=>e.edgeId===edgeId);if(!e)return fail('path_conflict');return e.toNodeId;});
 const pending=x.state.pendingEdgeId?x.plan.edges.find(e=>e.edgeId===x.state.pendingEdgeId):null;if(x.state.pendingEdgeId&&!pending)return fail('path_conflict');const nodeId=pending?.toNodeId??x.view.currentNodeId;
 const node=x.plan.nodes.find(n=>n.nodeId===nodeId);if(!node||node.eventKind!=='COMBAT'||node.visitDepth<1||node.visitDepth>11)return fail('node_unsupported');
 if(pending)ids.push(nodeId);
 let previous:Awaited<ReturnType<BScoutGunnerSource['generateNode']>>|undefined,selected:typeof previous;
 for(const id of ids){const n=x.plan.nodes.find(n=>n.nodeId===id)!;if(n.eventKind!=='COMBAT')continue;const source=await generator.generateNode(id,previous?{source:previous,expectedHash:previous.sourceHash}:undefined);if(id===nodeId)selected=source;previous=source;}
 if(!selected)return fail('path_conflict');
 const boardingSource=await new BBoardingEventSourceOwner(generator).pinFrozen(selected,selected.sourceHash,boardingDefinition);
 const {packetHash:oldRouteHash,...originalRoute}=await readBRouteBinding(db,accountId,runId);void oldRouteHash;
 const routeBody={...originalRoute,nativeNodeSource:selected,nodeSourceStatus:'ORIGINAL_PINNED_NODE_SOURCE'};
 const routeBinding={...routeBody,packetHash:await digest(routeBody)};
 const interceptBody={version:bInterceptAbilitySourceVersion,definition:interceptDefinition,definitionAvailable:true,runtimeSupported:false,canStart:false};
 const interceptDefinitionPacket={...interceptBody,packetHash:await digest(interceptBody)};
 const body={version:bNativeNodeVersion,accountId,runId,nodeId,mapRevision:x.row.revision,phase:x.state.phase,pendingEdgeId:x.state.pendingEdgeId,holdIds:[...x.state.holdIds],startSnapshotHash:await sha256Hex(x.row.snapshot_json),departurePinHash:x.pin.pinHash,nativeOriginHash:p.originHash,nativeContextId:x.pin.source.contextId,originalNativeOrigin:structuredClone(x.pin.origin),seedHash:p.seedHash,originalNodeSource:selected,boardingSource,interceptDefinition,interceptDefinitionPacket,routeBinding,interceptionAcceptedTiming:null,definitionAvailable:true,runtimeSupported:false,canStart:false,acceptedRouteEvidenceSupported:false,requiredConsumerOwners:['ORIGINAL_WAVE_ATTACK_INPUT_AND_OWNED_PROFILE','ORIGINAL_CARRIAGE_BUFF_AND_BASIC_LOOP_BINDING','ORIGINAL_ROOF_PATH_SOCKET_SPATIAL_SOURCE','EXPLICIT_TRAVEL_CONFIGURATION','ORIGINAL_ACCEPTED_INTERCEPTION_HASTE_AND_ELIGIBILITY']};
 const packet={...body,packetHash:await digest(body)};
 const latest=await readOriginalBRouteSource(db,accountId,runId);if(json(latest.row)!==json(x.row))return fail('source_stale');
 return packet;
}
