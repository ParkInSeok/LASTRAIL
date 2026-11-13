import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureMiraEffectiveSource} from './combat-b-mira-effective-source';
import type {MiraBasicSource} from './combat-b-mira-basic-source';

export const miraFirstTestProfile='mira-first-no-effect-test.v1.20261015';
export const miraFirstTestSourceVersion='combat.mira-first-test-static-source.v1.20261015';
const requestProfile=z.object({profile:z.literal(miraFirstTestProfile)}).passthrough();
const hash=z.string().regex(/^[a-f0-9]{64}$/u),account=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const json=(value:unknown)=>canonicalJson(z.json().parse(value)),digest=(value:unknown)=>sha256Hex(json(value));
function fail(code:string):never{throw new ActivityError(409,'combat_mira_first_test_'+code);}

/** Explicit, additive first-test policy. Never rewrites generic unknown research,
 * historical packets or a run's START admission. The component owner validates all
 * remaining request fields, actual slots, departure provenance and read coherence. */
export async function captureMiraFirstTestSource(db:D1Database,accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown){
  const parsed=requestProfile.safeParse(input);
  if(!parsed.success)throw new ActivityError(400,'combat_mira_first_test_request_invalid');
  const {profile,...request}=parsed.data;
  const originalComponent=await captureMiraEffectiveSource(db,accountId,request,originalMira,expectedMiraHash).catch((error:unknown)=>{
    if(error instanceof ActivityError&&error.code==='combat_mira_effective_request_invalid')throw new ActivityError(400,'combat_mira_first_test_request_invalid');
    throw error;
  });
  const unsupported:string[]=[];
  // This is the implemented support subset, not a new gameplay ban on equipment.
  // Preserve known core values but do not claim complete set/timing/unique composition.
  if(!originalComponent.equipment.equippedEmpty||originalComponent.equipment.slots.some(slot=>slot!==null))unsupported.push('FULL_EQUIPMENT_EFFECT_COMPOSITION_UNAVAILABLE');
  if(originalComponent.equipment.blocked.length)unsupported.push('UNSUPPORTED_EQUIPMENT_COMPONENT');
  const commander=originalComponent.commander.snapshot;
  if(commander!==null){
    const ranks=z.object({ranks:z.record(z.string(),z.number())}).safeParse(commander);
    if(!ranks.success||Object.keys(ranks.data.ranks).length)unsupported.push('COMMANDER_EFFECT_COMPOSITION_UNAVAILABLE');
  }
  const core=originalComponent.coreStats,permanent=originalComponent.originalMira.actor.owned.permanentStats;
  if(!core||!permanent)unsupported.push('ORIGINAL_STATIC_STATS_UNAVAILABLE');
  const effectiveStats=unsupported.length===0&&core&&permanent?{
    maxHp:core.maxHp,attack:core.attack,defense:core.defense,basicIntervalMs:permanent.basicIntervalMs,
  }:null;
  const body={version:miraFirstTestSourceVersion,profile,accountId,runId:originalComponent.runId,
    originalComponent,originalComponentHash:originalComponent.packetHash,
    research:{status:'NOT_APPLICABLE',modifiers:null,ledgerEvidence:null,
      policyVersion:'mira-first-test-research-na-20261015',
      policySource:'44_COMBAT_DETAIL_CONTRACTS.md#mira-first-test-research-na-20261015',
      reason:'APPROVED_RESEARCH_INACTIVE_FOR_THIS_FIRST_NO_EFFECT_MIRA_TEST_ONLY'},
    configuration:{support:'VERIFIED_EMPTY_MIRA_SLOTS_AND_NO_ACTIVE_COMMANDER_EFFECTS_ONLY',
      inventoryHash:originalComponent.equipment.inventoryHash,inventoryRevision:originalComponent.equipment.inventoryRevision,
      departurePinHash:originalComponent.departurePinHash,commanderSourceHash:originalComponent.commander.sourceHash},
    effectiveStats,staticStatsReady:effectiveStats!==null,unsupported,
    statStage:'FIRST_TEST_STATIC_BEFORE_DYNAMIC_COMBAT_EFFECTS',
    passiveApplication:'ORIGINAL_COMPONENT_OWN_BASIC_UNAPPLIED_FIRE_OWNER_ONCE',
    missing:['ACTUAL_GEOMETRY_AND_NATIVE_FIRE_HP_CHECKPOINT_BINDING','ORIGINAL_FIRE_D_CRITICAL_OPPORTUNITY_AND_TARGET_SPECIFIC_EFFECTS',...unsupported],
    startBinding:'EXPLICIT_INTERNAL_PROFILE_NOT_AN_OPERATING_START_ADMISSION',
    runtimeSupported:false as const,canStart:false as const};
  const packet={...body,packetHash:await digest(body)};
  if(new TextEncoder().encode(json(packet)).length>1024*1024)return fail('capacity');
  return structuredClone(packet);
}
export type MiraFirstTestSource=Awaited<ReturnType<typeof captureMiraFirstTestSource>>;
/** Frozen policy/source restore with an independently trusted original and hash.
 * Does not apply today's policy or re-query current gear, growth, geometry or defense. */
export async function readPinnedMiraFirstTestSource(input:unknown,original:MiraFirstTestSource,expectedHash:unknown,accountId:string):Promise<MiraFirstTestSource>{
  if(!hash.safeParse(expectedHash).success||!account.safeParse(accountId).success)return fail('packet_invalid');
  let candidate:string;try{candidate=json(input);}catch{return fail('packet_invalid');}
  if(new TextEncoder().encode(candidate).length>1024*1024)return fail('capacity');
  const {packetHash,...body}=original;
  if(original.accountId!==accountId||packetHash!==expectedHash||await digest(body)!==packetHash||candidate!==json(original))return fail('original_packet_conflict');
  return structuredClone(original);
}