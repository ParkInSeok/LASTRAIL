import { expect, it } from 'vitest';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { prepareOrdinaryNodeExecutionBinding } from '../../apps/game-api/src/ordinary-node-execution-binding';
import initSqlJs from 'sql.js';
import { readFileSync } from 'node:fs';
import { SqlJsD1Database } from '../helpers/growth-d1';
async function fixture() {
  const definitionRef = { definitionId: 'geometry', version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: 'a'.repeat(64) };
  const startSnapshot = canonicalJson({ ordinaryGeometryReference: definitionRef });
  const geometryBinding = { version: 'ordinary.node-geometry-binding.v1.20261031', accountId: 'account', runId: 'run', nodeId: 'node',
    entryMapRevision: 3, originGeneration: 1, definitionRef };
  const origin = { executionVersion: 'combat.ordinary-five-execution.v1.20261028', determinism: {
    version: 'ordinary.node-random.v1.20261029', algorithm: 'XORSHIFT32_13_17_5_V1', seed: 37, initialState: 37 } };
  const snapshot = { version: 'ordinary.node-entry-state.v1.20261028', accountId: 'account', runId: 'run', nodeId: 'node',
    entryMapRevision: 3, runtime: { ...origin, spatial: { definitionBinding: geometryBinding } } };
  const snapshotJson = canonicalJson(snapshot), snapshotHash = await sha256Hex(snapshotJson);
  const row = { start_snapshot_json: startSnapshot, snapshot_json: snapshotJson, snapshot_hash: snapshotHash, generation: 2 };
  const db = { prepare: (sql: string) => ({ bind: (...args: unknown[]) => ({ first: async () => row, sql, args }) }) } as unknown as D1Database;
  const context = { accountId: 'account', runId: 'run', nodeId: 'node', attemptGeneration: 2, startSnapshot };
  return { db, row, context, observation: { ...origin, snapshotHash, geometryBinding, nodeId: 'node', attemptGeneration: 2 } };
}
it('binds the restarted attempt to the unchanged original seed and geometry generation', async () => {
  const f = await fixture();
  const { assertions } = await prepareOrdinaryNodeExecutionBinding(f.db, f.context, f.observation);
  expect(assertions).toHaveLength(1);
  expect((assertions[0] as unknown as { args: unknown[] }).args).toContain(2);
  expect(f.observation.geometryBinding.originGeneration).toBe(1);
});
it('rejects stale attempts, changed seed/geometry and corrupt stored origin', async () => {
  for (const kind of ['attempt', 'seed', 'geometry', 'origin', 'start', 'missing']) {
    const f = await fixture();
    if (kind === 'attempt') f.observation.attemptGeneration = 1;
    if (kind === 'seed') { f.observation.determinism.seed = 38; f.observation.determinism.initialState = 38; }
    if (kind === 'geometry') f.observation.geometryBinding.definitionRef.definitionHash = 'b'.repeat(64);
    if (kind === 'origin') f.row.snapshot_json += ' ';
    if (kind === 'start') f.context.startSnapshot = '{}';
    await expect(prepareOrdinaryNodeExecutionBinding(f.db, f.context, kind === 'missing' ? {} : f.observation)).rejects.toThrow();
  }
});
it('fences a restart between verification and the enclosing commit in SQLite', async () => {
  const f = await fixture(), SQL = await initSqlJs(), sql = new SQL.Database();
  try {
    sql.run('CREATE TABLE runs(id TEXT PRIMARY KEY); INSERT INTO runs VALUES (\'run\'); CREATE TABLE operation_map_write_guard(valid INTEGER CHECK(valid=1));');
    sql.run(readFileSync('packages/db/migrations/0142_ordinary_node_attempts.sql', 'utf8'));
    sql.run('INSERT INTO ordinary_node_attempts VALUES(?,?,?,?,?,?)', ['run', 'node', f.row.start_snapshot_json,
      f.row.snapshot_json, f.row.snapshot_hash, 2]);
    const db = new SqlJsD1Database(sql) as unknown as D1Database;
    const { assertions } = await prepareOrdinaryNodeExecutionBinding(db, f.context, f.observation);
    await db.batch(assertions);
    sql.run('UPDATE ordinary_node_attempts SET generation=3');
    await expect(db.batch(assertions)).rejects.toThrow();
    await expect(prepareOrdinaryNodeExecutionBinding(db, f.context, f.observation)).rejects.toThrow();
    await db.batch((await prepareOrdinaryNodeExecutionBinding(db, { ...f.context, attemptGeneration: 3 },
      { ...f.observation, attemptGeneration: 3 })).assertions);
  } finally { sql.close(); }
});
