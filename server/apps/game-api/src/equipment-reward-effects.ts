import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { equipmentEffectOriginalReaderSchema } from './equipment-effect-snapshot';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import type { EquipmentEffectOriginalSource } from './equipment-effect-original';

type Identity = { itemId: string; instanceId: string; definitionId: string; definitionVersion: string; star: number };
const entries = z.array(z.strictObject({ itemId: z.string().regex(/^[\x21-\x7e]{1,128}$/u), snapshot: equipmentEffectOriginalReaderSchema }));

/** The original reward producer owns eligible pools, resolved rows and all original draws.
 * Low tiers have zero unique grades; high tiers require an explicit original, never a late claim draw. */
export function captureRewardEffects(items: Identity[], supplied: unknown) {
  const parsed = entries.safeParse(supplied === undefined ? [] : supplied);
  if (!parsed.success || new Set(parsed.data.map(value => value.itemId)).size !== parsed.data.length
    || parsed.data.some(value => !items.some(item => item.itemId === value.itemId))) {
    throw new ActivityError(409, 'equipment_reward_effect_invalid');
  }
  return items.map(item => {
    const provided = parsed.data.find(value => value.itemId === item.itemId);
    if (!provided && item.star >= 3) throw new ActivityError(409, 'equipment_reward_effect_required');
    const snapshot = provided?.snapshot ?? equipmentEffectOriginalReaderSchema.parse({ version: 'equipment.effect-original.v1',
      equipment: { definitionId: item.definitionId, definitionVersion: item.definitionVersion, star: item.star },
      uniqueEffects: [], enchantment: null });
    if (snapshot.equipment.definitionId !== item.definitionId || snapshot.equipment.definitionVersion !== item.definitionVersion
      || snapshot.equipment.star !== item.star) throw new ActivityError(409, 'equipment_reward_effect_invalid');
    return { itemId: item.itemId, instanceId: item.instanceId, snapshot };
  });
}

export function rewardEffectHash(runId: string, bundleId: string, itemId: string, instanceId: string,
  sourceVersion: string, snapshot: z.infer<typeof equipmentEffectOriginalReaderSchema>) {
  return sha256Hex(canonicalJson({ runId, bundleId, itemId, instanceId, sourceVersion, snapshot }));
}

/** DB source for fixed original rewards. No current catalog resolution, RNG or item issuance on read. */
export class EquipmentRewardEffectSource implements EquipmentEffectOriginalSource {
  constructor(private readonly db: D1Database) {}

  async prepare(context: Parameters<EquipmentEffectOriginalSource['prepare']>[0]) {
    await new EquipmentInventoryStore(this.db).readPinnedCapacity(context.accountId, context.runId);
    const row = await this.db.prepare(`SELECT fx.bundle_id,fx.snapshot_json,fx.payload_hash,item.instance_id,item.item_json,
      b.source_version,b.payload_hash AS bundle_hash FROM run_equipment_reward_effects fx
      JOIN run_equipment_reward_items item ON item.item_id=fx.item_id AND item.run_id=fx.run_id AND item.bundle_id=fx.bundle_id
      JOIN run_equipment_reward_bundles b ON b.run_id=fx.run_id AND b.bundle_id=fx.bundle_id
      WHERE fx.run_id=? AND fx.item_id=?`).bind(context.runId, context.sourceItemId)
      .first<{ bundle_id: string; snapshot_json: string; payload_hash: string; instance_id: string; item_json: string;
        source_version: string; bundle_hash: string }>();
    if (!row) throw new ActivityError(409, 'equipment_reward_effect_not_found');
    if (row.instance_id !== context.instanceId) throw new ActivityError(409, 'equipment_reward_effect_instance_conflict');
    let snapshot: z.infer<typeof equipmentEffectOriginalReaderSchema>;
    try {
      snapshot = equipmentEffectOriginalReaderSchema.parse(JSON.parse(row.snapshot_json));
      const item = JSON.parse(row.item_json) as Identity;
      if (item.itemId !== context.sourceItemId || item.instanceId !== context.instanceId
        || snapshot.equipment.definitionId !== item.definitionId || snapshot.equipment.definitionVersion !== item.definitionVersion
        || snapshot.equipment.star !== item.star
        || await rewardEffectHash(context.runId, row.bundle_id, context.sourceItemId, context.instanceId, row.source_version, snapshot) !== row.payload_hash) {
        throw new Error('reward original effect binding');
      }
      const items = (await this.db.prepare('SELECT item_json FROM run_equipment_reward_items WHERE run_id=? AND bundle_id=? ORDER BY sort_index')
        .bind(context.runId, row.bundle_id).all<{ item_json: string }>()).results.map(value => JSON.parse(value.item_json));
      if (await sha256Hex(canonicalJson({ sourceVersion: row.source_version, items })) !== row.bundle_hash) throw new Error('bundle checksum');
    } catch { throw new ActivityError(503, 'equipment_reward_effect_invalid'); }
    return { snapshot, assertions: [this.db.prepare(`INSERT INTO equipment_effect_original_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_reward_effects fx
        JOIN run_equipment_reward_items item ON item.run_id=fx.run_id AND item.item_id=fx.item_id AND item.bundle_id=fx.bundle_id
        JOIN run_equipment_reward_bundles b ON b.run_id=fx.run_id AND b.bundle_id=fx.bundle_id
        WHERE fx.run_id=? AND fx.item_id=? AND fx.snapshot_json=? AND fx.payload_hash=?
          AND item.instance_id=? AND item.item_json=? AND item.status='CLAIMED'
          AND b.source_version=? AND b.payload_hash=?) THEN 1 ELSE 0 END`)
      .bind(context.runId, context.sourceItemId, row.snapshot_json, row.payload_hash,
        context.instanceId, row.item_json, row.source_version, row.bundle_hash)] };
  }
}
