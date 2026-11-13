import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentRewardBundleStore } from '../../apps/game-api/src/equipment-reward-bundles';
import { prepareEquipmentRunFinalization, readEquipmentRunFinalization } from '../../apps/game-api/src/equipment-run-finalization';
import { fixtureEquipmentEffectSource } from '../helpers/equipment-effect-source-fixture';

const payloadJson = '{"verified":"synthetic-final"}';
const items = Array.from({ length: 3 }, (_, n) => ({ itemId: `final-item-${n}`, instanceId: `final-instance-${n}`,
  definitionId: `gear-${n}`, definitionVersion: 'fixture-v1', categoryId: 'weapon', setTag: 'SET01' as const, star: 1 }));

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const started = await (await f.start(quote.operationPreviewId, 'final-start')).json();
  const runId = started.runId as string;
  const rewards = new EquipmentRewardBundleStore(f.env.DB, { prepare: async () => ({ sourceVersion: 'fixture-v1', items,
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) },
  { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) }, fixtureEquipmentEffectSource(f.env.DB));
  const bundle = await rewards.publishBase('a', runId, 'final-node', 'synthetic-original');
  return { ...f, runId, rewards, bundle };
}

async function finish(f: Awaited<ReturnType<typeof fixture>>, outcome: 'VICTORY' | 'FAILURE' | 'ABANDONED' = 'VICTORY') {
  const prepared = await prepareEquipmentRunFinalization(f.env.DB, 'a', f.runId,
    { outcome, payloadJson, submittedAt: 123 });
  const status = outcome === 'VICTORY' ? 'COMPLETED' : outcome === 'FAILURE' ? 'FAILED' : 'ABANDONED';
  await f.env.DB.batch([
    f.env.DB.prepare('INSERT INTO run_final_results VALUES(?,?,?,?,?)').bind(f.runId, outcome, 12, payloadJson, 123),
    f.env.DB.prepare('UPDATE runs SET status=? WHERE id=?').bind(status, f.runId),
    ...prepared.statements,
  ]);
  return prepared.receipt;
}

it('closes only pending items with the verified final result in one batch', async () => {
  const f = await fixture();
  await f.rewards.claimItems('a', f.runId, { commandId: 'claim-one', bundleId: f.bundle.bundleId,
    itemIds: ['final-item-0'], expectedInventoryRevision: 1 });
  const receipt = await finish(f);
  expect(receipt).toMatchObject({ version: 'equipment.run-finalization.v1', outcome: 'VICTORY',
    closedItemIds: ['final-item-1', 'final-item-2'], inventoryRevision: 3 });
  expect((await f.rewards.read('a', f.runId, f.bundle.bundleId)).items.map(x => x.status))
    .toEqual(['CLAIMED', 'RUN_CLOSED', 'RUN_CLOSED']);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
  expect(await readEquipmentRunFinalization(f.env.DB, 'a', f.runId)).toEqual(receipt);
  await expect(f.rewards.claimItems('a', f.runId, { commandId: 'late', bundleId: f.bundle.bundleId,
    itemIds: ['final-item-1'], expectedInventoryRevision: 3 })).rejects.toThrow('equipment_run_not_active');
});

it('rolls back the final result and all equipment cleanup when finalization storage fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_equipment_final BEFORE INSERT ON run_equipment_finalizations BEGIN SELECT RAISE(ABORT,'final-injected'); END");
  await expect(finish(f)).rejects.toThrow('final-injected');
  expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
  expect(f.scalar('SELECT status FROM runs')).toBe('ACTIVE');
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='PENDING'")).toBe(3);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
});

it('fences a changed inventory or final result before finalization commits', async () => {
  const f = await fixture();
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE run_equipment_inventory SET revision=2'); };
  await expect(finish(f)).rejects.toThrow('equipment_finalization_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_finalizations')).toBe(0);
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='PENDING'")).toBe(3);
  const g = await fixture();
  g.adapter.beforeBatch = () => { g.adapter.beforeBatch = undefined;
    g.sql.run("UPDATE run_equipment_reward_items SET status='FORFEITED' WHERE item_id='final-item-2'"); };
  await expect(finish(g)).rejects.toThrow('equipment_finalization_state_conflict');
  expect(g.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
  expect(g.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(1);
});

it('does not finalize without the exact terminal result or reinterpret legacy capacity', async () => {
  const f = await fixture();
  const prepared = await prepareEquipmentRunFinalization(f.env.DB, 'a', f.runId,
    { outcome: 'VICTORY', payloadJson, submittedAt: 123 });
  if (!prepared) throw new Error('new START missing capacity');
  await expect(f.env.DB.batch(prepared.statements)).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM run_equipment_finalizations')).toBe(0);
  await expect(f.env.DB.batch([
    f.env.DB.prepare('INSERT INTO run_final_results VALUES(?,?,?,?,?)').bind(f.runId, 'VICTORY', 12, '{"wrong":true}', 123),
    f.env.DB.prepare("UPDATE runs SET status='COMPLETED' WHERE id=?").bind(f.runId),
    ...prepared.statements,
  ])).rejects.toThrow('equipment_finalization_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
  const legacy = await setup(true), quote = await (await legacy.preview()).json();
  const started = await (await legacy.start(quote.operationPreviewId, 'legacy-fixture')).json();
  const legacyRunId = started.runId as string;
  const snapshot = JSON.parse(String(legacy.scalar('SELECT snapshot_json FROM run_snapshots')));
  delete snapshot.equipmentInventoryCapacity;
  legacy.sql.run('DELETE FROM run_equipment_inventory');
  legacy.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(snapshot), legacyRunId]);
  expect(await prepareEquipmentRunFinalization(legacy.env.DB, 'a', legacyRunId,
    { outcome: 'VICTORY', payloadJson, submittedAt: 123 })).toBeNull();
});
