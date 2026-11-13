import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { runSuppliesStartStatements, RunSuppliesStore } from '../../apps/game-api/src/run-supplies-start';
import { CombatSupplyCreditStore, calculateCombatSupplyCredit, type CombatSupplySourceVerifier } from '../../apps/game-api/src/run-supplies-combat-credit';

async function fixture(ranks: Record<string, number> = {}) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'credit-start')).json();
  const runId = result.runId as string;
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  snapshot.encounterType = 'ORDINARY_12_NODE';
  snapshot.runSupplies = { version: 'run.supplies.v02', do04Rank: 0, initialBalance: 30 };
  snapshot.admission.commanderTraits = { ranks };
  const snapshotJson = JSON.stringify(snapshot);
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [snapshotJson]);
  await f.env.DB.batch(runSuppliesStartStatements(f.env.DB, 'a', runId, snapshotJson));
  return { ...f, runId };
}

function syntheticSource(db: D1Database, kind: 'COMBAT' | 'ELITE' = 'COMBAT'): CombatSupplySourceVerifier {
  return { prepare: async () => ({ sourceVersion: 'node-result-v1', kind,
    allInSuccess: false, targetSuccess: false,
    assertions: [db.prepare('INSERT INTO run_supplies_credit_write_guard(valid) VALUES(1)')] }) };
}

it('calculates each approved combat component from the base, without compounding', () => {
  expect(calculateCombatSupplyCredit('COMBAT', 4, true, true, 1, 1)).toEqual({
    base: 20, do01: 1, dc01: 5, dc02: 4, total: 30,
  });
  expect(calculateCombatSupplyCredit('ELITE', 4, true, true, 1, 1)).toEqual({
    base: 25, do01: 2, dc01: 6, dc02: 5, total: 38,
  });
  expect(() => calculateCombatSupplyCredit('COMBAT', 0, true, false, 0, 0)).toThrow('run_supplies_credit_policy_invalid');
});

it('requires an original result proof and credits each node once with terminal replay', async () => {
  const f = await fixture();
  await expect(new CombatSupplyCreditStore(f.env.DB).credit('a', f.runId, 'node-1', 'result-1'))
    .rejects.toThrow('run_supplies_credit_source_required');
  const store = new CombatSupplyCreditStore(f.env.DB, syntheticSource(f.env.DB));
  const first = await store.credit('a', f.runId, 'node-1', 'result-1');
  expect(first).toMatchObject({ version: 'run.supplies-combat-credit.v1', nodeId: 'node-1',
    sourceRef: 'result-1', components: { total: 20 }, balanceAfter: 50, revision: 2 });
  expect((await new RunSuppliesStore(f.env.DB).read('a', f.runId))?.balance).toBe(50);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.credit('a', f.runId, 'node-1', 'result-1')).toEqual(first);
  await expect(store.credit('a', f.runId, 'node-1', 'different')).rejects.toThrow('run_supplies_credit_source_conflict');
  await expect(store.credit('b', f.runId, 'node-1', 'result-1')).rejects.toThrow('run_supplies_run_not_found');
  expect(f.scalar("SELECT count(*) FROM run_supplies_ledger WHERE source_kind='COMBAT_NODE'")).toBe(1);
});

it('uses the pinned commander ranks and rejects unsupported bonus claims', async () => {
  const f = await fixture({ DO01: 4, DC01: 1, DC02: 1 });
  const store = new CombatSupplyCreditStore(f.env.DB, { prepare: async () => ({ sourceVersion: 'node-result-v1',
    kind: 'ELITE', allInSuccess: true, targetSuccess: true,
    assertions: [f.env.DB.prepare('INSERT INTO run_supplies_credit_write_guard(valid) VALUES(1)')] }) });
  expect((await store.credit('a', f.runId, 'elite-1', 'result-elite')).components).toEqual({
    base: 25, do01: 2, dc01: 6, dc02: 5, total: 38,
  });
  const unsupported = await fixture();
  const bad = new CombatSupplyCreditStore(unsupported.env.DB, { prepare: async () => ({ sourceVersion: 'node-result-v1',
    kind: 'COMBAT', allInSuccess: true, targetSuccess: false,
    assertions: [unsupported.env.DB.prepare('INSERT INTO run_supplies_credit_write_guard(valid) VALUES(1)')] }) });
  await expect(bad.credit('a', unsupported.runId, 'node-2', 'result-2')).rejects.toThrow('run_supplies_credit_policy_invalid');
  expect(unsupported.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
});

it('rolls back balance, ledger and receipt when source proof changes or receipt insert fails', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE credit_source_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO credit_source_test VALUES(1)');
  const store = new CombatSupplyCreditStore(f.env.DB, { prepare: async () => ({ sourceVersion: 'node-result-v1',
    kind: 'COMBAT', allInSuccess: false, targetSuccess: false,
    assertions: [f.env.DB.prepare('INSERT INTO run_supplies_credit_write_guard(valid) SELECT valid FROM credit_source_test')] }) });
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE credit_source_test SET valid=0'); };
  await expect(store.credit('a', f.runId, 'node-1', 'result-1')).rejects.toThrow('run_supplies_credit_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  f.sql.run('UPDATE credit_source_test SET valid=1');
  f.sql.run("CREATE TRIGGER fail_credit_receipt BEFORE INSERT ON run_supplies_node_credits BEGIN SELECT RAISE(ABORT,'injected-credit'); END");
  await expect(store.credit('a', f.runId, 'node-1', 'result-1')).rejects.toThrow('injected-credit');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar("SELECT count(*) FROM run_supplies_ledger WHERE source_kind='COMBAT_NODE'")).toBe(0);
});

it('does not treat a state revision without a matching ledger entry as spendable balance', async () => {
  const f = await fixture();
  f.sql.run('UPDATE run_supplies_state SET revision=revision+1,balance=balance+1');
  await expect(new RunSuppliesStore(f.env.DB).read('a', f.runId)).rejects.toThrow('run_supplies_storage_invalid');
});
