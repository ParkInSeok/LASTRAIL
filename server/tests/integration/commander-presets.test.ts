import { describe, it, expect } from 'vitest';
import { commanderFixture } from '../helpers/commander-db-fixture';
describe('S1 preset authority', () => {
    it('saves independent slots, activates, preserves no-op revisions and original receipts', async () => {
        const f = await commanderFixture();
        const first = await f.store.save('a', f.save, 'save');
        expect(first.presetRevision).toBe(1);
        expect(await f.store.save('a', f.save, 'save')).toEqual(first);
        await f.store.save('a', { ...f.save, slotId: 2 }, 'second');
        expect(f.count('commander_trait_presets')).toBe(2);
        expect((await f.store.activate('a', f.activate, 'activate')).selectionRevision).toBe(1);
        expect((await f.store.save('a', { ...f.save, expectedPresetRevision: 1 }, 'noop')).presetRevision).toBe(1);
        expect((await f.store.read('a')).activeSelection.slotId).toBe(1);
        expect(await f.store.readResult('b', 'save')).toBeNull();
        expect(f.count('commander_presets')).toBe(0);
    });
    it('rejects changed key and stale revisions without side effects', async () => {
        const f = await commanderFixture();
        await f.store.save('a', f.save, 'same');
        await expect(f.store.save('a', { ...f.save, ranks: {} }, 'same')).rejects.toThrow('idempotency_conflict');
        await expect(f.store.save('a', f.save, 'stale')).rejects.toThrow('commander_preset_revision_conflict');
        expect(f.count('commander_trait_receipts')).toBe(1);
    });
    it('replays after definition removal; keeps XP ownership and increments only real XP changes', async () => {
        const f = await commanderFixture();
        const r = await f.store.save('a', f.save, 'save');
        f.sql.run("UPDATE player_profiles SET display_name='other' WHERE account_id='a'");
        expect((await f.store.read('a')).progression?.revision).toBe(1);
        f.sql.run("UPDATE player_profiles SET commander_xp=commander_xp+1 WHERE account_id='a'");
        expect((await f.store.read('a')).progression?.revision).toBe(2);
        f.sql.run("UPDATE balance_config_versions SET config_json='{}'");
        expect(await f.store.save('a', f.save, 'save')).toEqual(r);
    });
    it('rolls back all changes on receipt failure and catches commit-time XP change', async () => {
        const f = await commanderFixture();
        f.sql.run("CREATE TRIGGER fail_receipt BEFORE INSERT ON commander_trait_receipts BEGIN SELECT RAISE(ABORT,'private'); END");
        await expect(f.store.save('a', f.save, 'fail')).rejects.toThrow('commander_state_unavailable');
        expect(f.count('commander_trait_presets')).toBe(0);
        expect(f.count('account_mutations')).toBe(0);
        f.sql.run('DROP TRIGGER fail_receipt');
        const batch = f.db.batch.bind(f.db);
        f.db.batch = async (rows) => { f.sql.run("UPDATE player_profiles SET commander_xp=commander_xp+1 WHERE account_id='a'"); return batch(rows); };
        await expect(f.store.save('a', f.save, 'race')).rejects.toThrow();
        expect(f.count('commander_trait_presets')).toBe(0);
    });
});
it('rejects malformed stored receipts instead of replaying unvalidated data', async () => {
    const f = await commanderFixture();
    f.sql.run("INSERT INTO account_mutations VALUES('bad','a',1,'COMMANDER_TRAITS','bad',0)");
    f.sql.run("INSERT INTO commander_trait_receipts VALUES('a','bad','bad','bad','{}',0)");
    await expect(f.store.readResult('a', 'bad')).rejects.toThrow('commander_state_unavailable');
});
it('keeps historical run visibility when the current definition is malformed', async () => {
    const f = await commanderFixture();
    f.sql.run("UPDATE balance_config_versions SET config_json='{\"commanderTraits\":{}}'");
    const result = await f.store.read('a');
    expect(result.rules).toMatchObject({ supportStatus: 'UNAVAILABLE' });
    expect(result.currentRunApplied).toEqual({ status: 'NONE' });
    await expect(f.store.save('a', f.save, 'bad')).rejects.toThrow('commander_definition_unsupported');
});
it('recovers a committed response loss even when handler support was withdrawn', async () => {
    const f = await commanderFixture(), batch = f.db.batch.bind(f.db);
    f.db.batch = async (rows) => { await batch(rows); throw Error('response lost'); };
    const first = await f.store.save('a', f.save, 'lost');
    expect(first.accepted).toBe(true);
    expect(f.count('account_mutations')).toBe(1);
    f.sql.run("UPDATE player_profiles SET commander_xp=commander_xp+1 WHERE account_id='a'");
    const { CommanderPresetStore } = await import('../../apps/game-api/src/commander-preset-store');
    expect(await new CommanderPresetStore(f.db).save('a', f.save, 'lost')).toEqual(first);
});
it('returns the winning same-key receipt and rejects a different-key stale concurrent save', async () => {
    for (const sameKey of [true, false]) {
        const f = await commanderFixture(), batch = f.db.batch.bind(f.db);
        let winner: unknown;
        f.db.batch = async (rows) => { f.db.batch = batch; winner = await f.store.save('a', f.save, 'winner'); return batch(rows); };
        const loser = f.store.save('a', f.save, sameKey ? 'winner' : 'loser');
        if (sameKey)
            expect(await loser).toEqual(winner);
        else
            await expect(loser).rejects.toThrow('commander_state_changed');
        expect(f.count('commander_trait_receipts')).toBe(1);
        expect(f.count('account_mutations')).toBe(1);
        expect(f.count('commander_trait_guard')).toBe(0);
        expect(f.sql.exec('PRAGMA foreign_key_check')).toEqual([]);
    }
});
it('keeps same-XP revisions and rejects unsafe XP or revision overflow atomically', async () => {
    const f = await commanderFixture();
    f.sql.run("UPDATE player_profiles SET commander_xp=commander_xp WHERE account_id='a'");
    expect((await f.store.read('a')).progression?.revision).toBe(1);
    for (const xp of [-1, 1.5, 9007199254740992])
        expect(() => f.sql.run("UPDATE player_profiles SET commander_xp=? WHERE account_id='a'", [xp])).toThrow();
    f.sql.run("UPDATE player_profiles SET commander_progression_revision=9007199254740991 WHERE account_id='a'");
    expect(() => f.sql.run("UPDATE player_profiles SET commander_xp=1000000 WHERE account_id='a'")).toThrow();
    expect((await f.store.read('a')).progression?.xp).toBe(999999);
});
it('free-resets only the chosen slot and preserves active selection on another slot save', async () => {
    const f = await commanderFixture();
    await f.store.save('a', f.save, 'a1');
    await f.store.activate('a', f.activate, 'active');
    await f.store.save('a', { ...f.save, slotId: 2 }, 'a2');
    await f.store.save('a', { ...f.save, expectedPresetRevision: 1, ranks: {} }, 'reset');
    const read = await f.store.read('a');
    expect(read.activeSelection).toEqual({ slotId: 1, selectionRevision: 1 });
    expect(read.slots[0]?.ranks).toEqual({});
    expect(read.slots[1]?.ranks).toEqual({ TR01: 1 });
    expect((await f.store.activate('a', { ...f.activate, expectedPresetRevision: 2, expectedSelectionRevision: 1 }, 'noop-active')).selectionRevision).toBe(1);
});
it('rejects activation if its preset changes immediately before commit', async () => {
    const f = await commanderFixture();
    await f.store.save('a', f.save, 'save');
    const batch = f.db.batch.bind(f.db);
    f.db.batch = async (rows) => { f.db.batch = batch; await f.store.save('a', { ...f.save, expectedPresetRevision: 1, ranks: {} }, 'reset'); return batch(rows); };
    await expect(f.store.activate('a', f.activate, 'activate')).rejects.toThrow('commander_state_changed');
    expect(f.count('commander_trait_selections')).toBe(0);
});
