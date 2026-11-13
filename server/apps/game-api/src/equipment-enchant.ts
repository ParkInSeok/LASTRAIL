import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentEffectStateStore } from './equipment-effect-state';
import { equipmentEffectStateReaderSchema, equipmentEnchantmentReaderSchema, equipmentEnchantmentSchema } from './equipment-effect-snapshot';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const revision = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1);
const committedRevision = z.number().int().min(2).max(Number.MAX_SAFE_INTEGER);
const commandSchema = z.strictObject({ commandId: id, instanceId: id, sourceRef: id,
  expectedSourceVersion: id, replaceExisting: z.boolean(),
  expectedInventoryRevision: revision, expectedInstanceRevision: revision, expectedEffectRevision: revision });
const legacyReceiptSchema = z.strictObject({ version: z.literal('equipment.enchant-receipt.v1'),
  commandId: id, instanceId: id, sourceRef: id, sourceVersion: id, effectRevision: committedRevision, instanceRevision: committedRevision,
  inventoryRevision: committedRevision, enchantment: equipmentEnchantmentSchema });
const receiptSchema=z.union([legacyReceiptSchema,legacyReceiptSchema.extend({version:z.literal('equipment.enchant-receipt.v2.20261015'),enchantment:equipmentEnchantmentReaderSchema})]);

export interface EquipmentEnchantSource {
  /** Prepare a resolved procedure from the pinned event/card pool. Prove owner/right,
   * eligible unlocked target and safe maintenance in this same DB batch, and consume
   * that right there. Check the displayed pool version before selecting, and assert
   * it again in the batch. Random selection must have no external committed side effects;
   * a failed batch consumes nothing. No production adapter or public route is installed. */
  prepare(context: { accountId: string; runId: string; instanceId: string; sourceRef: string; star: number; expectedSourceVersion: string }):
    Promise<{ sourceVersion: string; enchantment: unknown; assertions: D1PreparedStatement[]; consumeStatements: D1PreparedStatement[] }>;
}

/** Owns enchantment replacement only. Issuance, unique-effect synthesis and event production remain separate. */
export class EquipmentEnchantStore {
  constructor(private readonly db: D1Database, private readonly source?: EquipmentEnchantSource) {}

