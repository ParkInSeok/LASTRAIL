import { calculateEquipmentMerchantPrice, canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { walletId } from './crystal-wallet';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentMerchantOfferStore } from './equipment-merchant-offers';
import { merchantRanksFromSnapshot } from './equipment-merchant-pricing';
import { RunSuppliesStore } from './run-supplies-start';
import { prepareMerchantMap } from './equipment-merchant-map';
import { EquipmentEffectOriginalStore, type EquipmentEffectOriginalSource } from './equipment-effect-original';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const commandSchema = z.strictObject({
  commandId: id, offerId: id, stockId: id,
  expectedPrice: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER),
  expectedSuppliesRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
});
const receiptSchema = z.strictObject({
  version: z.literal('equipment.merchant-purchase.v1'), commandId: id, offerId: id,
  stockId: id, instanceId: id, price: z.number().int().positive(),
  balanceAfter: z.number().int().min(0), suppliesRevision: z.number().int().min(2),
  inventoryRevision: z.number().int().min(2),
});

export interface EquipmentMerchantPurchaseVerifier {
  /** Prove the active merchant node and permitted purchase in the same transaction.
   * No production verifier or public purchase route is installed. */
  prepare(context: { accountId: string; runId: string; offerId: string; nodeId: string; stockId: string }):
    Promise<{ assertions: D1PreparedStatement[] }>;
}

/** Owner-bound merchant purchase transaction over the original offer and START economy pins. */
export class EquipmentMerchantPurchaseStore {
  constructor(private readonly db: D1Database, private readonly verifier?: EquipmentMerchantPurchaseVerifier,
    private readonly effectSource?: EquipmentEffectOriginalSource) {}

