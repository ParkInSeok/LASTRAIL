import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore, type EquipmentMaintenanceVerifier } from './equipment-inventory-start';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const commandSchema = z.strictObject({ commandId: id, instanceId: id,
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1) });
const receiptSchema = z.strictObject({ version: z.literal('equipment.discard-receipt.v1'), commandId: id, instanceId: id,
  inventoryRevision: z.number().int().min(2).max(Number.MAX_SAFE_INTEGER),
  usedSlots: z.number().int().min(0).max(53), capacitySlots: z.number().int().min(30).max(54) })
  .refine(receipt => receipt.usedSlots < receipt.capacitySlots && (receipt.capacitySlots - 30) % 6 === 0);

/** Internal v19 transaction. No public route or capability upgrade for historical STARTs.
 * Safety assertions must come from authoritative gameplay state, never a client flag. */
export class EquipmentDiscardStore {
  constructor(private readonly db: D1Database, private readonly maintenance?: EquipmentMaintenanceVerifier) {}

  async discard(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof receiptSchema>> {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data, hash = await sha256Hex(canonicalJson(command));
    const inventoryStore = new EquipmentInventoryStore(this.db);
    const pinned = await inventoryStore.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'DISCARD_EQUIPMENT' || row.request_hash !== hash) throw new ActivityError(409, 'equipment_request_conflict');
      try {
        const receipt = receiptSchema.parse(JSON.parse(row.response_json));
        if (!pinned || receipt.commandId !== command.commandId || receipt.instanceId !== command.instanceId
          || receipt.inventoryRevision !== command.expectedInventoryRevision + 1
          || receipt.capacitySlots !== pinned.capacity.slots) throw new Error('receipt binding mismatch');
        return receipt;
      }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const prior = await replay();
    if (prior) return prior;
    const run = await this.db.prepare(`SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id
      WHERE r.id=? AND r.account_id=?`).bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    const inventory = await inventoryStore.readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const item = inventory.items.find(row => row.instance_id === command.instanceId);
    if (!item) throw new ActivityError(409, 'equipment_instance_not_found');
    if (inventory.revision !== command.expectedInventoryRevision) throw new ActivityError(409, 'equipment_revision_conflict');
    if (item.locked !== 0) throw new ActivityError(409, 'equipment_item_locked');
    if (item.location_kind !== 'STORED') throw new ActivityError(409, 'equipment_discard_stored_required');
    if (!this.maintenance) throw new ActivityError(409, 'equipment_maintenance_verification_required');
    const proof = await this.maintenance.prepare({ accountId, runId, instanceId: command.instanceId });
    if (!proof || !Array.isArray(proof.assertions) || !proof.assertions.length) {
      throw new ActivityError(409, 'equipment_maintenance_verification_required');
    }
    const receipt = receiptSchema.parse({ version: 'equipment.discard-receipt.v1', commandId: command.commandId,
      instanceId: command.instanceId, inventoryRevision: inventory.revision + 1,
      usedSlots: inventory.usedSlots - 1, capacitySlots: inventory.capacitySlots });
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO equipment_inventory_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            JOIN run_equipment_instances e ON e.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND s.snapshot_json=? AND i.revision=? AND i.capacity_slots=?
              AND e.instance_id=? AND e.revision=? AND e.source_item_id=?
              AND e.definition_id=? AND e.definition_version=? AND e.category_id=? AND e.set_tag=? AND e.star=?
              AND e.locked=0 AND e.location_kind='STORED' AND e.wearer_kind IS NULL AND e.wearer_id IS NULL AND e.slot_index IS NULL
              AND (SELECT count(*) FROM run_equipment_instances WHERE run_id=r.id AND location_kind='STORED')=?)
            THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, inventory.revision, inventory.capacitySlots,
          item.instance_id, item.revision, item.source_item_id, item.definition_id, item.definition_version,
          item.category_id, item.set_tag, item.star, inventory.usedSlots),
        this.db.prepare('DELETE FROM run_equipment_instances WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(runId, item.instance_id, item.revision),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(receipt.inventoryRevision, runId, inventory.revision),
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'DISCARD_EQUIPMENT',?,?)`).bind(runId, command.commandId, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_inventory_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_state_conflict');
      throw error;
    }
    return receipt;
  }
}
