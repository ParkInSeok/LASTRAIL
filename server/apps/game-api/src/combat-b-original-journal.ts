import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),rev=nat.min(1),digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const raw=z.discriminatedUnion('kind',[z.strictObject({kind:z.literal('WAVE_CLEAR'),waveOrdinal:rev,tick:nat,sequence:rev}),z.strictObject({kind:z.literal('TERMINAL'),outcome:z.literal('VICTORY'),tick:nat,sequence:rev})]);
const point=z.object({version:z.literal('unity.combat-b.event-point.v1.20261006'),bindingHash:digest,tick:nat,sequence:rev,commandRevision:rev,priorCommandPrefixHash:digest,state:z.json(),boarding:z.json(),waveEntry:z.json(),enemyTiming:z.json(),travel:z.json()}).passthrough();
export const bOriginalJournalSchema=z.strictObject({version:z.literal('unity.combat-b.original-event-journal.v1.20261006'),provenance:z.literal('ORIGINAL_UNITY_LOCAL_NOT_SERVER_ACCEPTANCE'),records:z.array(z.strictObject({eventRef:digest,binding:z.strictObject({kind:z.enum(['WAVE_CLEAR','TERMINAL']),rawEvent:raw,originalCommand:z.json(),commandRevision:rev,checkpointHash:digest}),checkpoint:point})).length(3)});
export type BJournalFact={eventRef:string;commandId:string;commandRevision:number;rawSequence:number;pointSequence:number;tick:number;checkpointHash:string};
export type BJournalVerification={version:'combat.b-original-journal-consistency.v1.20261006';journalHash:string;waves:BJournalFact[];terminal:BJournalFact};
const json=(x:unknown)=>canonicalJson(z.json().parse(x));
const need=(v:boolean,s:string)=>{if(!v)throw new ActivityError(409,'combat_b_result_journal_'+s);};
type Context={bindingHash:string;commands:{kind:string;eventId:string}[];results:{tick:number;outcome:string}[];sequence:number;waveEntry:{events:unknown[]};state:{direct:{clock:{stepTicks:number};allies:unknown}};travel:unknown};
type Node={origin:unknown;nodeId:string;sourceHash:string;waves:{instances:{instanceId:string;hp:string|number}[]}[]};
/** Verifies original event-point consistency, not replay or combat truth.
 * Journal is never regenerated from terminal state or a later command. */
