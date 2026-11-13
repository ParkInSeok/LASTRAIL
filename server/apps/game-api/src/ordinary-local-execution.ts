import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import type { Context, Hono } from 'hono';
import { ActivityError } from './activity-policy';
import { readBoundedBody, type ActivityAuthResult } from './activity-routes';
import { OrdinaryNodeAttemptStore } from './ordinary-node-attempt';
import { OrdinaryNativeGeometryRegistry } from './ordinary-native-geometry';
import { readOperationMapView } from './operation-map-view';
import type { FirstObjectiveEvidenceOwner } from './combat-first-objective-verifier';
import { prepareOrdinaryLocalBoardingSource } from './ordinary-local-boarding-source';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryAugmentOwnership, ordinaryAugmentOwnershipAssertion } from './ordinary-augment-ownership';

const id = z.string().min(1).max(200), hash = z.string().regex(/^[a-f0-9]{64}$/u), nat = z.number().int().nonnegative().safe();
const operation = z.discriminatedUnion('kind', [
  z.strictObject({ kind: z.literal('PAUSE'), value: z.boolean() }),
  z.strictObject({ kind: z.literal('ULTIMATE_AUTO'), value: z.boolean() }),
  z.strictObject({ kind: z.literal('SPEED'), value: z.union([z.literal(2), z.literal(3), z.literal(4)]) }),
  z.strictObject({ kind: z.literal('THROTTLE'), value: z.number().int().min(7000).max(13000) }),
  z.strictObject({ kind: z.literal('ADVANCE'), elapsed: nat.max(10000000), budget: nat.max(1000) }),
]);
export const ordinaryLocalInputSchema = z.strictObject({ runId: id, nodeId: id, attemptGeneration: nat.min(1),
  snapshotHash: hash, expectedSequence: nat.max(10000), inputs: z.array(z.strictObject({ sequence: nat.min(1).max(10000), tick: nat, command: operation })).max(128) });
const resultSchema = z.object({ version: z.literal('ordinary.local-execution-result.v1.20261106'), accountId: id,
  runId: id, nodeId: id, attemptGeneration: nat.min(1), snapshotHash: hash, lastSequence: nat.max(10000), tick: nat,
  checkpoint: z.record(z.string(), z.json()), obstacleResolution: z.json().nullable(), evidence: z.record(z.string(), z.json()).nullable(),
  healthChanges: z.array(z.strictObject({ eventId: id, targetId: id, reason: z.enum(['DAMAGE', 'HEAL', 'REPAIR', 'RECOVERY']), hp: nat.max(2147483647) })).max(1000) }).passthrough();
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (code: string): never => { throw new ActivityError(409, 'ordinary_local_' + code); };
type Input = z.infer<typeof ordinaryLocalInputSchema>;
type Row = { sequence: number; started_at: number; elapsed_ticks: number; inputs_json: string; result_json: string; snapshot_hash: string };