  async buy(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof receiptSchema>> {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data;
    const hash = await sha256Hex(canonicalJson(command));
    const inventoryStore = new EquipmentInventoryStore(this.db);
    await inventoryStore.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'PURCHASE_MERCHANT' || row.request_hash !== hash) throw new ActivityError(409, 'equipment_request_conflict');
      try { return receiptSchema.parse(JSON.parse(row.response_json)); }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const original = await replay();
    if (original) return original;
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');

    const offer = await new EquipmentMerchantOfferStore(this.db).read(accountId, runId, command.offerId);
    const item = offer.stock.find(stock => stock.stockId === command.stockId);
    if (!item) throw new ActivityError(409, 'equipment_merchant_stock_not_found');
    const sold = await this.db.prepare('SELECT 1 AS sold FROM run_equipment_merchant_purchases WHERE stock_id=? AND run_id=?')
      .bind(command.stockId, runId).first<{ sold: number }>();
    if (sold) throw new ActivityError(409, 'equipment_merchant_sold_out');
    const { mapRow, arrival, visitDepth } = await prepareMerchantMap(this.db, accountId, runId, offer.nodeId);
    if (offer.visitDepth !== visitDepth) throw new ActivityError(409, 'equipment_merchant_depth_conflict');

    const [supplies, inventory] = await Promise.all([
      new RunSuppliesStore(this.db).read(accountId, runId), inventoryStore.readInventory(accountId, runId),
    ]);
    if (!supplies) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    if (supplies.revision !== command.expectedSuppliesRevision || inventory.revision !== command.expectedInventoryRevision) {
      throw new ActivityError(409, 'equipment_revision_conflict');
    }
    const count = await this.db.prepare('SELECT count(*) AS n FROM run_equipment_merchant_purchases WHERE run_id=? AND offer_id=?')
      .bind(runId, command.offerId).first<{ n: number }>();
    const snapshotRow = await this.db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?')
      .bind(runId).first<{ snapshot_json: string }>();
    let do03Rank: number, x08Rank: number;
    try {
      ({ do03Rank, x08Rank } = merchantRanksFromSnapshot(snapshotRow!.snapshot_json));
      calculateEquipmentMerchantPrice({ star: item.star, do03Rank, x08Rank, priorPaidPurchases: count?.n ?? 0 });
    } catch { throw new ActivityError(503, 'equipment_merchant_price_policy_invalid'); }
    const price = calculateEquipmentMerchantPrice({ star: item.star, do03Rank, x08Rank, priorPaidPurchases: count?.n ?? 0 }).price;
    if (price !== command.expectedPrice) throw new ActivityError(409, 'equipment_merchant_price_changed');
    if (supplies.balance < price) throw new ActivityError(409, 'run_supplies_insufficient');
    if (inventory.usedSlots >= inventory.capacitySlots) throw new ActivityError(409, 'equipment_capacity_exceeded');
    if (!this.verifier) throw new ActivityError(409, 'equipment_merchant_purchase_verification_required');
    const proof = await this.verifier.prepare({ accountId, runId, offerId: command.offerId, nodeId: offer.nodeId, stockId: command.stockId });
    if (!proof || !Array.isArray(proof.assertions) || proof.assertions.length === 0) {
      throw new ActivityError(409, 'equipment_merchant_purchase_verification_required');
    }
    const instanceId = walletId('equipment_instance');
    const nextSupplies = supplies.revision + 1, nextInventory = inventory.revision + 1;
    const balanceAfter = supplies.balance - price;
    const receipt = receiptSchema.parse({ version: 'equipment.merchant-purchase.v1', commandId: command.commandId,
      offerId: command.offerId, stockId: command.stockId, instanceId, price, balanceAfter,
      suppliesRevision: nextSupplies, inventoryRevision: nextInventory });
    const effects = await new EquipmentEffectOriginalStore(this.db, this.effectSource).prepare(accountId, runId, {
      instanceId, sourceItemId: command.stockId, expectedInventoryRevision: nextInventory });
    // v17 merchant stock is a newly issued item without independent enchantment.
    if (effects.snapshot.enchantment !== null) throw new ActivityError(409, 'equipment_merchant_enchantment_invalid');
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO equipment_merchant_purchase_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            JOIN run_supplies_state w ON w.run_id=r.id JOIN run_equipment_merchant_offers o ON o.run_id=r.id
            JOIN run_supplies_ledger latest ON latest.run_id=w.run_id AND latest.revision=w.revision
            JOIN run_supplies_ledger initial ON initial.run_id=w.run_id AND initial.source_kind='START'
              AND initial.source_ref=w.run_id AND initial.revision=1
            JOIN run_equipment_merchant_stock st ON st.offer_id=o.offer_id AND st.run_id=r.id
            JOIN operation_map_states m ON m.run_id=r.id
            JOIN operation_map_receipts ar ON ar.run_id=r.id AND ar.event_id=?
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND m.revision=? AND m.state_json=? AND m.plan_json=? AND m.start_snapshot_json=s.snapshot_json
              AND m.start_snapshot_json=? AND ar.response_json=? AND o.visit_depth=?
              AND o.offer_id=? AND o.node_id=? AND st.stock_id=? AND st.stock_json=?
              AND i.revision=? AND w.revision=? AND w.balance=? AND w.balance>=?
              AND w.account_id=r.account_id AND latest.balance_after=w.balance
              AND initial.delta=w.initial_balance AND initial.balance_after=w.initial_balance
              AND i.capacity_version=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')
              AND i.do05_rank=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')
              AND i.capacity_slots=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')
              AND w.policy_version=json_extract(s.snapshot_json,'$.runSupplies.version')
              AND w.do04_rank=json_extract(s.snapshot_json,'$.runSupplies.do04Rank')
              AND w.initial_balance=json_extract(s.snapshot_json,'$.runSupplies.initialBalance')
              AND coalesce(json_extract(s.snapshot_json,'$.admission.commanderTraits.ranks.DO03'),0)=?
              AND coalesce(json_extract(s.snapshot_json,'$.admission.commanderTraits.ranks.X08'),0)=?
              AND (SELECT count(*) FROM run_equipment_merchant_purchases p WHERE p.run_id=r.id AND p.offer_id=o.offer_id)=?
              AND (SELECT count(*) FROM run_equipment_instances e WHERE e.run_id=r.id AND e.location_kind='STORED')<i.capacity_slots
              AND NOT EXISTS(SELECT 1 FROM run_equipment_merchant_purchases p WHERE p.stock_id=st.stock_id)
              AND NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=r.id AND q.command_id=?))
            THEN 1 ELSE 0 END`).bind(arrival.event_id, runId, accountId, mapRow.revision,
          mapRow.state_json, mapRow.plan_json, mapRow.start_snapshot_json, arrival.response_json, visitDepth,
          command.offerId, offer.nodeId, command.stockId,
          canonicalJson(item), inventory.revision, supplies.revision, supplies.balance, price,
          do03Rank, x08Rank, count?.n ?? 0, command.commandId),
        this.db.prepare('UPDATE run_supplies_state SET balance=?,revision=? WHERE run_id=? AND revision=? AND balance=?')
          .bind(balanceAfter, nextSupplies, runId, supplies.revision, supplies.balance),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(nextInventory, runId, inventory.revision),
        this.db.prepare(`INSERT INTO run_supplies_ledger(run_id,source_kind,source_ref,revision,delta,balance_after)
          VALUES(?,'MERCHANT_PURCHASE',?,?,?,?)`).bind(runId, command.stockId, nextSupplies, -price, balanceAfter),
        this.db.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,
          category_id,set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision)
          VALUES(?,?,?,?,?,?,?,?,0,'STORED',NULL,NULL,NULL,1)`).bind(instanceId, runId, command.stockId,
          item.definitionId, item.definitionVersion, item.categoryId, item.setTag, item.star),
        this.db.prepare(`INSERT INTO run_equipment_merchant_purchases(run_id,offer_id,stock_id,command_id,instance_id,
          price,supplies_revision,inventory_revision) VALUES(?,?,?,?,?,?,?,?)`)
          .bind(runId, command.offerId, command.stockId, command.commandId, instanceId, price, nextSupplies, nextInventory),
        ...effects.statements,
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'PURCHASE_MERCHANT',?,?)`).bind(runId, command.commandId, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_merchant_purchase_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_merchant_state_conflict');
      throw error;
    }
    return receipt;
  }
}
