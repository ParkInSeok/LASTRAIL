import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { equipmentEffectOriginalReaderSchema, equipmentEffectOriginalHash, equipmentEffectStateReaderSchema,
  type EquipmentEffectOriginalSnapshot } from './equipment-effect-snapshot';

/** Called only after the verified original INSERT inside the issuance batch. */
export async function equipmentEffectStateStartStatements(db: D1Database, runId: string, instanceId: string,
  sourceRef: string, original: EquipmentEffectOriginalSnapshot, originalHash: string): Promise<D1PreparedStatement[]> {
  const snapshot = equipmentEffectStateReaderSchema.parse({ ...original, version: original.version==='equipment.effect-original.v1'?'equipment.effect-state.v1':'equipment.effect-state.v2.20261015' });
  const hash = await sha256Hex(canonicalJson({ runId, instanceId, effectRevision: 1, instanceRevision: 1,
    sourceRef, originalHash, snapshot }));
  return [
    db.prepare(`INSERT INTO run_equipment_effect_history(run_id,instance_id,effect_revision,instance_revision,
      source_ref,original_hash,snapshot_json,payload_hash) VALUES(?,?,1,1,?,?,?,?)`)
      .bind(runId, instanceId, sourceRef, originalHash, canonicalJson(snapshot), hash),
    db.prepare('INSERT INTO run_equipment_effect_heads(run_id,instance_id,revision) VALUES(?,?,1)').bind(runId, instanceId),
  ];
}

type Row = { definition_id: string; definition_version: string; star: number; revision: number;
  effect_revision: number | null; history_revision: number | null; applied_instance_revision: number | null;
  snapshot_json: string | null; payload_hash: string | null; source_ref: string | null;
  original_hash: string | null; original_json: string | null; original_payload_hash: string | null;
  source_item_id: string | null };

/** Owns current effect lookup. No query initializes, replaces or executes effects. */
export class EquipmentEffectStateStore {
  constructor(private readonly db: D1Database) {}

  async read(accountId: string, runId: string, instanceId: string) {
    const inventory = new EquipmentInventoryStore(this.db);
    const pin = await inventory.readPinnedCapacity(accountId, runId);
    if (typeof instanceId !== 'string' || !/^[\x21-\x7e]{1,128}$/u.test(instanceId)) {
      throw new ActivityError(400, 'equipment_command_invalid');
    }
    if (!pin) return null;
    const row = await this.db.prepare(`SELECT e.definition_id,e.definition_version,e.star,e.revision,
      h.revision AS effect_revision,l.effect_revision AS history_revision,l.instance_revision AS applied_instance_revision,
      l.snapshot_json,l.payload_hash,l.source_ref,l.original_hash,o.snapshot_json AS original_json,
      o.payload_hash AS original_payload_hash,o.source_item_id
      FROM run_equipment_instances e LEFT JOIN run_equipment_effect_heads h ON h.run_id=e.run_id AND h.instance_id=e.instance_id
      LEFT JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision
      LEFT JOIN run_equipment_effect_originals o ON o.run_id=e.run_id AND o.instance_id=e.instance_id
      WHERE e.run_id=? AND e.instance_id=?`).bind(runId, instanceId).first<Row>();
    if (!row) return null;
    if (row.effect_revision === null) {
      if (row.original_json !== null) throw new ActivityError(503, 'equipment_effect_state_missing');
      return null;
    }
    try {
      if (!Number.isSafeInteger(row.effect_revision) || row.effect_revision < 1
        || row.history_revision !== row.effect_revision || !Number.isSafeInteger(row.applied_instance_revision)
        || row.applied_instance_revision! < 1 || row.applied_instance_revision! > row.revision
        || !row.source_ref || !row.source_item_id || !row.original_json || !row.snapshot_json
        || row.original_hash !== row.original_payload_hash) throw new Error('effect history binding');
      const original = equipmentEffectOriginalReaderSchema.parse(JSON.parse(row.original_json));
      if (await equipmentEffectOriginalHash(runId, instanceId, row.source_item_id, original) !== row.original_hash) {
        throw new Error('original checksum');
      }
      const snapshot = equipmentEffectStateReaderSchema.parse(JSON.parse(row.snapshot_json));
      if (snapshot.equipment.definitionId !== row.definition_id || snapshot.equipment.definitionVersion !== row.definition_version
        || snapshot.equipment.star !== row.star) throw new Error('effect instance mismatch');
      const hash = await sha256Hex(canonicalJson({ runId, instanceId, effectRevision: row.effect_revision,
        instanceRevision: row.applied_instance_revision, sourceRef: row.source_ref, originalHash: row.original_hash, snapshot }));
      if (hash !== row.payload_hash) throw new Error('current checksum');
      const current = await inventory.readPinnedCapacity(accountId, runId);
      if (!current || current.revision !== pin.revision) throw new Error('inventory changed during read');
      return { effectRevision: row.effect_revision, instanceRevision: row.revision,
        inventoryRevision: pin.revision, snapshot };
    } catch { throw new ActivityError(503, 'equipment_effect_state_invalid'); }
  }
}
