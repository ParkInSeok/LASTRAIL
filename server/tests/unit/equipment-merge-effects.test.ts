import { expect, it } from 'vitest';
import { planEquipmentMergeEffects, resolveEquipmentMergeEffects } from '../../apps/game-api/src/equipment-merge-effects';

const effect = (effectId: string, grade: number, definitionVersion = 'fixture-effects-v1') => ({
  effectId, grade, definitionVersion, definitionSnapshot: { fixtureOnly: true, resolvedGradeValue: grade * 100 } });
const enchantment = { effectId: 'EN08', appliedStar: 2, definitionVersion: 'fixture-enchant-v1',
  definitionSnapshot: { fixtureOnly: true, coefficient: 333 } };
const state = (star: number, uniqueEffects: ReturnType<typeof effect>[] = [], enchanted = true) => ({
  version: 'equipment.effect-state.v1', equipment: { definitionId: 'gear-a', definitionVersion: 'fixture-catalog-v1', star },
  uniqueEffects, enchantment: enchanted ? enchantment : null });

it('adds identical grades and resolves the next-grade definition rather than adding coefficient JSON', () => {
  const primary = state(3, [effect('EQ01', 1)]), secondary = state(3, [effect('EQ01', 1)]);
  const plan = planEquipmentMergeEffects(primary, secondary);
  expect(plan.fixedEffects).toMatchObject([{ effectId: 'EQ01', grade: 2, definitionVersion: 'fixture-effects-v1' }]);
  const next = effect('EQ01', 2);
  next.definitionSnapshot.resolvedGradeValue = 777; // Approved source value need not be the sum of two old coefficients.
  const result = resolveEquipmentMergeEffects(primary, secondary, [next]);
  expect(result).toMatchObject({ equipment: { star: 4 }, uniqueEffects: [next], enchantment });
  expect(primary.uniqueEffects[0].grade).toBe(1);
  expect(secondary.uniqueEffects[0].grade).toBe(1);
});

it('retains every different effect and sorts the result deterministically', () => {
  const a = state(4, [effect('EQ04', 1), effect('EQ01', 1)]);
  const b = state(4, [effect('EQ03', 1), effect('EQ02', 1)]);
  const resolved = [effect('EQ04', 1), effect('EQ02', 1), effect('EQ03', 1), effect('EQ01', 1)];
  const result = resolveEquipmentMergeEffects(a, b, resolved);
  expect(result.uniqueEffects.map(value => value.effectId)).toEqual(['EQ01', 'EQ02', 'EQ03', 'EQ04']);
  expect(result.equipment.star).toBe(5);
});

it('sums mixed overlapping grades without a per-effect grade-II cap', () => {
  const a = state(4, [effect('EQ01', 2)]), b = state(4, [effect('EQ01', 1), effect('EQ02', 1)]);
  expect(resolveEquipmentMergeEffects(a, b, [effect('EQ02', 1), effect('EQ01', 3)]).uniqueEffects)
    .toEqual([effect('EQ01', 3), effect('EQ02', 1)]);
});

it('describes 2-to-3 random grade-I grant without choosing a result in preview', () => {
  const a = state(2), b = state(2);
  const first = planEquipmentMergeEffects(a, b);
  expect(first).toMatchObject({ fixedEffects: [], randomGrant: { count: 1, grade: 1 } });
  expect(planEquipmentMergeEffects(a, b)).toEqual(first);
  expect(() => resolveEquipmentMergeEffects(a, b, [])).toThrow('equipment_merge_resolved_effects_invalid');
  expect(resolveEquipmentMergeEffects(a, b, [effect('EQ16', 1)]).uniqueEffects).toEqual([effect('EQ16', 1)]);
});

