import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {combatDamageReceiptSchema} from './combat-damage-store';
const id=z.string().min(1).max(200);
const contextSchema=z.strictObject({accountId:id,runId:id});
/** Internal projection of an ALREADY authenticated command result/original receipt.
 * No new lookup after success. Context must come from that command's authority owner.
 * Schema/hash integrity does not authenticate caller-supplied data, establish O22
 * ownership, resolve logical actor generations, or authorize HP changes. Never mount
 * this as a public receipt-to-heal endpoint. No healing or journal write occurs here.
 */
export async function captureLifestealDamageEvidence(context:unknown,input:unknown){
 const scope=contextSchema.parse(context),receipt=combatDamageReceiptSchema.parse(input);
 const recordedSourceActorId='actorCharge' in receipt?receipt.actorCharge.actorId:null;
 const body={version:'combat.lifesteal-damage-evidence.v1.20261019',...scope,
  eventId:receipt.eventId,targetId:receipt.targetId,confirmedHpLoss:receipt.hpLoss,
  recordedSourceActorId,attributionStatus:recordedSourceActorId===null?'MISSING_ATTRIBUTION_NOT_SHARED_PROOF':'RECORDED_ACTOR_REQUIRES_LOGICAL_OWNER_PROOF',
  receipt,canApplyHealing:false as const,
  missingProofs:['ORIGINAL_NODE_AND_LOGICAL_ACTOR_GENERATION','ORIGINAL_ACTION_PARENT_ATTRIBUTION',
   'PINNED_APPLIED_O22_OWNERSHIP','ORIGINAL_HEAL_MODIFIER_AND_LIVING_OWNER','ATOMIC_DAMAGE_TO_HEAL_DEDUP_BINDING']};
 return {...body,evidenceHash:await sha256Hex(canonicalJson(body))};
}
