import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';
import { EquipmentMergeStore, type EquipmentMergeSource } from '../../apps/game-api/src/equipment-merge';

const command = { commandId: 'merge-1', primaryInstanceId: 'main', secondaryInstanceId: 'aux',
  expectedInventoryRevision: 1, expectedPrimaryRevision: 1, expectedSecondaryRevision: 1,
  expectedPrimaryEffectRevision: 1, expectedSecondaryEffectRevision: 1,
  expectedSourceVersion: 'fixture-source-v1', confirmedLosses: true };
it('returns the committed effect detail with stable result identity and explicit consumption in opt-in v2',async()=>{
 const f=await fixture();try{
  const request={...command,responseVersion:'equipment.merge-receipt.v2.20261021'};
  const receipt=await f.store.merge('a',f.runId,request);
  expect(receipt).toMatchObject({version:request.responseVersion,resultInstanceId:'main',consumedInstanceId:'aux',inventoryRevision:2,
    effectSnapshot:{equipment:{star:4},enchantment:{effectId:'EN08'}}});
  const current=await f.state.read('a',f.runId,'main');
  expect(receipt).toHaveProperty('effectSnapshot',current!.snapshot);
  expect(await f.state.read('a',f.runId,'aux')).toBeNull();
  f.source.prepare=async()=>{throw Error('unexpected definition reread');};
  const before=f.sql.export();expect(await f.store.merge('a',f.runId,request)).toEqual(receipt);
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  await expect(f.store.merge('a',f.runId,command)).rejects.toThrow('equipment_request_conflict');
 }finally{f.sql.close();}
});
it('keeps legacy merge receipts unchanged when v2 was not requested',async()=>{
 const f=await fixture();try{const receipt=await f.store.merge('a',f.runId,command);
  expect(receipt.version).toBe('equipment.merge-receipt.v1');expect(receipt).not.toHaveProperty('effectSnapshot');
  await expect(f.store.merge('a',f.runId,{...command,responseVersion:'equipment.merge-receipt.v2.20261021'})).rejects.toThrow('equipment_request_conflict');
 }finally{f.sql.close();}
});
const effect = (grade: number) => ({ effectId: 'EQ01', grade, definitionVersion: 'fixture-effect-v1',
  definitionSnapshot: { fixtureOnly: true, coefficient: grade * 100 } });
