import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentPlacementStore } from '../../apps/game-api/src/equipment-placement';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';

const placement = { commandId: 'equip-1', instanceId: 'gear-1', expectedInventoryRevision: 1,
  expectedInstanceRevision: 1, target: { kind: 'operator', wearerId: 'operator-1', slotIndex: 0 }, expectedTargetInstanceId: null };
const rebase = { eventId: 'equip-1-hp', expectedRevision: 1, targetId: 'operator-1', change: { kind: 'MAX_HP_REBASE', maxHp: 2000 } };
async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const runId = (await (await f.start(quote.operationPreviewId, 'equipment-health-start')).json()).runId as string;
  await f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
    definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-1',?,'original-item','gear-a','fixture-v1','ATK','SET01',1,0,'STORED',1)`).bind(runId).run();
  // Synthetic source proofs only: real final-slot stat calculation and supported effect mapping remain required.
  const equipment = new EquipmentPlacementStore(f.env.DB, { prepare: async () => ({ assertions: [
    f.env.DB.prepare('INSERT INTO equipment_placement_write_guard(valid) VALUES(1)')] }) });
  const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [
    f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
  await health.initialize('a', runId, [{ id: 'operator-1', hp: 333, maxHp: 1000 }, { id: 'operator-2', hp: 700, maxHp: 1000 }]);
  return { ...f, runId, equipment, health };
}

it('prepares without writes and atomically saves final placement, health anchor and both receipts', async () => {
  const f = await fixture(), equip = await f.equipment.prepareMove('a', f.runId, placement), hp = await f.health.prepareChange('a', f.runId, rebase);
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('STORED');
  expect((await f.health.read('a', f.runId)).state.targets[0].health.hp).toBe(333);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  await f.env.DB.batch([...equip.statements, ...hp.statements]);
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('operator-1');
  expect((await f.health.read('a', f.runId)).state.targets[0].health).toMatchObject({ hp: 666, maxHp: 2000, anchorHp: 333, anchorMaxHp: 1000 });
  expect(await f.equipment.move('a', f.runId, placement)).toEqual(equip.receipt);
  expect(await f.health.commit('a', f.runId, rebase)).toEqual(hp.receipt);
  expect((await f.equipment.prepareMove('a', f.runId, placement)).statements).toEqual([]);
  expect((await f.health.prepareChange('a', f.runId, rebase)).statements).toEqual([]);
});

it.each([false, true])('swaps two wearers and rebases both atomically, injected failure=%s', async fail => {
  const f = await fixture();
  await f.equipment.move('a', f.runId, placement);
  f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
    definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-2',?,'original-2','gear-b','fixture-v1','ATK','SET02',1,0,'STORED',1)`, [f.runId]);
  await f.equipment.move('a', f.runId, { ...placement, commandId: 'equip-2', instanceId: 'gear-2', expectedInventoryRevision: 2,
    target: { kind: 'operator', wearerId: 'operator-2', slotIndex: 0 } });
  const beforeItems = f.sql.exec('SELECT * FROM run_equipment_instances ORDER BY instance_id');
  const beforeHealth = await f.health.read('a', f.runId);
  const equip = await f.equipment.prepareMove('a', f.runId, { ...placement, commandId: 'swap', expectedInventoryRevision: 3,
    expectedInstanceRevision: 2, expectedTargetInstanceId: 'gear-2', target: { kind: 'operator', wearerId: 'operator-2', slotIndex: 0 } });
  const hp = await f.health.prepareChanges('a', f.runId, [
    { ...rebase, eventId: 'swap-a', change: { kind: 'MAX_HP_REBASE', maxHp: 500 } },
    { ...rebase, eventId: 'swap-b', expectedRevision: 2, targetId: 'operator-2' },
  ]);
  if (fail) {
    f.sql.run("CREATE TRIGGER swap_health_fail BEFORE INSERT ON combat_damage_receipts WHEN NEW.event_id='swap-b' BEGIN SELECT RAISE(ABORT,'swap-health-failed'); END");
    await expect(f.env.DB.batch([...equip.statements, ...hp.statements])).rejects.toThrow('swap-health-failed');
    expect(f.sql.exec('SELECT * FROM run_equipment_instances ORDER BY instance_id')).toEqual(beforeItems);
    expect(await f.health.read('a', f.runId)).toEqual(beforeHealth);
    expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(2);
    expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
  } else {
    await f.env.DB.batch([...equip.statements, ...hp.statements]);
    expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('operator-2');
    expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='gear-2'")).toBe('operator-1');
    expect((await f.health.read('a', f.runId)).state.targets.map(t => t.health.hp)).toEqual([166, 1400]);
    expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(4);
  }
});

it.each(['equipment-receipt', 'health-receipt', 'hp-race', 'equipment-race'])('rolls back the complete pair on %s failure', async kind => {
  const f = await fixture(), equip = await f.equipment.prepareMove('a', f.runId, placement), hp = await f.health.prepareChange('a', f.runId, rebase);
  const before = (await f.health.read('a', f.runId)).state;
  if (kind.endsWith('receipt')) {
    const table = kind === 'equipment-receipt' ? 'run_equipment_receipts' : 'combat_damage_receipts';
    f.sql.run(`CREATE TRIGGER fixture_pair_fail BEFORE INSERT ON ${table} BEGIN SELECT RAISE(ABORT,'pair-receipt-failed'); END`);
  } else {
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
      f.sql.run(kind === 'hp-race' ? 'UPDATE combat_damage_states SET revision=2' : 'UPDATE run_equipment_inventory SET revision=2'); };
  }
  await expect(f.env.DB.batch([...equip.statements, ...hp.statements])).rejects.toThrow();
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('STORED');
  expect(JSON.parse(String(f.scalar('SELECT state_json FROM combat_damage_states')))).toEqual(before);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(kind === 'equipment-race' ? 2 : 1);
});

it('replays both originals after response loss and termination without applying another rebase', async () => {
  const f = await fixture(), equip = await f.equipment.prepareMove('a', f.runId, placement), hp = await f.health.prepareChange('a', f.runId, rebase);
  const batch = f.env.DB.batch.bind(f.env.DB);
  f.env.DB.batch = async statements => { await batch(statements); f.env.DB.batch = batch; throw new Error('pair-response-lost'); };
  await expect(f.env.DB.batch([...equip.statements, ...hp.statements])).rejects.toThrow('pair-response-lost');
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.equipment.move('a', f.runId, placement)).toEqual(equip.receipt);
  expect(await f.health.commit('a', f.runId, rebase)).toEqual(hp.receipt);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(2);
  expect((await f.health.read('a', f.runId)).revision).toBe(2);
});
