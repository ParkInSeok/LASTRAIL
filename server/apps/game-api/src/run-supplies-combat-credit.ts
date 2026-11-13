import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { RunSuppliesStore } from './run-supplies-start';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const source = z.strictObject({ sourceVersion: id, kind: z.enum(['COMBAT', 'ELITE']),
  allInSuccess: z.boolean(), targetSuccess: z.boolean() });
const componentsSchema = z.strictObject({ base: z.number().int().positive(), do01: z.number().int().nonnegative(),
  dc01: z.number().int().nonnegative(), dc02: z.number().int().nonnegative(), total: z.number().int().positive() });
const receiptSchema = z.strictObject({ version: z.literal('run.supplies-combat-credit.v1'), nodeId: id,
  sourceRef: id, sourceVersion: id, kind: z.enum(['COMBAT', 'ELITE']), components: componentsSchema,
  balanceAfter: z.number().int().nonnegative(), revision: z.number().int().positive() });
type Kind = z.infer<typeof source>['kind'];
type Receipt = z.infer<typeof receiptSchema>;
type CreditRow = { source_ref: string; payload_hash: string; receipt_json: string };

export function calculateCombatSupplyCredit(kind: Kind, do01Rank: number,
  allInSuccess: boolean, targetSuccess: boolean, dc01Rank: number, dc02Rank: number) {
  if ((kind !== 'COMBAT' && kind !== 'ELITE') || !Number.isInteger(do01Rank) || do01Rank < 0 || do01Rank > 4
    || !Number.isInteger(dc01Rank) || dc01Rank < 0 || dc01Rank > 1
    || !Number.isInteger(dc02Rank) || dc02Rank < 0 || dc02Rank > 1
    || typeof allInSuccess !== 'boolean' || typeof targetSuccess !== 'boolean'
    || (allInSuccess && dc01Rank !== 1) || (targetSuccess && dc02Rank !== 1)) {
    throw new Error('run_supplies_credit_policy_invalid');
  }
  const base = kind === 'COMBAT' ? 20 : 25;
  const do01 = Math.floor(base * do01Rank * 2 / 100);
  const dc01 = allInSuccess ? Math.floor(base * 25 / 100) : 0;
  const dc02 = targetSuccess ? Math.floor(base * 20 / 100) : 0;
  return { base, do01, dc01, dc02, total: base + do01 + dc01 + dc02 };
}

export interface CombatSupplySourceVerifier {
  /** Real node completion must prove source, type and successful objectives with
   * same-batch assertions. No production adapter or public route is installed. */
  prepare(context: { accountId: string; runId: string; nodeId: string; sourceRef: string }): Promise<{
    sourceVersion: string; kind: Kind; allInSuccess: boolean; targetSuccess: boolean;
    assertions: D1PreparedStatement[];
  }>;
}

/** Internal, once-per-node run supply income. It does not infer combat completion. */
export class CombatSupplyCreditStore {
  constructor(private readonly db: D1Database, private readonly verifier?: CombatSupplySourceVerifier) {}

  private original(runId: string, nodeId: string): Promise<CreditRow | null> {
    return this.db.prepare('SELECT source_ref,payload_hash,receipt_json FROM run_supplies_node_credits WHERE run_id=? AND node_id=?')
      .bind(runId, nodeId).first<CreditRow>();
  }

  private parseOriginal(row: CreditRow): Receipt {
    try { return receiptSchema.parse(JSON.parse(row.receipt_json)); }
    catch { throw new ActivityError(503, 'run_supplies_credit_storage_invalid'); }
  }