  async apply(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof receiptSchema>> {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data, requestHash = await sha256Hex(canonicalJson(command));
    const inventory = new EquipmentInventoryStore(this.db);
    await inventory.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'ENCHANT_EQUIPMENT' || row.request_hash !== requestHash) throw new ActivityError(409, 'equipment_request_conflict');
      try { return receiptSchema.parse(JSON.parse(row.response_json)); }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const prior = await replay();
    if (prior) return prior;
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    if (await this.db.prepare('SELECT 1 AS used FROM run_equipment_enchantment_changes WHERE run_id=? AND source_ref=?')
      .bind(runId, command.sourceRef).first()) throw new ActivityError(409, 'equipment_enchant_source_consumed');
    await inventory.readInventory(accountId, runId);
    const current = await new EquipmentEffectStateStore(this.db).read(accountId, runId, command.instanceId);
    if (!current) throw new ActivityError(409, 'equipment_effect_state_required');
    if (current.inventoryRevision !== command.expectedInventoryRevision || current.instanceRevision !== command.expectedInstanceRevision
      || current.effectRevision !== command.expectedEffectRevision) throw new ActivityError(409, 'equipment_revision_conflict');
    const row = await this.db.prepare(`SELECT e.locked,l.snapshot_json,l.payload_hash,l.original_hash,s.snapshot_json AS start_json
      FROM run_equipment_instances e JOIN run_equipment_effect_heads h ON h.run_id=e.run_id AND h.instance_id=e.instance_id
      JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision
      JOIN run_snapshots s ON s.run_id=e.run_id WHERE e.run_id=? AND e.instance_id=? AND h.revision=? AND e.revision=?`)
      .bind(runId, command.instanceId, current.effectRevision, current.instanceRevision)
      .first<{ locked: number; snapshot_json: string; payload_hash: string; original_hash: string; start_json: string }>();
    if (!row || row.snapshot_json !== canonicalJson(current.snapshot)) throw new ActivityError(409, 'equipment_revision_conflict');
    if (row.locked !== 0) throw new ActivityError(409, 'equipment_item_locked');
    if (command.replaceExisting !== (current.snapshot.enchantment !== null)) {
      throw new ActivityError(409, 'equipment_enchant_confirmation_required');
    }
    if (!this.source) throw new ActivityError(409, 'equipment_enchant_verification_required');
    const proof = await this.source.prepare({ accountId, runId, instanceId: command.instanceId, sourceRef: command.sourceRef,
      star: current.snapshot.equipment.star, expectedSourceVersion: command.expectedSourceVersion });
    if (!proof || !Array.isArray(proof.assertions) || !proof.assertions.length
      || !Array.isArray(proof.consumeStatements) || !proof.consumeStatements.length) {
      throw new ActivityError(409, 'equipment_enchant_verification_required');
    }
    if (proof.sourceVersion !== command.expectedSourceVersion) throw new ActivityError(409, 'equipment_enchant_source_version_conflict');
    const resolved = equipmentEnchantmentReaderSchema.safeParse(proof.enchantment);
    if (!resolved.success || resolved.data.appliedStar !== current.snapshot.equipment.star) {
      throw new ActivityError(409, 'equipment_enchant_source_invalid');
    }
    const nextVersion=current.snapshot.version!=='equipment.effect-state.v1'||resolved.data.effectId==='EN17'?'equipment.effect-state.v2.20261015':'equipment.effect-state.v1';
    const snapshot = equipmentEffectStateReaderSchema.parse({ ...current.snapshot,version:nextVersion, enchantment: resolved.data });
    const effectRevision = current.effectRevision + 1, instanceRevision = current.instanceRevision + 1,
      inventoryRevision = current.inventoryRevision + 1;
    const payloadHash = await sha256Hex(canonicalJson({ runId, instanceId: command.instanceId, effectRevision, instanceRevision,
      sourceRef: command.sourceRef, originalHash: row.original_hash, snapshot }));
    const receipt = receiptSchema.parse({ version: nextVersion==='equipment.effect-state.v1'?'equipment.enchant-receipt.v1':'equipment.enchant-receipt.v2.20261015', commandId: command.commandId,
      instanceId: command.instanceId, sourceRef: command.sourceRef, sourceVersion: proof.sourceVersion, effectRevision, instanceRevision, inventoryRevision,
      enchantment: resolved.data });
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO equipment_enchant_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            JOIN run_equipment_instances e ON e.run_id=r.id
            JOIN run_equipment_effect_heads h ON h.run_id=e.run_id AND h.instance_id=e.instance_id
            JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision
            JOIN run_equipment_effect_originals o ON o.run_id=e.run_id AND o.instance_id=e.instance_id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND s.snapshot_json=? AND i.account_id=r.account_id AND i.revision=?
              AND e.instance_id=? AND e.revision=? AND e.locked=0 AND h.revision=?
              AND l.snapshot_json=? AND l.payload_hash=? AND l.original_hash=? AND o.payload_hash=l.original_hash
              AND e.definition_id=json_extract(l.snapshot_json,'$.equipment.definitionId')
              AND e.definition_version=json_extract(l.snapshot_json,'$.equipment.definitionVersion')
              AND e.star=json_extract(l.snapshot_json,'$.equipment.star')
              AND NOT EXISTS(SELECT 1 FROM run_equipment_enchantment_changes c WHERE c.run_id=r.id AND c.source_ref=?)
              AND NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=r.id AND q.command_id=?)) THEN 1 ELSE 0 END`)
          .bind(runId, accountId, row.start_json, current.inventoryRevision, command.instanceId, current.instanceRevision,
            current.effectRevision, row.snapshot_json, row.payload_hash, row.original_hash, command.sourceRef, command.commandId),
        ...proof.consumeStatements,
        this.db.prepare('UPDATE run_equipment_instances SET revision=? WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(instanceRevision, runId, command.instanceId, current.instanceRevision),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(inventoryRevision, runId, current.inventoryRevision),
        this.db.prepare(`INSERT INTO run_equipment_enchantment_changes(run_id,source_ref,command_id,instance_id,
          effect_revision,instance_revision,inventory_revision,payload_hash) VALUES(?,?,?,?,?,?,?,?)`)
          .bind(runId, command.sourceRef, command.commandId, command.instanceId, effectRevision, instanceRevision, inventoryRevision, payloadHash),
        this.db.prepare(`INSERT INTO run_equipment_effect_history(run_id,instance_id,effect_revision,instance_revision,
          source_ref,original_hash,snapshot_json,payload_hash) VALUES(?,?,?,?,?,?,?,?)`)
          .bind(runId, command.instanceId, effectRevision, instanceRevision, command.sourceRef, row.original_hash, canonicalJson(snapshot), payloadHash),
        this.db.prepare('UPDATE run_equipment_effect_heads SET revision=? WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(effectRevision, runId, command.instanceId, current.effectRevision),
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'ENCHANT_EQUIPMENT',?,?)`).bind(runId, command.commandId, requestHash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_enchant_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed|equipment_effect_revision_invalid/iu.test(String(error))) {
        throw new ActivityError(409, 'equipment_enchant_state_conflict');
      }
      throw error;
    }
    return receipt;
  }
}
