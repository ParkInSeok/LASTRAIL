import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentRewardBundleStore } from '../../apps/game-api/src/equipment-reward-bundles';
import { EquipmentInventoryStore } from '../../apps/game-api/src/equipment-inventory-start';
import { fixtureEquipmentEffectSource } from '../helpers/equipment-effect-source-fixture';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';
import { equipmentEffectOriginalSchema } from '../../apps/game-api/src/equipment-effect-snapshot';

const rewardItems = Array.from({ length: 3 }, (_, n) => ({ itemId: `reward-${n}`,
  instanceId: `instance-${n}`, definitionId: `gear-${n}`, definitionVersion: 'fixture-v1',
  categoryId: 'weapon', setTag: 'SET01' as const, star: 1 }));

async function fixture(items = rewardItems) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const started = await (await f.start(quote.operationPreviewId, 'claim-start')).json();
  const runId = started.runId as string;
  const store = new EquipmentRewardBundleStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'fixture-v1', items,
    effects: items.filter(item => item.star >= 3).map(item => ({ itemId: item.itemId, snapshot: {
      version: 'equipment.effect-original.v1', equipment: { definitionId: item.definitionId, definitionVersion: item.definitionVersion, star: item.star },
      uniqueEffects: [{ effectId: 'EQ01', grade: 2 ** (item.star - 3), definitionVersion: 'fixture-effect-v1',
        definitionSnapshot: { fixtureOnly: true, coefficient: 123 } }], enchantment: null } })),
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')],
  }) }, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')],
  }) }, fixtureEquipmentEffectSource(f.env.DB));
  const bundle = await store.publishBase('a', runId, 'node-1', 'node-original');
  const claim = (commandId: string, itemIds: string[], expectedInventoryRevision = 1) =>
    store.claimItems('a', runId, { commandId, bundleId: bundle.bundleId, itemIds, expectedInventoryRevision });
  return { ...f, runId, store, bundle, claim };
}

function stored(f: Awaited<ReturnType<typeof fixture>>, count: number) {
  for (let n = 0; n < count; n++) f.sql.run(`INSERT INTO run_equipment_instances
    (instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES(?,? ,?,'old','fixture-v1','weapon','SET01',1,0,'STORED',1)`, [`old-${n}`, f.runId, `old-item-${n}`]);
}

it('requires effect provenance for every newly claimed item', async () => {
  const f = await fixture();
  const store = new EquipmentRewardBundleStore(f.env.DB, undefined, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) });
  await expect(store.claimItems('a', f.runId, { commandId: 'missing-effects', bundleId: f.bundle.bundleId,
    itemIds: ['reward-0'], expectedInventoryRevision: 1 })).rejects.toThrow('equipment_effect_source_required');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('initializes effects for a selected group and replays without resolving definitions again', async () => {
  const f = await fixture();
  await f.claim('effects-claim', ['reward-0', 'reward-1']);
  for (const instanceId of ['instance-0', 'instance-1']) {
    expect(await new EquipmentEffectStateStore(f.env.DB).read('a', f.runId, instanceId)).toMatchObject({
      effectRevision: 1, inventoryRevision: 2, snapshot: { uniqueEffects: [], enchantment: null } });
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(2);
  await f.claim('effects-claim', ['reward-1', 'reward-0']);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(2);
});

it('rolls back the whole selected group when a later effect head fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_claim_head BEFORE INSERT ON run_equipment_effect_heads WHEN NEW.instance_id='instance-1' BEGIN SELECT RAISE(ABORT,'injected-claim-head'); END");
  await expect(f.claim('failed-effects', ['reward-0', 'reward-1'])).rejects.toThrow('injected-claim-head');
  for (const table of ['run_equipment_instances', 'run_equipment_effect_originals', 'run_equipment_effect_history', 'run_equipment_effect_heads', 'run_equipment_receipts']) {
    expect(f.scalar(`SELECT count(*) FROM ${table}`)).toBe(0);
  }
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='PENDING'")).toBe(3);
});

it('pins a resolved grade-I effect when claiming a three-star original item', async () => {
  const f = await fixture(rewardItems.map(item => ({ ...item, star: 3 })));
  await f.claim('three-star', ['reward-0']);
  expect(await new EquipmentEffectStateStore(f.env.DB).read('a', f.runId, 'instance-0')).toMatchObject({
    snapshot: { equipment: { star: 3 }, uniqueEffects: [{ effectId: 'EQ01', grade: 1,
      definitionVersion: 'fixture-effect-v1', definitionSnapshot: { fixtureOnly: true, coefficient: 123 } }] } });
});

