import { z } from 'zod';
import { buffVersion, buffTypeSchema, readBuffRank } from './combat-b-carriage-buff-definitions';
import { ExactCombatValue, exactCombatValueSchema } from './combat-exact-value';
import { numericDefenseInputSchema, resolveNumericDefenseBudget } from './combat-numeric-defense';
export { buffVersion, captureDefinitions } from './combat-b-carriage-buff-definitions';
const id=z.string().min(1).max(128), slot=z.number().int().min(0).max(4);
const revision=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-1);
const occupant=z.discriminatedUnion('kind',[
 z.strictObject({kind:z.literal('OPERATOR'),operatorId:id}),z.strictObject({kind:z.literal('CARGO')}),
]);
export const buffDepartureSchema=z.strictObject({
 version:z.literal(buffVersion),accountId:id,selectionRevision:revision.min(1),rankRevision:revision.min(1),
 sourceStatus:z.enum(['ISOLATED_LOCAL_FIXTURE','PERSISTED_ACCOUNT_SOURCE']),
 cars:z.array(z.strictObject({slot,instanceId:id,carriageId:id,occupant,type:buffTypeSchema,rank:z.number().int().min(1).max(5)})).min(1).max(5),
}).superRefine((d,c)=>{
 const unique=(values:unknown[])=>new Set(values).size===values.length;
 const ops=d.cars.flatMap(x=>x.occupant.kind==='OPERATOR'?[x.occupant.operatorId]:[]);
 if(!unique(d.cars.map(x=>x.slot))||!unique(d.cars.map(x=>x.instanceId))||!unique(ops))c.addIssue({code:'custom',message:'combat_b_departure_duplicate'});
 const ranks=new Map<string,number>();
 for(const car of d.cars){if(ranks.has(car.type)&&ranks.get(car.type)!==car.rank)c.addIssue({code:'custom',message:'combat_b_shared_rank_conflict'});ranks.set(car.type,car.rank);}
});
export type BRefitDeparture=z.infer<typeof buffDepartureSchema>;
const mode=z.enum(['EXTERIOR','INTERIOR']),body=z.enum(['NORMAL','BROKEN']),actor=z.enum(['UP','DOWN','ABSENT']);
const initialSchema=z.strictObject({contextId:id,mode,cars:z.array(z.strictObject({slot,body,actor})).min(1).max(5)});
const kind=z.enum(['BASIC','ACTIVE','ULTIMATE','SENTRY','COMMANDER','DERIVED','INTERCEPT','HEAL','SHIELD','REPAIR','ITEM','OVERHEAL','EXPIRY_HEAL','RETURN','SUPPORT3']);
const target=z.enum(['STRUCTURE','ATTACHED','CLIMBING','APPROACH','ROOFTOP','ENEMY_TRAIN_BODY','ENEMY_TRAIN_PART','ENEMY','NONE']);
const actionSchema=z.strictObject({eventId:id,expectedRevision:revision,slot,kind,ability:z.enum(['BASIC','ACTIVE','ULTIMATE','INTERCEPT','EXTERNAL']),timing:z.enum(['TIME_COOLDOWN','INTERCEPTION_PHASE','NONE']),target,direct:z.boolean(),originalEligible:z.boolean()});
const transitionSchema=z.discriminatedUnion('kind',[
 z.strictObject({eventId:id,expectedRevision:revision,kind:z.literal('BODY'),slot,value:body}),
 z.strictObject({eventId:id,expectedRevision:revision,kind:z.literal('ACTOR'),slot,value:actor}),
 z.strictObject({eventId:id,expectedRevision:revision,kind:z.literal('MODE'),value:mode}),
]);
type Action=z.infer<typeof actionSchema>;
type Transition=z.infer<typeof transitionSchema>;
const amount=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const acceptedSchema=z.strictObject({version:z.literal(buffVersion),sourceId:z.string().min(1).max(4096),
 slot,instanceId:id,type:buffTypeSchema,rank:z.number().int().min(1).max(5),operatingRevision:revision,
 damageBps:amount,supportBps:amount,skillHasteBps:amount,interceptionHasteBps:amount,kind,ability:z.enum(['BASIC','ACTIVE','ULTIMATE','INTERCEPT','EXTERNAL']),timing:z.enum(['TIME_COOLDOWN','INTERCEPTION_PHASE','NONE']),target,
});
type Accepted=z.infer<typeof acceptedSchema>;
type Entry={tag:'ACTION';input:Action}|{tag:'TRANSITION';input:Transition};
const entrySchema=z.discriminatedUnion('tag',[
 z.strictObject({tag:z.literal('ACTION'),input:actionSchema}),z.strictObject({tag:z.literal('TRANSITION'),input:transitionSchema}),
]);
const snapshotSchema=z.strictObject({version:z.literal(buffVersion),departure:buffDepartureSchema,initial:initialSchema,
 journal:z.array(entrySchema).max(1024),actions:z.array(z.strictObject({input:actionSchema,source:acceptedSchema})).max(1024),
});
const exact=(n:bigint,d=1n)=>ExactCombatValue.from({numerator:String(n),denominator:String(d)});
const amplification=(bps:number)=>exact(10000n+BigInt(bps),10000n);
/** Flat terms must be supplied BEFORE original percent DEF groups; no armor conversion. */
export function resolveBuffDefenseStat(existingFlat:unknown,existingPercentBps:number,buffFlat:number){
 const parsed=z.strictObject({flat:exactCombatValueSchema,percent:amount,buff:amount}).parse({flat:existingFlat,percent:existingPercentBps,buff:buffFlat});
 return ExactCombatValue.from(parsed.flat).add(exact(BigInt(parsed.buff))).multiply(amplification(parsed.percent)).snapshot();
}
/** Internal authority port only. No HTTP/auth, HP, effect timer, cooldown clock or START owner.
 * Instantiate only from a trusted fixed departure source. Historical versions use their own kernel.
 * Parent vitals owner reports state after the breaking hit used its pre-hit DEF capture.
 */
