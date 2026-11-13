import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {CombatHealthStore,combatHealthReceiptSchema} from './combat-health-store';
import {numericDefenseInputSchema,resolveNumericDefenseBudget} from './combat-numeric-defense';
import {WorldBossRailJournal} from './world-boss-rail-journal';
import {LocalRailError} from './world-boss-rail-local-store';
import {WorldBossRailShield,railShieldSnapshotSchema,personalLayerSchema} from './world-boss-rail-shield';
export const railHealthVersion='world-boss.rail-health.v1.20261002';
export const railHealthStartVersion='world-boss.rail-health-start.v1.20261002';
const natural=z.number().int().nonnegative().safe(),id=z.string().min(1).max(200),targetId=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const initial=z.array(z.strictObject({id:targetId,hp:natural,maxHp:natural.positive()}).refine(t=>t.hp<=t.maxHp)).length(1);
const startSchema=z.strictObject({version:z.literal(railHealthStartVersion),status:z.literal('LOCAL_TEST_PROFILE'),attemptId:id,contentVersion:id,healthTargets:initial});
export const railHealthProfileSchema=z.strictObject({version:z.literal(railHealthVersion),status:z.literal('LOCAL_TEST_PROFILE'),attemptId:id,contentVersion:id,roundId:id,targetId,attackRef:id,damage:numericDefenseInputSchema,shield:personalLayerSchema.nullable(),protectedUntilMs:natural});
const requestSchema=z.strictObject({version:z.literal(railHealthVersion),attemptId:id,contentVersion:id,roundId:id});
const receiptSchema=z.strictObject({version:z.literal(railHealthVersion),resolutionId:z.string(),attemptId:id,contentVersion:id,roundId:id,targetId,sourceJournalRevision:natural.positive(),sourceAttackEventId:z.string().nullable(),sourceIntentEventId:z.string().nullable(),resolvedAtMs:natural,reason:z.enum(['EVASION','HIT','TARGET_DOWN','PROTECTED','CANCELLED','SKIPPED']),calculation:z.unknown().nullable(),resolvedDamage:z.string().regex(/^(0|[1-9][0-9]*)$/u),shieldBefore:railShieldSnapshotSchema,shieldAfter:railShieldSnapshotSchema,absorbed:natural,hpBefore:natural,hpLoss:natural,hpAfter:natural,healthRevisionBefore:natural.positive(),healthRevisionAfter:natural.positive(),shieldRevisionBefore:natural.positive(),shieldRevisionAfter:natural.positive(),healthReceipt:combatHealthReceiptSchema.nullable()});
type Profile=z.infer<typeof railHealthProfileSchema>;
type Receipt=z.infer<typeof receiptSchema>;
type Source=Awaited<ReturnType<WorldBossRailJournal['read']>>;
type Row={attempt_id:string;run_id:string;account_id:string;profile_json:string;start_snapshot_json:string;shield_revision:number;shield_json:string;initial_shield_json:string;receipt_json:string|null;run_status:string;owner_account:string;snapshot_json:string;health_revision:number;state_json:string;actor_state_json:string|null;initial_hash:string;account_status:string};
const json=(input:unknown)=>canonicalJson(input as Parameters<typeof canonicalJson>[0]);
const fail=(status:400|403|404|409|503,code:string):never=>{throw new LocalRailError(status,code);};
/** Internal LOCAL producer/composition adapter. No public damage/HP setter or world START.
 * Actual existing combat_damage_states v2 remains the sole HP owner. Frozen trial input
 * supplies damage/effect/target mapping; operational effect eligibility is NOT connected. */