export async function verifyBOriginalJournal(input:unknown,r:Context,node:Node):Promise<BJournalVerification>{
 const j=bOriginalJournalSchema.parse(input),facts:BJournalFact[]=[];
 const travel=z.object({nativeContextId:id,nodeId:id,nodeSourceHash:z.string(),bindingHash:digest,configurationHash:digest,origin:z.json()}).passthrough().parse(r.travel);
 for(let i=0;i<j.records.length;i++){
  const e=j.records[i]!,b=e.binding,p=e.checkpoint,raw=b.rawEvent,c=r.commands[b.commandRevision-1],result=r.results[b.commandRevision-1];
  need(b.kind===(i<2?'WAVE_CLEAR':'TERMINAL')&&b.kind===raw.kind,'order_conflict');
  need(e.eventRef==='sha256:'+await sha256Hex(json(b))&&b.checkpointHash==='sha256:'+await sha256Hex(json(p)),'hash_conflict');
  need(!!c&&!!result&&json(b.originalCommand)===json(c)&&(c.kind==='WAVE_BASIC'||c.kind==='ADVANCE'),'command_conflict');
  need(p.commandRevision===b.commandRevision&&p.bindingHash===r.bindingHash&&p.priorCommandPrefixHash==='sha256:'+await sha256Hex(json({commands:r.commands.slice(0,b.commandRevision-1)})),'prefix_conflict');
  need(raw.tick===p.tick&&p.tick>=(r.results[b.commandRevision-2]?.tick??0)&&p.tick<=result!.tick&&raw.sequence<=p.sequence&&p.sequence<=r.sequence,'cursor_conflict');
  const t=z.object({nativeContextId:id,nodeId:id,nodeSourceHash:z.string(),bindingHash:digest,configurationHash:digest,origin:z.json(),commandRevision:rev,sequence:rev,stepTicks:rev,stopped:z.boolean(),stopReason:z.string().nullable(),motion:z.object({currentTick:nat,completedWaves:nat}).passthrough()}).passthrough().parse(p.travel);
  need(t.nativeContextId===travel.nativeContextId&&t.nodeId===node.nodeId&&t.nodeId===travel.nodeId&&t.nodeSourceHash===node.sourceHash&&t.nodeSourceHash===travel.nodeSourceHash&&t.bindingHash===r.bindingHash&&t.configurationHash===travel.configurationHash&&json(t.origin)===json(travel.origin)&&json(t.origin)===json(node.origin)&&t.commandRevision===b.commandRevision&&t.sequence===p.sequence&&t.motion.currentTick===p.tick&&t.stepTicks===r.state.direct.clock.stepTicks,'source_conflict');
  const w=z.object({waveOrdinal:rev,cursor:nat,finished:z.boolean(),events:z.array(rawWaveSchema)}).passthrough().parse(p.waveEntry),ordinal=Math.min(i+1,2);
  need(w.waveOrdinal===ordinal&&w.cursor===node.waves[ordinal-1]!.instances.length&&w.finished===(ordinal===2)&&json(w.events)===json(r.waveEntry.events.slice(0,ordinal))&&t.motion.completedWaves===ordinal,'wave_conflict');
  if(raw.kind==='WAVE_CLEAR')need(raw.waveOrdinal===i+1&&json(raw)===json(r.waveEntry.events[i]),'wave_conflict');
  if(i===0)need(!t.stopped&&t.stopReason===null&&result!.outcome==='RUNNING','terminal_conflict');
  else need(t.stopped&&t.stopReason==='TERMINAL'&&result!.outcome==='VICTORY'&&r.results[b.commandRevision-2]?.outcome==='RUNNING','terminal_conflict');
  const state=z.object({direct:z.object({clock:z.object({completedSteps:nat,stepTicks:rev,speedHalfUnits:z.literal(2)}).passthrough(),enemies:z.record(z.string(),z.object({hp:nat,maxHp:rev}).passthrough()),allies:z.object({definition:z.json()}).passthrough()}).passthrough()}).passthrough().parse(p.state);
  const instances=node.waves.flatMap(w=>w.instances),cleared=new Set(node.waves.slice(0,ordinal).flatMap(w=>w.instances.map(v=>v.instanceId)));
  const finalAllies=z.object({definition:z.json()}).passthrough().parse(r.state.direct.allies);
  need(state.direct.clock.completedSteps===p.tick&&state.direct.clock.stepTicks===t.stepTicks&&json(state.direct.allies.definition)===json(finalAllies.definition)&&Object.keys(state.direct.enemies).length===instances.length&&instances.every(v=>{const h=state.direct.enemies[v.instanceId];return !!h&&h.maxHp===Number(v.hp)&&h.hp<=h.maxHp&&(!cleared.has(v.instanceId)||h.hp===0);}),'state_conflict');
  const lives=z.object({lives:z.array(z.object({id,phase:z.string(),registered:z.boolean(),targetable:z.boolean(),attackEligible:z.boolean()}).passthrough())}).passthrough().parse(p.boarding).lives;
  need(lives.length===instances.length&&new Set(lives.map(v=>v.id)).size===instances.length&&lives.every(v=>instances.some(x=>x.instanceId===v.id)&&(!cleared.has(v.id)||(v.phase==='Dead'&&!v.registered&&!v.targetable&&!v.attackEligible))),'boarding_conflict');
  facts.push({eventRef:e.eventRef,commandId:c!.eventId,commandRevision:b.commandRevision,rawSequence:raw.sequence,pointSequence:p.sequence,tick:p.tick,checkpointHash:b.checkpointHash});
 }
 const [first,second,terminal]=facts;
 need(first!.commandRevision<second!.commandRevision&&first!.rawSequence<second!.rawSequence&&second!.commandRevision===terminal!.commandRevision&&second!.checkpointHash===terminal!.checkpointHash&&second!.pointSequence===terminal!.pointSequence&&terminal!.rawSequence===terminal!.pointSequence,'terminal_conflict');
 need(new Set(facts.map(v=>v.eventRef)).size===facts.length,'duplicate_event');
 return {version:'combat.b-original-journal-consistency.v1.20261006',journalHash:'sha256:'+await sha256Hex(json(j)),waves:facts.slice(0,2),terminal:terminal!};
}
const rawWaveSchema=z.strictObject({kind:z.literal('WAVE_CLEAR'),waveOrdinal:rev,tick:nat,sequence:rev});
