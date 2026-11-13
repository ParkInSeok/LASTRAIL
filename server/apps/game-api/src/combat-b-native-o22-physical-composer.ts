import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bDepartureCombatSourceVersion,type BDepartureCombatContext,type BDepartureCombatSource} from './combat-b-departure-combat-pin';
import {operationRiskVersion} from './combat-b-operation-risk';
import {captureOperationMap} from './operation-map-plan';
import {prepareBNativeNodeOrigin} from './combat-b-native-node-source';
import {prepareBArrivalBoundarySource} from './combat-b-arrival-source';
import {captureRuntimeAugmentCatalog} from './augment-runtime-catalog';
import {bNativeO22ExecutorVersion,createBNativeO22ExecutorAdapter,type BNativeO22OriginalSourceOwner} from './combat-b-native-o22-executor';
import {createPersistedBNativeO22OriginalSourceOwner,type BNativeO22OwnedBase,type BNativeO22PhysicalCompositionSource} from './combat-b-native-o22-original-source';
import type {SelectionPrincipal} from './combat-b-selection-store';
const id=z.string().trim().min(1).max(128),hash=z.string().regex(/^[a-f0-9]{64}$/u),unityHash=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const point=z.tuple([z.number().int().min(-1e9).max(1e9),z.number().int().min(-1e9).max(1e9),z.number().int().min(-1e9).max(1e9)]);
const measurement=z.strictObject({version:z.literal('unity.combat-b.scene-measurement.v1.20261020'),sourceId:id,
 provenance:z.literal('UNITY_SCENE_MEASUREMENT_NOT_EXECUTION_AUTHORITY'),unitContractId:id,millimetersPerUnit:z.number().positive().finite(),
 coordinateSpace:z.literal('BOUND_FRAME_LOCAL'),quantization:z.literal('NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO'),
 bodyCornersMm:z.array(point).length(8),anchors:z.array(z.strictObject({id,positionMm:point})).min(1).max(128),
 geometryHash:unityHash,sourceRevision:z.number().int().positive().safe(),packetHash:unityHash});
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const digest=(value:unknown)=>sha256Hex(json(value));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_physical_'+code);};
export type BPhysicalMeasurementExport={packet:unknown;geometryCanonicalJson:string;packetCanonicalJson:string};
/** Preserve original Newtonsoft canonical bytes (not JS's rewritten float
 * representation). Their hashes AND parsed contents must match the packet.
 * These hashes detect substitution; only the independent authored owner below
 * supplies authority, scene references and component-lifetime identity. */
export async function readBPhysicalSceneMeasurement(input:BPhysicalMeasurementExport){
 if(input.geometryCanonicalJson.length>131072||input.packetCanonicalJson.length>131072)return fail('measurement_too_large');
 const p=measurement.parse(structuredClone(input.packet));
 const {geometryHash,sourceRevision,packetHash,...body}=p;
 if('sha256:'+await sha256Hex(input.geometryCanonicalJson)!==geometryHash
  ||'sha256:'+await sha256Hex(input.packetCanonicalJson)!==packetHash
  ||json(JSON.parse(input.geometryCanonicalJson))!==json(body)
  ||json(JSON.parse(input.packetCanonicalJson))!==json({...body,geometryHash,sourceRevision}))return fail('measurement_conflict');
 if(p.anchors.some((a,i)=>i>0&&p.anchors[i-1]!.id>=a.id))return fail('anchor_order_conflict');
 return p;
}
const binding=z.strictObject({
 physicalSlot:z.number().int().min(0).max(4),carriageInstanceId:id,sceneAssetId:id,frameReferenceId:id,colliderReferenceId:id,
 componentLifetimeId:id,authoringRevision:z.number().int().positive().safe(),
 sourceId:id,unitContractId:id,millimetersPerUnit:z.number().positive().finite(),packetHash:unityHash,sourceRevision:z.number().int().positive().safe(),
 // Explicit authored transform into the native frame. Never derive it from slot order.
 matrixConvention:z.literal('ROW_MAJOR_AFFINE'),frameToNative:z.array(z.number().finite()).length(16),
 roofAnchorIds:z.array(id).min(3).max(32),actorAnchorId:id,fireAnchorId:id,hitAnchorId:id,
 leftApproachAnchorIds:z.array(id).min(2).max(32),leftClimbAnchorIds:z.array(id).min(2).max(32),
 rightApproachAnchorIds:z.array(id).min(2).max(32),rightClimbAnchorIds:z.array(id).min(2).max(32)
});
const authoring=z.strictObject({
 context:z.json(),assignmentHash:hash,profileId:id,profileVersion:id,nativeContextId:id,seed:hash,plan:z.json(),runtimeAugmentCatalog:z.json(),
 // Required original rail/roof/topology contract, not a default numeric source.
 railDefinition:z.json(),railDefinitionHash:hash,nativeGeometryConsumerId:id,arrivalBoundaryDefinition:z.json(),
 instances:z.array(binding).min(1).max(5),
 connections:z.array(z.strictObject({fromSlot:z.number().int().min(0).max(4),toSlot:z.number().int().min(0).max(4),
  fromAnchorId:id,toAnchorId:id,intermediateNativePointsMm:z.array(point).max(32)})).max(10)
});
type Prepared=Awaited<ReturnType<BNativeO22OriginalSourceOwner['prepare']>>;
/** Implemented by an independently authenticated scene/authoring publication
 * owner, never the combat runtime registry being proved or a public request body.
 * geometry CAS covers exact scene refs/unit approval/lifetime/export bytes and
 * authored roof/topology/transforms; execution CAS covers the actual consumer,
 * complete catalog support and rail contract. Revisions are source-specific:
 * Unity component sourceRevision is never used as a server row revision. */
