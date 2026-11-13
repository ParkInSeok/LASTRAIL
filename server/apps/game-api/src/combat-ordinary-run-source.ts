import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { readOrdinaryFiveStatOrigin } from './combat-ordinary-stat-owner';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);

/** Account-scoped immutable START intake. Never reads current account growth or
 * substitutes the original maximum for current HP. Historical runs without this
 * source remain unsupported rather than being upgraded from today's lobby. */
export async function readOrdinaryRunStatOrigin(db: D1Database, accountId: string, runId: string) {
  id.parse(accountId); id.parse(runId);
  const row = await db.prepare('SELECT s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?')
    .bind(runId, accountId).first<{ snapshot_json: string }>();
  if (!row) throw new ActivityError(409, 'ordinary_stat_run_not_found');
  const snapshot = z.object({ ordinaryStatSource: z.strictObject({ origin: z.unknown(), originHash: z.string() }) }).safeParse(JSON.parse(row.snapshot_json));
  if (!snapshot.success) throw new ActivityError(409, 'ordinary_stat_origin_unavailable');
  const source = snapshot.data.ordinaryStatSource;
  const origin = await readOrdinaryFiveStatOrigin(source.origin, source.originHash);
  if (origin.accountId !== accountId || origin.runId !== runId) throw new ActivityError(409, 'ordinary_stat_scope_conflict');
  return { origin, originHash: source.originHash, startSnapshotJson: row.snapshot_json };
}
