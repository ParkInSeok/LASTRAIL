import {it,expect} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {readBPhysicalSceneMeasurement,createMeasuredBNativeO22PhysicalComposer,createMeasuredBNativeO22OriginalSourceOwner} from '../../apps/game-api/src/combat-b-native-o22-physical-composer';
import {persistedBDepartureFixture} from '../helpers/combat-b-persisted-departure-fixture';
import {captureAugmentUtilityDefinitions} from '../../apps/game-api/src/augment-utility-definition';
import {createBNativeO22ExecutorAdapter} from '../../apps/game-api/src/combat-b-native-o22-executor';
import {BPhysicalPublicationStore} from '../../apps/game-api/src/combat-b-physical-publication';
async function measurement(){
 const body={version:'unity.combat-b.scene-measurement.v1.20261020',sourceId:'synthetic-box',provenance:'UNITY_SCENE_MEASUREMENT_NOT_EXECUTION_AUTHORITY',
  unitContractId:'synthetic-unit',millimetersPerUnit:1000,coordinateSpace:'BOUND_FRAME_LOCAL',quantization:'NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO',
  bodyCornersMm:[[-1,-1,-1],[-1,-1,1],[-1,1,-1],[-1,1,1],[1,-1,-1],[1,-1,1],[1,1,-1],[1,1,1]],
  anchors:[{id:'back',positionMm:[-1,1,0]},{id:'front',positionMm:[1,1,0]},{id:'hit',positionMm:[0,1,1]}]};
 // Newtonsoft preserves the Unity double token. JS canonical JSON would erase .0.
 const geometryCanonicalJson=canonicalJson(body).replace('"millimetersPerUnit":1000,','"millimetersPerUnit":1000.0,');
 const geometryHash='sha256:'+await sha256Hex(geometryCanonicalJson);
 const packetBody={...body,geometryHash,sourceRevision:1};
 const packetCanonicalJson=canonicalJson(packetBody).replace('"millimetersPerUnit":1000,','"millimetersPerUnit":1000.0,');
 return {packet:{...packetBody,packetHash:'sha256:'+await sha256Hex(packetCanonicalJson)},geometryCanonicalJson,packetCanonicalJson};
}
it('validates actual Unity double-preserving original bytes without promoting measurement into authority',async()=>{
 const input=await measurement(),read=await readBPhysicalSceneMeasurement(input);
 expect(read).toEqual(input.packet);
 await expect(readBPhysicalSceneMeasurement({...input,packet:{...input.packet,sourceRevision:2}})).rejects.toThrow();
 await expect(readBPhysicalSceneMeasurement({...input,geometryCanonicalJson:canonicalJson(JSON.parse(input.geometryCanonicalJson))})).rejects.toThrow();
});
it('refuses missing independently authored source instead of fabricating topology',async()=>{
 await expect(createMeasuredBNativeO22PhysicalComposer().prepare({} as never,{} as never)).rejects.toThrow('combat_b_physical_authoring_source_unavailable');
});
it('composes real persisted owned raw/native fields and exact synthetic authored measurements through original owner and executor',async()=>{
 const f=await persistedBDepartureFixture();try{
  const context={accountId:'a',runId:f.newRunId,operationPreviewId:f.newPreviewId,operationId:f.source.binding.operationId,selectedRisk:f.source.binding.selectedRisk,
   departureVersion:'departure-b-preparation-v1.20261006',baseSnapshotHash:'0'.repeat(64),definitionVersion:f.source.binding.operationDefinitionVersion,planHash:f.source.binding.operationPlanHash,publicationHash:f.source.binding.operationPublicationHash};
  const measured=await measurement(),definitions=await captureAugmentUtilityDefinitions(),o22=definitions.definitions.find(d=>d.id==='O22')!;
  let missingAnchor=false,missingFence=false,wrongLifetime=false,testConsumer=false;
  const key=await crypto.subtle.importKey('raw',new TextEncoder().encode('LOCAL_MEASUREMENT_PUBLICATION_KEY_ONLY'),{name:'HMAC',hash:'SHA-256'},false,['sign','verify']);
  const owner=createMeasuredBNativeO22OriginalSourceOwner(f.env.DB,f.auth,{capture:async(c,owned)=>{
   const instances=owned.assignment.refitSnapshot.refit.cars.map(car=>({
    physicalSlot:car.slot,carriageInstanceId:car.instanceId,sceneAssetId:'synthetic-scene',frameReferenceId:'synthetic-frame-'+car.slot,
    colliderReferenceId:'synthetic-collider-'+car.slot,componentLifetimeId:'synthetic-lifetime-'+car.slot,authoringRevision:1,
    sourceId:measured.packet.sourceId,unitContractId:measured.packet.unitContractId,millimetersPerUnit:1000,packetHash:measured.packet.packetHash,sourceRevision:wrongLifetime?2:1,
    matrixConvention:'ROW_MAJOR_AFFINE',frameToNative:[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1],
    roofAnchorIds:['back','front','hit'],actorAnchorId:missingAnchor?'absent':'hit',fireAnchorId:'hit',hitAnchorId:'hit',
    leftApproachAnchorIds:['back','hit'],leftClimbAnchorIds:['hit','front'],rightApproachAnchorIds:['front','hit'],rightClimbAnchorIds:['hit','back']}));
   // Explicit synthetic rail/edge numbers. Production composer supplies none.
   const railDefinition={version:'rail-journey-v1',selectionScaleBps:10000,recoveryMs:500,baseSpeedMillimetersPerSecond:1000,
    throttleMinimumBps:1,throttleMaximumBps:13000,throttleCruiseBps:10000,offers:[]};
   const authoring={context:c,assignmentHash:owned.assignmentHash,profileId:'SYNTHETIC_MEASURED_PROFILE',profileVersion:'synthetic-v1',
    nativeContextId:'SYNTHETIC_NATIVE_CONTEXT',seed:'1'.repeat(64),plan:f.plan,
    runtimeAugmentCatalog:{version:'combat.runtime-augment-catalog.v1.20261019',catalogVersion:definitions.version,complete:true,environment:'LOCAL',
     candidates:[{id:'O22',definitionHash:o22.definitionHash,requiredCapabilities:o22.requiredCapabilities,requiresHealer:false}],capabilities:o22.requiredCapabilities,
     formation:{revision:1,sourceHash:owned.assignmentHash,operators:[{operatorId:'op_kyle',abilityDefinitionHash:owned.actor.definitionHash,healer:false}]}},
    railDefinition,railDefinitionHash:await sha256Hex(canonicalJson(railDefinition)),
    arrivalBoundaryDefinition:{definitionId:'synthetic-boundaries',definitionVersion:'synthetic-v1',stepTicks:200000,executorVersion:'combat.b-arrival-linear-fixed-step.v1.20261020',
     speedHalfUnits:2,configuration:railDefinition,boundaries:f.plan.edges.map(e=>({edgeId:e.edgeId,fromNodeId:e.fromNodeId,toNodeId:e.toNodeId,distanceMm:1000}))},
    nativeGeometryConsumerId:testConsumer?'unity.combat-b.physical-geometry-test.v1.20261006':'SYNTHETIC_INDEPENDENT_EXECUTION_OWNER',
    instances,connections:[{fromSlot:0,toSlot:4,fromAnchorId:'front',toAnchorId:'back',intermediateNativePointsMm:[]}]};
   const fence=()=>[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")];
   const bodyJson=canonicalJson({version:'combat.b-physical-publication.v1.20261020',environment:'DEV',publicationId:await sha256Hex(canonicalJson(authoring)),
    keyId:'local-key',authoring,measurements:instances.map(()=>measured)});
   const signature=Array.from(new Uint8Array(await crypto.subtle.sign('HMAC',key,new TextEncoder().encode(bodyJson))),b=>b.toString(16).padStart(2,'0')).join('');
   const publication=new BPhysicalPublicationStore(f.env.DB,'DEV',{keyId:'local-key',key},await sha256Hex(canonicalJson(authoring)));
   await publication.publish({bodyJson,signature});
   const captured=await publication.capture(c,owned);
   return {...captured,assertions:{...captured.assertions,rawSources:[...captured.assertions.rawSources,...fence()],geometry:missingFence?[]:captured.assertions.geometry}};
  }});
  const prepared=await owner.prepare(context),source=prepared.capture.source as {profile:Record<string,unknown>;rawProfiles:unknown[]};
  const adapter=await createBNativeO22ExecutorAdapter(source.profile,owner);
  const fences=await adapter.prepare(source as never,context);
  const definition=(source.profile.definition as Record<string,unknown>);
  expect(definition.bNativeNodeOrigin).toBeTruthy();
  expect(source.rawProfiles).toEqual([(definition.bNativeO22OwnedBase as {rawProfile:unknown}).rawProfile]);
  expect(definition.bMeasuredPhysicalGeometry).toMatchObject({instances:[{bodyCornersMm:measured.packet.bodyCornersMm},{}]});
  await f.env.DB.batch([...fences,f.env.DB.prepare('DELETE FROM departure_write_guard')]);
  missingAnchor=true;await expect(owner.prepare(context)).rejects.toThrow('anchor_binding_missing');missingAnchor=false;
  wrongLifetime=true;await expect(owner.prepare(context)).rejects.toThrow('instance_conflict');wrongLifetime=false;
  missingFence=true;await expect(owner.prepare(context)).rejects.toThrow('geometry_fence_missing');missingFence=false;
  testConsumer=true;await expect(owner.prepare(context)).rejects.toThrow('operating_consumer_required');testConsumer=false;
  f.sql.run('UPDATE local_b_source_guard_19 SET revision=2');
  await expect(f.env.DB.batch(fences)).rejects.toThrow();
 }finally{f.close();}
});