async function fixture(star = 3, mainEquipped = false, auxEquipped = false) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const started = await (await f.start(quote.operationPreviewId, 'merge-start')).json(), runId = started.runId as string;
  const originals = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async ({ instanceId }) => ({
    snapshot: { version: 'equipment.effect-original.v1', equipment: { definitionId: 'gear-a', definitionVersion: 'fixture-catalog-v1', star },
      uniqueEffects: star < 3 ? [] : [effect(2 ** (star - 3))], enchantment: {
        effectId: instanceId === 'main' ? 'EN08' : 'EN02', appliedStar: 2, definitionVersion: 'fixture-enchant-v1',
        definitionSnapshot: { fixtureOnly: true, coefficient: instanceId === 'main' ? 333 : 999 } } },
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] }) });
  for (const [index, id] of ['main', 'aux'].entries()) {
    const equipped = index === 0 ? mainEquipped : auxEquipped;
    const prepared = await originals.prepare('a', runId, { instanceId: id, sourceItemId: 'source-' + id, expectedInventoryRevision: 1 });
    await f.env.DB.batch([f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,
      definition_id,definition_version,category_id,set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision)
      VALUES(?,?,?,'gear-a','fixture-catalog-v1','ATK',?,?,0,?,?,?,?,1)`)
      .bind(id, runId, 'source-' + id, index === 0 ? 'SET01' : 'SET02', star,
        equipped ? 'EQUIPPED' : 'STORED', equipped ? 'operator' : null, equipped ? 'operator-fixture' : null, equipped ? index : null),
    ...prepared.statements]);
  }
  f.sql.run('CREATE TABLE fixture_merge_safe(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO fixture_merge_safe VALUES(1)');
  let draws = 0;
  const source: EquipmentMergeSource = { prepare: async context => {
    if (context.effects.randomGrant) draws++;
    return { sourceVersion: 'fixture-source-v1', resolvedEffects: context.effects.randomGrant ? [effect(1)]
      : context.effects.fixedEffects.map(value => ({ ...effect(value.grade), effectId: value.effectId,
        definitionVersion: value.definitionVersion, definitionSnapshot: value.knownDefinitionSnapshot ?? effect(value.grade).definitionSnapshot })),
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merge_write_guard(valid) SELECT valid FROM fixture_merge_safe')] };
  } };
  return { ...f, runId, originals, source, draws: () => draws,
    state: new EquipmentEffectStateStore(f.env.DB), store: new EquipmentMergeStore(f.env.DB, source) };
}

it.each([[false, false], [true, false], [false, true], [true, true]])('commits placement %s/%s, consumes auxiliary and retains original history', async (mainEquipped, auxEquipped) => {
  const f = await fixture(3, mainEquipped, auxEquipped);
  const beforeMain = await f.originals.read('a', f.runId, 'main'), beforeAux = await f.originals.read('a', f.runId, 'aux');
  const receipt = await f.store.merge('a', f.runId, command);
  expect(receipt).toMatchObject({ resultInstanceId: 'main', consumedInstanceId: 'aux', star: 4,
    inventoryRevision: 2, instanceRevision: 2, effectRevision: 2, storedAfter: mainEquipped ? 0 : 1 });
  expect(f.scalar("SELECT count(*) FROM run_equipment_instances WHERE instance_id='aux'")).toBe(0);
  expect(f.scalar("SELECT set_tag FROM run_equipment_instances WHERE instance_id='main'")).toBe('SET01');
  expect(f.scalar("SELECT location_kind FROM run_equipment_instances WHERE instance_id='main'")).toBe(mainEquipped ? 'EQUIPPED' : 'STORED');
  expect(await f.state.read('a', f.runId, 'main')).toMatchObject({ snapshot: { uniqueEffects: [effect(2)], enchantment: beforeMain!.enchantment } });
  expect(await f.state.read('a', f.runId, 'aux')).toBeNull();
  expect(await f.originals.read('a', f.runId, 'aux')).toEqual(beforeAux);
  expect(await f.originals.read('a', f.runId, 'main')).toEqual(beforeMain);
  expect(f.scalar('SELECT count(*) FROM run_equipment_effect_history')).toBe(3);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await f.store.merge('a', f.runId, command)).toEqual(receipt);
  await expect(f.store.merge('b', f.runId, command)).rejects.toThrow('equipment_run_not_found');
  await expect(f.store.merge('a', f.runId, { ...command, primaryInstanceId: 'aux' })).rejects.toThrow('equipment_request_conflict');
});

it('draws the first grade once at successful 2-to-3 merge and replays without another draw', async () => {
  const f = await fixture(2);
  const receipt = await f.store.merge('a', f.runId, command);
  expect(await f.store.merge('a', f.runId, command)).toEqual(receipt);
  expect(f.draws()).toBe(1);
  expect((await f.state.read('a', f.runId, 'main'))!.snapshot.uniqueEffects).toEqual([effect(1)]);
});

it('rejects absent safety/source, stale confirmation, locked materials and max-star merge', async () => {
  const f = await fixture();
  await expect(new EquipmentMergeStore(f.env.DB).merge('a', f.runId, command)).rejects.toThrow('equipment_merge_verification_required');
  await expect(f.store.merge('a', f.runId, { ...command, confirmedLosses: false })).rejects.toThrow('equipment_command_invalid');
  await expect(f.store.merge('a', f.runId, { ...command, expectedSecondaryEffectRevision: 2 })).rejects.toThrow('equipment_revision_conflict');
  f.sql.run("UPDATE run_equipment_instances SET locked=1,revision=2 WHERE instance_id='aux'");
  await expect(f.store.merge('a', f.runId, { ...command, expectedSecondaryRevision: 2 })).rejects.toThrow('equipment_merge_locked');
  const max = await fixture(5);
  await expect(max.store.merge('a', max.runId, command)).rejects.toThrow('equipment_merge_max_star');
});

it.each(['safe', 'aux', 'main', 'inventory', 'account', 'snapshot', 'terminal'])('rejects %s race without partial material consumption', async kind => {
  const f = await fixture();
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    if (kind === 'safe') f.sql.run('UPDATE fixture_merge_safe SET valid=0');
    if (kind === 'aux') f.sql.run("UPDATE run_equipment_instances SET locked=1,revision=2 WHERE instance_id='aux'");
    if (kind === 'main') f.sql.run("UPDATE run_equipment_instances SET locked=1,revision=2 WHERE instance_id='main'");
    if (kind === 'inventory') f.sql.run('UPDATE run_equipment_inventory SET revision=2');
    if (kind === 'account') f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
    if (kind === 'snapshot') f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.fixtureChange',true)");
    if (kind === 'terminal') f.sql.run("UPDATE runs SET status='COMPLETED'");
  };
  await expect(f.store.merge('a', f.runId, command)).rejects.toThrow('equipment_merge_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(2);
  expect(f.scalar("SELECT star FROM run_equipment_instances WHERE instance_id='main'")).toBe(3);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merges')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('merges a full stored inventory using final capacity without temporary extra space or supplies cost', async () => {
  const f = await fixture();
  for (let index = 0; index < 28; index++) {
    f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,
      category_id,set_tag,star,locked,location_kind,revision) VALUES(?,?,?,'gear-a','fixture-catalog-v1','ATK','SET01',3,0,'STORED',1)`,
    ['filler-' + index, f.runId, 'filler-source-' + index]);
  }
  const balance = f.scalar('SELECT balance FROM run_supplies_state');
  expect(await f.store.merge('a', f.runId, command)).toMatchObject({ storedAfter: 29, suppliesCost: 0 });
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(balance);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(29);
});

