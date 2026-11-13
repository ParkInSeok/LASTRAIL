import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {ExactCombatValue} from './combat-exact-value';
import {operationRiskDefinition,operationRiskVersion} from './combat-b-operation-risk';
import {captureOperationMap,type OperationMapPlan} from './operation-map-plan';
import {bWavePressureDefinition} from './combat-b-wave-manifest';
import {bDepartureCombatSourceVersion,type BDepartureCombatContext,type BDepartureCombatSource,type BDepartureCombatSourceOwner} from './combat-b-departure-combat-pin';
export const bScoutGunnerSourceVersion='combat.b-scout-gunner-source.v1.20261003';
export const bScoutGunnerDefinitionVersion='combat.b-scout-gunner-definition.v1.20261003';
const generationVersion='combat.b-scout-gunner-generation.v1.20261003';
const rngVersion='combat.b-wave-rng.sha256-counter64.v1',rngDomain='B_SCOUT_GUNNER_V04_WAVE';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u),integer=z.string().max(32).regex(/^(0|[1-9][0-9]*)$/u),positive=integer.refine(s=>s!=='0'),nat=z.number().int().nonnegative().safe();
const contextSchema=z.strictObject({accountId:id,runId:id,operationPreviewId:id,operationId:id,selectedRisk:z.enum(['I','II','III','IV','V']),departureVersion:id,baseSnapshotHash:hash,definitionVersion:id,planHash:hash,publicationHash:hash});
const originalSchema=z.strictObject({origin:contextSchema,nativeContextId:id,seed:hash,plan:z.unknown()});
const json=(v:unknown)=>canonicalJson(v as Parameters<typeof canonicalJson>[0]),digest=(v:unknown)=>sha256Hex(json(v));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_scout_gunner_'+code);};
const ratio=(n:number,d=1)=>({numerator:String(n),denominator:String(d)});
const stages=[{fromDepth:1,toDepth:4,ehp:1600,min:6,max:8},{fromDepth:5,toDepth:8,ehp:3600,min:9,max:12},{fromDepth:9,toDepth:11,ehp:8000,min:13,max:16}] as const;
const types=[{definitionId:'enemy_raider_scout',originalAtk:24,damageCoefficient:ratio(1),basicPeriodMs:1600,relativeEhpWeight:ratio(1)},
 {definitionId:'enemy_spike_cart_gunner',originalAtk:28,damageCoefficient:ratio(5,7),basicPeriodMs:2000,relativeEhpWeight:ratio(4,5)}] as const;
