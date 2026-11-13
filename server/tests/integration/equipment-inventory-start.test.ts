import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentInventoryStore } from '../../apps/game-api/src/equipment-inventory-start';

it('initializes one empty run-owned capacity state in the same START transaction', async () => {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'equipment-start')).json();
  expect(result.accepted).toBe(true);
  expect(f.scalar('SELECT count(*) FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT account_id FROM run_equipment_inventory')).toBe('a');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT capacity_slots FROM run_equipment_inventory')).toBe(30);
  expect(f.scalar('SELECT do05_rank FROM run_equipment_inventory')).toBe(0);
  expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(20);
  expect(await new EquipmentInventoryStore(f.env.DB).readPinnedCapacity('a', result.runId)).toMatchObject({ revision: 1, capacity: { slots: 30 } });
  await expect(new EquipmentInventoryStore(f.env.DB).readPinnedCapacity('b', result.runId)).rejects.toThrow('equipment_run_not_found');
  expect(() => f.sql.run('UPDATE run_equipment_inventory SET capacity_slots=36')).toThrow('equipment_inventory_pin_immutable');
  const original = f.scalar('SELECT snapshot_json FROM run_snapshots');
  expect(await (await f.start(quote.operationPreviewId, 'equipment-start')).json()).toEqual(result);
  expect(f.scalar('SELECT count(*) FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(original);
});

it('rolls back fuel, run and preview when capacity state creation fails', async () => {
  const f = await setup(true), quote = await (await f.preview()).json();
  f.sql.run("CREATE TRIGGER fail_equipment_start BEFORE INSERT ON run_equipment_inventory BEGIN SELECT RAISE(ABORT,'injected-equipment'); END");
  const result = await f.start(quote.operationPreviewId, 'equipment-fail');
  expect(result.status).toBe(409);
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_snapshots')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_inventory')).toBe(0);
  expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(30);
  expect(f.scalar('SELECT status FROM operation_previews')).toBe('ACTIVE');
});

it('rejects a mismatched owner or capacity and does not recreate a historical state', async () => {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'equipment-original')).json();
  const runId = result.runId as string;
  f.sql.run('DELETE FROM run_equipment_inventory WHERE run_id=?', [runId]);
  await expect(new EquipmentInventoryStore(f.env.DB).readPinnedCapacity('a', runId)).rejects.toThrow('equipment_inventory_state_missing');
  expect(() => f.sql.run("INSERT INTO run_equipment_inventory VALUES(?, 'b', 1, 'equipment.inventory-capacity.v18', 0, 30)", [runId])).toThrow();
  expect(() => f.sql.run("INSERT INTO run_equipment_inventory VALUES(?, 'a', 1, 'equipment.inventory-capacity.v18', 1, 36)", [runId])).toThrow();
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  delete snapshot.equipmentInventoryCapacity;
  f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(snapshot), runId]);
  expect(await new EquipmentInventoryStore(f.env.DB).readPinnedCapacity('a', runId)).toBeNull();
  expect(await (await f.start(quote.operationPreviewId, 'equipment-original')).json()).toEqual(result);
  expect(f.scalar('SELECT count(*) FROM run_equipment_inventory')).toBe(0);
});
