import { canonicalJson, HealthRebaseState, healthRebaseSnapshotSchema, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { OrdinaryNodeAttemptStore } from './ordinary-node-attempt';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const natural = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const initialSchema = z.array(z.strictObject({ id, hp: natural, maxHp: natural.min(1) })
  .refine(t => t.hp <= t.maxHp)).min(1).max(1000).refine(t => new Set(t.map(x => x.id)).size === t.length);
const changeSchema = z.discriminatedUnion('kind', [
  z.strictObject({ kind: z.literal('MAX_HP_REBASE'), maxHp: natural.min(1) }),
  z.strictObject({ kind: z.literal('ACTUAL_HP'), reason: z.enum(['DAMAGE', 'HEAL', 'REPAIR', 'RECOVERY']), hp: natural }),
]);
const commandSchema = z.strictObject({ eventId: id, expectedRevision: natural.min(1).max(Number.MAX_SAFE_INTEGER - 1),
  targetId: id, change: changeSchema,
  attempt: z.strictObject({ nodeId: z.string().min(1).max(200), generation: natural.min(1) }).optional() });
type Command = z.infer<typeof commandSchema>;
const targetSchema = z.strictObject({ id, health: healthRebaseSnapshotSchema });
const entrySchema = z.strictObject({ eventId: id, targetId: id, change: changeSchema,
  before: healthRebaseSnapshotSchema, after: healthRebaseSnapshotSchema });
const stateSchema = z.strictObject({ version: z.literal('combat.health-state.v2'), initialTargets: initialSchema,
  targets: z.array(targetSchema).min(1).max(1000), ledger: z.array(entrySchema).max(100000) });
const receiptSchema = entrySchema.extend({ version: z.literal('combat.health-receipt.v2'),
  revision: natural.min(2) });
export { receiptSchema as combatHealthReceiptSchema };
type Row = { revision: number; initial_hash: string; start_snapshot_json: string; state_json: string; actor_state_json: string | null };

function apply(before: z.infer<typeof healthRebaseSnapshotSchema>, change: Command['change']) {
  const candidate = HealthRebaseState.restore(before);
  if (change.kind === 'MAX_HP_REBASE') candidate.rebaseMaximum(change.maxHp);
  else {
    if (change.reason === 'DAMAGE' ? change.hp > before.hp : change.hp < before.hp) throw new Error('wrong HP direction');
    candidate.recordActualHp(change.hp);
  }
  return candidate.snapshot();
}

/** Validate a saved transition without reading or changing current HP. */
export function readCombatHealthReceipt(input: unknown) {
  try {
    const receipt = receiptSchema.parse(input);
    if (canonicalJson(apply(receipt.before, receipt.change)) !== canonicalJson(receipt.after)) throw new Error('receipt result');
    return receipt;
  } catch { throw new ActivityError(503, 'combat_health_receipt_invalid'); }
}

export interface CombatHealthVerifier {
  /** Must prove new-policy selection, original supported targets/definitions and the
   * actual HP event or final eligible maximum from authoritative state in this batch.
   * MAX_HP_REBASE also requires safe equipment/effect eligibility. No live adapter exists.
   * These internal commands must never be exposed as arbitrary client HP setters. */
  prepare(context: { kind: 'INITIALIZE' | 'CHANGE'; accountId: string; runId: string; startSnapshotJson: string;
    initialTargets?: z.infer<typeof initialSchema>; command?: Command;
    /** All CHANGE assertions in a group run before the one final checkpoint write. */
    batchBaseRevision?: number }): Promise<{ assertions: D1PreparedStatement[] }>;
}

/** V2 owner of the same combat_damage_states row, never a second independent HP cache.
 * V1 damage state and receipts are preserved; switching an existing row is forbidden.
 * Equipment/stats and actor charge integration remain separate required consumers. */
export class CombatHealthStore {
  constructor(private readonly db: D1Database, private readonly verifier?: CombatHealthVerifier) {}

  private async owner(accountId: string, runId: string) {
    const run = await this.db.prepare(`SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id
      WHERE r.id=? AND r.account_id=?`).bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (!run) throw new ActivityError(409, 'combat_run_not_found');
    return run;
  }
  private row(runId: string) {
    return this.db.prepare('SELECT revision,initial_hash,start_snapshot_json,state_json,actor_state_json FROM combat_damage_states WHERE run_id=?')
      .bind(runId).first<Row>();
  }
  private async state(row: Row) {
    try {
      const state = stateSchema.parse(JSON.parse(row.state_json));
      if (row.actor_state_json !== null || row.revision !== state.ledger.length + 1) throw new Error('unsupported actor state or revision');
      const hash = await sha256Hex(canonicalJson({ version: state.version, initialTargets: state.initialTargets, startSnapshot: row.start_snapshot_json }));
      if (hash !== row.initial_hash) throw new Error('initial checksum');
      const expected = new Map(state.initialTargets.map(t => [t.id, HealthRebaseState.create({
        policyVersion: 'health.max-hp-rebase.v1', hp: t.hp, maxHp: t.maxHp }).snapshot()]));
      const seen = new Set<string>();
      for (const entry of state.ledger) {
        if (seen.has(entry.eventId) || canonicalJson(expected.get(entry.targetId) ?? null) !== canonicalJson(entry.before)) throw new Error('ledger order');
        const after = apply(entry.before, entry.change);
        if (canonicalJson(after) !== canonicalJson(entry.after)) throw new Error('ledger result');
        expected.set(entry.targetId, after); seen.add(entry.eventId);
      }
      if (state.targets.length !== expected.size || new Set(state.targets.map(t => t.id)).size !== expected.size
        || state.targets.some(t => canonicalJson(expected.get(t.id) ?? null) !== canonicalJson(t.health))) throw new Error('checkpoint mismatch');
      return state;
    } catch { throw new ActivityError(503, 'combat_health_storage_invalid'); }
  }
  private async proof(context: Parameters<CombatHealthVerifier['prepare']>[0]) {
    if (!this.verifier) throw new ActivityError(409, 'combat_health_verification_required');
    const proof = await this.verifier.prepare(context);
    if (!proof || !Array.isArray(proof.assertions) || !proof.assertions.length) throw new ActivityError(409, 'combat_health_verification_required');
    return proof.assertions;
  }
  async read(accountId: string, runId: string) {
    const run = await this.owner(accountId, runId), row = await this.row(runId);
    if (!row) throw new ActivityError(409, 'combat_health_state_missing');
    if (row.start_snapshot_json !== run.snapshot_json) throw new ActivityError(409, 'combat_health_start_snapshot_changed');
    return { revision: row.revision, state: await this.state(row) };
  }
  async initialize(accountId: string, runId: string, raw: unknown) {
    const prepared = await this.prepareInitialization(accountId, runId, raw);
    if (!prepared.statements.length) return prepared.snapshot;
    try { await this.db.batch(prepared.statements); }
    catch (error) { if (!await prepared.replay()) throw error; }
    return this.read(accountId, runId);
  }
  /** Candidate initialization for an enclosing phase/stat transaction. The
   * original verifier and snapshot guards remain mandatory; this writes nothing. */
  async prepareInitialization(accountId: string, runId: string, raw: unknown) {
    const initialTargets = initialSchema.parse(raw).sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
    const run = await this.owner(accountId, runId);
    const initialHash = await sha256Hex(canonicalJson({ version: 'combat.health-state.v2', initialTargets, startSnapshot: run.snapshot_json }));
    const replay = async () => {
      const row = await this.row(runId);
      return row?.initial_hash === initialHash ? this.read(accountId, runId) : null;
    };
    const existing = await this.row(runId);
    if (existing) {
      if (existing.initial_hash !== initialHash) throw new ActivityError(409, 'combat_health_initialization_conflict');
      return { snapshot: await this.read(accountId, runId), statements: [] as D1PreparedStatement[], replay };
    }
    if (run.status !== 'ACTIVE') throw new ActivityError(409, 'combat_run_not_active');
    const assertions = await this.proof({ kind: 'INITIALIZE', accountId, runId, startSnapshotJson: run.snapshot_json,
      initialTargets: initialTargets.map(t => ({ ...t })) });
    const state = stateSchema.parse({ version: 'combat.health-state.v2', initialTargets,
      targets: initialTargets.map(t => ({ id: t.id, health: HealthRebaseState.create({ policyVersion: 'health.max-hp-rebase.v1', hp: t.hp, maxHp: t.maxHp }).snapshot() })), ledger: [] });
    const statements = [...assertions, this.db.prepare(`INSERT INTO combat_damage_write_guard(valid)
        SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
          WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=?)
          AND NOT EXISTS(SELECT 1 FROM combat_damage_states WHERE run_id=?) THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, runId),
      this.db.prepare('INSERT INTO combat_damage_states(run_id,revision,initial_hash,start_snapshot_json,state_json,actor_state_json) VALUES(?,1,?,?,?,NULL)')
        .bind(runId, initialHash, run.snapshot_json, canonicalJson(state)), this.db.prepare('DELETE FROM combat_damage_write_guard')];
    return { snapshot: { revision: 1, state }, statements, replay };
  }
  async commit(accountId: string, runId: string, raw: unknown) {
    const prepared = await this.prepareChange(accountId, runId, raw);
    if (!prepared.statements.length) return prepared.receipt;
    try { await this.db.batch(prepared.statements); }
    catch (error) {
      const completed = await prepared.replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'combat_health_state_conflict');
      throw error;
    }
    return prepared.receipt;
  }

  /** Internal composition with the related equipment transaction, in a single batch.
   * Prepared state is a candidate; no live HP changes occur here. All original-source
   * assertions must be kept. Original receipts contain no writes and must not authorize
   * a new equipment command or historical backfill. No public route is installed. */
  async prepareChange(accountId: string, runId: string, raw: unknown) {
    const command = commandSchema.parse(raw), group = await this.prepareChanges(accountId, runId, [command]);
    return { receipt: group.receipts[0]!, statements: group.statements,
      replay: async () => (await group.replay())?.[0] ?? null };
  }

  /** Ordered candidate changes over ONE initial checkpoint and one final write.
   * The producer owns stable event IDs, affected-target completeness and final maxima.
   * Entire group must be new or entirely replayed; partial history is never backfilled. */
  async prepareChanges(accountId: string, runId: string, raw: unknown) {
    const parsed = z.array(commandSchema).min(1).max(1000).safeParse(raw);
    if (!parsed.success || new Set(parsed.data.map(c => c.eventId)).size !== parsed.data.length
      || parsed.data.some((c, n) => c.expectedRevision !== parsed.data[0]!.expectedRevision + n
        || c.expectedRevision >= Number.MAX_SAFE_INTEGER)) throw new ActivityError(400, 'combat_health_group_invalid');
    const commands = parsed.data, first = commands[0]!, run = await this.owner(accountId, runId);
    const hashes = await Promise.all(commands.map(c => sha256Hex(canonicalJson(z.json().parse(c)))));
    const originalReceipt = async (command: Command, hash: string) => {
      const row = await this.db.prepare('SELECT request_hash,response_json FROM combat_damage_receipts WHERE run_id=? AND event_id=?')
        .bind(runId, command.eventId).first<{ request_hash: string; response_json: string }>();
      if (!row) return null;
      if (row.request_hash !== hash) throw new ActivityError(409, 'combat_health_event_conflict');
      try {
        const receipt = readCombatHealthReceipt(JSON.parse(row.response_json));
        if (receipt.eventId !== command.eventId || receipt.targetId !== command.targetId || receipt.revision !== command.expectedRevision + 1
          || canonicalJson(receipt.change) !== canonicalJson(command.change)) throw new Error('receipt mismatch');
        return receipt;
      } catch { throw new ActivityError(503, 'combat_health_receipt_invalid'); }
    };
    const replay = async () => {
      const originals = await Promise.all(commands.map((c, n) => originalReceipt(c, hashes[n]!)));
      if (originals.every(r => r === null)) return null;
      if (originals.some(r => r === null)) throw new ActivityError(409, 'combat_health_partial_replay');
      return originals as z.infer<typeof receiptSchema>[];
    };
    const original = await replay();
    if (original) return { receipts: original, statements: [] as D1PreparedStatement[], replay };
    if (run.status !== 'ACTIVE') throw new ActivityError(409, 'combat_run_not_active');
    const row = await this.row(runId);
    if (!row) throw new ActivityError(409, 'combat_health_state_missing');
    if (row.revision !== first.expectedRevision) throw new ActivityError(409, 'combat_health_revision_conflict');
    if (row.start_snapshot_json !== run.snapshot_json) throw new ActivityError(409, 'combat_health_start_snapshot_changed');
    const state = await this.state(row), receipts: z.infer<typeof receiptSchema>[] = [], assertions: D1PreparedStatement[] = [];
    for (const command of commands) if (command.change.kind === 'ACTUAL_HP' || command.attempt)
      assertions.push(...await new OrdinaryNodeAttemptStore(this.db).prepareHealthMutation(accountId, runId, command.attempt));
    for (const command of commands) {
      const target = state.targets.find(t => t.id === command.targetId);
      if (!target) throw new ActivityError(409, 'combat_health_target_unknown');
      let after: typeof target.health;
      try { after = apply(target.health, command.change); }
      catch { throw new ActivityError(409, 'combat_health_change_invalid'); }
      const entry = entrySchema.parse({ eventId: command.eventId, targetId: target.id, change: command.change, before: target.health, after });
      receipts.push(receiptSchema.parse({ ...entry, version: 'combat.health-receipt.v2', revision: command.expectedRevision + 1 }));
      target.health = after; state.ledger.push(entry);
    }
    stateSchema.parse(state);
    for (const command of commands) assertions.push(...await this.proof({ kind: 'CHANGE', accountId, runId,
      startSnapshotJson: run.snapshot_json, command: { ...command, change: { ...command.change } }, batchBaseRevision: row.revision }));
    const statements = [...assertions, this.db.prepare(`INSERT INTO combat_damage_write_guard(valid)
        SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
          JOIN combat_damage_states d ON d.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
          AND s.snapshot_json=? AND d.start_snapshot_json=? AND d.revision=? AND d.initial_hash=? AND d.state_json=? AND d.actor_state_json IS NULL)
          THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, row.start_snapshot_json, row.revision, row.initial_hash, row.state_json),
      this.db.prepare('UPDATE combat_damage_states SET revision=?,state_json=? WHERE run_id=? AND revision=?')
        .bind(row.revision + commands.length, canonicalJson(state), runId, row.revision),
      ...commands.map((command, n) => this.db.prepare('INSERT INTO combat_damage_receipts(run_id,event_id,request_hash,response_json) VALUES(?,?,?,?)')
        .bind(runId, command.eventId, hashes[n]!, canonicalJson(receipts[n]!))), this.db.prepare('DELETE FROM combat_damage_write_guard')];
    return { receipts, statements, replay };
  }
}
