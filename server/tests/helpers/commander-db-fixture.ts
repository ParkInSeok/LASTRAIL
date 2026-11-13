import initSqlJs from 'sql.js';
import { readFileSync, readdirSync } from 'node:fs';
import { SqlJsD1Database } from './growth-d1';
import definition from '../fixtures/commander-s1-test-only.json';
import { CommanderPresetStore } from '../../apps/game-api/src/commander-preset-store';
export async function commanderFixture() {
    const SQL = await initSqlJs(), sql = new SQL.Database();
    sql.run('PRAGMA foreign_keys=ON');
    for (const f of readdirSync('packages/db/migrations').filter(f => f.endsWith('.sql')).sort())
        sql.run(readFileSync('packages/db/migrations/' + f, 'utf8'));
    sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
    sql.run("INSERT INTO player_profiles(account_id,commander_xp,created_at,updated_at) VALUES('a',999999,0,0),('b',999999,0,0)");
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,1,'TEST_ONLY',?,0,'test')", [JSON.stringify({ commanderTraits: definition })]);
    sql.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at) VALUES('act',1,'PUBLISH','test',0)");
    sql.run("INSERT INTO balance_config_active VALUES(1,1,'act',0)");
    const db = new SqlJsD1Database(sql) as unknown as D1Database, store = new CommanderPresetStore(db, definition.handlers);
    const save = { slotId: 1 as const, expectedPresetRevision: 0, expectedProgressionRevision: 1, rulesVersion: definition.rulesVersion, traitDefinitionVersion: definition.traitDefinitionVersion, ranks: { TR01: 1 } };
    const activate = { ...save, expectedPresetRevision: 1, expectedSelectionRevision: 0 };
    delete (activate as {
        ranks?: unknown;
    }).ranks;
    const count = (table: string) => Number(sql.exec('SELECT count(*) FROM ' + table)[0]!.values[0]![0]);
    return { sql, db, store, save, activate, definition, count };
}
