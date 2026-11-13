import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { RunSuppliesStore } from './run-supplies-start';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const source = z.strictObject({ sourceVersion: id, tier: z.enum(['STANDARD', 'HIGH_RISK']),
  outcome: z.enum(['SUCCEEDED', 'FAILED']) });
const receiptSchema = z.strictObject({ version: z.literal('run.supplies-recovery-credit.v1'),
  nodeId: id, sourceRef: id, sourceVersion: id, tier: z.enum(['STANDARD', 'HIGH_RISK']),
  amount: z.union([z.literal(25), z.literal(40)]), balanceAfter: z.number().int().nonnegative(),
  revision: z.number().int().min(2) });
type Tier = z.infer<typeof source>['tier'];
type Receipt = z.infer<typeof receiptSchema>;
type CreditRow = { source_ref: string; payload_hash: string; receipt_json: string };

export interface RecoverySupplySourceVerifier {
  /** The actual objective producer must prove its fixed tier and successful result in
   * the same batch. No production adapter or public route is installed. */
  prepare(context: { accountId: string; runId: string; nodeId: string; sourceRef: string }): Promise<{
    sourceVersion: string; tier: Tier; outcome: 'SUCCEEDED' | 'FAILED'; assertions: D1PreparedStatement[];
  }>;
}

/** Internal, once-per-recovery-node optional income; never infers success from a visit. */
export class RecoverySupplyCreditStore {
  constructor(private readonly db: D1Database, private readonly verifier?: RecoverySupplySourceVerifier) {}

  private original(runId: string, nodeId: string): Promise<CreditRow | null> {
    return this.db.prepare('SELECT source_ref,payload_hash,receipt_json FROM run_supplies_recovery_credits WHERE run_id=? AND node_id=?')
      .bind(runId, nodeId).first<CreditRow>();
  }

  private parseOriginal(row: CreditRow): Receipt {
    try { return receiptSchema.parse(JSON.parse(row.receipt_json)); }
    catch { throw new ActivityError(503, 'run_supplies_recovery_storage_invalid'); }
  }

  async credit(accountId: string, runId: string, nodeId: string, sourceRef: string): Promise<Receipt> {
    if (!id.safeParse(nodeId).success || !id.safeParse(sourceRef).success) {
      throw new ActivityError(400, 'run_supplies_recovery_request_invalid');
    }
    const state = await new RunSuppliesStore(this.db).read(accountId, runId);
    if (!state) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    const old = await this.original(runId, nodeId);
    if (old) {
      if (old.source_ref !== sourceRef) throw new ActivityError(409, 'run_supplies_recovery_source_conflict');
      return this.parseOriginal(old);
    }
    const usedSource = await this.db.prepare('SELECT node_id FROM run_supplies_recovery_credits WHERE run_id=? AND source_ref=?')
      .bind(runId, sourceRef).first<{ node_id: string }>();
    if (usedSource) throw new ActivityError(409, 'run_supplies_recovery_source_conflict');
    const run = await this.db.prepare('SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?')
      .bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'run_supplies_run_not_active');
    if (!this.verifier) throw new ActivityError(409, 'run_supplies_recovery_source_required');
    const proof = await this.verifier.prepare({ accountId, runId, nodeId, sourceRef });
    const parsed = source.safeParse({ sourceVersion: proof?.sourceVersion, tier: proof?.tier, outcome: proof?.outcome });
    if (!parsed.success || parsed.data.outcome !== 'SUCCEEDED' || !Array.isArray(proof.assertions)
      || proof.assertions.length === 0) throw new ActivityError(409, 'run_supplies_recovery_source_invalid');
    const amount = parsed.data.tier === 'STANDARD' ? 25 : 40;
    if (!Number.isSafeInteger(state.balance + amount) || state.revision >= Number.MAX_SAFE_INTEGER - 1) {
      throw new ActivityError(409, 'run_supplies_recovery_overflow');
    }
    const revision = state.revision + 1, balanceAfter = state.balance + amount;
    const receipt = receiptSchema.parse({ version: 'run.supplies-recovery-credit.v1', nodeId, sourceRef,
      sourceVersion: parsed.data.sourceVersion, tier: parsed.data.tier, amount, balanceAfter, revision });
    const hash = await sha256Hex(canonicalJson(parsed.data));
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO run_supplies_recovery_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_supplies_state w ON w.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND s.snapshot_json=? AND w.revision=? AND w.balance=?
              AND w.policy_version=json_extract(s.snapshot_json,'$.runSupplies.version')
              AND w.do04_rank=json_extract(s.snapshot_json,'$.runSupplies.do04Rank')
              AND w.initial_balance=json_extract(s.snapshot_json,'$.runSupplies.initialBalance')
              AND NOT EXISTS(SELECT 1 FROM run_supplies_recovery_credits c
                WHERE c.run_id=r.id AND (c.node_id=? OR c.source_ref=?)))
            THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, state.revision, state.balance, nodeId, sourceRef),
        this.db.prepare('UPDATE run_supplies_state SET balance=?,revision=? WHERE run_id=? AND revision=? AND balance=?')
          .bind(balanceAfter, revision, runId, state.revision, state.balance),
        this.db.prepare(`INSERT INTO run_supplies_ledger(run_id,source_kind,source_ref,revision,delta,balance_after)
          VALUES(?,'RECOVERY_NODE',?,?,?,?)`).bind(runId, nodeId, revision, amount, balanceAfter),
        this.db.prepare(`INSERT INTO run_supplies_recovery_credits
          (run_id,node_id,source_ref,source_version,tier,amount,revision,payload_hash,receipt_json)
          VALUES(?,?,?,?,?,?,?,?,?)`).bind(runId, nodeId, sourceRef, parsed.data.sourceVersion,
          parsed.data.tier, amount, revision, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM run_supplies_recovery_write_guard'),
      ]);
    } catch (error) {
      const raced = await this.original(runId, nodeId);
      if (raced) {
        if (raced.source_ref !== sourceRef || raced.payload_hash !== hash) {
          throw new ActivityError(409, 'run_supplies_recovery_source_conflict');
        }
        return this.parseOriginal(raced);
      }
      const collidedSource = await this.db.prepare('SELECT node_id FROM run_supplies_recovery_credits WHERE run_id=? AND source_ref=?')
        .bind(runId, sourceRef).first<{ node_id: string }>();
      if (collidedSource) throw new ActivityError(409, 'run_supplies_recovery_source_conflict');
      if (/valid=1|UNIQUE constraint failed|revision_conflict/iu.test(String(error))) {
        throw new ActivityError(409, 'run_supplies_recovery_state_conflict');
      }
      throw error;
    }
    return receipt;
  }
}
