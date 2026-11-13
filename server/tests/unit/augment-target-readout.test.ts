import {expect,it} from 'vitest';
import {captureAugmentTargetReadout} from '../../apps/game-api/src/augment-target-readout';
import {augmentUtilityDefinitionVersion,captureAugmentUtilityDefinitions} from '../../apps/game-api/src/augment-utility-definition';
import type {SelectionSnapshot} from '../../apps/game-api/src/solo-augment-selection';
const snapshot:SelectionSnapshot={runId:'run',eventId:'grant',stageId:'stage',catalogVersion:augmentUtilityDefinitionVersion,grantRevision:2,state:'SELECTING',selectedCandidateId:null,slots:[{slotIndex:0,candidateId:'O22',freeRemaining:2,extraRemaining:1},{slotIndex:1,candidateId:'O23',freeRemaining:3,extraRemaining:1},{slotIndex:2,candidateId:'T16',freeRemaining:3,extraRemaining:1}]};
it('supplies exact original rules, separate scope, and unknown recipients without manufacturing names',async()=>{
 const before=structuredClone(snapshot),result=await captureAugmentTargetReadout(snapshot),catalog=await captureAugmentUtilityDefinitions();
 expect(result).toMatchObject({version:'augment.target-readout.v1.20261021',runId:'run',eventId:'grant',grantRevision:2,catalogVersion:catalog.version});
 expect(result.cards.map(c=>c.scope)).toEqual(['OPERATOR','OPERATOR','TRAIN']);
 for(const card of result.cards){expect(card.definition).toEqual(catalog.definitions.find(d=>d.id===card.candidateId));expect(card.targets).toBeNull();expect(card.targetResolution).toBe('SOURCE_NOT_SUPPLIED');}
 expect(result.cards[1]?.definition?.rules).toMatchObject({formationEligibility:'AT_LEAST_ONE_AUTHORITY_MAPPED_HEALER',requiresBasicAttackMuzzle:false,requiresBasicAttackForm:false});
 expect(snapshot).toEqual(before);
});
it('never uses the current catalog to explain an old run or unknown candidate',async()=>{
 for(const original of [{...snapshot,catalogVersion:'old'}, {...snapshot,slots:[{...snapshot.slots[0]!,candidateId:'O99'}]}]){
  const result=await captureAugmentTargetReadout(original);expect(result.cards.every(c=>c.definition===null&&c.scope===null)).toBe(true);
 }
});
it('binds replacement and confirmed readouts to the exact snapshot without declaring applied ownership',async()=>{
 const changed={...snapshot,grantRevision:3,slots:[{...snapshot.slots[0]!,candidateId:'O23'}]};
 const a=await captureAugmentTargetReadout(snapshot),b=await captureAugmentTargetReadout(changed);
 expect(a.cards[0]?.candidateId).toBe('O22');expect(b.cards[0]?.candidateId).toBe('O23');expect(b.grantRevision).toBe(3);
 const confirmed=await captureAugmentTargetReadout({...snapshot,state:'CONFIRMED',selectedCandidateId:'O22'});
 expect(confirmed.cards.map(c=>c.candidateId)).toEqual(['O22']);expect(confirmed).not.toHaveProperty('applied');
 a.cards[0]!.definition!.rules.damageToHealBps=9999;expect((await captureAugmentTargetReadout(snapshot)).cards[0]?.definition?.rules.damageToHealBps).toBe(1200);
});
