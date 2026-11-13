import {readFileSync,readdirSync} from 'node:fs';
import {createHash} from 'node:crypto';
import type initSqlJs from 'sql.js';
import {canonicalJson} from '@lastrail/common';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {CombatHealthStore} from '../../apps/game-api/src/combat-health-store';
import {WorldBossRailLocalStore} from '../../apps/game-api/src/world-boss-rail-local-store';
import {WorldBossRailJournal} from '../../apps/game-api/src/world-boss-rail-journal';
import {WorldBossRailHealthAdapter} from '../../apps/game-api/src/world-boss-rail-health';
type State=Awaited<ReturnType<WorldBossRailJournal['read']>>;
function pinnedJson(name:string,expected:string){const bytes=readFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/'+name);if(createHash('sha256').update(bytes).digest('hex')!==expected)throw new Error('world_socket_fixture_pin_conflict');return JSON.parse(bytes.toString('utf8'));}
/** Seed data only; caller owns the already allocated DB before any fallible read/provision. */
export async function seedWorldBossSocketTrial(sql:initSqlJs.Database,scenario:'PENDING_SOURCE'|'PENDING_HEALTH'|'APPLIED_HIT'){
 const trace=pinnedJson('world-boss-rail-journal-fixtures-20261002.json','88542909adecf83e87144c0da1c30901d7b6111fe0fb801fed971579178d9d66') as {states:{state:State}[]};
 const healthFixture=pinnedJson('world-boss-rail-health-fixtures-20261002.json','8398b289156069569f6425e50bbb5959c8a83726c02f08078023ac388f14668f') as {cases:{name:string;profile:unknown;originalStart:{version:string;status:string;attemptId:string;contentVersion:string;healthTargets:{id:string;hp:number;maxHp:number}[]}}[]};
 const c=healthFixture.cases.find(x=>x.name==='HIT_PARTIAL_SHIELD');if(!c)throw new Error('world_socket_health_fixture_missing');
 sql.run('PRAGMA foreign_keys=ON');for(const f of readdirSync('packages/db/migrations').filter(f=>f.endsWith('.sql')).sort())sql.run(readFileSync('packages/db/migrations/'+f,'utf8'));
 for(const f of ['world-boss-rail.sql','world-boss-rail-journal.sql','world-boss-rail-health.sql','world-boss-authority-http.sql'])sql.run(readFileSync('apps/game-api/local/'+f,'utf8'));
 for(const id of ['a','b','outsider'])sql.run('INSERT INTO accounts(id,created_at,updated_at) VALUES(?,0,0)',[id]);
 sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('trial-mutation','a',1,'LOCAL_TEST_FIXTURE',0)");sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'LOCAL_TRIAL','{}',0,'LOCAL_TEST')");
 sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('health-run','a','trial-mutation',1,'ACTIVE',1000)");
 const startJson=canonicalJson(c.originalStart),initial=c.originalStart.healthTargets;sql.run("INSERT INTO run_snapshots(run_id,snapshot_json,created_at) VALUES('health-run',?,1000)",[startJson]);
 const db=new SqlJsD1Database(sql) as unknown as D1Database,health=new CombatHealthStore(db,{prepare:async command=>{if(command.kind!=='INITIALIZE'||command.accountId!=='a'||command.runId!=='health-run'||command.startSnapshotJson!==startJson||canonicalJson(command.initialTargets??[])!==canonicalJson(initial))throw new Error('world_socket_initialization_proof_invalid');return {assertions:[db.prepare("INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id='health-run' AND r.account_id='a' AND r.status='ACTIVE' AND s.snapshot_json=?) THEN 1 ELSE 0 END").bind(startJson)]};}});
 await health.initialize('a','health-run',initial);
 const store=new WorldBossRailLocalStore(db),journal=new WorldBossRailJournal(db),h=new WorldBossRailHealthAdapter(db),original=trace.states[0]!.state;
 await store.provision('trial',JSON.parse(readFileSync('tests/fixtures/world-boss-rail-local-definition.json','utf8')),original.frame);await journal.attach('trial',1,original.geometry.profile,original.geometry.initial);await h.attach('a','health-run',c.profile);
 if(scenario!=='PENDING_SOURCE'){for(const e of trace.states.at(-1)!.state.entries.filter(e=>e.kind==='FRAME')){const owner=await journal.read('a','trial');await journal.advance('trial',owner.ownerRevision,e.frame,e.sample);}if(scenario==='APPLIED_HIT')await h.apply('a',{version:'world-boss.rail-health.v1.20261002',attemptId:'trial',contentVersion:'LOCAL_GEO',roundId:'p1r1'});}
}