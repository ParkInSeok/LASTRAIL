import { canonicalJson } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { equipmentEffectOriginalReaderSchema as snapshotSchema, equipmentEffectOriginalHash,
  type EquipmentEffectOriginalSnapshot } from './equipment-effect-snapshot';
import { equipmentEffectStateStartStatements } from './equipment-effect-state';
export type { EquipmentEffectOriginalSnapshot } from './equipment-effect-snapshot';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const contextSchema = z.strictObject({ instanceId: id, sourceItemId: id,
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER) });
type Context = z.infer<typeof contextSchema>;

export interface EquipmentEffectOriginalSource {
  /** Prove original issuance provenance, eligible definitions, grades and resolved procedure values
   * with caller-DB assertions. Prepare only; no production adapter is installed. */
  prepare(context: Context & { accountId: string; runId: string }):
    Promise<{ snapshot: unknown; assertions: D1PreparedStatement[] }>;
}

/** Immutable issuance provenance. This is not the current effects of a subsequently enchanted item. */
export class EquipmentEffectOriginalStore {
  constructor(private readonly db: D1Database, private readonly source?: EquipmentEffectOriginalSource) {}

  async read(accountId: string, runId: string, instanceId: string): Promise<EquipmentEffectOriginalSnapshot | null> {
    await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId, runId);
    if (!id.safeParse(instanceId).success) throw new ActivityError(400, 'equipment_command_invalid');
    const row = await this.db.prepare(`SELECT source_item_id,snapshot_json,payload_hash
      FROM run_equipment_effect_originals WHERE run_id=? AND instance_id=?`).bind(runId, instanceId)
      .first<{ source_item_id: string; snapshot_json: string; payload_hash: string }>();
    if (!row) return null;
    try {
      const snapshot = snapshotSchema.parse(JSON.parse(row.snapshot_json));
      if (await equipmentEffectOriginalHash(runId, instanceId, row.source_item_id, snapshot) !== row.payload_hash) {
        throw new Error('original effect checksum');
      }
      return snapshot;
    } catch { throw new ActivityError(503, 'equipment_effect_storage_invalid'); }
  }

  /** Append these statements AFTER the caller's issuance and inventory update in the SAME batch.
   * This method never writes on its own and never supplies default effects to historical items. */
  async prepare(accountId: string, runId: string, raw: unknown) {
    const parsed = contextSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const context = parsed.data;
    const pin = await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId, runId);
    if (!pin) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const run = await this.db.prepare(`SELECT r.status,s.snapshot_json FROM runs r
      JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`).bind(runId, accountId)
      .first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    if (!this.source) throw new ActivityError(409, 'equipment_effect_source_required');
    const supplied = await this.source.prepare({ ...context, accountId, runId });
    if (!supplied || !Array.isArray(supplied.assertions) || supplied.assertions.length === 0) {
      throw new ActivityError(409, 'equipment_effect_source_required');
    }
    const payload = snapshotSchema.safeParse(supplied.snapshot);
    if (!payload.success) throw new ActivityError(409, 'equipment_effect_snapshot_invalid');
    const snapshot = payload.data, bytes = canonicalJson(snapshot);
    const hash = await equipmentEffectOriginalHash(runId, context.instanceId, context.sourceItemId, snapshot);
    const statements: D1PreparedStatement[] = [
      ...supplied.assertions,
      this.db.prepare(`INSERT INTO equipment_effect_original_write_guard(valid)
        SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
          JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
          JOIN run_equipment_instances e ON e.run_id=r.id
          WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
            AND s.snapshot_json=? AND i.account_id=r.account_id AND i.revision=?
            AND e.instance_id=? AND e.source_item_id=? AND e.revision=1
            AND e.definition_id=? AND e.definition_version=? AND e.star=?
            AND NOT EXISTS(SELECT 1 FROM run_equipment_effect_originals o WHERE o.instance_id=e.instance_id))
          THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, context.expectedInventoryRevision,
        context.instanceId, context.sourceItemId, snapshot.equipment.definitionId,
        snapshot.equipment.definitionVersion, snapshot.equipment.star),
      this.db.prepare(`INSERT INTO run_equipment_effect_originals
        (instance_id,run_id,source_item_id,snapshot_json,payload_hash) VALUES(?,?,?,?,?)`)
        .bind(context.instanceId, runId, context.sourceItemId, bytes, hash),
      ...await equipmentEffectStateStartStatements(this.db, runId, context.instanceId, context.sourceItemId, snapshot, hash),
      this.db.prepare('DELETE FROM equipment_effect_original_write_guard'),
    ];
    return { snapshot, statements };
  }
}
