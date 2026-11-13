import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentInventoryStore } from '../../apps/game-api/src/equipment-inventory-start';
import { EquipmentPlacementStore, type EquipmentPlacementVerifier } from '../../apps/game-api/src/equipment-placement';

const slot = (wearerId: string, slotIndex: number) => ({ kind: 'operator' as const, wearerId, slotIndex });
const proof = (db: D1Database): EquipmentPlacementVerifier => ({ prepare: async () => ({
  assertions: [db.prepare('INSERT INTO equipment_placement_write_guard(valid) VALUES(1)')],
}) });

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'placement-start')).json();
  const runId = result.runId as string;
  for (let n = 1; n <= 3; n++) {
    f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
      definition_version,category_id,set_tag,star,locked,location_kind,revision)
      VALUES(?,?,?,?,?,'ATK',?,1,0,'STORED',1)`, [`item-${n}`, runId, `source-${n}`, `gear-${n}`, 'catalog-v15', `SET0${n}`]);
  }
  return { ...f, runId };
}

function command(commandId: string, instanceId: string, revision: number,
  target: ReturnType<typeof slot> | null, expectedTargetInstanceId: string | null = null,
  expectedInstanceRevision = 1) {
  return { commandId, instanceId, expectedInventoryRevision: revision, expectedInstanceRevision,
    target, expectedTargetInstanceId };
}

it('requires safe placement proof, equips one real slot and replays its receipt', async () => {
  const f = await fixture(), raw = command('place-1', 'item-1', 1, slot('operator-1', 0));
  await expect(new EquipmentPlacementStore(f.env.DB).move('a', f.runId, raw))
    .rejects.toThrow('equipment_placement_verification_required');
  const store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  const receipt = await store.move('a', f.runId, raw);
  expect(receipt).toMatchObject({ version: 'equipment.placement.v1', inventoryRevision: 2,
    instanceRevision: 2, displacedInstanceId: null });
  expect((await new EquipmentInventoryStore(f.env.DB).readInventory('a', f.runId))?.usedSlots).toBe(2);
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('operator-1');
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.move('a', f.runId, raw)).toEqual(receipt);
  await expect(store.move('a', f.runId, { ...raw, target: slot('operator-2', 0) }))
    .rejects.toThrow('equipment_request_conflict');
  await expect(store.move('b', f.runId, raw)).rejects.toThrow('equipment_run_not_found');
});

it('swaps a stored item into an occupied slot even at full capacity, preserving lock and set ownership', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  f.sql.run("UPDATE run_equipment_instances SET locked=1,revision=2 WHERE instance_id='item-1'");
  await store.move('a', f.runId, command('place-1', 'item-1', 1, slot('operator-1', 0), null, 2));
  for (let n = 0; n < 28; n++) {
    f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
      definition_version,category_id,set_tag,star,locked,location_kind,revision)
      VALUES(?,?,?,?,?,'ATK','SET01',1,0,'STORED',1)`, [`extra-${n}`, f.runId, `extra-source-${n}`, 'gear-extra', 'catalog-v15']);
  }
  expect((await new EquipmentInventoryStore(f.env.DB).readInventory('a', f.runId))?.usedSlots).toBe(30);
  const receipt = await store.move('a', f.runId, command('place-2', 'item-2', 2, slot('operator-1', 0), 'item-1'));
  expect(receipt).toMatchObject({ inventoryRevision: 3, displacedInstanceId: 'item-1' });
  const inventory = await new EquipmentInventoryStore(f.env.DB).readInventory('a', f.runId);
  expect(inventory?.usedSlots).toBe(30);
  expect(inventory?.items.find(i => i.instance_id === 'item-1')).toMatchObject({ location_kind: 'STORED', locked: 1 });
  expect(inventory?.items.find(i => i.instance_id === 'item-2')).toMatchObject({ location_kind: 'EQUIPPED', wearer_id: 'operator-1' });
});

