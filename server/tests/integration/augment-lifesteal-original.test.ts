import {afterEach,expect,it,vi} from 'vitest';
import {sha256Hex} from '@lastrail/common';
import {setup} from '../helpers/departure-fixture';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import * as definitions from '../../apps/game-api/src/augment-utility-definition';
import {AugmentLifestealOriginalStore} from '../../apps/game-api/src/augment-lifesteal-original';
const cleanup:(()=>void)[]=[];afterEach(()=>{vi.restoreAllMocks();for(const close of cleanup.splice(0))close();});
async function fixture(selected=true){
 const f=await setup(true);cleanup.push(()=>f.sql.close());const p=await(await f.preview()).json(),runId=(await(await f.start(p.operationPreviewId)).json()).runId as string;
 const grants=new SoloAugmentStore(f.env.DB,()=>0),eventId='o22-grant';
 const first=await grants.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId,eventId,stageId:'stage',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22']});
 if(selected)await grants.execute({accountId:'a',environment:'DEV'},runId,eventId,'select',{operation:'SELECT',command:{eventId,expectedRevision:0,slotIndex:0,candidateId:first.slots[0]!.candidateId}});
 return {...f,runId,grants,store:new AugmentLifestealOriginalStore(f.env.DB),request:{runId,eventId,environment:'DEV',expectedStoreRevision:selected?2:1,expectedCatalogVersion:augmentUtilityDefinitionVersion,expectedStartSnapshotHash:await sha256Hex(String(f.scalar('SELECT snapshot_json FROM run_snapshots')))}};
}
it('persists the confirmed O22 definition original and replays it without granting runtime APPLIED',async()=>{
 const f=await fixture(),original=await f.store.capture('a',f.request);
 expect(original).toMatchObject({version:'combat.o22-application-original.v1.20261019',status:'SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED',combatOwner:'UNITY_LOCAL',runId:f.runId,effectId:'O22',applied:false});
 expect(original.definition.selectedDefinition.rules.damageToHealBps).toBe(1200);
 expect(await new AugmentLifestealOriginalStore(f.env.DB).capture('a',f.request)).toEqual(original);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
});
it('rejects selecting and foreign-account requests',async()=>{
 const f=await fixture(false);await expect(f.store.capture('a',f.request)).rejects.toThrow();await expect(f.store.capture('b',f.request)).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
});
it('replays persisted selection and application bytes without the current definition producer or another write',async()=>{
 const f=await fixture(),original=await f.store.capture('a',f.request);
 // Real current producer output, not a fabricated v7 fixture. This verifies the
 // version-independent recovery path; historical v7 byte provenance is separate.
 const savedReceipt=String(f.scalar('SELECT response_json FROM solo_augment_receipts'));
 const savedOriginal=String(f.scalar('SELECT original_json FROM run_augment_effect_originals'));
 const producer=vi.spyOn(definitions,'captureAugmentUtilityDefinitions').mockRejectedValue(new Error('current definition unsupported'));
 const writes=vi.spyOn(f.env.DB,'batch').mockRejectedValue(new Error('recovery must not write'));
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 const recovered=await f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'select',
  {operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}});
 expect(recovered).toEqual(JSON.parse(savedReceipt));
 expect(await f.store.capture('a',f.request)).toEqual(original);
 expect(producer).not.toHaveBeenCalled();expect(writes).not.toHaveBeenCalled();
 expect(f.scalar('SELECT response_json FROM solo_augment_receipts')).toBe(savedReceipt);
 expect(f.scalar('SELECT original_json FROM run_augment_effect_originals')).toBe(savedOriginal);
 expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
});
it('rejects changed pinned definition content even when the receipt still names the original version',async()=>{
 const f=await fixture(),saved=JSON.parse(String(f.scalar('SELECT response_json FROM solo_augment_receipts')));
 saved.applicationOriginal.definition.selectedDefinition.rules.damageToHealBps=9999;
 f.sql.run('DROP TRIGGER solo_augment_receipt_no_update');
 f.sql.run('UPDATE solo_augment_receipts SET response_json=?',[JSON.stringify(saved)]);
 await expect(f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'select',
  {operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}})).rejects.toThrow('o22_original_storage_invalid');
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
});
it('rolls back a stale source and recovers a lost commit acknowledgement',async()=>{
 const f=await fixture(false),select=()=>f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'choose',{operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}});
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE solo_augment_grants SET write_revision=write_revision+1');};
 await expect(select()).rejects.toThrow();expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 const batch=f.env.DB.batch.bind(f.env.DB);let lost=true;f.env.DB.batch=async statements=>{const r=await batch(statements);if(lost){lost=false;throw Error('ACK lost');}return r;};
 const success=await select();expect(success).toHaveProperty('applicationOriginal');expect(await select()).toEqual(success);
 const current={...f.request,expectedStoreRevision:3};expect(await f.store.capture('a',current)).toEqual(success.applicationOriginal);
});
it('preserves the original after later grant mutation and rejects a second O22 grant',async()=>{
 const f=await fixture(),original=await f.store.capture('a',f.request);
 f.sql.run("UPDATE solo_augment_grants SET checkpoint_json='{}',write_revision=write_revision+1");
 expect(await f.store.capture('a',f.request)).toEqual(original);
 const eventId='duplicate-grant';await f.grants.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId:f.runId,eventId,stageId:'stage2',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22']});
 await expect(f.grants.execute({accountId:'a',environment:'DEV'},f.runId,eventId,'select2',{operation:'SELECT',command:{eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}})).rejects.toThrow();
 await expect(f.store.capture('a',{...f.request,eventId})).rejects.toThrow();expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
});
it('rolls back selection and application together if the command receipt cannot commit',async()=>{
 const f=await fixture(false);f.sql.run("CREATE TRIGGER reject_selection_receipt BEFORE INSERT ON solo_augment_receipts BEGIN SELECT RAISE(ABORT,'receipt failure'); END");
 await expect(f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'choose',{operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}})).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 expect((await f.grants.read({accountId:'a',environment:'DEV'},f.runId,f.request.eventId)).state).toBe('SELECTING');
});
it('rejects account suspension between source read and selection commit',async()=>{
 const f=await fixture(false);f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");};
 await expect(f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'choose',{operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}})).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(0);
});
it.each(['definition','scope'])('rejects corrupted %s in the selection receipt',async kind=>{
 const f=await fixture();const saved=JSON.parse(String(f.scalar('SELECT response_json FROM solo_augment_receipts')));
 if(kind==='definition')saved.applicationOriginal.definition=null;else saved.applicationOriginal.runId='other';
 // Isolated corruption injection only, not a production migration change.
 f.sql.run('DROP TRIGGER solo_augment_receipt_no_update');
 f.sql.run('UPDATE solo_augment_receipts SET response_json=?',[JSON.stringify(saved)]);
 await expect(f.grants.execute({accountId:'a',environment:'DEV'},f.runId,f.request.eventId,'select',{operation:'SELECT',command:{eventId:f.request.eventId,expectedRevision:0,slotIndex:0,candidateId:'O22'}})).rejects.toThrow('o22_original_storage_invalid');
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
});
