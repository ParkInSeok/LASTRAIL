import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';

const original = { version: 'equipment.effect-original.v1', equipment: {
  definitionId: 'gear-a', definitionVersion: 'catalog-fixture-v1', star: 4 },
  uniqueEffects: [{ effectId: 'EQ01', grade: 2, definitionVersion: 'effect-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 1200 } }],
  enchantment: { effectId: 'EN08', appliedStar: 3, definitionVersion: 'enchant-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 333 } } };
async function fixture(issue = true) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'effect-state-start')).json();
  const runId = result.runId as string;
  const store = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({ snapshot: original,
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')],
  }) });
  const prepared = await store.prepare('a', runId, { instanceId: 'gear-1', sourceItemId: 'source-1', expectedInventoryRevision: 1 });
  const issuance = f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
    definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-1',?,'source-1','gear-a','catalog-fixture-v1','ATK','SET01',4,0,'STORED',1)`).bind(runId);
  if (issue) await f.env.DB.batch([issuance, ...prepared.statements]);
  return { ...f, runId, originalStore: store, prepared, issuance, state: new EquipmentEffectStateStore(f.env.DB) };
}

it('initializes current effects and immutable history in the original issuance batch', async () => {
  const f = await fixture();
  expect(await f.state.read('a', f.runId, 'gear-1')).toMatchObject({
    effectRevision: 1, instanceRevision: 1, inventoryRevision: 1,
    snapshot: { ...original, version: 'equipment.effect-state.v1' },
  });
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(1);
  await expect(f.state.read('b', f.runId, 'gear-1')).rejects.toThrow('equipment_run_not_found');
  await expect(f.state.read('a', f.runId, ' ')).rejects.toThrow('equipment_command_invalid');
  expect(() => f.sql.run('DELETE FROM run_equipment_effect_history')).toThrow('equipment_effect_history_immutable');
});

it('rejects a physical star change without its matching effect transaction', async () => {
  const f = await fixture();
  f.sql.run("UPDATE run_equipment_instances SET star=5,revision=2 WHERE instance_id='gear-1'");
  await expect(f.state.read('a', f.runId, 'gear-1')).rejects.toThrow('equipment_effect_state_invalid');
  expect(await f.originalStore.read('a', f.runId, 'gear-1')).toEqual(original);
  expect(() => f.sql.run('UPDATE run_equipment_effect_heads SET revision=2')).toThrow('equipment_effect_revision_invalid');
});

it('preserves procedure values across lock and placement revisions, and after close', async () => {
  const f = await fixture();
  f.sql.run("UPDATE run_equipment_instances SET locked=1,revision=2 WHERE instance_id='gear-1'");
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.state.read('a', f.runId, 'gear-1')).toMatchObject({ effectRevision: 1, instanceRevision: 2,
    snapshot: { enchantment: original.enchantment } });
  expect(await f.originalStore.read('a', f.runId, 'gear-1')).toEqual(original);
  f.sql.run("DELETE FROM run_equipment_instances WHERE instance_id='gear-1'");
  expect(await f.state.read('a', f.runId, 'gear-1')).toBeNull();
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(1);
});

it('never creates current state on read and reports a missing head for an issued original', async () => {
  const f = await fixture(false);
  expect(await f.state.read('a', f.runId, 'gear-1')).toBeNull();
  await f.env.DB.batch([f.issuance]);
  expect(await f.state.read('a', f.runId, 'gear-1')).toBeNull();
  await f.env.DB.batch(f.prepared.statements);
  f.sql.run('DROP TRIGGER equipment_effect_head_no_delete');
  f.sql.run('DELETE FROM run_equipment_effect_heads');
  await expect(f.state.read('a', f.runId, 'gear-1')).rejects.toThrow('equipment_effect_state_missing');
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_heads')).toBe(0);
});

it('rolls back physical item, original and history when head insertion fails', async () => {
  const f = await fixture(false);
  f.sql.run("CREATE TRIGGER fail_effect_head BEFORE INSERT ON run_equipment_effect_heads BEGIN SELECT RAISE(ABORT,'injected-head'); END");
  await expect(f.env.DB.batch([f.issuance, ...f.prepared.statements])).rejects.toThrow('injected-head');
  for (const table of ['run_equipment_instances', 'run_equipment_effect_originals', 'run_equipment_effect_history', 'run_equipment_effect_heads']) {
    expect(f.scalar(`SELECT count(*) FROM ${table}`)).toBe(0);
  }
});

it('rejects current history checksum corruption without using the original as a silent fallback', async () => {
  const f = await fixture();
  f.sql.run('DROP TRIGGER equipment_effect_history_no_update');
  f.sql.run('UPDATE run_equipment_effect_history SET payload_hash=?', ['0'.repeat(64)]);
  await expect(f.state.read('a', f.runId, 'gear-1')).rejects.toThrow('equipment_effect_state_invalid');
  expect(await f.originalStore.read('a', f.runId, 'gear-1')).toEqual(original);
});
