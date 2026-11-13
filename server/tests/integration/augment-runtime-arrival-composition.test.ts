import {afterEach,expect,it} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {setupBDeparturePinFixture} from '../helpers/combat-b-departure-pin-fixture';
import {CombatRuntimeRegistry} from '../../apps/game-api/src/combat-runtime-registry';
import {createAugmentRuntimeCatalogSource,runtimeAugmentCatalogVersion} from '../../apps/game-api/src/augment-runtime-catalog';
import {prepareBDepartureCombatSnapshot} from '../../apps/game-api/src/combat-b-departure-combat-pin';
import {captureAugmentUtilityDefinitions,augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import {OperationMapPublisher} from '../../apps/game-api/src/operation-map-publisher';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {issueAugmentForArrival} from '../../apps/game-api/src/augment-arrival-issuer';
import type {AugmentEligibleCatalogSource} from '../../apps/game-api/src/augment-node-issuer';

const cleanup:(()=>void)[]=[];
afterEach(()=>{for(const close of cleanup.splice(0))close();});

/** Synthetic LOCAL source/arrival evidence, exercising the installed registry,
 * pinned provider, original map receipts and real grant/no-award transaction.
 * This is NOT evidence of an operational Unity host or Player combat execution. */
async function fixture(mode:'candidates'|'zero'|'invalid'='candidates'){
 const f=await setupBDeparturePinFixture();cleanup.push(f.close);
 const base=JSON.parse(f.baseJson),plan=structuredClone(f.plan);
 plan.enabledEventKinds.push('AUGMENT');
 const node=plan.nodes.find(n=>n.nodeId==='n1-1')!;node.eventKind='AUGMENT';
 for(const n of plan.nodes){n.encounterProfileRef='synthetic-arrival-'+n.eventKind;n.rewardPlanRef='synthetic-reward-'+n.eventKind;}
 const kinds=[...new Set(plan.nodes.map(n=>n.eventKind))];
 const publication=await new OperationMapPublisher({read:async()=>({version:'operation.map-catalog.v1',catalogId:'SYNTHETIC_ARRIVAL_COMPOSITION_NOT_RUNTIME',revision:1,definitionVersion:plan.definitionVersion,
  encounters:kinds.map(eventKind=>({ref:'synthetic-arrival-'+eventKind,version:'SYNTHETIC_v1',profile:'ORDINARY_12_NODE',eventKind,definition:{purpose:'LOCAL_TRANSACTION_TEST_ONLY'}})),
  rewards:kinds.map(eventKind=>({ref:'synthetic-reward-'+eventKind,version:'SYNTHETIC_v1',profile:'ORDINARY_12_NODE',eventKind,definition:{}}))})}).publish(plan);
 base.admission.operationMapPlan=plan;base.departureResponse.admission.operationMapPlan=plan;base.operationMapContent=publication;
 const definitions=await captureAugmentUtilityDefinitions();
 const candidates=mode==='zero'?[]:definitions.definitions.map(d=>({id:d.id,definitionHash:d.definitionHash,requiredCapabilities:d.requiredCapabilities,requiresHealer:d.id==='O23'}));
 const contract={version:runtimeAugmentCatalogVersion,catalogVersion:augmentUtilityDefinitionVersion,complete:mode!=='invalid',environment:'LOCAL',candidates,
  capabilities:[...new Set(candidates.flatMap(c=>c.requiredCapabilities))],formation:{revision:1,sourceHash:'a'.repeat(64),operators:[{operatorId:'op_sera',abilityDefinitionHash:'b'.repeat(64),healer:true}]}};
 const source=structuredClone(f.source);
 source.binding.operationPlanHash=await sha256Hex(canonicalJson(plan));source.binding.operationPublicationHash=publication.contentHash;
 Object.assign(source.profile.definition,{runtimeAugmentCatalog:contract});
 const executor={profileId:source.profile.id,profileVersion:source.profile.version,executorVersion:source.profile.executorVersion,profileHash:await sha256Hex(canonicalJson(source.profile)),
  prepare:async()=>[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")]};
 f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) SELECT ?,'a',mutation_id,objective,risk,balance_config_version,?,'ACTIVE',expires_at,created_at,admission_version FROM operation_previews WHERE id=?",[f.newPreviewId,JSON.stringify(base),f.previewId]);
 const registry=new CombatRuntimeRegistry(f.env.DB,'LOCAL',[executor]);
 await registry.register({accountId:'a',operationPreviewId:f.newPreviewId,source});
 const pinned=await prepareBDepartureCombatSnapshot('a',f.newRunId,JSON.stringify(base),registry);
 await f.env.DB.batch([
  f.env.DB.prepare("UPDATE operation_previews SET status='CONSUMED' WHERE id=?").bind(f.newPreviewId),
  f.env.DB.prepare("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a',?,'local-retained-mutation',1,'ACTIVE',0)").bind(f.newRunId,f.newPreviewId),
  ...pinned.atomicAssertions,f.env.DB.prepare('INSERT INTO run_snapshots VALUES(?,?,0)').bind(f.newRunId,pinned.snapshotJson),f.env.DB.prepare('DELETE FROM departure_write_guard')
 ]);
 const map=new OperationMapStore(f.env.DB,{prepare:async c=>({proof:{kind:c.command.kind,verificationId:'SYNTHETIC_ORIGINAL_ARRIVAL_NOT_PLAYER',holdIds:[]},
  assertions:[f.env.DB.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")]})});
 await map.initialize('a',f.newRunId);
 await map.commit('a',f.newRunId,{eventId:'synthetic-travel',expectedRevision:1,kind:'CONFIRM',edgeId:'entry:n1-1'});
 const receipt=await map.commit('a',f.newRunId,{eventId:'synthetic-arrival',expectedRevision:2,kind:'ARRIVE',edgeId:'entry:n1-1',evidenceRef:'SYNTHETIC_ORIGINAL_ARRIVAL_NOT_PLAYER'});
 const originalProvider=createAugmentRuntimeCatalogSource(f.env.DB,'LOCAL');let catalogReads=0;
 const catalog:AugmentEligibleCatalogSource={prepare:async c=>{catalogReads++;return originalProvider.prepare(c);}};
 return {...f,map,receipt,candidates,catalog,reads:()=>catalogReads,
  issue:()=>issueAugmentForArrival(f.env.DB,'a',f.newRunId,'LOCAL',receipt,catalog)};
}

it('synthetic registered runtime catalog reaches the actual arrival issuer and recovers the same grant without catalog reread',async()=>{
 const f=await fixture(),first=await f.issue();
 expect(first).toMatchObject({state:'SELECTING',stageId:'n1-1',catalogVersion:augmentUtilityDefinitionVersion});
 if(!first||'outcome' in first)throw Error('expected original grant');
 expect(first.slots).toHaveLength(3);
 expect(new Set(first.slots.map(s=>s.candidateId)).size).toBe(3);
 expect(first.slots.every(s=>f.candidates.some(c=>c.id===s.candidateId))).toBe(true);
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants WHERE run_id=?',[f.newRunId])).toBe(1);
 f.sql.run('UPDATE combat_runtime_sources SET enabled=0');
 expect(await f.issue()).toEqual(first);expect(f.reads()).toBe(1);
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants WHERE run_id=?',[f.newRunId])).toBe(1);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
});

it('synthetic complete empty runtime catalog commits one no-award completion and original-arrival retry reuses it',async()=>{
 const f=await fixture('zero'),first=await f.issue();
 expect(first).toMatchObject({outcome:'NO_AWARD',catalogVersion:augmentUtilityDefinitionVersion,mapReceipt:{state:{phase:'READY',holdIds:[]}}});
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants WHERE run_id=?',[f.newRunId])).toBe(0);
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE run_id=? AND event_id LIKE 'node:%'",[f.newRunId])).toBe(1);
 expect(await f.issue()).toEqual(first);expect(f.reads()).toBe(1);
 expect((await f.map.read('a',f.newRunId)).state.phase).toBe('READY');
});

it('synthetic incomplete source fails after original arrival without turning its error into no-award or altering map state',async()=>{
 const f=await fixture('invalid'),before=await f.map.read('a',f.newRunId);
 await expect(f.issue()).rejects.toThrow();
 expect(await f.map.read('a',f.newRunId)).toEqual(before);
 expect(await f.map.lookup('a',f.newRunId,f.receipt.eventId)).toEqual(f.receipt);
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants WHERE run_id=?',[f.newRunId])).toBe(0);
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE run_id=? AND event_id LIKE 'node:%'",[f.newRunId])).toBe(0);
});

it('invented arrival receipt fails before runtime catalog access or grant creation',async()=>{
 const f=await fixture();
 await expect(issueAugmentForArrival(f.env.DB,'a',f.newRunId,'LOCAL',{...f.receipt,eventId:'invented'},f.catalog)).rejects.toThrow('augment_arrival_receipt_invalid');
 expect(f.reads()).toBe(0);expect(f.scalar('SELECT count(*) FROM solo_augment_grants WHERE run_id=?',[f.newRunId])).toBe(0);
});