it('rejects a definition mismatch and rolls back all selected items', async () => {
  const f = await fixture(), source = fixtureEquipmentEffectSource(f.env.DB);
  const store = new EquipmentRewardBundleStore(f.env.DB, undefined, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) }, { prepare: async context => {
      const proof = await source.prepare(context), snapshot = equipmentEffectOriginalSchema.parse(proof.snapshot);
      return { ...proof, snapshot: context.sourceItemId === 'reward-1'
        ? { ...snapshot, equipment: { ...snapshot.equipment, definitionId: 'wrong-definition' } } : snapshot };
    } });
  await expect(store.claimItems('a', f.runId, { commandId: 'bad-definition', bundleId: f.bundle.bundleId,
    itemIds: ['reward-0', 'reward-1'], expectedInventoryRevision: 1 })).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(0);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
});

it('claims a selected set atomically, leaves the remainder pending, and replays after termination', async () => {
  const f = await fixture();
  stored(f, 29);
  const result = await f.claim('claim-1', ['reward-0']);
  expect(result).toMatchObject({ version: 'equipment.claim-receipt.v1', claimedItemIds: ['reward-0'], inventoryRevision: 2 });
  expect(f.scalar("SELECT count(*) FROM run_equipment_instances WHERE source_item_id='reward-0' AND location_kind='STORED'")).toBe(1);
  expect((await f.store.read('a', f.runId, f.bundle.bundleId)).items.map(x => x.status)).toEqual(['CLAIMED', 'PENDING', 'PENDING']);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.claim('claim-1', ['reward-0'])).toEqual(result);
  await expect(f.claim('claim-2', ['reward-1'], 2)).rejects.toThrow('equipment_run_not_active');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
});

it('rejects over-capacity and already-claimed sets without creating partial equipment', async () => {
  const f = await fixture();
  stored(f, 29);
  await expect(f.claim('too-many', ['reward-0', 'reward-1'])).rejects.toThrow('equipment_capacity_exceeded');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(29);
  await f.claim('first', ['reward-0']);
  await expect(f.claim('other', ['reward-0'], 2)).rejects.toThrow('equipment_reward_item_not_pending');
  await expect(f.claim('full', ['reward-1'], 2)).rejects.toThrow('equipment_capacity_exceeded');
  await expect(f.claim('first', ['reward-1'])).rejects.toThrow('equipment_request_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
});

it('normalizes item order, checks ownership, and rolls back on same-batch races', async () => {
  const f = await fixture();
  const result = await f.claim('two', ['reward-1', 'reward-0']);
  expect(result.claimedItemIds).toEqual(['reward-0', 'reward-1']);
  expect(await f.claim('two', ['reward-0', 'reward-1'])).toEqual(result);
  await expect(f.claim('two', ['reward-2'])).rejects.toThrow('equipment_request_conflict');
  await expect(f.store.claimItems('b', f.runId, { commandId: 'foreign', bundleId: f.bundle.bundleId,
    itemIds: ['reward-2'], expectedInventoryRevision: 2 })).rejects.toThrow('equipment_run_not_found');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE run_equipment_inventory SET revision=3'); };
  await expect(f.claim('race', ['reward-2'], 2)).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar("SELECT status FROM run_equipment_reward_items WHERE item_id='reward-2'")).toBe('PENDING');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(2);
});

it('rolls back all item rows, inventory revision and receipt when one physical insert fails', async () => {
  const f = await fixture();
  f.sql.run(`CREATE TRIGGER fail_second_claim BEFORE INSERT ON run_equipment_instances
    WHEN NEW.source_item_id='reward-1' BEGIN SELECT RAISE(ABORT,'claim-injected'); END`);
  await expect(f.claim('broken', ['reward-0', 'reward-1'])).rejects.toThrow('claim-injected');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='CLAIMED'")).toBe(0);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('requires live node proof and shares command keys with other equipment operations', async () => {
  const f = await fixture();
  const withoutProof = new EquipmentRewardBundleStore(f.env.DB);
  await expect(withoutProof.claimItems('a', f.runId, { commandId: 'no-proof', bundleId: f.bundle.bundleId,
    itemIds: ['reward-0'], expectedInventoryRevision: 1 })).rejects.toThrow('equipment_claim_verification_required');
  const claim = await f.claim('shared-key', ['reward-0']);
  expect(claim.inventoryRevision).toBe(2);
  await expect(new EquipmentInventoryStore(f.env.DB).setLock('a', f.runId, {
    commandId: 'shared-key', instanceId: 'instance-0', expectedInventoryRevision: 2,
    expectedInstanceRevision: 1, locked: true,
  })).rejects.toThrow('equipment_request_conflict');
  expect(f.scalar("SELECT locked FROM run_equipment_instances WHERE instance_id='instance-0'")).toBe(0);
});

