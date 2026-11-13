import type { EquipmentEffectOriginalSource } from '../../apps/game-api/src/equipment-effect-original';

/** Synthetic resolved definitions only; never install this as a production source. */
export function fixtureEquipmentEffectSource(db: D1Database): EquipmentEffectOriginalSource {
  return { prepare: async context => {
    const item = await db.prepare(`SELECT item_json AS bytes FROM run_equipment_reward_items WHERE run_id=? AND item_id=?
      UNION ALL SELECT stock_json AS bytes FROM run_equipment_merchant_stock WHERE run_id=? AND stock_id=?`)
      .bind(context.runId, context.sourceItemId, context.runId, context.sourceItemId).first<{ bytes: string }>();
    if (!item) throw new Error('fixture_effect_source_missing');
    const definition = JSON.parse(item.bytes) as { definitionId: string; definitionVersion: string; star: number };
    return { snapshot: { version: 'equipment.effect-original.v1', equipment: {
      definitionId: definition.definitionId, definitionVersion: definition.definitionVersion, star: definition.star },
      uniqueEffects: definition.star < 3 ? [] : [{ effectId: 'EQ01', grade: 2 ** (definition.star - 3),
        definitionVersion: 'fixture-effect-v1', definitionSnapshot: { fixtureOnly: true, coefficient: 123 } }], enchantment: null },
    assertions: [db.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] };
  } };
}