export class BCarriageBuffRuntime {
 private readonly departure:BRefitDeparture;
 private readonly initial:z.infer<typeof initialSchema>;
 private mode:z.infer<typeof mode>;
 private readonly states=new Map<number,{body:z.infer<typeof body>;actor:z.infer<typeof actor>}>();
 private revision=0;
 private readonly journal:Entry[]=[];
 private readonly events=new Map<string,Entry>();
 private readonly actions=new Map<string,{input:Action;source:Accepted}>();
 constructor(departure:unknown,initial:unknown){
  this.departure=buffDepartureSchema.parse(departure);this.initial=initialSchema.parse(initial);this.mode=this.initial.mode;
  if(this.initial.cars.length!==this.departure.cars.length||new Set(this.initial.cars.map(c=>c.slot)).size!==this.initial.cars.length)throw new Error('combat_b_initial_invalid');
  for(const state of this.initial.cars){const car=this.car(state.slot);if((car.occupant.kind==='CARGO')!==(state.actor==='ABSENT'))throw new Error('combat_b_actor_invalid');this.states.set(state.slot,{body:state.body,actor:state.actor});}
 }
 private car(slot:number){const car=this.departure.cars.find(c=>c.slot===slot);if(!car)throw new Error('combat_b_slot_invalid');return car;}
 private replay(entry:Entry){const old=this.events.get(entry.input.eventId);if(!old)return false;if(JSON.stringify(old)!==JSON.stringify(entry))throw new Error('combat_b_event_conflict');return true;}
 private preflight(entry:Entry){if(entry.input.expectedRevision!==this.revision)throw new Error('combat_b_state_revision_conflict');if(this.journal.length>=1024)throw new Error('combat_b_journal_capacity');}
 private record(entry:Entry){this.journal.push(entry);this.events.set(entry.input.eventId,entry);}
 transition(input:unknown){
  const event=transitionSchema.parse(input),entry:Entry={tag:'TRANSITION',input:event};
  if(this.replay(entry))return;this.preflight(entry);
  if(event.kind==='MODE')this.mode=event.value;
  else{const car=this.car(event.slot),state=this.states.get(event.slot)!;
   if(event.kind==='BODY')state.body=event.value;
   else {if((car.occupant.kind==='CARGO')!==(event.value==='ABSENT'))throw new Error('combat_b_actor_invalid');state.actor=event.value;}
  }
  this.revision++;this.record(entry);
 }
 accept(input:unknown):Accepted{
  const a=actionSchema.parse(input),entry:Entry={tag:'ACTION',input:a};
  if(this.replay(entry))return structuredClone(this.actions.get(a.eventId)!.source);
  this.preflight(entry);const car=this.car(a.slot),state=this.states.get(a.slot)!;
  if(['BASIC','ACTIVE','ULTIMATE','INTERCEPT'].includes(a.kind)&&a.ability!==a.kind)throw new Error('combat_b_ability_mismatch');
  if(['HEAL','SHIELD'].includes(a.kind)&&!['ACTIVE','ULTIMATE'].includes(a.ability))throw new Error('combat_b_ability_mismatch');
  if(!['BASIC','ACTIVE','ULTIMATE','INTERCEPT','HEAL','SHIELD'].includes(a.kind)&&a.ability!=='EXTERNAL')throw new Error('combat_b_ability_mismatch');
  if(this.mode!=='EXTERIOR')throw new Error('combat_b_interior_unsupported');
  if(car.occupant.kind!=='OPERATOR'||state.actor!=='UP')throw new Error('combat_b_actor_unavailable');
  if((a.timing==='TIME_COOLDOWN'&&a.ability!=='ACTIVE')||(a.timing==='INTERCEPTION_PHASE'&&a.ability!=='INTERCEPT'))throw new Error('combat_b_timing_mismatch');
  if(a.kind==='INTERCEPT'&&!a.originalEligible)throw new Error('combat_b_interception_ineligible');
  const operatingEligible=state.body==='NORMAL'&&a.originalEligible;
  const active=operatingEligible&&a.direct;
  const value=readBuffRank(car.type,car.rank);
  let damageBps=0,supportBps=0,skillHasteBps=0,interceptionHasteBps=0;
  const directDamage=['BASIC','ACTIVE','ULTIMATE'].includes(a.kind);
  if(a.kind==='BASIC'&&a.target==='NONE')throw new Error('combat_b_target_required');
  if(active&&directDamage){
   if(a.slot===0&&a.target==='STRUCTURE')damageBps+=2000;
   if(car.type==='BCB01'&&a.kind==='BASIC')damageBps+=value.value;
   if(car.type==='BCB04'){if(a.target==='STRUCTURE')damageBps+=value.value;else if(a.target==='ATTACHED'||a.target==='CLIMBING')damageBps+=value.secondary;}
   if(car.type==='BCB07'&&(a.target==='ENEMY_TRAIN_BODY'||a.target==='ENEMY_TRAIN_PART'))damageBps+=value.value;
  }
  if(active&&car.type==='BCB05'&&(a.kind==='HEAL'||a.kind==='SHIELD'))supportBps=value.value;
  if(operatingEligible&&car.type==='BCB02'&&a.ability==='ACTIVE'&&a.timing==='TIME_COOLDOWN')skillHasteBps=value.value;
  if(operatingEligible&&car.type==='BCB03'&&a.kind==='INTERCEPT'&&a.timing==='INTERCEPTION_PHASE')interceptionHasteBps=value.value;
  const source:Accepted={version:buffVersion,sourceId:JSON.stringify([buffVersion,this.initial.contextId,this.departure.accountId,car.instanceId,a.eventId]),slot:a.slot,instanceId:car.instanceId,type:car.type,rank:car.rank,operatingRevision:this.revision,damageBps,supportBps,skillHasteBps,interceptionHasteBps,kind:a.kind,ability:a.ability,timing:a.timing,target:a.target};
  this.actions.set(a.eventId,{input:a,source});this.record(entry);return structuredClone(source);
 }
 private source(eventId:string){const source=this.actions.get(eventId)?.source;if(!source)throw new Error('combat_b_action_missing');return source;}
 damage(eventId:string,input:unknown){
  const source=this.source(eventId),original=numericDefenseInputSchema.parse(input);
  // C is independent of original capped outgoing groups and applied before their existing owner.
  const base=ExactCombatValue.from(original.base).multiply(amplification(source.damageBps)).snapshot();
  return {...resolveNumericDefenseBudget({...original,base}),buffVersion,group:'B_DIRECT_DAMAGE_C',valueBps:source.damageBps};
 }
 support(eventId:string,originalBudget:unknown){const source=this.source(eventId);const exactBudget=ExactCombatValue.from(originalBudget).multiply(amplification(source.supportBps));return {version:buffVersion,exact:exactBudget.snapshot(),projected:exactBudget.floor(),valueBps:source.supportBps};}
 /** Exact newly accepted time only. Original clock owns eligible reductions/tick ceil and phase preservation. */
 time(eventId:string,base:unknown,existingHasteBps:number){
  const source=this.source(eventId);amount.parse(existingHasteBps);
  const added=source.ability==='ACTIVE'?source.skillHasteBps:source.ability==='INTERCEPT'?source.interceptionHasteBps:0;
  const denominator=10000n+BigInt(existingHasteBps)+BigInt(added);
  return ExactCombatValue.from(base).multiply(exact(10000n,denominator)).snapshot();
 }
 defense(selectedSlot:number,target:'BODY'|'OPERATOR'){
  slot.parse(selectedSlot);z.enum(['BODY','OPERATOR']).parse(target);const car=this.car(selectedSlot),state=this.states.get(selectedSlot)!;
  if(this.mode!=='EXTERIOR'||state.body!=='NORMAL'||car.type!=='BCB06')return 0;
  const rank=readBuffRank(car.type,car.rank);
  return target==='BODY'?rank.value:car.occupant.kind==='OPERATOR'&&state.actor==='UP'?rank.secondary:0;
 }
 capacity(){return {limit:1024,recorded:this.journal.length,remaining:1024-this.journal.length,scope:'BOUNDED_LOCAL_NOT_FULL_RUN' as const};}
 snapshot(){return structuredClone({version:buffVersion,departure:this.departure,initial:this.initial,journal:this.journal,actions:[...this.actions.values()]});}
 static restore(input:unknown){
  const parsed=snapshotSchema.parse(input),runtime=new BCarriageBuffRuntime(parsed.departure,parsed.initial);
  for(const entry of parsed.journal){if(entry.tag==='ACTION')runtime.accept(entry.input);else runtime.transition(entry.input);}
  if(JSON.stringify(runtime.snapshot())!==JSON.stringify(parsed))throw new Error('combat_b_snapshot_conflict');
  return runtime;
 }
}