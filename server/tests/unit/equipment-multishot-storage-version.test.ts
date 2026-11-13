import {expect,it} from 'vitest';
import * as snapshots from '../../apps/game-api/src/equipment-effect-snapshot';
import {resolveEquipmentMergeEffects} from '../../apps/game-api/src/equipment-merge-effects';
const enchantment={effectId:'EN17',appliedStar:1,definitionVersion:'equipment.multishot-catalog.v1.20261015',definitionSnapshot:{version:'multishot-enchant-v1.20261015',appliedStar:1,coefficientBps:1500,maxSecondaryTargets:2,radiusL:{numerator:1,denominator:2},attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'}};
const original={version:'equipment.effect-original.v2.20261015',equipment:{definitionId:'gear',definitionVersion:'gear-v1',star:1},uniqueEffects:[],enchantment};
it('new original reader accepts formal EN17 while the unchanged v1 consumer rejects it',()=>{
 expect(snapshots.equipmentEffectOriginalReaderSchema?.safeParse(original).success).toBe(true);
 expect(snapshots.equipmentEffectOriginalSchema.safeParse(original).success).toBe(false);
 expect(snapshots.equipmentEffectOriginalSchema.safeParse({...original,version:'equipment.effect-original.v1'}).success).toBe(false);
});
it('rejects catalog aliases and mismatched fixed procedure definitions',()=>{
 for(const bad of [{...enchantment,definitionVersion:'LOCAL_ALIAS'},{...enchantment,appliedStar:2},{...enchantment,definitionSnapshot:{...enchantment.definitionSnapshot,coefficientBps:2500}}]){
  expect(snapshots.equipmentEffectOriginalReaderSchema?.safeParse({...original,enchantment:bad}).success).toBe(false);
 }
});
it('new reader returns a legacy source unchanged and rejects a new EN ID disguised as v1',()=>{
 const legacy={...original,version:'equipment.effect-original.v1',enchantment:null};
 expect(snapshots.equipmentEffectOriginalReaderSchema?.parse(legacy)).toEqual(legacy);
 expect(snapshots.equipmentEffectOriginalReaderSchema?.safeParse({...original,version:legacy.version}).success).toBe(false);
});
it('merge preserves new primary envelope and original 1-star procedure after upgrading to 2',()=>{
 const primary={...original,version:'equipment.effect-state.v2.20261015'};
 const secondary={...primary,enchantment:null};
 const result=resolveEquipmentMergeEffects(primary,secondary,[]);
 expect(result).toEqual({...primary,equipment:{...primary.equipment,star:2}});
 expect(snapshots.equipmentEffectStateSchema.safeParse(result).success).toBe(false);
});
