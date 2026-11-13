import { canonicalJson } from '@lastrail/common';
import { z } from 'zod';
import { equipmentEffectStateReaderSchema, equipmentUniqueEffectSchema } from './equipment-effect-snapshot';

type Effect = z.infer<typeof equipmentUniqueEffectSchema>;
type State = z.infer<typeof equipmentEffectStateReaderSchema>;
type Requirement = Pick<Effect, 'effectId' | 'grade' | 'definitionVersion'> & {
  /** An unchanged grade already has a fixed value. A merged grade needs its approved definition row. */
  knownDefinitionSnapshot: Effect['definitionSnapshot'] | null;
};

function readPair(primary: unknown, secondary: unknown): [State, State] {
  let a: State, b: State;
  try {
    // Detach nested resolved JSON from callers. Domain preparation never mutates a live snapshot.
    a = equipmentEffectStateReaderSchema.parse(JSON.parse(canonicalJson(equipmentEffectStateReaderSchema.parse(primary))));
    b = equipmentEffectStateReaderSchema.parse(JSON.parse(canonicalJson(equipmentEffectStateReaderSchema.parse(secondary))));
  } catch { throw new Error('equipment_merge_effect_snapshot_invalid'); }
  if (a.equipment.star !== b.equipment.star) throw new Error('equipment_merge_effect_pair_invalid');
  if (a.equipment.star === 5) throw new Error('equipment_merge_max_star');
  return [a, b];
}

function planPair(a: State, b: State) {
  const requirements = new Map<string, Requirement>();
  for (const effect of a.uniqueEffects) {
    requirements.set(effect.effectId, { effectId: effect.effectId, grade: effect.grade,
      definitionVersion: effect.definitionVersion, knownDefinitionSnapshot: effect.definitionSnapshot });
  }
  for (const effect of b.uniqueEffects) {
    const prior = requirements.get(effect.effectId);
    if (!prior) {
      requirements.set(effect.effectId, { effectId: effect.effectId, grade: effect.grade,
        definitionVersion: effect.definitionVersion, knownDefinitionSnapshot: effect.definitionSnapshot });
      continue;
    }
    if (prior.definitionVersion !== effect.definitionVersion
      || (prior.grade === effect.grade && canonicalJson(prior.knownDefinitionSnapshot) !== canonicalJson(effect.definitionSnapshot))) {
      // The same ID does not establish compatibility across definition versions.
      throw new Error('equipment_merge_effect_definition_conflict');
    }
    prior.grade += effect.grade;
    prior.knownDefinitionSnapshot = null;
  }
  return {
    version: 'equipment.merge-effects.v1' as const,
    equipment: { ...a.equipment, star: a.equipment.star + 1 },
    fixedEffects: [...requirements.values()].sort((x, y) => x.effectId.localeCompare(y.effectId)),
    randomGrant: a.equipment.star === 2 ? { count: 1 as const, grade: 1 as const } : null,
    inheritedEnchantment: a.enchantment,
  };
}

/** J02/J03: deterministic preview only, with no selection, storage or material consumption. */
export function planEquipmentMergeEffects(primary: unknown, secondary: unknown) {
  const [a, b] = readPair(primary, secondary);
  return planPair(a, b);
}

/** Validate source-resolved grade rows or the one confirmed 2-to-3 random grant.
 * Caller must prove catalog support/eligibility, pinned source versions and safe maintenance,
 * then commit this snapshot with both material revisions/consumption/history/receipt in one batch.
 * No DB writer or production definition/RNG adapter is installed by this pure domain module. */
export function resolveEquipmentMergeEffects(primary: unknown, secondary: unknown, resolved: unknown): State {
  const [a, b] = readPair(primary, secondary), plan = planPair(a, b);
  const parsed = z.array(equipmentUniqueEffectSchema).max(4).safeParse(resolved);
  if (!parsed.success) throw new Error('equipment_merge_resolved_effects_invalid');
  const effects = parsed.data;
  if (new Set(effects.map(effect => effect.effectId)).size !== effects.length) {
    throw new Error('equipment_merge_resolved_effects_invalid');
  }
  if (plan.randomGrant) {
    if (effects.length !== 1 || effects[0]?.grade !== 1) throw new Error('equipment_merge_resolved_effects_invalid');
  } else {
    if (effects.length !== plan.fixedEffects.length) throw new Error('equipment_merge_resolved_effects_invalid');
    for (const required of plan.fixedEffects) {
      const effect = effects.find(value => value.effectId === required.effectId);
      if (!effect || effect.grade !== required.grade || effect.definitionVersion !== required.definitionVersion
        || (required.knownDefinitionSnapshot !== null
          && canonicalJson(effect.definitionSnapshot) !== canonicalJson(required.knownDefinitionSnapshot))) {
        throw new Error('equipment_merge_resolved_effects_invalid');
      }
    }
  }
  // Schema enforces total grades 0/0/1/2/4 and primary procedure-star preservation.
  return equipmentEffectStateReaderSchema.parse(JSON.parse(canonicalJson({ version: a.version,
    equipment: plan.equipment, uniqueEffects: [...effects].sort((x, y) => x.effectId.localeCompare(y.effectId)),
    enchantment: plan.inheritedEnchantment })));
}