export interface BMeasuredPhysicalAuthoringOwner{
 capture(context:Readonly<BDepartureCombatContext>,owned:Readonly<BNativeO22OwnedBase>):Promise<{
  authoring:unknown;authoringHash:string;measurements:readonly BPhysicalMeasurementExport[];assertions:Prepared['assertions'];
 }>;
}
export class MeasuredBNativeO22PhysicalComposer implements BNativeO22PhysicalCompositionSource{
 constructor(private readonly owner?:BMeasuredPhysicalAuthoringOwner){}
 async prepare(context:Readonly<BDepartureCombatContext>,owned:Readonly<BNativeO22OwnedBase>):Promise<Prepared>{
  if(!this.owner)return fail('authoring_source_unavailable');
  const original=await this.owner.capture(structuredClone(context),structuredClone(owned));
  const a=authoring.parse(structuredClone(original.authoring));
  if(await digest(a)!==original.authoringHash||json(a.context)!==json(context)||a.assignmentHash!==owned.assignmentHash
   ||owned.accountId!==context.accountId||owned.runId!==context.runId||a.nativeContextId===context.runId)return fail('authoring_conflict');
  if(await digest(a.railDefinition)!==a.railDefinitionHash)return fail('rail_conflict');
  // The currently exported TEST-only parser cannot consume operating measurements.
  if(['unity.combat-b.geometry-test-model.v1.20261004','unity.combat-b.geometry-instances-test.v1.20261004','unity.combat-b.physical-geometry-test.v1.20261006'].includes(a.nativeGeometryConsumerId))return fail('operating_consumer_required');
  const measurements=await Promise.all(original.measurements.map(readBPhysicalSceneMeasurement));
  if(measurements.length!==a.instances.length||new Set(a.instances.map(i=>i.physicalSlot)).size!==a.instances.length)return fail('instance_conflict');
  const cars=owned.assignment.refitSnapshot.refit.cars;
  if(cars.length!==a.instances.length)return fail('placement_conflict');
  const geometryInstances=a.instances.map((instance,index)=>{
   const m=measurements[index]!;
   const car=cars.find(c=>c.slot===instance.physicalSlot);
   if(!car||car.instanceId!==instance.carriageInstanceId
    ||m.sourceId!==instance.sourceId||m.unitContractId!==instance.unitContractId||m.millimetersPerUnit!==instance.millimetersPerUnit
    ||m.packetHash!==instance.packetHash||m.sourceRevision!==instance.sourceRevision)return fail('instance_conflict');
   const anchors=new Map(m.anchors.map(p=>[p.id,p.positionMm]));
   const required=[...instance.roofAnchorIds,instance.actorAnchorId,instance.fireAnchorId,instance.hitAnchorId,
    ...instance.leftApproachAnchorIds,...instance.leftClimbAnchorIds,...instance.rightApproachAnchorIds,...instance.rightClimbAnchorIds];
   if(required.some(ref=>!anchors.has(ref)))return fail('anchor_binding_missing');
   const t=instance.frameToNative;
   const determinant=t[0]!*(t[5]!*t[10]!-t[6]!*t[9]!)-t[1]!*(t[4]!*t[10]!-t[6]!*t[8]!)+t[2]!*(t[4]!*t[9]!-t[5]!*t[8]!);
   if(t[12]!==0||t[13]!==0||t[14]!==0||t[15]!==1||!Number.isFinite(determinant)||determinant===0)return fail('frame_transform_invalid');
   // Points remain frame-local with their exact authored transform; no guessed
   // world dimensions, AABB conversion, collider-to-roof inference or spacing.
   return {...instance,measurement:structuredClone(m),bodyCornersMm:structuredClone(m.bodyCornersMm),
    roofPointsMm:instance.roofAnchorIds.map(ref=>structuredClone(anchors.get(ref)!))};
  });
  for(const c of a.connections){
   const from=geometryInstances.find(i=>i.physicalSlot===c.fromSlot),to=geometryInstances.find(i=>i.physicalSlot===c.toSlot);
   if(!from||!to||c.fromSlot===c.toSlot||!from.measurement.anchors.some(x=>x.id===c.fromAnchorId)||!to.measurement.anchors.some(x=>x.id===c.toAnchorId))return fail('topology_conflict');
  }
  const catalog=await captureRuntimeAugmentCatalog(a.runtimeAugmentCatalog),plan=captureOperationMap(a.plan);
  if(catalog.contract.formation.sourceHash!==owned.assignmentHash||catalog.contract.formation.operators.length!==1
   ||catalog.contract.formation.operators[0]!.operatorId!==owned.actor.operatorId
   ||catalog.contract.formation.operators[0]!.abilityDefinitionHash!==owned.actor.definitionHash)return fail('catalog_formation_conflict');
  if(await digest(plan)!==context.planHash||plan.definitionVersion!==context.definitionVersion)return fail('plan_conflict');
  const arrival=await prepareBArrivalBoundarySource(context,plan,a.arrivalBoundaryDefinition);
  if(json(arrival.definition.configuration)!==json(a.railDefinition))return fail('rail_conflict');
  const geometryBody={version:'combat.b-measured-physical-composition.v1.20261020',authoringHash:original.authoringHash,
   consumerId:a.nativeGeometryConsumerId,coordinateSpace:'BOUND_FRAME_LOCAL_WITH_EXPLICIT_NATIVE_TRANSFORM',
   instances:geometryInstances,connections:a.connections,railDefinition:a.railDefinition,railDefinitionHash:a.railDefinitionHash};
  const geometry={...geometryBody,sourceHash:await digest(geometryBody)};
  const raw=structuredClone(owned.rawProfile);
  const composition={mode:'ORDINARY_SOLO',factory:'BDirectAttackBasicLoop.CreateOrdinaryTravelEventRuntime',activeOwner:null,miraFirst:null,
   healingModifiers:[],operatorReplacement:false,revival:false,
   operators:[{operatorId:owned.actor.operatorId,actorRef:raw.actorRef,attackKind:'INSTANT_BASIC',hpSourceId:raw.hp.sourceId,actionSourceId:raw.actions[0]!.sourceId}]};
  const source:BDepartureCombatSource={version:bDepartureCombatSourceVersion,binding:{accountId:context.accountId,runId:context.runId,operationPreviewId:context.operationPreviewId,
   operationId:context.operationId,selectedRisk:context.selectedRisk,operationDefinitionVersion:context.definitionVersion,operationPlanHash:context.planHash,operationPublicationHash:context.publicationHash},
   contextId:a.nativeContextId,riskApplicationVersion:operationRiskVersion,rawProfiles:[raw],
   profile:{id:a.profileId,version:a.profileVersion,executorVersion:bNativeO22ExecutorVersion,definition:{
    runtimeAugmentCatalog:catalog.contract,bNativeO22OwnedBase:z.json().parse(structuredClone(owned)),bMeasuredPhysicalGeometry:geometry,bArrivalBoundarySource:arrival,
    bNativeNodeOrigin:await prepareBNativeNodeOrigin(context,a.nativeContextId,a.seed,plan)}}};
  const prepared:Prepared={capture:{source,context:structuredClone(context),plan,namedDefinition:structuredClone(owned.assignment.definition),composition},assertions:original.assertions};
  // Reuse the real executor's exact version, original source, plan, named
  // composition and six nonempty CAS validations. This does not install a host.
  await (await createBNativeO22ExecutorAdapter(source.profile,{prepare:async()=>prepared})).prepare(source,context);
  return prepared;
 }
}
export function createMeasuredBNativeO22PhysicalComposer(owner?:BMeasuredPhysicalAuthoringOwner){return new MeasuredBNativeO22PhysicalComposer(owner);}
export function createMeasuredBNativeO22OriginalSourceOwner(db:D1Database,auth:SelectionPrincipal,owner?:BMeasuredPhysicalAuthoringOwner){
 return createPersistedBNativeO22OriginalSourceOwner(db,auth,createMeasuredBNativeO22PhysicalComposer(owner));
}
