import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { EquipmentPlacementStore, equipmentPlacementCommandSchema, equipmentPlacementReceiptSchema, type EquipmentPlacementAssignment } from './equipment-placement';
import { CombatHealthStore, combatHealthReceiptSchema } from './combat-health-store';
import {equipmentMaintenanceApplicationSchema,equipmentMaintenanceConfigurationSchema} from './equipment-maintenance-application';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const commandSchema = z.strictObject({ placement: equipmentPlacementCommandSchema,
  expectedHealthRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 2), expectedStatVersion: id,
  responseVersion:z.literal('equipment.health-placement.v2.20261019').optional() });
const targetSchema = z.strictObject({ wearerKind: z.enum(['operator', 'turret']), wearerId: id, targetId: id,
  maxHp: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER) });
const legacyReceiptSchema = z.strictObject({ version: z.literal('equipment.health-placement.v1'), commandId: id,
  statVersion: id, placement: equipmentPlacementReceiptSchema, targets: z.array(targetSchema).min(1).max(2),
  health: z.array(combatHealthReceiptSchema).min(1).max(2) });
const receiptSchema=z.discriminatedUnion('version',[legacyReceiptSchema,legacyReceiptSchema.extend({version:z.literal('equipment.health-placement.v2.20261019'),application:equipmentMaintenanceApplicationSchema})]);
type Assignment=EquipmentPlacementAssignment;

export interface EquipmentFinalHealthSource {
  /** Resolve final REAL three-slot stats from supported pinned equipment/effect definitions.
   * Prove safe eligibility, every involved current effect/definition/owner and exact final
   * assignment in the same batch. Return exactly one mapped HP actor per affected wearer.
   * No production source exists; never accept maxima or mapping directly from a client. */
  prepare(context: { accountId: string; runId: string; commandId: string; expectedStatVersion: string;
    assignments: Assignment[] }): Promise<{ statVersion: string; targets: unknown[]; assertions: D1PreparedStatement[]; configuration?:unknown }>;
}

/** Coordinates existing domain writers; neither placement nor HP rules are duplicated. */
export class EquipmentHealthPlacementStore {
  constructor(private readonly db: D1Database, private readonly equipment: EquipmentPlacementStore,
    private readonly health: CombatHealthStore, private readonly source?: EquipmentFinalHealthSource) {}