it('rejects an empty proof, changed source version or wrong resolved grade without material consumption', async () => {
  const f = await fixture();
  for (const kind of ['empty', 'version', 'grade']) {
    const store = new EquipmentMergeStore(f.env.DB, { prepare: async context => {
      const proof = await f.source.prepare(context);
      if (kind === 'empty') return { ...proof, assertions: [] };
      if (kind === 'version') return { ...proof, sourceVersion: 'changed-pool' };
      return { ...proof, resolvedEffects: [effect(1)] };
    } });
    await expect(store.merge('a', f.runId, command)).rejects.toThrow(kind === 'empty' ? 'equipment_merge_verification_required'
      : kind === 'version' ? 'equipment_merge_source_version_conflict' : 'equipment_merge_resolved_effects_invalid');
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(2);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merges')).toBe(0);
});

it('rejects an identical instance or different categories, and keeps existing command-kind receipts intact', async () => {
  const f = await fixture();
  await expect(f.store.merge('a', f.runId, { ...command, secondaryInstanceId: 'main' })).rejects.toThrow('equipment_merge_material_invalid');
  f.sql.run("UPDATE run_equipment_instances SET category_id='HP',revision=2 WHERE instance_id='aux'");
  await expect(f.store.merge('a', f.runId, { ...command, expectedSecondaryRevision: 2 })).rejects.toThrow('equipment_merge_pair_invalid');
  f.sql.run("INSERT INTO run_equipment_receipts VALUES(?,?,'SET_LOCK',?,?)", [f.runId, command.commandId, '0'.repeat(64), '{}']);
  await expect(f.store.merge('a', f.runId, command)).rejects.toThrow('equipment_request_conflict');
});

it.each(['run_equipment_effect_history', 'run_equipment_receipts'])('rolls back both materials, revisions and head after %s failure', async table => {
  const f = await fixture();
  f.sql.run(`CREATE TRIGGER fail_merge BEFORE INSERT ON ${table} BEGIN SELECT RAISE(ABORT,'injected-merge'); END`);
  await expect(f.store.merge('a', f.runId, command)).rejects.toThrow('injected-merge');
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(2);
  expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
  expect(f.scalar("SELECT revision FROM run_equipment_effect_heads WHERE instance_id='main'")).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merges')).toBe(0);
});