export const bRiskSpawnCadenceVersion='combat.b-risk-spawn-cadence.v1.20261003';
const exactRatioSchema=z.strictObject({numerator:integer,denominator:positive});
export function roundBTimingToClock(value:unknown,stepInput:unknown){
 const fraction=exactRatioSchema.safeParse(value),step=z.number().int().positive().safe().safeParse(stepInput);
 if(!fraction.success||!step.success)return fail('clock_invalid');
 const n=BigInt(fraction.data.numerator),d=BigInt(fraction.data.denominator),s=BigInt(step.data),ticks=(n+d*s-1n)/(d*s);
 const minimum=ticks<1n?1n:ticks;return {durationTicks:String(minimum),durationUs:String(minimum*s)};
}
export function gunnerFlightTiming(input:unknown){
 const x=z.strictObject({distanceMm:z.number().int().min(0).max(24000),stepUs:z.number().int().positive().safe()}).safeParse(input);
 if(!x.success)return fail('flight_invalid');const raw=ExactCombatValue.from(ratio(x.data.distanceMm*1000000,24000)).snapshot(),clock=roundBTimingToClock(raw,x.data.stepUs);
 return {speedMmPerSecond:24000,exactFlightUs:raw,flightTicks:clock.durationTicks,flightUs:clock.durationUs};
}
export function bRiskSpawnCadence(riskInput:unknown,depthInput:unknown){
 const r=z.enum(['I','II','III','IV','V']).safeParse(riskInput),d=z.number().int().min(1).max(11).safeParse(depthInput);
 if(!r.success||!d.success)return fail('cadence_invalid');const speedNumerator=10+['I','II','III','IV','V'].indexOf(r.data),stage=stages.find(s=>d.data>=s.fromDepth&&d.data<=s.toDepth)!;
 const baseIntervalMs=stage.fromDepth===1?1800:stage.fromDepth===5?1500:1200;
 return {version:bRiskSpawnCadenceVersion,selectedRisk:r.data,baseIntervalMs,speed:ratio(speedNumerator,10),intervalUs:ExactCombatValue.from(ratio(baseIntervalMs*10000,speedNumerator)).snapshot(),clockRounding:'CEIL_ONCE_AT_ORIGINAL_STEP' as const};
}
export function bRiskSpawnCadenceAtClock(risk:unknown,depth:unknown,stepUs:unknown,entryGroup:unknown){
 const group=nat.safeParse(entryGroup);if(!group.success||group.data>8)return fail('entry_group_invalid');const cadence=bRiskSpawnCadence(risk,depth),clock=roundBTimingToClock(cadence.intervalUs,stepUs);
 return {version:bRiskSpawnCadenceVersion,intervalTicks:clock.durationTicks,intervalUs:clock.durationUs,entryNotBeforeTicks:String(BigInt(clock.durationTicks)*BigInt(group.data)),entryNotBeforeUs:String(BigInt(clock.durationUs)*BigInt(group.data))};
}
const spatial=(gunner:boolean)=>({version:'combat.b-scout-gunner-spatial.v1.20261003',approval:'44#combat-b-range-projectile-movement-approved-v05-20261003',maximumDistanceMm:gunner?24000:1800,movementSpeedMmPerSecond:gunner?1500:1800,geometryUnits:'REAL_MM',lineOfSightRequired:true,connectedPathRequired:true,stopDuringPreparationAndRecovery:true,noTarget:'MOVE_TO_REACHABLE_ATTACK_POSITION',automaticRetreat:false,targetPolicy:gunner?'FARTHEST_LIVING_OPERATOR_IN_RANGE_AND_LOS_CAR_ORDER_THEN_ENTITY_ID':'LIVING_OPERATOR_ON_ASSIGNED_CAR',impactRangeRecheck:!gunner,preparationTargetLoss:'CANCEL_LAUNCH',noReachableTarget:'ORIGINAL_SAFE_STOP_OWNER',movementReservesAttackSlot:false,projectile:gunner?{speedMmPerSecond:24000,periodBasis:'LAUNCH_TO_LAUNCH',firstLaunch:'TARGETABLE_PLUS_FULL_PERIOD',flight:'EXACT_DISTANCE_OVER_SPEED_CEIL_ONCE_AT_ORIGINAL_STEP_MINIMUM_ONE',fixedSourceAndTargetGeneration:true,interceptableByBasic:true,interceptionIndependentOfAuto:true,survivesSourceDeath:true,targetGenerationLoss:'MISS',distanceExitAfterLaunch:'KEEP_PROJECTILE',reflectionAndAreaEffect:'EXISTING_ABILITY_ONLY'}:null});
/** Immutable APPROVED v04 source definitions, not registered execution profiles or historical A edits. */
export async function bScoutGunnerDefinition(){
 const definitions=await Promise.all(types.map(async t=>{
  const raw=ExactCombatValue.from(ratio(t.originalAtk)).multiply(ExactCombatValue.from(t.damageCoefficient)).snapshot();
  const base={version:bScoutGunnerDefinitionVersion,...structuredClone(t),defense:53,preparationMs:500,recoveryMs:300,
   rawAction:{sourceId:'B_V04_'+t.definitionId+'_OPERATOR_BASIC_RAW',kind:'RAW_ACTION_DAMAGE',value:raw,riskAppliedVersion:null,progressAppliedVersion:null},
   lineage:{catalogVersion:'combat.design.2026-09-09.001',catalogSha256:'66451f6948acc0d112864aaad0d95a3f0673993612ce691825727d1c305e263f',catalogField:'enemies['+t.definitionId+'].atk',approval:'44#combat-b-scout-gunner-approved-v04-20261003'},
   spatial:spatial(t.definitionId===types[1].definitionId),fixedActorHp:false,targetSelection:null,geometry:null,movement:null,projectile:null,runtimeSupported:false};return {...base,typeHash:await digest(base)};
 }));
 const risk=await operationRiskDefinition();const base={version:bScoutGunnerDefinitionVersion,approval:'44#combat-b-scout-gunner-approved-v04-20261003',types:definitions,waveBudgets:structuredClone(stages),
  composition:{scoutWeight:60,gunnerWeight:40,minimumEach:1,maximumGunner:'FLOOR_N_OVER_2',countDistribution:'UNIFORM_IN_APPROVED_STAGE_RANGE',kindDistribution:'CONDITIONAL_BINOMIAL_EXACT_INTEGER_WEIGHTS',avoidRepeat:'PREVIOUS_N_K_IF_OTHER_LEGAL_K_EXISTS',order:'DETERMINISTIC_FISHER_YATES'},
  hpPolicy:{defense:53,riskDefinitionVersion:operationRiskVersion,riskDefinitionHash:risk.definitionHash,riskEhpOnce:true,relativeWeights:'SCOUT_1_GUNNER_4_OVER_5',integerRemainder:'FIXED_ORDINAL_FIRST',hp:'FLOOR_INTEGER_EHP_TIMES_1000_OVER_1053',zeroHp:'DEFINITION_ERROR'},
  spawnCadence:{version:bRiskSpawnCadenceVersion,approval:'44#combat-b-risk-spawn-cadence-approved-20261003',riskSpeeds:['I','II','III','IV','V'].map((risk,index)=>({risk,speed:ratio(10+index,10)})),baseIntervalMs:[1800,1500,1200],clockRounding:'CONSUMER_CEIL_INTERVAL_ONCE_AT_ORIGINAL_STEP',entryOffsetClockPolicy:'ENTRY_GROUP_TIMES_SINGLE_ROUNDED_INTERVAL',catchUp:false,attackMovementProjectileUnchanged:true},boarding:null,sameTickGunnerInterceptionPolicy:null,pressurePolicy:bWavePressureDefinition(),runtimeSupported:false,geometry:null,movement:null,projectile:null,canStart:false};return {...base,definitionHash:await digest(base)};
}
const previousComposition=z.strictObject({count:z.number().int().min(6).max(16),gunnerCount:z.number().int().min(1).max(8)}).refine(p=>p.gunnerCount<=Math.floor(p.count/2));
export function scoutGunnerCompositionOptions(countInput:unknown,previousInput?:unknown){
 const parsed=z.number().int().min(6).max(16).safeParse(countInput);if(!parsed.success)return fail('count_invalid');const count=parsed.data,previous=previousInput===undefined?undefined:previousComposition.parse(previousInput);
 const rows=[];let choose=1n;for(let k=1;k<=Math.floor(count/2);k++){choose=choose*BigInt(count-k+1)/BigInt(k);rows.push({gunnerCount:k,weight:String(choose*2n**BigInt(k)*3n**BigInt(count-k))});}
 const filtered=previous?.count===count?rows.filter(r=>r.gunnerCount!==previous.gunnerCount):rows;return filtered.length?filtered:rows;
}
/** Private domain-specific rejection stream, independent of map RNG and Math.random.
 * A 64-bit word accommodates exact binomial totals exceeding uint32. No modulo bias.
 */
