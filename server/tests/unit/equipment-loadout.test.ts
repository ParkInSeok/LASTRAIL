import { describe, expect, it } from 'vitest';
import { EquipmentInventoryCapacity, EquipmentLoadout } from '../../packages/common/src';

const instances = [
  { instanceId: 'a1', ownerId: 'p1', definitionId: 'gear-a', setTag: 'SET01', star: 5 },
  { instanceId: 'a2', ownerId: 'p1', definitionId: 'gear-a', setTag: 'SET01', star: 1 },
  { instanceId: 'b1', ownerId: 'p1', definitionId: 'gear-b', setTag: 'SET02', star: 2 },
  { instanceId: 'c1', ownerId: 'p1', definitionId: 'gear-c', setTag: 'SET03', star: 3 },
  { instanceId: 'other', ownerId: 'p2', definitionId: 'gear-a', setTag: 'SET01', star: 1 },
] as const;

const empty = [null, null, null] as const;
const roster = () => new EquipmentLoadout('p1', instances);

describe('equipment.loadout.v1', () => {
  it('counts actual items per wearer and activates cumulative stages without star multiplication', () => {
    const state = roster().replaceWearers([{ wearerId: 'op1', kind: 'operator', slots: ['a1', 'a2', 'b1'] }]);
    expect(state.effectsFor('op1')).toEqual([
      { setTag: 'SET01', count: 2, activeStages: [1, 2] },
      { setTag: 'SET02', count: 1, activeStages: [1] },
    ]);
    expect(state.effectsFor('op2')).toEqual([]);
    expect(state.oc01ReductionFor('op1', 1)).toBe(0);
  });

  it('applies OC01 only to an invested operator wearing three distinct real tags', () => {
    const state = roster().replaceWearers([
      { wearerId: 'op1', kind: 'operator', slots: ['a1', 'b1', 'c1'] },
      { wearerId: 'turret1', kind: 'turret', slots: empty },
    ]);
    expect(state.effectsFor('op1')).toEqual([
      { setTag: 'SET01', count: 1, activeStages: [1] },
      { setTag: 'SET02', count: 1, activeStages: [1] },
      { setTag: 'SET03', count: 1, activeStages: [1] },
    ]);
    expect(state.oc01ReductionFor('op1', 1)).toBe(1000);
    expect(state.oc01ReductionFor('op1', 0)).toBe(0);
    expect(state.oc01ReductionFor('turret1', 1)).toBe(0);
  });

  it('moves an instance atomically and removes the old wearer stage', () => {
    const first = roster().replaceWearers([
      { wearerId: 'op1', kind: 'operator', slots: ['a1', 'a2', null] },
      { wearerId: 'op2', kind: 'operator', slots: empty },
    ]);
    const moved = first.replaceWearers([
      { wearerId: 'op1', kind: 'operator', slots: ['a1', null, null] },
      { wearerId: 'op2', kind: 'operator', slots: ['a2', null, null] },
    ]);
    expect(first.effectsFor('op1')[0]?.activeStages).toEqual([1, 2]);
    expect(moved.effectsFor('op1')[0]?.activeStages).toEqual([1]);
    expect(moved.effectsFor('op2')[0]?.activeStages).toEqual([1]);
    expect(first.effectsFor('op2')).toEqual([]);
  });

  it('rejects duplicate, foreign, unknown or malformed instances and leaves original state intact', () => {
    const first = roster().replaceWearers([{ wearerId: 'op1', kind: 'operator', slots: ['a1', null, null] }]);
    for (const slots of [['a1', 'a1', null], ['other', null, null], ['missing', null, null]] as const) {
      expect(() => first.replaceWearers([{ wearerId: 'op1', kind: 'operator', slots }])).toThrow();
    }
    expect(first.effectsFor('op1')[0]?.count).toBe(1);
    expect(() => new EquipmentLoadout('p1', [{ ...instances[0], setTag: 'SET07' }])).toThrow();
    expect(() => new EquipmentLoadout('p1', [{ ...instances[0], star: 6 }])).toThrow();
  });
});

