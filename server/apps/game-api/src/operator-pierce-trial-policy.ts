import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {capturePierceDefinition} from './operator-pierce-definition';
export const pierceLocalTrialPolicyVersion='combat.pierce-local-trial-policy.v1.20261016';
export const pierceLocalTrialProfile='mira-pierce-comparison-local.v1.20261016';
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
/** Explicit new policy, not a mutation of an old Mira/EN17 packet or an executable context.
 * A complete source/context writer still needs formal effect identity/ownership and checkpoint binding. */
export async function capturePierceLocalTrialPolicy(){
 const definition=await capturePierceDefinition();
 const body={version:pierceLocalTrialPolicyVersion,profile:pierceLocalTrialProfile,environment:'LOCAL',
  approval:'31#pierce-local-trial-policy-approved-20261016',formalAugmentId:null,
  logicalDefinitionVersion:definition.version,logicalDefinitionHash:definition.definitionHash,
  actors:['op_mira'],abilityId:'op_mira.basic',research:{status:'NOT_APPLICABLE',ledgerSubstitution:'FORBIDDEN'},
  critical:{status:'NOT_APPLICABLE',chanceBps:null,rngDraws:0},
  groups:[{key:'A',pierceSource:'VERIFIED_ABSENT',en17AppliedStar:null,en17CoefficientBps:null},
   {key:'B',pierceSource:'VERIFIED_FORMAL_OWNED_SELECTION',en17AppliedStar:null,en17CoefficientBps:null},
   {key:'C',pierceSource:'VERIFIED_ABSENT',en17AppliedStar:2,en17CoefficientBps:2500},
   {key:'D',pierceSource:'VERIFIED_FORMAL_OWNED_SELECTION',en17AppliedStar:2,en17CoefficientBps:2500}],
  originalSources:'PRESERVE_ACCOUNT_RUN_FORMATION_GROWTH_MAP_SEED_AND_PACKET_HASH',
  staticStats:'ORIGINAL_SUPPORTED_EQUIPMENT_AND_SETS_NO_UNSUPPORTED_EFFECT_SUPPRESSION',
  ownBasic:'ORIGINAL_FIRE_OWNER_ONCE_NOT_PREAPPLIED',completedDamageInput:'FORBIDDEN',
  comparison:'CONTROL_BASE_STATS_PLACEMENT_DEFENSE_CLOCK_AND_ORIGINAL_OPPORTUNITIES_SEPARATE_EFFECT_HASHES',
  contextBinding:'EXPLICIT_PROFILE_POLICY_HASH_ORIGINAL_SOURCE_HASHES_AND_CHECKPOINT_REQUIRED',
  excluded:['OPERATING_START','ORDINARY_COOP','MULTIPLE_OPERATORS','O08','O10','EN17_STAR_3_TO_5'],
  effectOwnershipEstablished:false as const,runtimeSupported:false as const,canStart:false as const};
 return {...body,policyHash:await sha256Hex(json(body))};
}
export type PierceLocalTrialPolicy=Awaited<ReturnType<typeof capturePierceLocalTrialPolicy>>;
export async function readPierceLocalTrialPolicy(raw:unknown):Promise<PierceLocalTrialPolicy>{
 const expected=await capturePierceLocalTrialPolicy();
 try{if(json(raw)===json(expected))return expected;}catch{/* Reject malformed data, never default to an exemption. */}
 throw Error('operator_pierce_trial_policy_mismatch');
}
