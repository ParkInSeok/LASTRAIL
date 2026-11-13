import { calculateEquipmentMerchantPrice, canonicalJson, selectEquipmentMerchantStar, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { walletId } from './crystal-wallet';
import { prepareMerchantMap } from './equipment-merchant-map';
import { captureMerchantEffects, merchantEffectHash } from './equipment-merchant-effects';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const sourceStock = z.strictObject({
  stockId: id, definitionId: id, definitionVersion: id, categoryId: id,
  setTag: z.enum(['SET01', 'SET02', 'SET03', 'SET04', 'SET05', 'SET06']),
  star: z.number().int().min(1).max(3), roll: z.number().int().min(0).max(99),
});
const suppliedSource = z.strictObject({
  sourceVersion: id, visitDepth: z.number().int().min(1).max(11),
  stock: z.array(sourceStock).min(1).max(3),
});
const storedStock = sourceStock.extend({ basePrice: z.number().int().positive() });
type SourceStock = z.infer<typeof sourceStock>;
type Stock = z.infer<typeof storedStock>;
type Offer = { version: 'equipment.merchant-offer.v1'; offerId: string; nodeId: string;
  sourceRef: string; sourceVersion: string; visitDepth: number; stock: Stock[] };
type OfferRow = { offer_id: string; node_id: string; source_ref: string; source_version: string;
  visit_depth: number; payload_hash: string };

export interface EquipmentMerchantSourceVerifier {
  /** The real node/catalog producer must prove its fixed seed, eligible definitions and
   * current merchant node with same-batch assertions. No production adapter is installed. */
  prepare(context: { accountId: string; runId: string; nodeId: string; sourceRef: string }):
    Promise<{ sourceVersion: string; visitDepth: number; stock: SourceStock[];
      /** Fixed original grade rows from supported definitions; mandatory for three-star stock. */
      effects?: { stockId: string; snapshot: unknown }[]; assertions: D1PreparedStatement[] }>;
}

/** Original offer ledger only. It neither spends tactical supply nor grants equipment. */
export class EquipmentMerchantOfferStore {
  constructor(private readonly db: D1Database, private readonly source?: EquipmentMerchantSourceVerifier) {}

  private async owner(accountId: string, runId: string): Promise<void> {
    const pinned = await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId, runId);
    if (!pinned) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
  }

  private original(runId: string, nodeId: string): Promise<OfferRow | null> {
    return this.db.prepare('SELECT offer_id,node_id,source_ref,source_version,visit_depth,payload_hash FROM run_equipment_merchant_offers WHERE run_id=? AND node_id=?')
      .bind(runId, nodeId).first<OfferRow>();
  }

  async read(accountId: string, runId: string, offerId: string): Promise<Offer> {
    await this.owner(accountId, runId);
    const row = await this.db.prepare('SELECT offer_id,node_id,source_ref,source_version,visit_depth,payload_hash FROM run_equipment_merchant_offers WHERE run_id=? AND offer_id=?')
      .bind(runId, offerId).first<OfferRow>();
    if (!row) throw new ActivityError(409, 'equipment_merchant_offer_not_found');
    const rows = (await this.db.prepare('SELECT stock_json FROM run_equipment_merchant_stock WHERE run_id=? AND offer_id=? ORDER BY sort_index')
      .bind(runId, offerId).all<{ stock_json: string }>()).results;
    try {
      const stock = rows.map(r => storedStock.parse(JSON.parse(r.stock_json)));
      if (stock.length < 1 || stock.length > 3 || new Set(stock.map(item => item.stockId)).size !== stock.length) throw new Error('merchant stock count');
      for (const item of stock) {
        if (item.star !== selectEquipmentMerchantStar(row.visit_depth, item.roll)
          || item.basePrice !== calculateEquipmentMerchantPrice({ star: item.star, do03Rank: 0, x08Rank: 0, priorPaidPurchases: 0 }).price) {
          throw new Error('merchant stock policy');
        }
      }
      const hash = await sha256Hex(canonicalJson({ sourceVersion: row.source_version, visitDepth: row.visit_depth, stock }));
      if (hash !== row.payload_hash) throw new Error('merchant stock checksum');
      return { version: 'equipment.merchant-offer.v1', offerId, nodeId: row.node_id,
        sourceRef: row.source_ref, sourceVersion: row.source_version, visitDepth: row.visit_depth, stock };
    } catch { throw new ActivityError(503, 'equipment_merchant_storage_invalid'); }
  }

  async publish(accountId: string, runId: string, nodeId: string, sourceRef: string): Promise<Offer> {
    if (!id.safeParse(nodeId).success || !id.safeParse(sourceRef).success) throw new ActivityError(400, 'equipment_merchant_request_invalid');
    await this.owner(accountId, runId);
    const existing = await this.original(runId, nodeId);
    if (existing) {
      if (existing.source_ref !== sourceRef) throw new ActivityError(409, 'equipment_merchant_source_conflict');
      return this.read(accountId, runId, existing.offer_id);
    }
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    const { mapRow, arrival, visitDepth: mapDepth } = await prepareMerchantMap(this.db, accountId, runId, nodeId);
    if (!this.source) throw new ActivityError(409, 'equipment_merchant_source_required');
    const supplied = await this.source.prepare({ accountId, runId, nodeId, sourceRef });
    const parsed = suppliedSource.safeParse({ sourceVersion: supplied?.sourceVersion,
      visitDepth: supplied?.visitDepth, stock: supplied?.stock });
    if (!parsed.success || new Set(parsed.data.stock.map(item => item.stockId)).size !== parsed.data.stock.length
      || parsed.data.stock.some(item => item.star !== selectEquipmentMerchantStar(parsed.data.visitDepth, item.roll))) {
      throw new ActivityError(409, 'equipment_merchant_stock_invalid');
    }
    if (!Array.isArray(supplied.assertions) || supplied.assertions.length === 0) {
      throw new ActivityError(409, 'equipment_merchant_source_required');
    }
    const { sourceVersion, visitDepth } = parsed.data;
    if (visitDepth !== mapDepth) throw new ActivityError(409, 'equipment_merchant_depth_conflict');
    const stock = parsed.data.stock.map(item => ({ ...item,
      basePrice: calculateEquipmentMerchantPrice({ star: item.star, do03Rank: 0, x08Rank: 0, priorPaidPurchases: 0 }).price }));
    const offerId = walletId('equipment_merchant_offer');
    const effects = captureMerchantEffects(stock, supplied.effects);
    const effectStatements: D1PreparedStatement[] = [];
    for (const effect of effects) {
      effectStatements.push(this.db.prepare(`INSERT INTO run_equipment_merchant_effects(run_id,stock_id,offer_id,snapshot_json,payload_hash)
        VALUES(?,?,?,?,?)`).bind(runId, effect.stockId, offerId, canonicalJson(effect.snapshot),
        await merchantEffectHash(runId, offerId, effect.stockId, sourceVersion, effect.snapshot)));
    }
    const hash = await sha256Hex(canonicalJson({ sourceVersion, visitDepth, stock }));
    try {
      await this.db.batch([
        ...supplied.assertions,
        this.db.prepare(`INSERT INTO equipment_merchant_offer_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            JOIN operation_map_states m ON m.run_id=r.id
            JOIN operation_map_receipts ar ON ar.run_id=r.id AND ar.event_id=?
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND m.revision=? AND m.state_json=? AND m.plan_json=? AND m.start_snapshot_json=s.snapshot_json
              AND m.start_snapshot_json=? AND ar.response_json=?
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots
              AND NOT EXISTS(SELECT 1 FROM run_equipment_merchant_offers o WHERE o.run_id=r.id AND o.node_id=?))
            THEN 1 ELSE 0 END`).bind(arrival.event_id, runId, accountId, mapRow.revision,
              mapRow.state_json, mapRow.plan_json, mapRow.start_snapshot_json, arrival.response_json, nodeId),
        this.db.prepare(`INSERT INTO run_equipment_merchant_offers
          (offer_id,run_id,node_id,source_ref,source_version,visit_depth,payload_hash) VALUES(?,?,?,?,?,?,?)`)
          .bind(offerId, runId, nodeId, sourceRef, sourceVersion, visitDepth, hash),
        ...stock.map((item, index) => this.db.prepare(`INSERT INTO run_equipment_merchant_stock
          (stock_id,run_id,offer_id,sort_index,stock_json) VALUES(?,?,?,?,?)`)
          .bind(item.stockId, runId, offerId, index, canonicalJson(item))),
        ...effectStatements,
        this.db.prepare('DELETE FROM equipment_merchant_offer_write_guard'),
      ]);
    } catch (error) {
      const raced = await this.original(runId, nodeId);
      if (raced) {
        if (raced.source_ref !== sourceRef || raced.payload_hash !== hash) throw new ActivityError(409, 'equipment_merchant_source_conflict');
        return this.read(accountId, runId, raced.offer_id);
      }
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_merchant_state_conflict');
      throw error;
    }
    return this.read(accountId, runId, offerId);
  }
}
