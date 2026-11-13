import { z } from 'zod';
import { canonicalJson, HealthRebaseState, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { CombatHealthStore, type CombatHealthVerifier } from './combat-health-store';
import { calculateEquipmentCoreStats } from './equipment-run-stat-owner';
import { OrdinaryFiveStatOwner } from './combat-ordinary-stat-owner';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryFiveEquipmentSource } from './combat-ordinary-equipment-source';

const commandSchema = z.strictObject({ requestId: z.string().regex(/^[\x21-\x7e]{1,128}$/u),
  expectedRevision: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER - 1) });
const version = 'combat.ordinary-five-stat-application.v1.20261023' as const;
const json = (value: unknown) => canonicalJson(z.json().parse(value));
type Stats = Awaited<ReturnType<OrdinaryFiveStatOwner['resolve']>>;
type Receipt = { version: typeof version; runId: string; revision: number; healthRevision: number; stats: Stats };

/** Internal mode-owned modifier source. Missing is never equivalent to no effects.
 * Proofs must fence all four source groups in the stat application's DB batch.
 * No default absence producer: initial START state does not prove current effects.
 * Ongoing combat requires its own authority. Never accept HTTP modifiers. */
export interface OrdinaryStatModifierSource {
  prepare(context: { accountId: string; runId: string; originHash: string; healthRevision: number; inventoryRevision: number }):
    Promise<{ modifiers: unknown; assertions: D1PreparedStatement[] }>;
}

/** Owns derived core-stat revision/receipt only, never current HP or equipment.
 * The original START, current health, placement/effect and modifier assertions
 * are checked atomically with publishing this derived snapshot. */
