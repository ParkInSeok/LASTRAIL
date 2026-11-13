import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';
import { CombatDamageStore } from '../../apps/game-api/src/combat-damage-store';

const initial = [{ id: 'actor-1', hp: 333, maxHp: 1000 }];
const rebase = { eventId: 'rebase-1', expectedRevision: 1, targetId: 'actor-1', change: { kind: 'MAX_HP_REBASE', maxHp: 1500 } };
async function fixture(initialize = true) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const runId = (await (await f.start(quote.operationPreviewId, 'health-start')).json()).runId as string;
  // Explicit synthetic original-definition/safety proof, never installed in production.
  f.sql.run('CREATE TABLE fixture_health_proof(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO fixture_health_proof VALUES(1)');
  let calls = 0;
  const verifier = { prepare: async () => { calls++; return { assertions: [f.env.DB.prepare(
    'INSERT INTO combat_damage_write_guard(valid) SELECT valid FROM fixture_health_proof')] }; } };
  const store = new CombatHealthStore(f.env.DB, verifier);
  if (initialize) await store.initialize('a', runId, initial);
  return { ...f, runId, store, verifier, calls: () => calls };
}

it('persists anchor and ordered rebase/damage events in the single existing HP state row', async () => {
  const f = await fixture(), originalStart = f.scalar('SELECT snapshot_json FROM run_snapshots');
  const first = await f.store.commit('a', f.runId, rebase);
  expect(first).toMatchObject({ version: 'combat.health-receipt.v2', revision: 2, change: { kind: 'MAX_HP_REBASE' }, after: { hp: 499, anchorHp: 333 } });
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'back', expectedRevision: 2, change: { kind: 'MAX_HP_REBASE', maxHp: 1000 } });
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'damage', expectedRevision: 3, change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 300 } });
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'double', expectedRevision: 4, change: { kind: 'MAX_HP_REBASE', maxHp: 2000 } });
  const restored = await new CombatHealthStore(f.env.DB).read('a', f.runId);
  expect(restored).toMatchObject({ revision: 5, state: { version: 'combat.health-state.v2', targets: [{ id: 'actor-1', health: { hp: 600, anchorHp: 300, anchorMaxHp: 1000 } }] } });
  expect(restored.state.ledger).toHaveLength(4);
  expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(1);
  expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(originalStart);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.commit('a', f.runId, rebase)).toEqual(first);
  expect(f.calls()).toBe(5);
});

it('requires actual source proof, owner, matching event and current revision', async () => {
  const f = await fixture();
  await expect(new CombatHealthStore(f.env.DB).commit('a', f.runId, rebase)).rejects.toThrow('combat_health_verification_required');
  await expect(f.store.commit('b', f.runId, rebase)).rejects.toThrow('combat_run_not_found');
  await f.store.commit('a', f.runId, rebase);
  await expect(f.store.commit('a', f.runId, { ...rebase, change: { kind: 'MAX_HP_REBASE', maxHp: 1600 } })).rejects.toThrow('combat_health_event_conflict');
  await expect(f.store.commit('a', f.runId, { ...rebase, eventId: 'stale' })).rejects.toThrow('combat_health_revision_conflict');
  await expect(f.store.commit('a', f.runId, { ...rebase, eventId: 'wrong-damage', expectedRevision: 2,
    change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 500 } })).rejects.toThrow('combat_health_change_invalid');
});

it('preserves anchors for zero effective loss and restores a true lethal change as zero', async () => {
  const f = await fixture();
  await f.store.commit('a', f.runId, rebase);
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'zero', expectedRevision: 2, change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 499 } });
  expect((await f.store.read('a', f.runId)).state.targets[0].health.anchorHp).toBe(333);
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'lethal', expectedRevision: 3, change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 0 } });
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'increase', expectedRevision: 4, change: { kind: 'MAX_HP_REBASE', maxHp: 2000 } });
  expect((await f.store.read('a', f.runId)).state.targets[0].health.hp).toBe(0);
});

it('rejects conversion of a legacy state and does not reset a progressed new state on initialization replay', async () => {
  const f = await fixture(false), legacy = new CombatDamageStore(f.env.DB);
  await legacy.initialize('a', f.runId, initial);
  const before = await legacy.read('a', f.runId);
  await expect(f.store.initialize('a', f.runId, initial)).rejects.toThrow('combat_health_initialization_conflict');
  expect(await legacy.read('a', f.runId)).toEqual(before);
  const fresh = await fixture();
  await fresh.store.commit('a', fresh.runId, rebase);
  expect(await fresh.store.initialize('a', fresh.runId, initial)).toMatchObject({ revision: 2 });
});

