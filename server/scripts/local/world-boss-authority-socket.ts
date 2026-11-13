import {randomBytes} from 'node:crypto';

import initSqlJs from 'sql.js';
import game from '../../apps/game-api/src/index';
import {verifyAccessToken} from '../../apps/game-api/src/auth';
import {hashPassword} from '../../apps/game-api/src/password-auth';
import {createWorldBossAuthorityLocalHttp,authorityHttpVersion} from '../../apps/game-api/src/world-boss-authority-local-http';
import {railVersion} from '../../apps/game-api/src/world-boss-rail-contract';
import {journalVersion} from '../../apps/game-api/src/world-boss-rail-journal';
import {railHealthVersion} from '../../apps/game-api/src/world-boss-rail-health';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {testMaintenancePolicy} from '../../tests/helpers/maintenance-policy-fixture';
import {seedWorldBossSocketTrial} from './world-boss-socket-database';
import {startWorldBossLoopback} from './world-boss-loopback';
export const worldBossSocketVersion='world-boss.authority-socket.v1.20261002';
export type WorldSocketRole='train'|'operator'|'outsider';
type Scenario='PENDING_SOURCE'|'PENDING_HEALTH'|'APPLIED_HIT';
type Intake={origin:string;accountId:string;accessTokenExpiresAt:string;headers:Record<string,string>};
export class WorldBossSocketError extends Error {readonly listenerClosed=true;readonly databaseDiscarded=true;constructor(readonly code:string,readonly cleanupFailure=false){super('world_socket_setup_failed:'+code);}}
/** Disposable SQL.js world trial. All domain data retain their original fixed LOCAL epoch. */
export async function startWorldBossAuthoritySocket(options:{port?:number;lifetimeMs?:number;restore?:Uint8Array;scenario?:Scenario}={}){
 const port=options.port??0,lifetimeMs=options.lifetimeMs??600000,scenario=options.scenario??'PENDING_SOURCE';
 if(!Number.isSafeInteger(port)||port<0||port>65535||!Number.isSafeInteger(lifetimeMs)||lifetimeMs<1||lifetimeMs>900000||!['PENDING_SOURCE','PENDING_HEALTH','APPLIED_HIT'].includes(scenario)||options.restore&&options.scenario)throw new Error('world_socket_options_invalid');
 let sql:initSqlJs.Database|undefined,listener:Awaited<ReturnType<typeof startWorldBossLoopback>>|undefined,timer:ReturnType<typeof setTimeout>|undefined,closing:Promise<void>|undefined;
 let state:'STARTING'|'RUNNING'|'CLOSING'|'CLOSED'='STARTING',secret=randomBytes(48).toString('base64url'),discarded=false,cleanupFailure=false,pending=Promise.resolve();let finished!:()=>void;const closed=new Promise<void>(resolve=>{finished=resolve;});
 const intakes=new Map<WorldSocketRole,Intake>(),passwords=new Map<WorldSocketRole,{id:string;loginId:string;password:string}>();
 const canIssue=()=>state==='STARTING'||state==='RUNNING';
 const current=()=>{if(state!=='RUNNING'||!sql||discarded)throw new Error('world_socket_closed');return sql;};
 const queue=<T>(action:()=>Promise<T>)=>{const task=pending.then(action);pending=task.then(()=>undefined,()=>undefined);return task;};
 const close=()=>{if(!closing)closing=(async()=>{state='CLOSING';if(timer)clearTimeout(timer);try{await listener?.close();await pending;if(sql&&!discarded)sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE status='ACTIVE'");}finally{intakes.clear();passwords.clear();secret='';if(sql&&!discarded){sql.close();discarded=true;}state='CLOSED';finished();}})();return closing;};
 try{
  const SQL=await initSqlJs();sql=new SQL.Database(options.restore);
  if(options.restore){sql.run('PRAGMA foreign_keys=ON');sql.run('SELECT count(*) FROM world_boss_authority_http_commands');}
  else await seedWorldBossSocketTrial(sql,scenario);
  sql.create_function('unixepoch',()=>Date.now()/1000);testMaintenancePolicy(sql);sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE status='ACTIVE'");
  const versionCode='123',appVersion='1.0.0-api-qa',appHeaders={'X-LastRail-App-Version-Code':versionCode,'X-LastRail-App-Version':appVersion,'X-LastRail-App-Platform':'android'};
  for(const [role,id] of [['train','a'],['operator','b'],['outsider','outsider']] as const){const loginId='socket'+randomBytes(12).toString('hex'),password='LocalOnly!'+randomBytes(24).toString('base64url'),hash=await hashPassword(password);passwords.set(role,{id,loginId,password});sql.run('DELETE FROM auth_password_credentials WHERE account_id=?',[id]);sql.run('INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES(?,?,?,?,?,?,?,?)',[id,loginId,hash.passwordHash,hash.algorithm,hash.paramsJson,loginId+'@example.invalid',Date.now(),Date.now()]);sql.run("INSERT INTO account_tester_entitlements(account_id,environment,version_code,status,expires_at,granted_by,created_at,updated_at) VALUES(?,'DEV','123','ACTIVE',?,'LOCAL_TEST_ONLY',?,?) ON CONFLICT(account_id,environment,version_code) DO UPDATE SET status='ACTIVE',expires_at=excluded.expires_at,revoked_at=NULL",[id,Date.now()+lifetimeMs,Date.now(),Date.now()]);}
  const db=new SqlJsD1Database(sql) as unknown as D1Database;
  const localEnv={DB:db,ENVIRONMENT:'LOCAL' as const,ALLOWED_ADMIN_ORIGINS:'',AUTH_SIGNING_KEY_CURRENT:secret,LOCAL_WORLD_RAIL_PROFILE:'LOCAL_TEST_PROFILE' as const};
  // Existing C2 exact123 legacy admission fixture. Not a current release approval or DEV connection.
  const version={version:appVersion,versionCode,minimumVersionCode:versionCode,contentRelease:{status:'APPROVED',releaseId:'LOCAL_AUTH_FIXTURE_ONLY',contentVersion:'LOCAL_AUTH_ONLY',catalogUrl:'https://example.invalid/local-auth-not-published',catalogHash:'sha256:'+'a'.repeat(64),minimumVersionCode:versionCode,maximumVersionCode:versionCode}};
  const authEnv={...localEnv,ENVIRONMENT:'DEV' as const,CLIENT_ADMISSION_MODE:'ENFORCED',VERSION_BUCKET:{get:async()=>({httpEtag:'"LOCAL_AUTH_ONLY"',json:async()=>version})}};
  const authority=createWorldBossAuthorityLocalHttp();
  listener=await startWorldBossLoopback(request=>queue(async()=>{
   if(state==='CLOSING'||state==='CLOSED')return Response.json({accepted:false,errorCode:'world_socket_closed'},{status:503});
   const u=new URL(request.url),authRoute=request.method==='POST'&&['/v1/auth/password/login','/v1/auth/refresh'].includes(u.pathname);
   if(authRoute){if(u.search)return Response.json({accepted:false,errorCode:'contract_violation'},{status:400});if(request.headers.get('X-LastRail-App-Version-Code')!==versionCode)return Response.json({accepted:false,errorCode:'local_exact_version_required'},{status:403});return game.request(request,undefined,authEnv);}
   if(!/^\/_local\/world-boss\/attempts\/[^/]+\/authority(?:\/commands)?$/u.test(u.pathname))return Response.json({accepted:false,errorCode:'world_socket_route_not_found'},{status:404});
   return authority.request(request,undefined,localEnv);
  }),port);
  const origin=listener.origin;
  const renew=async(role:WorldSocketRole)=>{
   if(!canIssue())throw new Error('world_socket_closed');const p=passwords.get(role);if(!p)throw new Error('world_socket_role_invalid');
   const response=await fetch(origin+'/v1/auth/password/login',{method:'POST',headers:{...appHeaders,'Content-Type':'application/json'},body:JSON.stringify({loginId:p.loginId,password:p.password}),signal:AbortSignal.timeout(5000)});if(response.status!==200)throw new Error('world_socket_supported_login_failed');
   const bundle=await response.json() as {accessToken:string;accessTokenExpiresAt:string},verified=await verifyAccessToken({secret,token:bundle.accessToken,nowMs:Date.now()});if(!verified||verified.accountId!==p.id)throw new Error('world_socket_login_invalid');
   const intake={origin,accountId:p.id,accessTokenExpiresAt:bundle.accessTokenExpiresAt,headers:{Authorization:'Bearer '+bundle.accessToken,'Content-Type':'application/json','X-World-Authority-Version':authorityHttpVersion,'X-World-Rail-Version':railVersion,'X-World-Journal-Version':journalVersion,'X-World-Health-Version':railHealthVersion,'X-World-Content-Version':'LOCAL_GEO'}};if(!canIssue())throw new Error('world_socket_closed');intakes.set(role,intake);return structuredClone(intake);
  };
  for(const role of ['train','operator','outsider'] as const)await renew(role);
  state='RUNNING';timer=setTimeout(()=>{void close().catch(()=>{cleanupFailure=true;});},lifetimeMs);timer.unref();
  return {origin,closed,close,renew,intake:(role:WorldSocketRole)=>{current();const v=intakes.get(role);if(!v)throw new Error('world_socket_role_invalid');return structuredClone(v);},
   revoke:(role:WorldSocketRole)=>queue(async()=>{const s=current(),i=intakes.get(role);if(!i)throw new Error('world_socket_role_invalid');const token=await verifyAccessToken({secret,token:i.headers.Authorization!.slice(7),nowMs:Date.now()});if(!token)throw new Error('world_socket_token_expired');s.run("UPDATE auth_sessions SET status='REVOKED' WHERE id=? AND account_id=?",[token.sessionId,token.accountId]);}),
   checkpoint:()=>queue(async()=>current().export()),
   diagnostics:()=>{const s=current(),count=(table:string)=>Number(s.exec('SELECT count(*) FROM '+table)[0]!.values[0]![0]);return {journalEntries:count('world_boss_rail_geometry_journal'),transportCommands:count('world_boss_authority_http_commands'),healthReceipts:count('world_boss_rail_health_receipts')};},
   get metadata(){return {version:worldBossSocketVersion,origin,lifecycle:state,databaseDiscarded:discarded,cleanupFailure,execution:'ACTUAL_LOOPBACK_TCP_SQLITE',clockMode:'LIVE',operationalProducer:false,originalTrialEpochMs:10000,scenario:options.restore?'MEMORY_CHECKPOINT_RESTORED':scenario,lifetimeMs,auth:'existing real password/login + GAME session replacement; exact123 DEV-policy LOCAL auth fixture only',credentialDelivery:'MEMORY_OR_PARENT_OWNED_IPC_ONLY',credentialArtifact:false};}};
 }catch(error){try{await close();}catch{cleanupFailure=true;}const code=error&&typeof error==='object'&&'code' in error?String(error.code):'SETUP_FAILED';throw new WorldBossSocketError(code,cleanupFailure);}
}