import {it,expect,afterEach} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import {captureUtilitySelectionDefinition,readPinnedUtilitySelectionDefinition} from '../../apps/game-api/src/augment-utility-source';
const cleanups:(()=>void)[]=[];afterEach(()=>{for(const close of cleanups.splice(0))close();});
async function fixture(confirmed=true,catalog=augmentUtilityDefinitionVersion,ids=['O22','O23','T05']){
 const f=await setup(true);cleanups.push(()=>f.sql.close());
 const preview=await(await f.preview()).json(),run=await(await f.start(preview.operationPreviewId)).json();
 const runId=run.runId as string,eventId='utility-fixture-grant',store=new SoloAugmentStore(f.env.DB,()=>0);
 const first=await store.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId,eventId,stageId:'stage',catalogVersion:catalog,eligibleIds:ids});
 if(confirmed)await store.execute({accountId:'a',environment:'DEV'},runId,eventId,'choose',{operation:'SELECT',command:{eventId,expectedRevision:0,slotIndex:0,candidateId:first.slots[0]!.candidateId}});
 return {...f,runId,eventId,request:{runId,eventId,environment:'DEV',expectedStoreRevision:confirmed?2:1,expectedCatalogVersion:catalog,expectedStartSnapshotHash:await sha256Hex(String(f.scalar('SELECT snapshot_json FROM run_snapshots')))}};
}
it('binds the selected definition to the actual grant and original START without inventing applied ownership',async()=>{
 const f=await fixture(),before=Buffer.from(f.sql.export());
 const source=await captureUtilitySelectionDefinition(f.env.DB,'a',f.request);
 expect(source.selectedDefinition).toMatchObject({id:'O22',rules:{damageToHealBps:1200}});
 expect(source.selectionSource.selection.state).toBe('CONFIRMED');
 expect(source.startSnapshotHash).toBe(f.request.expectedStartSnapshotHash);
 expect(source.candidateDefinitions).toEqual([]);
 expect(source).toMatchObject({resistance:{approval:'31#augment-resistance-approved-20261019',runtimeSupported:false,
  control:{maximumSuccessfulAugmentControlsPerEnemyAction:1,ordering:'AUTHORITY_SEQUENCE'},
  healing:{input:'AFTER_ORIGINAL_HEAL_MODIFIERS_ONCE',rejectedAmountIsExcess:false}}});
 expect(Object.keys(source.definitionCatalog).sort()).toEqual(['definitionHash','version']);
 expect(source).toMatchObject({effectOwnershipEstablished:false,applied:false,runtimeSupported:false,canStart:false});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 expect(f.scalar('SELECT count(*) FROM run_augment_history')).toBe(0);
});
it.each([['O23'],['O23','T05']])('persists sparse candidates and keeps confirmed recovery pinned for %j',async(...ids)=>{
 const f=await fixture(true,augmentUtilityDefinitionVersion,ids);
 const packet=await captureUtilitySelectionDefinition(f.env.DB,'a',f.request);
 expect(packet.selectionSource.selection.slots.map(s=>s.candidateId)).toEqual(ids);
 expect(packet.selectedDefinition?.id).toBe('O23');
 expect(packet.eligibility).toMatchObject({runtimeIntegrated:false,sourceFailureIsZero:false});
 const store=new SoloAugmentStore(f.env.DB,()=>{throw Error('must not redraw');});
 const replay=await store.execute({accountId:'a',environment:'DEV'},f.runId,f.eventId,'choose',
  {operation:'SELECT',command:{eventId:f.eventId,expectedRevision:0,slotIndex:0,candidateId:'O23'}});
 expect(replay.snapshot).toEqual(packet.selectionSource.selection);
 expect(await readPinnedUtilitySelectionDefinition(packet,packet,packet.packetHash,
  {accountId:'a',environment:'DEV',runId:f.runId,eventId:f.eventId})).toEqual(packet);
});
it('keeps SELECTING unbound and rejects foreign catalog or unknown definition',async()=>{
 const f=await fixture(false);expect((await captureUtilitySelectionDefinition(f.env.DB,'a',f.request)).selectedDefinition).toBeNull();
 const old=await fixture(true,'legacy');await expect(captureUtilitySelectionDefinition(old.env.DB,'a',old.request)).rejects.toThrow('utility_selection_catalog_unsupported');
 const unknown=await fixture(true,augmentUtilityDefinitionVersion,['O99','O23','T05']);await expect(captureUtilitySelectionDefinition(unknown.env.DB,'a',unknown.request)).rejects.toThrow('utility_selection_definition_missing');
});
it('restores only the independently pinned original after current grant changes and detaches data',async()=>{
 const f=await fixture(),original=await captureUtilitySelectionDefinition(f.env.DB,'a',f.request);
 const trust={accountId:'a',runId:f.runId,eventId:f.eventId,environment:'DEV'};
 f.sql.run("UPDATE solo_augment_grants SET checkpoint_json='{}',write_revision=write_revision+1");
 const restored=await readPinnedUtilitySelectionDefinition(original,original,original.packetHash,trust);
 expect(restored).toEqual(original);restored.selectedDefinition!.rules.damageToHealBps=9999;
 expect(original.selectedDefinition!.rules.damageToHealBps).toBe(1200);
 const changed=structuredClone(original);changed.selectedDefinition!.rules.damageToHealBps=9999;
 const {packetHash,...body}=changed;void packetHash;changed.packetHash=await sha256Hex(canonicalJson(body));
 await expect(readPinnedUtilitySelectionDefinition(changed,original,original.packetHash,trust)).rejects.toThrow('utility_selection_original_conflict');
 await expect(readPinnedUtilitySelectionDefinition(original,original,original.packetHash,{...trust,accountId:'b'})).rejects.toThrow();
});
it('preserves authority, original snapshot, and grant version rejection from the existing source',async()=>{
 const f=await fixture();
 await expect(captureUtilitySelectionDefinition(f.env.DB,'b',f.request)).rejects.toThrow('forbidden');
 await expect(captureUtilitySelectionDefinition(f.env.DB,'a',{...f.request,expectedStoreRevision:1})).rejects.toThrow('revision_conflict');
 f.sql.run("UPDATE run_snapshots SET snapshot_json='{}'");
 await expect(captureUtilitySelectionDefinition(f.env.DB,'a',f.request)).rejects.toThrow('start_conflict');
});
