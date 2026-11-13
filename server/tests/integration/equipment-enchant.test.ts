import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';
import { EquipmentEnchantStore, type EquipmentEnchantSource } from '../../apps/game-api/src/equipment-enchant';

const original = { version: 'equipment.effect-original.v1', equipment: {
  definitionId: 'gear-a', definitionVersion: 'catalog-fixture-v1', star: 4 },
  uniqueEffects: [{ effectId: 'EQ01', grade: 2, definitionVersion: 'effect-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 1200 } }],
  enchantment: { effectId: 'EN08', appliedStar: 3, definitionVersion: 'enchant-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 333 } } };
const enchantment = { effectId: 'EN02', appliedStar: 4, definitionVersion: 'enchant-fixture-v2',
  definitionSnapshot: { fixtureOnly: true, coefficient: 444 } };
const command = { commandId: 'enchant-1', instanceId: 'gear-1', sourceRef: 'event-1',
  expectedSourceVersion: 'event-pool-fixture-v1', replaceExisting: true,
  expectedInventoryRevision: 1, expectedInstanceRevision: 1, expectedEffectRevision: 1 };
async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const start = await (await f.start(quote.operationPreviewId, 'enchant-start')).json();
  const runId = start.runId as string;
  const originals = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({ snapshot: original,
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] }) });
  const prepared = await originals.prepare('a', runId, { instanceId: 'gear-1', sourceItemId: 'source-1', expectedInventoryRevision: 1 });
  await f.env.DB.batch([f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,
    definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-1',?,'source-1','gear-a','catalog-fixture-v1','ATK','SET01',4,0,'STORED',1)`).bind(runId), ...prepared.statements]);
  // Synthetic source only: real event/card ownership, safe state and RNG adapters remain separate.
  f.sql.run('CREATE TABLE fixture_enchant_right(source_ref TEXT PRIMARY KEY,available INTEGER NOT NULL)');
  f.sql.run("INSERT INTO fixture_enchant_right VALUES('event-1',1)");
  let draws = 0;
  const source: EquipmentEnchantSource = { prepare: async () => {
    draws++;
    return { sourceVersion: command.expectedSourceVersion, enchantment, assertions: [f.env.DB.prepare(`INSERT INTO equipment_enchant_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM fixture_enchant_right WHERE source_ref='event-1' AND available=1) THEN 1 ELSE 0 END`)],
    consumeStatements: [f.env.DB.prepare("UPDATE fixture_enchant_right SET available=0 WHERE source_ref='event-1'")] };
  } };
  return { ...f, runId, originals, source, draws: () => draws,
    state: new EquipmentEffectStateStore(f.env.DB), store: new EquipmentEnchantStore(f.env.DB, source) };
}

it('atomically replaces only enchantment, consumes the right and replays without another draw after close', async () => {
  const f = await fixture(), receipt = await f.store.apply('a', f.runId, command);
  expect(receipt).toMatchObject({ enchantment, effectRevision: 2, instanceRevision: 2, inventoryRevision: 2 });
  expect(await f.state.read('a', f.runId, 'gear-1')).toMatchObject({ snapshot: { enchantment, uniqueEffects: original.uniqueEffects } });
  expect(await f.originals.read('a', f.runId, 'gear-1')).toEqual(original);
  expect(f.scalar('SELECT available FROM fixture_enchant_right')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(2);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.apply('a', f.runId, command)).toEqual(receipt);
  expect(f.draws()).toBe(1);
  await expect(f.store.apply('a', f.runId, { ...command, sourceRef: 'other' })).rejects.toThrow('equipment_request_conflict');
  await expect(f.store.apply('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
});

it('requires both source proof and atomic right consumption', async () => {
  const f = await fixture();
  await expect(new EquipmentEnchantStore(f.env.DB).apply('a', f.runId, command)).rejects.toThrow('equipment_enchant_verification_required');
  const proof = await f.source.prepare({ accountId: 'a', runId: f.runId, instanceId: 'gear-1', sourceRef: 'event-1', star: 4,
    expectedSourceVersion: command.expectedSourceVersion });
  for (const missing of [{ ...proof, assertions: [] }, { ...proof, consumeStatements: [] }]) {
    await expect(new EquipmentEnchantStore(f.env.DB, { prepare: async () => missing }).apply('a', f.runId, command))
      .rejects.toThrow('equipment_enchant_verification_required');
  }
  expect(f.scalar('SELECT available FROM fixture_enchant_right')).toBe(1);
});

it('rejects locked items, stale confirmation and unsupported or wrong-star procedures', async () => {
  const f = await fixture();
  await expect(f.store.apply('a', f.runId, { ...command, expectedEffectRevision: 2 })).rejects.toThrow('equipment_revision_conflict');
  f.sql.run('UPDATE run_equipment_instances SET locked=1,revision=2');
  await expect(f.store.apply('a', f.runId, { ...command, expectedInstanceRevision: 2 })).rejects.toThrow('equipment_item_locked');
  f.sql.run('UPDATE run_equipment_instances SET locked=0,revision=3');
  const proof = await f.source.prepare({ accountId: 'a', runId: f.runId, instanceId: 'gear-1', sourceRef: 'event-1', star: 4,
    expectedSourceVersion: command.expectedSourceVersion });
  for (const invalid of [{ ...enchantment, appliedStar: 3 }, { ...enchantment, effectId: 'EN17' }]) {
    await expect(new EquipmentEnchantStore(f.env.DB, { prepare: async () => ({ ...proof, enchantment: invalid }) })
      .apply('a', f.runId, { ...command, expectedInstanceRevision: 3 })).rejects.toThrow('equipment_enchant_source_invalid');
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('cannot consume one source right under a new command', async () => {
  const f = await fixture();
  await f.store.apply('a', f.runId, command);
  await expect(f.store.apply('a', f.runId, { ...command, commandId: 'second', expectedEffectRevision: 2,
    expectedInstanceRevision: 2, expectedInventoryRevision: 2 })).rejects.toThrow('equipment_enchant_source_consumed');
  expect(f.draws()).toBe(1);
});

it('requires the confirmed existing effect and displayed source version before replacement', async () => {
  const f = await fixture();
  await expect(f.store.apply('a', f.runId, { ...command, replaceExisting: false })).rejects.toThrow('equipment_enchant_confirmation_required');
  expect(f.draws()).toBe(0);
  await expect(f.store.apply('a', f.runId, { ...command, expectedSourceVersion: 'stale-pool' }))
    .rejects.toThrow('equipment_enchant_source_version_conflict');
  expect(f.scalar('SELECT available FROM fixture_enchant_right')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_enchantment_changes')).toBe(0);
});

it('allows the same effect to be drawn again at the new procedure star without touching unique effects', async () => {
  const f = await fixture();
  const same = new EquipmentEnchantStore(f.env.DB, { prepare: async context => ({ ...await f.source.prepare(context),
    enchantment: { ...enchantment, effectId: original.enchantment.effectId } }) });
  const receipt = await same.apply('a', f.runId, command);
  expect(receipt.enchantment).toMatchObject({ effectId: 'EN08', appliedStar: 4 });
  expect(await f.state.read('a', f.runId, 'gear-1')).toMatchObject({ snapshot: { uniqueEffects: original.uniqueEffects,
    enchantment: receipt.enchantment } });
  expect(await f.originals.read('a', f.runId, 'gear-1')).toEqual(original);
});

it('requires a current effect record and preserves other command kinds in the shared namespace', async () => {
  const f = await fixture();
  f.sql.run('DROP TRIGGER equipment_effect_head_no_delete');
  f.sql.run('DELETE FROM run_equipment_effect_heads');
  await expect(f.store.apply('a', f.runId, command)).rejects.toThrow('equipment_effect_state_missing');
  expect(f.draws()).toBe(0);
  f.sql.run("INSERT INTO run_equipment_receipts VALUES(?,?,'SET_LOCK',?,?)", [f.runId, command.commandId, '0'.repeat(64), '{}']);
  await expect(f.store.apply('a', f.runId, command)).rejects.toThrow('equipment_request_conflict');
});

it.each(['right', 'inventory', 'instance', 'snapshot', 'account'])('fences %s changes in the commit batch', async kind => {
  const f = await fixture();
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    if (kind === 'right') f.sql.run('UPDATE fixture_enchant_right SET available=0');
    if (kind === 'inventory') f.sql.run('UPDATE run_equipment_inventory SET revision=2');
    if (kind === 'instance') f.sql.run('UPDATE run_equipment_instances SET locked=1,revision=2');
    if (kind === 'snapshot') f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureChange',true)");
    if (kind === 'account') f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
  };
  await expect(f.store.apply('a', f.runId, command)).rejects.toThrow('equipment_enchant_state_conflict');
  expect(f.scalar('SELECT revision FROM run_equipment_effect_heads')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_enchantment_changes')).toBe(0);
  if (kind !== 'right') expect(f.scalar('SELECT available FROM fixture_enchant_right')).toBe(1);
});

it.each(['run_equipment_effect_history', 'run_equipment_receipts'])('rolls back right and all revisions when %s insertion fails', async table => {
  const f = await fixture();
  f.sql.run(`CREATE TRIGGER injected_enchant BEFORE INSERT ON ${table} BEGIN SELECT RAISE(ABORT,'injected-enchant'); END`);
  await expect(f.store.apply('a', f.runId, command)).rejects.toThrow('injected-enchant');
  expect(f.scalar('SELECT available FROM fixture_enchant_right')).toBe(1);
  for (const table of ['run_equipment_effect_heads', 'run_equipment_instances', 'run_equipment_inventory']) {
    expect(f.scalar(`SELECT revision FROM ${table}`)).toBe(1);
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_enchantment_changes')).toBe(0);
  expect(await f.state.read('a', f.runId, 'gear-1')).toMatchObject({ snapshot: { enchantment: original.enchantment } });
});
