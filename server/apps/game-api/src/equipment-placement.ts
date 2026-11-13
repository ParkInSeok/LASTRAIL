import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const slot = z.strictObject({ kind: z.enum(['operator', 'turret']), wearerId: id,
  slotIndex: z.number().int().min(0).max(2) });
type Slot = z.infer<typeof slot>;
const commandSchema = z.strictObject({ commandId: id, instanceId: id,
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
  expectedInstanceRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 2),
  target: slot.nullable(), expectedTargetInstanceId: id.nullable() });
const receiptSchema = z.strictObject({ version: z.literal('equipment.placement.v1'), commandId: id,
  instanceId: id, target: slot.nullable(), displacedInstanceId: id.nullable(),
  inventoryRevision: z.number().int().min(2), instanceRevision: z.number().int().min(2),
  displacedInstanceRevision: z.number().int().min(2).nullable() });
export { commandSchema as equipmentPlacementCommandSchema, receiptSchema as equipmentPlacementReceiptSchema };

export type EquipmentPlacementAssignment={wearerKind:'operator'|'turret';wearerId:string;
  slots:({instanceId:string;definitionId:string;definitionVersion:string;categoryId:string;setTag:string;star:number}|null)[]};
export interface EquipmentPlacementVerifier {
  /** Same-batch proof of safe maintenance, real wearer ownership and definition eligibility
   * for both resulting assignments. No production implementation or public route exists. */
  /** Read-only eligibility of BOTH resulting assignments, including safe phase,
   * wearer ownership and supported placement. Stamp every authority input/revision.
   * Must not execute/consume rights; absence cannot be replaced by prepared SQL assertions. */
  readPreview?(context:{accountId:string;runId:string;instanceId:string;source:Slot|null;target:Slot|null;
    displacedInstanceId:string|null;inventoryRevision:number;assignments:EquipmentPlacementAssignment[]}):
    Promise<{sourceVersion:string;sourceRevision:number;sourceHash:string;allowed:boolean}>;
  prepare(context: { accountId: string; runId: string; instanceId: string; source: Slot | null;
    target: Slot | null; displacedInstanceId: string | null }): Promise<{ assertions: D1PreparedStatement[] }>;
}

type Item = NonNullable<Awaited<ReturnType<EquipmentInventoryStore['readInventory']>>>['items'][number];
const place = (item: Item): Slot | null => item.location_kind === 'EQUIPPED'
  ? { kind: item.wearer_kind as Slot['kind'], wearerId: item.wearer_id!, slotIndex: item.slot_index! } : null;
const sameSlot = (a: Slot, b: Slot) => a.kind === b.kind && a.wearerId === b.wearerId && a.slotIndex === b.slotIndex;

/** Prepared preview facts are owner snapshots, not shared mutable command state. */
function freezeSnapshot<T>(value:T):T{
  if(value!==null&&typeof value==='object'){for(const child of Object.values(value))freezeSnapshot(child);Object.freeze(value);}return value;
}

/** Repositions existing physical items; no issuance, deletion, lock change or combat-state mutation. */
export class EquipmentPlacementStore {
  constructor(private readonly db: D1Database, private readonly verifier?: EquipmentPlacementVerifier) {}