it('keeps low-tier effects empty and never accepts an unsolicited random grant', () => {
  const a = state(1, [], false), b = state(1, [], false);
  expect(planEquipmentMergeEffects(a, b).randomGrant).toBeNull();
  expect(resolveEquipmentMergeEffects(a, b, [])).toMatchObject({ equipment: { star: 2 }, uniqueEffects: [], enchantment: null });
  expect(() => resolveEquipmentMergeEffects(a, b, [effect('EQ01', 1)])).toThrow('equipment_merge_resolved_effects_invalid');
});

it('inherits only the primary procedure values without scaling them at the new star', () => {
  const a = state(3, [effect('EQ01', 1)]), b = state(3, [effect('EQ02', 1)]);
  b.enchantment = { ...enchantment, effectId: 'EN02', definitionSnapshot: { fixtureOnly: true, coefficient: 9000 } };
  expect(resolveEquipmentMergeEffects(a, b, [effect('EQ01', 1), effect('EQ02', 1)]).enchantment).toEqual(enchantment);
  a.enchantment = null;
  expect(resolveEquipmentMergeEffects(a, b, [effect('EQ01', 1), effect('EQ02', 1)]).enchantment).toBeNull();
});

it('returns independent snapshots so consumer mutation cannot change either material', () => {
  const a = state(3, [effect('EQ01', 1)]), b = state(3, [effect('EQ02', 1)]);
  const result = resolveEquipmentMergeEffects(a, b, [effect('EQ01', 1), effect('EQ02', 1)]);
  result.enchantment!.definitionSnapshot.coefficient = 0;
  result.uniqueEffects[0].definitionSnapshot.resolvedGradeValue = 0;
  expect(a.enchantment).toEqual(enchantment);
  expect(a.uniqueEffects[0]).toEqual(effect('EQ01', 1));
});

it('rejects mixed stars, max star and old/original snapshot formats instead of reinterpreting them', () => {
  const a = state(3, [effect('EQ01', 1)]);
  expect(() => planEquipmentMergeEffects(a, state(4, [effect('EQ01', 2)]))).toThrow('equipment_merge_effect_pair_invalid');
  expect(() => planEquipmentMergeEffects(state(5, [effect('EQ01', 4)]), state(5, [effect('EQ01', 4)])))
    .toThrow('equipment_merge_max_star');
  expect(() => planEquipmentMergeEffects({ ...a, version: 'equipment.effect-original.v1' }, a))
    .toThrow('equipment_merge_effect_snapshot_invalid');
});

it('rejects the same ID under incompatible definition versions or contradictory same-grade originals', () => {
  const a = state(3, [effect('EQ01', 1)]), b = state(3, [effect('EQ01', 1, 'different-version')]);
  expect(() => planEquipmentMergeEffects(a, b)).toThrow('equipment_merge_effect_definition_conflict');
  b.uniqueEffects = [effect('EQ01', 1)];
  b.uniqueEffects[0].definitionSnapshot.resolvedGradeValue = 999;
  expect(() => planEquipmentMergeEffects(a, b)).toThrow('equipment_merge_effect_definition_conflict');
});

it.each([
  [], [effect('EQ01', 1)], [effect('EQ01', 2, 'changed-version')], [effect('EQ02', 2)],
  [effect('EQ01', 2), effect('EQ02', 1)], [effect('EQ01', 1), effect('EQ01', 1)], [effect('EQ17', 2)],
])('rejects missing, changed or unsupported resolved effects %#', resolved => {
  const a = state(3, [effect('EQ01', 1)]);
  expect(() => resolveEquipmentMergeEffects(a, a, resolved)).toThrow('equipment_merge_resolved_effects_invalid');
});

it('does not replace a known unchanged effect with newly invented source values', () => {
  const a = state(3, [effect('EQ01', 1)]), b = state(3, [effect('EQ02', 1)]);
  const altered = effect('EQ01', 1);
  altered.definitionSnapshot.resolvedGradeValue = 700;
  expect(() => resolveEquipmentMergeEffects(a, b, [altered, effect('EQ02', 1)]))
    .toThrow('equipment_merge_resolved_effects_invalid');
});
