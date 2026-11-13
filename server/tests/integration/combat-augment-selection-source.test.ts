import {it,expect,afterEach} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {captureCombatAuthority} from '../../apps/game-api/src/combat-authority';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {captureAugmentSelectionSource,readPinnedAugmentSelectionSource} from '../../apps/game-api/src/combat-augment-selection-source';
const account='a',environment='DEV';
const cleanups:(()=>void)[]=[];afterEach(()=>{for(const close of cleanups.splice(0))close();});
async function fixture(confirmed=true){
 const f=await setup(true);cleanups.push(()=>f.sql.close());
 const preview=await(await f.preview()).json(),run=await(await f.start(preview.operationPreviewId)).json();
 const runId=run.runId as string,eventId='grant',store=new SoloAugmentStore(f.env.DB,()=>0);
 const first=await store.issue({accountId:account,selectionOwner:account,environment,runId,eventId,stageId:'stage',catalogVersion:'LOCAL_fixture_catalog',eligibleIds:['LOCAL_card_A','LOCAL_card_B','LOCAL_card_C','LOCAL_card_D']});
 if(confirmed)await store.execute({accountId:account,environment},runId,eventId,'choose',{operation:'SELECT',command:{eventId,expectedRevision:0,slotIndex:0,candidateId:first.slots[0]!.candidateId}});
 const request={runId,eventId,environment,expectedStoreRevision:confirmed?2:1,expectedCatalogVersion:'LOCAL_fixture_catalog',expectedStartSnapshotHash:await sha256Hex(String(f.scalar('SELECT snapshot_json FROM run_snapshots')))};
 return {...f,runId,eventId,store,request};
}
it('reads actual confirmed grant/START source without writes or asserting effect ownership',async()=>{
 const f=await fixture(),before=Buffer.from(f.sql.export());
 const source=await captureAugmentSelectionSource(f.env.DB,account,f.request);
 expect(source.selection.selectedCandidateId).toBe('LOCAL_card_A');
 expect(source.catalogVersion).toBe('LOCAL_fixture_catalog');
 expect(source.storeRevision).toBe(2);expect(source.selection.grantRevision).toBe(1);
 expect(source.effectBinding).toBeNull();expect(source.effectOwnershipEstablished).toBe(false);
 expect(source.runtimeSupported).toBe(false);expect(source.canStart).toBe(false);
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 expect(f.scalar('SELECT count(*) FROM run_augment_history')).toBe(0);
});
it('keeps unconfirmed selection explicitly unconfirmed',async()=>{
 const f=await fixture(false),source=await captureAugmentSelectionSource(f.env.DB,account,f.request);
 expect(source.selection.state).toBe('SELECTING');expect(source.selection.selectedCandidateId).toBeNull();
});
it.each(['owner','environment','revision','catalog','start','missing','inactive','mode','issue','checkpoint'])('rejects %s mismatch instead of providing empty ownership',async kind=>{
 const f=await fixture(),req={...f.request};let owner=account;
 if(kind==='owner')owner='b';if(kind==='environment')req.environment='PRD';
 if(kind==='revision')req.expectedStoreRevision=1;if(kind==='catalog')req.expectedCatalogVersion='next';
 if(kind==='start')f.sql.run("UPDATE run_snapshots SET snapshot_json='{}'");
 if(kind==='missing')req.eventId='absent';
 if(kind==='inactive')f.sql.run("UPDATE accounts SET status='BANNED' WHERE id='a'");
 if(kind==='mode'){
  const start=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  start.admission.combatAuthority=captureCombatAuthority('ORDINARY_COOP');const encoded=JSON.stringify(start);
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[encoded]);f.sql.run('UPDATE solo_augment_grants SET start_snapshot_json=?',[encoded]);
  req.expectedStartSnapshotHash=await sha256Hex(encoded);
 }
 if(kind==='issue')f.sql.run("UPDATE solo_augment_grants SET issue_hash='bad'");
 if(kind==='checkpoint')f.sql.run("UPDATE solo_augment_grants SET checkpoint_json='{}'");
 const code:Record<string,string>={owner:'forbidden',environment:'forbidden',revision:'revision_conflict',catalog:'issue_conflict',start:'start_conflict',missing:'missing',inactive:'inactive',mode:'mode_unsupported',issue:'issue_conflict',checkpoint:'checkpoint_invalid'};
 await expect(captureAugmentSelectionSource(f.env.DB,owner,req)).rejects.toThrow('augment_selection_source_'+code[kind]);
});
it('detects grant revision changes across awaited hashing',async()=>{
 const f=await fixture();const prepare=f.env.DB.prepare.bind(f.env.DB);let reads=0;
 f.env.DB.prepare=((sql:string)=>{
  if(sql.includes('AS current_start_snapshot_json')&&++reads===2)f.sql.run('UPDATE solo_augment_grants SET write_revision=write_revision+1');
  return prepare(sql);
 }) as typeof f.env.DB.prepare;
 await expect(captureAugmentSelectionSource(f.env.DB,account,f.request)).rejects.toThrow('augment_selection_source_stale');
});
it('restores frozen original independently of current grant/definitions and rejects new self-rehashed selections',async()=>{
 const f=await fixture(),original=await captureAugmentSelectionSource(f.env.DB,account,f.request),wire=JSON.parse(JSON.stringify(original));
 f.sql.run("UPDATE solo_augment_grants SET checkpoint_json='{}',write_revision=write_revision+1");
 expect(await readPinnedAugmentSelectionSource(wire,original,original.packetHash,{accountId:account,runId:f.runId,eventId:f.eventId,environment})).toEqual(original);
 const forged=structuredClone(wire);forged.selection.selectedCandidateId='LOCAL_card_D';
 const {packetHash:_,...body}=forged;void _;forged.packetHash=await sha256Hex(canonicalJson(body));
 await expect(readPinnedAugmentSelectionSource(forged,original,original.packetHash,{accountId:account,runId:f.runId,eventId:f.eventId,environment})).rejects.toThrow('augment_selection_source_original_conflict');
});
it('rejects a DB stage identity conflicting with the original issuance',async()=>{
 const f=await fixture();f.sql.run("UPDATE solo_augment_grants SET stage_id='other-stage'");
 await expect(captureAugmentSelectionSource(f.env.DB,account,f.request)).rejects.toThrow('augment_selection_source_issue_conflict');
});
it('preserves the actual rerolled candidate, counters and final choice',async()=>{
 const f=await fixture(false),first=await f.store.read({accountId:account,environment},f.runId,f.eventId);
 const rolled=await f.store.execute({accountId:account,environment},f.runId,f.eventId,'roll',{operation:'REROLL',command:{eventId:f.eventId,expectedRevision:0,slotIndex:0,candidateId:first.slots[0]!.candidateId,mode:'FREE'}});
 await f.store.execute({accountId:account,environment},f.runId,f.eventId,'final',{operation:'SELECT',command:{eventId:f.eventId,expectedRevision:1,slotIndex:0,candidateId:rolled.snapshot.slots[0]!.candidateId}});
 const source=await captureAugmentSelectionSource(f.env.DB,account,{...f.request,expectedStoreRevision:3});
 expect(source.selection.selectedCandidateId).toBe('LOCAL_card_D');
 expect(source.selection.slots.map(x=>x.freeRemaining)).toEqual([2,3,3]);expect(source.selection.grantRevision).toBe(2);
});
it('binds frozen restore to every identity and detaches returned data',async()=>{
 const f=await fixture(),original=await captureAugmentSelectionSource(f.env.DB,account,f.request);
 const trust={accountId:account,runId:f.runId,eventId:f.eventId,environment};
 for(const field of ['accountId','runId','eventId','environment'] as const){
  await expect(readPinnedAugmentSelectionSource(original,original,original.packetHash,{...trust,[field]:'other'})).rejects.toThrow('augment_selection_source_original_conflict');
 }
 await expect(readPinnedAugmentSelectionSource(original,original,'0'.repeat(64),trust)).rejects.toThrow('augment_selection_source_original_conflict');
 const restored=await readPinnedAugmentSelectionSource(original,original,original.packetHash,trust);
 restored.selection.slots[0]!.freeRemaining=0;expect(original.selection.slots[0]!.freeRemaining).toBe(3);
});
