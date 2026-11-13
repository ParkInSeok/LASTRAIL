import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
import {readOwnedCombatProfile} from './combat-owned-profile';
import {publishCombatDefinitionContract} from './combat-definition-contract';
import type {RefitRead,LocalReference} from './combat-b-local-flow-source';
export const activeSourceVersion='combat.b-active-source.v1.20261003';
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=async(x:unknown)=>'sha256:'+await sha256Hex(json(x));
/** Trusted isolated supplier inputs only. Runtime command/Ready/HP ownership stays in Unity. */
export async function captureBActiveSource(db:D1Database,current:RefitRead,reference:LocalReference){
 if(!current.refit||current.eligibilityError)throw new ActivityError(409,'combat_b_active_refit_ineligible');
 const departure=buffDepartureSchema.parse(current.refit),cars=[...departure.cars].sort((a,b)=>a.slot-b.slot);
 if(cars.some((c,i)=>c.slot!==i))throw new ActivityError(409,'combat_b_active_geometry_unavailable');
 const operators=cars.filter(c=>c.occupant.kind==='OPERATOR');
 if(operators.length!==1||operators[0]!.occupant.kind!=='OPERATOR'||operators[0]!.occupant.operatorId!=='op_mira')throw new ActivityError(503,'combat_b_active_actor_unsupported');
 const car=operators[0]!,profile=await readOwnedCombatProfile(db,departure.accountId,{expectedFormationRevision:1,expectedLoadoutRevision:1});
 const stat=profile.operators.find(o=>o.operatorId==='op_mira')?.permanentStats;
 if(!stat)throw new ActivityError(503,'combat_b_active_owned_stats_unavailable');
 const definition=await publishCombatDefinitionContract();
 const original=definition.definition.operators.find(o=>o.operatorId==='op_mira')?.abilities.find(a=>a.id==='op_mira.active');
 if(!original||original.effect!=='FIXED_TARGET_BURST'||original.cooldownMs!==10000||original.shots!==6||original.intervalMs!==200||original.windupMs!==300||original.recoveryMs!==300||original.damageBps!==4500)throw new ActivityError(503,'combat_b_active_definition_unsupported');
 // Frozen LOCAL reference: validate its claimed original bytes AND normalized contents.
 // A changed reference needs a separately reviewed supplier version; rehashing does not approve it.
 let referenceValid=false;try{referenceValid=reference.sha256==='73807c72d099ad0814000896bb2472abdd71e4add154f71a1c9871cc4b021819'&&await digest(reference.template)==='sha256:b0d9865eeba8bf4949d2172baf4e025b87b785c7fe89baceb89c895ed50b42ba';}catch{/* Invalid reference is explicitly unavailable. */}
 if(!referenceValid)throw new ActivityError(503,'combat_b_active_reference_unsupported');
 const t=reference.template,enemy=t.entities.filter(e=>e.kind==='LOCAL_ENEMY');
 if(t.stepTimeSpanTicks!==10000||enemy.length!==1||t.initialShields.length!==0)throw new ActivityError(503,'combat_b_active_reference_unsupported');
 const get=(kind:string)=>{const e=t.entities.find(x=>x.kind===kind);if(!e)throw new ActivityError(503,'combat_b_active_reference_unsupported');return structuredClone(e);};
 const op=get('OPERATOR');op.id='op_mira';op.definitionId='op_mira';op.slot=car.slot;op.maxHp=stat.maxHp;op.attack=structuredClone(stat.attack);op.defense={numerator:String(stat.defense),denominator:'1'};op.basicIntervalMicroseconds=stat.basicIntervalMs*1000;
 // The original reference has explicitly empty external arithmetic. Never import a BASIC passive into ACTIVE.
 if([op.attackSources,op.defenseSources,op.effects,op.weakening,op.penetration,op.reductions.equipment,op.reductions.guardStance,op.reductions.independent].some(x=>x.length))throw new ActivityError(503,'combat_b_active_modifiers_unavailable');
 const train=get('TRAIN');train.id=profile.selectedTrain.trainId;
 const entities=[train,...cars.map(c=>({...get('CARRIAGE'),id:c.instanceId,slot:c.slot})),op,structuredClone(enemy[0]!)];
 const geometry=t.geometry.find(g=>g.sourceId==='op_mira'&&g.targetId===enemy[0]!.id);
 if(!geometry||!geometry.eligible||geometry.distanceMm>geometry.maximumDistanceMm||Math.abs(car.slot-enemy[0]!.slot)>2)throw new ActivityError(409,'combat_b_active_target_unavailable');
 const actor={actorId:'op_mira',operatorId:'op_mira',instanceId:car.instanceId,carriageId:car.carriageId,slot:car.slot,stats:structuredClone(stat)};
 const ability={...structuredClone(original),kind:'ACTIVE',timing:'TIME_COOLDOWN'};
 const ownedProfileHash=await digest(profile),runtimeInput={version:'combat.b-active-runtime-input.v1.20261003',provenance:'ISOLATED_LOCAL_FIXTURE',formation:{version:'unity.combat-b.formation-local.v1',trainId:train.id,mode:'Solo',battlefield:'ExternalVertical',internalNodeId:null,slots:cars.map(c=>({carriageId:c.instanceId,operatorId:c.occupant.kind==='OPERATOR'?c.occupant.operatorId:null}))},entities,geometry:[{...structuredClone(geometry),sourceId:actor.actorId,sourceSlot:car.slot}],initialShields:[]};
 const runtimeInputHash=await digest(runtimeInput),contextId='LOCAL_ACTIVE:'+await sha256Hex(json({accountId:departure.accountId,refitRevision:current.revision,sourceHash:current.sourceHash,runtimeInputHash}));
 const buffInitial={contextId,mode:'EXTERIOR',cars:cars.map(c=>({slot:c.slot,body:'NORMAL',actor:c.occupant.kind==='OPERATOR'?'UP':'ABSENT'}))};
 const payload={version:activeSourceVersion,provenance:'ISOLATED_LOCAL_FIXTURE',accountId:departure.accountId,selectionRevision:current.selectionRevision,refitRevision:current.revision,rankRevision:current.rankSource.revision,sourceHash:current.sourceHash,refitDefinitionHash:current.definitionHash,departure,definition,ownedProfile:profile,ownedProfileHash,actor,ability,runtimeInput,runtimeInputHash,contextId,buffInitial,
 timing:{version:'unity.combat-b.active-timing-local.v1',stepTimeSpanTicks:t.stepTimeSpanTicks,firstReadyMs:definition.definition.policy.actionRules.firstActiveReadyMs,generalHaste:[],cooldownReduction:[],en08:null,modifierProvenance:'EXPLICIT_EMPTY_LOCAL_EXTERNAL_INPUTS_NOT_OPERATING_PROFILE',carriageSource:'ORIGINAL_BCB02_AT_ACCEPTANCE',readyOwner:'ORIGINAL_UNITY_READY_AND_FIXED_STEP_DEBT',cooldownAt:'CAST_ACCEPTANCE',pause:'ORIGINAL_GAMEPLAY_CLOCK',preserveAcrossNodes:true},
 target:{category:'ENEMY',candidateIds:[enemy[0]!.id],initialFocusId:enemy[0]!.id,lockAt:'CAST_ACCEPTANCE',range:'APPROVED_MIRA_BASIC_OWN_AND_ADJACENT_2_ELIGIBLE_PARALLEL',localGeometryOnly:true,recheck:'EVERY_SHOT_ALIVE_RANGE_BATTLEFIELD',retarget:false},
 phase:{version:'combat.b-mira-active-phase.v1.20261003',states:['IDLE','QUEUED','WINDUP','BURST','RECOVERY','TERMINAL'],shotOffsetsMs:[300,500,700,900,1100,1300],occupancyMs:1600,queue:'ORIGINAL_SHARED_ACTOR_QUEUE',autoPriority:'ULTIMATE_BEFORE_ACTIVE',attackSnapshot:'FIRST_EFFECT_ONCE',shotCommit:'ONCE_PER_COMMAND_AND_ORDINAL',invalidTarget:'CANCEL_ALL_UNFIRED_THEN_RECOVERY_300MS',casterDown:'CANCEL_UNFIRED_NO_CD_REFUND',hitOnlyCancel:false,focusChange:'KEEP_LOCKED_TARGET',committedProjectile:'PRESERVE_ORIGINAL_TARGET',damage:'ORIGINAL_DIRECT_SKILL_DEF_SHIELD_PIPELINE_ONCE_PER_SHOT',castTrigger:'ONCE_PER_ACCEPTANCE_NOT_PER_SHOT',basicCharge:false,basicDuringRecovery:false,shortenBasicReady:false,fixedIntervalsUseAttackSpeed:false},
 initial:{version:'combat.b-active-initial.v1.20261003',contextId,tick:0,readyTick:0,phase:'IDLE',commands:[],committedShots:[],reservationCursor:0,attackSnapshot:null,lockedTargetId:null},
 runtimeOwnership:{acceptance:'UNITY_ORIGINAL_COMMAND_OWNER',commandIdentity:'ORIGINAL_COMMAND_ID_ACTOR_ABILITY',order:'ORIGINAL_TICK_AND_SEQUENCE',restore:'UNITY_ORIGINAL_CHECKPOINT_PLUS_VERSIONED_ACTIVE_SUFFIX',requiredRestoreFields:['packetHash','contextId','actorId','instanceId','abilityId','tick','readyTick','commandId','sequence','dueTick','phase','lockedTargetId','attackSnapshot','reservationCursor','committedShotOrdinals','recoveryUntilTick','originalBuffSnapshot','originalClockDebt'],ongoingSnapshotSupplied:false},
 sourceEvidence:[...structuredClone(t.sourceEvidence),{sourceId:'REFERENCE_ONLY:AttackConsumer.local-original-input',sha256:reference.sha256},{sourceId:'44:six-skills-detail-v02',role:'APPROVED_PHASE_AND_NUMBERS'},{sourceId:'44:combat-b-operator-role-approved-v05-20261002',role:'APPROVED_MIRA_BASIC_RANGE'}],
 preparedFor:'UNITY_LOCAL_MIRA_ACTIVE_SOURCE_CONSUMPTION',canExecute:false,startReady:false,remaining:['UNITY_ORIGINAL_B_ACTOR_ACTIVE_EFFECT_QUEUE_AND_CHECKPOINT_ADAPTER','OPERATING_MODIFIER_PROFILE_AND_MULTI_TARGET_BATTLEFIELD_PRODUCER','ORIGINAL_PROJECTILE_TRAVEL_SOURCE_IF_CONSUMER_REQUIRES_PROJECTILES','FULL_RUN_START_PLAYER_VIEW']};
 return {...payload,packetHash:await digest(payload)};
}
export type BActiveSourcePacket=Awaited<ReturnType<typeof captureBActiveSource>>;
/** Internal restore port receives the trusted frozen departure, never a freshly republished source.
 * Rehashing untrusted input is insufficient; require exact equality to its original captured packet.
 */
export function readPinnedBActiveSource(input:unknown,original:BActiveSourcePacket):BActiveSourcePacket{
 let equal=false;try{equal=json(input)===json(original);}catch{/* Invalid external JSON remains rejected. */}
 if(!equal)throw new ActivityError(409,'combat_b_active_original_packet_conflict');
 return structuredClone(original);
}
