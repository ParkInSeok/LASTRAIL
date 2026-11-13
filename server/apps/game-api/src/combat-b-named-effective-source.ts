import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bNamedOperatorRequest,captureBNamedOperatorSource,type BNamedOriginalRefitOwner} from './combat-b-named-operator-source';
import {readOwnedTrainCombatProfile} from './combat-owned-train-profile';
import {readCommanderDeparture} from './commander-departure';
import {readCommanderAuthority} from './commander-preset-store';
export const bNamedEffectiveSourceVersion='combat.b-named-effective-source.v1.20261004';
const hash=z.string().regex(/^[a-f0-9]{64}$/u),json=(v:unknown)=>canonicalJson(z.json().parse(v)),digest=(v:unknown)=>sha256Hex(json(v));
const request=bNamedOperatorRequest.extend({expectedEffectSourceHash:hash.optional()});
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_named_effective_'+code);};
/** Original validated commander authority is read, never given speculative handler support. */
async function commanderSource(db:D1Database,accountId:string){
 const before=await readCommanderAuthority(db,accountId);let snapshot:Awaited<ReturnType<typeof readCommanderDeparture>>['snapshot']=null,status='VERIFIED_NO_ACTIVE_EFFECTS',errorCode:string|null=null;
 try{snapshot=(await readCommanderDeparture(db,accountId)).snapshot;}
 catch(e){if(!(e instanceof ActivityError)||e.code!=='commander_definition_unsupported')throw e;status='UNSUPPORTED_SELECTED_EFFECTS';errorCode=e.code;}
 const after=await readCommanderAuthority(db,accountId);if(json(before)!==json(after))return fail('commander_changed');
 if(snapshot&&Object.keys(snapshot.ranks).length!==0){status='UNSUPPORTED_SELECTED_EFFECTS';errorCode='commander_effect_execution_owner_unavailable';}
 return {owner:'readCommanderDeparture/readCommanderAuthority',status,errorCode,authority:before,snapshot,sourceHash:await digest(before),appliedToBase:false};
}
/** Additive predeparture proof, not a final runtime effect calculator. */
export async function captureBNamedEffectiveSource(db:D1Database,accountId:string,input:unknown,refitOwner?:BNamedOriginalRefitOwner){
 const p=request.safeParse(input);if(!p.success)throw new ActivityError(400,'combat_b_named_effective_request_invalid');const {expectedEffectSourceHash,...namedRequest}=p.data;
 const namedSource=await captureBNamedOperatorSource(db,accountId,namedRequest,refitOwner),lifecycle=await readOwnedTrainCombatProfile(db,accountId,{expectedFormationRevision:namedSource.formationRevision,expectedLoadoutRevision:namedSource.loadoutRevision,expectedSourceHash:namedSource.ownedSourceHash}),commander=await commanderSource(db,accountId);
 // Existing approved applicability belongs to the original intake. A run-owned item
 // is not a missing account predeparture item and not proof of an empty run inventory.
 const applicability={equipment:{...lifecycle.applicability.equipment,items:null,verifiedEmptyRunInventory:false,owner:'EquipmentInventoryStore/EquipmentEffectStateStore'},research:{...lifecycle.applicability.research,modifiers:null},commander};
 const effectSourceHash=await digest(applicability);if(expectedEffectSourceHash&&expectedEffectSourceHash!==effectSourceHash)return fail('source_changed');
 const actors=await Promise.all(namedSource.actors.map(async a=>({actorId:a.actorId,originalActorHash:await digest(a),resolvedPredepartureStats:commander.status==='VERIFIED_NO_ACTIVE_EFFECTS'?a.permanentStats:null,effectiveStats:null,statStage:'PREDEPARTURE_OWNED_BASE_NO_APPLICABLE_RUN_EFFECTS_NOT_RUNTIME',modifierApplications:0,missing:[...(commander.status==='VERIFIED_NO_ACTIVE_EFFECTS'?[]:['ORIGINAL_COMMANDER_EFFECT_EXECUTION_OWNER']),'ORIGINAL_RUNTIME_EFFECTIVE_STAT_OWNER','ACTUAL_RUN_EQUIPPED_ITEMS_EFFECT_BINDING','ORIGINAL_ACCEPTED_INTERCEPTION_TIMING_AND_CLOCK']})));
 const currentCommander=await commanderSource(db,accountId);if(json(currentCommander)!==json(commander))return fail('commander_changed');
 const currentNamed=await captureBNamedOperatorSource(db,accountId,{...namedRequest,expectedOwnedSourceHash:namedSource.ownedSourceHash},refitOwner);if(currentNamed.packetHash!==namedSource.packetHash)return fail('named_changed');
 if(json(await commanderSource(db,accountId))!==json(commander))return fail('commander_changed');
 const body={version:bNamedEffectiveSourceVersion,accountId,namedSource,originalNamedPacketHash:namedSource.packetHash,applicability,effectSourceHash,refitProviderStatus:refitOwner?'ORIGINAL_PERSISTED_PROVIDER_BOUND':'OPERATING_PROVIDER_UNAVAILABLE',actors,runtimeSupported:false as const,canStart:false as const};return schema.parse(bounded({...body,packetHash:await digest(body)}));
}
const stats=z.strictObject({maxHp:z.number().int().positive().safe(),attack:z.strictObject({numerator:z.string().regex(/^[0-9]+$/u),denominator:z.string().regex(/^[1-9][0-9]*$/u)}),defense:z.number().int().nonnegative().safe(),basicIntervalMs:z.number().int().positive().safe()});
const actor=z.strictObject({actorId:z.enum(['op_kyle','op_sable']),originalActorHash:hash,resolvedPredepartureStats:stats.nullable(),effectiveStats:z.null(),statStage:z.literal('PREDEPARTURE_OWNED_BASE_NO_APPLICABLE_RUN_EFFECTS_NOT_RUNTIME'),modifierApplications:z.literal(0),missing:z.array(z.string().min(1).max(128)).max(10)});
const schema=z.strictObject({version:z.literal(bNamedEffectiveSourceVersion),accountId:z.string().min(1).max(200),namedSource:z.json(),originalNamedPacketHash:hash,applicability:z.json(),effectSourceHash:hash,refitProviderStatus:z.enum(['ORIGINAL_PERSISTED_PROVIDER_BOUND','OPERATING_PROVIDER_UNAVAILABLE']),actors:z.array(actor).min(1).max(2).refine(a=>new Set(a.map(v=>v.actorId)).size===a.length),runtimeSupported:z.literal(false),canStart:z.literal(false),packetHash:hash});
function bounded(input:unknown){const t=json(input);if(new TextEncoder().encode(t).length>768*1024)return fail('capacity');return JSON.parse(t) as unknown;}
export type BNamedEffectiveSource=Awaited<ReturnType<typeof captureBNamedEffectiveSource>>;
export async function readPinnedBNamedEffectiveSource(input:unknown,original:BNamedEffectiveSource,expectedOriginalHash:unknown){const h=hash.safeParse(expectedOriginalHash),p=schema.safeParse(bounded(input));if(!h.success||!p.success)return fail('packet_invalid');const {packetHash,...body}=p.data;if(h.data!==original.packetHash||packetHash!==h.data||await digest(body)!==h.data||json(p.data)!==json(original))return fail('original_packet_conflict');return structuredClone(p.data);}