class WaveRng{
 private counter=0;private words:bigint[]=[];
 constructor(private readonly seed:string,private readonly binding:unknown){}
 async below(bound:bigint){const range=1n<<64n;if(bound<1n||bound>range)return fail('rng_bound');const limit=range/bound*bound;
  for(let attempts=0;attempts<128;attempts++){if(!this.words.length){const h=await digest([rngVersion,rngDomain,this.seed,this.binding,this.counter++]);this.words=h.match(/.{16}/gu)!.map(w=>BigInt('0x'+w));}const value=this.words.shift()!;if(value<limit)return value%bound;}
  return fail('rng_exhausted');
 }
}
const prior=z.strictObject({count:z.number().int().min(6).max(16),gunnerCount:z.number().int().min(1).max(8),generationHash:hash,sourceHash:hash});
const instance=z.strictObject({instanceId:id,ordinal:nat,definitionRef:z.strictObject({definitionId:id,version:z.literal(bScoutGunnerDefinitionVersion),typeHash:hash}),exactEhp:z.strictObject({numerator:integer,denominator:positive}),ehp:positive,hp:positive,remainderUnitApplied:z.boolean(),hpRiskAppliedVersion:z.literal(operationRiskVersion),defense:z.literal(53),entryGroup:nat,entryNotBeforeUs:exactRatioSchema});
const waveSchema=z.strictObject({version:z.literal(generationVersion),nodeId:id,visitDepth:z.number().int().min(1).max(11),waveOrdinal:z.union([z.literal(1),z.literal(2)]),count:z.number().int().min(6).max(16),gunnerCount:z.number().int().min(1).max(8),previousComposition:prior.nullable(),
 rng:z.strictObject({version:z.literal(rngVersion),domain:z.literal(rngDomain),countOffset:nat,compositionTicket:integer,shuffleChoices:z.array(nat).max(15)}),baseWaveEhp:z.union([z.literal(1600),z.literal(3600),z.literal(8000)]),riskAdjustedWaveEhp:positive,instances:z.array(instance).min(6).max(16),generationHash:hash});
