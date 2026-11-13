import {expect,it} from 'vitest';
import {persistedBDepartureFixture} from '../helpers/combat-b-persisted-departure-fixture';
import {createPersistedBNativeO22OriginalSourceOwner} from '../../apps/game-api/src/combat-b-native-o22-original-source';
import {BRefitStore} from '../../apps/game-api/src/combat-b-refit-store';
import {PersistedBSelectionCatalog,PersistedBRefitSource} from '../../apps/game-api/src/combat-b-persisted-sources';

async function fixture(){
 const f=await persistedBDepartureFixture();
 const context={accountId:'a',runId:f.newRunId,operationPreviewId:f.newPreviewId,operationId:f.source.binding.operationId,selectedRisk:f.source.binding.selectedRisk,departureVersion:'departure-b-preparation-v1.20261006',baseSnapshotHash:'0'.repeat(64),definitionVersion:f.source.binding.operationDefinitionVersion,planHash:f.source.binding.operationPlanHash,publicationHash:f.source.binding.operationPublicationHash};
 return {...f,context,producer:createPersistedBNativeO22OriginalSourceOwner(f.env.DB,f.auth)};
}
it('produces real persisted Kyle growth and raw baseline with approved applicability and conditional BCB budgets',async()=>{
 const f=await fixture();try{const p=await f.producer.captureOwnedBase(f.context),a=p.snapshot.actor;
  expect(a.operatorId).toBe('op_kyle');expect(a.growth.level).toBe(10);
  expect(p.snapshot.rawProfile.hp.value.numerator).toBe(String(a.stats.maxHp));
  expect(p.snapshot.rawProfile.actions[0]!.value).toEqual(a.stats.attack);
  expect(p.snapshot.applicability.equipment.status).toBe('NOT_APPLICABLE_BEFORE_RUN');
  expect(p.snapshot.applicability.research.status).toBe('NO_APPROVED_ACTIVE_NODE_CATALOG');
  const normal=p.snapshot.basicBudgets.find(r=>r.body==='NORMAL'&&r.target==='ENEMY')!,broken=p.snapshot.basicBudgets.find(r=>r.body==='BROKEN'&&r.target==='ENEMY')!;
  expect(normal.damageBps).toBe(1000);expect(broken.damageBps).toBe(0);expect(normal.rawBeforeBcb).toEqual(a.stats.attack);
  expect(p.snapshot.geometry).toBeNull();expect(p.snapshot.completeRuntimeSource).toBe(false);
  await f.env.DB.batch(p.atomicAssertions);
 }finally{f.close();}
});
it('real growth changes alter source and invalidate earlier consuming fences',async()=>{
 const f=await fixture();try{const first=await f.producer.captureOwnedBase(f.context);
  f.sql.run("UPDATE operator_training_core_slots SET trained_level=11,revision=revision+1 WHERE account_id='a' AND operator_id='op_kyle'");
  await expect(f.env.DB.batch(first.atomicAssertions)).rejects.toThrow();
  const second=await f.producer.captureOwnedBase(f.context);expect(second.snapshot.sourceHash).not.toBe(first.snapshot.sourceHash);expect(second.snapshot.actor.stats.maxHp).toBeGreaterThan(first.snapshot.actor.stats.maxHp);
  expect(first.snapshot.actor.growth.level).toBe(10);
 }finally{f.close();}
});
it('uses actual selected BCB06 rank for normal and broken defense without baking it into raw damage',async()=>{
 const f=await fixture();try{const refits=new BRefitStore(f.env.DB,new PersistedBRefitSource(),new PersistedBSelectionCatalog()),r=await refits.read(f.auth);
  await refits.save(f.auth,{version:r.version,expectedRevision:r.revision,selectionRevision:r.refit!.selectionRevision,rankRevision:r.refit!.rankRevision,definitionHash:r.definitionHash,sourceHash:r.sourceHash,refits:[{slot:0,type:'BCB06',rank:1},{slot:4,type:'BCB06',rank:1}]},'native-source-defense');
  const p=await f.producer.captureOwnedBase(f.context);
  expect(p.snapshot.defenseByBody.find(r=>r.body==='NORMAL')!.bcbFlat).toBe(20);
  expect(p.snapshot.defenseByBody.find(r=>r.body==='BROKEN')!.bcbFlat).toBe(0);
  expect(p.snapshot.basicBudgets.find(r=>r.body==='NORMAL'&&r.target==='ENEMY')!.damageBps).toBe(0);
 }finally{f.close();}
});
it('missing geometry/native composition cannot turn computed owned stats into admitted runtime support',async()=>{
 const f=await fixture();try{await expect(f.producer.prepare(f.context)).rejects.toThrow('combat_b_native_o22_geometry_composition_source_unavailable');
  expect(f.scalar('SELECT count(*) FROM combat_runtime_sources')).toBe(0);
  await expect(f.producer.captureOwnedBase({...f.context,accountId:'b'})).rejects.toThrow('combat_b_native_o22_account_conflict');
 }finally{f.close();}
});
it('passes exact calculated owned source to a physical producer and fences it in the final native preparation',async()=>{
 const f=await fixture();try{
  let replaceRaw=false;
  const physical={prepare:async(context:typeof f.context,owned:Awaited<ReturnType<typeof f.producer.captureOwnedBase>>['snapshot'])=>{
   const source=structuredClone(f.source);Object.assign(source.profile.definition,{bNativeO22OwnedBase:owned});
   source.rawProfiles=[structuredClone(owned.rawProfile)];if(replaceRaw)source.rawProfiles[0]!.hp.value.numerator='1';
   const fence=()=>[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")];
   return {capture:{source,context,plan:f.plan,namedDefinition:owned.assignment.definition,composition:{}},assertions:{profile:fence(),rawSources:fence(),formation:fence(),geometry:fence(),nativeSeed:fence(),execution:fence()}};
  }};
  const producer=createPersistedBNativeO22OriginalSourceOwner(f.env.DB,f.auth,physical),prepared=await producer.prepare(f.context);
  expect(prepared.assertions.formation.length).toBeGreaterThan(1);
  await f.env.DB.batch([...prepared.assertions.formation,f.env.DB.prepare('DELETE FROM departure_write_guard')]);
  f.sql.run("UPDATE account_operators SET promotion=1,revision=revision+1 WHERE account_id='a' AND operator_id='op_kyle'");
  await expect(f.env.DB.batch([...prepared.assertions.formation])).rejects.toThrow();
  replaceRaw=true;await expect(producer.prepare(f.context)).rejects.toThrow('combat_b_native_o22_owned_composition_conflict');
 }finally{f.close();}
});
