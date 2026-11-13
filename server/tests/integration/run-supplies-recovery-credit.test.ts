import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { runSuppliesStartStatements, RunSuppliesStore } from '../../apps/game-api/src/run-supplies-start';
import { CombatSupplyCreditStore } from '../../apps/game-api/src/run-supplies-combat-credit';
import { RecoverySupplyCreditStore, type RecoverySupplySourceVerifier } from '../../apps/game-api/src/run-supplies-recovery-credit';

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'recovery-start')).json();
  const runId = result.runId as string;
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  snapshot.encounterType = 'ORDINARY_12_NODE';
  snapshot.runSupplies = { version: 'run.supplies.v02', do04Rank: 0, initialBalance: 30 };
  const snapshotJson = JSON.stringify(snapshot);
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [snapshotJson]);
  await f.env.DB.batch(runSuppliesStartStatements(f.env.DB, 'a', runId, snapshotJson));
  return { ...f, runId };
}

function syntheticSource(db: D1Database, tier: 'STANDARD' | 'HIGH_RISK' = 'STANDARD'): RecoverySupplySourceVerifier {
  return { prepare: async () => ({ sourceVersion: 'recovery-result-v1', tier, outcome: 'SUCCEEDED',
    assertions: [db.prepare('INSERT INTO run_supplies_recovery_write_guard(valid) VALUES(1)')] }) };
}

it('requires verified success and credits exactly 25 or 40, never both for one node', async () => {
  const f = await fixture();
  await expect(new RecoverySupplyCreditStore(f.env.DB).credit('a', f.runId, 'node-3', 'objective-3'))
    .rejects.toThrow('run_supplies_recovery_source_required');
  const standard = new RecoverySupplyCreditStore(f.env.DB, syntheticSource(f.env.DB));
  const first = await standard.credit('a', f.runId, 'node-3', 'objective-3');
  expect(first).toMatchObject({ version: 'run.supplies-recovery-credit.v1', tier: 'STANDARD',
    amount: 25, balanceAfter: 55, revision: 2 });
  const high = new RecoverySupplyCreditStore(f.env.DB, syntheticSource(f.env.DB, 'HIGH_RISK'));
  await expect(high.credit('a', f.runId, 'node-3', 'other-objective'))
    .rejects.toThrow('run_supplies_recovery_source_conflict');
  await expect(high.credit('a', f.runId, 'node-7', 'objective-3'))
    .rejects.toThrow('run_supplies_recovery_source_conflict');
  const second = await high.credit('a', f.runId, 'node-7', 'objective-7');
  expect(second).toMatchObject({ tier: 'HIGH_RISK', amount: 40, balanceAfter: 95, revision: 3 });
  expect((await new RunSuppliesStore(f.env.DB).read('a', f.runId))?.balance).toBe(95);
  expect(f.scalar("SELECT count(*) FROM run_supplies_ledger WHERE source_kind='RECOVERY_NODE'")).toBe(2);
});

it('replays the original outcome after terminal state and rejects another owner or unsupported result', async () => {
  const f = await fixture(), store = new RecoverySupplyCreditStore(f.env.DB, syntheticSource(f.env.DB));
  const first = await store.credit('a', f.runId, 'node-3', 'objective-3');
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.credit('a', f.runId, 'node-3', 'objective-3')).toEqual(first);
  await expect(store.credit('b', f.runId, 'node-3', 'objective-3')).rejects.toThrow('run_supplies_run_not_found');
  const g = await fixture();
  const failure = new RecoverySupplyCreditStore(g.env.DB, { prepare: async () => ({
    sourceVersion: 'recovery-result-v1', tier: 'HIGH_RISK', outcome: 'FAILED',
    assertions: [g.env.DB.prepare('INSERT INTO run_supplies_recovery_write_guard(valid) VALUES(1)')],
  }) });
  await expect(failure.credit('a', g.runId, 'node-3', 'objective-3'))
    .rejects.toThrow('run_supplies_recovery_source_invalid');
  expect(g.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
});

it('uses the same run wallet as combat income without reissuing the combat base', async () => {
  const f = await fixture();
  const combat = new CombatSupplyCreditStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'node-result-v1', kind: 'COMBAT', allInSuccess: false, targetSuccess: false,
    assertions: [f.env.DB.prepare('INSERT INTO run_supplies_credit_write_guard(valid) VALUES(1)')],
  }) });
  await combat.credit('a', f.runId, 'node-3', 'combat-3');
  const recovery = new RecoverySupplyCreditStore(f.env.DB, syntheticSource(f.env.DB));
  expect(await recovery.credit('a', f.runId, 'node-3', 'objective-3')).toMatchObject({
    amount: 25, balanceAfter: 75, revision: 3,
  });
  expect(f.scalar("SELECT count(*) FROM run_supplies_ledger WHERE source_kind='COMBAT_NODE'")).toBe(1);
});

it('rolls back credit on same-batch proof race, wallet conflict and receipt insertion failure', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE recovery_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO recovery_test VALUES(1)');
  const store = new RecoverySupplyCreditStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'recovery-result-v1', tier: 'STANDARD', outcome: 'SUCCEEDED',
    assertions: [f.env.DB.prepare('INSERT INTO run_supplies_recovery_write_guard(valid) SELECT valid FROM recovery_test')],
  }) });
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE recovery_test SET valid=0'); };
  await expect(store.credit('a', f.runId, 'node-3', 'objective-3'))
    .rejects.toThrow('run_supplies_recovery_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  f.sql.run('UPDATE recovery_test SET valid=1');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE run_supplies_state SET revision=revision+1,balance=balance+1');
    f.sql.run("INSERT INTO run_supplies_ledger(run_id,source_kind,source_ref,revision,delta,balance_after) VALUES(?, 'TEST', 'race', 2, 1, 31)", [f.runId]); };
  await expect(store.credit('a', f.runId, 'node-3', 'objective-3'))
    .rejects.toThrow('run_supplies_recovery_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_supplies_recovery_credits')).toBe(0);
  f.sql.run("CREATE TRIGGER fail_recovery_credit BEFORE INSERT ON run_supplies_recovery_credits BEGIN SELECT RAISE(ABORT,'injected-recovery'); END");
  await expect(store.credit('a', f.runId, 'node-3', 'objective-3')).rejects.toThrow('injected-recovery');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(31);
  expect(f.scalar("SELECT count(*) FROM run_supplies_ledger WHERE source_kind='RECOVERY_NODE'")).toBe(0);
});