it('rechecks final capacity and account activity inside the commit batch', async () => {
  const f = await fixture();
  stored(f, 29);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run(`INSERT INTO run_equipment_instances
    (instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('concurrent',?,'concurrent-source','gear','fixture-v1','weapon','SET01',1,0,'STORED',1)`, [f.runId]); };
  await expect(f.claim('capacity-race', ['reward-0'])).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
  expect(f.scalar("SELECT status FROM run_equipment_reward_items WHERE item_id='reward-0'")).toBe('PENDING');
  const g = await fixture();
  g.adapter.beforeBatch = () => { g.adapter.beforeBatch = undefined; g.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'"); };
  await expect(g.claim('stopped', ['reward-0'])).rejects.toThrow('equipment_state_conflict');
  expect(g.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('declines exactly the remaining pending items after a partial claim and replays the receipt', async () => {
  const f = await fixture();
  await f.claim('first', ['reward-0']);
  const command = { commandId: 'decline-1', bundleId: f.bundle.bundleId,
    itemIds: ['reward-2', 'reward-1'], expectedInventoryRevision: 2 };
  const receipt = await f.store.declineRemaining('a', f.runId, command);
  expect(receipt).toMatchObject({ version: 'equipment.decline-receipt.v1',
    declinedItemIds: ['reward-1', 'reward-2'], inventoryRevision: 3 });
  expect((await f.store.read('a', f.runId, f.bundle.bundleId)).items.map(x => x.status))
    .toEqual(['CLAIMED', 'FORFEITED', 'FORFEITED']);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.declineRemaining('a', f.runId, { ...command, itemIds: ['reward-1', 'reward-2'] })).toEqual(receipt);
  await expect(f.store.declineRemaining('a', f.runId, { ...command, commandId: 'new' }))
    .rejects.toThrow('equipment_run_not_active');
});

it('requires an exact visible remaining set and never silently expands it', async () => {
  const f = await fixture();
  const decline = (commandId: string, itemIds: string[]) => f.store.declineRemaining('a', f.runId, {
    commandId, bundleId: f.bundle.bundleId, itemIds, expectedInventoryRevision: 1,
  });
  await expect(decline('subset', ['reward-1'])).rejects.toThrow('equipment_reward_remaining_changed');
  await expect(decline('foreign-item', ['reward-0', 'reward-1', 'other'])).rejects.toThrow('equipment_reward_remaining_changed');
  await expect(decline('duplicate', ['reward-0', 'reward-0'])).rejects.toThrow('equipment_decline_command_invalid');
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(0);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
});

it('fails closed without live node proof and shares the equipment command key', async () => {
  const f = await fixture();
  const command = { commandId: 'shared', bundleId: f.bundle.bundleId,
    itemIds: ['reward-0', 'reward-1', 'reward-2'], expectedInventoryRevision: 1 };
  await expect(new EquipmentRewardBundleStore(f.env.DB).declineRemaining('a', f.runId, command))
    .rejects.toThrow('equipment_resolution_verification_required');
  await expect(f.store.declineRemaining('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
  await f.claim('shared', ['reward-0']);
  await expect(f.store.declineRemaining('a', f.runId, command)).rejects.toThrow('equipment_request_conflict');
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(0);
});

it('rolls back forfeiture when the commit races or its receipt fails', async () => {
  const f = await fixture();
  const command = { commandId: 'race', bundleId: f.bundle.bundleId,
    itemIds: ['reward-0', 'reward-1', 'reward-2'], expectedInventoryRevision: 1 };
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE run_equipment_inventory SET revision=2'); };
  await expect(f.store.declineRemaining('a', f.runId, command)).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(0);
  f.sql.run("CREATE TRIGGER fail_decline_receipt BEFORE INSERT ON run_equipment_receipts BEGIN SELECT RAISE(ABORT,'decline-injected'); END");
  await expect(f.store.declineRemaining('a', f.runId, { ...command, expectedInventoryRevision: 2 }))
    .rejects.toThrow('decline-injected');
  expect(f.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(0);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
  const g = await fixture();
  g.adapter.beforeBatch = () => { g.adapter.beforeBatch = undefined;
    g.sql.run("UPDATE run_equipment_reward_items SET status='FORFEITED' WHERE item_id='reward-2'"); };
  await expect(g.store.declineRemaining('a', g.runId, { ...command, bundleId: g.bundle.bundleId }))
    .rejects.toThrow('equipment_state_conflict');
  expect(g.scalar("SELECT count(*) FROM run_equipment_reward_items WHERE status='FORFEITED'")).toBe(1);
  expect(g.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});
