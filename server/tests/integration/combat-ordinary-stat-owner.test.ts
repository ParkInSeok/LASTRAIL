import { expect, it } from 'vitest';
import { ce03ProducerFixture } from '../helpers/ce03-producer-fixture';
import { OrdinaryFiveStatOwner, prepareOrdinaryFiveStatOrigin } from '../../apps/game-api/src/combat-ordinary-stat-owner';
import { readOrdinaryFiveEquipmentSource } from '../../apps/game-api/src/combat-ordinary-equipment-source';
import { equipmentStatFixture } from '../helpers/equipment-run-stat-fixture';

async function fixture() {
  const f = await ce03ProducerFixture();
  const prepared = await prepareOrdinaryFiveStatOrigin(f.env.DB, 'a', 'future-run', { expectedFormationRevision: 1, expectedLoadoutRevision: 1 });
  const owner = await OrdinaryFiveStatOwner.fromOriginal(prepared.origin, prepared.originHash);
  const proof = { sourceId: 'actual-owner-test-double', version: 'v1', revision: 1, status: 'NO_ACTIVE_STAT_MODIFIERS' as const };
  const state = { accountId: 'a', runId: 'future-run', revision: 1, inventoryRevision: 1,
    modifiers: { research: proof, commander: proof, augments: proof, temporary: proof },
    actors: prepared.origin.actors.map(a => ({ operatorId: a.operatorId, currentHp: a.permanentStats.maxHp,
      slots: [null, null, null] as unknown[] })) };
  return { ...f, prepared, owner, state };
}
it('uses actual five owned growth sources and does not reread lobby growth for the run', async () => {
  const f = await fixture();
  try {
    expect(f.prepared.assertions.length).toBeGreaterThan(0);
    f.sql.run("UPDATE account_operators SET promotion=3 WHERE operator_id='op_kyle'");
    const result = await f.owner.resolve(f.state);
    expect(result.actors).toHaveLength(5);
    expect(result.actors[0]).toMatchObject({ operatorId: 'op_kyle', currentHp: 1400,
      effectiveStats: { maxHp: 1400, attack: { numerator: '36', denominator: '1' }, defense: { numerator: '250', denominator: '1' } } });
    await expect(f.env.DB.batch(f.prepared.assertions)).rejects.toThrow();
  } finally { f.sql.close(); }
});
it('applies real slot stat inputs once, then conditional Kyle defense; never resets current HP', async () => {
  const f = await fixture();
  try {
    f.state.actors[0]!.slots[0] = { instanceId: 'def-item', instanceRevision: 1, effectRevision: 1,
      definitionVersion: 'equipment.run-hp-atk-def.v1.20261005', categoryId: 'DEF', star: 1, setTag: 'SET04', uniqueEffects: [], enchantment: null };
    f.state.actors[0]!.currentHp = 699;
    const result = await f.owner.resolve(f.state);
    expect(result.actors[0]).toMatchObject({ currentHp: 699, effectiveStats: { maxHp: 1484,
      defense: { numerator: '358', denominator: '1' } } });
    expect(await f.owner.resolve(structuredClone(f.state))).toEqual(result);
    const boundary = structuredClone(f.state); boundary.revision++; boundary.actors[0]!.currentHp = 742;
    expect((await f.owner.resolve(boundary)).actors[0]!.effectiveStats.defense).toEqual({ numerator: '275', denominator: '1' });
    await expect(f.owner.resolve(f.state)).rejects.toThrow('revision_stale');
  } finally { f.sql.close(); }
});
it('rejects missing modifiers, foreign runs, duplicate equipment and unsupported effects', async () => {
  const f = await fixture();
  try {
    const missing = structuredClone(f.state) as Record<string, unknown>; delete missing.modifiers;
    await expect(f.owner.resolve(missing)).rejects.toThrow();
    await expect(f.owner.resolve({ ...f.state, runId: 'other' })).rejects.toThrow('scope_conflict');
    const item = { instanceId: 'same', instanceRevision: 1, effectRevision: 1, definitionVersion: 'equipment.run-hp-atk-def.v1.20261005', categoryId: 'ATK', star: 1, setTag: 'SET01', uniqueEffects: [], enchantment: null };
    f.state.actors[0]!.slots[0] = item; f.state.actors[1]!.slots[0] = item;
    await expect(f.owner.resolve(f.state)).rejects.toThrow('duplicate_instance');
    f.state.actors[1]!.slots[0] = null;
    f.state.actors[0]!.slots[0] = { ...item, enchantment: { id: 'unknown' } };
    await expect(f.owner.resolve(f.state)).rejects.toThrow();
  } finally { f.sql.close(); }
});
it('rejects corrupt origin and equal-revision conflicting state without replacing the prior result', async () => {
  const f = await fixture();
  try {
    const corrupt = structuredClone(f.prepared.origin); corrupt.actors[0]!.permanentStats.maxHp++;
    await expect(OrdinaryFiveStatOwner.fromOriginal(corrupt, f.prepared.originHash)).rejects.toThrow('origin_hash');
    const result = await f.owner.resolve(f.state);
    const conflict = structuredClone(f.state); conflict.actors[0]!.currentHp--;
    await expect(f.owner.resolve(conflict)).rejects.toThrow('revision_conflict');
    result.actors[0]!.effectiveStats.maxHp = 1;
    expect((await f.owner.resolve(f.state)).actors[0]!.effectiveStats.maxHp).toBe(1400);
  } finally { f.sql.close(); }
});
it('reads actual run equipment slots and effect history, and fences later inventory changes', async () => {
  const f = await equipmentStatFixture();
  try {
    await f.store.move('a', f.runId, f.move('hp-1', 'equip-five', { kind: 'operator', wearerId: 'op_kyle', slotIndex: 0 }));
    const source = await readOrdinaryFiveEquipmentSource(f.env.DB, 'a', f.runId);
    expect(source.actors).toHaveLength(5);
    expect(source.actors.find(a => a.operatorId === 'op_kyle')!.slots[0]).toMatchObject({ instanceId: 'hp-1', categoryId: 'HP', star: 1, uniqueEffects: [], enchantment: null });
    expect(source.actors.find(a => a.operatorId === 'op_eve')!.slots).toEqual([null, null, null]);
    await f.env.DB.batch(source.assertions);
    f.sql.run('UPDATE run_equipment_inventory SET revision=revision+1');
    await expect(f.env.DB.batch(source.assertions)).rejects.toThrow();
    await expect(readOrdinaryFiveEquipmentSource(f.env.DB, 'b', f.runId)).rejects.toThrow();
  } finally { f.sql.close(); }
});
it('rejects stale item/effect versions even under a newer run revision, and permits only one concurrent update', async () => {
  const f = await fixture();
  try {
    const gear = { instanceId: 'item', instanceRevision: 2, effectRevision: 2, definitionVersion: 'equipment.run-hp-atk-def.v1.20261005', categoryId: 'ATK', star: 1, setTag: 'SET01', uniqueEffects: [], enchantment: null };
    f.state.actors[0]!.slots[0] = gear;
    await f.owner.resolve(f.state);
    const stale = structuredClone(f.state); stale.revision++; stale.inventoryRevision++;
    stale.actors[0]!.slots[0] = { ...gear, effectRevision: 1 };
    await expect(f.owner.resolve(stale)).rejects.toThrow('item_revision_stale');
    stale.actors[0]!.slots[0] = { ...gear, star: 2 };
    await expect(f.owner.resolve(stale)).rejects.toThrow('item_revision_conflict');
    stale.actors[0]!.slots[0] = { ...gear, star: 2, instanceRevision: 3, effectRevision: 3 };
    stale.inventoryRevision = 1;
    await expect(f.owner.resolve(stale)).rejects.toThrow('inventory_revision_conflict');
    const first = structuredClone(f.state); first.revision++; first.actors[0]!.currentHp--;
    const second = structuredClone(first); second.actors[0]!.currentHp--;
    const results = await Promise.allSettled([f.owner.resolve(first), f.owner.resolve(second)]);
    expect(results.filter(r => r.status === 'fulfilled')).toHaveLength(1);
    expect(results.filter(r => r.status === 'rejected')).toHaveLength(1);
  } finally { f.sql.close(); }
});
it('does not invent stats when actual training cores are incomplete', async () => {
  const f = await ce03ProducerFixture();
  try {
    f.sql.run("DELETE FROM operator_training_core_slots WHERE account_id='a' AND slot_index=4");
    await expect(prepareOrdinaryFiveStatOrigin(f.env.DB, 'a', 'new-run', { expectedFormationRevision: 1, expectedLoadoutRevision: 1 })).rejects.toThrow('growth_source_unavailable');
    expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);
  } finally { f.sql.close(); }
});
