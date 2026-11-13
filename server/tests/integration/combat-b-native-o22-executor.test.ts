import {expect,it} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {setupBDeparturePinFixture} from '../helpers/combat-b-departure-pin-fixture';
import {prepareBNativeNodeOrigin} from '../../apps/game-api/src/combat-b-native-node-source';
import {bNamedOperatorDefinition} from '../../apps/game-api/src/combat-b-named-operator-source';
import {createBNativeO22ExecutorAdapter,loadPersistedBNativeO22Executors,bNativeO22ExecutorVersion,bNativeO22ConsumerCatalogVersion} from '../../apps/game-api/src/combat-b-native-o22-executor';
import {CombatRuntimeRegistry} from '../../apps/game-api/src/combat-runtime-registry';
import {captureAugmentUtilityDefinitions} from '../../apps/game-api/src/augment-utility-definition';

/** Synthetic authority captures only. No operational profile is seeded. */
async function fixture(){
 const f=await setupBDeparturePinFixture(),source=structuredClone(f.source);
 const context={accountId:'a',runId:f.newRunId,operationPreviewId:f.newPreviewId,operationId:source.binding.operationId,selectedRisk:source.binding.selectedRisk,
  departureVersion:'departure-v4',baseSnapshotHash:'0'.repeat(64),definitionVersion:source.binding.operationDefinitionVersion,planHash:source.binding.operationPlanHash,publicationHash:source.binding.operationPublicationHash};
 source.profile.executorVersion=bNativeO22ExecutorVersion;
 Object.assign(source.profile.definition,{runtimeAugmentCatalog:{catalogVersion:bNativeO22ConsumerCatalogVersion,complete:true},
  bNativeNodeOrigin:await prepareBNativeNodeOrigin(context,source.contextId,'1'.repeat(64),f.plan)});
 const capture={source,context,plan:f.plan,namedDefinition:await bNamedOperatorDefinition(),
  composition:{mode:'ORDINARY_SOLO',factory:'BDirectAttackBasicLoop.CreateOrdinaryTravelEventRuntime',activeOwner:null,miraFirst:null,healingModifiers:[],operatorReplacement:false,revival:false,
   operators:[{operatorId:'op_kyle',actorRef:source.rawProfiles[0]!.actorRef,attackKind:'INSTANT_BASIC',hpSourceId:source.rawProfiles[0]!.hp.sourceId,actionSourceId:source.rawProfiles[0]!.actions[0]!.sourceId}]}};
 const guards=()=>[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")];
 const owner={prepare:async()=>({capture:structuredClone(capture),assertions:{profile:guards(),rawSources:guards(),formation:guards(),geometry:guards(),nativeSeed:guards(),execution:guards()}})};
 return {...f,source,context,capture,owner};
}
it('requires the actual original producer instead of accepting executor identity as support',async()=>{
 const f=await fixture();try{const adapter=await createBNativeO22ExecutorAdapter(f.source.profile);await expect(adapter.prepare(f.source,f.context)).rejects.toThrow('combat_b_native_o22_original_source_unavailable');}finally{f.close();}
});
it('rejects future unsupported catalog versions',async()=>{
 const f=await fixture();try{Object.assign(f.source.profile.definition,{runtimeAugmentCatalog:{catalogVersion:'augment.utility-definitions.v9.unapproved',complete:true}});
  await expect(createBNativeO22ExecutorAdapter(f.source.profile,f.owner)).rejects.toThrow('combat_b_native_o22_consumer_catalog_unsupported');
 }finally{f.close();}
});
it('accepts v8 only with the canonical definitions consumed by Unity and still requires original authority',async()=>{
 const f=await fixture();try{
  const definitions=await captureAugmentUtilityDefinitions();
  expect(definitions.definitionHash).toBe('5eb2c9d669f7fb43ca5955371ced5d9d6581df1b533ecf35049dfc0d773f1813');
  expect(definitions.definitions.find(d=>d.id==='O22')!.definitionHash).toBe('2e089220f84434bd20da82d2877fd2cd02ec82a8fd60c1ef27076c4eed20e5c0');
  expect(definitions.definitions.find(d=>d.id==='O23')!.definitionHash).toBe('0aa08bebed057a3591052882f12a92657fa58873a00d1deedfc47588057c6f25');
  Object.assign(f.source.profile.definition,{runtimeAugmentCatalog:{catalogVersion:definitions.version,complete:true}});
  f.capture.source=structuredClone(f.source);
  const adapter=await createBNativeO22ExecutorAdapter(f.source.profile,f.owner);
  expect(await adapter.prepare(f.source,f.context)).toHaveLength(6);
  const missing=await createBNativeO22ExecutorAdapter(f.source.profile);
  await expect(missing.prepare(f.source,f.context)).rejects.toThrow('original_source_unavailable');
 }finally{f.close();}
});
it('binds independently verified native source and all mutable owner fences to the consuming transaction',async()=>{
 const f=await fixture();try{const adapter=await createBNativeO22ExecutorAdapter(f.source.profile,f.owner);
  expect(adapter.profileHash).toBe(await sha256Hex(canonicalJson(f.source.profile)));
  const guards=await adapter.prepare(f.source,f.context);expect(guards).toHaveLength(6);
  await f.env.DB.batch([...guards,f.env.DB.prepare('DELETE FROM departure_write_guard')]);
  f.sql.run('UPDATE local_b_source_guard_19 SET revision=2');await expect(f.env.DB.batch([...guards])).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM departure_write_guard')).toBe(0);
 }finally{f.close();}
});
it('rejects mismatched source, incomplete fences and unsupported active or delayed operator composition',async()=>{
 const f=await fixture();try{const adapter=await createBNativeO22ExecutorAdapter(f.source.profile,f.owner),altered=structuredClone(f.source);altered.rawProfiles[0]!.hp.value.numerator='987';
  await expect(adapter.prepare(altered,f.context)).rejects.toThrow('combat_b_native_o22_original_conflict');
  const missing={prepare:async()=>{const p=await f.owner.prepare();p.assertions.geometry=[];return p;}};
  await expect((await createBNativeO22ExecutorAdapter(f.source.profile,missing)).prepare(f.source,f.context)).rejects.toThrow('combat_b_native_o22_geometry_fence_missing');
  f.capture.composition.operators[0]!.attackKind='DELAYED_PROJECTILE';await expect(adapter.prepare(f.source,f.context)).rejects.toThrow();
 }finally{f.close();}
});
it('loads only the authenticated original registered executor and still requires independent native source proof',async()=>{
 const f=await fixture();try{
  expect(await loadPersistedBNativeO22Executors(f.env.DB,'LOCAL','a',f.newPreviewId)).toEqual([]);
  f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) SELECT ?,'a',mutation_id,objective,risk,balance_config_version,preview_json,'ACTIVE',expires_at,created_at,admission_version FROM operation_previews WHERE id=?",[f.newPreviewId,f.previewId]);
  const executable=await createBNativeO22ExecutorAdapter(f.source.profile,f.owner);
  await new CombatRuntimeRegistry(f.env.DB,'LOCAL',[executable]).register({accountId:'a',operationPreviewId:f.newPreviewId,source:f.source});
  expect(await loadPersistedBNativeO22Executors(f.env.DB,'LOCAL','b',f.newPreviewId,f.owner)).toEqual([]);
  expect(await loadPersistedBNativeO22Executors(f.env.DB,'DEV','a',f.newPreviewId,f.owner)).toEqual([]);
  const [missing]=await loadPersistedBNativeO22Executors(f.env.DB,'LOCAL','a',f.newPreviewId);
  await expect(missing!.prepare(f.source,f.context)).rejects.toThrow('original_source_unavailable');
  const [bound]=await loadPersistedBNativeO22Executors(f.env.DB,'LOCAL','a',f.newPreviewId,f.owner);
  expect(await bound!.prepare(f.source,f.context)).toHaveLength(6);
  f.sql.run('UPDATE combat_runtime_sources SET enabled=0');
  expect(await loadPersistedBNativeO22Executors(f.env.DB,'LOCAL','a',f.newPreviewId,f.owner)).toEqual([]);
 }finally{f.close();}
});
