import { expect, it } from 'vitest';
import { planEquipmentMerge, type EquipmentMergeMaterial } from '../../packages/common/src/equipment-merge-core';

const primary: EquipmentMergeMaterial = { instanceId: 'main', ownerId: 'a', categoryId: 'ATK',
  star: 4, setTag: 'SET01', locked: false, location: null };
const secondary: EquipmentMergeMaterial = { ...primary, instanceId: 'aux', setTag: 'SET02' };
const opSlot = { kind: 'operator' as const, wearerId: 'op1', slotIndex: 0 };
const turretSlot = { kind: 'turret' as const, wearerId: 'turret1', slotIndex: 2 };

it.each([
  [null, null, 29], [opSlot, null, 29], [null, turretSlot, 30], [opSlot, turretSlot, 30],
])('keeps main placement and clears auxiliary placement at a full inventory', (mainLocation, auxLocation, storedAfter) => {
  const plan = planEquipmentMerge('a', { ...primary, location: mainLocation },
    { ...secondary, location: auxLocation }, { usedSlots: 30, capacitySlots: 30 });
  expect(plan).toMatchObject({ version: 'equipment.merge-core.v1', nextStar: 5, setTag: 'SET01',
    resultLocation: mainLocation, vacatedLocation: auxLocation, storedAfter, suppliesCost: 0 });
});

it('explicit primary reversal changes retained tag and result location without mutating materials', () => {
  const main = { ...primary, location: opSlot }, aux = { ...secondary };
  const first = planEquipmentMerge('a', main, aux, { usedSlots: 30, capacitySlots: 30 });
  const reverse = planEquipmentMerge('a', aux, main, { usedSlots: 30, capacitySlots: 30 });
  expect(first.resultLocation).toEqual(opSlot);
  expect(reverse).toMatchObject({ setTag: 'SET02', resultLocation: null, vacatedLocation: opSlot, storedAfter: 30 });
  first.resultLocation!.slotIndex = 2;
  expect(main.location.slotIndex).toBe(0);
  expect(primary.star).toBe(4);
});

it('allows each approved upgrade to five stars and frees one stored slot at the 54-slot boundary', () => {
  for (const star of [1, 2, 3, 4]) {
    expect(planEquipmentMerge('a', { ...primary, star }, { ...secondary, star },
      { usedSlots: 54, capacitySlots: 54 })).toMatchObject({ nextStar: star + 1, storedAfter: 53 });
  }
});

it('rejects identical, foreign, locked, mismatched and maximum-star materials', () => {
  const inventory = { usedSlots: 2, capacitySlots: 30 };
  for (const item of [primary, { ...secondary, ownerId: 'b' }, { ...secondary, locked: true },
    { ...secondary, categoryId: 'HP' }, { ...secondary, star: 3 }]) {
    expect(() => planEquipmentMerge('a', primary, item, inventory)).toThrow();
  }
  expect(() => planEquipmentMerge('a', { ...primary, locked: true }, secondary, inventory)).toThrow('equipment_merge_locked');
  expect(() => planEquipmentMerge('a', { ...primary, star: 5 }, { ...secondary, star: 5 }, inventory))
    .toThrow('equipment_merge_max_star');
});

it('rejects malformed placement, impossible inventory and duplicate physical slots', () => {
  for (const item of [{ ...secondary, star: 6 }, { ...secondary, setTag: 'SET07' },
    { ...secondary, location: { ...opSlot, slotIndex: 3 } }]) {
    expect(() => planEquipmentMerge('a', primary, item as EquipmentMergeMaterial,
      { usedSlots: 2, capacitySlots: 30 })).toThrow('equipment_merge_material_invalid');
  }
  expect(() => planEquipmentMerge('a', primary, secondary, { usedSlots: 1, capacitySlots: 30 }))
    .toThrow('equipment_merge_inventory_invalid');
  expect(() => planEquipmentMerge('a', primary, secondary, { usedSlots: 31, capacitySlots: 30 }))
    .toThrow('equipment_merge_inventory_invalid');
  expect(() => planEquipmentMerge('a', { ...primary, location: opSlot }, { ...secondary, location: opSlot },
    { usedSlots: 0, capacitySlots: 30 })).toThrow('equipment_merge_material_invalid');
});
