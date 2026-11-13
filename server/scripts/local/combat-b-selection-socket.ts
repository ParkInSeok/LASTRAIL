import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import {join} from 'node:path';
import {createAccessToken} from '../../apps/game-api/src/auth';
import {createCombatBSelectionHttp,selectionVersion} from '../../apps/game-api/src/combat-b-selection-http';
import {LocalFixtureBSelectionCatalog} from '../../apps/game-api/src/combat-b-selection-catalog';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {startWorldBossLoopback} from './world-boss-loopback';
type Cleanup={listenerClosed:boolean;databaseDiscarded:boolean;cleanupFailure:boolean};
/** Disposable trusted LOCAL supervisor port; secret intake/checkpoint are memory only.
 * The supervisor owns child cancellation/signals and must close this service in finally.
 */
export async function startBSelectionSocket(options:{port?:number;lifetimeMs?:number;restore?:Uint8Array}={}){
 const lifetime=options.lifetimeMs??600000,port=options.port??0;
 if(!Number.isSafeInteger(lifetime)||lifetime<1||lifetime>900000||!Number.isSafeInteger(port)||port<0||port>65535)throw new Error('combat_b_socket_options_invalid');
 const SQL=await initSqlJs(),sql=new SQL.Database(options.restore);
 let listener:Awaited<ReturnType<typeof startWorldBossLoopback>>|undefined;
 let secret=crypto.randomUUID()+crypto.randomUUID();
 const bundles=new Map<'a'|'b',{accountId:string;accessExpiresAt:number;headers:Record<string,string>}>();
 try{
  if(!options.restore){
   for(const f of readdirSync('packages/db/migrations').filter(f=>/^\d+_.+\.sql$/u.test(f)).sort())sql.run(readFileSync(join('packages/db/migrations',f),'utf8'));
   sql.run(readFileSync('apps/game-api/local/combat-b-selection.sql','utf8'));
   sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
   const catalog={version:'LOCAL_B_CATALOG_1',status:'ISOLATED_LOCAL_FIXTURE',unlockedSlotCount:5,carriages:[{carriageId:'LOCAL_CAR',occupants:['OPERATOR','CARGO']}]};
   for(const account of ['a','b']){
    sql.run('INSERT INTO account_carriages(account_id,carriage_id) VALUES(?,?)',[account,'LOCAL_CAR']);
    sql.run('INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES(?,?,0)',[account,'op_eve']);
    sql.run('INSERT INTO combat_b_fixture_catalog VALUES(?,1,?)',[account,JSON.stringify(catalog)]);
   }
  }else{
   // Only trusted private supplier bytes; original B selection/receipt/catalog untouched.
   sql.run('SELECT account_id FROM combat_b_selections');
   sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE status='ACTIVE'");
  }
  const now=Date.now(),accessExpiresAt=now+900000;
  for(const account of ['a','b'] as const){
   const sessionId=crypto.randomUUID();
   sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?,?,0,'ACTIVE',?,?)",[sessionId,account,crypto.randomUUID(),crypto.randomUUID(),now,accessExpiresAt]);
   const token=await createAccessToken({secret,accountId:account,sessionId,nowMs:now,ttlMs:900000});
   bundles.set(account,{accountId:account,accessExpiresAt,headers:{Authorization:'Bearer '+token,'X-Combat-B-Selection-Version':selectionVersion}});
  }
  const app=createCombatBSelectionHttp(new LocalFixtureBSelectionCatalog());
  const env={DB:new SqlJsD1Database(sql) as unknown as D1Database,ENVIRONMENT:'LOCAL',LOCAL_B_SELECTION:'ISOLATED_LOCAL_FIXTURE',AUTH_SIGNING_KEY_CURRENT:secret} as unknown as Env;
  listener=await startWorldBossLoopback(async request=>await app.fetch(request,env),port);
  const activeListener=listener;
  let closing:Promise<Cleanup>|undefined,closedResolve:(v:Cleanup)=>void;
  const closed=new Promise<Cleanup>(resolve=>{closedResolve=resolve;});
  const close=()=>{
   if(closing)return closing;
   if(timer)clearTimeout(timer);
   closing=(async()=>{
    const result:Cleanup={listenerClosed:false,databaseDiscarded:false,cleanupFailure:false};
    try{await activeListener.close();result.listenerClosed=true;}catch{result.cleanupFailure=true;}
    try{sql.close();result.databaseDiscarded=true;}catch{result.cleanupFailure=true;}
    bundles.clear();secret='';(env as Env&{AUTH_SIGNING_KEY_CURRENT?:string}).AUTH_SIGNING_KEY_CURRENT='';
    closedResolve(result);return result;
   })();
   return closing;
  };
  const timer=setTimeout(()=>{void close();},lifetime);
  return {origin:activeListener.origin,closed,close,
   metadata:{version:selectionVersion,provenance:'ISOLATED_LOCAL_FIXTURE',actualTcp:true,lifetimeMs:lifetime},
   intake(role:'a'|'b'){
    const v=bundles.get(role);if(closing||!v)throw new Error('combat_b_socket_closed');
    return {origin:activeListener.origin,accountId:v.accountId,accessExpiresAt:v.accessExpiresAt,headers:{...v.headers}};
   },
   checkpoint(){
    if(closing)throw new Error('combat_b_socket_closed');
    return sql.export(); // caller owns copy, never save/log/upload, fill(0) after restore.
   }};
 }catch{
  const cleanup:Cleanup={listenerClosed:!listener,databaseDiscarded:false,cleanupFailure:false};
  try{if(listener){await listener.close();cleanup.listenerClosed=true;}}catch{cleanup.cleanupFailure=true;}
  try{sql.close();cleanup.databaseDiscarded=true;}catch{cleanup.cleanupFailure=true;}
  bundles.clear();secret='';
  throw Object.assign(new Error('combat_b_socket_setup_failed'),{cleanup});
 }
}