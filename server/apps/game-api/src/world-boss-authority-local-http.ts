import {Hono} from 'hono';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {resolveAuthContext} from './index';
import {ActivityError} from './activity-policy';
import {readBoundedBody} from './activity-routes';
import {railCommandSchema,railVersion} from './world-boss-rail-contract';
import {WorldBossRailJournal,journalVersion} from './world-boss-rail-journal';
import {WorldBossRailHealthAdapter,railHealthVersion} from './world-boss-rail-health';
import {LocalRailError} from './world-boss-rail-local-store';
export const authorityHttpVersion='world-boss.authority-local-http.v1.20261002';
type LocalEnv=Env&{LOCAL_WORLD_RAIL_PROFILE:'LOCAL_TEST_PROFILE'};
type Auth={accountId:string;sessionId:string};

type Command=z.infer<typeof railCommandSchema>;
type Metadata={transport_version:string;request_owner_revision:number;command_json:string};
const json=(v:unknown)=>canonicalJson(v as Parameters<typeof canonicalJson>[0]);
const localUrl=(url:string)=>{try{const u=new URL(url);return u.protocol==='http:'&&['localhost','127.0.0.1','[::1]'].includes(u.hostname);}catch{return false;}};
const conflict=(code:string):never=>{throw new LocalRailError(409,code);};
/** Adds transport authorization/header CAS to the existing domain batch, without owning its state. */
class AuthorityCommandStore {
 constructor(private readonly db:D1Database){}
 private metadata(attemptId:string,accountId:string,key:string){return this.db.prepare('SELECT transport_version,request_owner_revision,command_json FROM world_boss_authority_http_commands WHERE attempt_id=? AND account_id=? AND command_id=?').bind(attemptId,accountId,key).first<Metadata>();}
 private verify(meta:Metadata,c:Command,revision:number){
  if(meta.transport_version!==authorityHttpVersion||!Number.isSafeInteger(meta.request_owner_revision)||meta.request_owner_revision<=0)throw new LocalRailError(503,'authority_http_state_invalid');
  try{if(json(railCommandSchema.parse(JSON.parse(meta.command_json)))!==meta.command_json)throw new Error('noncanonical');}catch{throw new LocalRailError(503,'authority_http_state_invalid');}
  if(meta.command_json!==json(c)||meta.request_owner_revision!==revision)conflict('authority_http_key_conflict');
 }
 async command(auth:Auth,c:Command,revision:number,now:number){
  const original=new WorldBossRailJournal(this.db),s=await original.read(auth.accountId,c.attemptId,c.contentVersion),meta=await this.metadata(c.attemptId,auth.accountId,c.commandId);
  const old=s.rail.receipts.find(x=>x.accountId===auth.accountId&&x.commandId===c.commandId);
  if(meta){const entry=s.entries.find(e=>e.key===json(['COMMAND',auth.accountId,c.commandId]));if(!old||!entry||entry.kind!=='COMMAND'||entry.requestDigest!==meta.command_json||entry.ownerRevision-1!==meta.request_owner_revision||json(entry.receipt)!==json(old.receipt))throw new LocalRailError(503,'authority_http_receipt_detached');this.verify(meta,c,revision);return original.command(auth.accountId,c,now);}
  if(old)conflict('authority_http_receipt_not_owned');
  if(s.ownerRevision!==revision)conflict('authority_http_owner_revision_conflict');
  const hash=await sha256Hex(json(s.entries.at(-1)!)),db=this.db;
  const guarded=new Proxy(db,{get(target,key){if(key==='batch')return async(statements:D1PreparedStatement[])=>{
   const authGuard=db.prepare(`INSERT INTO world_boss_authority_http_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM auth_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.id=? AND s.account_id=? AND s.status='ACTIVE' AND s.expires_at>? AND a.status='ACTIVE') THEN 1 ELSE 0 END`).bind(auth.sessionId,auth.accountId,Date.now());
   const sourceGuard=db.prepare(`INSERT INTO world_boss_authority_http_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM world_boss_rail_geometry_heads h JOIN world_boss_local_rail_attempts r ON r.attempt_id=h.attempt_id WHERE h.attempt_id=? AND h.journal_version=? AND h.owner_revision=? AND h.last_hash=? AND h.rail_json=? AND h.frame_json=? AND h.geometry_json=? AND h.binding_json=? AND r.profile_status='LOCAL_TEST_PROFILE' AND r.owner_revision=h.owner_revision AND r.rail_json=h.rail_json AND r.frame_json=h.frame_json) THEN 1 ELSE 0 END`).bind(c.attemptId,journalVersion,revision,hash,json(s.rail),json(s.frame),json(s.geometry),json(s.binding));
   return db.batch([authGuard,sourceGuard,...statements,db.prepare('INSERT INTO world_boss_authority_http_commands VALUES(?,?,?,?,?,?)').bind(c.attemptId,auth.accountId,c.commandId,authorityHttpVersion,revision,json(c)),db.prepare('DELETE FROM world_boss_authority_http_guard')]);
  };const value=Reflect.get(target,key,target);return typeof value==='function'?value.bind(target):value;}});
  const receipt=await new WorldBossRailJournal(guarded).command(auth.accountId,c,now);
  const stored=await this.metadata(c.attemptId,auth.accountId,c.commandId);if(!stored)throw new LocalRailError(503,'authority_http_receipt_detached');this.verify(stored,c,revision);return receipt;
 }
}
async function readState(db:D1Database,accountId:string,attemptId:string,contentVersion:string){
 const journal=new WorldBossRailJournal(db),before=await journal.read(accountId,attemptId,contentVersion),health=await new WorldBossRailHealthAdapter(db).read(accountId,attemptId),after=await journal.read(accountId,attemptId,contentVersion);
 if(json(before)!==json(after))conflict('authority_http_read_conflict');
 return {transportVersion:authorityHttpVersion,status:'LOCAL_TEST_PROFILE' as const,inputProvenance:'TRUSTED_LOCAL_TRIAL' as const,journal:before,health};
}
/** Isolated Hono fetch factory only: no game-worker registration, socket, or health-apply endpoint. */
export function createWorldBossAuthorityLocalHttp(){
 const app=new Hono<{Bindings:LocalEnv}>();
 app.notFound(c=>c.json({accepted:false,errorCode:'authority_route_not_found'},404));
 app.use('*',async(c,next)=>{
  c.header('Cache-Control','no-store');c.header('X-World-Authority-Transport',authorityHttpVersion);
  if(c.env.ENVIRONMENT!=='LOCAL'||c.env.LOCAL_WORLD_RAIL_PROFILE!=='LOCAL_TEST_PROFILE'||!localUrl(c.req.url))return c.json({accepted:false,errorCode:'authority_local_disabled'},404);
  const origin=c.req.header('Origin');if(origin&&(!localUrl(origin)||new URL(origin).origin!==new URL(c.req.url).origin))return c.json({accepted:false,errorCode:'authority_origin_forbidden'},403);
  return next();
 });
 const handler=(write:boolean)=>async(c:Parameters<typeof resolveAuthContext>[0])=>{
  try{
   const auth=await resolveAuthContext(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
   const attempt=c.req.param('attemptId')??'';if(!attempt||attempt.length>200||new URL(c.req.url).search)throw new ActivityError(400,'contract_violation');
   for(const [header,version] of [['X-World-Authority-Version',authorityHttpVersion],['X-World-Rail-Version',railVersion],['X-World-Journal-Version',journalVersion],['X-World-Health-Version',railHealthVersion]] as const)if(c.req.header(header)!==version)conflict('authority_version_conflict');
   const content=c.req.header('X-World-Content-Version');if(!content||content.length>200)throw new ActivityError(400,'authority_content_version_required');
   let response:Awaited<ReturnType<typeof readState>>|{accepted:true;transportVersion:string;status:'LOCAL_TEST_PROFILE';requestJournalRevision:number;receipt:Awaited<ReturnType<WorldBossRailJournal['command']>>};
   if(!write){if((await readBoundedBody(c)).length)throw new ActivityError(400,'contract_violation');response=await readState(c.env.DB,auth.accountId,attempt,content);}
   else{
    if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
    const key=c.req.header('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'idempotency_key_required');
    const rawRevision=c.req.header('X-World-Journal-Revision')??'',revision=Number(rawRevision);if(!/^[1-9][0-9]*$/u.test(rawRevision)||!Number.isSafeInteger(revision))throw new ActivityError(400,'authority_owner_revision_required');
    let input:unknown;try{input=JSON.parse(await readBoundedBody(c));}catch(error){if(error instanceof ActivityError)throw error;throw new ActivityError(400,'contract_violation');}
    const command=railCommandSchema.parse(input);if(command.attemptId!==attempt||command.contentVersion!==content)conflict('rail_definition_conflict');if(command.commandId!==key)throw new ActivityError(400,'rail_command_key_mismatch');
    const receipt=await new AuthorityCommandStore(c.env.DB).command(auth,command,revision,Date.now());response={accepted:true,transportVersion:authorityHttpVersion,status:'LOCAL_TEST_PROFILE',requestJournalRevision:revision,receipt};
   }
   const fresh=await resolveAuthContext(c);if('errorCode' in fresh)return c.json({accepted:false,errorCode:fresh.errorCode},fresh.status);
   return c.json(response);
  }catch(error){
   // Re-check after a denied commit: session/account revocation must retain its actual auth error.
   const fresh=await resolveAuthContext(c).catch(()=>null);if(fresh&&'errorCode' in fresh)return c.json({accepted:false,errorCode:fresh.errorCode},fresh.status);
   if(error instanceof ActivityError||error instanceof LocalRailError)return c.json({accepted:false,errorCode:error.code},error.status);
   if(error instanceof z.ZodError)return c.json({accepted:false,errorCode:'contract_violation'},400);
   const code=error instanceof Error?error.message:'';
   if(code==='rail_role_forbidden')return c.json({accepted:false,errorCode:code},403);
   if(['rail_definition_conflict','rail_command_key_conflict','rail_revision_conflict','rail_attempt_ended','rail_round_not_found','rail_round_not_open','rail_window_closed','rail_route_already_confirmed','rail_actor_unavailable','rail_internal_already_accepted'].includes(code))return c.json({accepted:false,errorCode:code},409);
   return c.json({accepted:false,errorCode:'authority_temporarily_unavailable'},503);
  }
 };
 app.get('/_local/world-boss/attempts/:attemptId/authority',handler(false));
 app.post('/_local/world-boss/attempts/:attemptId/authority/commands',handler(true));
 return app;
}
export type WorldBossAuthorityRead=Awaited<ReturnType<typeof readState>>;
export type WorldBossAuthorityCommand=Command;