const sourceSchema=z.strictObject({version:z.literal(bScoutGunnerSourceVersion),origin:contextSchema,nativeContextId:id,seedHash:hash,definition:z.json(),definitionHash:hash,nodeId:id,visitDepth:z.number().int().min(1).max(11),priorSourceHash:hash.nullable(),waves:z.array(waveSchema).length(2),definitionAvailable:z.literal(true),runtimeSupported:z.literal(false),canStart:z.literal(false),sourceHash:hash});
export type BScoutGunnerNodeSource=z.infer<typeof sourceSchema>;
type Definition=Awaited<ReturnType<typeof bScoutGunnerDefinition>>;
type Prior=z.infer<typeof prior>;
function compositionFromTicket(count:number,previous:Prior|null,ticket:bigint){const options=scoutGunnerCompositionOptions(count,previous?{count:previous.count,gunnerCount:previous.gunnerCount}:undefined),total=options.reduce((n,r)=>n+BigInt(r.weight),0n);if(ticket<0n||ticket>=total)return fail('composition_ticket');let cursor=ticket;for(const r of options){if(cursor<BigInt(r.weight))return r.gunnerCount;cursor-=BigInt(r.weight);}return fail('composition_ticket');}
function orderedKinds(count:number,gunnerCount:number,choices:number[]){if(choices.length!==count-1)return fail('shuffle_invalid');const ids=[...Array(count-gunnerCount).fill(types[0].definitionId),...Array(gunnerCount).fill(types[1].definitionId)] as string[];let index=0;for(let i=count-1;i>0;i--){const j=choices[index++]!;if(j<0||j>i)return fail('shuffle_invalid');[ids[i],ids[j]]=[ids[j]!,ids[i]!];}return ids;}
async function allocateInstances(ids:string[],nodeId:string,depth:number,waveOrdinal:number,origin:BDepartureCombatContext,nativeContextId:string,definition:Definition,riskDefinition?:Awaited<ReturnType<typeof operationRiskDefinition>>){
 const stage=stages.find(s=>depth>=s.fromDepth&&depth<=s.toDepth)!;const risks=riskDefinition??await operationRiskDefinition(),risk=risks.risks.find(r=>r.id===origin.selectedRisk)!;
 const budget=ExactCombatValue.from(ratio(stage.ehp)).multiply(ExactCombatValue.from(risk.hp)),total=BigInt(budget.floor()),weights=ids.map(id=>id===types[0].definitionId?5:4),sum=weights.reduce((n,w)=>n+w,0);
 const shares=weights.map(w=>budget.multiply(ExactCombatValue.from(ratio(w,sum)))),rounded=shares.map(v=>BigInt(v.floor())),remainder=total-rounded.reduce((n,v)=>n+v,0n);
 if(remainder<0n||remainder>=BigInt(ids.length))return fail('allocation_invalid');const identity=await digest([generationVersion,origin,nativeContextId,nodeId,waveOrdinal]),p=definition.pressurePolicy.stages.find(s=>depth>=s.fromDepth&&depth<=s.toDepth)!;
 const rows=ids.map((definitionId,ordinal)=>{const bonus=BigInt(ordinal)<remainder,ehp=rounded[ordinal]!+(bonus?1n:0n),hp=ehp*1000n/1053n;if(hp<1n)return fail('zero_hp');const t=definition.types.find(t=>t.definitionId===definitionId)!;
  const entryGroup=ordinal<p.firstGroup?0:1+Math.floor((ordinal-p.firstGroup)/2);return {instanceId:'B4:'+identity+':'+ordinal,ordinal,definitionRef:{definitionId,version:bScoutGunnerDefinitionVersion,typeHash:t.typeHash},exactEhp:shares[ordinal]!.snapshot(),ehp:String(ehp),hp:String(hp),remainderUnitApplied:bonus,hpRiskAppliedVersion:operationRiskVersion,defense:53 as const,entryGroup,entryNotBeforeUs:ExactCombatValue.from(bRiskSpawnCadence(origin.selectedRisk,depth).intervalUs).multiply(ExactCombatValue.from(ratio(entryGroup))).snapshot()};});
 return {baseWaveEhp:stage.ehp,riskAdjustedWaveEhp:String(total),instances:rows};
}
/** Immutable source owner for the approved definition/generator. No mutable global RNG,
 * current template lookup, spawn/HP execution, network or resource lifetime is owned here.
 */
