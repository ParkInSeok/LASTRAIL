import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { equipmentEffectOriginalReaderSchema } from './equipment-effect-snapshot';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import type { EquipmentEffectOriginalSource } from './equipment-effect-original';

type StockIdentity = { stockId: string; definitionId: string; definitionVersion: string; star: number };
const entries = z.array(z.strictObject({ stockId: z.string().regex(/^[\x21-\x7e]{1,128}$/u),
  snapshot: equipmentEffectOriginalReaderSchema })).max(3);

/** Capture original producer results before stock publication. Low tiers have zero unique grades;
 * three-star needs an explicitly resolved grade-I result, never a default ID or a late draw. */
export function captureMerchantEffects(stock: StockIdentity[], supplied: unknown) {
  const parsed = entries.safeParse(supplied === undefined ? [] : supplied);
  if (!parsed.success || new Set(parsed.data.map(item => item.stockId)).size !== parsed.data.length
    || parsed.data.some(item => !stock.some(value => value.stockId === item.stockId))) {
    throw new ActivityError(409, 'equipment_merchant_effect_invalid');
  }
  return stock.map(item => {
    const provided = parsed.data.find(value => value.stockId === item.stockId);
    if (!provided && item.star >= 3) throw new ActivityError(409, 'equipment_merchant_effect_required');
    const snapshot = provided?.snapshot ?? equipmentEffectOriginalReaderSchema.parse({ version: 'equipment.effect-original.v1',
      equipment: { definitionId: item.definitionId, definitionVersion: item.definitionVersion, star: item.star },
      uniqueEffects: [], enchantment: null });
    if (snapshot.equipment.definitionId !== item.definitionId || snapshot.equipment.definitionVersion !== item.definitionVersion
      || snapshot.equipment.star !== item.star || snapshot.enchantment !== null) {
      throw new ActivityError(409, 'equipment_merchant_effect_invalid');
    }
    return { stockId: item.stockId, snapshot };
  });
}

export function merchantEffectHash(runId: string, offerId: string, stockId: string, sourceVersion: string,
  snapshot: z.infer<typeof equipmentEffectOriginalReaderSchema>) {
  return sha256Hex(canonicalJson({ runId, offerId, stockId, sourceVersion, snapshot }));
}

/** Real DB adapter: reads fixed original stock effects, proves that exact stock was purchased
 * into the caller's instance in this batch. No RNG, live definition lookup or synthetic support flag. */
export class EquipmentMerchantEffectSource implements EquipmentEffectOriginalSource {
  constructor(private readonly db: D1Database) {}

  async prepare(context: Parameters<EquipmentEffectOriginalSource['prepare']>[0]) {
    await new EquipmentInventoryStore(this.db).readPinnedCapacity(context.accountId, context.runId);
    const row = await this.db.prepare(`SELECT fx.offer_id,fx.snapshot_json,fx.payload_hash,st.stock_json,
      o.source_version,o.visit_depth,o.payload_hash AS offer_hash FROM run_equipment_merchant_effects fx
      JOIN run_equipment_merchant_stock st ON st.stock_id=fx.stock_id AND st.run_id=fx.run_id AND st.offer_id=fx.offer_id
      JOIN run_equipment_merchant_offers o ON o.run_id=fx.run_id AND o.offer_id=fx.offer_id
      WHERE fx.run_id=? AND fx.stock_id=?`).bind(context.runId, context.sourceItemId)
      .first<{ offer_id: string; snapshot_json: string; payload_hash: string; stock_json: string;
        source_version: string; visit_depth: number; offer_hash: string }>();
    if (!row) throw new ActivityError(409, 'equipment_merchant_effect_not_found');
    let snapshot: z.infer<typeof equipmentEffectOriginalReaderSchema>;
    try {
      snapshot = equipmentEffectOriginalReaderSchema.parse(JSON.parse(row.snapshot_json));
      const item = JSON.parse(row.stock_json) as StockIdentity;
      if (snapshot.enchantment !== null || snapshot.equipment.definitionId !== item.definitionId
        || snapshot.equipment.definitionVersion !== item.definitionVersion || snapshot.equipment.star !== item.star
        || await merchantEffectHash(context.runId, row.offer_id, context.sourceItemId, row.source_version, snapshot) !== row.payload_hash) {
        throw new Error('original stock effect binding');
      }
      const stockRows = (await this.db.prepare('SELECT stock_json FROM run_equipment_merchant_stock WHERE run_id=? AND offer_id=? ORDER BY sort_index')
        .bind(context.runId, row.offer_id).all<{ stock_json: string }>()).results;
      const hash = await sha256Hex(canonicalJson({ sourceVersion: row.source_version, visitDepth: row.visit_depth,
        stock: stockRows.map(value => JSON.parse(value.stock_json)) }));
      if (hash !== row.offer_hash) throw new Error('original offer checksum');
    } catch { throw new ActivityError(503, 'equipment_merchant_effect_invalid'); }
    return { snapshot, assertions: [this.db.prepare(`INSERT INTO equipment_effect_original_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_merchant_effects fx
        JOIN run_equipment_merchant_stock st ON st.run_id=fx.run_id AND st.stock_id=fx.stock_id AND st.offer_id=fx.offer_id
        JOIN run_equipment_merchant_offers o ON o.run_id=fx.run_id AND o.offer_id=fx.offer_id
        JOIN run_equipment_merchant_purchases p ON p.run_id=fx.run_id AND p.stock_id=fx.stock_id AND p.offer_id=fx.offer_id
        WHERE fx.run_id=? AND fx.stock_id=? AND fx.snapshot_json=? AND fx.payload_hash=?
          AND st.stock_json=? AND o.source_version=? AND o.payload_hash=?
          AND p.instance_id=? AND p.inventory_revision=?) THEN 1 ELSE 0 END`)
      .bind(context.runId, context.sourceItemId, row.snapshot_json, row.payload_hash, row.stock_json,
        row.source_version, row.offer_hash, context.instanceId, context.expectedInventoryRevision)] };
  }
}