  async credit(accountId: string, runId: string, nodeId: string, sourceRef: string): Promise<Receipt> {
    if (!id.safeParse(nodeId).success || !id.safeParse(sourceRef).success) {
      throw new ActivityError(400, 'run_supplies_credit_request_invalid');
    }
    const state = await new RunSuppliesStore(this.db).read(accountId, runId);
    if (!state) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    const old = await this.original(runId, nodeId);
    if (old) {
      if (old.source_ref !== sourceRef) throw new ActivityError(409, 'run_supplies_credit_source_conflict');
      return this.parseOriginal(old);
    }
    const run = await this.db.prepare('SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?')
      .bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'run_supplies_run_not_active');
    if (!this.verifier) throw new ActivityError(409, 'run_supplies_credit_source_required');
    const proof = await this.verifier.prepare({ accountId, runId, nodeId, sourceRef });
    const parsed = source.safeParse({ sourceVersion: proof?.sourceVersion, kind: proof?.kind,
      allInSuccess: proof?.allInSuccess, targetSuccess: proof?.targetSuccess });
    if (!parsed.success || !Array.isArray(proof.assertions) || proof.assertions.length === 0) {
      throw new ActivityError(409, 'run_supplies_credit_source_required');
    }
    let ranks: Record<string, number>;
    try {
      const snapshot = JSON.parse(run.snapshot_json) as { admission?: { commanderTraits?: { ranks?: Record<string, number> } } };
      ranks = snapshot.admission?.commanderTraits?.ranks ?? {};
    } catch { throw new ActivityError(503, 'run_supplies_storage_invalid'); }
    const do01Rank = ranks.DO01 ?? 0, dc01Rank = ranks.DC01 ?? 0, dc02Rank = ranks.DC02 ?? 0;
    let components: ReturnType<typeof calculateCombatSupplyCredit>;
    try { components = calculateCombatSupplyCredit(parsed.data.kind, do01Rank,
      parsed.data.allInSuccess, parsed.data.targetSuccess, dc01Rank, dc02Rank); }
    catch { throw new ActivityError(409, 'run_supplies_credit_policy_invalid'); }
    if (!Number.isSafeInteger(state.balance + components.total) || state.revision >= Number.MAX_SAFE_INTEGER - 1) {
      throw new ActivityError(409, 'run_supplies_credit_overflow');
    }
    const balanceAfter = state.balance + components.total, revision = state.revision + 1;
    const receipt = receiptSchema.parse({ version: 'run.supplies-combat-credit.v1', nodeId, sourceRef,
      sourceVersion: parsed.data.sourceVersion, kind: parsed.data.kind, components, balanceAfter, revision });
    const hash = await sha256Hex(canonicalJson({ ...parsed.data, do01Rank, dc01Rank, dc02Rank }));
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO run_supplies_credit_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_supplies_state w ON w.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND s.snapshot_json=? AND w.revision=? AND w.balance=?
              AND NOT EXISTS(SELECT 1 FROM run_supplies_node_credits c WHERE c.run_id=r.id AND c.node_id=?))
            THEN 1 ELSE 0 END`).bind(runId, accountId, run.snapshot_json, state.revision, state.balance, nodeId),
        this.db.prepare('UPDATE run_supplies_state SET balance=?,revision=? WHERE run_id=? AND revision=? AND balance=?')
          .bind(balanceAfter, revision, runId, state.revision, state.balance),
        this.db.prepare(`INSERT INTO run_supplies_ledger
          (run_id,source_kind,source_ref,revision,delta,balance_after) VALUES(?,'COMBAT_NODE',?,?,?,?)`)
          .bind(runId, nodeId, revision, components.total, balanceAfter),
        this.db.prepare(`INSERT INTO run_supplies_node_credits
          (run_id,node_id,source_ref,source_version,payload_hash,receipt_json) VALUES(?,?,?,?,?,?)`)
          .bind(runId, nodeId, sourceRef, parsed.data.sourceVersion, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM run_supplies_credit_write_guard'),
      ]);
    } catch (error) {
      const raced = await this.original(runId, nodeId);
      if (raced) {
        if (raced.source_ref !== sourceRef || raced.payload_hash !== hash) {
          throw new ActivityError(409, 'run_supplies_credit_source_conflict');
        }
        return this.parseOriginal(raced);
      }
      if (/valid=1|UNIQUE constraint failed|revision_conflict/iu.test(String(error))) {
        throw new ActivityError(409, 'run_supplies_credit_state_conflict');
      }
      throw error;
    }
    return receipt;
  }
}
