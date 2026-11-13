import { firstEnemyTrainBodyDefinition, firstEnemyTrainBodyId } from './combat-first-enemy-spatial';
import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { CombatHealthStore } from './combat-health-store';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryFiveEquipmentSource } from './combat-ordinary-equipment-source';
import { OrdinaryFiveStatOwner, stateSchema } from './combat-ordinary-stat-owner';
import { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';
import { readOrdinaryRailFromOriginal } from './combat-ordinary-rail-source';
import { readOrdinaryRoofFromOriginal } from './combat-ordinary-roof-source';
import { readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import type { OrdinaryNodeStartOwner } from './ordinary-node-attempt';
import { ordinaryNodeExecutionOriginSchema } from './ordinary-node-execution-origin';
import { readOrdinaryNodeActions, cooldownTimeSpanToNodeSteps } from './ordinary-node-actions';
import { readOrdinaryRelocationFromOriginal } from './combat-ordinary-relocation-source';
import { prepareOrdinaryNodeEnemies } from './ordinary-node-enemies';
import { readOrdinaryRunAutomation } from './ordinary-run-automation';
import { OrdinaryNativeGeometryRegistry } from './ordinary-native-geometry';
import { prepareOrdinaryObstacleSequence, type OrdinaryObstacleSequencePin } from './ordinary-obstacle-sequence';

const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (reason: string): never => { throw new ActivityError(409, 'ordinary_node_' + reason); };

/** Current owner reads, used only at verified entry. Never called to replace a saved node origin on restart. */
export async function readOrdinaryNodeEntryOwners(db: D1Database, accountId: string, runId: string) {
  const original = await readOrdinaryRunStatOrigin(db, accountId, runId);
  const automation = await readOrdinaryRunAutomation(db, accountId, runId);
  const health = await new CombatHealthStore(db).read(accountId, runId);
  const healthRow = await db.prepare('SELECT revision,initial_hash,start_snapshot_json,state_json,actor_state_json FROM combat_damage_states WHERE run_id=?')
    .bind(runId).first<{ revision: number; initial_hash: string; start_snapshot_json: string; state_json: string; actor_state_json: string | null }>();
  if (!healthRow || healthRow.start_snapshot_json !== original.startSnapshotJson || healthRow.revision !== health.revision
    || healthRow.actor_state_json !== null || json(health.state) !== healthRow.state_json) return fail('health_source_changed');
  const equipment = await readOrdinaryFiveEquipmentSource(db, accountId, runId);
  const row = await db.prepare('SELECT revision,origin_hash,source_json,result_json FROM ordinary_run_stat_states WHERE run_id=? AND account_id=?')
    .bind(runId, accountId).first<{ revision: number; origin_hash: string; source_json: string; result_json: string }>();
  if (!row || row.origin_hash !== original.originHash) return fail('stat_source_missing');
  const source = stateSchema.parse(JSON.parse(row.source_json));
  const stored = await (await OrdinaryFiveStatOwner.fromOriginal(original.origin, original.originHash)).resolve(source);
  if (row.revision !== source.revision || json(stored) !== row.result_json || source.inventoryRevision !== equipment.inventoryRevision
    || source.actors.some(a => json(a.slots) !== json(equipment.actors.find(e => e.operatorId === a.operatorId)?.slots ?? null)))
    return fail('stat_equipment_source_changed');
  const train = await readOrdinaryTrainFromOriginal(original), rail = await readOrdinaryRailFromOriginal(original, train),
    roof = await readOrdinaryRoofFromOriginal(original, rail), kit = await readOrdinaryKitFromOriginal(original);
  if (!train || !rail || !roof || !kit) return fail('entry_definition_missing');
  const relocation = await readOrdinaryRelocationFromOriginal(original, undefined, kit);
  const expectedIds = [...original.origin.actors.map(a => a.operatorId), train.packet.train.trainId];
  if (health.state.targets.length !== 6 || health.state.targets.some(t => !expectedIds.includes(t.id))) return fail('six_target_source_conflict');
  const hp = (id: string) => health.state.targets.find(t => t.id === id)!.health;
  // Re-evaluate HP-conditional stats at entry without overwriting the persisted stat or HP owner.
  const entrySource = { ...source, actors: source.actors.map(a => ({ ...a, currentHp: hp(a.operatorId).hp })) };
  const current = await (await OrdinaryFiveStatOwner.fromOriginal(original.origin, original.originHash)).resolve(entrySource);
  if (current.actors.some(a => a.effectiveStats.maxHp !== hp(a.operatorId).maxHp)) return fail('entry_maximum_conflict');
  const entry = {
    ...(automation ? { automation: automation.state } : {}),
    accountId, runId, startSnapshotHash: await sha256Hex(original.startSnapshotJson),
    owners: { healthRevision: health.revision, statRevision: row.revision, inventoryRevision: equipment.inventoryRevision },
    actors: original.origin.actors.map(a => ({ operatorId: a.operatorId, slot: a.slot, hp: hp(a.operatorId).hp,
      maxHp: hp(a.operatorId).maxHp, slots: source.actors.find(s => s.operatorId === a.operatorId)!.slots,
      effectiveStats: current.actors.find(s => s.operatorId === a.operatorId)!.effectiveStats })),
    train: { trainId: train.packet.train.trainId, hp: hp(train.packet.train.trainId).hp, maxHp: hp(train.packet.train.trainId).maxHp },
    definitions: { originHash: original.originHash, kitHash: kit.kitHash, trainHash: train.sourceHash,
      railHash: rail.sourceHash, roofHash: roof.sourceHash, relocationHash: relocation.relocationHash },
    modifierSource: source.modifiers,
  };
  const assertions = [...equipment.assertions, ...(automation ? [automation.assertion] : []),
    db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_damage_states
      WHERE run_id=? AND revision=? AND initial_hash=? AND start_snapshot_json=? AND state_json=? AND actor_state_json IS NULL)
      AND EXISTS(SELECT 1 FROM ordinary_run_stat_states WHERE run_id=? AND account_id=? AND revision=? AND origin_hash=? AND source_json=? AND result_json=?)
      AND EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=? AND account_id=? AND phase='RUNNING' AND origin_hash=? AND start_snapshot_json=?)
      THEN 1 ELSE 0 END`).bind(runId, healthRow.revision, healthRow.initial_hash, original.startSnapshotJson, healthRow.state_json,
        runId, accountId, row.revision, row.origin_hash, row.source_json, row.result_json,
        runId, accountId, original.originHash, original.startSnapshotJson),
    db.prepare('DELETE FROM equipment_placement_write_guard')];
  return { entry, relocation, actorStats: current.actors, assertions, startSnapshot: original.startSnapshotJson, original };
}

const runtimeSchema = z.strictObject({ executionVersion: z.string().min(1).max(200), sequence: z.json().optional(),
  boardingSourceHash: z.string().regex(/^[a-f0-9]{64}$/u).optional(),
  effects: z.record(z.string(), z.json()), actions: z.record(z.string(), z.json()),
  spatial: z.record(z.string(), z.json()), enemies: z.record(z.string(), z.json()), determinism: z.record(z.string(), z.json()) });
/** Actual entry execution owner, never raw client data. It proves the finalized node-entry effects,
 * actions, geometry/enemy schedules and original random source, including each modifier source's freshness. */
export interface OrdinaryNodeEntryRuntimeOwner {
  prepare(context: Parameters<OrdinaryNodeStartOwner['prepare']>[0] & {
    entry: Awaited<ReturnType<typeof readOrdinaryNodeEntryOwners>>['entry'];
    relocation: Awaited<ReturnType<typeof readOrdinaryNodeEntryOwners>>['relocation'];
    enemies?: Awaited<ReturnType<typeof prepareOrdinaryNodeEnemies>>;
    sequence?: OrdinaryObstacleSequencePin;
    geometry?: { definition: Awaited<ReturnType<OrdinaryNativeGeometryRegistry['read']>>['definition'];
      binding: { version: string; accountId: string; runId: string; nodeId: string; entryMapRevision: number;
        originGeneration: 1; definitionRef: Awaited<ReturnType<OrdinaryNativeGeometryRegistry['read']>>['reference'] } }
  }): Promise<{ state: unknown; assertions: D1PreparedStatement[] }>;
}
export class OrdinaryNodeEntrySource implements OrdinaryNodeStartOwner {
  constructor(private readonly db: D1Database, private readonly runtime?: OrdinaryNodeEntryRuntimeOwner) {}
  async prepare(context: Parameters<OrdinaryNodeStartOwner['prepare']>[0]) {
    const owned = await readOrdinaryNodeEntryOwners(this.db, context.accountId, context.runId);
    if (owned.startSnapshot !== context.startSnapshot) return fail('entry_start_conflict');
    if (!this.runtime) return fail('runtime_entry_unavailable');
    const origin = ordinaryNodeExecutionOriginSchema.safeParse(context.executionOrigin);
    if (!origin.success || !context.arrivalEventId) return fail('execution_origin_unavailable');
    const enemies = context.firstPlayableScaling ? await prepareOrdinaryNodeEnemies(owned.original, context) : undefined;
    const start = JSON.parse(owned.startSnapshot) as Record<string, unknown>;
    const sequence = await prepareOrdinaryObstacleSequence(start, context.nodeId, enemies);
    const geometry = Object.hasOwn(start, 'ordinaryGeometryReference')
      ? await new OrdinaryNativeGeometryRegistry(this.db, start.ordinaryGeometryReference).read() : undefined;
    if (enemies && geometry?.definition.entities.some(e => e.definitionRef === firstEnemyTrainBodyId)) {
      const body = await firstEnemyTrainBodyDefinition();
      if (!enemies?.spatial || enemies.spatial.definition.version !== body.spatialReference.version
        || enemies.spatial.definition.definitionHash !== body.spatialReference.definitionHash
        || enemies.spatial.definition.geometry.definitionHash !== body.geometryReference.definitionHash)
        return fail('enemy_body_source_conflict');
    }
    const geometryBinding = geometry ? { version: 'ordinary.node-geometry-binding.v1.20261031',
      accountId: context.accountId, runId: context.runId, nodeId: context.nodeId, entryMapRevision: context.mapRevision,
      originGeneration: 1 as const, definitionRef: geometry.reference } : undefined;
    const runtime = await this.runtime.prepare({ ...structuredClone(context), entry: structuredClone(owned.entry),
      relocation: structuredClone(owned.relocation), ...(enemies ? { enemies: structuredClone(enemies) } : {}),
      ...(sequence ? { sequence: structuredClone(sequence) } : {}),
      ...(geometry && geometryBinding ? { geometry: { definition: structuredClone(geometry.definition), binding: structuredClone(geometryBinding) } } : {}) });
    if (!Array.isArray(runtime.assertions) || !runtime.assertions.length) return fail('runtime_entry_unverified');
    const state = runtimeSchema.parse(runtime.state);
    if (json(state.sequence ?? null) !== json(sequence ?? null)) return fail('obstacle_sequence_conflict');
    if (json(state.spatial.definitionBinding ?? null) !== json(geometryBinding ?? null)) return fail('geometry_binding_conflict');
    if (enemies && json(state.enemies) !== json(enemies)) return fail('enemy_source_conflict');
    if (state.executionVersion !== origin.data.executionVersion || json(state.determinism) !== json(origin.data.determinism))
      return fail('execution_origin_conflict');
    const actions = readOrdinaryNodeActions(state.actions, owned.entry.actors.map(a => a.operatorId));
    if (owned.entry.automation && actions.ultimateAuto !== owned.entry.automation.ultimateAuto)
      return fail('automation_source_conflict');
    if (owned.entry.automation && actions.actors.some(a => {
      const original = owned.entry.automation!.actors.find(o => o.operatorId === a.operatorId);
      return !original || a.activeCooldownRemainingTicks !== cooldownTimeSpanToNodeSteps(original.activeCooldownRemainingTicks, actions.stepTimeSpanTicks)
        || a.ultimateCharge !== original.ultimateCharge;
    })) return fail('actions_source_conflict');
    return { snapshot: { version: 'ordinary.node-entry-state.v1.20261028', ...owned.entry,
      nodeId: context.nodeId, entryMapRevision: context.mapRevision,
      runtime: { ...state, actions: { ...actions, stats: owned.actorStats.map(actor => ({
        operatorId: actor.operatorId, conditionalDefenseFlat: actor.conditionalDefenseFlat, skillMagnitude: actor.skillMagnitude,
      })) } } },
    assertions: [...owned.assertions, ...(geometry ? [geometry.assertion] : []), ...runtime.assertions] };
  }
}
