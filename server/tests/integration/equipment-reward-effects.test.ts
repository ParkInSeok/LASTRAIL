import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentRewardBundleStore } from '../../apps/game-api/src/equipment-reward-bundles';
import { EquipmentRewardEffectSource, captureRewardEffects } from '../../apps/game-api/src/equipment-reward-effects';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';

const items = [0, 1].map(index => ({ itemId: 'reward-' + index, instanceId: 'gear-' + index,
  definitionId: 'equipment-a', definitionVersion: 'fixture-catalog-v1', categoryId: 'ATK', setTag: 'SET01', star: 3 }));
const snapshot = { version: 'equipment.effect-original.v1', equipment: { definitionId: 'equipment-a', definitionVersion: 'fixture-catalog-v1', star: 3 },
  uniqueEffects: [{ effectId: 'EQ02', grade: 1, definitionVersion: 'fixture-effect-v1', definitionSnapshot: { fixtureOnly: true, coefficient: 123 } }],
  enchantment: null };
async function fixture(v2=false) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const started = await (await f.start(quote.operationPreviewId, 'reward-effects-start')).json(), runId = started.runId as string;
  let calls = 0;
  const store = new EquipmentRewardBundleStore(f.env.DB, { prepare: async () => {
    calls++;
    return { sourceVersion: 'fixture-source-v1', items:v2?items.map(i=>({...i,star:1})):items, effects: items.map(item => ({ itemId: item.itemId, snapshot:v2?{version:'equipment.effect-original.v2.20261015',equipment:{...snapshot.equipment,star:1},uniqueEffects:[],enchantment:{effectId:'EN17',appliedStar:1,definitionVersion:'equipment.multishot-catalog.v1.20261015',definitionSnapshot:{version:'multishot-enchant-v1.20261015',appliedStar:1,coefficientBps:1500,maxSecondaryTargets:2,radiusL:{numerator:1,denominator:2},attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'}}}:snapshot })),
      assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] };
  } }, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) },
  new EquipmentRewardEffectSource(f.env.DB));
  return { ...f, runId, store, calls: () => calls };
}

it('pins reward effects before physical issuance and keeps partial claim and replay results fixed', async () => {
  const f = await fixture(), bundle = await f.store.publishBase('a', f.runId, 'node-1', 'original-reward');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_effects')).toBe(2);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  const command = { commandId: 'claim-1', bundleId: bundle.bundleId, itemIds: ['reward-0'], expectedInventoryRevision: 1 };
  const receipt = await f.store.claimItems('a', f.runId, command);
  expect((await new EquipmentEffectStateStore(f.env.DB).read('a', f.runId, 'gear-0'))!.snapshot.uniqueEffects).toEqual(snapshot.uniqueEffects);
  await f.store.claimItems('a', f.runId, { ...command, commandId: 'claim-2', itemIds: ['reward-1'], expectedInventoryRevision: 2 });
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.claimItems('a', f.runId, command)).toEqual(receipt);
  expect(await f.store.publishBase('a', f.runId, 'node-1', 'original-reward')).toMatchObject({ items: [
    { itemId: 'reward-0', status: 'CLAIMED' }, { itemId: 'reward-1', status: 'CLAIMED' }] });
  expect(f.calls()).toBe(1);
});

it('requires high-tier fixed originals and rejects mismatched, duplicated or unknown item IDs', () => {
  expect(() => captureRewardEffects(items, undefined)).toThrow('equipment_reward_effect_required');
  expect(captureRewardEffects(items, items.map(item => ({ itemId: item.itemId, snapshot })))).toHaveLength(2);
  for (const invalid of [[{ itemId: 'unknown', snapshot }], [{ itemId: 'reward-0', snapshot }, { itemId: 'reward-0', snapshot }],
    [{ itemId: 'reward-0', snapshot: { ...snapshot, equipment: { ...snapshot.equipment, definitionId: 'other' } } }]]) {
    expect(() => captureRewardEffects(items, invalid)).toThrow('equipment_reward_effect_invalid');
  }
  expect(captureRewardEffects(items.map(item => ({ ...item, star: 1 })), undefined).every(item => item.snapshot.uniqueEffects.length === 0)).toBe(true);
});

it('rolls back original bundle and all pending items if a later effect insert fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_reward_fx BEFORE INSERT ON run_equipment_reward_effects WHEN NEW.item_id='reward-1' BEGIN SELECT RAISE(ABORT,'injected-reward-effects'); END");
  await expect(f.store.publishBase('a', f.runId, 'node-1', 'original-reward')).rejects.toThrow('injected-reward-effects');
  for (const table of ['run_equipment_reward_bundles', 'run_equipment_reward_items', 'run_equipment_reward_effects']) {
    expect(f.scalar(`SELECT count(*) FROM ${table}`)).toBe(0);
  }
});

