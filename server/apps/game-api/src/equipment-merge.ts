import { canonicalJson, planEquipmentMerge, sha256Hex, type EquipmentMergeMaterial } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentEffectStateStore } from './equipment-effect-state';
import {equipmentEffectStateReaderSchema} from './equipment-effect-snapshot';
import { planEquipmentMergeEffects, resolveEquipmentMergeEffects } from './equipment-merge-effects';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u), revision = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1);
export const equipmentMergeCommandSchema = z.strictObject({ commandId: id, primaryInstanceId: id, secondaryInstanceId: id,
  expectedInventoryRevision: revision, expectedPrimaryRevision: revision, expectedSecondaryRevision: revision,
  expectedPrimaryEffectRevision: revision, expectedSecondaryEffectRevision: revision,
  expectedSourceVersion: id, confirmedLosses: z.literal(true),
  responseVersion:z.literal('equipment.merge-receipt.v2.20261021').optional() });
type MergeRequest=z.infer<typeof equipmentMergeCommandSchema>;
type LegacyMergeRequest=Omit<MergeRequest,'responseVersion'>;
/** Omitted version preserves the legacy canonical request bytes. */
export function equipmentMergeRequestValue(command:MergeRequest):LegacyMergeRequest|(LegacyMergeRequest&{responseVersion:'equipment.merge-receipt.v2.20261021'}){
  const {responseVersion,...required}=command;
  return responseVersion?{...required,responseVersion}:required;
}
const location = z.strictObject({ kind: z.enum(['operator', 'turret']), wearerId: id, slotIndex: z.number().int().min(0).max(2) }).nullable();
const committed = z.number().int().min(2).max(Number.MAX_SAFE_INTEGER);
const legacyMergeReceiptSchema = z.strictObject({ version: z.literal('equipment.merge-receipt.v1'), commandId: id,
  resultInstanceId: id, consumedInstanceId: id, sourceVersion: id, star: z.number().int().min(2).max(5),
  instanceRevision: committed, effectRevision: committed, inventoryRevision: committed,
  resultLocation: location, vacatedLocation: location, storedAfter: z.number().int().min(0), suppliesCost: z.literal(0) });
export const equipmentMergeReceiptSchema=z.discriminatedUnion('version',[legacyMergeReceiptSchema,
  legacyMergeReceiptSchema.extend({version:z.literal('equipment.merge-receipt.v2.20261021'),effectSnapshot:equipmentEffectStateReaderSchema})]);

export interface EquipmentMergeSource {
  /** Prove actual safe maintenance, pinned supported grade/pool definitions and result-slot eligibility
   * in this same batch. Select a first effect only after version checks at final confirmation.
   * No external committed RNG side effects, production source or public route is installed. */
  prepare(context: { accountId: string; runId: string; primaryInstanceId: string; secondaryInstanceId: string;
    expectedSourceVersion: string; effects: ReturnType<typeof planEquipmentMergeEffects>; placement: ReturnType<typeof planEquipmentMerge> }):
    Promise<{ sourceVersion: string; resolvedEffects: unknown; assertions: D1PreparedStatement[] }>;
}
type History = { snapshot_json: string; payload_hash: string; original_hash: string };

/** The primary identity becomes the result in its existing slot; auxiliary original/history survive consumption. */
export class EquipmentMergeStore {
  constructor(private readonly db: D1Database, private readonly source?: EquipmentMergeSource) {}

