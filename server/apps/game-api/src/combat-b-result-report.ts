import {verifyBResultDeparture} from './combat-b-result-departure';
import {bResultV2BodyLimit,bResultBodyLimit} from './combat-b-result-budget';
export {bResultV2BodyLimit,bResultBodyLimit,bResultV2BudgetVersion} from './combat-b-result-budget';
import {bOriginalJournalSchema,verifyBOriginalJournal,type BJournalVerification} from './combat-b-original-journal';
import {prepareBResultStorage,recoverBResultStorage} from './combat-b-result-storage';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readBNativeNodeSource} from './combat-b-native-node-source';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
export const bResultReportVersion='combat.b-result-report.v1.20261006';
export const bResultReportV2Version='combat.b-result-report.v2.20261006';
export const bResultReceiptV2Version='combat.b-result-consistency-receipt.v2.20261006';

export const bResultReceiptVersion='combat.b-result-consistency-receipt.v1.20261006';

export class BResultBodyError extends Error{readonly status=413;readonly code='combat_b_result_body_too_large';constructor(){super('combat_b_result_body_too_large');}}
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),rev=nat.min(1),hash=z.string().regex(/^[a-f0-9]{64}$/u),digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const json=(x:unknown)=>canonicalJson(z.json().parse(x));
const fail=(s:string):never=>{throw new ActivityError(409,'combat_b_result_'+s);};
const need=(v:boolean,s:string)=>{if(!v)fail(s);};
const command=z.object({kind:z.enum(['ADVANCE','WAVE_BASIC','ORDINARY_MOVE','PAUSE']),eventId:id}).passthrough();
const waveEvent=z.strictObject({kind:z.literal('WAVE_CLEAR'),waveOrdinal:rev,tick:nat,sequence:rev});
const vitals=z.object({hp:nat,maxHp:rev}).passthrough();
const runtime=z.object({version:z.literal('unity.combat-b.travel-native-local.v1.20261005'),bindingHash:digest,capacity:rev.max(10000),sequence:rev,commands:z.array(command).min(1).max(10000),results:z.array(z.object({tick:nat,outcome:z.enum(['RUNNING','VICTORY','DEFEAT']),hits:z.array(z.json())}).passthrough()).min(1).max(10000),waveSourceHash:hash,
 state:z.object({direct:z.object({clock:z.object({stepTicks:rev,debtHalfTicks:nat,completedSteps:nat,speedHalfUnits:z.literal(2),paused:z.boolean()}).passthrough(),enemies:z.record(z.string(),vitals),allies:z.object({definition:z.object({formation:z.object({trainId:id,mode:z.literal('Solo'),battlefield:z.literal('ExternalVertical'),slots:z.array(z.strictObject({carriageId:id,operatorId:id.nullable()})).min(1).max(5)}).passthrough(),hpBindings:z.array(z.strictObject({targetId:id,maxHp:rev})).min(1).max(11)}).passthrough(),vitals:z.record(z.string(),vitals)}).passthrough()}).passthrough()}).passthrough(),
 waveEntry:z.object({waveOrdinal:z.literal(2),cursor:nat,started:z.literal(true),finished:z.literal(true),unknownProjectile:z.literal(false),events:z.array(waveEvent).length(2)}).passthrough(),
 boarding:z.object({lives:z.array(z.object({id,phase:z.literal('Dead'),registered:z.literal(false),targetable:z.literal(false),attackEligible:z.literal(false)}).passthrough()).min(1).max(32)}).passthrough(),
 gunnerIntercept:z.object({terminal:z.literal(true),projectiles:z.array(z.object({id,terminal:z.enum(['HIT','INTERCEPTED'])}).passthrough()).max(10000),interception:z.object({terminal:z.literal(true)}).passthrough()}).passthrough(),
 travel:z.object({nativeContextId:id,nodeId:id,nodeSourceHash:hash,commandRevision:rev,sequence:rev,stopped:z.literal(true),stopReason:z.literal('TERMINAL'),stepTicks:rev,motion:z.object({currentTick:nat,completedWaves:z.literal(2)}).passthrough()}).passthrough()
}).passthrough();
const v1Report=z.strictObject({version:z.literal(bResultReportVersion),requestId:id,reportOriginMapRevision:rev,checkpointHash:digest,nodeSession:z.object({version:z.literal('unity.combat-b.node-controller-local.v1.20261005'),accountId:id,runId:id,localRevision:nat,node:z.json(),runtime}).passthrough()});
const v2Report=v1Report.extend({version:z.literal(bResultReportV2Version),nodeSession:v1Report.shape.nodeSession.extend({runtime:runtime.extend({version:z.literal('unity.combat-b.travel-native-local.v2.20261006'),originalEvents:bOriginalJournalSchema})})});
export const bResultReportSchema=z.discriminatedUnion('version',[v1Report,v2Report]).superRefine((b,c)=>{if(b.version===bResultReportVersion&&'originalEvents' in b.nodeSession.runtime)c.addIssue({code:'custom',message:'v1 journal retrofill is unsupported'});});
export type BResultReceipt={version:typeof bResultReceiptVersion|typeof bResultReceiptV2Version;originalJournal?:BJournalVerification;finalCommandId?:string;evidenceRef:string;requestId:string;reportOriginMapRevision:number;originalEventId:string;commandRevision:number;nativeSequence:number;checkpointHash:string;completedWaves:number;verificationLevel:'STRUCTURE_SOURCE_RESULT_CONSISTENCY';combatTruth:'NOT_PROVEN';fullNativeReplayPerformed:false;arrivalAccepted:false;progressAuthorized:boolean;missingSources:string[];rewardsAuthorized:false};
type Row={account_id:string;request_hash:string;report_json:string;receipt_json:string;origin_state_json:string;origin_revision:number};
/** Server validates the approved solo RESULT consistency boundary. No attack simulation,
 * damage truth proof, anti-cheat policy, automatic arrival or reward settlement. */
