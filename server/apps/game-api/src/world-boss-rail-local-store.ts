import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
import {WorldBossRailAttempt} from './world-boss-rail-state';
import {railCommandSchema,railAvailabilitySchema,railVersion} from './world-boss-rail-contract';
const id=z.string().min(1).max(200),time=z.number().int().nonnegative().safe();
const frameSchema=z.strictObject({observedAtMs:time,validUntilMs:time,availability:railAvailabilitySchema}).refine(f=>f.validUntilMs>f.observedAtMs);
type Frame=z.infer<typeof frameSchema>;
type Row={attempt_id:string;profile_status:string;owner_revision:number;rail_json:string;frame_json:string};
export class LocalRailError extends Error {constructor(readonly status:400|403|404|409|503,readonly code:string){super(code);}}
const fail=(status:400|403|404|409|503,code:string):never=>{throw new LocalRailError(status,code);};
/** Internal LOCAL owner/repository. No public start, clock, frame or snapshot upload endpoint. */
export class WorldBossRailLocalStore {
 constructor(private readonly db:D1Database){}
 async provision(attemptId:string,definition:unknown,input:unknown){
  const a=new WorldBossRailAttempt(id.parse(attemptId),definition),frame=frameSchema.parse(input);
  if(frame.validUntilMs>a.snapshot().definition.startedAt+180000)throw new Error('rail_local_frame_invalid');
  a.advance(frame.observedAtMs,frame.availability);
  await this.db.prepare("INSERT INTO world_boss_local_rail_attempts VALUES(?,'LOCAL_TEST_PROFILE',1,?,?)").bind(attemptId,canonicalJson(a.snapshot()),canonicalJson(frame)).run();
 }
 private async load(attemptId:string){
  id.parse(attemptId);const row=await this.db.prepare('SELECT * FROM world_boss_local_rail_attempts WHERE attempt_id=?').bind(attemptId).first<Row>();
  if(!row)return fail(404,'rail_local_attempt_not_found');
  try{
   if(row.profile_status!=='LOCAL_TEST_PROFILE'||!Number.isSafeInteger(row.owner_revision)||row.owner_revision<=0)throw new Error('invalid owner');
   const a=WorldBossRailAttempt.restore(JSON.parse(row.rail_json)),frame=frameSchema.parse(JSON.parse(row.frame_json)),s=a.snapshot();
   if(s.attemptId!==row.attempt_id||s.now<frame.observedAtMs||frame.validUntilMs>s.definition.startedAt+180000)throw new Error('invalid identity/frame');
   return {row,a,frame};
  }catch{return fail(503,'rail_local_state_invalid');}
 }
 private participant(a:WorldBossRailAttempt,accountId:string){const d=a.snapshot().definition;if(accountId!==d.trainAccountId&&accountId!==d.operatorAccountId)return fail(403,'rail_attempt_forbidden');}
 private async commit(row:Row,a:WorldBossRailAttempt,frame:Frame){
  if(row.owner_revision>=Number.MAX_SAFE_INTEGER)return fail(503,'rail_local_revision_exhausted');
  await this.db.batch([
   this.db.prepare('INSERT INTO world_boss_local_rail_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM world_boss_local_rail_attempts WHERE attempt_id=? AND profile_status=? AND owner_revision=? AND rail_json=? AND frame_json=?) THEN 1 ELSE 0 END').bind(row.attempt_id,'LOCAL_TEST_PROFILE',row.owner_revision,row.rail_json,row.frame_json),
   this.db.prepare('UPDATE world_boss_local_rail_attempts SET owner_revision=?,rail_json=?,frame_json=? WHERE attempt_id=? AND owner_revision=?').bind(row.owner_revision+1,canonicalJson(a.snapshot()),canonicalJson(frame),row.attempt_id,row.owner_revision),
   this.db.prepare('DELETE FROM world_boss_local_rail_write_guard'),
  ]);
 }
 /** Only the trusted server owner may publish a frame; client requests never invoke this. */
 async publishFrame(attemptId:string,expectedOwnerRevision:number,input:unknown){
  const {row,a}=await this.load(attemptId),frame=frameSchema.parse(input);
  if(row.owner_revision!==expectedOwnerRevision)return fail(409,'rail_local_owner_revision_conflict');
  if(frame.validUntilMs>a.snapshot().definition.startedAt+180000)throw new Error('rail_local_frame_invalid');
  a.advance(frame.observedAtMs,frame.availability);await this.commit(row,a,frame);return {ownerRevision:row.owner_revision+1,snapshot:a.snapshot()};
 }
 async read(accountId:string,attemptId:string,contentVersion?:string){
  const {row,a,frame}=await this.load(attemptId);this.participant(a,accountId);
  if(contentVersion!==undefined&&contentVersion!==a.snapshot().definition.contentVersion)return fail(409,'rail_definition_conflict');
  return {version:railVersion,status:'LOCAL_TEST_PROFILE' as const,ownerRevision:row.owner_revision,authoritativeAtMs:frame.observedAtMs,frameValidUntilMs:frame.validUntilMs,snapshot:a.snapshot()};
 }
 async command(accountId:string,input:unknown,now:number){
  const c=railCommandSchema.parse(input);time.parse(now);const {row,a,frame}=await this.load(c.attemptId);this.participant(a,accountId);
  if(c.contentVersion!==a.snapshot().definition.contentVersion)return fail(409,'rail_definition_conflict');
  const replay=(attempt:WorldBossRailAttempt)=>{const r=attempt.snapshot().receipts.find(r=>r.accountId===accountId&&r.commandId===c.commandId);if(!r)return null;if(r.digest!==canonicalJson(c))return fail(409,'rail_command_key_conflict');return structuredClone(r.receipt);};
  const old=replay(a);if(old)return old;
  if(now>=a.snapshot().definition.startedAt+180000)return fail(409,'rail_attempt_ended');
  if(now<frame.observedAtMs||now>=frame.validUntilMs||now<a.snapshot().now)return fail(503,'rail_authority_frame_unavailable');
  const receipt=a.command(c,accountId,now,frame.availability);
  try{await this.commit(row,a,frame);}catch(error){
   const latest=await this.load(c.attemptId);this.participant(latest.a,accountId);const completed=replay(latest.a);if(completed)return completed;
   if(String(error).includes('valid=1'))return fail(409,'rail_local_state_conflict');throw error;
  }
  return receipt;
 }
}