export class BScoutGunnerSource{
 private constructor(private readonly origin:BDepartureCombatContext,private readonly nativeContextId:string,private readonly seed:string,private readonly plan:OperationMapPlan,private readonly definition:Definition,private readonly seedHash:string,private readonly riskDefinition?:Awaited<ReturnType<typeof operationRiskDefinition>>){}
 static async create(input:unknown){const parsed=originalSchema.safeParse(structuredClone(input));if(!parsed.success)return fail('original_invalid');const x=parsed.data,plan=captureOperationMap(x.plan);if(x.nativeContextId===x.origin.runId||await digest(plan)!==x.origin.planHash||plan.definitionVersion!==x.origin.definitionVersion)return fail('original_conflict');return new BScoutGunnerSource(x.origin,x.nativeContextId,x.seed,plan,await bScoutGunnerDefinition(),await sha256Hex(x.seed));}
 /** Frozen original SQL departure definitions; never reads a mutable catalog or rerolls seed. */
 static async fromFrozenDeparture(input:unknown,definitionInput:unknown,riskInput:unknown){
  const x=originalSchema.parse(structuredClone(input)),plan=captureOperationMap(x.plan);
  const definition=structuredClone(definitionInput) as Definition,risk=structuredClone(riskInput) as Awaited<ReturnType<typeof operationRiskDefinition>>;
  if(!definition||!risk||definition.version!==bScoutGunnerDefinitionVersion||risk.version!==operationRiskVersion)return fail('definition_conflict');
  const {definitionHash,...body}=definition,{definitionHash:riskHash,...riskBody}=risk;
  if(await digest(body)!==definitionHash||'sha256:'+await digest(riskBody)!==riskHash||definition.hpPolicy.riskDefinitionHash!==riskHash||x.nativeContextId===x.origin.runId||await digest(plan)!==x.origin.planHash||plan.definitionVersion!==x.origin.definitionVersion)return fail('original_conflict');
  return new BScoutGunnerSource(x.origin,x.nativeContextId,x.seed,plan,definition,await sha256Hex(x.seed),risk);
 }
 private node(id:string){const node=this.plan.nodes.find(n=>n.nodeId===id);if(!node||node.eventKind!=='COMBAT'||node.visitDepth<1||node.visitDepth>11)return fail('node_unsupported');return node;}
 private async generateWave(nodeId:string,depth:number,waveOrdinal:1|2,previous:Prior|null){
  const stage=stages.find(s=>depth>=s.fromDepth&&depth<=s.toDepth)!,rng=new WaveRng(this.seed,[this.origin,this.nativeContextId,nodeId,waveOrdinal,previous]);
  const offset=Number(await rng.below(BigInt(stage.max-stage.min+1))),count=stage.min+offset,options=scoutGunnerCompositionOptions(count,previous?{count:previous.count,gunnerCount:previous.gunnerCount}:undefined),total=options.reduce((n,r)=>n+BigInt(r.weight),0n),ticket=await rng.below(total),gunnerCount=compositionFromTicket(count,previous,ticket),choices=[];
  for(let i=count-1;i>0;i--)choices.push(Number(await rng.below(BigInt(i+1))));const allocation=await allocateInstances(orderedKinds(count,gunnerCount,choices),nodeId,depth,waveOrdinal,this.origin,this.nativeContextId,this.definition,this.riskDefinition);
  const payload={version:generationVersion,nodeId,visitDepth:depth,waveOrdinal,count,gunnerCount,previousComposition:previous,rng:{version:rngVersion,domain:rngDomain,countOffset:offset,compositionTicket:String(ticket),shuffleChoices:choices},...allocation};return waveSchema.parse({...payload,generationHash:await digest(payload)});
 }
 async generateNode(nodeId:string,previous?:{source:unknown;expectedHash:string}){
  const node=this.node(id.parse(nodeId));let priorSourceHash:string|null=null,priorWave:Prior|null=null;
  if(previous){const old=await this.readNode(previous.source,previous.expectedHash);if(old.visitDepth>=node.visitDepth)return fail('previous_conflict');const w=old.waves[1]!;priorSourceHash=old.sourceHash;priorWave={count:w.count,gunnerCount:w.gunnerCount,generationHash:w.generationHash,sourceHash:old.sourceHash};}
  const first=await this.generateWave(nodeId,node.visitDepth,1,priorWave),second=await this.generateWave(nodeId,node.visitDepth,2,{count:first.count,gunnerCount:first.gunnerCount,generationHash:first.generationHash,sourceHash:priorSourceHash??'0'.repeat(64)});
  const payload={version:bScoutGunnerSourceVersion,origin:structuredClone(this.origin),nativeContextId:this.nativeContextId,seedHash:this.seedHash,definition:structuredClone(this.definition),definitionHash:this.definition.definitionHash,nodeId,visitDepth:node.visitDepth,priorSourceHash,waves:[first,second],definitionAvailable:true as const,runtimeSupported:false as const,canStart:false as const};return sourceSchema.parse({...payload,sourceHash:await digest(payload)});
 }
 /** Expected original source hash is mandatory; no new RNG draws on restore. */
 async readNode(input:unknown,expectedHash:string){const h=hash.safeParse(expectedHash),parsed=sourceSchema.safeParse(structuredClone(input));if(!h.success||!parsed.success)return fail('source_invalid');const p=parsed.data,{sourceHash,...payload}=p;
  if(sourceHash!==h.data||await digest(payload)!==sourceHash||json(p.origin)!==json(this.origin)||p.nativeContextId!==this.nativeContextId||p.seedHash!==this.seedHash||p.definitionHash!==this.definition.definitionHash||json(p.definition)!==json(this.definition))return fail('source_conflict');const node=this.node(p.nodeId);if(node.visitDepth!==p.visitDepth)return fail('node_conflict');
  for(const [index,w] of p.waves.entries()){if(w.waveOrdinal!==index+1||w.nodeId!==p.nodeId||w.visitDepth!==node.visitDepth)return fail('wave_conflict');const stage=stages.find(s=>node.visitDepth>=s.fromDepth&&node.visitDepth<=s.toDepth)!;if(w.rng.countOffset>stage.max-stage.min||w.count!==stage.min+w.rng.countOffset||w.gunnerCount!==compositionFromTicket(w.count,w.previousComposition,BigInt(w.rng.compositionTicket)))return fail('composition_conflict');
   if(index===0&&w.previousComposition?.sourceHash!==p.priorSourceHash&&!(w.previousComposition===null&&p.priorSourceHash===null))return fail('previous_conflict');if(index===1){const first=p.waves[0]!;if(json(w.previousComposition)!==json({count:first.count,gunnerCount:first.gunnerCount,generationHash:first.generationHash,sourceHash:p.priorSourceHash??'0'.repeat(64)}))return fail('previous_conflict');}
   const allocation=await allocateInstances(orderedKinds(w.count,w.gunnerCount,w.rng.shuffleChoices),p.nodeId,node.visitDepth,w.waveOrdinal,this.origin,this.nativeContextId,this.definition,this.riskDefinition);if(json(allocation)!==json({baseWaveEhp:w.baseWaveEhp,riskAdjustedWaveEhp:w.riskAdjustedWaveEhp,instances:w.instances}))return fail('allocation_conflict');const {generationHash,...base}=w;if(await digest(base)!==generationHash)return fail('generation_conflict');
  }return p;
 }
}
export interface BScoutGunnerSeedSourceOwner{
 /** Internal original seed/node/previous receipt owner. Fence original seed/manifest/support
  * revisions in the SAME S19 batch. Never accept these values from an HTTP request. */
 prepare(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>):Promise<{seed:string;nodeId:string;previous?:{source:unknown;expectedHash:string};assertions:readonly D1PreparedStatement[]}>;
}
/** Additive definition/generation metadata at the original owner seam. It deliberately
 * cannot admit a runtime until the actual geometry/executor/profile contract is supplied.
 * Instance-specific EHP/HP is a NEW source schema, never a replacement for old rawProfiles
 * or the v1 generic template hp value. No fake HP/template adaptation is performed.
 */
