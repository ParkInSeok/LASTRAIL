import { canonicalJson, EquipmentInventoryCapacity, EquipmentLoadout, sha256Hex, type EquipmentInstance, type EquipmentSetTag, type EquipmentWearer } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';

/** Only called while composing the original START batch, after runs/run_snapshots inserts. */
export function equipmentInventoryStartStatements(db: D1Database, accountId: string, runId: string, snapshotJson: string): D1PreparedStatement[] {
  const snapshot: unknown = JSON.parse(snapshotJson);
  const capacity = EquipmentInventoryCapacity.fromRunSnapshot(snapshot);
  if (!capacity) throw new Error('equipment_capacity_snapshot_missing');
  const rank = (snapshot as { equipmentInventoryCapacity: { do05Rank: number } }).equipmentInventoryCapacity.do05Rank;
  return [db.prepare('INSERT INTO run_equipment_inventory(run_id,account_id,revision,capacity_version,do05_rank,capacity_slots) VALUES(?,?,1,?,?,?)')
    .bind(runId, accountId, capacity.version, rank, capacity.slots)];
}

type CapacityRow = {
  snapshot_json: string;
  inventory_run_id: string | null;
  inventory_account_id: string | null;
  revision: number | null;
  capacity_version: string | null;
  do05_rank: number | null;
  capacity_slots: number | null;
};

type InstanceRow = {
  instance_id: string;
  source_item_id: string;
  definition_id: string;
  definition_version: string;
  category_id: string;
  set_tag: string;
  star: number;
  locked: number;
  location_kind: string;
  wearer_kind: string | null;
  wearer_id: string | null;
  slot_index: number | null;
  revision: number;
};

const lockCommand = z.strictObject({
  commandId: z.string().regex(/^[\x21-\x7e]{1,128}$/u),
  instanceId: z.string().min(1).max(128),
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
  expectedInstanceRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
  locked: z.boolean(),
});
const lockReceipt = z.strictObject({
  version: z.literal('equipment.lock-receipt.v1'),
  commandId: z.string(), instanceId: z.string(), locked: z.boolean(),
  inventoryRevision: z.number().int().positive(), instanceRevision: z.number().int().positive(),
});

export interface EquipmentMaintenanceVerifier {
  /** Must prove actual safe equipment maintenance from server-owned state in the same batch.
   * No production verifier is installed; fixture assertions cannot authorize live play. */
  prepare(context: { accountId: string; runId: string; instanceId: string }): Promise<{ assertions: D1PreparedStatement[] }>;
}

/** Internal owner-bound read. No item issuance, mutation or public inventory route is installed. */
export class EquipmentInventoryStore {
  constructor(private readonly db: D1Database, private readonly maintenanceVerifier?: EquipmentMaintenanceVerifier) {}

  async readPinnedCapacity(accountId: string, runId: string): Promise<{ revision: number; capacity: EquipmentInventoryCapacity } | null> {
    const row = await this.db.prepare(`SELECT s.snapshot_json,i.run_id AS inventory_run_id,i.account_id AS inventory_account_id,
      i.revision,i.capacity_version,i.do05_rank,i.capacity_slots
      FROM runs r JOIN run_snapshots s ON s.run_id=r.id
      LEFT JOIN run_equipment_inventory i ON i.run_id=r.id
      WHERE r.id=? AND r.account_id=?`).bind(runId, accountId).first<CapacityRow>();
    if (!row) throw new ActivityError(409, 'equipment_run_not_found');
    let capacity: EquipmentInventoryCapacity | null;
    try { capacity = EquipmentInventoryCapacity.fromRunSnapshot(JSON.parse(row.snapshot_json)); }
    catch { throw new ActivityError(503, 'equipment_inventory_storage_invalid'); }
    if (!capacity) {
      if (row.inventory_run_id !== null) throw new ActivityError(503, 'equipment_inventory_storage_invalid');
      return null;
    }
    if (row.inventory_run_id === null) throw new ActivityError(503, 'equipment_inventory_state_missing');
    if (row.inventory_account_id !== accountId || row.capacity_version !== capacity.version
      || row.do05_rank !== (capacity.slots - 30) / 6 || row.capacity_slots !== capacity.slots
      || !Number.isSafeInteger(row.revision) || row.revision! < 1) {
      throw new ActivityError(503, 'equipment_inventory_storage_invalid');
    }
    return { revision: row.revision!, capacity };
  }

