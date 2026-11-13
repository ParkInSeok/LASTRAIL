import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOwnedCombatProfile} from './combat-owned-profile';
import {captureOwnedCombatBaseDefinitions} from './combat-owned-base-definitions';
import {captureCombatPolicyRequirements} from './combat-policy-catalog';
import {publishCombatDefinitionContract} from './combat-definition-contract';
import {readCommanderDeparture} from './commander-departure';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
import type {RefitRead} from './combat-b-local-flow-source';

export const miraBasicDefinitionVersion='mira-basic-ballistics-v1.20261015';
export const miraBasicSourceVersion='combat.mira-basic-source.v1.20261015';
export const miraBasicAbilityId='op_mira.basic';
const id=z.string().min(1).max(128),hash=z.string().regex(/^[a-f0-9]{64}$/u),revision=z.number().int().positive().safe();
const requestSchema=z.strictObject({expectedFormationRevision:revision,expectedLoadoutRevision:revision,
  expectedOwnedSourceHash:z.string().regex(/^sha256:[a-f0-9]{64}$/u).optional(),expectedDefinitionHash:hash.optional()});
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const digest=(value:unknown)=>sha256Hex(json(value));
function fail(code:string):never{throw new ActivityError(409,'combat_mira_basic_'+code);}

/** Additive BASIC definition. Existing ACTIVE/ultimate/old definitions are never replaced. */
export async function miraBasicDefinition(){
  const base=captureOwnedCombatBaseDefinitions(),policy=captureCombatPolicyRequirements(),registry=await publishCombatDefinitionContract();
  if(registry.definition.operators.some(actor=>actor.abilities.some(ability=>ability.id===miraBasicAbilityId)))return fail('registry_conflict');
  const passive=policy.passives.find(p=>p.operatorId==='op_mira');
  if(!passive||passive.source!=='OWN_BASIC'||passive.group!=='NON_EQUIPMENT_OUTGOING'||!('valueBps' in passive)||passive.valueBps!==1000)return fail('passive_definition_changed');
  const body={version:miraBasicDefinitionVersion,scope:'NEW_B_EXPERIMENTAL_MIRA_PROJECTILE_BASIC_ONLY',approval:'44#friendly-basic-ballistics-detail-v1-20261015',
    originalBase:base,originalBaseHash:await digest(base),originalPolicy:policy,originalPolicyHash:await digest(policy),
    registrySource:{contentHash:registry.contentHash,collisionCheck:'ORIGINAL_PUBLISHED_ABILITY_INDEX'},
    ability:{id:miraBasicAbilityId,operatorId:'op_mira',kind:'BASIC',attackForm:'PROJECTILE',
      attackCoefficient:{numerator:'1',denominator:'1'},originalBasicIntervalMs:base.operators.op_mira.basicIntervalMs,
      opportunityPolicy:'ONE_ORIGINAL_BASIC_OPPORTUNITY_NO_NEW_BURST',
      range:{physicalAdjacentCars:2,targetCategory:'ENEMY',maximumDistanceMm:24000,geometryRequired:true,
        rule:'INTERSECTION_WITH_ORIGINAL_DOMAIN_LOS_AND_TARGET_ELIGIBILITY'}},
    ballistics:{speedMmPerSecond:48000,radiusMm:60,maximumFlightDistanceMm:24000,maximumFlightMs:500,lengthUnitMm:12000,
      trajectory:'FIXED_DIRECTION_NON_HOMING',attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'},
    passive:structuredClone(passive),passiveApplication:'ORIGINAL_BASIC_FIRE_OWNER_ONCE_NOT_APPLIED_TO_PERMANENT_STATS',
    runtimeSupported:false as const,canStart:false as const};
  return {...body,definitionHash:await digest(body)};
}
/** Borrow the original persisted placement/refit owner; no client-carriage assertions. */
export interface MiraBasicRefitOwner{read(accountId:string):Promise<RefitRead>;}

/** Read-only owned-source producer, not an atomic START or runtime effective-stat owner.
 * Missing equipment/research/scene sources remain unknown instead of empty modifiers. */
export async function captureMiraBasicSource(db:D1Database,accountId:string,input:unknown,refitOwner?:MiraBasicRefitOwner){
  const parsed=requestSchema.safeParse(input);
  if(!parsed.success||!id.safeParse(accountId).success)throw new ActivityError(400,'combat_mira_basic_request_invalid');
  const req=parsed.data,profile=await readOwnedCombatProfile(db,accountId,{expectedFormationRevision:req.expectedFormationRevision,
    expectedLoadoutRevision:req.expectedLoadoutRevision,...(req.expectedOwnedSourceHash?{expectedSourceHash:req.expectedOwnedSourceHash}:{})});
  const owned=profile.operators.find(actor=>actor.operatorId==='op_mira');
  if(!owned)throw new ActivityError(403,'combat_mira_basic_not_selected');
  const definition=await miraBasicDefinition();
  if((req.expectedDefinitionHash&&req.expectedDefinitionHash!==definition.definitionHash)
    ||profile.definitionHash!=='sha256:'+definition.originalBaseHash||profile.policyHash!=='sha256:'+definition.originalPolicyHash)return fail('definition_changed');
  const commander=await readCommanderDeparture(db,accountId),refit=refitOwner?await refitOwner.read(accountId):null;
  const departure=refit?.refit?buffDepartureSchema.parse(refit.refit):null;
  if(refit&&(refit.eligibilityError||!departure||departure.accountId!==accountId
    ||refit.selectionRevision!==departure.selectionRevision||refit.rankSource.revision!==departure.rankRevision))return fail('refit_conflict');
  if(departure&&departure.cars.some(car=>{const occupant=car.occupant;return occupant.kind==='OPERATOR'&&!profile.operators.some(actor=>actor.operatorId===occupant.operatorId);}))return fail('refit_operator_conflict');
  const car=departure?.cars.find(car=>car.occupant.kind==='OPERATOR'&&car.occupant.operatorId==='op_mira')??null;
  if(departure&&!car)return fail('refit_operator_not_placed');
  // Preserve original read coherence across every awaited borrowed owner. No commit/START claim.
  if(refitOwner&&refit&&json(await refitOwner.read(accountId))!==json(refit))return fail('refit_changed');
  if(json(await readCommanderDeparture(db,accountId))!==json(commander))return fail('commander_changed');
  await readOwnedCombatProfile(db,accountId,{expectedFormationRevision:profile.formationRevision,
    expectedLoadoutRevision:profile.loadoutRevision,expectedSourceHash:profile.sourceHash});
  const missing=[...profile.missing,'EFFECTIVE_STATS_AFTER_ORIGINAL_MODIFIERS','ACTUAL_MUZZLE_HIT_SHAPE_AND_GEOMETRY',
    'ORIGINAL_FIRE_OPPORTUNITY_D_CRITICAL_AND_BODY_TARGETS','MODE_COMBAT_LIFECYCLE_HP_AND_CHECKPOINT_BINDING'];
  if(!car)missing.push('ORIGINAL_B_PLACEMENT_AND_BCB_REFIT');
  const body={version:miraBasicSourceVersion,accountId,definition,definitionHash:definition.definitionHash,
    ownedProfile:profile,ownedProfileHash:await digest(profile),ownedSourceHash:profile.sourceHash,
    formationRevision:profile.formationRevision,loadoutRevision:profile.loadoutRevision,
    refit:refit?{revision:refit.revision,sourceHash:refit.sourceHash,definitionHash:refit.definitionHash,departure}:null,
    actor:{actorId:'op_mira',operatorId:'op_mira',abilityId:miraBasicAbilityId,owned,
      placement:car?{physicalSlot:car.slot,carriageInstanceId:car.instanceId,carriageDefinitionId:car.carriageId}:null,
      bcb:car?{type:car.type,rank:car.rank,rankRevision:departure!.rankRevision,
        selectionRevision:departure!.selectionRevision,sourceStatus:departure!.sourceStatus}:null,nativeSupported:false as const},
    modifierSources:{equipment:profile.equipment,research:profile.research,commander:commander.snapshot,
      commanderSource:commander,passive:definition.passive,application:'MODE_COMBAT_OWNER_REQUIRED_NOT_EFFECTIVE_STATS'},
    geometry:null,sourceStatus:'VERIFIED_OWNED_SELECTION_READ_ONLY_CANDIDATE_NOT_START',missing,
    runtimeSupported:false as const,canStart:false as const};
  const result={...body,packetHash:await digest(body)};
  if(new TextEncoder().encode(json(result)).length>512*1024)return fail('capacity');
  return structuredClone(result);
}
export type MiraBasicSource=Awaited<ReturnType<typeof captureMiraBasicSource>>;
/** Independently trusted frozen original/hash only, never client self-rehashed approval.
 * Exact canonical equality rejects additional fields and altered nested content. No current
 * definition or owned read occurs during restoration of an original captured source. */
export async function readPinnedMiraBasicSource(input:unknown,original:MiraBasicSource,expectedOriginalHash:unknown,accountId:string):Promise<MiraBasicSource>{
  if(!hash.safeParse(expectedOriginalHash).success||!id.safeParse(accountId).success)return fail('packet_invalid');
  let candidate:string;
  try{candidate=json(input);}catch{return fail('packet_invalid');}
  if(new TextEncoder().encode(candidate).length>512*1024)return fail('capacity');
  const {packetHash,...body}=original;
  if(original.accountId!==accountId||packetHash!==expectedOriginalHash||await digest(body)!==packetHash||candidate!==json(original))return fail('original_packet_conflict');
  return structuredClone(original);
}