import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
export const operatorPierceDefinitionVersion='operator-pierce-reservation.v3.20261016';
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
/** Approved logical procedure only. Formal catalog ID/owned binding/trial policy are separate decisions.
 * Does not execute Unity geometry, contact, damage, proc or reward ownership. */
export async function capturePierceDefinition(){
 const body={version:operatorPierceDefinitionVersion,
  approval:'31#pierce-reservation-v3-approved-20261016',compositionApproval:'31#operator-pierce-composition-v2-20261015',
  formalAugmentId:null,ownedSource:null,trialPolicy:null,
  scope:'OWN_ELIGIBLE_RANGED_PROJECTILE_BASIC_ONLY',firstIntegrationCandidate:'op_mira.basic',
  procedure:{pathsPerOriginalOpportunity:1,maxNewRearReservations:2,
   reservedCoefficients:[{numerator:'3',denominator:'5'},{numerator:'3',denominator:'10'}],
   damageInput:'ORIGINAL_WHOLE_BODY_PRE_DEFENSE_D_AFTER_O15_O08_O20',attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT_ONCE_FINAL_FLOOR',
   coefficientAssignment:'RESERVATION_RANK_FIXED_NOT_CONTACT_ORDER',missingShare:'NO_MERGE_NO_PROMOTION_NO_REPLACEMENT',
   ray:'ORIGINAL_MUZZLE_TO_PRIMARY_HIT_ANCHOR_FIXED',rearAnchor:'STRICTLY_BEYOND_LAST_ORIGINAL_BODY_ANCHOR',
   firstContact:'NOT_BEFORE_PRIMARY_FIRST_CONTACT',reservationOrder:'COLLISION_DISTANCE_THEN_STABLE_ID',
   targetIdentity:'ID_AND_GENERATION_SHARED_HP_DEDUP',flight:'SAME_ORIGINAL_PROJECTILE_CUMULATIVE_DISTANCE_TIME',
   geometry:'ORIGINAL_SPACE_CAPSULE_RADIUS_RANGE_AND_OBSTRUCTION_NO_DEFAULTS',
   primaryMiss:'CONTINUE_IF_ORIGINAL_PROJECTILE_ALIVE',physicalTermination:'STOP_REMAINING_PATH',
   compositionOrder:['ORIGINAL_BODY','PIERCE_RESERVATION','EXPANDED_EXCLUSION','EN17_SECONDARY'],
   o08:'PRESERVE_ORIGINAL_BODY_AND_BUDGET_ONE_PRIMARY_PATH',o10:'PRESERVE_ORIGINAL_START_TIMING_SINGLE_PATH',
   derivativePaths:0,newOpportunities:0,newCriticalDraws:0,automaticEN02EN13EN14Triggers:0,
   originalBodyProc:'PRESERVED',killReward:'ORIGINAL_OWNER_ONCE_DERIVATIVE_NOT_DIRECT'},
  catalogPolicy:{approvedLogicalCount:41,train:20,operator:21,candidateWeight:'UNIFORM_PER_ELIGIBLE_CARD',
   typePreDraw:false,freeRerollsPerCard:3,extraRerollsPerCard:1,existingIssuedCandidates:'PRESERVE_ORIGINAL_SNAPSHOT',operatingCatalogPublished:false},
  runtimeSupported:false as const,canStart:false as const};
 return {...body,definitionHash:await sha256Hex(json(body))};
}
export type OperatorPierceDefinition=Awaited<ReturnType<typeof capturePierceDefinition>>;
/** Current logical-publication reader, not a historical START reader. Reject rehashed policy changes. */
export async function readPierceDefinition(raw:unknown):Promise<OperatorPierceDefinition>{
 const expected=await capturePierceDefinition();
 try{if(json(raw)===json(expected))return expected;}catch{/* Invalid external JSON is rejected below. */}
 throw Error('operator_pierce_definition_mismatch');
}