describe('equipment.inventory-capacity.v18', () => {
  const stored = (count: number, equipped = 0) => {
    const items = Array.from({ length: count + equipped }, (_, index) => ({
      instanceId: `item-${index}`, ownerId: 'p1', definitionId: 'gear-a', setTag: 'SET01' as const, star: 1,
    }));
    const slots = Array.from({ length: 3 }, (_, index) => index < equipped ? `item-${count + index}` : null);
    return new EquipmentLoadout('p1', items, [{ wearerId: 'op1', kind: 'operator', slots }]);
  };

  it('pins the approved START capacity to DO05 rank and counts only unequipped real instances', () => {
    expect(Array.from({ length: 5 }, (_, rank) => EquipmentInventoryCapacity.forNewStart(rank).slots))
      .toEqual([30, 36, 42, 48, 54]);
    expect(stored(29, 3).storedCount()).toBe(29);
    expect(EquipmentInventoryCapacity.forNewStart(0).remaining(stored(29, 3))).toBe(1);
    expect(EquipmentInventoryCapacity.forNewStart(4).remaining(stored(53, 1))).toBe(1);
    expect(() => EquipmentInventoryCapacity.forNewStart(-1)).toThrow('equipment_capacity_rank_invalid');
    expect(() => EquipmentInventoryCapacity.forNewStart(5)).toThrow('equipment_capacity_rank_invalid');
  });

  it('reads only an exact pinned v18 snapshot and leaves historical runs without v18 unclassified', () => {
    expect(EquipmentInventoryCapacity.fromRunSnapshot({})).toBeNull();
    expect(EquipmentInventoryCapacity.fromRunSnapshot({ equipmentInventoryCapacity: { version: 'equipment.inventory-capacity.v18', do05Rank: 4, slots: 54 } })?.slots).toBe(54);
    for (const equipmentInventoryCapacity of [null, { version: 'equipment.inventory-capacity.v18', do05Rank: 0, slots: 24 },
      { version: 'equipment.inventory-capacity.v18', do05Rank: 5, slots: 60 }, { version: 'unknown', do05Rank: 0, slots: 30 }]) {
      expect(() => EquipmentInventoryCapacity.fromRunSnapshot({ equipmentInventoryCapacity })).toThrow('equipment_capacity_snapshot_invalid');
    }
  });

  it('allows only the selected reward count that fits and never creates an extra slot', () => {
    const capacity = EquipmentInventoryCapacity.forNewStart(0);
    expect(capacity.canStore(stored(29), 1)).toBe(true);
    expect(capacity.canStore(stored(29), 3)).toBe(false);
    expect(capacity.canStore(stored(30), 1)).toBe(false);
    expect(capacity.canStore(stored(30), 0)).toBe(true);
    expect(EquipmentInventoryCapacity.forNewStart(4).canStore(stored(53), 1)).toBe(true);
    expect(EquipmentInventoryCapacity.forNewStart(4).canStore(stored(54), 1)).toBe(false);
    expect(() => capacity.canStore(stored(29), -1)).toThrow('equipment_capacity_count_invalid');
  });

  it('checks final placement for equip, swap and unequip without an intermediate slot', () => {
    const capacity = EquipmentInventoryCapacity.forNewStart(0);
    const full = stored(30, 1);
    expect(full.storedCount()).toBe(30);
    const equipped = new EquipmentLoadout('p1', Array.from({ length: 31 }, (_, index) => ({
      instanceId: `item-${index}`, ownerId: 'p1', definitionId: 'gear-a', setTag: 'SET01' as const, star: 1,
    })), [{ wearerId: 'op1', kind: 'operator', slots: ['item-0', 'item-30', null] }]);
    expect(capacity.canStore(equipped, 0)).toBe(true);
    expect(capacity.canStore(full.replaceWearers([{ wearerId: 'op1', kind: 'operator', slots: ['item-0', null, null] }]), 0)).toBe(true);
    expect(capacity.canStore(full.replaceWearers([{ wearerId: 'op1', kind: 'operator', slots: [null, null, null] }]), 0)).toBe(false);
    expect(capacity.canStore(stored(29, 1), 1)).toBe(true);
    expect(capacity.canStore(stored(30, 1), 1)).toBe(false);
  });
});
