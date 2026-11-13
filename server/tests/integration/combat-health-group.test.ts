import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';

const commands = [
  { eventId: 'swap-a', expectedRevision: 1, targetId: 'a-1', change: { kind: 'MAX_HP_REBASE', maxHp: 2000 } },
  { eventId: 'swap-b', expectedRevision: 2, targetId: 'a-2', change: { kind: 'MAX_HP_REBASE', maxHp: 500 } },
];
async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const runId = (await (await f.start(quote.operationPreviewId, 'health-group-start')).json()).runId as string;
  let proofs = 0;
  const store = new CombatHealthStore(f.env.DB, { prepare: async () => { proofs++; return { assertions: [
    f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }; } });
  await store.initialize('a', runId, [{ id: 'a-1', hp: 333, maxHp: 1000 }, { id: 'a-2', hp: 700, maxHp: 1000 }]);
  return { ...f, runId, store, proofs: () => proofs };
}

it('prepares two final maxima against one original state and commits them without intermediate exposure', async () => {
  const f = await fixture(), prepared = await f.store.prepareChanges('a', f.runId, commands);
  expect((await f.store.read('a', f.runId)).revision).toBe(1);
  expect(prepared.receipts.map(x => x.revision)).toEqual([2, 3]);
  await f.env.DB.batch(prepared.statements);
  const state = await f.store.read('a', f.runId);
  expect(state.revision).toBe(3);
  expect(state.state.targets.map(t => t.health.hp)).toEqual([666, 350]);
  expect(state.state.targets.map(t => t.health.anchorHp)).toEqual([333, 700]);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect((await f.store.prepareChanges('a', f.runId, commands)).statements).toEqual([]);
  expect(await prepared.replay()).toEqual(prepared.receipts);
  expect(f.proofs()).toBe(3);
});

it('rejects mixed original and new events before preparing another effect', async () => {
  const f = await fixture();
  await f.store.commit('a', f.runId, commands[0]);
  const proofs = f.proofs();
  await expect(f.store.prepareChanges('a', f.runId, commands)).rejects.toThrow('combat_health_partial_replay');
  expect(f.proofs()).toBe(proofs);
  expect((await f.store.read('a', f.runId)).state.targets[1].health.hp).toBe(700);
});

it('rolls back both maxima and receipts when the second receipt cannot be saved', async () => {
  const f = await fixture(), before = await f.store.read('a', f.runId);
  const prepared = await f.store.prepareChanges('a', f.runId, commands);
  f.sql.run("CREATE TRIGGER group_fail BEFORE INSERT ON combat_damage_receipts WHEN NEW.event_id='swap-b' BEGIN SELECT RAISE(ABORT,'group-failed'); END");
  await expect(f.env.DB.batch(prepared.statements)).rejects.toThrow('group-failed');
  expect(await f.store.read('a', f.runId)).toEqual(before);
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
});

it.each([[], [commands[0], commands[0]], [commands[0], { ...commands[1], expectedRevision: 4 }]].map(raw => ({ raw })))('rejects malformed group $raw', async ({ raw }) => {
  const f = await fixture();
  await expect(f.store.prepareChanges('a', f.runId, raw)).rejects.toThrow('combat_health_group_invalid');
  expect(f.proofs()).toBe(1);
});

it('applies sequential actual HP changes to a candidate while retaining the anchor on zero change', async () => {
  const f = await fixture(), prepared = await f.store.prepareChanges('a', f.runId, [commands[0],
    { eventId: 'zero', expectedRevision: 2, targetId: 'a-1', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 666 } },
    { eventId: 'back', expectedRevision: 3, targetId: 'a-1', change: { kind: 'MAX_HP_REBASE', maxHp: 1000 } }]);
  await f.env.DB.batch(prepared.statements);
  expect((await f.store.read('a', f.runId)).state.targets[0].health).toMatchObject({ hp: 333, anchorHp: 333, anchorMaxHp: 1000 });
});
