import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentEffectOriginalStore, type EquipmentEffectOriginalSource } from '../../apps/game-api/src/equipment-effect-original';

const snapshot = { version: 'equipment.effect-original.v1', equipment: {
  definitionId: 'gear-a', definitionVersion: 'catalog-v15', star: 4 },
  uniqueEffects: [{ effectId: 'EQ01', grade: 2, definitionVersion: 'effect-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 1200 } }],
  enchantment: { effectId: 'EN08', appliedStar: 3, definitionVersion: 'enchant-fixture-v1',
    definitionSnapshot: { fixtureOnly: true, coefficient: 333 } } };

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'effect-original-start')).json();
  return { ...f, runId: result.runId as string };
}
function source(db: D1Database, payload: unknown = snapshot): EquipmentEffectOriginalSource {
  return { prepare: async () => ({ snapshot: payload,
    assertions: [db.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] }) };
}
function issuance(f: Awaited<ReturnType<typeof fixture>>) {
  return f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
    definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES('gear-1',?,'source-1','gear-a','catalog-v15','ATK','SET01',4,0,'STORED',1)`).bind(f.runId);
}
const context = { instanceId: 'gear-1', sourceItemId: 'source-1', expectedInventoryRevision: 1 };

it('requires production-source assertions and never backfills missing original effects', async () => {
  const f = await fixture(), store = new EquipmentEffectOriginalStore(f.env.DB);
  expect(await store.read('a', f.runId, 'gear-1')).toBeNull();
  await expect(store.prepare('a', f.runId, context)).rejects.toThrow('equipment_effect_source_required');
  const empty = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({ snapshot, assertions: [] }) });
  await expect(empty.prepare('a', f.runId, context)).rejects.toThrow('equipment_effect_source_required');
});

it('stores original resolved values with issuance and preserves them after placement and close', async () => {
  const f = await fixture(), store = new EquipmentEffectOriginalStore(f.env.DB, source(f.env.DB));
  const prepared = await store.prepare('a', f.runId, context);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(0);
  await f.env.DB.batch([issuance(f), ...prepared.statements]);
  expect(await store.read('a', f.runId, 'gear-1')).toEqual(snapshot);
  f.sql.run("UPDATE run_equipment_instances SET revision=2,locked=1 WHERE instance_id='gear-1'");
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.read('a', f.runId, 'gear-1')).toEqual(snapshot);
  f.sql.run("DELETE FROM run_equipment_instances WHERE instance_id='gear-1'");
  expect(await store.read('a', f.runId, 'gear-1')).toEqual(snapshot);
  await expect(store.read('b', f.runId, 'gear-1')).rejects.toThrow('equipment_run_not_found');
  expect(() => f.sql.run('DELETE FROM run_equipment_effect_originals')).toThrow('equipment_effect_original_immutable');
});

it('rejects unsupported IDs, wrong grade totals, duplicate effects and future procedure stars', async () => {
  const f = await fixture();
  for (const payload of [
    { ...snapshot, uniqueEffects: [{ ...snapshot.uniqueEffects[0], effectId: 'EQ17' }] },
    { ...snapshot, uniqueEffects: [{ ...snapshot.uniqueEffects[0], grade: 1 }] },
    { ...snapshot, uniqueEffects: [snapshot.uniqueEffects[0], snapshot.uniqueEffects[0]] },
    { ...snapshot, enchantment: { ...snapshot.enchantment, appliedStar: 5 } },
  ]) {
    await expect(new EquipmentEffectOriginalStore(f.env.DB, source(f.env.DB, payload))
      .prepare('a', f.runId, context)).rejects.toThrow('equipment_effect_snapshot_invalid');
  }
});

it('rolls back original issuance on inventory mismatch and failed original snapshot insertion', async () => {
  const f = await fixture(), store = new EquipmentEffectOriginalStore(f.env.DB, source(f.env.DB));
  const stale = await store.prepare('a', f.runId, { ...context, expectedInventoryRevision: 2 });
  await expect(f.env.DB.batch([issuance(f), ...stale.statements])).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  const prepared = await store.prepare('a', f.runId, context);
  f.sql.run("CREATE TRIGGER fail_effect_original BEFORE INSERT ON run_equipment_effect_originals BEGIN SELECT RAISE(ABORT,'injected-effect'); END");
  await expect(f.env.DB.batch([issuance(f), ...prepared.statements])).rejects.toThrow('injected-effect');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(0);
});

it('rejects a corrupted original record instead of recalculating its procedure values', async () => {
  const f = await fixture(), store = new EquipmentEffectOriginalStore(f.env.DB, source(f.env.DB));
  const prepared = await store.prepare('a', f.runId, context);
  await f.env.DB.batch([issuance(f), ...prepared.statements]);
  f.sql.run('DROP TRIGGER equipment_effect_original_no_update');
  f.sql.run('UPDATE run_equipment_effect_originals SET payload_hash=?', ['0'.repeat(64)]);
  await expect(store.read('a', f.runId, 'gear-1')).rejects.toThrow('equipment_effect_storage_invalid');
});

it('accepts only the approved per-item grade totals from one through five stars', async () => {
  const f = await fixture();
  for (const star of [1, 2, 3, 4, 5]) {
    const grade = star < 3 ? 0 : 2 ** (star - 3);
    const payload = { ...snapshot, equipment: { ...snapshot.equipment, star }, enchantment: null,
      uniqueEffects: grade === 0 ? [] : [{ ...snapshot.uniqueEffects[0], grade }] };
    expect((await new EquipmentEffectOriginalStore(f.env.DB, source(f.env.DB, payload))
      .prepare('a', f.runId, context)).snapshot).toEqual(payload);
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(0);
});

it('rolls back issuance when its producer source changes before the batch', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE effect_source_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO effect_source_test VALUES(1)');
  const store = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({ snapshot,
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) SELECT valid FROM effect_source_test')],
  }) });
  const prepared = await store.prepare('a', f.runId, context);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE effect_source_test SET valid=0'); };
  await expect(f.env.DB.batch([issuance(f), ...prepared.statements])).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(0);
});