it('rejects source owner or reserved-instance mismatch and forbids changes to fixed originals', async () => {
  const f = await fixture();
  await f.store.publishBase('a', f.runId, 'node-1', 'original-reward');
  const source = new EquipmentRewardEffectSource(f.env.DB);
  const context = { accountId: 'a', runId: f.runId, instanceId: 'gear-0', sourceItemId: 'reward-0', expectedInventoryRevision: 2 };
  await expect(source.prepare({ ...context, accountId: 'b' })).rejects.toThrow('equipment_run_not_found');
  await expect(source.prepare({ ...context, instanceId: 'wrong' })).rejects.toThrow('equipment_reward_effect_instance_conflict');
  expect(() => f.sql.run('DELETE FROM run_equipment_reward_effects')).toThrow('equipment_reward_effect_immutable');
});

it('never recreates missing original effects on old bundle replay', async () => {
  const f = await fixture(), bundle = await f.store.publishBase('a', f.runId, 'node-1', 'original-reward');
  f.sql.run('DROP TRIGGER equipment_reward_effect_no_delete');
  f.sql.run('DELETE FROM run_equipment_reward_effects');
  expect(await f.store.publishBase('a', f.runId, 'node-1', 'original-reward')).toEqual(bundle);
  await expect(f.store.claimItems('a', f.runId, { commandId: 'missing', bundleId: bundle.bundleId,
    itemIds: ['reward-0'], expectedInventoryRevision: 1 })).rejects.toThrow('equipment_reward_effect_not_found');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('rejects corrupted records and fences fixed-original changes in the claim batch', async () => {
  const f = await fixture(), bundle = await f.store.publishBase('a', f.runId, 'node-1', 'original-reward');
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    f.sql.run('DROP TRIGGER equipment_reward_effect_no_update');
    f.sql.run("UPDATE run_equipment_reward_effects SET payload_hash=? WHERE item_id='reward-0'", ['0'.repeat(64)]);
  };
  await expect(f.store.claimItems('a', f.runId, { commandId: 'race', bundleId: bundle.bundleId,
    itemIds: ['reward-0', 'reward-1'], expectedInventoryRevision: 1 })).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='PENDING'")).toBe(2);
  await expect(new EquipmentRewardEffectSource(f.env.DB).prepare({ accountId: 'a', runId: f.runId,
    instanceId: 'gear-0', sourceItemId: 'reward-0', expectedInventoryRevision: 2 })).rejects.toThrow('equipment_reward_effect_invalid');
});

it('keeps fixed originals while rolling back all selected items after current-head failure', async () => {
  const f = await fixture(), bundle = await f.store.publishBase('a', f.runId, 'node-1', 'original-reward');
  f.sql.run("CREATE TRIGGER fail_reward_head BEFORE INSERT ON run_equipment_effect_heads WHEN NEW.instance_id='gear-1' BEGIN SELECT RAISE(ABORT,'injected-fixed-head'); END");
  await expect(f.store.claimItems('a', f.runId, { commandId: 'failed-heads', bundleId: bundle.bundleId,
    itemIds: ['reward-0', 'reward-1'], expectedInventoryRevision: 1 })).rejects.toThrow('injected-fixed-head');
  expect(f.scalar('SELECT count(*) FROM run_equipment_reward_effects')).toBe(2);
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='PENDING'")).toBe(2);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  for (const table of ['run_equipment_instances', 'run_equipment_effect_originals', 'run_equipment_effect_history', 'run_equipment_effect_heads', 'run_equipment_receipts']) {
    expect(f.scalar(`SELECT count(*) FROM ${table}`)).toBe(0);
  }
});

it('publishes and claims fixed v2 EN17 rewards through the actual bundle writer and source reader',async()=>{
 const f=await fixture(true),bundle=await f.store.publishBase('a',f.runId,'node-1','en17-reward');
 const command={commandId:'en17-reward-claim',bundleId:bundle.bundleId,itemIds:['reward-0'],expectedInventoryRevision:1};
 const result=await f.store.claimItems('a',f.runId,command);expect((await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,'gear-0'))?.snapshot).toMatchObject({version:'equipment.effect-state.v2.20261015',enchantment:{effectId:'EN17',appliedStar:1}});
 expect(await f.store.claimItems('a',f.runId,command)).toEqual(result);
});