export class WorldBossRailHealthAdapter {
 constructor(private readonly db:D1Database){}
 private async owner(accountId:string,runId:string){const row=await this.db.prepare('SELECT r.account_id,r.status,s.snapshot_json,a.status AS account_status FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN accounts a ON a.id=r.account_id WHERE r.id=?').bind(runId).first<{account_id:string;status:string;snapshot_json:string;account_status:string}>();if(!row)return fail(404,'rail_health_run_missing');if(row.account_id!==accountId)return fail(403,'rail_health_owner_forbidden');return row;}
 private validateStart(raw:string,p:Profile){const s=startSchema.parse(JSON.parse(raw));if(s.attemptId!==p.attemptId||s.contentVersion!==p.contentVersion||s.healthTargets[0]!.id!==p.targetId)throw new Error('rail_health_start_conflict');return s;}
 private validateProfile(p:Profile,s:Source){const cfg=s.rail.definition.rounds.find(r=>r.roundId===p.roundId);if(!cfg||cfg.phase!==1||p.targetId!=='train'||cfg.targetRef!==p.targetId||cfg.railAttackRef!==p.attackRef||s.geometry.profile.roundId!==p.roundId||p.contentVersion!==s.rail.definition.contentVersion) return fail(409,'rail_health_profile_unsupported');if(p.shield&&p.shield.recipientId!==p.targetId)return fail(409,'rail_health_shield_target_conflict');}
 async attach(accountId:string,runId:string,raw:unknown){
  const p=railHealthProfileSchema.parse(raw),source=await new WorldBossRailJournal(this.db).read(accountId,p.attemptId,p.contentVersion);this.validateProfile(p,source);
  if(source.rail.definition.trainAccountId!==accountId)return fail(403,'rail_health_owner_forbidden');
  const owner=await this.owner(accountId,runId);if(owner.account_status!=='ACTIVE')return fail(403,'rail_health_owner_inactive');
  const start=this.validateStart(owner.snapshot_json,p),health=await new CombatHealthStore(this.db).read(accountId,runId);
  if(json(health.state.initialTargets)!==json(start.healthTargets))return fail(409,'rail_health_start_conflict');
  const old=await this.db.prepare('SELECT * FROM world_boss_rail_health_links WHERE attempt_id=?').bind(p.attemptId).first<Row>();
  if(old){if(old.run_id!==runId||old.account_id!==accountId||old.profile_json!==json(p)||old.start_snapshot_json!==owner.snapshot_json)return fail(409,'rail_health_attachment_conflict');return this.read(accountId,p.attemptId);}
  if(owner.status!=='ACTIVE'||source.geometry.sample.atMs>=source.geometry.profile.attack.impactAtMs)return fail(409,'rail_health_attach_window_closed');
  const sourceHeadHash=await sha256Hex(json(source.entries.at(-1)!)),initialHealthHash=await sha256Hex(json({version:health.state.version,initialTargets:health.state.initialTargets,startSnapshot:owner.snapshot_json}));
  const shield=WorldBossRailShield.create(p.targetId,source.geometry.sample.atMs,p.protectedUntilMs,p.shield).snapshot();
  try{await this.db.batch([
   this.db.prepare(`INSERT INTO world_boss_rail_health_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots n ON n.run_id=r.id JOIN combat_damage_states d ON d.run_id=r.id JOIN world_boss_rail_geometry_heads g ON g.attempt_id=? JOIN world_boss_local_rail_attempts rail ON rail.attempt_id=g.attempt_id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND n.snapshot_json=? AND d.start_snapshot_json=? AND d.revision=? AND d.state_json=? AND d.initial_hash=? AND d.actor_state_json IS NULL AND g.owner_revision=? AND g.rail_json=? AND g.geometry_json=? AND g.frame_json=? AND g.binding_json=? AND g.journal_version=? AND g.last_hash=? AND rail.owner_revision=g.owner_revision AND rail.rail_json=g.rail_json AND rail.frame_json=g.frame_json) AND NOT EXISTS(SELECT 1 FROM world_boss_rail_health_links WHERE attempt_id=? OR run_id=?) THEN 1 ELSE 0 END`).bind(p.attemptId,runId,accountId,owner.snapshot_json,owner.snapshot_json,health.revision,json(health.state),initialHealthHash,source.ownerRevision,json(source.rail),json(source.geometry),json(source.frame),json(source.binding),source.version,sourceHeadHash,p.attemptId,runId),
   this.db.prepare('INSERT INTO world_boss_rail_health_links VALUES(?,?,?,?,?,1,?,?)').bind(p.attemptId,runId,accountId,json(p),owner.snapshot_json,json(shield),json(shield)),
   this.db.prepare('DELETE FROM world_boss_rail_health_guard'),
  ]);}catch(error){const raced=await this.db.prepare('SELECT * FROM world_boss_rail_health_links WHERE attempt_id=?').bind(p.attemptId).first<Row>();if(raced){if(raced.run_id!==runId||raced.account_id!==accountId||raced.profile_json!==json(p)||raced.start_snapshot_json!==owner.snapshot_json)return fail(409,'rail_health_attachment_conflict');return this.read(accountId,p.attemptId);}if(/valid=1|UNIQUE constraint failed/iu.test(String(error)))return fail(409,'rail_health_state_conflict');throw error;}return this.read(accountId,p.attemptId);
 }
 private async load(accountId:string,attemptId:string){
  const source=await new WorldBossRailJournal(this.db).read(accountId,attemptId);
  const row=await this.db.prepare(`SELECT l.*,r.status AS run_status,r.account_id AS owner_account,n.snapshot_json,d.revision AS health_revision,d.state_json,d.actor_state_json,d.initial_hash,a.status AS account_status,w.response_json AS receipt_json FROM world_boss_rail_health_links l JOIN runs r ON r.id=l.run_id JOIN run_snapshots n ON n.run_id=r.id JOIN combat_damage_states d ON d.run_id=r.id JOIN accounts a ON a.id=r.account_id LEFT JOIN world_boss_rail_health_receipts w ON w.attempt_id=l.attempt_id AND w.round_id=json_extract(l.profile_json,'$.roundId') WHERE l.attempt_id=?`).bind(attemptId).first<Row>();
  if(!row)return fail(503,'rail_health_link_missing');
  if(row.account_status!=='ACTIVE')return fail(403,'rail_health_owner_inactive');
  try{
   const p=railHealthProfileSchema.parse(JSON.parse(row.profile_json));this.validateProfile(p,source);const start=this.validateStart(row.start_snapshot_json,p);
   if(p.attemptId!==attemptId||row.snapshot_json!==row.start_snapshot_json||row.owner_account!==row.account_id||row.account_id!==source.rail.definition.trainAccountId||!Number.isSafeInteger(row.shield_revision)||row.shield_revision<1)throw new Error('link identity invalid');
   const shield=WorldBossRailShield.restore(JSON.parse(row.shield_json));if(shield.snapshot().targetId!==p.targetId)throw new Error('shield target invalid');
   const health=await new CombatHealthStore(this.db).read(row.account_id,row.run_id);
   if(health.revision!==row.health_revision||json(health.state)!==row.state_json)return fail(409,'rail_health_state_conflict');
   if(json(health.state.initialTargets)!==json(start.healthTargets))throw new Error('health initial invalid');
   const target=health.state.targets.find(t=>t.id===p.targetId);if(!target)throw new Error('target absent');
   const initialShield=WorldBossRailShield.restore(JSON.parse(row.initial_shield_json));
   const seed=initialShield.snapshot();if(seed.targetId!==p.targetId||json(seed.layer)!==json(p.shield)||seed.protectedUntilMs!==p.protectedUntilMs||seed.lastAtMs<source.geometry.initial.atMs||seed.lastAtMs>=source.geometry.profile.attack.impactAtMs)throw new Error('shield seed invalid');
   if(row.receipt_json===null){if(row.shield_revision!==1||json(shield.snapshot())!==json(seed))throw new Error('unbound shield state');}
   else{const receipt=receiptSchema.parse(JSON.parse(row.receipt_json)),origin=this.resolution(source,p.roundId);
    if(json(receipt)!==row.receipt_json||receipt.sourceJournalRevision!==origin.entry.ownerRevision||receipt.sourceAttackEventId!==(origin.attack?.eventId??null)||receipt.sourceIntentEventId!==(origin.intent?.eventId??null)||receipt.resolvedAtMs!==origin.result.resolvedAt)throw new Error('receipt source detached');
    if(receipt.attemptId!==attemptId||receipt.targetId!==p.targetId||receipt.contentVersion!==p.contentVersion||receipt.roundId!==p.roundId||receipt.shieldRevisionBefore!==1||receipt.shieldRevisionAfter!==row.shield_revision||json(receipt.shieldBefore)!==json(seed)||json(receipt.shieldAfter)!==json(shield.snapshot())||receipt.hpBefore-receipt.hpLoss!==receipt.hpAfter)throw new Error('shield receipt mismatch');
    if(receipt.healthReceipt){const hr=receipt.healthReceipt;if(hr.before.hp!==receipt.hpBefore||hr.after.hp!==receipt.hpAfter||hr.targetId!==p.targetId||hr.change.kind!=='ACTUAL_HP'||hr.change.reason!=='DAMAGE'||hr.change.hp!==receipt.hpAfter||receipt.healthRevisionAfter!==hr.revision||receipt.healthRevisionBefore+1!==hr.revision||!health.state.ledger.some(e=>e.eventId===hr.eventId&&json(e.before)===json(hr.before)&&json(e.after)===json(hr.after)&&json(e.change)===json(hr.change)))throw new Error('health receipt detached');}
    else if(receipt.hpLoss!==0||receipt.healthRevisionBefore!==receipt.healthRevisionAfter)throw new Error('missing HP receipt');
    const restored=WorldBossRailShield.restore(seed),hit=['HIT','PROTECTED'].includes(receipt.reason)?restored.applyResolved(receipt.resolvedDamage,receipt.resolvedAtMs,receipt.hpBefore):{absorbed:0,hpLoss:0,hpAfter:receipt.hpBefore};
    if(hit.absorbed!==receipt.absorbed||hit.hpLoss!==receipt.hpLoss||hit.hpAfter!==receipt.hpAfter||json(restored.snapshot())!==json(receipt.shieldAfter))throw new Error('shield resolution mismatch');
   }
   return {row,p,source,sourceHeadHash:await sha256Hex(json(source.entries.at(-1)!)),shield,health,target};
  }catch(error){if(error instanceof LocalRailError)throw error;return fail(503,'rail_health_storage_invalid');}
 }
 async read(accountId:string,attemptId:string){const s=await this.load(accountId,attemptId);return {version:railHealthVersion,status:'LOCAL_TEST_PROFILE' as const,inputProvenance:'TRUSTED_LOCAL_TRIAL' as const,runId:s.row.run_id,healthRevision:s.health.revision,health:s.target.health,shieldRevision:s.row.shield_revision,shield:s.shield.snapshot(),processingStatus:s.row.receipt_json?'APPLIED' as const:s.source.rail.results.some(r=>r.roundId===s.p.roundId)?'PENDING_HEALTH' as const:'PENDING_SOURCE' as const,receipt:s.row.receipt_json?receiptSchema.parse(JSON.parse(s.row.receipt_json)):null};}
 private guard(s:Awaited<ReturnType<WorldBossRailHealthAdapter['load']>>,sourceRevision:number,sourceEntry:string){return this.db.prepare(`INSERT INTO world_boss_rail_health_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM world_boss_rail_health_links l JOIN runs r ON r.id=l.run_id JOIN accounts a ON a.id=r.account_id JOIN run_snapshots n ON n.run_id=r.id JOIN combat_damage_states d ON d.run_id=r.id JOIN world_boss_rail_geometry_heads g ON g.attempt_id=l.attempt_id JOIN world_boss_local_rail_attempts rail ON rail.attempt_id=l.attempt_id WHERE l.attempt_id=? AND l.run_id=? AND l.account_id=? AND l.profile_json=? AND l.start_snapshot_json=? AND l.shield_revision=? AND l.shield_json=? AND r.status='ACTIVE' AND r.account_id=l.account_id AND a.status='ACTIVE' AND n.snapshot_json=l.start_snapshot_json AND d.start_snapshot_json=l.start_snapshot_json AND d.revision=? AND d.state_json=? AND d.initial_hash=? AND d.actor_state_json IS NULL AND g.owner_revision=? AND rail.owner_revision=g.owner_revision AND rail.rail_json=g.rail_json AND rail.frame_json=g.frame_json AND g.rail_json=? AND g.frame_json=? AND g.geometry_json=? AND g.binding_json=? AND g.last_hash=? AND g.journal_version=?) AND EXISTS(SELECT 1 FROM world_boss_rail_geometry_journal WHERE attempt_id=? AND owner_revision=? AND entry_json=?) THEN 1 ELSE 0 END`).bind(s.row.attempt_id,s.row.run_id,s.row.account_id,s.row.profile_json,s.row.start_snapshot_json,s.row.shield_revision,s.row.shield_json,s.health.revision,s.row.state_json,s.row.initial_hash,s.source.ownerRevision,json(s.source.rail),json(s.source.frame),json(s.source.geometry),json(s.source.binding),s.sourceHeadHash,s.source.version,s.row.attempt_id,sourceRevision,sourceEntry);}
 private resolution(source:Source,roundId:string){
  const result=source.rail.results.find(r=>r.roundId===roundId);if(!result)return fail(503,'rail_health_result_unavailable');
  const aborted=['CANCELLED','SKIPPED'].includes(result.outcome),entry=source.entries.find(e=>aborted?e.sample?.atMs===result.resolvedAt:e.binding?.result.roundId===roundId),attack=aborted?null:source.geometry.attackResult;
  if(!entry||(!aborted&&(!source.binding||!attack)))return fail(503,'rail_health_result_unavailable');
  return {result,entry,attack,intent:result.failureAttacks[0]??null,aborted};
 }
 async apply(accountId:string,raw:unknown):Promise<Receipt>{
  const c=requestSchema.parse(raw),s=await this.load(accountId,c.attemptId);
  if(accountId!==s.row.account_id)return fail(403,'rail_health_owner_forbidden');
  if(c.contentVersion!==s.p.contentVersion||c.roundId!==s.p.roundId)return fail(409,'rail_health_definition_conflict');
  const {result,entry,attack,intent,aborted}=this.resolution(s.source,c.roundId);

  if(result.failureAttacks.length>1||intent&&(intent.attackRef!==s.p.attackRef||intent.targetRef!==s.p.targetId||intent.reason!=='RAIL_FAILURE'||intent.carriageTransfer||intent.penalty||intent.defensePipeline!=='SUPPORTED_DEF_REDUCTION_SHIELD'))return fail(409,'rail_health_intent_unsupported');
  const immutableSource={result,attack,intent,sourceJournalRevision:entry.ownerRevision},requestHash=await sha256Hex(json({command:c,runId:s.row.run_id,profile:s.p,start:s.row.start_snapshot_json,source:immutableSource}));
  const replay=async()=>{const original=await this.db.prepare('SELECT request_hash,response_json FROM world_boss_rail_health_receipts WHERE attempt_id=? AND round_id=?').bind(c.attemptId,c.roundId).first<{request_hash:string;response_json:string}>();if(!original)return null;if(original.request_hash!==requestHash)return fail(409,'rail_health_event_conflict');try{const r=receiptSchema.parse(JSON.parse(original.response_json));if(json(r)!==original.response_json||r.attemptId!==c.attemptId||r.contentVersion!==c.contentVersion||r.roundId!==c.roundId||r.targetId!==s.p.targetId||r.sourceJournalRevision!==entry.ownerRevision||r.sourceIntentEventId!==(intent?.eventId??null)||r.sourceAttackEventId!==(attack?.eventId??null)||r.resolvedAtMs!==result.resolvedAt||r.hpBefore-r.hpLoss!==r.hpAfter||r.shieldRevisionAfter!==r.shieldRevisionBefore+1)throw new Error('receipt source invalid');return r;}catch{return fail(503,'rail_health_receipt_invalid');}};
  const old=await replay();if(old)return old;
  if(s.row.run_status!=='ACTIVE')return fail(409,'rail_health_run_not_active');
  if(s.row.shield_revision>=Number.MAX_SAFE_INTEGER)return fail(503,'rail_health_revision_exhausted');
  const hp=s.target.health.hp,before=s.shield.snapshot(),protectedNow=result.resolvedAt<before.protectedUntilMs;
  const reason=aborted?result.outcome as 'CANCELLED'|'SKIPPED':hp===0?'TARGET_DOWN':!intent?'EVASION':protectedNow?'PROTECTED':'HIT';
  const damage=structuredClone(s.p.damage);
  // EQ06 uses the authoritative pre-hit HP condition once. Trial independent groups are pinned.
  if(BigInt(hp)*2n>BigInt(s.target.health.maxHp))damage.reductions.equipment=[];
  const calculation=reason==='HIT'?resolveNumericDefenseBudget(damage):null,resolvedDamage=calculation?.ordinaryBeforeShield??'0',shield=WorldBossRailShield.restore(before);
  const applied=['EVASION','TARGET_DOWN','CANCELLED','SKIPPED'].includes(reason)?{absorbed:0,hpLoss:0,hpAfter:hp}:shield.applyResolved(resolvedDamage,result.resolvedAt,hp);
  const eventId='wb:'+await sha256Hex(json([railHealthVersion,c.attemptId,c.roundId,intent?.eventId??'EVASION']));
  const command={eventId,expectedRevision:s.health.revision,targetId:s.p.targetId,change:{kind:'ACTUAL_HP' as const,reason:'DAMAGE' as const,hp:applied.hpAfter}};
  const assertion=()=>this.guard(s,entry.ownerRevision,json(entry));
  const health=new CombatHealthStore(this.db,{prepare:async context=>{if(context.kind!=='CHANGE'||context.accountId!==s.row.account_id||context.runId!==s.row.run_id||context.startSnapshotJson!==s.row.start_snapshot_json||json(context.command)!==json(command)||context.batchBaseRevision!==s.health.revision) return fail(409,'rail_health_proof_invalid');return {assertions:[assertion()]};}});
  const prepared=reason==='HIT'?await health.prepareChange(s.row.account_id,s.row.run_id,command):null;
  if(prepared&&prepared.statements.length===0)return fail(503,'rail_health_partial_history');
  const receipt=receiptSchema.parse({version:railHealthVersion,resolutionId:json([c.attemptId,c.roundId,'HEALTH']),attemptId:c.attemptId,contentVersion:c.contentVersion,roundId:c.roundId,targetId:s.p.targetId,sourceJournalRevision:entry.ownerRevision,sourceAttackEventId:attack?.eventId??null,sourceIntentEventId:intent?.eventId??null,resolvedAtMs:result.resolvedAt,reason,calculation,resolvedDamage,shieldBefore:before,shieldAfter:shield.snapshot(),absorbed:applied.absorbed,hpBefore:hp,hpLoss:applied.hpLoss,hpAfter:applied.hpAfter,healthRevisionBefore:s.health.revision,healthRevisionAfter:prepared?.receipt.revision??s.health.revision,shieldRevisionBefore:s.row.shield_revision,shieldRevisionAfter:s.row.shield_revision+1,healthReceipt:prepared?.receipt??null});
  try{await this.db.batch([
   assertion(),...(prepared?.statements??[]),
   this.db.prepare('UPDATE world_boss_rail_health_links SET shield_revision=?,shield_json=? WHERE attempt_id=?').bind(receipt.shieldRevisionAfter,json(receipt.shieldAfter),c.attemptId),
   this.db.prepare('INSERT INTO world_boss_rail_health_receipts VALUES(?,?,?,?)').bind(c.attemptId,c.roundId,requestHash,json(receipt)),
   this.db.prepare('DELETE FROM world_boss_rail_health_guard'),
  ]);}catch(error){const completed=await replay();if(completed)return completed;if(/valid=1|UNIQUE constraint failed/iu.test(String(error)))return fail(409,'rail_health_state_conflict');throw error;}
  return receipt;
 }
}