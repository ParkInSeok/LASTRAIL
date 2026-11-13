import {readFileSync,readdirSync} from 'node:fs';
import initSqlJs from 'sql.js';
import {canonicalJson} from '@lastrail/common';
import {SqlJsD1Database} from './growth-d1';
import {CombatHealthStore} from '../../apps/game-api/src/combat-health-store';
import {WorldBossRailLocalStore} from '../../apps/game-api/src/world-boss-rail-local-store';
import {WorldBossRailJournal} from '../../apps/game-api/src/world-boss-rail-journal';
import {WorldBossRailHealthAdapter,railHealthVersion,railHealthStartVersion} from '../../apps/game-api/src/world-boss-rail-health';
import {numericDefenseBudgetVersion} from '../../apps/game-api/src/combat-numeric-defense';
import {railVersion} from '../../apps/game-api/src/world-boss-rail-contract';
const availability={trainAvailable:true,operatorAvailable:true,trainConnected:true,operatorConnected:true};
export const frame=(observedAtMs:number)=>({observedAtMs,validUntilMs:observedAtMs+1000,availability});
export const sample=(atMs:number,sequence:number)=>({atMs,sequence,headDistanceMm:10000+(atMs-10000)*10,distanceRemainder2000:0,actualMmPerSecond:10000,targetMmPerSecond:10000,forcedSlowdownBps:0});
export async function worldHealthFixture(options:{hp?:number;shield?:number;expires?:number;protectedUntil?:number;attach?:boolean;base?:string}={}){
 const SQL=await initSqlJs(),sql=new SQL.Database();sql.create_function('unixepoch',()=>0);sql.run('PRAGMA foreign_keys=ON');for(const f of readdirSync('packages/db/migrations').filter(f=>f.endsWith('.sql')).sort())sql.run(readFileSync('packages/db/migrations/'+f,'utf8'));
 for(const f of ['world-boss-rail.sql','world-boss-rail-journal.sql','world-boss-rail-health.sql'])sql.run(readFileSync('apps/game-api/local/'+f,'utf8'));
 for(const account of ['a','b','outsider'])sql.run('INSERT INTO accounts(id,created_at,updated_at) VALUES(?,0,0)',[account]);
 sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('trial-mutation','a',1,'LOCAL_TEST_FIXTURE',0)");
 sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'LOCAL_TRIAL','{}',0,'LOCAL_TEST')");
 sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('health-run','a','trial-mutation',1,'ACTIVE',1000)");
 const initial=[{id:'train',hp:options.hp??1000,maxHp:1000}],start={version:railHealthStartVersion,status:'LOCAL_TEST_PROFILE',attemptId:'trial',contentVersion:'LOCAL_GEO',healthTargets:initial},startJson=canonicalJson(start);
 sql.run("INSERT INTO run_snapshots(run_id,snapshot_json,created_at) VALUES('health-run',?,1000)",[startJson]);
 const adapter=new SqlJsD1Database(sql),db=adapter as unknown as D1Database;
 // Actual SQL guard of explicit trial start/target seed, not a fixed 'proof=1' success stub.
 const health=new CombatHealthStore(db,{prepare:async c=>{if(c.kind!=='INITIALIZE'||c.accountId!=='a'||c.runId!=='health-run'||c.startSnapshotJson!==startJson||canonicalJson(c.initialTargets??[])!==canonicalJson(initial))throw new Error('trial_health_initialization_proof_invalid');return {assertions:[db.prepare("INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id='health-run' AND r.account_id='a' AND r.status='ACTIVE' AND s.snapshot_json=?) THEN 1 ELSE 0 END").bind(startJson)]};}});
 await health.initialize('a','health-run',initial);
 const old=new WorldBossRailLocalStore(db),journal=new WorldBossRailJournal(db),h=new WorldBossRailHealthAdapter(db);
 await old.provision('trial',JSON.parse(readFileSync('tests/fixtures/world-boss-rail-local-definition.json','utf8')),frame(10000));
 const fixture=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/world-boss-rail-journal-fixtures-20261002.json','utf8'));
 await journal.attach('trial',1,fixture.states[0].state.geometry.profile,sample(10000,0));
 const profile={version:railHealthVersion,status:'LOCAL_TEST_PROFILE',attemptId:'trial',contentVersion:'LOCAL_GEO',roundId:'p1r1',targetId:'train',attackRef:'LOCAL_RAIL',damage:{version:numericDefenseBudgetVersion,base:{numerator:options.base??'1000',denominator:'1'},effects:[],defense:{numerator:'1000',denominator:'1'},weakening:[],penetration:[],reductions:{equipment:[],guardStance:[],independent:[{groupId:'TRIAL_REDUCTION_A',valueBps:1000},{groupId:'TRIAL_REDUCTION_B',valueBps:2000}]}},shield:options.shield===0?null:{sourceId:'TRIAL_SHIELD',recipientId:'train',amount:options.shield??100,expiresAtMs:options.expires??18000},protectedUntilMs:options.protectedUntil??0};
 if(options.attach!==false)await h.attach('a','health-run',profile);
 const command={version:railVersion,attemptId:'trial',contentVersion:'LOCAL_GEO',roundId:'p1r1',commandId:'route',expectedRevision:1,action:{kind:'CONFIRM_ROUTE',side:'RIGHT'}};
 const request={version:railHealthVersion,attemptId:'trial',contentVersion:'LOCAL_GEO',roundId:'p1r1'};
 const resolve=async(safe=false)=>{if(safe)await journal.command('a',command,10000);for(const [i,at] of [11000,12000,13000,14000,15000,15500,16500,17000].entries()){const owner=await journal.read('a','trial');await journal.advance('trial',owner.ownerRevision,frame(at),sample(at,i+1));}};
 return {SQL,sql,adapter,db,old,journal,h,health,initial,start,startJson,profile,command,request,resolve};
}