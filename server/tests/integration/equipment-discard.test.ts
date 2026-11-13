import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentDiscardStore } from '../../apps/game-api/src/equipment-discard';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { equipmentInventoryStartStatements } from '../../apps/game-api/src/equipment-inventory-start';
import { EquipmentRewardBundleStore } from '../../apps/game-api/src/equipment-reward-bundles';
import { EquipmentRewardEffectSource } from '../../apps/game-api/src/equipment-reward-effects';

const command = { commandId: 'discard-1', instanceId: 'gear-1', expectedInventoryRevision: 1 };
async function fixture(slots = 30) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const runId = (await (await f.start(quote.operationPreviewId, 'discard-start')).json()).runId as string;
  if (slots === 54) {
    // Synthetic pinned START capacity for this boundary test, not a live DO05 investment.
    f.sql.run('DELETE FROM run_equipment_inventory');
    const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
    snapshot.equipmentInventoryCapacity = { version: 'equipment.inventory-capacity.v18', do05Rank: 4, slots: 54 };
    const json = JSON.stringify(snapshot);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [json]);
    await f.env.DB.batch(equipmentInventoryStartStatements(f.env.DB, 'a', runId, json));
  }
  await f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
    definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-1',?,'source-1','gear-a','fixture-v1','ATK','SET01',1,0,'STORED',1)`).bind(runId).run();
  // Synthetic safety state; this does not authorize a production maintenance adapter.
  f.sql.run('CREATE TABLE fixture_safe(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO fixture_safe VALUES(1)');
  let proofs = 0;
  const verifier = { prepare: async () => { proofs++; return { assertions: [f.env.DB.prepare(
    'INSERT INTO equipment_inventory_write_guard(valid) SELECT valid FROM fixture_safe')] }; } };
  return { ...f, runId, verifier, proofs: () => proofs, store: new EquipmentDiscardStore(f.env.DB, verifier) };
}

