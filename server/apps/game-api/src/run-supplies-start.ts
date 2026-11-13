import { z } from 'zod';
import { ActivityError } from './activity-policy';

const pinSchema = z.strictObject({ version: z.literal('run.supplies.v02'),
  do04Rank: z.number().int().min(0).max(4), initialBalance: z.number().int().min(30).max(50) });
type Pin = z.infer<typeof pinSchema>;
type StateRow = { account_id: string; policy_version: string; do04_rank: number; initial_balance: number;
  balance: number; revision: number; start_delta: number | null; start_balance_after: number | null;
  ledger_revision: number | null; ledger_balance_after: number | null };

export function initialRunSupplies(do04Rank: number): number {
  if (!Number.isInteger(do04Rank) || do04Rank < 0 || do04Rank > 4) throw new Error('run_supplies_rank_invalid');
  return 30 + do04Rank * 5;
}

export function runSuppliesStartStatements(db: D1Database, accountId: string, runId: string,
  snapshotJson: string): D1PreparedStatement[] {
  const snapshot: unknown = JSON.parse(snapshotJson);
  const data = snapshot as { encounterType?: unknown; runSupplies?: unknown };
  if (data.encounterType !== 'ORDINARY_12_NODE') return [];
  const pin = pinSchema.parse(data.runSupplies);
  if (pin.initialBalance !== initialRunSupplies(pin.do04Rank)) throw new Error('run_supplies_start_invalid');
  return [
    db.prepare(`INSERT INTO run_supplies_state
      (run_id,account_id,policy_version,do04_rank,initial_balance,balance,revision) VALUES(?,?,?,?,?,?,1)`)
      .bind(runId, accountId, pin.version, pin.do04Rank, pin.initialBalance, pin.initialBalance),
    db.prepare(`INSERT INTO run_supplies_ledger
      (run_id,source_kind,source_ref,revision,delta,balance_after) VALUES(?,'START',?,1,?,?)`)
      .bind(runId, runId, pin.initialBalance, pin.initialBalance),
  ];
}

/** Owner-bound run economy state. Later earning/spending must update this and its ledger atomically. */
export class RunSuppliesStore {
  constructor(private readonly db: D1Database) {}

  async read(accountId: string, runId: string): Promise<(Pin & { balance: number; revision: number }) | null> {
    const run = await this.db.prepare('SELECT snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?')
      .bind(runId, accountId).first<{ snapshot_json: string }>();
    if (!run) throw new ActivityError(409, 'run_supplies_run_not_found');
    const row = await this.db.prepare(`SELECT w.*,st.delta AS start_delta,st.balance_after AS start_balance_after,
      latest.revision AS ledger_revision,latest.balance_after AS ledger_balance_after
      FROM run_supplies_state w
      LEFT JOIN run_supplies_ledger st ON st.run_id=w.run_id AND st.source_kind='START'
        AND st.source_ref=w.run_id AND st.revision=1
      LEFT JOIN run_supplies_ledger latest ON latest.run_id=w.run_id AND latest.revision=w.revision
      WHERE w.run_id=?`).bind(runId).first<StateRow>();
    let pin: Pin | null;
    try {
      const snapshot: unknown = JSON.parse(run.snapshot_json);
      const data = snapshot as { encounterType?: unknown; runSupplies?: unknown };
      if (data.encounterType !== 'ORDINARY_12_NODE' || data.runSupplies === undefined) pin = null;
      else pin = pinSchema.parse(data.runSupplies);
    } catch { throw new ActivityError(503, 'run_supplies_storage_invalid'); }
    if (!pin) {
      if (row) throw new ActivityError(503, 'run_supplies_storage_invalid');
      return null;
    }
    if (!row) throw new ActivityError(503, 'run_supplies_state_missing');
    if (row.account_id !== accountId || row.policy_version !== pin.version || row.do04_rank !== pin.do04Rank
      || row.initial_balance !== pin.initialBalance || pin.initialBalance !== initialRunSupplies(pin.do04Rank)
      || !Number.isSafeInteger(row.balance) || row.balance < 0 || !Number.isSafeInteger(row.revision) || row.revision < 1
      || row.start_delta !== pin.initialBalance || row.start_balance_after !== pin.initialBalance
      || row.ledger_revision !== row.revision || row.ledger_balance_after !== row.balance) {
      throw new ActivityError(503, 'run_supplies_storage_invalid');
    }
    return { ...pin, balance: row.balance, revision: row.revision };
  }
}