it('rejects full-capacity unequip and unexpected replacement before any mutation', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  await store.move('a', f.runId, command('place-1', 'item-1', 1, slot('operator-1', 0)));
  for (let n = 0; n < 28; n++) {
    f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
      definition_version,category_id,set_tag,star,locked,location_kind,revision)
      VALUES(?,?,?,?,?,'ATK','SET01',1,0,'STORED',1)`, [`extra-${n}`, f.runId, `extra-source-${n}`, 'gear-extra', 'catalog-v15']);
  }
  await expect(store.move('a', f.runId, command('place-2', 'item-1', 2, null, null, 2)))
    .rejects.toThrow('equipment_capacity_exceeded');
  await expect(store.move('a', f.runId, command('place-3', 'item-2', 2, slot('operator-1', 0))))
    .rejects.toThrow('equipment_target_changed');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('EQUIPPED');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
});

it('swaps two occupied slots and rolls back on proof race or receipt failure', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  await store.move('a', f.runId, command('place-1', 'item-1', 1, slot('operator-1', 0)));
  await store.move('a', f.runId, command('place-2', 'item-2', 2, slot('operator-2', 1)));
  const swapped = await store.move('a', f.runId, command('place-3', 'item-1', 3, slot('operator-2', 1), 'item-2', 2));
  expect(swapped).toMatchObject({ inventoryRevision: 4, instanceRevision: 4, displacedInstanceId: 'item-2' });
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('operator-2');
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='item-2'")).toBe('operator-1');
  f.sql.run('CREATE TABLE placement_test(safe INTEGER NOT NULL)');
  f.sql.run('INSERT INTO placement_test VALUES(1)');
  const guarded = new EquipmentPlacementStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_placement_write_guard(valid) SELECT safe FROM placement_test')],
  }) });
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE placement_test SET safe=0'); };
  await expect(guarded.move('a', f.runId, command('place-4', 'item-3', 4, slot('operator-1', 2))))
    .rejects.toThrow('equipment_placement_state_conflict');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='item-3'")).toBe('STORED');
  f.sql.run('UPDATE placement_test SET safe=1');
  f.sql.run("CREATE TRIGGER fail_place_receipt BEFORE INSERT ON run_equipment_receipts BEGIN SELECT RAISE(ABORT,'injected-placement'); END");
  await expect(guarded.move('a', f.runId, command('place-4', 'item-3', 4, slot('operator-1', 2))))
    .rejects.toThrow('injected-placement');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='item-3'")).toBe('STORED');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(4);
});

it('moves between empty slots and unequips only when the final storage count fits', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  await store.move('a', f.runId, command('place-1', 'item-1', 1, slot('operator-1', 0)));
  const moved = await store.move('a', f.runId, command('place-2', 'item-1', 2, slot('operator-2', 1), null, 2));
  expect(moved).toMatchObject({ inventoryRevision: 3, instanceRevision: 4 });
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('operator-2');
  const removed = await store.move('a', f.runId, command('place-3', 'item-1', 3, null, null, 4));
  expect(removed).toMatchObject({ inventoryRevision: 4, instanceRevision: 5 });
  expect((await new EquipmentInventoryStore(f.env.DB).readInventory('a', f.runId))?.usedSlots).toBe(3);
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='item-1'")).toBeNull();
});

it('does not overwrite a slot that changes between preview and atomic write', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run(`UPDATE run_equipment_instances SET location_kind='EQUIPPED',wearer_kind='operator',
      wearer_id='operator-1',slot_index=0,revision=2 WHERE instance_id='item-2'`); };
  await expect(store.move('a', f.runId, command('place-1', 'item-1', 1, slot('operator-1', 0))))
    .rejects.toThrow('equipment_placement_state_conflict');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('STORED');
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});
it.each(['start', 'occupancy'])('rejects %s changes between eligibility preparation and placement commit', async changed => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    if (changed === 'start') {
      f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureEligibilityVersion','changed')");
    } else {
      // Storage corruption injection: a real issuance must also increment inventory revision.
      f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
        definition_version,category_id,set_tag,star,locked,location_kind,revision)
        VALUES('race-item',?,'race-source','race-gear','fixture-v1','ATK','SET01',1,0,'STORED',1)`, [f.runId]);
    }
  };
  await expect(store.move('a', f.runId, command('guarded-place', 'item-1', 1, slot('operator-1', 0))))
    .rejects.toThrow('equipment_placement_state_conflict');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='item-1'")).toBe('STORED');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('rejects changed START during a two-wearer swap without exposing the temporary vacant source slot', async () => {
  const f = await fixture(), store = new EquipmentPlacementStore(f.env.DB, proof(f.env.DB));
  await store.move('a', f.runId, command('equip-a', 'item-1', 1, slot('operator-1', 0)));
  await store.move('a', f.runId, command('equip-b', 'item-2', 2, slot('operator-2', 1)));
  const before = f.sql.exec('SELECT * FROM run_equipment_instances ORDER BY instance_id');
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureEligibilityVersion','changed')");
  };
  await expect(store.move('a', f.runId, command('swap-race', 'item-1', 3, slot('operator-2', 1), 'item-2', 2)))
    .rejects.toThrow('equipment_placement_state_conflict');
  expect(f.sql.exec('SELECT * FROM run_equipment_instances ORDER BY instance_id')).toEqual(before);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(3);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(2);
});
