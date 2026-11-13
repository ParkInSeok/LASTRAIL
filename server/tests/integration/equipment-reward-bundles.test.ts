import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentRewardBundleStore, type EquipmentRewardSourceVerifier } from '../../apps/game-api/src/equipment-reward-bundles';

const sourceRef = 'reward-node-1';
const item = { itemId: 'reward-item-1', instanceId: 'reserved-instance-1', definitionId: 'gear-a',
  definitionVersion: 'fixture-v1', categoryId: 'weapon', setTag: 'SET01', star: 1 };

async function fixture(verifier?: EquipmentRewardSourceVerifier) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'bundle-start')).json();
  return { ...f, runId: result.runId as string, store: new EquipmentRewardBundleStore(f.env.DB, verifier) };
}

function syntheticSource(db: D1Database): EquipmentRewardSourceVerifier {
  return { prepare: async () => ({ sourceVersion: 'fixture-v1', items: [item],
    assertions: [db.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) };
}

it('fails closed without an original source and pins pending identities once', async () => {
  const f = await fixture();
  await expect(f.store.publishBase('a', f.runId, 'node-1', sourceRef)).rejects.toThrow('equipment_reward_source_required');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_bundles')).toBe(0);
  const store = new EquipmentRewardBundleStore(f.env.DB, syntheticSource(f.env.DB));
  const first = await store.publishBase('a', f.runId, 'node-1', sourceRef);
  expect(first).toMatchObject({ version: 'equipment.reward-bundle.v1', nodeId: 'node-1', sourceRef,
    items: [{ itemId: 'reward-item-1', instanceId: 'reserved-instance-1', status: 'PENDING' }] });
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_items')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.publishBase('a', f.runId, 'node-1', sourceRef)).toEqual(first);
  await expect(store.publishBase('a', f.runId, 'node-1', 'different-source')).rejects.toThrow('equipment_reward_source_conflict');
  await expect(store.read('b', f.runId, first.bundleId)).rejects.toThrow('equipment_run_not_found');
});

it('rolls back the bundle and all items when source proof changes or item insertion fails', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE reward_source_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO reward_source_test VALUES(1)');
  const verifier: EquipmentRewardSourceVerifier = { prepare: async () => ({ sourceVersion: 'fixture-v1', items: [item],
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) SELECT valid FROM reward_source_test')] }) };
  const store = new EquipmentRewardBundleStore(f.env.DB, verifier);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE reward_source_test SET valid=0'); };
  await expect(store.publishBase('a', f.runId, 'node-1', sourceRef)).rejects.toThrow('equipment_reward_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_bundles')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_items')).toBe(0);
  f.sql.run('UPDATE reward_source_test SET valid=1');
  f.sql.run("CREATE TRIGGER fail_reward_item BEFORE INSERT ON run_equipment_reward_items BEGIN SELECT RAISE(ABORT,'injected-item'); END");
  await expect(store.publishBase('a', f.runId, 'node-1', sourceRef)).rejects.toThrow('injected-item');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_bundles')).toBe(0);
});

it('rejects duplicate original identities, foreign owners and terminal new publication', async () => {
  const f = await fixture({ prepare: async () => ({ sourceVersion: 'fixture-v1', items: [item, { ...item, itemId: 'second' }],
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) });
  await expect(f.store.publishBase('a', f.runId, 'node-1', sourceRef)).rejects.toThrow('equipment_reward_items_invalid');
  await expect(f.store.publishBase('b', f.runId, 'node-1', sourceRef)).rejects.toThrow('equipment_run_not_found');
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  await expect(f.store.publishBase('a', f.runId, 'node-1', sourceRef)).rejects.toThrow('equipment_run_not_active');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_bundles')).toBe(0);
});
