import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
import {WorldBossRailAttempt} from './world-boss-rail-state';
import {RailGeometryMotion,bindGeometryResult} from './world-boss-rail-geometry';
import {geometrySampleSchema} from './world-boss-rail-geometry-contract';
import {railAvailabilitySchema,railCommandSchema} from './world-boss-rail-contract';
import {LocalRailError} from './world-boss-rail-local-store';
export const journalVersion='world-boss.rail-journal.v1.20261002';
const integer=z.number().int().nonnegative().safe(),id=z.string().min(1).max(200);
const frameSchema=z.strictObject({observedAtMs:integer,validUntilMs:integer,availability:railAvailabilitySchema}).refine(f=>f.validUntilMs>f.observedAtMs);
type Frame=z.infer<typeof frameSchema>;
type Rail=ReturnType<WorldBossRailAttempt['snapshot']>;
type Geometry=ReturnType<RailGeometryMotion['snapshot']>;
type Receipt=Rail['receipts'][number]['receipt'];
type Binding=ReturnType<typeof bindGeometryResult>|null;
type Ack={version:typeof journalVersion;ownerRevision:number;sampleSequence:number};
type Entry={version:typeof journalVersion;ownerRevision:number;kind:'ATTACH'|'COMMAND'|'FRAME';key:string;requestDigest:string;previousHash:string|null;railHash:string;geometryHash:string;frameHash:string;receipt:Receipt|null;inheritedReceipts:Rail['receipts'];frame:Frame;ack:Ack;sample:Geometry['sample']|null;crossings:Geometry['events'];attack:Geometry['attackResult'];binding:Binding};
type Row={attempt_id:string;profile_status:string;owner_revision:number;rail_json:string;frame_json:string;journal_version:string|null;head_revision:number|null;head_rail:string|null;head_frame:string|null;geometry_json:string|null;binding_json:string|null;last_hash:string|null};
type Loaded={row:Row;a:WorldBossRailAttempt;frame:Frame;m:RailGeometryMotion;binding:Binding;entries:Entry[]};
const fail=(status:400|403|404|409|503,code:string):never=>{throw new LocalRailError(status,code);};
async function hash(value:unknown){const bytes=new TextEncoder().encode(canonicalJson(value as Parameters<typeof canonicalJson>[0]));return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',bytes)),b=>b.toString(16).padStart(2,'0')).join('');}
const json=(value:unknown)=>canonicalJson(value as Parameters<typeof canonicalJson>[0]);
/** LOCAL durable owner adapter, not an operational world movement owner. Only server code calls
 * attach/advance; their samples are TRUSTED_LOCAL_TRIAL, never client-authoritative telemetry.
 * Source rail row remains authoritative. Joined reads and writer fencing prevent a second truth.
 */
export class WorldBossRailJournal {
 constructor(private readonly db:D1Database){}
 private async source(attemptId:string){
  id.parse(attemptId);
  const row=await this.db.prepare(`SELECT r.*,h.journal_version,h.owner_revision AS head_revision,h.rail_json AS head_rail,h.frame_json AS head_frame,h.geometry_json,h.binding_json,h.last_hash FROM world_boss_local_rail_attempts r LEFT JOIN world_boss_rail_geometry_heads h ON h.attempt_id=r.attempt_id WHERE r.attempt_id=?`).bind(attemptId).first<Row>();
  if(!row)return fail(404,'rail_local_attempt_not_found');
  try{
   if(row.profile_status!=='LOCAL_TEST_PROFILE'||!Number.isSafeInteger(row.owner_revision)||row.owner_revision<1)throw new Error('owner invalid');
   const a=WorldBossRailAttempt.restore(JSON.parse(row.rail_json)),frame=frameSchema.parse(JSON.parse(row.frame_json)),s=a.snapshot();
   if(s.attemptId!==attemptId||s.now<frame.observedAtMs||frame.validUntilMs>s.definition.startedAt+180000)throw new Error('owner identity/frame invalid');
   return {row,a,frame};
  }catch{return fail(503,'rail_journal_state_invalid');}
 }
 private participant(a:WorldBossRailAttempt,accountId:string){const d=a.snapshot().definition;if(accountId!==d.trainAccountId&&accountId!==d.operatorAccountId)return fail(403,'rail_attempt_forbidden');}
 private coherence(a:WorldBossRailAttempt,frame:Frame,m:RailGeometryMotion){
  const s=a.snapshot(),g=m.snapshot(),cfg=s.definition.rounds.find(r=>r.roundId===g.profile.roundId),r=s.rounds.find(r=>r.roundId===g.profile.roundId);
  if(!cfg||!r||g.profile.geometryRef!==JSON.stringify([s.attemptId,g.profile.roundId])||g.profile.contentVersion!==s.definition.contentVersion||g.sample.atMs!==frame.observedAtMs||s.now!==frame.observedAtMs||g.selectedRoute!==r.route)throw new Error('rail_journal_geometry_owner_conflict');
  const receipt=s.receipts.find(x=>x.receipt.roundId===r.roundId&&x.receipt.internalAction===null)?.receipt;
  const selected=g.selectedAtSequence===null?null:g.samples.find(x=>x.sequence===g.selectedAtSequence);
  if((g.selectedRoute==='ENTRY'&&(receipt||selected))||(g.selectedRoute!=='ENTRY'&&(!receipt||!selected||selected.atMs!==receipt.acceptedAt||receipt.route!==g.selectedRoute)))throw new Error('rail_journal_receipt_sample_conflict');
  if(g.profile.attack.impactAtMs<s.definition.startedAt+cfg.lockAt||g.profile.attack.impactAtMs>s.definition.startedAt+cfg.resolveAt)throw new Error('rail_journal_attack_window_conflict');
  const result=s.results.find(x=>x.roundId===r.roundId);
  if(result&&!['CANCELLED','SKIPPED'].includes(result.outcome))return bindGeometryResult(s,g);
  return null;
 }
 private async load(attemptId:string):Promise<Loaded>{
  const {row,a,frame}=await this.source(attemptId);
  if(row.journal_version===null)return fail(503,'rail_journal_not_attached');
  try{
   if(row.journal_version!==journalVersion||row.head_revision!==row.owner_revision||row.head_rail!==row.rail_json||row.head_frame!==row.frame_json)throw new Error('owner detached');
   const m=RailGeometryMotion.restore(JSON.parse(row.geometry_json!)),binding=this.coherence(a,frame,m);
   if(json(binding)!==row.binding_json)throw new Error('binding invalid');
   // Immutable prefix restricted to the joined head revision: concurrent commits cannot tear reads.
   const logs=await this.db.prepare('SELECT * FROM world_boss_rail_geometry_journal WHERE attempt_id=? AND owner_revision<=? ORDER BY owner_revision').bind(attemptId,row.owner_revision).all<{owner_revision:number;entry_key:string;entry_json:string;entry_hash:string}>();
   const entries:Entry[]=[];let previous:string|null=null;
   for(const log of logs.results){const e=JSON.parse(log.entry_json) as Entry;
    if(e.version!==journalVersion||e.ownerRevision!==log.owner_revision||e.key!==log.entry_key||e.previousHash!==previous||log.entry_hash!==await hash(e)||json(e)!==log.entry_json||(entries.length===0?e.kind!=='ATTACH':e.ownerRevision!==entries.at(-1)!.ownerRevision+1)||e.ack?.ownerRevision!==e.ownerRevision||e.ack?.version!==journalVersion||e.frameHash!==await hash(frameSchema.parse(e.frame)))throw new Error('journal invalid');
    entries.push(e);previous=log.entry_hash;
   }
   const inherited=entries[0]?.inheritedReceipts;
   if(!Array.isArray(inherited)||entries.slice(1).some(e=>!Array.isArray(e.inheritedReceipts)||e.inheritedReceipts.length!==0)||inherited.some(record=>!a.snapshot().receipts.some(r=>json(r)===json(record))))throw new Error('inherited receipt invalid');
   for(const record of a.snapshot().receipts){if(!inherited.some(r=>json(r)===json(record))&&!entries.some(e=>e.kind==='COMMAND'&&e.key===json(['COMMAND',record.accountId,record.commandId])&&e.requestDigest===record.digest&&json(e.receipt)===json(record.receipt)))throw new Error('receipt journal missing');}
   const last=entries.at(-1);
   if(!last||last.ownerRevision!==row.owner_revision||previous!==row.last_hash||last.railHash!==await hash(a.snapshot())||last.geometryHash!==await hash(m.snapshot())||last.frameHash!==await hash(frame)||json(last.binding)!==json(binding))throw new Error('journal head invalid');
   return {row,a,frame,m,binding,entries};
  }catch{return fail(503,'rail_journal_state_invalid');}
 }
 async read(accountId:string,attemptId:string,contentVersion?:string){const s=await this.load(attemptId);this.participant(s.a,accountId);if(contentVersion!==undefined&&contentVersion!==s.a.snapshot().definition.contentVersion)return fail(409,'rail_definition_conflict');return {version:journalVersion,status:'LOCAL_DURABLE_TRIAL' as const,inputProvenance:'TRUSTED_LOCAL_TRIAL' as const,ownerRevision:s.row.owner_revision,frame:s.frame,rail:s.a.snapshot(),geometry:s.m.snapshot(),binding:s.binding,entries:s.entries};}
 private replay(s:Loaded,key:string,digest:string){const e=s.entries.find(e=>e.key===key);if(!e)return null;if(e.requestDigest!==digest)return fail(409,'rail_journal_key_conflict');return e;}
 private async entry(kind:Entry['kind'],key:string,requestDigest:string,revision:number,previousHash:string|null,a:WorldBossRailAttempt,frame:Frame,m:RailGeometryMotion,binding:Binding,receipt:Receipt|null,oldGeometry:Geometry|null):Promise<Entry>{
  const g=m.snapshot();return {version:journalVersion,ownerRevision:revision,kind,key,requestDigest,previousHash,railHash:await hash(a.snapshot()),geometryHash:await hash(g),frameHash:await hash(frame),receipt,inheritedReceipts:kind==='ATTACH'?a.snapshot().receipts:[],frame,ack:{version:journalVersion,ownerRevision:revision,sampleSequence:g.sample.sequence},sample:kind==='COMMAND'?null:g.sample,crossings:g.events.filter(e=>!oldGeometry?.events.some(old=>old.eventId===e.eventId)),attack:oldGeometry?.attackResult?null:g.attackResult,binding};
 }
 /** Attach only before this round has any unbound route receipt/result; no automatic reinvest/replay. */
 async attach(attemptId:string,expectedOwnerRevision:number,profile:unknown,initial:unknown){
  const {row,a,frame}=await this.source(attemptId),m=new RailGeometryMotion(profile,initial),digest=json({expectedOwnerRevision,profile:m.snapshot().profile,initial:m.snapshot().initial});
  if(row.journal_version!==null){const e=this.replay(await this.load(attemptId),'ATTACH',digest);if(e)return e.ack;return fail(409,'rail_journal_key_conflict');}
  if(row.owner_revision!==expectedOwnerRevision)return fail(409,'rail_local_owner_revision_conflict');
  const g=m.snapshot(),s=a.snapshot();
  if(s.receipts.some(x=>x.receipt.roundId===g.profile.roundId)||s.results.some(x=>x.roundId===g.profile.roundId))return fail(409,'rail_journal_attach_requires_uncommitted_round');
  if(g.sample.atMs>=g.profile.attack.impactAtMs)return fail(409,'rail_journal_attach_after_impact');
  const binding=this.coherence(a,frame,m),e=await this.entry('ATTACH','ATTACH',digest,row.owner_revision,null,a,frame,m,binding,null,null),h=await hash(e);
  try{await this.db.batch([
   this.guard(row,false),
   this.db.prepare('INSERT INTO world_boss_rail_geometry_heads VALUES(?,?,?,?,?,?,?,?)').bind(attemptId,journalVersion,row.owner_revision,row.rail_json,row.frame_json,json(g),json(binding),h),
   this.log(attemptId,e,h),this.db.prepare('DELETE FROM world_boss_rail_journal_guard'),
  ]);}catch(error){if((await this.source(attemptId)).row.journal_version!==null){const replay=this.replay(await this.load(attemptId),'ATTACH',digest);if(replay)return replay.ack;}if(String(error).includes('valid=1'))return fail(409,'rail_journal_state_conflict');throw error;}
  return e.ack;
 }
 private guard(row:Row,attached:boolean){return this.db.prepare(`INSERT INTO world_boss_rail_journal_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM world_boss_local_rail_attempts WHERE attempt_id=? AND profile_status='LOCAL_TEST_PROFILE' AND owner_revision=? AND rail_json=? AND frame_json=?) AND ${attached?'EXISTS(SELECT 1 FROM world_boss_rail_geometry_heads WHERE attempt_id=? AND owner_revision=? AND last_hash=? AND rail_json=? AND frame_json=? AND geometry_json=? AND binding_json=?)':'NOT EXISTS(SELECT 1 FROM world_boss_rail_geometry_heads WHERE attempt_id=?)'} THEN 1 ELSE 0 END`).bind(row.attempt_id,row.owner_revision,row.rail_json,row.frame_json,...(attached?[row.attempt_id,row.owner_revision,row.last_hash,row.head_rail,row.head_frame,row.geometry_json,row.binding_json]:[row.attempt_id]));}
 private log(attemptId:string,e:Entry,h:string){return this.db.prepare('INSERT INTO world_boss_rail_geometry_journal VALUES(?,?,?,?,?)').bind(attemptId,e.ownerRevision,e.key,json(e),h);}
 private async commit(s:Loaded,kind:'COMMAND'|'FRAME',key:string,digest:string,a:WorldBossRailAttempt,frame:Frame,m:RailGeometryMotion,receipt:Receipt|null){
  if(s.row.owner_revision>=Number.MAX_SAFE_INTEGER)return fail(503,'rail_local_revision_exhausted');
  const binding=this.coherence(a,frame,m),e=await this.entry(kind,key,digest,s.row.owner_revision+1,s.row.last_hash,a,frame,m,binding,receipt,s.m.snapshot()),h=await hash(e),rail=json(a.snapshot()),f=json(frame);
  await this.db.batch([
   this.guard(s.row,true),
   this.log(s.row.attempt_id,e,h),
   this.db.prepare('UPDATE world_boss_rail_geometry_heads SET owner_revision=?,rail_json=?,frame_json=?,geometry_json=?,binding_json=?,last_hash=? WHERE attempt_id=?').bind(e.ownerRevision,rail,f,json(m.snapshot()),json(binding),h,s.row.attempt_id),
   this.db.prepare('UPDATE world_boss_local_rail_attempts SET owner_revision=?,rail_json=?,frame_json=? WHERE attempt_id=?').bind(e.ownerRevision,rail,f,s.row.attempt_id),
   this.db.prepare('DELETE FROM world_boss_rail_journal_guard'),
  ]);return e;
 }
 async command(accountId:string,input:unknown,now:number){
  const c=railCommandSchema.parse(input);integer.parse(now);const s=await this.load(c.attemptId);this.participant(s.a,accountId);
  if(c.contentVersion!==s.a.snapshot().definition.contentVersion)return fail(409,'rail_definition_conflict');
  const key=json(['COMMAND',accountId,c.commandId]),digest=json(c),old=s.a.snapshot().receipts.find(x=>x.accountId===accountId&&x.commandId===c.commandId);
  if(old){if(old.digest!==digest)return fail(409,'rail_command_key_conflict');const e=this.replay(s,key,digest);if(e?json(e.receipt)!==json(old.receipt):!s.entries[0]!.inheritedReceipts.some(record=>json(record)===json(old)))return fail(503,'rail_journal_state_invalid');return structuredClone(old.receipt);}
  if(c.roundId!==s.m.snapshot().profile.roundId)return fail(503,'rail_journal_round_unavailable');
  if(now!==s.m.snapshot().sample.atMs)return fail(503,'rail_journal_sample_clock_required');
  if(now>=s.frame.validUntilMs)return fail(503,'rail_authority_frame_unavailable');
  const a=WorldBossRailAttempt.restore(s.a.snapshot()),m=RailGeometryMotion.restore(s.m.snapshot());
  const receipt=a.command(c,accountId,now,s.frame.availability);if(c.action.kind==='CONFIRM_ROUTE')m.selectRoute(c.action.side);
  try{await this.commit(s,'COMMAND',key,digest,a,s.frame,m,receipt);}catch(error){const latest=await this.load(c.attemptId);this.participant(latest.a,accountId);const e=this.replay(latest,key,digest);if(e?.receipt)return e.receipt;if(String(error).includes('valid=1'))return fail(409,'rail_journal_state_conflict');throw error;}
  return receipt;
 }
 /** Internal trial ingress. The actual operational movement owner must later call a separate adapter.
  * Identity/version/clock/consist come from persisted owner+profile, never a player DTO. */
 async advance(attemptId:string,expectedOwnerRevision:number,frameInput:unknown,sampleInput:unknown){
  const frame=frameSchema.parse(frameInput),sample=geometrySampleSchema.parse(sampleInput),s=await this.load(attemptId),key=json(['FRAME',sample.sequence]),digest=json({expectedOwnerRevision,frame,sample}),old=this.replay(s,key,digest);if(old)return old.ack;
  if(expectedOwnerRevision!==s.row.owner_revision)return fail(409,'rail_local_owner_revision_conflict');
  if(frame.observedAtMs!==sample.atMs)return fail(409,'rail_journal_frame_sample_conflict');
  if(frame.validUntilMs>s.a.snapshot().definition.startedAt+180000)return fail(409,'rail_local_frame_invalid');
  const a=WorldBossRailAttempt.restore(s.a.snapshot()),m=RailGeometryMotion.restore(s.m.snapshot()),g=m.snapshot();
  if(sample.sequence!==g.sample.sequence+1)return fail(409,'rail_journal_sample_sequence_conflict');
  if(sample.atMs>g.profile.attack.impactAtMs&&!g.attackResult)return fail(409,'rail_journal_impact_sample_required');
  m.accept(sample);if(sample.atMs===g.profile.attack.impactAtMs)m.observeAttack();a.advance(frame.observedAtMs,frame.availability);
  try{return (await this.commit(s,'FRAME',key,digest,a,frame,m,null)).ack;}catch(error){const latest=await this.load(attemptId),e=this.replay(latest,key,digest);if(e)return e.ack;if(String(error).includes('valid=1'))return fail(409,'rail_journal_state_conflict');throw error;}
 }
}