  async merge(accountId: string, runId: string, raw: unknown) {
    const prepared = await this.prepareMerge(accountId, runId, raw);
    if (!prepared.statements.length) return prepared.receipt;
    try { await this.db.batch(prepared.statements); }
    catch (error) {
      const completed = await prepared.replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed|equipment_merge_material_invalid|equipment_effect_revision_invalid/iu.test(String(error))) {
        throw new ActivityError(409, 'equipment_merge_state_conflict');
      }
      throw error;
    }
    return prepared.receipt;
  }

  /** Candidate only. Keep all returned assertions/statements in ONE commit with
   * dependent HP changes. A replay contains no writes and cannot authorize backfill. */
  async prepareMerge(accountId: string, runId: string, raw: unknown) {
    const parsed = equipmentMergeCommandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data, requestHash = await sha256Hex(canonicalJson(equipmentMergeRequestValue(command)));
    const inventory = new EquipmentInventoryStore(this.db);
    await inventory.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'MERGE_EQUIPMENT' || row.request_hash !== requestHash) throw new ActivityError(409, 'equipment_request_conflict');
      try {
        const saved=equipmentMergeReceiptSchema.parse(JSON.parse(row.response_json));
        if(saved.version!==(command.responseVersion??'equipment.merge-receipt.v1'))throw Error('response version mismatch');
        return saved;
      }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const prior = await replay();
    if (prior) return {receipt: prior, statements: [] as D1PreparedStatement[], replay, projection: null};
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    const stock = await inventory.readInventory(accountId, runId);
    if (!stock) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const main = stock.items.find(item => item.instance_id === command.primaryInstanceId),
      aux = stock.items.find(item => item.instance_id === command.secondaryInstanceId);
    if (!main || !aux) throw new ActivityError(409, 'equipment_merge_material_invalid');
    const material = (row: typeof main): EquipmentMergeMaterial => ({ instanceId: row.instance_id, ownerId: accountId,
      categoryId: row.category_id, star: row.star, setTag: row.set_tag as EquipmentMergeMaterial['setTag'], locked: row.locked !== 0,
      location: row.location_kind === 'STORED' ? null : { kind: row.wearer_kind as 'operator' | 'turret', wearerId: row.wearer_id!, slotIndex: row.slot_index! } });
    let placement: ReturnType<typeof planEquipmentMerge>;
    try { placement = planEquipmentMerge(accountId, material(main), material(aux), {
      usedSlots: stock.usedSlots, capacitySlots: stock.capacitySlots }); }
    catch (error) { throw new ActivityError(409, error instanceof Error ? error.message : 'equipment_merge_material_invalid'); }
    const stateStore = new EquipmentEffectStateStore(this.db);
    const a = await stateStore.read(accountId, runId, main.instance_id), b = await stateStore.read(accountId, runId, aux.instance_id);
    if (!a || !b) throw new ActivityError(409, 'equipment_effect_state_required');
    if (stock.revision !== command.expectedInventoryRevision || a.inventoryRevision !== stock.revision || b.inventoryRevision !== stock.revision
      || main.revision !== command.expectedPrimaryRevision || aux.revision !== command.expectedSecondaryRevision
      || a.instanceRevision !== main.revision || b.instanceRevision !== aux.revision
      || a.effectRevision !== command.expectedPrimaryEffectRevision || b.effectRevision !== command.expectedSecondaryEffectRevision) {
      throw new ActivityError(409, 'equipment_revision_conflict');
    }
    const history = async (instanceId: string, effectRevision: number) => this.db.prepare(`SELECT snapshot_json,payload_hash,original_hash
      FROM run_equipment_effect_history WHERE run_id=? AND instance_id=? AND effect_revision=?`)
      .bind(runId, instanceId, effectRevision).first<History>();
    const [ah, bh, start] = await Promise.all([history(main.instance_id, a.effectRevision), history(aux.instance_id, b.effectRevision),
      this.db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?').bind(runId).first<{ snapshot_json: string }>()]);
    if (!ah || !bh || !start || ah.snapshot_json !== canonicalJson(a.snapshot) || bh.snapshot_json !== canonicalJson(b.snapshot)) {
      throw new ActivityError(409, 'equipment_revision_conflict');
    }
    const effects = planEquipmentMergeEffects(a.snapshot, b.snapshot);
    if (!this.source) throw new ActivityError(409, 'equipment_merge_verification_required');
    const proof = await this.source.prepare({ accountId, runId, primaryInstanceId: main.instance_id, secondaryInstanceId: aux.instance_id,
      expectedSourceVersion: command.expectedSourceVersion, effects, placement });
    if (!proof || !Array.isArray(proof.assertions) || !proof.assertions.length) throw new ActivityError(409, 'equipment_merge_verification_required');
    if (proof.sourceVersion !== command.expectedSourceVersion) throw new ActivityError(409, 'equipment_merge_source_version_conflict');
    const snapshot = resolveEquipmentMergeEffects(a.snapshot, b.snapshot, proof.resolvedEffects);
    const effectRevision = a.effectRevision + 1, instanceRevision = main.revision + 1, inventoryRevision = stock.revision + 1;
    const payloadHash = await sha256Hex(canonicalJson({ runId, instanceId: main.instance_id, effectRevision, instanceRevision,
      sourceRef: command.commandId, originalHash: ah.original_hash, snapshot }));
    const receipt = equipmentMergeReceiptSchema.parse({ version: command.responseVersion??'equipment.merge-receipt.v1', commandId: command.commandId,
      resultInstanceId: main.instance_id, consumedInstanceId: aux.instance_id, sourceVersion: proof.sourceVersion,
      star: placement.nextStar, instanceRevision, effectRevision, inventoryRevision, resultLocation: placement.resultLocation,
      vacatedLocation: placement.vacatedLocation, storedAfter: placement.storedAfter, suppliesCost: 0,
      ...(command.responseVersion?{effectSnapshot:snapshot}:{}) });
    const guardMaterial = (row: typeof main, current: typeof a, h: History) => this.db.prepare(`INSERT INTO equipment_merge_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_instances e
        JOIN run_equipment_effect_heads head ON head.run_id=e.run_id AND head.instance_id=e.instance_id
        JOIN run_equipment_effect_history l ON l.run_id=head.run_id AND l.instance_id=head.instance_id AND l.effect_revision=head.revision
        JOIN run_equipment_effect_originals o ON o.run_id=e.run_id AND o.instance_id=e.instance_id
        WHERE e.run_id=? AND e.instance_id=? AND e.revision=? AND e.locked=0 AND head.revision=?
          AND e.source_item_id=? AND e.definition_id=? AND e.definition_version=? AND e.category_id=? AND e.set_tag=? AND e.star=?
          AND e.location_kind=? AND e.wearer_kind IS ? AND e.wearer_id IS ? AND e.slot_index IS ?
          AND l.snapshot_json=? AND l.payload_hash=? AND l.original_hash=? AND o.payload_hash=l.original_hash)
        THEN 1 ELSE 0 END`).bind(runId, row.instance_id, row.revision, current.effectRevision,
      row.source_item_id, row.definition_id, row.definition_version, row.category_id, row.set_tag, row.star,
      row.location_kind, row.wearer_kind, row.wearer_id, row.slot_index, h.snapshot_json, h.payload_hash, h.original_hash);
    const statements = [
        ...proof.assertions,
        this.db.prepare(`INSERT INTO equipment_merge_write_guard(valid) SELECT CASE WHEN EXISTS(
          SELECT 1 FROM runs r JOIN accounts account ON account.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
          JOIN run_equipment_inventory i ON i.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND account.status='ACTIVE'
            AND i.account_id=r.account_id AND i.revision=? AND s.snapshot_json=?
            AND i.capacity_version=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')
            AND i.capacity_slots=? AND i.do05_rank=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')
            AND (SELECT count(*) FROM run_equipment_instances e WHERE e.run_id=r.id AND e.location_kind='STORED')=?
            AND NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=r.id AND q.command_id=?)) THEN 1 ELSE 0 END`)
          .bind(runId, accountId, stock.revision, start.snapshot_json, stock.capacitySlots, stock.usedSlots, command.commandId),
        guardMaterial(main, a, ah), guardMaterial(aux, b, bh),
        this.db.prepare(`INSERT INTO run_equipment_merges(run_id,command_id,primary_instance_id,secondary_instance_id,
          primary_effect_revision,secondary_effect_revision,instance_revision,inventory_revision,result_snapshot_json,payload_hash)
          VALUES(?,?,?,?,?,?,?,?,?,?)`).bind(runId, command.commandId, main.instance_id, aux.instance_id, a.effectRevision,
          b.effectRevision, instanceRevision, inventoryRevision, canonicalJson(snapshot), payloadHash),
        this.db.prepare('DELETE FROM run_equipment_instances WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(runId, aux.instance_id, aux.revision),
        this.db.prepare('UPDATE run_equipment_instances SET star=?,revision=? WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(placement.nextStar, instanceRevision, runId, main.instance_id, main.revision),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?').bind(inventoryRevision, runId, stock.revision),
        this.db.prepare(`INSERT INTO run_equipment_effect_history(run_id,instance_id,effect_revision,instance_revision,
          source_ref,original_hash,snapshot_json,payload_hash) VALUES(?,?,?,?,?,?,?,?)`)
          .bind(runId, main.instance_id, effectRevision, instanceRevision, command.commandId, ah.original_hash, canonicalJson(snapshot), payloadHash),
        this.db.prepare('UPDATE run_equipment_effect_heads SET revision=? WHERE run_id=? AND instance_id=? AND revision=?')
          .bind(effectRevision, runId, main.instance_id, a.effectRevision),
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'MERGE_EQUIPMENT',?,?)`).bind(runId, command.commandId, requestHash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_merge_write_guard'),
      ];
    return {receipt, statements, replay, projection: {accountId,runId,primary:main,secondary:aux,snapshot}};
  }
}

export type PreparedEquipmentMerge = Awaited<ReturnType<EquipmentMergeStore['prepareMerge']>>;