  async move(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof receiptSchema>> {
    const prepared = await this.prepareMove(accountId, runId, raw);
    if (!prepared.statements.length) return prepared.receipt;
    try { await this.db.batch(prepared.statements); }
    catch (error) {
      const completed = await prepared.replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_placement_state_conflict');
      throw error;
    }
    return prepared.receipt;
  }

  /** Internal transaction composition. This performs reads and prepares statements only.
   * Execute with all related health updates in ONE batch; never split or skip guards.
   * An original receipt has no statements: do not attach a new effect to an old placement. */
  async prepareMove(accountId: string, runId: string, raw: unknown) {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_command_invalid');
    const command = parsed.data, hash = await sha256Hex(canonicalJson(command));
    const inventoryStore = new EquipmentInventoryStore(this.db);
    await inventoryStore.readPinnedCapacity(accountId, runId);
    const replay = async () => {
      const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
        .bind(runId, command.commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.command_kind !== 'EQUIP_OR_MOVE' || row.request_hash !== hash) throw new ActivityError(409, 'equipment_request_conflict');
      try { return receiptSchema.parse(JSON.parse(row.response_json)); }
      catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
    };
    const original = await replay();
    if (original) return { receipt: original, statements: [] as D1PreparedStatement[], replay,projection:null };
    const run = await this.db.prepare(`SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id
      WHERE r.id=? AND r.account_id=?`).bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    const inventory = await inventoryStore.readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const source = inventory.items.find(item => item.instance_id === command.instanceId);
    if (!source) throw new ActivityError(409, 'equipment_instance_not_found');
    if (inventory.revision !== command.expectedInventoryRevision || source.revision !== command.expectedInstanceRevision) {
      throw new ActivityError(409, 'equipment_revision_conflict');
    }
    const sourceSlot = place(source);
    if ((sourceSlot === null && command.target === null) || (sourceSlot !== null && command.target !== null
      && sameSlot(sourceSlot, command.target))) throw new ActivityError(409, 'equipment_placement_unchanged');
    const displaced = command.target ? inventory.items.find(item => item.location_kind === 'EQUIPPED'
      && item.wearer_kind === command.target!.kind && item.wearer_id === command.target!.wearerId
      && item.slot_index === command.target!.slotIndex) ?? null : null;
    if ((displaced?.instance_id ?? null) !== command.expectedTargetInstanceId) {
      throw new ActivityError(409, 'equipment_target_changed');
    }
    const storedAfter = inventory.usedSlots + (sourceSlot === null ? -1 : command.target === null ? 1 : 0)
      + (sourceSlot === null && displaced ? 1 : 0);
    if (storedAfter > inventory.capacitySlots) throw new ActivityError(409, 'equipment_capacity_exceeded');
    if (!this.verifier) throw new ActivityError(409, 'equipment_placement_verification_required');
    const proof = await this.verifier.prepare({ accountId, runId, instanceId: command.instanceId,
      source: sourceSlot, target: command.target, displacedInstanceId: displaced?.instance_id ?? null });
    if (!proof || !Array.isArray(proof.assertions) || proof.assertions.length === 0) {
      throw new ActivityError(409, 'equipment_placement_verification_required');
    }
    const sourceNextRevision = source.revision + (sourceSlot && command.target ? 2 : 1);
    const receipt = receiptSchema.parse({ version: 'equipment.placement.v1', commandId: command.commandId,
      instanceId: command.instanceId, target: command.target, displacedInstanceId: displaced?.instance_id ?? null,
      inventoryRevision: inventory.revision + 1, instanceRevision: sourceNextRevision,
      displacedInstanceRevision: displaced ? displaced.revision + 1 : null });
    const affected=new Map<string,EquipmentPlacementAssignment>();
    for(const location of [sourceSlot,command.target])if(location)affected.set(location.kind+':'+location.wearerId,
      {wearerKind:location.kind,wearerId:location.wearerId,slots:[null,null,null]});
    for(const item of inventory.items){
      const location=item.instance_id===source.instance_id?command.target:item.instance_id===displaced?.instance_id?sourceSlot:place(item);
      if(!location)continue;const assignment=affected.get(location.kind+':'+location.wearerId);
      if(assignment)assignment.slots[location.slotIndex]={instanceId:item.instance_id,definitionId:item.definition_id,
        definitionVersion:item.definition_version,categoryId:item.category_id,setTag:item.set_tag,star:item.star};
    }
    const projection={accountId,runId,command,sourceSlot,runSnapshotJson:run.snapshot_json,inventory,
      assignments:[...affected.entries()].sort(([a],[b])=>a<b?-1:a>b?1:0).map(([,a])=>a)};
    const statements: D1PreparedStatement[] = [
      ...proof.assertions,
      this.db.prepare(`INSERT INTO equipment_placement_write_guard(valid)
        SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
          JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
          JOIN run_equipment_instances e ON e.run_id=r.id
          WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
            AND i.revision=? AND e.instance_id=? AND e.revision=?
            AND s.snapshot_json=?
            AND e.location_kind=? AND e.wearer_kind IS ? AND e.wearer_id IS ? AND e.slot_index IS ?
            AND i.capacity_version=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')
            AND i.do05_rank=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')
            AND i.capacity_slots=json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')
            AND (SELECT instance_id FROM run_equipment_instances t WHERE t.run_id=r.id AND t.location_kind='EQUIPPED'
              AND t.wearer_kind IS ? AND t.wearer_id IS ? AND t.slot_index IS ?) IS ?
            AND (? IS NULL OR EXISTS(SELECT 1 FROM run_equipment_instances d
              WHERE d.run_id=r.id AND d.instance_id=? AND d.revision=?))
            AND (SELECT count(*) FROM run_equipment_instances st WHERE st.run_id=r.id AND st.location_kind='STORED')=?
            AND (SELECT count(*) FROM run_equipment_instances st WHERE st.run_id=r.id AND st.location_kind='STORED')+?<=i.capacity_slots
            AND NOT EXISTS(SELECT 1 FROM run_equipment_receipts q WHERE q.run_id=r.id AND q.command_id=?))
          THEN 1 ELSE 0 END`).bind(runId, accountId, inventory.revision, source.instance_id,
        source.revision, run.snapshot_json, source.location_kind, source.wearer_kind, source.wearer_id, source.slot_index,
        command.target?.kind ?? null, command.target?.wearerId ?? null, command.target?.slotIndex ?? null,
        displaced?.instance_id ?? null, displaced?.instance_id ?? null, displaced?.instance_id ?? null,
        displaced?.revision ?? null, inventory.usedSlots, storedAfter - inventory.usedSlots, command.commandId),
    ];
    if (sourceSlot) {
      statements.push(this.db.prepare(`UPDATE run_equipment_instances SET location_kind='STORED',
        wearer_kind=NULL,wearer_id=NULL,slot_index=NULL,revision=? WHERE run_id=? AND instance_id=? AND revision=?`)
        .bind(source.revision + 1, runId, source.instance_id, source.revision));
    }
    if (displaced) {
      statements.push(this.db.prepare(`UPDATE run_equipment_instances SET location_kind=?,wearer_kind=?,
        wearer_id=?,slot_index=?,revision=? WHERE run_id=? AND instance_id=? AND revision=?`)
        .bind(sourceSlot ? 'EQUIPPED' : 'STORED', sourceSlot?.kind ?? null,
          sourceSlot?.wearerId ?? null, sourceSlot?.slotIndex ?? null, displaced.revision + 1,
          runId, displaced.instance_id, displaced.revision));
    }
    if (command.target) {
      statements.push(this.db.prepare(`UPDATE run_equipment_instances SET location_kind='EQUIPPED',
        wearer_kind=?,wearer_id=?,slot_index=?,revision=? WHERE run_id=? AND instance_id=? AND revision=?`)
        .bind(command.target.kind, command.target.wearerId, command.target.slotIndex, sourceNextRevision,
          runId, source.instance_id, source.revision + (sourceSlot ? 1 : 0)));
    }
    statements.push(
      this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
        .bind(receipt.inventoryRevision, runId, inventory.revision),
      this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
        VALUES(?,?,'EQUIP_OR_MOVE',?,?)`).bind(runId, command.commandId, hash, canonicalJson(receipt)),
      this.db.prepare('DELETE FROM equipment_placement_write_guard'),
    );
    return { receipt, statements, replay,projection:structuredClone(projection) };
  }
  /** Preview executes NO prepared statements. Projection comes from the same
   * transition owner as actual placement; only a read-only authority stamp permits it. */
  async preparePreviewMove(accountId:string,runId:string,raw:unknown){
    const prepared=await this.prepareMove(accountId,runId,raw);
    if(!prepared.projection)return {status:'ALREADY_COMMITTED' as const,receipt:prepared.receipt,projection:null,proof:null,assertUnchanged:null};
    const inspect=this.verifier?.readPreview?.bind(this.verifier);
    if(!inspect)throw new ActivityError(409,'equipment_placement_preview_verification_required');
    const projection=freezeSnapshot(structuredClone(prepared.projection)),receipt=freezeSnapshot(structuredClone(prepared.receipt)),expected=canonicalJson(projection.inventory);
    const context={accountId,runId,instanceId:projection.command.instanceId,source:projection.sourceSlot,
      target:projection.command.target,displacedInstanceId:prepared.receipt.displacedInstanceId,
      inventoryRevision:projection.inventory.revision,assignments:structuredClone(projection.assignments)};
    const schema=z.strictObject({sourceVersion:id,sourceRevision:z.number().int().positive().safe(),
      sourceHash:z.string().regex(/^[a-f0-9]{64}$/u),allowed:z.boolean()});
    const readProof=async()=>{const parsed=schema.safeParse(await inspect(structuredClone(context)));
      if(!parsed.success)throw new ActivityError(409,'equipment_placement_preview_verification_required');
      if(!parsed.data.allowed)throw new ActivityError(409,'equipment_placement_preview_not_allowed');return parsed.data;};
    const proof=freezeSnapshot(await readProof());
    const assertUnchanged=async()=>{
      const inventory=await new EquipmentInventoryStore(this.db).readInventory(accountId,runId);
      const run=await this.db.prepare(`SELECT r.status,a.status AS account_status,s.snapshot_json FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`).bind(runId,accountId).first<{status:string;account_status:string;snapshot_json:string}>();
      if(canonicalJson(inventory)!==expected||run?.status!=='ACTIVE'||run.account_status!=='ACTIVE'||run.snapshot_json!==projection.runSnapshotJson
        ||canonicalJson(await readProof())!==canonicalJson(proof))throw new ActivityError(409,'equipment_placement_preview_state_conflict');
    };
    await assertUnchanged();
    return Object.freeze({status:'PREPARED' as const,receipt,projection,proof,assertUnchanged});
  }

}
