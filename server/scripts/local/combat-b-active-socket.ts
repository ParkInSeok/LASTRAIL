import {LocalRankFixtureOwner,rankFixtureVersion} from '../../apps/game-api/src/combat-b-local-rank-fixture';
import {verifyAccessToken} from '../../apps/game-api/src/auth';
import initSqlJs from 'sql.js';
import {createHash} from 'node:crypto';
import {createBActiveSourceHttp} from '../../apps/game-api/src/combat-b-active-http';
import {activeSourceVersion} from '../../apps/game-api/src/combat-b-active-source';
import {localSourceVersion} from '../../apps/game-api/src/combat-b-local-flow-source';
import {canonicalJson} from '../../packages/common/src/index';
import {readFileSync,readdirSync} from 'node:fs';
import {join} from 'node:path';
import {createAccessToken} from '../../apps/game-api/src/auth';
import {refitHttpVersion} from '../../apps/game-api/src/combat-b-refit-http';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {startWorldBossLoopback} from './world-boss-loopback';
type Cleanup={listenerClosed:boolean;databaseDiscarded:boolean;cleanupFailure:boolean};
/** Disposable trusted LOCAL supervisor port; secret intake/checkpoint are memory only.
 * The supervisor owns child cancellation/signals and must close this service in finally.
 */
