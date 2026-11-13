import { calculateEquipmentMerchantPrice } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentMerchantOfferStore } from './equipment-merchant-offers';
import { merchantRanksFromSnapshot } from './equipment-merchant-pricing';
import { RunSuppliesStore } from './run-supplies-start';

type SaleRow = { stock_id: string; price: number; supplies_revision: number };

/** Read-only owner projection; a displayed price does not authorize a purchase. */
export class EquipmentMerchantStatusStore {
  constructor(private readonly db: D1Database) {}

  async read(accountId: string, runId: string, offerId: string) {
    const offer = await new EquipmentMerchantOfferStore(this.db).read(accountId, runId, offerId);
    const [supplies, inventory, sales, snapshot] = await Promise.all([
      new RunSuppliesStore(this.db).read(accountId, runId),
      new EquipmentInventoryStore(this.db).readInventory(accountId, runId),
      this.db.prepare(`SELECT stock_id,price,supplies_revision FROM run_equipment_merchant_purchases
        WHERE run_id=? AND offer_id=? ORDER BY supplies_revision`).bind(runId, offerId).all<SaleRow>(),
      this.db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?').bind(runId).first<{ snapshot_json: string }>(),
    ]);
    if (!supplies) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    let ranks: ReturnType<typeof merchantRanksFromSnapshot>;
    try { ranks = merchantRanksFromSnapshot(snapshot!.snapshot_json); }
    catch { throw new ActivityError(503, 'equipment_merchant_price_policy_invalid'); }
    const paid = sales.results;
    if (paid.length > offer.stock.length) throw new ActivityError(503, 'equipment_merchant_storage_invalid');
    const sold = new Set<string>();
    for (const [index, sale] of paid.entries()) {
      const item = offer.stock.find(stock => stock.stockId === sale.stock_id);
      if (!item || sold.has(sale.stock_id) || !Number.isSafeInteger(sale.supplies_revision)
        || sale.supplies_revision > supplies.revision
        || sale.price !== calculateEquipmentMerchantPrice({ star: item.star, ...ranks, priorPaidPurchases: index }).price) {
        throw new ActivityError(503, 'equipment_merchant_storage_invalid');
      }
      sold.add(sale.stock_id);
    }
    const current = offer.stock.map(item => ({ stockId: item.stockId, definitionId: item.definitionId,
      definitionVersion: item.definitionVersion, categoryId: item.categoryId, setTag: item.setTag,
      star: item.star, basePrice: item.basePrice, sold: sold.has(item.stockId),
      currentPrice: sold.has(item.stockId) ? null : calculateEquipmentMerchantPrice({
        star: item.star, ...ranks, priorPaidPurchases: paid.length,
      }).price }));
    // A concurrent purchase increments both revisions. Never return a mixed quote/state.
    const [latestSupplies, latestInventory, run] = await Promise.all([
      new RunSuppliesStore(this.db).read(accountId, runId),
      new EquipmentInventoryStore(this.db).readInventory(accountId, runId),
      this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?')
        .bind(runId, accountId).first<{ status: string }>(),
    ]);
    if (!latestSupplies || !latestInventory || latestSupplies.revision !== supplies.revision
      || latestInventory.revision !== inventory.revision) throw new ActivityError(409, 'equipment_merchant_view_changed');
    return { version: 'equipment.merchant-status.v1' as const, offerId, nodeId: offer.nodeId,
      sourceVersion: offer.sourceVersion, visitDepth: offer.visitDepth, runStatus: run!.status,
      supplies: { balance: supplies.balance, revision: supplies.revision },
      inventory: { capacitySlots: inventory.capacitySlots, usedSlots: inventory.usedSlots, revision: inventory.revision },
      stock: current };
  }
}
