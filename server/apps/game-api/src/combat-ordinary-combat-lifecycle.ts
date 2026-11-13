import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { CombatHealthStore, type CombatHealthVerifier } from './combat-health-store';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryInitialModifiers } from './combat-ordinary-initial-modifiers';
import { readOrdinaryFiveEquipmentSource } from './combat-ordinary-equipment-source';
import { OrdinaryFiveStatOwner, readOrdinaryFiveStatOrigin, stateSchema, type SourceState } from './combat-ordinary-stat-owner';
import { prepareOrdinaryInitialHealthSource } from './combat-ordinary-initial-health-source';
import { readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import { readOrdinaryBasicFromOriginal } from './combat-ordinary-basic-source';
import { readOrdinaryRelocationFromOriginal } from './combat-ordinary-relocation-source';
import { readFirstBattleEncounterFromOriginal } from './combat-first-battle-source';
import { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';
import { readOrdinaryRailFromOriginal } from './combat-ordinary-rail-source';
import { readOrdinaryRoofFromOriginal } from './combat-ordinary-roof-source';
import { readOrdinaryRunAutomation } from './ordinary-run-automation';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u), hp = z.number().int().nonnegative().safe();
const commandSchema = z.union([z.strictObject({ requestId: id,
  initialTargets: z.array(z.strictObject({ id, hp, maxHp: hp.min(1) })).length(5) }),
z.strictObject({ requestId: id, source: z.literal('ORIGINAL_START') })]);
const version = 'combat.ordinary-phase-initialization.v1.20261023';
const json = (x: unknown) => canonicalJson(z.json().parse(x));
const fail = (reason: string): never => { throw new ActivityError(409, `ordinary_combat_${reason}`); };
type Health = Awaited<ReturnType<CombatHealthStore['read']>>;
type Stats = Awaited<ReturnType<OrdinaryFiveStatOwner['resolve']>>;
type Receipt = { version: typeof version; runId: string; phase: 'RUNNING'; health: Health; stats: Stats;
  ordinaryAutomation?: NonNullable<Awaited<ReturnType<typeof readOrdinaryRunAutomation>>>['state'];
  sourceState?: SourceState; // Historical receipts remain immutable and may lack this initial-state proof.
  firstBattleEncounterBinding?: { version: string; originHash: string; sourceHash: string };
  ordinaryTrainBinding?: { version: string; ordinaryOriginHash: string; sourceHash: string };
  ordinaryRailBinding?: { version: string; ordinaryOriginHash: string; sourceHash: string };
  ordinaryRoofBinding?: { version: string; ordinaryOriginHash: string; sourceHash: string };
  trainInitialState?: ReturnType<typeof prepareOrdinaryInitialHealthSource>['trainInitialState'];
  ordinaryKitBinding?: { version: string; originHash: string; kitHash: string };
  ordinaryBasicBinding?: { version: string; originHash: string; kitHash: string; basicHash: string };
  ordinaryRelocationBinding?: { version: string; originHash: string; basicHash: string; relocationHash: string } };

/** The recovery route supplies one SQL snapshot. This is initialization state,
 * never permission to create a fresh simulation or replace current checkpoint HP. */
export async function projectOrdinaryInitialization(accountId: string, runId: string, startSnapshotJson: string | null,
  phase: { accountId: string; phase: string; revision: number; originHash: string; startSnapshotJson: string } | null) {
  const unavailable = { version: 'combat.ordinary-initialization-state.v1.20261025', status: 'UNAVAILABLE' as const,
    revision: null, originHash: null, runtimeExecutionVerified: false as const };
  if (startSnapshotJson === null) return unavailable;
  try {
    const snapshot = z.record(z.string(), z.unknown()).parse(JSON.parse(startSnapshotJson));
    if (!phase && !Object.hasOwn(snapshot, 'ordinaryStatSource') && !Object.hasOwn(snapshot, 'ordinaryKitSource'))
      return { ...unavailable, status: 'NOT_APPLICABLE' as const };
    if (!phase || phase.accountId !== accountId || phase.startSnapshotJson !== startSnapshotJson
      || !((phase.phase === 'PREPARED' && phase.revision === 1) || (phase.phase === 'RUNNING' && phase.revision === 3)))
      return unavailable;
    const source = z.strictObject({ origin: z.unknown(), originHash: z.string() }).parse(snapshot.ordinaryStatSource);
    const origin = await readOrdinaryFiveStatOrigin(source.origin, source.originHash);
    if (origin.accountId !== accountId || origin.runId !== runId || source.originHash !== phase.originHash) return unavailable;
    return { ...unavailable, status: phase.phase as 'PREPARED' | 'RUNNING', revision: phase.revision, originHash: phase.originHash };
  } catch { return unavailable; }
}

/** Owns the new ordinary-run execution barrier. START creates PREPARED; this
 * single transaction initializes the existing HP owner and derived stats, then
 * releases the barrier. RUNNING permits execution but does not claim that all
 * mode handlers are installed. No public route or guessed initial HP is supplied.
 * The same existing HP verifier must prove the initial values from real sources. */
export class OrdinaryCombatLifecycle {
  constructor(private readonly db: D1Database, private readonly healthVerifier?: CombatHealthVerifier) {}
  async activate(accountId: string, runId: string, input: unknown): Promise<Receipt> {
    id.parse(accountId); id.parse(runId);
    const command = commandSchema.parse(input);
    const original = await readOrdinaryRunStatOrigin(this.db, accountId, runId);
    const requestHash = await sha256Hex(json({ accountId, runId, command }));
    const replay = async (): Promise<Receipt | null> => {
      const row = await this.db.prepare('SELECT request_hash,response_json,response_hash FROM ordinary_combat_activation_receipts WHERE run_id=? AND request_id=?')
        .bind(runId, command.requestId).first<{ request_hash: string; response_json: string; response_hash: string }>();
      if (!row) return null;
      if (row.request_hash !== requestHash) return fail('request_conflict');
      if (await sha256Hex(row.response_json) !== row.response_hash) return fail('receipt_invalid');
      const receipt: Receipt = JSON.parse(row.response_json);
      if (receipt.version !== version || receipt.runId !== runId || receipt.phase !== 'RUNNING') return fail('receipt_invalid');
      return receipt;
    };
    const prior = await replay(); if (prior) return prior;
    const firstBattleEncounterSource = await readFirstBattleEncounterFromOriginal(original);
    const trainSource = await readOrdinaryTrainFromOriginal(original);
    const railSource = await readOrdinaryRailFromOriginal(original, trainSource);
    const roofSource = await readOrdinaryRoofFromOriginal(original, railSource);
    if (trainSource?.packet.version === 'combat.ordinary-train-source.v2.20261027' && !('source' in command))
      return fail('original_initialization_required');
    const phase = await this.db.prepare('SELECT phase,origin_hash,start_snapshot_json FROM ordinary_combat_phases WHERE run_id=? AND account_id=?')
      .bind(runId, accountId).first<{ phase: string; origin_hash: string; start_snapshot_json: string }>();
    if (phase?.phase !== 'PREPARED' || phase.origin_hash !== original.originHash || phase.start_snapshot_json !== original.startSnapshotJson) return fail('phase_conflict');
    const ordinaryKitSource = 'source' in command ? await readOrdinaryKitFromOriginal(original) : undefined;
    const originalSnapshot = JSON.parse(original.startSnapshotJson);
    // Historical STARTs retain their old contract; never manufacture new profiles.
    const ordinaryBasicSource = ordinaryKitSource && Object.hasOwn(originalSnapshot, 'ordinaryBasicSource')
      ? await readOrdinaryBasicFromOriginal(original, ordinaryKitSource) : undefined;
    const ordinaryRelocationSource = 'source' in command && Object.hasOwn(originalSnapshot, 'ordinaryRelocationSource')
      ? await readOrdinaryRelocationFromOriginal(original, ordinaryBasicSource, ordinaryKitSource) : undefined;
    const initial = await readOrdinaryInitialModifiers(this.db, accountId, runId);
    const automation = await readOrdinaryRunAutomation(this.db, accountId, runId);
    const equipment = await readOrdinaryFiveEquipmentSource(this.db, accountId, runId);
    const production = 'source' in command ? prepareOrdinaryInitialHealthSource(this.db, original, equipment, trainSource) : null;
    const initialTargets = 'initialTargets' in command ? command.initialTargets : production!.targets;
    const health = await new CombatHealthStore(this.db, production?.verifier ?? this.healthVerifier).prepareInitialization(accountId, runId, initialTargets);
    if (!health.statements.length) return fail('partial_initialization');
    const source = stateSchema.parse({ accountId, runId, revision: 1, inventoryRevision: equipment.inventoryRevision,
      modifiers: initial.modifiers, actors: original.origin.actors.map(actor => {
        const target = health.snapshot.state.targets.find(t => t.id === actor.operatorId);
        if (!target) return fail('initial_target_conflict');
        return { operatorId: actor.operatorId, currentHp: target.health.hp,
          slots: equipment.actors.find(a => a.operatorId === actor.operatorId)!.slots };
      }) });
    const stats = await (await OrdinaryFiveStatOwner.fromOriginal(original.origin, original.originHash)).resolve(source);
    if (stats.actors.some(a => a.effectiveStats.maxHp !== health.snapshot.state.targets.find(t => t.id === a.operatorId)!.health.maxHp)) return fail('initial_maximum_conflict');
    const receipt: Receipt = { version, runId, phase: 'RUNNING', health: health.snapshot, stats, sourceState: source,
      ...(automation ? { ordinaryAutomation: automation.state } : {}),
      ...(production?.trainInitialState ? { trainInitialState: production.trainInitialState } : {}),
      ...(roofSource ? { ordinaryRoofBinding: { version: roofSource.packet.version,
        ordinaryOriginHash: original.originHash, sourceHash: roofSource.sourceHash } } : {}),
      ...(railSource ? { ordinaryRailBinding: { version: railSource.packet.version,
        ordinaryOriginHash: railSource.packet.ordinaryOriginHash, sourceHash: railSource.sourceHash } } : {}),
      ...(trainSource ? { ordinaryTrainBinding: { version: trainSource.packet.version,
        ordinaryOriginHash: trainSource.packet.ordinaryOriginHash, sourceHash: trainSource.sourceHash } } : {}),
      ...(firstBattleEncounterSource ? { firstBattleEncounterBinding: {
        version: firstBattleEncounterSource.packet.version, originHash: firstBattleEncounterSource.packet.originHash,
        sourceHash: firstBattleEncounterSource.sourceHash } } : {}),
      ...(ordinaryKitSource ? { ordinaryKitBinding: { version: ordinaryKitSource.packet.version,
        originHash: ordinaryKitSource.packet.originHash, kitHash: ordinaryKitSource.kitHash } } : {}),
      ...(ordinaryBasicSource ? { ordinaryBasicBinding: { version: ordinaryBasicSource.packet.version,
        originHash: ordinaryBasicSource.packet.originHash, kitHash: ordinaryBasicSource.packet.kitHash,
        basicHash: ordinaryBasicSource.basicHash } } : {}),
      ...(ordinaryRelocationSource ? { ordinaryRelocationBinding: { version: ordinaryRelocationSource.packet.version,
        originHash: ordinaryRelocationSource.packet.originHash, basicHash: ordinaryRelocationSource.packet.basicHash,
        relocationHash: ordinaryRelocationSource.relocationHash } } : {}) };
    const responseJson = json(receipt), responseHash = await sha256Hex(responseJson);
    try {
      await this.db.batch([
        ...(automation ? [automation.assertion, this.db.prepare('DELETE FROM operation_map_write_guard')] : []),
        ...(production ? [] : equipment.assertions),
        this.db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(
          SELECT 1 FROM ordinary_combat_phases p JOIN runs r ON r.id=p.run_id JOIN accounts a ON a.id=r.account_id
          JOIN run_snapshots s ON s.run_id=r.id WHERE p.run_id=? AND p.account_id=? AND p.phase='PREPARED' AND p.revision=1
          AND p.origin_hash=? AND p.start_snapshot_json=? AND s.snapshot_json=p.start_snapshot_json AND r.status='ACTIVE' AND a.status='ACTIVE')
          AND NOT EXISTS(SELECT 1 FROM ordinary_run_stat_states WHERE run_id=?)
          AND NOT EXISTS(SELECT 1 FROM run_augment_history WHERE run_id=?)
          AND NOT EXISTS(SELECT 1 FROM solo_augment_grants WHERE run_id=?)
          AND NOT EXISTS(SELECT 1 FROM run_augment_effect_originals WHERE run_id=?)
          THEN 1 ELSE 0 END`).bind(runId, accountId, original.originHash, original.startSnapshotJson, runId, runId, runId, runId),
        this.db.prepare("UPDATE ordinary_combat_phases SET phase='INITIALIZING',revision=2 WHERE run_id=? AND phase='PREPARED'").bind(runId),
        ...health.statements,
        this.db.prepare('INSERT INTO ordinary_run_stat_states VALUES(?,?,?,?,?,?)')
          .bind(runId, accountId, 1, original.originHash, json(source), json(stats)),
        this.db.prepare("UPDATE ordinary_combat_phases SET phase='RUNNING',revision=3 WHERE run_id=? AND phase='INITIALIZING'").bind(runId),
        this.db.prepare('INSERT INTO ordinary_combat_activation_receipts VALUES(?,?,?,?,?)')
          .bind(runId, command.requestId, requestHash, responseJson, responseHash),
        this.db.prepare('DELETE FROM departure_write_guard'), this.db.prepare('DELETE FROM equipment_placement_write_guard'),
      ]);
    } catch (error) {
      const committed = await replay(); if (committed) return committed;
      if (/CHECK|UNIQUE|ordinary_phase/iu.test(String(error))) return fail('initialization_conflict');
      throw new ActivityError(503, 'ordinary_combat_initialization_outcome_unknown');
    }
    return receipt;
  }
}
