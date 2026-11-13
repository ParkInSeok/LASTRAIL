import { expect, it, vi } from 'vitest';
import initSqlJs from 'sql.js';
import { readFileSync } from 'node:fs';
import { OrdinaryLocalExecutionStore, ordinaryLocalInputSchema } from '../../apps/game-api/src/ordinary-local-execution';
import { SqlJsD1Database } from '../helpers/growth-d1';

it('persists only replayed inputs, recovers receipts, and fences conflicting sequences atomically', async () => {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.run('CREATE TABLE runs(id TEXT PRIMARY KEY); INSERT INTO runs VALUES(\'r\'); CREATE TABLE operation_map_write_guard(valid INTEGER CHECK(valid=1));');
  sql.run(readFileSync('apps/game-api/local/ordinary-execution.sql', 'utf8'));
  const db = new SqlJsD1Database(sql) as unknown as D1Database;
  let now = 0, calls = 0, unavailable = false;
  const engine = { pin: { unitTestOnly: true }, replay: async (raw: unknown) => {
    calls++; if (unavailable) throw new Error('actual execution unavailable');
    const input = raw as { inputs: unknown[] };
    return { version: 'ordinary.local-execution-result.v1.20261106', accountId: 'a', runId: 'r', nodeId: 'n',
      attemptGeneration: 1, snapshotHash: 'a'.repeat(64), lastSequence: input.inputs.length, tick: input.inputs.length,
      checkpoint: { testOnly: true }, obstacleResolution: null, evidence: null, healthChanges: [] };
  } };
  const store = new OrdinaryLocalExecutionStore(db, engine, () => now);
  // Isolated journal test only; production source reads original DB ownership and the geometry registry.
  vi.spyOn(store, 'source').mockResolvedValue({ input: {} as never, entry: {}, startSnapshot: '{}',
    assertion: db.prepare('INSERT INTO operation_map_write_guard VALUES(1)'), assertions: [] });
  const input = { runId: 'r', nodeId: 'n', attemptGeneration: 1, snapshotHash: 'a'.repeat(64), expectedSequence: 0, inputs: [] };
  try {
    const first = await store.commit('a', 'open', input);
    unavailable = true;
    expect(await store.commit('a', 'open', input)).toEqual(first); expect(calls).toBe(1);
    await expect(store.commit('b', 'open', input)).rejects.toThrow('request_conflict');
    await expect(store.commit('a', 'open', { ...input, expectedSequence: 1 })).rejects.toThrow('request_conflict');
    const advance = { ...input, inputs: [{ sequence: 1, tick: 0, command: { kind: 'ADVANCE', elapsed: 10000000, budget: 100 } }] };
    await expect(store.commit('a', 'advance', advance)).rejects.toThrow('time_budget');
    now = 1000;
    await expect(store.commit('a', 'advance', advance)).rejects.toThrow('actual execution unavailable');
    expect(sql.exec('SELECT sequence FROM ordinary_local_execution')[0].values[0][0]).toBe(0);
    unavailable = false;
    expect((await store.commit('a', 'advance', advance)).result.lastSequence).toBe(1);
    await expect(store.commit('a', 'stale', advance)).rejects.toThrow('sequence_conflict');
    expect(sql.exec('SELECT COUNT(*) FROM ordinary_local_execution_receipts')[0].values[0][0]).toBe(2);
  } finally { sql.close(); }
});

it('rejects HP injection and uses the existing clock half-units, not floating speed aliases', () => {
  const body = { runId: 'r', nodeId: 'n', attemptGeneration: 1, snapshotHash: 'a'.repeat(64), expectedSequence: 0 };
  expect(ordinaryLocalInputSchema.safeParse({ ...body, inputs: [{ sequence: 1, tick: 0, command: { kind: 'SPEED', value: 3 } }] }).success).toBe(true);
  for (const command of [{ kind: 'SPEED', value: 1.5 }, { kind: 'DAMAGE', hp: 0 }, { kind: 'ADVANCE', elapsed: 10000001, budget: 100 }])
    expect(ordinaryLocalInputSchema.safeParse({ ...body, inputs: [{ sequence: 1, tick: 0, command }] }).success).toBe(false);
});