  async move(accountId: string, runId: string, raw: unknown) {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, 'equipment_health_command_invalid');
    const command = parsed.data;
    const request={placement:command.placement,expectedHealthRevision:command.expectedHealthRevision,expectedStatVersion:command.expectedStatVersion,
      ...(command.responseVersion?{responseVersion:command.responseVersion}:{})};
    const requestHash = await sha256Hex(canonicalJson(request));
    if (!await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId, runId)) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    const replay = async () => {
      const row = await this.db.prepare('SELECT request_hash,response_json,response_hash FROM run_equipment_health_links WHERE run_id=? AND command_id=?')
        .bind(runId, command.placement.commandId).first<{ request_hash: string; response_json: string; response_hash: string }>();
      if (!row) return null;
      if (row.request_hash !== requestHash) throw new ActivityError(409, 'equipment_health_request_conflict');
      try {
        const receipt = receiptSchema.parse(JSON.parse(row.response_json));
        if (await sha256Hex(canonicalJson(receipt)) !== row.response_hash || receipt.commandId !== command.placement.commandId
          || receipt.statVersion !== command.expectedStatVersion || receipt.targets.length !== receipt.health.length
          || new Set(receipt.targets.map(t => t.targetId)).size !== receipt.targets.length
          || new Set(receipt.targets.map(t => `${t.wearerKind}:${t.wearerId}`)).size !== receipt.targets.length) throw new Error('linked receipt mismatch');
        for (const [n, target] of receipt.targets.entries()) {
          const h = receipt.health[n]!;
          const eventId = await this.eventId(runId, requestHash, target);
          if (h.eventId !== eventId || h.targetId !== target.targetId || h.change.kind !== 'MAX_HP_REBASE'
            || h.change.maxHp !== target.maxHp || h.revision !== command.expectedHealthRevision + n + 1) throw new Error('linked target mismatch');
        }
        if(receipt.version!==(command.responseVersion??'equipment.health-placement.v1'))throw new Error('response version mismatch');
        if(receipt.version==='equipment.health-placement.v2.20261019'){
          const app=receipt.application,c=app.configuration;
          if(app.applicationId!==receipt.commandId||app.inventoryRevision!==receipt.placement.inventoryRevision
            ||app.healthRevision!==receipt.health.at(-1)!.revision||c.sourceVersion!==receipt.statVersion
            ||c.observedInventoryRevision!==command.placement.expectedInventoryRevision||c.actors.length!==receipt.targets.length
            ||c.actors.some((a,n)=>{const t=receipt.targets[n]!;return a.wearerKind!==t.wearerKind||a.wearerId!==t.wearerId||a.targetId!==t.targetId||a.stats.maxHp!==t.maxHp;}))throw new Error('application mismatch');
        }
        const eq = await this.equipment.prepareMove(accountId, runId, command.placement);
        const hp = await this.health.prepareChanges(accountId, runId, receipt.health.map((h, n) => ({ eventId: h.eventId,
          targetId: h.targetId, expectedRevision: command.expectedHealthRevision + n, change: h.change })));
        if (eq.statements.length || hp.statements.length || canonicalJson(eq.receipt) !== canonicalJson(receipt.placement)
          || canonicalJson(hp.receipts) !== canonicalJson(receipt.health)) throw new Error('linked originals missing');
        return receipt;
      } catch { throw new ActivityError(503, 'equipment_health_receipt_invalid'); }
    };
    const original = await replay();
    if (original) return original;
    const equip = await this.equipment.prepareMove(accountId, runId, command.placement);
    if (!equip.statements.length) throw new ActivityError(409, 'equipment_health_partial_replay');
    const inventory = await new EquipmentInventoryStore(this.db).readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    if (inventory.revision !== command.placement.expectedInventoryRevision) throw new ActivityError(409, 'equipment_revision_conflict');
    if(!equip.projection||canonicalJson(equip.projection.inventory)!==canonicalJson(inventory))throw new ActivityError(409,'equipment_revision_conflict');
    const assignments=structuredClone(equip.projection.assignments);
    const affected=new Set(assignments.map(a=>a.wearerKind+':'+a.wearerId));
    if (!this.source) throw new ActivityError(409, 'equipment_health_stat_required');
    const proof = await this.source.prepare({ accountId, runId, commandId: command.placement.commandId,
      expectedStatVersion: command.expectedStatVersion, assignments: structuredClone(assignments) });
    const targets = z.array(targetSchema).min(1).max(2).safeParse(proof?.targets);
    if (!targets.success || proof.statVersion !== command.expectedStatVersion || !Array.isArray(proof.assertions) || !proof.assertions.length
      || targets.data.length !== assignments.length || new Set(targets.data.map(t => t.targetId)).size !== targets.data.length
      || new Set(targets.data.map(t => `${t.wearerKind}:${t.wearerId}`)).size !== assignments.length
      || targets.data.some(t => !affected.has(`${t.wearerKind}:${t.wearerId}`))) throw new ActivityError(409, 'equipment_health_stat_invalid');
    const changes = [];
    for (const [n, a] of assignments.entries()) {
      const target = targets.data.find(t => t.wearerKind === a.wearerKind && t.wearerId === a.wearerId)!;
      const eventId = await this.eventId(runId, requestHash, target);
      changes.push({ eventId, expectedRevision: command.expectedHealthRevision + n, targetId: target.targetId,
        change: { kind: 'MAX_HP_REBASE' as const, maxHp: target.maxHp } });
    }
    const hp = await this.health.prepareChanges(accountId, runId, changes);
    if (!hp.statements.length) throw new ActivityError(409, 'equipment_health_partial_replay');
    let application;
    if(command.responseVersion){
      const parsedConfiguration=equipmentMaintenanceConfigurationSchema.safeParse(proof.configuration);
      if(!parsedConfiguration.success)throw new ActivityError(409,'equipment_health_configuration_required');
      const c=parsedConfiguration.data;
      if(c.sourceVersion!==proof.statVersion||c.observedInventoryRevision!==inventory.revision||c.actors.length!==assignments.length
        ||c.actors.some((a,n)=>{const assignment=assignments[n]!,target=targets.data.find(t=>t.wearerKind===a.wearerKind&&t.wearerId===a.wearerId);
          return a.wearerKind!==assignment.wearerKind||a.wearerId!==assignment.wearerId||canonicalJson(a.slots)!==canonicalJson(assignment.slots)
            ||!target||a.targetId!==target.targetId||a.stats.maxHp!==target.maxHp;}))throw new ActivityError(409,'equipment_health_configuration_invalid');
      application={applicationId:command.placement.commandId,status:'SERVER_COMMITTED_CLIENT_APPLY_REQUIRED',
        inventoryRevision:equip.receipt.inventoryRevision,healthRevision:hp.receipts.at(-1)!.revision,configuration:c};
    }
    const receipt = receiptSchema.parse({ version: command.responseVersion??'equipment.health-placement.v1', commandId: command.placement.commandId,
      statVersion: proof.statVersion, placement: equip.receipt,
      targets: assignments.map(a => targets.data.find(t => t.wearerKind === a.wearerKind && t.wearerId === a.wearerId)!), health: hp.receipts,
      ...(application?{application}:{}) });
    const responseJson = canonicalJson(receipt), responseHash = await sha256Hex(responseJson);
    if(new TextEncoder().encode(responseJson).length>512*1024)throw new ActivityError(409,'equipment_health_response_capacity');
    try {
      await this.db.batch([...proof.assertions, ...equip.statements, ...hp.statements,
        this.db.prepare('INSERT INTO run_equipment_health_links(run_id,command_id,request_hash,response_json,response_hash) VALUES(?,?,?,?,?)')
          .bind(runId, command.placement.commandId, requestHash, responseJson, responseHash)]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_health_state_conflict');
      throw error;
    }
    return receipt;
  }
  private async eventId(runId: string, requestHash: string, target: z.infer<typeof targetSchema>) {
    return `eqhp:${await sha256Hex(canonicalJson({ runId, requestHash, wearerKind: target.wearerKind,
      wearerId: target.wearerId, targetId: target.targetId }))}`;
  }
}
