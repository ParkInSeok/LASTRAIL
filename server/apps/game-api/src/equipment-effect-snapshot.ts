import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { parseMultishotDefinition } from './equipment-multishot-definition';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
// Source-owned resolved JSON, retained without interpreting units or current-star scaling.
const definition = z.record(z.string(), z.json()).refine(value => Object.keys(value).length > 0);
export const equipmentEnchantmentSchema = z.strictObject({ effectId: z.string().regex(/^EN(0[1-9]|1[0-6])$/u),
  appliedStar: z.number().int().min(1).max(5), definitionVersion: id, definitionSnapshot: definition });
export const equipmentUniqueEffectSchema = z.strictObject({ effectId: z.string().regex(/^EQ(0[1-9]|1[0-6])$/u),
  grade: z.number().int().min(1).max(4), definitionVersion: id, definitionSnapshot: definition });
const content = z.strictObject({
  equipment: z.strictObject({ definitionId: id, definitionVersion: id, star: z.number().int().min(1).max(5) }),
  uniqueEffects: z.array(equipmentUniqueEffectSchema).max(4),
  enchantment: equipmentEnchantmentSchema.nullable(),
});
const valid = (value: z.infer<typeof content>) => {
  const expectedGrade = value.equipment.star < 3 ? 0 : 2 ** (value.equipment.star - 3);
  return value.uniqueEffects.reduce((total, effect) => total + effect.grade, 0) === expectedGrade
    && new Set(value.uniqueEffects.map(effect => effect.effectId)).size === value.uniqueEffects.length
    && (value.enchantment === null || value.enchantment.appliedStar <= value.equipment.star);
};
export const equipmentEffectOriginalSchema = content.extend({ version: z.literal('equipment.effect-original.v1') }).refine(valid);
export const equipmentEffectStateSchema = content.extend({ version: z.literal('equipment.effect-state.v1') }).refine(valid);
// Legacy schemas above intentionally remain strict EN01-16 readers.
export const multishotCatalogVersion = 'equipment.multishot-catalog.v1.20261015';
export const equipmentMultishotEnchantmentSchema = z.strictObject({effectId:z.literal('EN17'),
  appliedStar:z.number().int().min(1).max(5),definitionVersion:z.literal(multishotCatalogVersion),
  definitionSnapshot:definition}).refine(value=>{
    try {return parseMultishotDefinition(value.definitionSnapshot).appliedStar===value.appliedStar;}
    catch {return false;}
  },{message:'equipment_multishot_definition_invalid'});
export const equipmentEnchantmentReaderSchema = z.union([equipmentEnchantmentSchema,equipmentMultishotEnchantmentSchema]);
const newContent=content.extend({enchantment:equipmentEnchantmentReaderSchema.nullable()});
export const equipmentEffectOriginalV2Schema=newContent.extend({version:z.literal('equipment.effect-original.v2.20261015')}).refine(valid);
export const equipmentEffectStateV2Schema=newContent.extend({version:z.literal('equipment.effect-state.v2.20261015')}).refine(valid);
export const equipmentEffectOriginalReaderSchema=z.union([equipmentEffectOriginalSchema,equipmentEffectOriginalV2Schema]);
export const equipmentEffectStateReaderSchema=z.union([equipmentEffectStateSchema,equipmentEffectStateV2Schema]);
export type EquipmentEffectOriginalSnapshot = z.infer<typeof equipmentEffectOriginalReaderSchema>;
export function equipmentEffectOriginalHash(runId: string, instanceId: string, sourceItemId: string,
  snapshot: EquipmentEffectOriginalSnapshot) {
  return sha256Hex(canonicalJson({ runId, instanceId, sourceItemId, snapshot }));
}
