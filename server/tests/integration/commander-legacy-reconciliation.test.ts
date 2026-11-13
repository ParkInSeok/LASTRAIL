import { it, expect } from 'vitest';
import { commanderFixture } from '../helpers/commander-db-fixture';
import { CommanderLegacyReconciliation, type CommanderLegacyAdapter } from '../../apps/game-api/src/commander-legacy-reconciliation';
async function fixture(invested: boolean) {
    const f = await commanderFixture(), nodes = new Map(f.definition.nodes.map(n => [n.id, n])), ranks: Record<string, number> = {};
    const invest = (id: string) => { ranks[id] = 1; const parent = nodes.get(id)!.parents[0]; if (parent)
        invest(parent); };
    if (invested)
        invest('OC01');
    f.sql.run("INSERT INTO commander_trait_presets VALUES('a',1,1,?, 'TEST_ONLY_old',1,?,0)", [f.definition.rulesVersion, JSON.stringify({ ranks, preferredSynergyTagId: 'TEST_ONLY_old_tag' })]);
    const adapter: CommanderLegacyAdapter = { sourceVersion: 'TEST_ONLY_old', definition: { ...f.definition, traitDefinitionVersion: 'TEST_ONLY_old' }, cascadeRule: 'OR_DESCENDANTS', decode: row => JSON.parse(row.ranks_json) };
    const service = new CommanderLegacyReconciliation(f.db, [adapter], f.definition.handlers);
    return { ...f, service, ranks };
}
it('refunds old invested3P in the same preset once without minting earned points', async () => { const f = await fixture(true), before = f.sql.exec("SELECT commander_xp FROM player_profiles WHERE account_id='a'")[0]!.values[0]![0]; const first = await f.service.reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion); expect(first).toMatchObject({ accepted: true, refundedPoints: 3, presetRevision: 2 }); expect(await f.service.reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).toEqual(first); expect(f.sql.exec("SELECT commander_xp FROM player_profiles WHERE account_id='a'")[0]!.values[0]![0]).toBe(before); const ranks = JSON.parse(String(f.sql.exec('SELECT ranks_json FROM commander_trait_presets')[0]!.values[0]![0])); expect(ranks.OC01).toBeUndefined(); expect(ranks.preferredSynergyTagId).toBeUndefined(); expect(f.count('commander_trait_migrations')).toBe(1); });
it('removes a tag without a rank for zero refund; unknown definitions do not migrate', async () => { const f = await fixture(false); expect(await f.service.reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).toMatchObject({ refundedPoints: 0 }); expect(await f.service.reconcile('b', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).toEqual({ status: 'NOT_APPLICABLE' }); });
it('rolls back the preset if the migration receipt cannot be written', async () => { const f = await fixture(true); f.sql.run("CREATE TRIGGER fail_migration BEFORE INSERT ON commander_trait_migrations BEGIN SELECT RAISE(ABORT,'test fail'); END"); const before = f.sql.exec('SELECT ranks_json FROM commander_trait_presets')[0]!.values[0]![0]; await expect(f.service.reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).rejects.toThrow(); expect(f.sql.exec('SELECT ranks_json FROM commander_trait_presets')[0]!.values[0]![0]).toBe(before); expect(f.count('commander_trait_receipts')).toBe(0); });
it('requires a trusted source adapter and rolls back a concurrent preset edit', async () => {
    const f = await fixture(true);
    await expect(new CommanderLegacyReconciliation(f.db).reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).rejects.toThrow('commander_definition_unsupported');
    expect(f.count('commander_trait_migrations')).toBe(0);
    const batch = f.db.batch.bind(f.db);
    f.db.batch = async (rows) => { f.sql.run("UPDATE commander_trait_presets SET revision=revision+1 WHERE account_id='a'"); return batch(rows); };
    await expect(f.service.reconcile('a', 1, 'TEST_ONLY_old', f.definition.traitDefinitionVersion)).rejects.toThrow('commander_state_changed');
    expect(f.count('commander_trait_receipts')).toBe(0);
});

it('uses the trusted old graph for descendant removal and alternate-parent survival',async()=>{
 for(const alternate of [false,true]){
 const f=await fixture(true),old=structuredClone(f.definition);old.traitDefinitionVersion='TEST_ONLY_old';
 const child=old.nodes.find(n=>n.kind!=='capstone'&&!f.ranks[n.id]&&!old.nodes.some(other=>other.parents.includes(n.id)))!;
 const parents=[...child.parents];child.parents=alternate?['OC01',...parents]:['OC01'];const ranks={...f.ranks,[child.id]:1};
 const invest=(id:string)=>{ranks[id]=1;const parent=f.definition.nodes.find(n=>n.id===id)!.parents[0];if(parent)invest(parent);};if(alternate)parents.forEach(invest);
 f.sql.run("UPDATE commander_trait_presets SET ranks_json=? WHERE account_id='a'",[JSON.stringify({ranks,preferredSynergyTagId:'TEST_ONLY'})]);
 const service=new CommanderLegacyReconciliation(f.db,[{sourceVersion:'TEST_ONLY_old',definition:old,cascadeRule:'OR_DESCENDANTS',decode:row=>JSON.parse(row.ranks_json)}],f.definition.handlers);
 expect(await service.reconcile('a',1,'TEST_ONLY_old',f.definition.traitDefinitionVersion)).toMatchObject({refundedPoints:alternate?3:4});
 const after=JSON.parse(String(f.sql.exec('SELECT ranks_json FROM commander_trait_presets')[0]!.values[0]![0]));expect(after[child.id]).toBe(alternate?1:undefined);
 }
});
