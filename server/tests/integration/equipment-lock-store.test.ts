import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentInventoryStore, type EquipmentMaintenanceVerifier } from '../../apps/game-api/src/equipment-inventory-start';

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'lock-start')).json();
  const runId = result.runId as string;
  f.sql.run(`INSERT INTO run_equipment_instances
    (run_id,instance_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES(?,?,?,?,?,?,?,1,0,'STORED',1)`, [runId, 'item-1', 'synthetic-source', 'gear-a', 'fixture-v1', 'weapon', 'SET01']);
  return { ...f, runId, store: new EquipmentInventoryStore(f.env.DB) };
}

const command = { commandId: 'lock-1', instanceId: 'item-1', expectedInventoryRevision: 1, expectedInstanceRevision: 1, locked: true };
const syntheticVerifier = (db: D1Database): EquipmentMaintenanceVerifier => ({
  prepare: async () => ({ assertions: [db.prepare('INSERT INTO equipment_inventory_write_guard(valid) VALUES(1)')] }),
});

it('requires verified safe maintenance and atomically changes only the owned lock and revisions', async () => {
  const f = await fixture();
  await expect(f.store.setLock('a', f.runId, command)).rejects.toThrow('equipment_maintenance_verification_required');
  await expect(new EquipmentInventoryStore(f.env.DB, { prepare: async () => ({ assertions: [] }) })
    .setLock('a', f.runId, command)).rejects.toThrow('equipment_maintenance_verification_required');
  expect(f.scalar("SELECT locked FROM run_equipment_instances WHERE instance_id='item-1'")).toBe(0);
  const store = new EquipmentInventoryStore(f.env.DB, syntheticVerifier(f.env.DB));
  const receipt = await store.setLock('a', f.runId, command);
  expect(receipt).toMatchObject({ version: 'equipment.lock-receipt.v1', locked: true, inventoryRevision: 2, instanceRevision: 2 });
  expect((await store.readInventory('a', f.runId))?.items[0]).toMatchObject({ locked: 1, revision: 2, location_kind: 'STORED' });
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(1);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.setLock('a', f.runId, command)).toEqual(receipt);
  await expect(store.setLock('a', f.runId, { ...command, locked: false })).rejects.toThrow('equipment_request_conflict');
  await expect(store.setLock('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
});

it('rejects stale, repeated or unsafe changes without receipts', async () => {
  const f = await fixture();
  const store = new EquipmentInventoryStore(f.env.DB, syntheticVerifier(f.env.DB));
  await expect(store.setLock('a', f.runId, { ...command, expectedInventoryRevision: 2 })).rejects.toThrow('equipment_revision_conflict');
  await expect(store.setLock('a', f.runId, { ...command, expectedInstanceRevision: 2 })).rejects.toThrow('equipment_revision_conflict');
  await expect(store.setLock('a', f.runId, { ...command, locked: false })).rejects.toThrow('equipment_lock_unchanged');
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('recovers a lost acknowledgement and prevents a concurrent item change', async () => {
  const f = await fixture(), store = new EquipmentInventoryStore(f.env.DB, syntheticVerifier(f.env.DB));
  const batch = f.env.DB.batch.bind(f.env.DB);
  let lose = true;
  f.env.DB.batch = async statements => { const result = await batch(statements); if (lose) { lose = false; throw new Error('lost'); } return result; };
  const original = await store.setLock('a', f.runId, command);
  expect(original.locked).toBe(true);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(1);
  const g = await fixture(), racing = new EquipmentInventoryStore(g.env.DB, syntheticVerifier(g.env.DB));
  g.adapter.beforeBatch = () => { g.adapter.beforeBatch = undefined; g.sql.run("UPDATE run_equipment_instances SET revision=2 WHERE instance_id='item-1'"); };
  await expect(racing.setLock('a', g.runId, command)).rejects.toThrow('equipment_state_conflict');
  expect(g.scalar("SELECT locked FROM run_equipment_instances WHERE instance_id='item-1'")).toBe(0);
  expect(g.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  const h = await fixture(), suspended = new EquipmentInventoryStore(h.env.DB, syntheticVerifier(h.env.DB));
  h.adapter.beforeBatch = () => { h.adapter.beforeBatch = undefined; h.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'"); };
  await expect(suspended.setLock('a', h.runId, command)).rejects.toThrow('equipment_state_conflict');
  expect(h.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('fences maintenance proof changes and rolls back both revisions if receipt storage fails', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE maintenance_test(safe INTEGER NOT NULL)');
  f.sql.run('INSERT INTO maintenance_test VALUES(1)');
  const verifier: EquipmentMaintenanceVerifier = { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO equipment_inventory_write_guard(valid) SELECT CASE WHEN safe=1 THEN 1 ELSE 0 END FROM maintenance_test')] }) };
  const store = new EquipmentInventoryStore(f.env.DB, verifier);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE maintenance_test SET safe=0'); };
  await expect(store.setLock('a', f.runId, command)).rejects.toThrow('equipment_state_conflict');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  f.sql.run('UPDATE maintenance_test SET safe=1');
  f.sql.run("CREATE TRIGGER fail_lock_receipt BEFORE INSERT ON run_equipment_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
  await expect(store.setLock('a', f.runId, command)).rejects.toThrow('injected');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar("SELECT revision FROM run_equipment_instances WHERE instance_id='item-1'")).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});