export async function startBActiveSocket(options:{port?:number;lifetimeMs?:number;restore?:Uint8Array}={}){
 const lifetime=options.lifetimeMs??600000,port=options.port??0;
 if(!Number.isSafeInteger(lifetime)||lifetime<1||lifetime>900000||!Number.isSafeInteger(port)||port<0||port>65535)throw new Error('combat_b_refit_socket_options_invalid');
 const SQL=await initSqlJs(),sql=new SQL.Database(options.restore);
 let listener:Awaited<ReturnType<typeof startWorldBossLoopback>>|undefined;
 let secret=crypto.randomUUID()+crypto.randomUUID();
 const bundles=new Map<'a'|'b',{accountId:string;accessExpiresAt:number;headers:Record<string,string>}>();
 try{
  if(!options.restore){
   for(const f of readdirSync('packages/db/migrations').filter(f=>/^\d+_.+\.sql$/u.test(f)).sort())sql.run(readFileSync(join('packages/db/migrations',f),'utf8'));
   sql.run(readFileSync('apps/game-api/local/combat-b-selection.sql','utf8'));
   sql.run(readFileSync('apps/game-api/local/combat-b-refit.sql','utf8'));
   sql.run(readFileSync('apps/game-api/local/combat-b-refit-http.sql','utf8'));
   sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
   const catalog={version:'LOCAL_B_CATALOG_1',status:'ISOLATED_LOCAL_FIXTURE',unlockedSlotCount:5,carriages:[{carriageId:'LOCAL_CAR',occupants:['OPERATOR','CARGO']}]};
   for(const account of ['a','b']){
    sql.run('INSERT INTO account_carriages(account_id,carriage_id) VALUES(?,?)',[account,'LOCAL_CAR']);
    sql.run('INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES(?,?,0)',[account,'op_eve']);
    sql.run('INSERT INTO combat_b_fixture_catalog VALUES(?,1,?)',[account,JSON.stringify(catalog)]);
    const placement={version:'combat.b-placement.v2.20261002',mountedTurrets:[],placements:[{slot:0,carriageId:'LOCAL_CAR',occupant:{kind:'OPERATOR',operatorId:'op_eve'}},{slot:1,carriageId:'LOCAL_CAR',occupant:{kind:'CARGO'}}]};
    sql.run('INSERT INTO combat_b_selections VALUES(?,?,?,?,1,1,0)',[account,'combat.b-selection-http.v1.20261002',canonicalJson(placement),'LOCAL_B_CATALOG_1']);
    const source={status:'ISOLATED_LOCAL_FIXTURE',ranks:['BCB01','BCB02','BCB03','BCB04','BCB05','BCB06','BCB07'].map(type=>({type,rank:1})),instances:[{slot:0,carriageId:'LOCAL_CAR',instanceId:account+'_physical1'},{slot:1,carriageId:'LOCAL_CAR',instanceId:account+'_physical2'}]};
    sql.run('INSERT INTO combat_b_fixture_refits VALUES(?,1,1,?)',[account,JSON.stringify(source)]);
   }
  }else{
   // Only trusted private supplier bytes; original B selection/receipt/catalog untouched.
   sql.run('SELECT account_id FROM combat_b_refit_selections');
   sql.run('SELECT account_id FROM combat_b_refit_receipts');
   sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE status='ACTIVE'");
  }
  // New LOCAL extension captures existing physical inventory once; restore keeps original registry.
  const installed=sql.exec("SELECT name FROM sqlite_master WHERE type='table' AND name='combat_b_local_physical_inventory'").length>0;
  if(!installed){
   sql.run(readFileSync('apps/game-api/local/combat-b-source-binding.sql','utf8'));
   const originalProfile=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json','utf8')).profile;
   for(const account of ['a','b']){
    // Exact pre-existing isolated owned-profile fixture growth inputs, never new operating issuance.
    for(const op of originalProfile.operators){
     sql.run('INSERT OR IGNORE INTO account_operators(account_id,operator_id,acquired_at) VALUES(?,?,0)',[account,op.operatorId]);
     sql.run('UPDATE account_operators SET promotion=?,skill_training=?,revision=? WHERE account_id=? AND operator_id=?',[op.promotion,op.skillTraining,op.sources.operator.revision,account,op.operatorId]);
     sql.run('INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level) VALUES(?,?,?,?)',[account,op.slot,op.operatorId,op.level]);
    }
    sql.run('INSERT INTO account_train(account_id,train_id,revision,updated_at) VALUES(?,?,1,0)',[account,account+'_local_train']);
    for(const car of ['car_combat','car_gun'])sql.run('INSERT INTO account_carriages(account_id,carriage_id) VALUES(?,?)',[account,car]);
    sql.run('INSERT INTO commander_presets(id,account_id,name,preset_json,revision,updated_at) VALUES(?,?,?, ?,1,0)',[account+'_local_preset',account,'LOCAL_OWNED_PROFILE','{}']);
    sql.run('INSERT INTO account_formation(account_id,preset_id,preset_revision,formation_json,revision,updated_at) VALUES(?,?,1,?,1,0)',[account,account+'_local_preset',JSON.stringify({schemaVersion:1,operators:originalProfile.operators.map((o:{slot:number;operatorId:string})=>({slot:o.slot,operatorId:o.operatorId}))})]);
    sql.run('INSERT INTO account_train_loadout VALUES(?,?,1,0)',[account,JSON.stringify({rulesVersion:'train-loadout-v1',trainId:account+'_local_train',carriages:[{slot:0,carriageId:'car_combat'},{slot:1,carriageId:'car_gun'}]})]);
   }
  }
  if(!sql.exec("SELECT name FROM sqlite_master WHERE type='table' AND name='combat_b_local_rank_receipts'").length)sql.run(readFileSync('apps/game-api/local/combat-b-rank-fixture.sql','utf8'));
  const now=Date.now(),accessExpiresAt=now+900000;
  for(const account of ['a','b'] as const){
   const sessionId=crypto.randomUUID();
   sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?,?,0,'ACTIVE',?,?)",[sessionId,account,crypto.randomUUID(),crypto.randomUUID(),now,accessExpiresAt]);
   const token=await createAccessToken({secret,accountId:account,sessionId,nowMs:now,ttlMs:900000});
   bundles.set(account,{accountId:account,accessExpiresAt,headers:{Authorization:'Bearer '+token,'X-Combat-B-Refit-Version':refitHttpVersion,'X-Combat-B-Selection-Version':'combat.b-selection-http.v1.20261002','X-Combat-B-Source-Version':localSourceVersion,'X-Combat-B-Active-Version':activeSourceVersion}});
  }
  const referenceBytes=readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261002/AttackConsumer/local-original-input.json');
  const app=createBActiveSourceHttp({template:JSON.parse(referenceBytes.toString('utf8')),sha256:createHash('sha256').update(referenceBytes).digest('hex')});
  const env={DB:new SqlJsD1Database(sql) as unknown as D1Database,ENVIRONMENT:'LOCAL',LOCAL_B_REFIT:'ISOLATED_LOCAL_FIXTURE',LOCAL_B_SELECTION:'ISOLATED_LOCAL_FIXTURE',AUTH_SIGNING_KEY_CURRENT:secret} as unknown as Env;
  listener=await startWorldBossLoopback(async request=>await app.fetch(request,env),port);
  const activeListener=listener,owner=new LocalRankFixtureOwner(env.DB),privateCommands=new Set<Promise<unknown>>();
  let closing:Promise<Cleanup>|undefined,closedResolve:(v:Cleanup)=>void;
  const closed=new Promise<Cleanup>(resolve=>{closedResolve=resolve;});
  const close=()=>{
   if(closing)return closing;
   if(timer)clearTimeout(timer);
   closing=(async()=>{
    const result:Cleanup={listenerClosed:false,databaseDiscarded:false,cleanupFailure:false};
    try{await activeListener.close();result.listenerClosed=true;}catch{result.cleanupFailure=true;}
    await Promise.allSettled([...privateCommands]);
    try{sql.close();result.databaseDiscarded=true;}catch{result.cleanupFailure=true;}
    bundles.clear();secret='';(env as Env&{AUTH_SIGNING_KEY_CURRENT?:string}).AUTH_SIGNING_KEY_CURRENT='';
    closedResolve(result);return result;
   })();
   return closing;
  };
  const timer=setTimeout(()=>{void close();},lifetime);
  return {origin:activeListener.origin,closed,close,
   metadata:{version:rankFixtureVersion,sourceVersion:localSourceVersion,provenance:'ISOLATED_LOCAL_FIXTURE',actualTcp:true,lifetimeMs:lifetime},
   intake(role:'a'|'b'){
    const v=bundles.get(role);if(closing||!v)throw new Error('combat_b_refit_socket_closed');
    return {origin:activeListener.origin,accountId:v.accountId,accessExpiresAt:v.accessExpiresAt,headers:{...v.headers}};
   },
   transitionRanks(role:'a'|'b',input:unknown,key:string){
    const bundle=bundles.get(role);if(closing||!bundle)return Promise.reject(new Error('combat_b_rank_fixture_closed'));
    const job=(async()=>{const verified=await verifyAccessToken({secret,token:bundle.headers.Authorization!.slice(7),nowMs:Date.now()});if(!verified||verified.accountId!==bundle.accountId)throw new Error('combat_b_rank_fixture_auth_invalid');return owner.transition({accountId:verified.accountId,sessionId:verified.sessionId,accessExpiresAt:verified.expiresAt},input,key);})();
    privateCommands.add(job);void job.finally(()=>privateCommands.delete(job)).catch(()=>undefined);return job;
   },
   checkpoint(){
    if(closing)throw new Error('combat_b_refit_socket_closed');
    return sql.export(); // caller owns copy, never save/log/upload, fill(0) after restore.
   }};
 }catch{
  const cleanup:Cleanup={listenerClosed:!listener,databaseDiscarded:false,cleanupFailure:false};
  try{if(listener){await listener.close();cleanup.listenerClosed=true;}}catch{cleanup.cleanupFailure=true;}
  try{sql.close();cleanup.databaseDiscarded=true;}catch{cleanup.cleanupFailure=true;}
  bundles.clear();secret='';
  throw Object.assign(new Error('combat_b_refit_socket_setup_failed'),{cleanup});
 }
}