export class OrdinaryRunStatStore {
  constructor(private readonly db: D1Database, private readonly modifiers?: OrdinaryStatModifierSource,
    private readonly healthVerifier?: CombatHealthVerifier) {}
  async apply(accountId: string, runId: string, input: unknown): Promise<Receipt> {
    const command = commandSchema.parse(input);
    const source = await readOrdinaryRunStatOrigin(this.db, accountId, runId);
    const requestHash = await sha256Hex(json({ accountId, runId, command }));
    const replay = async (): Promise<Receipt | null> => {
      const row = await this.db.prepare('SELECT account_id,request_hash,response_json,response_hash FROM ordinary_run_stat_receipts WHERE run_id=? AND request_id=?')
        .bind(runId, command.requestId).first<{ account_id: string; request_hash: string; response_json: string; response_hash: string }>();
      if (!row) return null;
      if (row.account_id !== accountId || row.request_hash !== requestHash) throw new ActivityError(409, 'ordinary_stat_request_conflict');
      if (await sha256Hex(row.response_json) !== row.response_hash) throw new ActivityError(503, 'ordinary_stat_receipt_invalid');
      const receipt: Receipt = JSON.parse(row.response_json);
      if (receipt.version !== version || receipt.runId !== runId || receipt.revision !== command.expectedRevision + 1) throw new ActivityError(503, 'ordinary_stat_receipt_invalid');
      return receipt;
    };
    const previous = await replay(); if (previous) return previous;
    if (!this.modifiers) throw new ActivityError(409, 'ordinary_stat_modifier_source_unavailable');
    const current = await this.db.prepare('SELECT revision,origin_hash,source_json,result_json FROM ordinary_run_stat_states WHERE run_id=? AND account_id=?')
      .bind(runId, accountId).first<{ revision: number; origin_hash: string; source_json: string; result_json: string }>();
    if ((current?.revision ?? 0) !== command.expectedRevision || current && current.origin_hash !== source.originHash) throw new ActivityError(409, 'ordinary_stat_revision_conflict');
    const healthOwner = new CombatHealthStore(this.db, this.healthVerifier);
    const health = await healthOwner.read(accountId, runId);
    const equipment = await readOrdinaryFiveEquipmentSource(this.db, accountId, runId);
    const proof = await this.modifiers.prepare({ accountId, runId, originHash: source.originHash,
      healthRevision: health.revision, inventoryRevision: equipment.inventoryRevision });
    if (!proof.assertions.length) throw new ActivityError(409, 'ordinary_stat_modifier_proof_unavailable');
    const nextRevision = command.expectedRevision + 1;
    const state = { accountId, runId, revision: nextRevision, inventoryRevision: equipment.inventoryRevision,
      modifiers: proof.modifiers, actors: source.origin.actors.map(actor => {
        // Ordinary five-actor contract binds health target IDs to the pinned operator IDs.
        const target = health.state.targets.find(t => t.id === actor.operatorId);
        if (!target) throw new ActivityError(409, 'ordinary_stat_health_target_missing');
        return { operatorId: actor.operatorId, currentHp: target.health.hp,
          slots: equipment.actors.find(a => a.operatorId === actor.operatorId)!.slots };
      }) };
    // Project through the existing HP owner policy before conditional stats. In
    // particular a maximum decrease may leave the OLD HP above the NEW maximum.
    // No writes occur here; prepareChanges independently validates the same change.
    const changes: { eventId: string; expectedRevision: number; targetId: string;
      change: { kind: 'MAX_HP_REBASE'; maxHp: number } }[] = [];
    for (const actor of state.actors) {
      const base = source.origin.actors.find(a => a.operatorId === actor.operatorId)!.permanentStats;
      const maximum = calculateEquipmentCoreStats({ maxHp: base.maxHp, attack: base.attack,
        defense: { numerator: String(base.defense), denominator: '1' } },
      actor.slots.filter(i => i !== null).map(i => ({ categoryId: i.categoryId, star: i.star, setTag: i.setTag }))).maxHp;
      const before = health.state.targets.find(t => t.id === actor.operatorId)!.health;
      if (before.maxHp === maximum) continue;
      const candidate = HealthRebaseState.restore(before);
      actor.currentHp = candidate.rebaseMaximum(maximum).hp;
      changes.push({ eventId: `ordinary-stat:${await sha256Hex(json({ requestId: command.requestId, targetId: actor.operatorId }))}`,
        expectedRevision: health.revision + changes.length, targetId: actor.operatorId,
        change: { kind: 'MAX_HP_REBASE' as const, maxHp: maximum } });
    }
    const statOwner = await OrdinaryFiveStatOwner.fromOriginal(source.origin, source.originHash);
    if (current) {
      const restored = await statOwner.resolve(JSON.parse(current.source_json));
      if (restored.revision !== current.revision || json(restored) !== current.result_json) throw new ActivityError(503, 'ordinary_stat_storage_invalid');
    }
    const stats = await statOwner.resolve(state);
    // This verifier is private to this stat transaction: equipment and modifier
    // assertions below execute exactly once, before the HP statements. Never
    // expose it as a general HP setter or run its statements independently.
    // The stat owner above has validated every source and the resulting maximum.
    const maximumVerifier: CombatHealthVerifier = { prepare: async context => {
      if (context.kind !== 'CHANGE' || context.accountId !== accountId || context.runId !== runId
        || context.startSnapshotJson !== source.startSnapshotJson || context.batchBaseRevision !== health.revision
        || !context.command || context.command.change.kind !== 'MAX_HP_REBASE'
        || !changes.some(change => json(change) === json(context.command)))
        throw new ActivityError(409, 'ordinary_stat_maximum_proof_invalid');
      return { assertions: [this.db.prepare(`INSERT INTO combat_damage_write_guard(valid)
        SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases
          WHERE run_id=? AND account_id=? AND phase='RUNNING' AND revision=3
          AND origin_hash=? AND start_snapshot_json=?) THEN 1 ELSE 0 END`)
        .bind(runId, accountId, source.originHash, source.startSnapshotJson)] };
    } };
    const rebased = changes.length ? await new CombatHealthStore(this.db, this.healthVerifier ?? maximumVerifier)
      .prepareChanges(accountId, runId, changes) : null;
    // A historical health receipt cannot authorize a fresh derived-state write.
    if (rebased && !rebased.statements.length) throw new ActivityError(409, 'ordinary_stat_health_partial_replay');
    if (stats.actors.some(a => {
      const final = rebased?.receipts.find(r => r.targetId === a.operatorId)?.after
        ?? health.state.targets.find(t => t.id === a.operatorId)!.health;
      return final.maxHp !== a.effectiveStats.maxHp || final.hp !== a.currentHp;
    }))
      throw new ActivityError(409, 'ordinary_stat_health_rebase_required');
    const receipt: Receipt = { version, runId, revision: nextRevision, healthRevision: health.revision + changes.length, stats };
    const responseJson = json(receipt), responseHash = await sha256Hex(responseJson);
    try {
      await this.db.batch([
        ...equipment.assertions, ...proof.assertions,
        this.db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(
          SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
          JOIN combat_damage_states h ON h.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE'
          AND a.status='ACTIVE' AND s.snapshot_json=? AND h.start_snapshot_json=s.snapshot_json
          AND h.revision=? AND h.state_json=? AND h.actor_state_json IS NULL)
          AND COALESCE((SELECT revision FROM ordinary_run_stat_states WHERE run_id=?),0)=?
          AND (SELECT source_json FROM ordinary_run_stat_states WHERE run_id=?) IS ?
          AND (SELECT result_json FROM ordinary_run_stat_states WHERE run_id=?) IS ?
          AND (SELECT origin_hash FROM ordinary_run_stat_states WHERE run_id=?) IS ?
          THEN 1 ELSE 0 END`).bind(runId, accountId, source.startSnapshotJson, health.revision, json(health.state), runId, command.expectedRevision,
            runId, current?.source_json ?? null, runId, current?.result_json ?? null, runId, current?.origin_hash ?? null),
        ...rebased?.statements ?? [],
        this.db.prepare(`INSERT INTO ordinary_run_stat_states(run_id,account_id,revision,origin_hash,source_json,result_json) VALUES(?,?,?,?,?,?)
          ON CONFLICT(run_id) DO UPDATE SET revision=excluded.revision,source_json=excluded.source_json,result_json=excluded.result_json`)
          .bind(runId, accountId, nextRevision, source.originHash, json(state), json(stats)),
        this.db.prepare('INSERT INTO ordinary_run_stat_receipts VALUES(?,?,?,?,?,?)').bind(runId, command.requestId, accountId, requestHash, responseJson, responseHash),
        this.db.prepare('DELETE FROM departure_write_guard'), this.db.prepare('DELETE FROM equipment_placement_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay(); if (completed) return completed;
      if (/CHECK|UNIQUE/iu.test(String(error))) throw new ActivityError(409, 'ordinary_stat_source_changed');
      throw new ActivityError(503, 'ordinary_stat_outcome_unknown');
    }
    return receipt;
  }
}