it.each(['proof', 'revision', 'start', 'run', 'account', 'actors', 'hash'])('fences %s changes in the same commit batch', async kind => {
  const f = await fixture();
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    const changes: Record<string, string> = { proof: 'UPDATE fixture_health_proof SET valid=0',
      revision: 'UPDATE combat_damage_states SET revision=2',
      start: "UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureChanged',1)",
      run: "UPDATE runs SET status='COMPLETED'", account: "UPDATE accounts SET status='DELETED'",
      actors: "UPDATE combat_damage_states SET actor_state_json='{}'", hash: "UPDATE combat_damage_states SET initial_hash='changed'" };
    f.sql.run(changes[kind]);
  };
  await expect(f.store.commit('a', f.runId, rebase)).rejects.toThrow('combat_health_state_conflict');
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
  expect(JSON.parse(String(f.scalar('SELECT state_json FROM combat_damage_states'))).targets[0].health.hp).toBe(333);
});

it('requires nonempty proof at initialization and rejects actor-state replacement without losing it', async () => {
  const f = await fixture(false);
  await expect(new CombatHealthStore(f.env.DB).initialize('a', f.runId, initial)).rejects.toThrow('combat_health_verification_required');
  await expect(new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [] }) }).initialize('a', f.runId, initial))
    .rejects.toThrow('combat_health_verification_required');
  expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
  await f.store.initialize('a', f.runId, initial);
  f.sql.run("UPDATE combat_damage_states SET actor_state_json='{}'");
  await expect(f.store.commit('a', f.runId, rebase)).rejects.toThrow('combat_health_storage_invalid');
  expect(f.scalar('SELECT actor_state_json FROM combat_damage_states')).toBe('{}');
});

it('records real healing before a rebase while zero effective healing keeps the anchor', async () => {
  const f = await fixture();
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'heal', change: { kind: 'ACTUAL_HP', reason: 'HEAL', hp: 433 } });
  await f.store.commit('a', f.runId, { ...rebase, expectedRevision: 2, change: { kind: 'MAX_HP_REBASE', maxHp: 2000 } });
  await f.store.commit('a', f.runId, { ...rebase, eventId: 'zero-heal', expectedRevision: 3, change: { kind: 'ACTUAL_HP', reason: 'HEAL', hp: 866 } });
  const result = await f.store.read('a', f.runId);
  expect(result.state.targets[0].health).toMatchObject({ hp: 866, anchorHp: 433, anchorMaxHp: 1000 });
  await expect(f.store.commit('a', f.runId, { ...rebase, eventId: 'bad-heal', expectedRevision: 4, change: { kind: 'ACTUAL_HP', reason: 'HEAL', hp: 865 } }))
    .rejects.toThrow('combat_health_change_invalid');
});

it('reconstructs ledger transitions rather than accepting two mutually consistent but forged checkpoints', async () => {
  const f = await fixture();
  await f.store.commit('a', f.runId, rebase);
  const state = JSON.parse(String(f.scalar('SELECT state_json FROM combat_damage_states')));
  const forged = { version: 'health.max-hp-rebase.v1', hp: 750, maxHp: 1500, anchorHp: 500, anchorMaxHp: 1000 };
  state.ledger[0].after = forged; state.targets[0].health = forged;
  f.sql.run('UPDATE combat_damage_states SET state_json=?', [JSON.stringify(state)]);
  await expect(f.store.read('a', f.runId)).rejects.toThrow('combat_health_storage_invalid');
});

it('rolls back all health and anchor changes if receipt storage fails', async () => {
  const f = await fixture(), before = await f.store.read('a', f.runId);
  f.sql.run("CREATE TRIGGER fixture_health_receipt BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'fixture-health-failed'); END");
  await expect(f.store.commit('a', f.runId, rebase)).rejects.toThrow('fixture-health-failed');
  expect(await f.store.read('a', f.runId)).toEqual(before);
});

it('recovers a lost response without a second application', async () => {
  const f = await fixture(), batch = f.env.DB.batch.bind(f.env.DB);
  f.env.DB.batch = async statements => { const result = await batch(statements); f.env.DB.batch = batch; void result; throw new Error('lost response'); };
  expect(await f.store.commit('a', f.runId, rebase)).toMatchObject({ revision: 2, after: { hp: 499 } });
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(1);
});

it('rejects corrupted stored anchors or a ledger that does not reconstruct the checkpoint', async () => {
  const f = await fixture();
  await f.store.commit('a', f.runId, rebase);
  f.sql.run("UPDATE combat_damage_states SET state_json=json_set(state_json,'$.targets[0].health.anchorHp',500)");
  await expect(f.store.read('a', f.runId)).rejects.toThrow('combat_health_storage_invalid');
});
