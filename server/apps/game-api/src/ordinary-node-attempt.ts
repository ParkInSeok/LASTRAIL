import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { captureOperationMap, projectOperationMap } from './operation-map-plan';
import { readMapProgress, type firstPlayableScalingSchema } from './operation-map-state';
import { readDepartureMap } from './operation-map-departure';
import { verifyPinnedOperationMapContent } from './operation-map-publisher';
import { issueOrdinaryNodeExecutionOrigin, type OrdinaryNodeExecutionOrigin } from './ordinary-node-execution-origin';

const id = z.string().min(1).max(200), revision = z.number().int().positive().safe();
export const nodeRestartCommand = z.strictObject({ requestId: id, nodeId: id, expectedRevision: revision, expectedGeneration: revision });
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (code: string): never => { throw new ActivityError(409, 'ordinary_node_' + code); };
const restartedReceipt = z.strictObject({ version: z.literal('ordinary.node-restart.v1.20261028'), requestId: id,
  accountId: id, runId: id, nodeId: id, status: z.literal('RESTARTED'), revision, generation: revision,
  snapshot: z.record(z.string(), z.json()), snapshotHash: z.string().regex(/^[a-f0-9]{64}$/u) });
type RestartResult = z.infer<typeof restartedReceipt> | { version: 'ordinary.node-restart.v1.20261028'; requestId: string;
  accountId: string; runId: string; nodeId: string; status: 'RESULT_AVAILABLE'; runStatus: string;
  revision: number; state: ReturnType<typeof readMapProgress>; generation: null };
type Origin = { start_snapshot_json: string; snapshot_json: string; snapshot_hash: string; generation: number };
/** Internal verified entry owner only. Capture all six node-entry HP values, equipment/augments,
 * initial effects/cooldowns/positions/motion/enemies and original definitions/seed.
 * Fence every observed source; current HP, initial run HP and client checkpoints are not substitutes. */
export interface OrdinaryNodeStartOwner {
  prepare(context: { accountId: string; runId: string; nodeId: string; startSnapshot: string; mapRevision: number;
    arrivalEventId?: string; firstPlayableScaling?: z.infer<typeof firstPlayableScalingSchema>;
    executionOrigin?: OrdinaryNodeExecutionOrigin }):
    Promise<{ snapshot: Record<string, unknown>; assertions: D1PreparedStatement[] }>;
}

