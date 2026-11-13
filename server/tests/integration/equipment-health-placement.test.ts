import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';
import { EquipmentPlacementStore } from '../../apps/game-api/src/equipment-placement';
import { EquipmentHealthPlacementStore } from '../../apps/game-api/src/equipment-health-placement';
import { canonicalJson, sha256Hex } from '@lastrail/common';

const command = { placement: { commandId: 'swap', instanceId: 'gear-1', expectedInventoryRevision: 1,
  expectedInstanceRevision: 1, target: { kind: 'operator', wearerId: 'operator-2', slotIndex: 0 }, expectedTargetInstanceId: 'gear-2' },
  expectedHealthRevision: 1, expectedStatVersion: 'fixture-stats-v1' };
async function fixture() {
  const f = await setup(true), q = await (await f.preview()).json();
  const runId = (await (await f.start(q.operationPreviewId, 'linked-health-start')).json()).runId as string;
  for (let n = 1; n <= 2; n++) f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,
    definition_id,definition_version,category_id,set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision)
    VALUES(?, ?, ?, ?, 'fixture-v1','ATK','SET01',1,0,'EQUIPPED','operator',?,0,1)`,
  [`gear-${n}`, runId, `source-${n}`, `definition-${n}`, `operator-${n}`]);
  const equipment = new EquipmentPlacementStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO equipment_placement_write_guard(valid) VALUES(1)')] }) });
  const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
  await health.initialize('a', runId, [{ id: 'actor-1', hp: 333, maxHp: 1000 }, { id: 'actor-2', hp: 700, maxHp: 1000 }]);
  let calls = 0;
  const source = { prepare: async (context: { assignments: { wearerKind: string; wearerId: string; slots: ({ instanceId: string } | null)[] }[] }) => {
    calls++;
    return { statVersion: 'fixture-stats-v1', targets: context.assignments.map(a => ({ wearerKind: a.wearerKind,
      wearerId: a.wearerId, targetId: a.wearerId.replace('operator', 'actor'), maxHp: a.slots[0]?.instanceId === 'gear-1' ? 2000 : 500 })),
    assertions: [f.env.DB.prepare('INSERT INTO equipment_placement_write_guard(valid) VALUES(1)')] };
  } };
  // Synthetic final-stat source, eligibility and policy proofs; no live production adapter.
  return { ...f, runId, equipment, health, source, calls: () => calls,
    store: new EquipmentHealthPlacementStore(f.env.DB, equipment, health, source) };
}

it('links one placement to every affected final wearer maximum and replays the original complete transaction', async () => {
  const f = await fixture(), receipt = await f.store.move('a', f.runId, command);
  expect(receipt.version).toBe('equipment.health-placement.v1');
  expect(receipt.health.map(x => x.after.hp)).toEqual([166, 1400]);
  expect(f.scalar('SELECT count(*) FROM run_equipment_health_links')).toBe(1);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.move('a', f.runId, command)).toEqual(receipt);
  expect(f.calls()).toBe(1);
  await expect(f.store.move('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
  await expect(f.store.move('a', f.runId, { ...command, expectedStatVersion: 'other' })).rejects.toThrow('equipment_health_request_conflict');
});

it('rejects old standalone placement instead of adding health effects later', async () => {
  const f = await fixture();
  await f.equipment.move('a', f.runId, command.placement);
  await expect(f.store.move('a', f.runId, command)).rejects.toThrow('equipment_health_partial_replay');
  expect(f.calls()).toBe(0);
  expect((await f.health.read('a', f.runId)).revision).toBe(1);
});

it('rejects an independently committed first HP result instead of completing the swap later', async () => {
  const f = await fixture(), requestHash = await sha256Hex(canonicalJson(command));
  const eventId = `eqhp:${await sha256Hex(canonicalJson({ runId: f.runId, requestHash,
    wearerKind: 'operator', wearerId: 'operator-1', targetId: 'actor-1' }))}`;
  await f.health.commit('a', f.runId, { eventId, expectedRevision: 1, targetId: 'actor-1', change: { kind: 'MAX_HP_REBASE', maxHp: 500 } });
  await expect(f.store.move('a', f.runId, command)).rejects.toThrow('combat_health_partial_replay');
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('operator-1');
  expect(f.scalar('SELECT count(*) FROM run_equipment_health_links')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(1);
});

it.each(['missing', 'version', 'incomplete', 'duplicate-target'])('rejects %s final-stat proof before any writes', async kind => {
  const f = await fixture(), proof = await f.source.prepare({ assignments: [
    { wearerKind: 'operator', wearerId: 'operator-1', slots: [{ instanceId: 'gear-2' }, null, null] },
    { wearerKind: 'operator', wearerId: 'operator-2', slots: [{ instanceId: 'gear-1' }, null, null] }] });
  const invalid = kind === 'missing' ? { ...proof, assertions: [] } : kind === 'version' ? { ...proof, statVersion: 'other' }
    : kind === 'incomplete' ? { ...proof, targets: proof.targets.slice(0, 1) }
    : { ...proof, targets: proof.targets.map(t => ({ ...t, targetId: 'actor-1' })) };
  const store = new EquipmentHealthPlacementStore(f.env.DB, f.equipment, f.health, { prepare: async () => invalid });
  await expect(store.move('a', f.runId, command)).rejects.toThrow('equipment_health_stat_invalid');
  expect((await f.health.read('a', f.runId)).revision).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it.each(['link', 'health'])('rolls back equipment, all maxima and receipts when %s storage fails', async kind => {
  const f = await fixture(), before = await f.health.read('a', f.runId);
  const table = kind === 'link' ? 'run_equipment_health_links' : 'combat_damage_receipts';
  f.sql.run(`CREATE TRIGGER linked_fail BEFORE INSERT ON ${table} BEGIN SELECT RAISE(ABORT,'linked-failed'); END`);
  await expect(f.store.move('a', f.runId, command)).rejects.toThrow('linked-failed');
  expect(await f.health.read('a', f.runId)).toEqual(before);
  expect(f.scalar("SELECT wearer_id FROM run_equipment_instances WHERE instance_id='gear-1'")).toBe('operator-1');
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
});

it('recovers the linked original after response loss', async () => {
  const f = await fixture(), batch = f.env.DB.batch.bind(f.env.DB);
  f.env.DB.batch = async statements => { await batch(statements); f.env.DB.batch = batch; throw new Error('linked-response-lost'); };
  const receipt = await f.store.move('a', f.runId, command);
  expect(receipt.health).toHaveLength(2);
  expect(f.calls()).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_health_links')).toBe(1);
});

it('rejects stale health revision before committing the swap', async () => {
  const f = await fixture();
  await expect(f.store.move('a', f.runId, { ...command, expectedHealthRevision: 2 })).rejects.toThrow('combat_health_revision_conflict');
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_health_links')).toBe(0);
});

it('fences final-stat source assertions in the combined commit', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE fixture_linked_safe(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO fixture_linked_safe VALUES(1)');
  const source = { prepare: async (context: Parameters<typeof f.source.prepare>[0]) => ({ ...await f.source.prepare(context),
    assertions: [f.env.DB.prepare('INSERT INTO equipment_placement_write_guard(valid) SELECT valid FROM fixture_linked_safe')] }) };
  const store = new EquipmentHealthPlacementStore(f.env.DB, f.equipment, f.health, source);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE fixture_linked_safe SET valid=0'); };
  await expect(store.move('a', f.runId, command)).rejects.toThrow('equipment_health_state_conflict');
  expect((await f.health.read('a', f.runId)).revision).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('rejects a corrupted wearer mapping even when its response checksum was replaced', async () => {
  const f = await fixture(), receipt = await f.store.move('a', f.runId, command);
  expect(() => f.sql.run("UPDATE run_equipment_health_links SET response_json='{}'")).toThrow('equipment_health_link_immutable');
  f.sql.run('DROP TRIGGER equipment_health_link_no_update'); // Explicit corruption injection.
  const damaged = { ...receipt, targets: receipt.targets.map(t => ({ ...t, wearerId: 'unrelated-wearer' })) };
  const json = canonicalJson(damaged);
  f.sql.run('UPDATE run_equipment_health_links SET response_json=?,response_hash=?', [json, await sha256Hex(json)]);
  await expect(f.store.move('a', f.runId, command)).rejects.toThrow('equipment_health_receipt_invalid');
  expect(f.calls()).toBe(1);
});
