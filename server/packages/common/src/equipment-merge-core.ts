import { z } from 'zod';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const locationSchema = z.strictObject({ kind: z.enum(['operator', 'turret']), wearerId: id,
  slotIndex: z.number().int().min(0).max(2) });
const materialSchema = z.strictObject({ instanceId: id, ownerId: id, categoryId: id,
  star: z.number().int().min(1).max(5),
  setTag: z.enum(['SET01', 'SET02', 'SET03', 'SET04', 'SET05', 'SET06']),
  locked: z.boolean(), location: locationSchema.nullable() });
const inventorySchema = z.strictObject({ usedSlots: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),
  capacitySlots: z.number().int().positive().max(Number.MAX_SAFE_INTEGER) });

export type EquipmentMergeMaterial = z.infer<typeof materialSchema>;

/** J02/D2/v18 core only. No effect merge, RNG, safety authorization or material consumption. */
export function planEquipmentMerge(ownerId: string, primary: EquipmentMergeMaterial,
  secondary: EquipmentMergeMaterial, inventory: z.infer<typeof inventorySchema>) {
  const main = materialSchema.safeParse(primary), aux = materialSchema.safeParse(secondary);
  if (!id.safeParse(ownerId).success || !main.success || !aux.success
    || main.data.instanceId === aux.data.instanceId
    || (main.data.location !== null && aux.data.location !== null
      && main.data.location.kind === aux.data.location.kind
      && main.data.location.wearerId === aux.data.location.wearerId
      && main.data.location.slotIndex === aux.data.location.slotIndex)) {
    throw new Error('equipment_merge_material_invalid');
  }
  const a = main.data, b = aux.data;
  if (a.ownerId !== ownerId || b.ownerId !== ownerId) throw new Error('equipment_merge_owner_invalid');
  if (a.locked || b.locked) throw new Error('equipment_merge_locked');
  if (a.categoryId !== b.categoryId || a.star !== b.star) throw new Error('equipment_merge_pair_invalid');
  if (a.star === 5) throw new Error('equipment_merge_max_star');
  const state = inventorySchema.safeParse(inventory);
  const storedMaterials = Number(a.location === null) + Number(b.location === null);
  if (!state.success || state.data.usedSlots > state.data.capacitySlots || state.data.usedSlots < storedMaterials) {
    throw new Error('equipment_merge_inventory_invalid');
  }
  // The result replaces the primary at its original location. Only a stored auxiliary frees a storage slot.
  return { version: 'equipment.merge-core.v1' as const, primaryInstanceId: a.instanceId,
    secondaryInstanceId: b.instanceId, nextStar: a.star + 1, setTag: a.setTag,
    resultLocation: a.location, vacatedLocation: b.location,
    storedAfter: state.data.usedSlots - Number(b.location === null), suppliesCost: 0 as const };
}