  async readInventory(accountId: string, runId: string) {
    const pinned = await this.readPinnedCapacity(accountId, runId);
    if (!pinned) return null;
    const rows = (await this.db.prepare(`SELECT instance_id,source_item_id,definition_id,definition_version,category_id,
      set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision
      FROM run_equipment_instances WHERE run_id=? ORDER BY instance_id`).bind(runId).all<InstanceRow>()).results;
    try {
      const wearers = new Map<string, { wearerId: string; kind: 'operator' | 'turret'; slots: (string | null)[] }>();
      const instances: EquipmentInstance[] = [];
      for (const row of rows) {
        if (!row.source_item_id || !row.definition_version || !row.category_id || !Number.isSafeInteger(row.revision)
          || row.revision < 1 || (row.locked !== 0 && row.locked !== 1)) throw new Error('invalid item');
        instances.push({ instanceId: row.instance_id, ownerId: accountId, definitionId: row.definition_id,
          setTag: row.set_tag as EquipmentSetTag, star: row.star });
        if (row.location_kind === 'STORED') {
          if (row.wearer_kind !== null || row.wearer_id !== null || row.slot_index !== null) throw new Error('invalid storage');
          continue;
        }
        if (row.location_kind !== 'EQUIPPED' || (row.wearer_kind !== 'operator' && row.wearer_kind !== 'turret')
          || !row.wearer_id || !Number.isInteger(row.slot_index) || row.slot_index! < 0 || row.slot_index! > 2) throw new Error('invalid placement');
        const key = `${row.wearer_kind}:${row.wearer_id}`;
        const wearer = wearers.get(key) ?? { wearerId: row.wearer_id, kind: row.wearer_kind, slots: [null, null, null] };
        if (wearer.slots[row.slot_index!] !== null) throw new Error('duplicate placement');
        wearer.slots[row.slot_index!] = row.instance_id;
        wearers.set(key, wearer);
      }
      const loadout = new EquipmentLoadout(accountId, instances, [...wearers.values()] satisfies EquipmentWearer[]);
      if (!pinned.capacity.canStore(loadout, 0)) throw new Error('over capacity');
      const current = await this.readPinnedCapacity(accountId, runId);
      if (!current || current.revision !== pinned.revision) throw new Error('changed during read');
      return { revision: pinned.revision, capacitySlots: pinned.capacity.slots, usedSlots: loadout.storedCount(), items: rows };
    } catch {
      throw new ActivityError(503, 'equipment_inventory_storage_invalid');
    }
  }

  async setLock(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof lockReceipt>> {
    const parsed = lockCommand.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data, hash = await sha256Hex(canonicalJson(command));
    // Ownership is checked before any receipt lookup, including after run termination.
    await this.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'SET_LOCK' || row.request_hash !== hash) throw new ActivityError(409, 'equipment_request_conflict');
      try { return lockReceipt.parse(JSON.parse(row.response_json)); }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const original = await replay();
    if (original) return original;
    const status = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (status?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    const inventory = await this.readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const item = inventory.items.find(row => row.instance_id === command.instanceId);
    if (!item) throw new ActivityError(409, 'equipment_instance_not_found');
    if (inventory.revision !== command.expectedInventoryRevision || item.revision !== command.expectedInstanceRevision) {
      throw new ActivityError(409, 'equipment_revision_conflict');
    }
    if (Boolean(item.locked) === command.locked) throw new ActivityError(409, 'equipment_lock_unchanged');
    if (!this.maintenanceVerifier) throw new ActivityError(409, 'equipment_maintenance_verification_required');
    const prepared = await this.maintenanceVerifier.prepare({ accountId, runId, instanceId: command.instanceId });
    if (!prepared || !Array.isArray(prepared.assertions) || prepared.assertions.length === 0) {
      throw new ActivityError(409, 'equipment_maintenance_verification_required');
    }
    const receipt = lockReceipt.parse({ version: 'equipment.lock-receipt.v1', commandId: command.commandId,
      instanceId: command.instanceId, locked: command.locked,
      inventoryRevision: inventory.revision + 1, instanceRevision: item.revision + 1 });
    try {
      await this.db.batch([
        ...prepared.assertions,
        this.db.prepare(`INSERT INTO equipment_inventory_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            JOIN run_equipment_instances e ON e.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND i.revision=? AND e.instance_id=? AND e.revision=? AND e.locked=?
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots)
            THEN 1 ELSE 0 END`).bind(runId, accountId, inventory.revision, command.instanceId, item.revision, item.locked),
        this.db.prepare('UPDATE run_equipment_instances SET locked=?,revision=? WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(command.locked ? 1 : 0, receipt.instanceRevision, runId, command.instanceId, item.revision),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(receipt.inventoryRevision, runId, inventory.revision),
        this.db.prepare("INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json) VALUES(?,?,'SET_LOCK',?,?)")
          .bind(runId, command.commandId, hash, canonicalJson(receipt)),
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
