import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureBActiveSource} from './combat-b-active-source';
import type {RefitRead,LocalReference} from './combat-b-local-flow-source';
import {BCarriageBuffRuntime} from './combat-b-carriage-buffs';
import {exactCombatValueSchema} from './combat-exact-value';
export const activeProjectileSourceVersion='combat.b-active-projectile-source.v1.20261003';
export const miraProjectileId='combat.b.mira.active.projectile.v1';
export const miraImpactEffectId='combat.b.mira.active.impact.v1';
export const miraProjectileWireVersion='combat.b-mira-projectile-wire.v1.20261003';
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=async(x:unknown)=>'sha256:'+await sha256Hex(json(x));
const safe=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),id=z.string().min(1).max(128),hash=z.string().regex(/^sha256:[a-f0-9]{64}$/);
export const miraTargetIdentitySchema=z.strictObject({targetId:id,generation:id,battlefieldGeneration:id});
/** Wire projection only; original Unity owner supplies real acceptance, first-shot ATK and per-shot modifier. */
export const miraEmissionSchema=z.strictObject({version:z.literal(miraProjectileWireVersion),sourcePacketHash:hash,contextId:id,projectileId:z.literal(miraProjectileId),effectId:z.literal(miraImpactEffectId),shotId:id,commandId:id,actorId:z.literal('op_mira'),instanceId:id,slot:z.number().int().min(0).max(4),abilityId:z.literal('op_mira.active'),acceptedTick:safe,emittedTick:safe,impactDue:safe,sequence:safe.min(1),shotIndex:z.number().int().min(0).max(5),target:miraTargetIdentitySchema,attackSnapshot:exactCombatValueSchema,damageBps:z.literal(4500),modifierSourceHash:hash,modifierEventId:id,modifierSnapshot:z.unknown()});
const resolutionSchema=z.strictObject({tick:safe,sequence:safe.min(1),result:z.enum(['RESOLVED_HIT','MISS_TARGET_INVALID','CLEANED_TERMINAL']),originalImpactReceiptId:id.nullable()});
export const miraProjectileSuffixSchema=z.strictObject({version:z.literal(miraProjectileWireVersion),sourcePacketHash:hash,contextId:id,clockTick:safe,clockDebtTimeSpanTicks:safe,sequenceCursor:safe,readyTick:safe,commandId:id.nullable(),acceptedTick:safe.nullable(),phase:z.enum(['IDLE','QUEUED','WINDUP','BURST','RECOVERY','TERMINAL']),nextShotIndex:z.number().int().min(0).max(6),lockedTarget:miraTargetIdentitySchema.nullable(),attackSnapshot:exactCombatValueSchema.nullable(),recoveryUntilTick:safe.nullable(),shots:z.array(z.strictObject({emission:miraEmissionSchema,status:z.enum(['IN_FLIGHT','RESOLVED_HIT','RESOLVED_MISS','CLEANED_TERMINAL']),resolution:resolutionSchema.nullable()})).max(1024)});
/** Exact integer logical quantization. No wall-clock/attack-speed/haste contribution. */
export function miraTravelTicks(stepTimeSpanTicks:number){
 if(!Number.isSafeInteger(stepTimeSpanTicks)||stepTimeSpanTicks<1)throw new ActivityError(400,'combat_b_mira_step_invalid');
 return Number((1200000n+BigInt(stepTimeSpanTicks)-1n)/BigInt(stepTimeSpanTicks));
}
function ticks(ms:number,step:number){return Number((BigInt(ms)*10000n+BigInt(step)-1n)/BigInt(step));}
export async function captureBActiveProjectileSource(db:D1Database,current:RefitRead,reference:LocalReference){
 const original=await captureBActiveSource(db,current,reference),step=original.timing.stepTimeSpanTicks;
 const contextId='LOCAL_MIRA_PROJECTILE:'+await sha256Hex(json({version:activeProjectileSourceVersion,originalPacketHash:original.packetHash}));
 const initialTargetIdentity={targetId:original.target.candidateIds[0]!,generation:'LOCAL_MIRA_ENEMY:'+await sha256Hex(json({contextId,targetId:original.target.candidateIds[0]})),battlefieldGeneration:contextId};
 const shotTicks=original.phase.shotOffsetsMs.map(ms=>ticks(ms,step));
 const projectile={version:miraProjectileWireVersion,id:miraProjectileId,effectId:miraImpactEffectId,travelMs:120,travelTicks:miraTravelTicks(step),quantizedTravelMicroseconds:miraTravelTicks(step)*step/10,
 impactDueRule:'ACTUAL_EMITTED_TICK_PLUS_CEIL_120MS_ORIGINAL_STEP',targetIdentityFields:['targetId','generation','battlefieldGeneration'],targetAtEmission:'LOCKED_AT_ACCEPTANCE_RECHECK_ALIVE_RANGE_BATTLEFIELD',targetAtImpact:'SAME_LIVE_ENTITY_AND_BATTLEFIELD_GENERATION_NO_RANGE_OR_CASTER_UP_RECHECK',invalidTarget:'MISS_TARGET_INVALID_NO_RETARGET_NO_HIT_EFFECTS',casterDown:'CANCEL_UNFIRED_KEEP_IN_FLIGHT',snapshotAttack:'FIRST_ACTUAL_SHOT_ONCE_PER_COMMAND',snapshotCarriageModifier:'EACH_ACTUAL_SHOT_FROM_ORIGINAL_B_BUFF_OWNER',defenseReductionShield:'ORIGINAL_CURRENT_TARGET_STATE_AT_IMPACT',damageBps:4500,coefficientApplications:1,critAndOtherEffects:'ORIGINAL_APPROVED_PIPELINE_SOURCES_ONLY',projectileCollision:'NONE_ADDED',terminal:'PRESERVE_EARLIER_RESOLVED_RECEIPTS_CLEAN_REMAINING_WITHOUT_HP_OR_REWARD',eventOrder:'ORIGINAL_AUTHORITY_TICK_SEQUENCE',emissionAtomicity:'ORIGINAL_SHOT_CREATE_AND_RESERVATION_ADVANCE_TOGETHER',impactAtomicity:'ORIGINAL_HP_SHIELD_RECEIPT_AND_SHOT_RESOLUTION_TOGETHER',pauseAndSplit:'ORIGINAL_LOGICAL_CLOCK_DEBT_ONLY',rendererAuthority:false};
 const initial={version:miraProjectileWireVersion,sourcePacketHash:null,contextId,clockTick:0,clockDebtTimeSpanTicks:0,sequenceCursor:0,readyTick:0,commandId:null,acceptedTick:null,phase:'IDLE',nextShotIndex:0,lockedTarget:null,attackSnapshot:null,recoveryUntilTick:null,shots:[]};
 // No circular hash: suffix sourcePacketHash is injected by the consumer from this outer packetHash.
 const payload={version:activeProjectileSourceVersion,provenance:'ISOLATED_LOCAL_FIXTURE',accountId:original.accountId,contextId,initialTargetIdentity,originalActiveSource:original,originalPacketHash:original.packetHash,actor:original.actor,ability:{...original.ability,projectileId:miraProjectileId,effectId:miraImpactEffectId},departure:original.departure,buffInitial:{...original.buffInitial,contextId},projectile,timing:original.timing,phase:{...original.phase,attackSnapshot:'FIRST_ACTUAL_SHOT_ONCE',version:'combat.b-mira-active-projectile-phase.v1.20261003'},timeline:{shotTicks,impactTicks:shotTicks.map(t=>t+projectile.travelTicks),recoveryUntilTick:ticks(original.phase.occupancyMs,step)},initial,
 wireSchemas:{emission:z.toJSONSchema(miraEmissionSchema),suffix:z.toJSONSchema(miraProjectileSuffixSchema)},restore:{version:miraProjectileWireVersion,pinnedOriginalRequired:true,owner:'UNITY_ORIGINAL_COMMAND_CLOCK_READY_BUFF_HP_OWNER',crossCheckWithOriginal:['tick','clockDebtTimeSpanTicks','sequence','Ready','commandId','acceptedTick','phase','Due','lockedTargetIdentity','firstShotAttackSnapshot','perShotModifierSource','nextShotIndex','recoveryUntilTick','shotId','impactDue','originalImpactReceipt'],onPastDue:'REPLAY_UNRESOLVED_ORIGINAL_SEQUENCE_WITHOUT_CURRENT_SOURCE_RECAPTURE',capacity:'1024_LOCAL_SUFFIX_VALIDATION_BOUND_NOT_GAMEPLAY_SHOT_LIMIT'},
 sourceEvidence:[...original.sourceEvidence,{sourceId:'44:mira-basic-active-projectile-approved-20261003',role:'APPROVED_120MS_IMPACT_AND_RESTORE_RULES'}],supplyComplete:true,canExecute:false,startReady:false,remaining:['UNITY_ORIGINAL_B_ACTIVE_ACCEPTANCE_BURST_PROJECTILE_IMPACT_HP_AND_FULL_CHECKPOINT_ADAPTER','OPERATING_MODIFIER_BATTLEFIELD_FULL_RUN_PLAYER_VIEW']};
 return {...payload,packetHash:await digest(payload)};
}
export type BActiveProjectileSource=Awaited<ReturnType<typeof captureBActiveProjectileSource>>;
export async function miraShotId(contextId:string,commandId:string,shotIndex:number){id.parse(contextId);id.parse(commandId);z.number().int().min(0).max(5).parse(shotIndex);return 'MIRA_SHOT:'+await sha256Hex(json({contextId,commandId,shotIndex}));}
/** Deterministic wire reader, not a command acceptance or HP/effect execution authority. */
export async function readMiraEmission(input:unknown,source:BActiveProjectileSource){
 const x=miraEmissionSchema.safeParse(input);if(!x.success)throw new ActivityError(400,'combat_b_mira_emission_invalid');const shot=x.data;
 if(shot.sourcePacketHash!==source.packetHash||shot.contextId!==source.contextId||shot.instanceId!==source.actor.instanceId||shot.slot!==source.actor.slot||json(shot.target)!==json(source.initialTargetIdentity)||shot.emittedTick!==shot.acceptedTick+source.timeline.shotTicks[shot.shotIndex]!||shot.impactDue!==shot.emittedTick+source.projectile.travelTicks||!Number.isSafeInteger(shot.impactDue)||shot.shotId!==await miraShotId(source.contextId,shot.commandId,shot.shotIndex))throw new ActivityError(409,'combat_b_mira_emission_source_conflict');
 let modifierValid=false;
 try{const state=BCarriageBuffRuntime.restore(shot.modifierSnapshot).snapshot();const row=state.actions.find(a=>a.input.eventId===shot.modifierEventId);
 modifierValid=shot.modifierEventId===shot.shotId&&json(state.departure)===json(source.departure)&&json(state.initial)===json(source.buffInitial)&&!!row&&row.input.slot===source.actor.slot&&row.input.kind==='ACTIVE'&&row.input.ability==='ACTIVE'&&row.input.timing==='NONE'&&row.input.target==='ENEMY'&&row.input.direct&&row.input.originalEligible&&await digest(row.source)===shot.modifierSourceHash;
 }catch{/* No unverified modifier substitute. Original per-shot buff proof must restore. */}
 if(!modifierValid)throw new ActivityError(409,'combat_b_mira_modifier_source_conflict');
 return structuredClone(shot);
}
export async function readMiraProjectileSuffix(input:unknown,source:BActiveProjectileSource){
 const parsed=miraProjectileSuffixSchema.safeParse(input);if(!parsed.success)throw new ActivityError(400,'combat_b_mira_restore_invalid');const s=parsed.data;
 const conflict=()=>{throw new ActivityError(409,'combat_b_mira_restore_source_conflict');};
 if(s.sourcePacketHash!==source.packetHash||s.contextId!==source.contextId)conflict();
 const ids=new Set<string>(),ordinals=new Set<number>(),sequenceOwners=new Map<number,string>();let priorSequence=0;
 for(const entry of s.shots){const e=entry.emission;if(sequenceOwners.has(e.sequence))conflict();sequenceOwners.set(e.sequence,"EMIT:"+e.shotId);}
 for(const entry of s.shots){const e=await readMiraEmission(entry.emission,source);if(ids.has(e.shotId)||ordinals.has(e.shotIndex)||e.sequence<=priorSequence||e.sequence>s.sequenceCursor||e.emittedTick>s.clockTick||e.commandId!==s.commandId||e.acceptedTick!==s.acceptedTick||json(e.target)!==json(s.lockedTarget)||json(e.attackSnapshot)!==json(s.attackSnapshot))conflict();ids.add(e.shotId);ordinals.add(e.shotIndex);priorSequence=e.sequence;
 if(entry.status==='IN_FLIGHT'){if(entry.resolution!==null)conflict();}else{
 const r=entry.resolution;if(!r||r.tick>s.clockTick||r.sequence<=e.sequence||r.sequence>s.sequenceCursor||r.tick<e.emittedTick)conflict();
 const owner=r!.result==='CLEANED_TERMINAL'?'TERMINAL:'+r!.tick:'IMPACT:'+e.shotId;const previous=sequenceOwners.get(r!.sequence);if(previous&&previous!==owner)conflict();sequenceOwners.set(r!.sequence,owner);
 if(entry.status==='RESOLVED_HIT'&&(r!.result!=='RESOLVED_HIT'||r!.tick!==e.impactDue||!r!.originalImpactReceiptId))conflict();
 if(entry.status==='RESOLVED_MISS'&&(r!.result!=='MISS_TARGET_INVALID'||r!.tick!==e.impactDue||r!.originalImpactReceiptId!==null))conflict();
 if(entry.status==='CLEANED_TERMINAL'&&(r!.result!=='CLEANED_TERMINAL'||r!.originalImpactReceiptId!==null||s.phase!=='TERMINAL'))conflict();
 }}
 if(s.commandId===null){if(s.acceptedTick!==null||s.lockedTarget!==null||s.attackSnapshot!==null||s.shots.length||s.nextShotIndex!==0||!['IDLE','QUEUED','TERMINAL'].includes(s.phase))conflict();}
 else if(s.acceptedTick===null||s.acceptedTick>s.clockTick||s.readyTick<s.acceptedTick||s.lockedTarget===null||s.nextShotIndex!==s.shots.length||(s.shots.length>0&&s.attackSnapshot===null)||s.shots.some((e,i)=>e.emission.shotIndex!==i))conflict();
 if(s.recoveryUntilTick!==null&&s.recoveryUntilTick<s.acceptedTick!)conflict();
 return structuredClone(s);
}
export function readPinnedBActiveProjectileSource(input:unknown,original:BActiveProjectileSource){let equal=false;try{equal=json(input)===json(original);}catch{/* invalid external JSON */}if(!equal)throw new ActivityError(409,'combat_b_mira_original_source_conflict');return structuredClone(original);}