/** Instantiated only by the isolated loopback supervisor with a hash-pinned shared game engine. */
export class OrdinaryLocalExecutionStore implements FirstObjectiveEvidenceOwner {
  constructor(private readonly db: D1Database, private readonly engine: { pin: unknown; inputVersion?: 'ordinary.local-execution-input.v3.20261109'; replay(source: unknown): Promise<unknown> },
    private readonly now: () => number = Date.now) {}
  private row(input: Pick<Input, 'runId' | 'nodeId' | 'attemptGeneration'>) {
    return this.db.prepare('SELECT sequence,started_at,elapsed_ticks,inputs_json,result_json,snapshot_hash FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=?')
      .bind(input.runId, input.nodeId, input.attemptGeneration).first<Row>();
  }
  async source(accountId: string, input: Pick<Input, 'runId' | 'nodeId' | 'attemptGeneration' | 'snapshotHash'>) {
    const localScope = await this.db.prepare('SELECT engine_pin_json FROM ordinary_local_node_scope WHERE account_id=? AND run_id=? AND node_id=?')
      .bind(accountId, input.runId, input.nodeId).first<{ engine_pin_json: string }>();
    if (!localScope || localScope.engine_pin_json !== json(this.engine.pin)) return fail('engine_origin_conflict');
    const status = await readOperationMapView(this.db, accountId, input.runId), attempt = status.currentAttempt;
    if (!attempt || attempt.nodeId !== input.nodeId || attempt.generation !== input.attemptGeneration || attempt.snapshotHash !== input.snapshotHash)
      return fail('attempt_conflict');
    const row = await this.db.prepare('SELECT start_snapshot_json,snapshot_json,snapshot_hash FROM ordinary_node_attempts WHERE run_id=? AND node_id=? AND generation=?')
      .bind(input.runId, input.nodeId, input.attemptGeneration).first<{ start_snapshot_json: string; snapshot_json: string; snapshot_hash: string }>();
    if (!row || await sha256Hex(row.snapshot_json) !== input.snapshotHash) return fail('origin_conflict');
    const original = JSON.parse(row.start_snapshot_json), entry = JSON.parse(row.snapshot_json);
    const initialization = await this.db.prepare('SELECT response_json,response_hash FROM ordinary_combat_activation_receipts WHERE run_id=? ORDER BY request_id LIMIT 1')
      .bind(input.runId).first<{ response_json: string; response_hash: string }>();
    const arrivals = await this.db.prepare('SELECT response_json FROM operation_map_receipts WHERE run_id=?').bind(input.runId).all<{ response_json: string }>();
    const arrival = arrivals.results.map(r => JSON.parse(r.response_json)).find(r => r.nodeStart?.nodeId === input.nodeId && r.nodeStart?.snapshotHash === input.snapshotHash);
    if (!initialization || await sha256Hex(initialization.response_json) !== initialization.response_hash || !arrival?.firstPlayableScaling)
      return fail('origin_unavailable');
    const geometry = await new OrdinaryNativeGeometryRegistry(this.db, original.ordinaryGeometryReference).read();
    const hasBoardingGeometry = 'scope' in geometry.definition.source && !!geometry.definition.source.boardingGeometry;
    const boardingPin = entry.runtime?.boardingSourceHash;
    if (hasBoardingGeometry !== Object.hasOwn(entry.runtime ?? {}, 'boardingSourceHash')
      || (hasBoardingGeometry && !hash.safeParse(boardingPin).success)) return fail('boarding_origin_conflict');
    const boardingSource = hasBoardingGeometry ? await prepareOrdinaryLocalBoardingSource(
      await readOrdinaryRunStatOrigin(this.db, accountId, input.runId), input.nodeId,
      { executionVersion: entry.runtime.executionVersion, determinism: entry.runtime.determinism }) : undefined;
    if (boardingSource && boardingSource.sourceHash !== boardingPin) return fail('boarding_origin_conflict');
    const augmentOwnership = this.engine.inputVersion ? await readOrdinaryAugmentOwnership(this.db, accountId, input.runId) : undefined;
    return { input: { version: this.engine.inputVersion ?? (boardingSource ? 'ordinary.local-execution-input.v2.20261107' : 'ordinary.local-execution-input.v1.20261106'), accountId, runId: input.runId,
      ...(augmentOwnership ? {augmentOwnership} : {}),
      ...(boardingSource ? { boardingSource } : {}),
      originalStart: original,
      initializationResponse: { accepted: true, runtimeExecutionVerified: false, initializationReceipt: JSON.parse(initialization.response_json) },
      nodeStart: arrival.nodeStart, firstPlayableScaling: arrival.firstPlayableScaling, currentAttempt: attempt, geometryDefinition: geometry.definitionJson },
      entry, startSnapshot: row.start_snapshot_json, assertion: geometry.assertion,
      assertions: [...(augmentOwnership ? [await ordinaryAugmentOwnershipAssertion(this.db, augmentOwnership)] : []), ...await new OrdinaryNodeAttemptStore(this.db).prepareHealthMutation(accountId, input.runId, { nodeId: input.nodeId, generation: input.attemptGeneration }),
        this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_local_node_scope
          WHERE account_id=? AND run_id=? AND node_id=? AND engine_pin_json=?) THEN 1 ELSE 0 END`)
          .bind(accountId, input.runId, input.nodeId, localScope.engine_pin_json)] };
  }
  async commit(accountId: string, requestId: string, raw: unknown) {
    const input = ordinaryLocalInputSchema.parse(raw), requestHash = await sha256Hex(json({ accountId, input }));
    const saved = async () => {
      const row = await this.db.prepare('SELECT account_id,request_hash,response_json FROM ordinary_local_execution_receipts WHERE run_id=? AND request_id=?')
        .bind(input.runId, requestId).first<{ account_id: string; request_hash: string; response_json: string }>();
      if (row && (row.account_id !== accountId || row.request_hash !== requestHash)) return fail('request_conflict');
      return row ? JSON.parse(row.response_json) : null;
    };
    const original = await saved(); if (original) return original;
    const source = await this.source(accountId, input), previous = await this.row(input);
    if (previous && previous.snapshot_hash !== input.snapshotHash || (previous?.sequence ?? 0) !== input.expectedSequence) return fail('sequence_conflict');
    if (input.inputs.some((i, n) => i.sequence !== input.expectedSequence + n + 1)) return fail('sequence_conflict');
    const inputs: unknown[] = [...(previous ? JSON.parse(previous.inputs_json) : []), ...input.inputs];
    if (inputs.length > 10000) return fail('capacity');
    const startedAt = previous?.started_at ?? this.now(), elapsed = (previous?.elapsed_ticks ?? 0)
      + input.inputs.reduce((sum, i) => sum + (i.command.kind === 'ADVANCE' ? i.command.elapsed : 0), 0);
    if (elapsed > (Math.max(0, this.now() - startedAt) + 250) * 10000) return fail('time_budget');
    const result = resultSchema.parse(await this.engine.replay({ ...source.input, inputs }));
    if (result.accountId !== accountId || result.runId !== input.runId || result.nodeId !== input.nodeId
      || result.attemptGeneration !== input.attemptGeneration || result.snapshotHash !== input.snapshotHash
      || result.lastSequence !== inputs.length || previous && result.tick < resultSchema.parse(JSON.parse(previous.result_json)).tick)
      return fail('execution_conflict');
    if (previous && result.checkpoint.configurationSha256 !== resultSchema.parse(JSON.parse(previous.result_json)).checkpoint.configurationSha256)
      return fail('execution_origin_changed');
    const response = { accepted: true, version: 'ordinary.local-execution-receipt.v1.20261106', requestId,
      evidenceRef: `local:${requestId}`, result };
    const guard = previous ? this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
      SELECT 1 FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=? AND sequence=? AND result_json=? AND inputs_json=?) THEN 1 ELSE 0 END`)
      .bind(input.runId, input.nodeId, input.attemptGeneration, previous.sequence, previous.result_json, previous.inputs_json)
      : this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN NOT EXISTS(
        SELECT 1 FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=?) THEN 1 ELSE 0 END`)
        .bind(input.runId, input.nodeId, input.attemptGeneration);
    try {
      await this.db.batch([...source.assertions, source.assertion, guard,
        this.db.prepare(`INSERT INTO ordinary_local_execution VALUES(?,?,?,?,?,?,?,?,?,?) ON CONFLICT(run_id,node_id,generation)
          DO UPDATE SET sequence=excluded.sequence,elapsed_ticks=excluded.elapsed_ticks,inputs_json=excluded.inputs_json,result_json=excluded.result_json`)
          .bind(accountId, input.runId, input.nodeId, input.attemptGeneration, input.snapshotHash, inputs.length, startedAt, elapsed, json(inputs), json(result)),
        this.db.prepare('INSERT INTO ordinary_local_execution_receipts VALUES(?,?,?,?,?)').bind(accountId, input.runId, requestId, requestHash, json(response)),
        this.db.prepare('DELETE FROM operation_map_write_guard')]);
    } catch (error) { const recovered = await saved(); if (recovered) return recovered; throw error; }
    return response;
  }
  async prepare(context: Parameters<FirstObjectiveEvidenceOwner['prepare']>[0]) {
    const command = context.command;
    if (command.kind !== 'COMPLETE') return fail('command_unsupported');
    if (!command.evidenceRef.startsWith('local:') || command.attemptGeneration === undefined) return fail('evidence_unavailable');
    const row = await this.row({ runId: context.runId, nodeId: command.nodeId!, attemptGeneration: command.attemptGeneration });
    if (!row) return fail('evidence_unavailable');
    const receipt = await this.db.prepare('SELECT response_json FROM ordinary_local_execution_receipts WHERE run_id=? AND account_id=? AND request_id=?')
      .bind(context.runId, context.accountId, command.evidenceRef.slice(6)).first<{ response_json: string }>();
    const result = resultSchema.parse(JSON.parse(row.result_json));
    if (!receipt || json(JSON.parse(receipt.response_json).result) !== row.result_json || !result.evidence) return fail('evidence_unavailable');
    const source = await this.source(context.accountId, { runId: context.runId, nodeId: command.nodeId!,
      attemptGeneration: command.attemptGeneration, snapshotHash: row.snapshot_hash });
    const localScope = await this.db.prepare('SELECT hold_id,entry_effects_json FROM ordinary_local_node_scope WHERE run_id=? AND node_id=? AND account_id=? AND revision=1')
      .bind(context.runId, command.nodeId, context.accountId).first<{ hold_id: string; entry_effects_json: string }>();
    if (!localScope || !id.safeParse(localScope.hold_id).success) return fail('hold_source_unavailable');
    const runtime = source.entry.runtime;
    return { evidence: { ...result.evidence, evidenceRef: command.evidenceRef, mapRevision: command.expectedRevision },
      holdIds: [localScope.hold_id], ...(result.obstacleResolution === null ? {} : { obstacleResolution: result.obstacleResolution }),
      healthChanges: result.healthChanges,
      executionBinding: { nodeId: command.nodeId, attemptGeneration: command.attemptGeneration, snapshotHash: row.snapshot_hash,
        executionVersion: runtime.executionVersion, determinism: runtime.determinism, geometryBinding: runtime.spatial.definitionBinding },
      assertions: [...source.assertions, source.assertion,
        this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_local_node_scope
          WHERE run_id=? AND node_id=? AND account_id=? AND revision=1 AND hold_id=? AND entry_effects_json=?) THEN 1 ELSE 0 END`)
          .bind(context.runId, command.nodeId, context.accountId, localScope.hold_id, localScope.entry_effects_json),
        this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
        SELECT 1 FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=? AND sequence=? AND result_json=?) THEN 1 ELSE 0 END`)
        .bind(context.runId, command.nodeId, command.attemptGeneration, row.sequence, row.result_json)] };
  }
}

export function registerOrdinaryLocalExecution(app: Hono<{ Bindings: Env }>,
  authenticate: (context: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>, store: OrdinaryLocalExecutionStore) {
  app.post('/__local/ordinary-execution', async c => {
    c.header('Cache-Control', 'no-store'); const auth = await authenticate(c);
    if ('errorCode' in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    const key = c.req.header('Idempotency-Key');
    if (!key || !/^[\x21-\x7e]{1,128}$/u.test(key) || new URL(c.req.url).search
      || c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase() !== 'application/json') return c.json({ accepted: false, errorCode: 'contract_violation' }, 400);
    let parsed: ReturnType<typeof ordinaryLocalInputSchema.safeParse>;
    try { parsed = ordinaryLocalInputSchema.safeParse(JSON.parse(await readBoundedBody(c, 65536))); }
    catch { return c.json({ accepted: false, errorCode: 'contract_violation' }, 400); }
    if (!parsed.success) return c.json({ accepted: false, errorCode: 'contract_violation' }, 400);
    try { return c.json(await store.commit(auth.accountId, key, parsed.data)); }
    catch (error) { if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      return c.json({ accepted: false, errorCode: 'ordinary_local_outcome_unknown' }, 503); }
  });
}