export class OrdinaryNodeAttemptStore {
  constructor(private readonly db: D1Database) {}
  /** Guard a new combat mutation in its enclosing HP transaction; receipts are recovered first. */
  async prepareHealthMutation(accountId: string, runId: string, attempt?: { nodeId: string; generation: number }) {
    if (!attempt) {
      if (await this.db.prepare('SELECT 1 FROM ordinary_node_attempts WHERE run_id=? LIMIT 1').bind(runId).first())
        return fail('attempt_required');
      return [this.db.prepare(`INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN NOT EXISTS(
        SELECT 1 FROM ordinary_node_attempts WHERE run_id=?) THEN 1 ELSE 0 END`).bind(runId)];
    }
    const { row, state, currentNodeId } = await this.context(accountId, runId);
    const origin = await this.origin(runId, attempt.nodeId);
    if (!origin || origin.generation !== attempt.generation || currentNodeId !== attempt.nodeId
      || row.status !== 'ACTIVE' || state.phase !== 'ACTIVE' || state.holdIds.length) return fail('attempt_conflict');
    await this.readOrigin(origin, row.snapshot_json);
    return [this.guard(accountId, runId, row), this.db.prepare(`INSERT INTO combat_damage_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_node_attempts WHERE run_id=? AND node_id=?
        AND generation=? AND start_snapshot_json=? AND snapshot_hash=?) THEN 1 ELSE 0 END`)
      .bind(runId, attempt.nodeId, attempt.generation, row.snapshot_json, origin.snapshot_hash),
    this.db.prepare('DELETE FROM operation_map_write_guard')];
  }
  private origin(runId: string, nodeId: string) {
    return this.db.prepare('SELECT start_snapshot_json,snapshot_json,snapshot_hash,generation FROM ordinary_node_attempts WHERE run_id=? AND node_id=?')
      .bind(runId, nodeId).first<Origin>();
  }
  private async context(accountId: string, runId: string) {
    id.parse(accountId); id.parse(runId);
    const row = await this.db.prepare(`SELECT r.status,s.snapshot_json,m.revision,m.state_json,m.plan_json
      FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
      JOIN operation_map_states m ON m.run_id=r.id AND m.start_snapshot_json=s.snapshot_json
      WHERE r.id=? AND r.account_id=? AND a.status='ACTIVE'`).bind(runId, accountId)
      .first<{ status: string; snapshot_json: string; revision: number; state_json: string; plan_json: string }>();
    if (!row) return fail('run_not_found');
    const original = JSON.parse(row.snapshot_json), originalPlan = readDepartureMap(original);
    if (!originalPlan || json(originalPlan) !== json(JSON.parse(row.plan_json))) return fail('plan_conflict');
    const plan = captureOperationMap(JSON.parse(row.plan_json));
    await verifyPinnedOperationMapContent(original, plan);
    const state = readMapProgress(plan, JSON.parse(row.state_json));
    return { row, plan, state, currentNodeId: projectOperationMap(plan, state.traversedEdgeIds).currentNodeId };
  }
  private guard(accountId: string, runId: string, row: Awaited<ReturnType<OrdinaryNodeAttemptStore['context']>>['row']) {
    return this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
      SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
      JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE'
      AND a.status='ACTIVE' AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json
      AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END`)
      .bind(runId, accountId, row.snapshot_json, row.revision, row.state_json, row.plan_json);
  }
  private async readOrigin(origin: Origin, start: string) {
    if (origin.start_snapshot_json !== start || await sha256Hex(origin.snapshot_json) !== origin.snapshot_hash)
      return fail('snapshot_conflict');
    return z.record(z.string(), z.json()).parse(JSON.parse(origin.snapshot_json));
  }
  /** Prepare only; the verified ARRIVE owner commits these statements with progress and its receipt. */
  async prepareArrival(accountId: string, runId: string, nodeId: string, owner: OrdinaryNodeStartOwner,
    arrival?: { eventId: string; firstPlayableScaling?: z.infer<typeof firstPlayableScalingSchema> }) {
    id.parse(nodeId); const { row, plan, state } = await this.context(accountId, runId);
    const node = plan.nodes.find(n => n.nodeId === nodeId);
    if (!node) return fail('node_invalid');
    if (!['COMBAT', 'ELITE', 'BOSS'].includes(node.eventKind)) return undefined;
    if (row.status !== 'ACTIVE' || state.phase !== 'TRAVELLING' || state.holdIds.length
      || plan.edges.find(e => e.edgeId === state.pendingEdgeId)?.toNodeId !== nodeId) return fail('arrival_source_conflict');
    if (await this.origin(runId, nodeId)) return fail('snapshot_already_captured');
    if (row.revision >= Number.MAX_SAFE_INTEGER) return fail('revision_exhausted');
    const prepared = await owner.prepare({ accountId, runId, nodeId, startSnapshot: row.snapshot_json, mapRevision: row.revision + 1,
      executionOrigin: issueOrdinaryNodeExecutionOrigin(),
      ...(arrival ? { arrivalEventId: arrival.eventId, ...(arrival.firstPlayableScaling ? { firstPlayableScaling: structuredClone(arrival.firstPlayableScaling) } : {}) } : {}) });
    if (!Array.isArray(prepared.assertions) || !prepared.assertions.length || prepared.assertions.length > 128) return fail('source_unverified');
    const snapshot = z.record(z.string(), z.json()).parse(prepared.snapshot), text = json(snapshot), snapshotHash = await sha256Hex(text);
    return { binding: { version: 'ordinary.node-start.v1.20261028' as const, accountId, runId, nodeId,
      generation: 1, entryMapRevision: row.revision + 1, snapshot, snapshotHash },
    assertions: [this.guard(accountId, runId, row), ...prepared.assertions,
      this.db.prepare('INSERT INTO ordinary_node_attempts VALUES(?,?,?,?,?,1)').bind(runId, nodeId, row.snapshot_json, text, snapshotHash)] };
  }
  async capture(accountId: string, runId: string, nodeId: string, owner: OrdinaryNodeStartOwner) {
    id.parse(nodeId); const { row, plan, state, currentNodeId } = await this.context(accountId, runId);
    if (row.status !== 'ACTIVE' || state.phase !== 'ACTIVE' || state.holdIds.length || currentNodeId !== nodeId
      || !['COMBAT', 'ELITE', 'BOSS'].includes(plan.nodes.find(n => n.nodeId === nodeId)!.eventKind)) return fail('entry_not_active');
    const old = await this.origin(runId, nodeId);
    if (old) return { generation: old.generation, snapshot: await this.readOrigin(old, row.snapshot_json) };
    const prepared = await owner.prepare({ accountId, runId, nodeId, startSnapshot: row.snapshot_json, mapRevision: row.revision });
    if (!Array.isArray(prepared.assertions) || !prepared.assertions.length || prepared.assertions.length > 128) return fail('source_unverified');
    const snapshot = z.record(z.string(), z.json()).parse(prepared.snapshot), text = json(snapshot), hash = await sha256Hex(text);
    await this.db.batch([this.guard(accountId, runId, row), ...prepared.assertions,
      this.db.prepare('INSERT INTO ordinary_node_attempts VALUES(?,?,?,?,?,1)').bind(runId, nodeId, row.snapshot_json, text, hash),
      this.db.prepare('DELETE FROM operation_map_write_guard')]);
    return { generation: 1, snapshot };
  }
  async restart(accountId: string, runId: string, input: unknown): Promise<RestartResult> {
    const c = nodeRestartCommand.parse(input), hash = await sha256Hex(json(c));
    const { row, plan, state, currentNodeId } = await this.context(accountId, runId);
    if (!plan.nodes.some(n => n.nodeId === c.nodeId && ['COMBAT', 'ELITE', 'BOSS'].includes(n.eventKind))) return fail('node_invalid');
    const origin = await this.origin(runId, c.nodeId);
    if (currentNodeId !== c.nodeId && !origin) return fail('node_not_entered');
    const identity = { version: 'ordinary.node-restart.v1.20261028' as const, requestId: c.requestId, accountId, runId, nodeId: c.nodeId };
    const saved = await this.db.prepare('SELECT request_hash,response_json FROM ordinary_node_restart_receipts WHERE run_id=? AND request_id=?')
      .bind(runId, c.requestId).first<{ request_hash: string; response_json: string }>();
    if (saved && saved.request_hash !== hash) return fail('request_conflict');
    // A pending finish has no confirmed result; recover its original request before reentry.
    if (row.status === 'FINISH_PENDING') return fail('transaction_pending');
    // Outcome wins even when the caller is recovering an earlier successful restart receipt.
    if (row.status !== 'ACTIVE' || state.phase !== 'ACTIVE' || currentNodeId !== c.nodeId)
      return { ...identity, status: 'RESULT_AVAILABLE', runStatus: row.status, revision: row.revision, state, generation: null };
    if (state.holdIds.length) return fail('transaction_pending');
    if (!origin) return fail('snapshot_missing');
    const snapshot = await this.readOrigin(origin, row.snapshot_json);
    if (saved) {
      const original = restartedReceipt.parse(JSON.parse(saved.response_json));
      if (original.generation !== origin.generation || original.revision !== row.revision) return fail('restart_superseded');
      return original;
    }
    if (row.revision !== c.expectedRevision || origin.generation !== c.expectedGeneration) return fail('attempt_conflict');
    if (row.revision >= Number.MAX_SAFE_INTEGER || origin.generation >= Number.MAX_SAFE_INTEGER) return fail('revision_exhausted');
    const receipt = { ...identity, status: 'RESTARTED' as const, revision: row.revision + 1, generation: origin.generation + 1,
      snapshot, snapshotHash: origin.snapshot_hash };
    try {
      await this.db.batch([this.guard(accountId, runId, row),
        this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_node_attempts
          WHERE run_id=? AND node_id=? AND generation=? AND snapshot_hash=? AND snapshot_json=?) THEN 1 ELSE 0 END`)
          .bind(runId, c.nodeId, origin.generation, origin.snapshot_hash, origin.snapshot_json),
        this.db.prepare('UPDATE ordinary_node_attempts SET generation=generation+1 WHERE run_id=? AND node_id=?').bind(runId, c.nodeId),
        this.db.prepare('UPDATE operation_map_states SET revision=revision+1 WHERE run_id=?').bind(runId),
        this.db.prepare('INSERT INTO ordinary_node_restart_receipts VALUES(?,?,?,?)').bind(runId, c.requestId, hash, json(receipt)),
        this.db.prepare('DELETE FROM operation_map_write_guard')]);
    } catch (error) {
      const committed = await this.db.prepare('SELECT 1 FROM ordinary_node_restart_receipts WHERE run_id=? AND request_id=?').bind(runId, c.requestId).first();
      if (committed) return this.restart(accountId, runId, c);
      if (String(error).includes('valid=1')) return fail('state_conflict');
      throw error;
    }
    return receipt;
  }
}