export function createBScoutGunnerDefinitionSourceOwner(parent:BDepartureCombatSourceOwner,seedOwner:BScoutGunnerSeedSourceOwner|undefined,planInput:unknown):BDepartureCombatSourceOwner{
 const plan=captureOperationMap(structuredClone(planInput));return {prepare:async context=>{
  if(!seedOwner)return fail('seed_owner_unavailable');const resolved=await parent.prepare(structuredClone(context)),source=structuredClone(resolved.source) as BDepartureCombatSource;
  if(!source||source.version!==bDepartureCombatSourceVersion||source.profile?.definition?.scoutGunnerSourceVersion!==bScoutGunnerSourceVersion||Object.hasOwn(source.profile.definition,'bScoutGunnerSource'))return fail('opt_in_required');
  const parentAssertions=Array.from(resolved.assertions??[]);if(!parentAssertions.length||parentAssertions.some(s=>typeof s.bind!=='function'))return fail('source_fence_missing');
  const original=await seedOwner.prepare(structuredClone(source),structuredClone(context)),seed=structuredClone(original.seed),nodeId=original.nodeId,previous=original.previous?structuredClone(original.previous):undefined,assertions=Array.from(original.assertions??[]);
  if(!assertions.length||assertions.some(s=>typeof s.bind!=='function')||assertions.length+parentAssertions.length>128)return fail('source_fence_missing');const generator=await BScoutGunnerSource.create({origin:context,nativeContextId:source.contextId,seed,plan}),data=await generator.generateNode(nodeId,previous);
  return {source:{...source,profile:{...source.profile,definition:{...source.profile.definition,bScoutGunnerSource:data}}},assertions:[...parentAssertions,...assertions]};
 },supports:async()=>false};
}
export {sourceSchema as bScoutGunnerNodeSourceSchema};
