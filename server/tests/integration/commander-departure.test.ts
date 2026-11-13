import { it, expect } from 'vitest';
import { commanderFixture } from '../helpers/commander-db-fixture';
import { setup, body } from '../helpers/departure-fixture';
import definition from '../fixtures/commander-s1-test-only.json';
import { CommanderPresetStore } from '../../apps/game-api/src/commander-preset-store';
import { readCommanderDeparture, commanderDepartureGuards } from '../../apps/game-api/src/commander-departure';
it('fences absence-to-selection races and rolls back the whole transaction', async () => {
    const f = await commanderFixture(), state = await readCommanderDeparture(f.db, 'a', definition.handlers);
    await f.store.save('a', f.save, 's');
    await f.store.activate('a', f.activate, 'a');
    await expect(f.db.batch([...commanderDepartureGuards(f.db, 'a', state), f.db.prepare("UPDATE player_profiles SET display_name='bad' WHERE account_id='a'")])).rejects.toThrow();
    expect(f.sql.exec("SELECT display_name FROM player_profiles WHERE account_id='a'")[0]!.values[0]![0]).toBeNull();
});
it('preserves legacy only without S1 state and refuses unsupported chosen effects', async () => {
    const f = await commanderFixture();
    await f.store.save('a', f.save, 's');
    await f.store.activate('a', f.activate, 'a');
    await expect(readCommanderDeparture(f.db, 'a')).rejects.toThrow('commander_definition_unsupported');
    f.sql.run("UPDATE balance_config_versions SET config_json='{}'");
    await expect(readCommanderDeparture(f.db, 'a')).rejects.toThrow('commander_definition_unavailable');
    expect((await readCommanderDeparture(f.db, 'b')).snapshot).toBeNull();
});
it('pins an actual START, keeps it on original replay, and rejects a changed quote', async () => {
    const f = await setup(true);
    f.sql.run("INSERT INTO player_profiles(account_id,commander_xp,created_at,updated_at) VALUES('a',0,0,0)");
    const config = JSON.parse(String(f.scalar('SELECT config_json FROM balance_config_versions')));
    f.sql.run('UPDATE balance_config_versions SET config_json=?', [JSON.stringify({ ...config, commanderTraits: definition })]);
    const store = new CommanderPresetStore(f.env.DB), save = { slotId: 1 as const, expectedPresetRevision: 0, expectedProgressionRevision: 1, rulesVersion: definition.rulesVersion, traitDefinitionVersion: definition.traitDefinitionVersion, ranks: {} };
    await store.save('a', save, 'save');
    await store.activate('a', { slotId: 1, expectedPresetRevision: 1, expectedProgressionRevision: 1, expectedSelectionRevision: 0, rulesVersion: definition.rulesVersion, traitDefinitionVersion: definition.traitDefinitionVersion }, 'active');
    const q = await (await f.preview(body, 'q')).json();
    const first = await f.start(q.operationPreviewId, 'start');
    expect(first.status).toBe(200);
    const r = await first.json();
    expect(r.admission.commanderTraits).toMatchObject({ slotId: 1, presetRevision: 1, ranks: {} });
    const before = f.scalar('SELECT snapshot_json FROM run_snapshots');
    expect(JSON.parse(String(before)).equipmentInventoryCapacity).toEqual({ version: 'equipment.inventory-capacity.v18', do05Rank: 0, slots: 30 });
    expect(r.equipmentInventoryCapacity).toBeUndefined();
    await store.save('a', { ...save, slotId: 2 }, 'save2');
    expect(await (await f.start(q.operationPreviewId, 'start')).json()).toEqual(r);
    expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(before);
    f.sql.run("UPDATE balance_config_versions SET config_json='{\"commanderTraits\":{}}'");
    expect((await store.read('a')).currentRunApplied).toMatchObject({ status: 'APPLIED', snapshot: { progressionRevision: 1, slotId: 1 } });
});
it('pins new storage capacity without backfilling an old START on replay', async () => {
    const f = await setup(true), q = await (await f.preview(body, 'capacity-q')).json();
    const result = await (await f.start(q.operationPreviewId, 'capacity-start')).json();
    const pinned = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
    expect(pinned.equipmentInventoryCapacity).toEqual({ version: 'equipment.inventory-capacity.v18', do05Rank: 0, slots: 30 });
    delete pinned.equipmentInventoryCapacity;
    const legacyBytes = JSON.stringify(pinned);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [legacyBytes]);
    expect(await (await f.start(q.operationPreviewId, 'capacity-start')).json()).toEqual(result);
    expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(legacyBytes);
});
it('requires reprepare for old public quotes but replays already committed old START', async () => {
    const f = await setup(true), q = await (await f.preview(body, 'old-q')).json();
    f.sql.run("UPDATE operation_previews SET admission_version='departure-v2' WHERE id=?", [q.operationPreviewId]);
    const request = () => f.post('/v1/runs/start', { operationPreviewId: q.operationPreviewId }, 'old-start');
    expect(await (await request()).json()).toMatchObject({ errorCode: 'departure_quote_changed' });
    f.sql.run("UPDATE operation_previews SET admission_version='departure-v4' WHERE id=?", [q.operationPreviewId]);
    const r = await (await f.start(q.operationPreviewId, 'old-start')).json();
    f.sql.run("UPDATE operation_previews SET admission_version='departure-v2' WHERE id=?", [q.operationPreviewId]);
    expect(await (await request()).json()).toEqual(r);
});
it('rejects quote changes in XP and pins the legacy absence at commit', async () => {
    const f = await setup(true);
    f.sql.run("INSERT INTO player_profiles(account_id,commander_xp,created_at,updated_at) VALUES('a',0,0,0)");
    const config = JSON.parse(String(f.scalar('SELECT config_json FROM balance_config_versions')));
    f.sql.run('UPDATE balance_config_versions SET config_json=?', [JSON.stringify({ ...config, commanderTraits: definition })]);
    const q = await (await f.preview(body, 'q')).json();
    f.sql.run("UPDATE player_profiles SET commander_xp=1 WHERE account_id='a'");
    expect((await f.start(q.operationPreviewId, 'start')).status).toBe(409);
    expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);
    const g = await commanderFixture();
    g.sql.run("UPDATE balance_config_versions SET config_json='{}'");
    const state = await readCommanderDeparture(g.db, 'b');
    g.sql.run('UPDATE balance_config_versions SET config_json=?', [JSON.stringify({ commanderTraits: definition })]);
    await expect(g.db.batch(commanderDepartureGuards(g.db, 'b', state))).rejects.toThrow('commander_state_changed');
});

it('distinguishes missing, legacy and malformed stored run snapshots without recalculation',async()=>{
 const {readCommanderRunApplied}=await import('../../apps/game-api/src/commander-preset-store');
 for(const [snapshot,status] of [[null,'UNAVAILABLE'],['{}','LEGACY_UNRECORDED'],['{"commanderTraits":{}}','UNAVAILABLE']] as const){
 const db={prepare:()=>({bind:()=>({first:async()=>({id:'historical',owner_account_id:'a',snapshot_json:snapshot})})})} as unknown as D1Database;
 expect((await readCommanderRunApplied(db,'a')).status).toBe(status);
 }
});
