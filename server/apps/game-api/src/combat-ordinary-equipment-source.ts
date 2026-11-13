import { canonicalJson } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentEffectStateStore } from './equipment-effect-state';
import { captureCombatPolicyRequirements } from './combat-policy-catalog';

const fail = (code: string): never => { throw new ActivityError(409, `ordinary_stat_equipment_${code}`); };
const columns = ['instance_id', 'source_item_id', 'definition_id', 'definition_version', 'category_id', 'set_tag', 'star', 'locked', 'location_kind', 'wearer_kind', 'wearer_id', 'slot_index', 'revision'];
const inventoryJson = `SELECT json_group_array(json_object(${columns.map(c => `'${c}',${c}`).join(',')})) AS value FROM (SELECT * FROM run_equipment_instances WHERE run_id=? ORDER BY instance_id)`;
type Slot = { instanceId: string; instanceRevision: number; effectRevision: number; definitionVersion: string;
  categoryId: string; star: number; setTag: string; uniqueEffects: unknown[]; enchantment: unknown };

/** Read actual run-owned placement/effect histories. An empty slot is established by
 * a complete inventory read, never by missing effect history. Returned SQL proofs
 * must accompany the caller's authoritative stat application in the same batch.
 * This supplies equipment only, not HP/other modifier authority or START readiness. */
export async function readOrdinaryFiveEquipmentSource(db: D1Database, accountId: string, runId: string) {
  const store = new EquipmentInventoryStore(db);
  const inventory = await store.readInventory(accountId, runId);
  if (!inventory) return fail('inventory_missing');
  const original = await db.prepare(inventoryJson).bind(runId).first<{ value: string }>();
  if (!original || canonicalJson(JSON.parse(original.value)) !== canonicalJson(inventory.items)) return fail('inventory_changed');
  const actors = captureCombatPolicyRequirements().scope.roster.map(operatorId => ({ operatorId, slots: [null, null, null] as (Slot | null)[] }));
  const assertions = [db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_inventory i JOIN runs r ON r.id=i.run_id AND r.account_id=i.account_id WHERE i.run_id=? AND i.account_id=? AND i.revision=? AND r.status='ACTIVE') AND json((SELECT value FROM (${inventoryJson})))=json(?) THEN 1 ELSE 0 END`)
    .bind(runId, accountId, inventory.revision, runId, original.value)];
  const effects = new EquipmentEffectStateStore(db);
  for (const item of inventory.items) {
    if (item.location_kind !== 'EQUIPPED' || item.wearer_kind !== 'operator') continue;
    const actor = actors.find(a => a.operatorId === item.wearer_id);
    if (!actor) return fail('wearer_unsupported');
    const effect = await effects.read(accountId, runId, item.instance_id);
    if (!effect) return fail('effect_missing');
    if (effect.inventoryRevision !== inventory.revision || effect.instanceRevision !== item.revision) return fail('effect_changed');
    actor.slots[item.slot_index!] = { instanceId: item.instance_id, instanceRevision: item.revision,
      effectRevision: effect.effectRevision, definitionVersion: item.definition_version, categoryId: item.category_id,
      star: item.star, setTag: item.set_tag, uniqueEffects: structuredClone(effect.snapshot.uniqueEffects), enchantment: structuredClone(effect.snapshot.enchantment) };
    assertions.push(db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_effect_heads h JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision WHERE h.run_id=? AND h.instance_id=? AND h.revision=? AND json(l.snapshot_json)=json(?)) THEN 1 ELSE 0 END`)
      .bind(runId, item.instance_id, effect.effectRevision, canonicalJson(effect.snapshot)));
  }
  const current = await store.readInventory(accountId, runId);
  if (!current || current.revision !== inventory.revision || canonicalJson(current.items) !== canonicalJson(inventory.items)) return fail('inventory_changed');
  return { version: 'combat.ordinary-five-equipment-source.v1.20261023', accountId, runId, inventoryRevision: inventory.revision, actors, assertions };
}