export class BResultReportStore{
 constructor(private readonly db:D1Database){}
 private row(runId:string,ref:string){return this.db.prepare('SELECT account_id,request_hash,report_json,receipt_json,origin_state_json,origin_revision FROM b_result_reports WHERE run_id=? AND evidence_ref=?').bind(runId,ref).first<Row>();}
 private async access(accountId:string,runId:string){id.parse(accountId);id.parse(runId);const x=await readOriginalBRouteSource(this.db,accountId,runId);need(x.row.preview_account===accountId,'account_conflict');return x;}
 async read(accountId:string,runId:string,ref:string){id.parse(ref);await this.access(accountId,runId);const row=await this.row(runId,ref);if(!row)return null;need(row.account_id===accountId,'account_conflict');const recovered=await recoverBResultStorage(this.db,runId,ref,row.report_json,row.request_hash);return {receipt:JSON.parse(row.receipt_json) as BResultReceipt,report:bResultReportSchema.parse(recovered.report),storage:recovered.storage};}
 async submit(accountId:string,runId:string,input:unknown):Promise<BResultReceipt>{
 const b=bResultReportSchema.parse(input),text=json(b);if(new TextEncoder().encode(text).length>(b.version===bResultReportV2Version?bResultV2BodyLimit:bResultBodyLimit))throw new BResultBodyError();
 const x=await this.access(accountId,runId),requestHash=await sha256Hex(text);
 const replay=async()=>{const old=await this.db.prepare('SELECT request_hash,receipt_json,report_json,evidence_ref FROM b_result_reports WHERE run_id=? AND request_id=?').bind(runId,b.requestId).first<{request_hash:string;receipt_json:string;report_json:string;evidence_ref:string}>();if(!old)return null;need(old.request_hash===requestHash,'request_conflict');await recoverBResultStorage(this.db,runId,old.evidence_ref,old.report_json,old.request_hash);return JSON.parse(old.receipt_json) as BResultReceipt;};const old=await replay();if(old)return old;
 need(x.row.status==='ACTIVE'&&x.row.revision===b.reportOriginMapRevision,'state_conflict');
 need(x.state.phase==='TRAVELLING'||x.state.phase==='ACTIVE','phase_conflict');
 const p=await readBNativeNodeSource(this.db,accountId,runId),n=b.nodeSession,r=n.runtime,t=r.travel,last=r.results.at(-1)!;
 need(n.accountId===accountId&&n.runId===runId,'account_conflict');
 need(json(n.node)===json(p.originalNodeSource)&&r.waveSourceHash===p.originalNodeSource.sourceHash&&t.nativeContextId===p.nativeContextId&&t.nodeId===p.nodeId&&t.nodeSourceHash===r.waveSourceHash,'source_conflict');
 need(b.checkpointHash==='sha256:'+await sha256Hex(json(r)),'checkpoint_hash_conflict');
 need(r.commands.length===r.results.length&&t.commandRevision===r.commands.length&&t.sequence===r.sequence&&t.stepTicks===r.state.direct.clock.stepTicks,'cursor_conflict');
 need(new Set(r.commands.map(c=>c.eventId)).size===r.commands.length,'command_duplicate');
 const ids=new Set(p.originalNodeSource.waves.flatMap(w=>w.instances.map(i=>i.instanceId))),f=r.state.direct.allies.definition.formation;
 need(new Set(f.slots.map(s=>s.carriageId)).size===f.slots.length&&new Set(f.slots.filter(s=>s.operatorId).map(s=>s.operatorId)).size===f.slots.filter(s=>s.operatorId).length,'formation_duplicate');
 const allies=new Set([f.trainId,...f.slots.flatMap(s=>[s.carriageId,...s.operatorId?[s.operatorId]:[]])]);
 need(Object.keys(r.state.direct.enemies).length===ids.size&&Object.keys(r.state.direct.enemies).every(e=>ids.has(e))&&p.originalNodeSource.waves.flatMap(w=>w.instances).every(i=>r.state.direct.enemies[i.instanceId]?.hp===0&&r.state.direct.enemies[i.instanceId]?.maxHp===Number(i.hp)),'enemy_completion_conflict');
 need(r.boarding.lives.length===ids.size&&new Set(r.boarding.lives.map(e=>e.id)).size===ids.size&&r.boarding.lives.every(e=>ids.has(e.id)),'boarding_completion_conflict');
 need(Object.keys(r.state.direct.allies.vitals).length===allies.size&&Object.entries(r.state.direct.allies.vitals).every(([key,v])=>allies.has(key)&&v.hp<=v.maxHp)&&r.state.direct.allies.vitals[f.trainId]!.hp>0,'ally_vitals_conflict');
 need(new Set(r.state.direct.allies.definition.hpBindings.map(h=>h.targetId)).size===allies.size&&r.state.direct.allies.definition.hpBindings.every(h=>allies.has(h.targetId)&&r.state.direct.allies.vitals[h.targetId]?.maxHp===h.maxHp),'hp_binding_conflict');
 need(new Set(r.gunnerIntercept.projectiles.map(e=>e.id)).size===r.gunnerIntercept.projectiles.length,'projectile_duplicate');
 let previousTick=0,paused=false,terminal=false,remainingBudget=0n;
 const operators=new Set(f.slots.flatMap(s=>s.operatorId?[s.operatorId]:[])),step=BigInt(r.state.direct.clock.stepTicks);
 for(let i=0;i<r.commands.length;i++){
  const c=r.commands[i]!,result=r.results[i]!,delta=result.tick-previousTick;
  need(delta>=0&&result.tick<=last.tick,'time_order_conflict');
  if(terminal)need((c.kind==='ADVANCE'||c.kind==='PAUSE')&&delta===0&&result.outcome==='VICTORY','terminal_command_conflict');
  if(c.kind==='ADVANCE'){
   const a=z.object({elapsed:nat,maximumSteps:nat.max(10000)}).parse(c);
   if(!paused&&!terminal)remainingBudget+=BigInt(a.elapsed);
   need(delta<=a.maximumSteps&&(!paused||delta===0)&&BigInt(delta)*step<=remainingBudget,'command_clock_conflict');
   remainingBudget-=BigInt(delta)*step;
  }else{
   need(delta===0,'command_clock_conflict');
   if(c.kind==='PAUSE')paused=z.object({value:z.boolean()}).parse(c).value;
   else if(c.kind==='ORDINARY_MOVE'){need(!paused,'paused_movement');const a=z.object({actorId:id,slot:nat.max(4),pointMm:z.array(z.number().int().safe()).length(3),geometryHash:digest}).parse(c);need(f.slots[a.slot]?.operatorId===a.actorId,'command_actor_conflict');}
   else{const a=z.object({sourceId:id,targetId:id,targetGeneration:rev}).parse(c);need(operators.has(a.sourceId)&&ids.has(a.targetId),'command_actor_conflict');}
  }
  need(result.outcome!=='DEFEAT','result_conflict');terminal=result.outcome==='VICTORY';previousTick=result.tick;
 }
 need(last.outcome==='VICTORY'&&r.state.direct.clock.completedSteps===last.tick&&t.motion.currentTick===last.tick&&r.state.direct.clock.paused===paused&&BigInt(r.state.direct.clock.debtHalfTicks)<=remainingBudget*2n,'clock_result_conflict');
 const registry=z.object({hazards:z.object({owners:z.array(z.object({owner:id,pending:z.boolean()}).passthrough()).max(32),unknown:z.array(z.json()).max(32)}).passthrough()}).passthrough().safeParse(n.observation);
 const owners=['WAVE_ENTRY','ENEMY_ACTION','ENEMY_ADMISSION','GUNNER_PROJECTILE','INTERCEPTION'];
 need(registry.success&&registry.data.hazards.unknown.length===0&&registry.data.hazards.owners.length===owners.length&&new Set(registry.data.hazards.owners.map(o=>o.owner)).size===owners.length&&registry.data.hazards.owners.every(o=>owners.includes(o.owner)&&!o.pending),'hazard_registry_conflict');
 need(r.waveEntry.cursor===p.originalNodeSource.waves[1]!.instances.length,'planned_entry_conflict');
 const waves=r.waveEntry.events;need(waves.every((w,i)=>w.waveOrdinal===i+1&&w.sequence<=r.sequence&&w.tick<=last.tick)&&waves[0]!.sequence<waves[1]!.sequence&&waves[0]!.tick<waves[1]!.tick&&waves[1]!.tick===last.tick,'wave_order_conflict');
 need((waves[1]!.tick-waves[0]!.tick)*r.state.direct.clock.stepTicks>=20000000,'wave_gap_conflict');
 const journal=b.version===bResultReportV2Version?await verifyBOriginalJournal(b.nodeSession.runtime.originalEvents,r,p.originalNodeSource):undefined;
 const persisted=await prepareBResultStorage(b,text);if(new TextEncoder().encode(persisted.reportJson).length>bResultBodyLimit)throw new BResultBodyError();
 const missingSources=await verifyBResultDeparture(x.row.snapshot_json,{accountId,runId},f);
 // A client native bindingHash identifies reported bytes; it is not a combat
 // truth attestation. Progress uses the original snapshot selection and server rules.
 const originalEvent=r.commands.at(-1)!.eventId;
 const duplicate=async()=>this.db.prepare("SELECT 1 FROM b_result_reports WHERE run_id=? AND json_extract(report_json,'$.nodeSession.node.nodeId')=? AND (json_array_length(report_json,'$.nodeSession.runtime.commands')=? OR json_extract(report_json,'$.nodeSession.runtime.commands[#-1].eventId')=? OR (? IS NOT NULL AND (json_extract(receipt_json,'$.originalJournal.terminal.commandId')=? OR json_extract(receipt_json,'$.originalJournal.terminal.eventRef')=?)))").bind(runId,p.nodeId,r.commands.length,originalEvent,journal?.terminal.commandId??null,journal?.terminal.commandId??null,journal?.terminal.eventRef??null).first();
 if(await duplicate()){const completed=await replay();if(completed)return completed;return fail('original_event_conflict');}
 const evidenceRef='B_RESULT:'+crypto.randomUUID(),receipt:BResultReceipt={version:journal?bResultReceiptV2Version:bResultReceiptVersion,...(journal?{originalJournal:journal,finalCommandId:originalEvent}:{}),evidenceRef,requestId:b.requestId,reportOriginMapRevision:b.reportOriginMapRevision,originalEventId:journal?.terminal.commandId??r.commands.at(-1)!.eventId,commandRevision:r.commands.length,nativeSequence:r.sequence,checkpointHash:b.checkpointHash,completedWaves:2,verificationLevel:'STRUCTURE_SOURCE_RESULT_CONSISTENCY',combatTruth:'NOT_PROVEN',fullNativeReplayPerformed:false,arrivalAccepted:false,progressAuthorized:missingSources.length===0,missingSources,rewardsAuthorized:false};
 try{await this.db.batch([
 this.db.prepare(`INSERT INTO b_result_report_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN operation_previews p ON p.id=r.preview_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND p.account_id=? AND p.objective=? AND p.risk=? AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END`).bind(runId,accountId,x.row.preview_account,x.row.objective,x.row.risk,x.row.snapshot_json,x.row.revision,x.row.state_json,x.row.plan_json),
 this.db.prepare('INSERT INTO b_result_reports VALUES(?,?,?,?,?,?,?,?,?)').bind(runId,accountId,b.requestId,evidenceRef,requestHash,persisted.reportJson,json(receipt),x.row.state_json,x.row.revision),...persisted.chunks.map(chunk=>this.db.prepare('INSERT INTO b_result_journal_chunks VALUES(?,?,?,?)').bind(runId,evidenceRef,chunk.ordinal,chunk.text)),this.db.prepare('DELETE FROM b_result_report_write_guard')]);}
 catch(e){const saved=await replay();if(saved)return saved;if(await duplicate())return fail('original_event_conflict');if(/valid=1|UNIQUE constraint/u.test(String(e)))return fail('state_conflict');throw e;}
 return (await replay())!;
 }
}