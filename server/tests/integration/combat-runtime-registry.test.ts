import {expect,it} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {setupBDeparturePinFixture} from '../helpers/combat-b-departure-pin-fixture';
import {CombatRuntimeRegistry,createBDepartureCombatSourceOwner} from '../../apps/game-api/src/combat-runtime-registry';
import {captureRuntimeAugmentCatalog,createAugmentRuntimeCatalogSource} from '../../apps/game-api/src/augment-runtime-catalog';
import {prepareBDepartureCombatSnapshot} from '../../apps/game-api/src/combat-b-departure-combat-pin';
import {captureAugmentUtilityDefinitions,augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';

async function fixture(catalog?:unknown){
 const f=await setupBDeparturePinFixture();
 const source=structuredClone(f.source);
 if(catalog!==undefined)Object.assign(source.profile.definition,{runtimeAugmentCatalog:catalog});
 const identity={profileId:source.profile.id,profileVersion:source.profile.version,executorVersion:source.profile.executorVersion,profileHash:await sha256Hex(canonicalJson(source.profile))};
 const adapter={...identity,prepare:async(s:typeof source)=>{
  if(canonicalJson(s.rawProfiles)!==canonicalJson(source.rawProfiles))throw Error('actual_source_changed');
  return [f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")];
 }};
 const origin={accountId:'a',runId:f.newRunId,operationPreviewId:f.newPreviewId,operationId:source.binding.operationId,selectedRisk:source.binding.selectedRisk,
 departureVersion:'departure-v4',baseSnapshotHash:'0'.repeat(64),definitionVersion:source.binding.operationDefinitionVersion,planHash:source.binding.operationPlanHash,publicationHash:source.binding.operationPublicationHash};
 // These fixtures are deliberately synthetic contract tests, never registered game profiles.
 const registry=new CombatRuntimeRegistry(f.env.DB,'LOCAL',[adapter]);
 const registration={accountId:'a',operationPreviewId:f.newPreviewId,source};
 return {...f,source,identity,adapter,origin,registry,registration};
}
function addPreview(f:Awaited<ReturnType<typeof fixture>>){
 f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) SELECT ?,'a',mutation_id,objective,risk,balance_config_version,preview_json,'ACTIVE',expires_at,created_at,admission_version FROM operation_previews WHERE id=?",[f.newPreviewId,f.previewId]);
}
it('installed empty registry rejects unknown executors without registering synthetic support',async()=>{
 const f=await fixture();try{
  await expect(createBDepartureCombatSourceOwner(f.env.DB,'LOCAL').prepare(f.origin)).rejects.toThrow('combat_runtime_source_unavailable');
  await expect(new CombatRuntimeRegistry(f.env.DB,'LOCAL',[]).register(f.registration)).rejects.toThrow('combat_runtime_executor_unavailable');
  expect(f.scalar('SELECT count(*) FROM combat_runtime_sources')).toBe(0);
 }finally{f.close();}
});
it('registration requires the authenticated original preview owner',async()=>{
 const f=await fixture();try{await expect(f.registry.register(f.registration)).rejects.toThrow();expect(f.scalar('SELECT count(*) FROM combat_runtime_sources')).toBe(0);}finally{f.close();}
});
it('preserves the registered candidate identity and rejects rebinding or an already used run',async()=>{
 const f=await fixture();try{
  addPreview(f);await f.registry.register(f.registration);
  expect(await f.registry.inspectPreview('a',f.newPreviewId)).toMatchObject({candidateRunId:f.newRunId});
  await expect(f.registry.prepare({...f.origin,runId:'different-candidate'})).rejects.toThrow('combat_runtime_source_conflict');
  expect((await f.registry.prepare(f.origin)).source).toEqual(f.source);
  f.sql.run("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a',?,'local-retained-mutation',1,'ACTIVE',0)",[f.newRunId,f.newPreviewId]);
  await expect(f.registry.inspectPreview('a',f.newPreviewId)).rejects.toThrow('combat_runtime_candidate_run_used');
  await expect(f.registry.prepare(f.origin)).rejects.toThrow('combat_runtime_candidate_run_used');
  await expect(f.registry.register(f.registration)).rejects.toThrow();
 }finally{f.close();}
});
it('registered source is exact, replayable, immutable and fenced against revocation or owner changes',async()=>{
 const f=await fixture();try{
  addPreview(f);const receipt=await f.registry.register(f.registration);expect(await f.registry.register(f.registration)).toEqual(receipt);
  expect(await f.registry.inspectPreview('a',f.newPreviewId)).toMatchObject({profileHash:f.identity.profileHash});
  const prepared=await f.registry.prepare(f.origin);expect(prepared.source).toEqual(f.source);expect(await f.registry.supports(prepared.source,f.origin)).toBe(true);
  const altered=structuredClone(prepared.source);altered.rawProfiles[0]!.hp.value.numerator='999';expect(await f.registry.supports(altered,f.origin)).toBe(false);
  await f.env.DB.batch([...prepared.assertions,f.env.DB.prepare('DELETE FROM departure_write_guard')]);
  f.sql.run('UPDATE combat_runtime_sources SET enabled=0');
  await expect(f.env.DB.batch(prepared.assertions)).rejects.toThrow();
  expect(()=>f.sql.run('UPDATE combat_runtime_sources SET enabled=1')).toThrow('combat_runtime_source_immutable');
  expect(()=>f.sql.run('DELETE FROM combat_runtime_sources')).toThrow('combat_runtime_source_immutable');
  expect(()=>f.sql.run("UPDATE combat_runtime_sources SET source_hash=?",['f'.repeat(64)])).toThrow('combat_runtime_source_immutable');
  await expect(f.registry.prepare(f.origin)).rejects.toThrow('combat_runtime_source_unavailable');
 }finally{f.close();}
});
it('actual source revision drift aborts the consumer batch and missing executor fences reject',async()=>{
 const f=await fixture();try{
  addPreview(f);await f.registry.register(f.registration);const prepared=await f.registry.prepare(f.origin);
  f.sql.run('UPDATE local_b_source_guard_19 SET revision=2');await expect(f.env.DB.batch(prepared.assertions)).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM departure_write_guard')).toBe(0);
  const missing=new CombatRuntimeRegistry(f.env.DB,'LOCAL',[{...f.adapter,prepare:async()=>[]}]);
  await expect(missing.prepare(f.origin)).rejects.toThrow('combat_runtime_executor_fence_missing');
  await expect(f.registry.prepare({...f.origin,publicationHash:'f'.repeat(64)})).rejects.toThrow('combat_runtime_source_conflict');
  await expect(new CombatRuntimeRegistry(f.env.DB,'DEV',[f.adapter]).prepare(f.origin)).rejects.toThrow('combat_runtime_source_unavailable');
 }finally{f.close();}
});
it('catalog uses complete structural capabilities and healer mapping, never HP or muzzle conditions',async()=>{
 const definitions=await captureAugmentUtilityDefinitions();
 const rows=definitions.definitions.map(d=>({id:d.id,definitionHash:d.definitionHash,requiredCapabilities:d.requiredCapabilities,requiresHealer:d.id==='O23'}));
 const input={version:'combat.runtime-augment-catalog.v1.20261019',catalogVersion:augmentUtilityDefinitionVersion,complete:true,environment:'LOCAL',
  candidates:rows,capabilities:[...new Set(rows.flatMap(r=>r.requiredCapabilities))],formation:{revision:1,sourceHash:'a'.repeat(64),operators:[{operatorId:'op_sera',abilityDefinitionHash:'b'.repeat(64),healer:true}]}};
 expect((await captureRuntimeAugmentCatalog(input)).eligibleIds).toEqual(rows.map(r=>r.id));
 expect((await captureRuntimeAugmentCatalog({...input,formation:{...input.formation,operators:[]}})).eligibleIds).toEqual(rows.filter(r=>r.id!=='O23').map(r=>r.id));
 await expect(captureRuntimeAugmentCatalog({...input,complete:false})).rejects.toThrow();
 await expect(captureRuntimeAugmentCatalog({...input,candidates:[{...rows[4],definitionHash:'c'.repeat(64)}]})).rejects.toThrow();
});
it('persisted pinned catalog supplies all eligible rows and survives admission revocation without upgrading old policy',async()=>{
 const definitions=await captureAugmentUtilityDefinitions(),rows=definitions.definitions.map(d=>({id:d.id,definitionHash:d.definitionHash,requiredCapabilities:d.requiredCapabilities,requiresHealer:d.id==='O23'}));
 const contract={version:'combat.runtime-augment-catalog.v1.20261019',catalogVersion:augmentUtilityDefinitionVersion,complete:true,environment:'LOCAL',candidates:rows,
  capabilities:[...new Set(rows.flatMap(r=>r.requiredCapabilities))],formation:{revision:1,sourceHash:'a'.repeat(64),operators:[{operatorId:'op_sera',abilityDefinitionHash:'b'.repeat(64),healer:true}]}};
 const f=await fixture(contract);try{
  addPreview(f);await f.registry.register(f.registration);
  const pin=await prepareBDepartureCombatSnapshot('a',f.newRunId,f.baseJson,f.registry);
  // Original departure order: consume preview and insert run before source CAS.
  await f.env.DB.batch([
   f.env.DB.prepare("UPDATE operation_previews SET status='CONSUMED' WHERE id=?").bind(f.newPreviewId),
   f.env.DB.prepare("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a',?,'local-retained-mutation',1,'ACTIVE',0)").bind(f.newRunId,f.newPreviewId),
   ...pin.atomicAssertions,
   f.env.DB.prepare('INSERT INTO run_snapshots VALUES(?,?,0)').bind(f.newRunId,pin.snapshotJson),
   f.env.DB.prepare('DELETE FROM departure_write_guard')
  ]);
  const provider=createAugmentRuntimeCatalogSource(f.env.DB,'LOCAL'),context={accountId:'a',runId:f.newRunId,stageId:'synthetic-node',environment:'LOCAL',startSnapshot:pin.snapshotJson};
  const prepared=await provider.prepare(context);expect(prepared.eligibleIds).toEqual(rows.map(r=>r.id));
  // A well-formed pin issued by another synthetic owner still cannot rebind
  // this immutable registry original to a different run.
  const otherRun='LOCAL_DIFFERENT_PIN_RUN',otherBase=JSON.parse(f.baseJson);
  otherBase.runId=otherRun;otherBase.departureResponse.runId=otherRun;
  const otherSource=structuredClone(f.source);otherSource.binding.runId=otherRun;
  const otherPin=await prepareBDepartureCombatSnapshot('a',otherRun,JSON.stringify(otherBase),{
   prepare:async()=>({source:otherSource,assertions:[f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')]}),supports:async()=>true
  });
  await expect(provider.prepare({...context,runId:otherRun,startSnapshot:otherPin.snapshotJson})).rejects.toThrow('augment_runtime_catalog_registry_conflict');
  f.sql.run('UPDATE combat_runtime_sources SET enabled=0');
  expect((await provider.prepare(context)).eligibleIds).toEqual(prepared.eligibleIds);
  await f.env.DB.batch([...prepared.assertions,f.env.DB.prepare('DELETE FROM solo_augment_write_guard')]);
  await expect(provider.prepare({...context,environment:'DEV'})).rejects.toThrow('augment_runtime_catalog_environment_conflict');
  await expect(provider.prepare({...context,accountId:'b'})).rejects.toThrow();
  f.sql.run("UPDATE accounts SET status='BANNED' WHERE id='a'");
  await expect(f.env.DB.batch(prepared.assertions)).rejects.toThrow();
 }finally{f.close();}
});