it('deletes exactly one stored item and replays the original result after termination', async () => {
  const f = await fixture();
  const receipt = await f.store.discard('a', f.runId, command);
  expect(receipt).toEqual({ version: 'equipment.discard-receipt.v1', commandId: 'discard-1',
    instanceId: 'gear-1', inventoryRevision: 2, usedSlots: 0, capacitySlots: 30 });
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.discard('a', f.runId, command)).toEqual(receipt);
  expect(f.proofs()).toBe(1);
  await expect(f.store.discard('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
  await expect(f.store.discard('a', f.runId, { ...command, instanceId: 'other' })).rejects.toThrow('equipment_request_conflict');
});

it('requires safety proof and rejects consumed items, stale revisions, locks and equipped items', async () => {
  const f = await fixture();
  await expect(new EquipmentDiscardStore(f.env.DB).discard('a', f.runId, command)).rejects.toThrow('equipment_maintenance_verification_required');
  await expect(f.store.discard('a', f.runId, { ...command, expectedInventoryRevision: 2 })).rejects.toThrow('equipment_revision_conflict');
  f.sql.run('UPDATE run_equipment_instances SET locked=1,revision=2');
  await expect(f.store.discard('a', f.runId, command)).rejects.toThrow('equipment_item_locked');
  f.sql.run("UPDATE run_equipment_instances SET locked=0,revision=3,location_kind='EQUIPPED',wearer_kind='operator',wearer_id='operator-1',slot_index=0");
  await expect(f.store.discard('a', f.runId, command)).rejects.toThrow('equipment_discard_stored_required');
  f.sql.run("UPDATE run_equipment_instances SET revision=4,location_kind='STORED',wearer_kind=NULL,wearer_id=NULL,slot_index=NULL");
  await f.store.discard('a', f.runId, command);
  await expect(f.store.discard('a', f.runId, { ...command, commandId: 'new', expectedInventoryRevision: 2 })).rejects.toThrow('equipment_instance_not_found');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
});

it.each(['safe', 'lock', 'placement', 'inventory', 'snapshot', 'account', 'run'])('fences %s changes before commit', async kind => {
  const f = await fixture();
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    const changes: Record<string, string> = {
      safe: 'UPDATE fixture_safe SET valid=0', lock: 'UPDATE run_equipment_instances SET locked=1,revision=2',
      placement: "UPDATE run_equipment_instances SET revision=2,location_kind='EQUIPPED',wearer_kind='operator',wearer_id='operator-1',slot_index=0",
      inventory: 'UPDATE run_equipment_inventory SET revision=2',
      snapshot: "UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureChanged',1)",
      account: "UPDATE accounts SET status='DELETED'", run: "UPDATE runs SET status='COMPLETED'",
    };
    f.sql.run(changes[kind]);
  };
  await expect(f.store.discard('a', f.runId, command)).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it.each([30, 54])('frees one slot in a full %i-slot inventory and retains immutable effect originals and history', async slots => {
  const f = await fixture(slots);
  const original = { version: 'equipment.effect-original.v1', equipment: {
    definitionId: 'gear-a', definitionVersion: 'fixture-v1', star: 1 }, uniqueEffects: [], enchantment: null };
  const originals = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({ snapshot: original,
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] }) });
  const prepared = await originals.prepare('a', f.runId, { instanceId: 'gear-1', sourceItemId: 'source-1', expectedInventoryRevision: 1 });
  await f.env.DB.batch(prepared.statements);
  for (let i = 2; i <= slots; i++) {
    await f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
      definition_version,category_id,set_tag,star,locked,location_kind,revision)
      VALUES(?, ?, ?, 'gear-a','fixture-v1','ATK','SET01',1,0,'STORED',1)`)
      .bind(`gear-${i}`, f.runId, `source-${i}`).run();
  }
  expect(await f.store.discard('a', f.runId, command)).toMatchObject({ usedSlots: slots - 1, capacitySlots: slots });
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(slots - 1);
  expect(await originals.read('a', f.runId, 'gear-1')).toEqual(original);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_heads')).toBe(1);
});

it.each([{ instanceId: 'other' }, { commandId: 'other' }, { usedSlots: 31 }, { capacitySlots: 31 },
  { capacitySlots: 36 }, { inventoryRevision: 3 }])('rejects a damaged original receipt %j', async damage => {
  const f = await fixture(), receipt = await f.store.discard('a', f.runId, command);
  f.sql.run('DROP TRIGGER run_equipment_receipt_immutable_update'); // Corruption injection only.
  f.sql.run('UPDATE run_equipment_receipts SET response_json=?', [JSON.stringify({ ...receipt, ...damage })]);
  await expect(f.store.discard('a', f.runId, command)).rejects.toThrow('equipment_receipt_invalid');
  expect(f.proofs()).toBe(1);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
});

it('preserves pending reward originals and requires an explicit claim after freeing a slot', async () => {
  const f = await fixture();
  for (let i = 2; i <= 30; i++) f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,
    definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES(?, ?, ?, 'gear-a','fixture-v1','ATK','SET01',1,0,'STORED',1)`, [`gear-${i}`, f.runId, `source-${i}`]);
  let publications = 0;
  const rewards = new EquipmentRewardBundleStore(f.env.DB, { prepare: async () => {
    publications++;
    return { sourceVersion: 'fixture-v1', items: [{ itemId: 'pending-1', instanceId: 'incoming-1',
      definitionId: 'gear-a', definitionVersion: 'fixture-v1', categoryId: 'ATK', setTag: 'SET01', star: 1 }],
    assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] };
  } }, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO equipment_reward_write_guard(valid) VALUES(1)')] }) },
  new EquipmentRewardEffectSource(f.env.DB));
  const bundle = await rewards.publishBase('a', f.runId, 'node-1', 'original-reward');
  const claim = { commandId: 'claim-after-discard', bundleId: bundle.bundleId, itemIds: ['pending-1'], expectedInventoryRevision: 1 };
  await expect(rewards.claimItems('a', f.runId, claim)).rejects.toThrow('equipment_capacity_exceeded');
  await expect(f.store.discard('a', f.runId, { ...command, instanceId: 'incoming-1' })).rejects.toThrow('equipment_instance_not_found');
  const original = f.sql.exec('SELECT * FROM run_equipment_reward_effects');
  await f.store.discard('a', f.runId, command);
  expect(await rewards.read('a', f.runId, bundle.bundleId)).toEqual(bundle);
  expect(f.sql.exec('SELECT * FROM run_equipment_reward_effects')).toEqual(original);
  expect(f.scalar("SELECT count(*) FROM run_equipment_instances WHERE instance_id='incoming-1'")).toBe(0);
  expect(await rewards.publishBase('a', f.runId, 'node-1', 'original-reward')).toEqual(bundle);
  expect(publications).toBe(1);
  await expect(rewards.claimItems('a', f.runId, claim)).rejects.toThrow('equipment_revision_conflict');
  await rewards.claimItems('a', f.runId, { ...claim, expectedInventoryRevision: 2 });
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
  expect((await rewards.read('a', f.runId, bundle.bundleId)).items[0].status).toBe('CLAIMED');
  expect(await f.store.discard('a', f.runId, command)).toMatchObject({ inventoryRevision: 2, usedSlots: 29 });
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(3);
});

it('rolls deletion and revision back if the receipt cannot be saved', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fixture_receipt_fail BEFORE INSERT ON run_equipment_receipts BEGIN SELECT RAISE(ABORT,'fixture_receipt_failed'); END");
  await expect(f.store.discard('a', f.runId, command)).rejects.toThrow('fixture_receipt_failed');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
});
