import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentInventoryStore } from '../../apps/game-api/src/equipment-inventory-start';

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'items-start')).json();
  return { ...f, runId: result.runId as string, store: new EquipmentInventoryStore(f.env.DB) };
}

function insert(f: Awaited<ReturnType<typeof fixture>>, id: string, location: 'STORED' | 'EQUIPPED' = 'STORED', slot = 0) {
  f.sql.run(`INSERT INTO run_equipment_instances
    (run_id,instance_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision)
    VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,1)`, [f.runId, id, `source-${id}`, 'gear-a', 'fixture-v1', 'weapon', 'SET01', 1, 0,
    location, location === 'EQUIPPED' ? 'operator' : null, location === 'EQUIPPED' ? 'op0' : null, location === 'EQUIPPED' ? slot : null]);
}

it('reads owned physical items and counts equipped gear outside storage capacity', async () => {
  const f = await fixture();
  expect(await f.store.readInventory('a', f.runId)).toMatchObject({ revision: 1, capacitySlots: 30, usedSlots: 0, items: [] });
  for (let i = 0; i < 30; i++) insert(f, `stored-${i}`);
  insert(f, 'equipped', 'EQUIPPED');
  const read = await f.store.readInventory('a', f.runId);
  expect(read).toMatchObject({ revision: 1, capacitySlots: 30, usedSlots: 30 });
  expect(read?.items).toHaveLength(31);
  await expect(f.store.readInventory('b', f.runId)).rejects.toThrow('equipment_run_not_found');
  insert(f, 'overflow');
  await expect(f.store.readInventory('a', f.runId)).rejects.toThrow('equipment_inventory_storage_invalid');
});

it('rejects reused identities, one-slot double assignment, invalid location and identity rewrites', async () => {
  const f = await fixture();
  insert(f, 'first', 'EQUIPPED');
  expect(() => insert(f, 'second', 'EQUIPPED')).toThrow();
  expect(() => insert(f, 'first')).toThrow();
  expect(() => f.sql.run(`INSERT INTO run_equipment_instances
    (run_id,instance_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES(?,?,?,?,?,?,?,?,?,'STORED',1)`, [f.runId, 'new-id', 'source-first', 'gear-a', 'fixture-v1', 'weapon', 'SET01', 1, 0])).toThrow();
  expect(() => f.sql.run("UPDATE run_equipment_instances SET source_item_id='changed',revision=2 WHERE instance_id='first'")).toThrow('equipment_instance_identity_immutable');
  expect(() => f.sql.run("UPDATE run_equipment_instances SET revision=3 WHERE instance_id='first'")).toThrow('equipment_instance_revision_invalid');
  expect(() => f.sql.run("UPDATE run_equipment_instances SET location_kind='STORED',revision=2 WHERE instance_id='first'")).toThrow();
});

it('keeps historical runs without v18 state or physical items unclassified', async () => {
  const f = await fixture(), snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  f.sql.run('DELETE FROM run_equipment_inventory WHERE run_id=?', [f.runId]);
  delete snapshot.equipmentInventoryCapacity;
  f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(snapshot), f.runId]);
  expect(await f.store.readInventory('a', f.runId)).toBeNull();
});
