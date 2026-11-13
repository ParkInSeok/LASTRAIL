// equipment.loadout.v1: account-owned instances and three real slots per wearer.
export type EquipmentSetTag = 'SET01' | 'SET02' | 'SET03' | 'SET04' | 'SET05' | 'SET06';

export interface EquipmentInstance {
  readonly instanceId: string;
  readonly ownerId: string;
  readonly definitionId: string;
  readonly setTag: EquipmentSetTag;
  readonly star: number;
}

export interface EquipmentWearer {
  readonly wearerId: string;
  readonly kind: 'operator' | 'turret';
  readonly slots: readonly (string | null)[];
}

export interface EquipmentSetEffect {
  readonly setTag: EquipmentSetTag;
  readonly count: number;
  readonly activeStages: readonly number[];
}

const TAGS = new Set<string>(['SET01', 'SET02', 'SET03', 'SET04', 'SET05', 'SET06']);
const idIsValid = (value: unknown): value is string => typeof value === 'string' && value.trim().length > 0;

export class EquipmentLoadout {
  private readonly instances: ReadonlyMap<string, EquipmentInstance>;
  private readonly wearers: ReadonlyMap<string, EquipmentWearer>;

  constructor(readonly ownerId: string, instances: readonly EquipmentInstance[], wearers: readonly EquipmentWearer[] = []) {
    if (!idIsValid(ownerId)) throw new Error('equipment_owner_invalid');
    const indexed = new Map<string, EquipmentInstance>();
    for (const instance of instances) {
      if (!idIsValid(instance.instanceId) || !idIsValid(instance.ownerId) || !idIsValid(instance.definitionId)
        || !TAGS.has(instance.setTag) || !Number.isInteger(instance.star) || instance.star < 1 || instance.star > 5
        || indexed.has(instance.instanceId)) throw new Error('equipment_instance_invalid');
      indexed.set(instance.instanceId, { ...instance });
    }
    this.instances = indexed;

    const occupied = new Set<string>();
    const assigned = new Map<string, EquipmentWearer>();
    for (const wearer of wearers) {
      if (!idIsValid(wearer.wearerId) || (wearer.kind !== 'operator' && wearer.kind !== 'turret')
        || !Array.isArray(wearer.slots) || wearer.slots.length !== 3 || assigned.has(wearer.wearerId)) {
        throw new Error('equipment_wearer_invalid');
      }
      for (const slot of wearer.slots) {
        if (slot === null) continue;
        const instance = indexed.get(slot);
        if (!instance || instance.ownerId !== ownerId || occupied.has(slot)) throw new Error('equipment_assignment_invalid');
        occupied.add(slot);
      }
      assigned.set(wearer.wearerId, { wearerId: wearer.wearerId, kind: wearer.kind, slots: [...wearer.slots] });
    }
    this.wearers = assigned;
  }

  replaceWearers(wearers: readonly EquipmentWearer[]): EquipmentLoadout {
    return new EquipmentLoadout(this.ownerId, [...this.instances.values()], wearers);
  }

  /** Only owned, unequipped physical instances occupy storage. Pending rewards are not instances here. */
  storedCount(): number {
    const equipped = new Set<string>();
    for (const wearer of this.wearers.values()) {
      for (const slot of wearer.slots) if (slot !== null) equipped.add(slot);
    }
    let count = 0;
    for (const instance of this.instances.values()) {
      if (instance.ownerId === this.ownerId && !equipped.has(instance.instanceId)) count += 1;
    }
    return count;
  }

  effectsFor(wearerId: string): EquipmentSetEffect[] {
    const wearer = this.wearers.get(wearerId);
    if (!wearer) return [];
    const counts = new Map<EquipmentSetTag, number>();
    for (const id of wearer.slots) {
      if (id === null) continue;
      const tag = this.instances.get(id)!.setTag;
      counts.set(tag, (counts.get(tag) ?? 0) + 1);
    }
    return [...counts].sort(([a], [b]) => a.localeCompare(b)).map(([setTag, count]) => ({
      setTag, count, activeStages: Array.from({ length: count }, (_, i) => i + 1),
    }));
  }

  // The caller owns the commander rank. Ineligibility contributes zero and never blocks a save or START.
  oc01ReductionFor(wearerId: string, oc01Rank: number): number {
    if (oc01Rank !== 1 || this.wearers.get(wearerId)?.kind !== 'operator') return 0;
    return this.effectsFor(wearerId).length === 3 ? 1000 : 0;
  }
}

/** Pin slots once at START. A live DO05/preset edit must not recalculate an existing run. */
export class EquipmentInventoryCapacity {
  readonly version = 'equipment.inventory-capacity.v18' as const;

  private constructor(readonly slots: number) {}

  static forNewStart(do05Rank: number): EquipmentInventoryCapacity {
    if (!Number.isInteger(do05Rank) || do05Rank < 0 || do05Rank > 4) {
      throw new Error('equipment_capacity_rank_invalid');
    }
    return new EquipmentInventoryCapacity(30 + 6 * do05Rank);
  }

  /** Missing means a historical run with no v18 capacity; never silently assign it 30 slots. */
  static fromRunSnapshot(snapshot: unknown): EquipmentInventoryCapacity | null {
    if (typeof snapshot !== 'object' || snapshot === null || Array.isArray(snapshot)) {
      throw new Error('equipment_capacity_snapshot_invalid');
    }
    if (!Object.hasOwn(snapshot, 'equipmentInventoryCapacity')) return null;
    const raw = (snapshot as Record<string, unknown>).equipmentInventoryCapacity;
    if (typeof raw !== 'object' || raw === null || Array.isArray(raw)
      || Object.keys(raw).sort().join(',') !== 'do05Rank,slots,version') {
      throw new Error('equipment_capacity_snapshot_invalid');
    }
    const value = raw as Record<string, unknown>;
    if (value.version !== 'equipment.inventory-capacity.v18') {
      throw new Error('equipment_capacity_snapshot_invalid');
    }
    try {
      const capacity = EquipmentInventoryCapacity.forNewStart(value.do05Rank as number);
      if (capacity.slots !== value.slots) throw new Error();
      return capacity;
    } catch {
      throw new Error('equipment_capacity_snapshot_invalid');
    }
  }

  remaining(loadout: EquipmentLoadout): number {
    return Math.max(0, this.slots - loadout.storedCount());
  }

  /** Evaluate the authoritative final loadout in the same transaction as claim or purchase. */
  canStore(loadout: EquipmentLoadout, additionalInstances: number): boolean {
    if (!Number.isInteger(additionalInstances) || additionalInstances < 0) {
      throw new Error('equipment_capacity_count_invalid');
    }
    return loadout.storedCount() + additionalInstances <= this.slots;
  }
}
