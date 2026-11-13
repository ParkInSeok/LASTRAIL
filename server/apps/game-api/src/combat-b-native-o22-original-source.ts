import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {prepareBDepartureAssignment} from './combat-b-departure-assignment';
import {BCarriageBuffRuntime,resolveBuffDefenseStat} from './combat-b-carriage-buffs';
import {ExactCombatValue} from './combat-exact-value';
import {capturePredepartureEffectApplicability} from './combat-owned-train-profile';
import type {SelectionPrincipal} from './combat-b-selection-store';
import type {BDepartureCombatContext} from './combat-b-departure-combat-pin';
import type {BNativeO22OriginalSourceOwner} from './combat-b-native-o22-executor';

export const bNativeO22OwnedBaseVersion='combat.b-native-o22-owned-base.v1.20261020';
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const digest=(value:unknown)=>sha256Hex(json(value));
const fail=(reason:string):never=>{throw new ActivityError(409,'combat_b_native_o22_'+reason);};
const targets=['STRUCTURE','ATTACHED','CLIMBING','APPROACH','ROOFTOP','ENEMY_TRAIN_BODY','ENEMY_TRAIN_PART','ENEMY'] as const;
type OriginalPrepared=Awaited<ReturnType<BNativeO22OriginalSourceOwner['prepare']>>;

/** Physical/native composition owner is a separate missing producer, not arbitrary
 * JSON input. It must consume this actual owned base, validate original geometry,
 * rail/edge boundaries, seed and all executable/catalog sources, and supply CAS.
 * No ordinary geometry schema/measurements are invented or stored here. */
export interface BNativeO22PhysicalCompositionSource {
 prepare(context:Readonly<BDepartureCombatContext>,owned:Readonly<BNativeO22OwnedBase>):Promise<OriginalPrepared>;
}

/** Builds actual predeparture named/raw values from saved B selection/refit and
 * account growth. Equipment is RUN_OWNED and research has no approved active
 * catalog by original policy; these are explicitly pinned applicability facts,
 * not empty invented ledgers or implicit zero multipliers.
 * Original BCB rules generate conditional projections; they are not accepted
 * attack events and are never baked into raw damage or applied twice at runtime. */
export class PersistedBNativeO22OriginalSourceOwner implements BNativeO22OriginalSourceOwner {
 private readonly auth:SelectionPrincipal;
 constructor(private readonly db:D1Database,auth:SelectionPrincipal,private readonly physical?:BNativeO22PhysicalCompositionSource){this.auth=Object.freeze({...auth});}
 async captureOwnedBase(context:Readonly<BDepartureCombatContext>){
  if(context.accountId!==this.auth.accountId)return fail('account_conflict');
  if(await this.db.prepare('SELECT 1 FROM runs WHERE id=?').bind(context.runId).first())return fail('existing_run_requires_frozen_source');
  const original=await prepareBDepartureAssignment(this.db,this.auth,context.runId),a=original.assignment;
  if(a.actors.length!==1||a.actors[0]!.operatorId!=='op_kyle')return fail('named_scope_unsupported');
  const actor=a.actors[0]!,refit=a.refitSnapshot.refit;
  const applicability=capturePredepartureEffectApplicability();
  // This intake deliberately does not reuse Mira-specific research N/A policies.
  const applicabilityHash=await digest(applicability);
  const rawProfile={actorRef:actor.operatorId,hp:{sourceId:'native-o22-hp:'+a.assignmentHash,kind:'BASE_HP' as const,value:{numerator:String(actor.stats.maxHp),denominator:'1'},riskAppliedVersion:null},
   actions:[{sourceId:'native-o22-basic:'+a.assignmentHash,kind:'RAW_ACTION_DAMAGE' as const,value:structuredClone(actor.stats.attack),riskAppliedVersion:null,progressAppliedVersion:null}]};
  const basicBudgets:Array<{body:'NORMAL'|'BROKEN';target:typeof targets[number];damageBps:number;rawBeforeBcb:typeof actor.stats.attack;afterBcb:typeof actor.stats.attack}>=[];
  const defenseByBody:Array<{body:'NORMAL'|'BROKEN';bcbFlat:number;defense:typeof actor.stats.attack}>=[];
  for(const body of ['NORMAL','BROKEN'] as const){
   // Pure conditions for both possible body states, not an assertion of live state.
   const initial={contextId:'NATIVE_O22_CONDITIONAL_PROJECTION',mode:'EXTERIOR',cars:refit.cars.map(c=>({slot:c.slot,body:c.slot===actor.physicalSlot?body:'NORMAL',actor:c.occupant.kind==='OPERATOR'?'UP':'ABSENT'}))};
   const runtime=new BCarriageBuffRuntime(refit,initial),bcbFlat=runtime.defense(actor.physicalSlot,'OPERATOR');
   defenseByBody.push({body,bcbFlat,defense:resolveBuffDefenseStat({numerator:String(actor.stats.defense),denominator:'1'},0,bcbFlat)});
   for(const target of targets){
    const accepted=runtime.accept({eventId:'PROJECTION:'+body+':'+target,expectedRevision:0,slot:actor.physicalSlot,kind:'BASIC',ability:'BASIC',timing:'NONE',target,direct:true,originalEligible:true});
    const afterBcb=ExactCombatValue.from(actor.stats.attack).multiply(ExactCombatValue.from({numerator:String(10000+accepted.damageBps),denominator:'10000'})).snapshot();
    basicBudgets.push({body,target,damageBps:accepted.damageBps,rawBeforeBcb:structuredClone(actor.stats.attack),afterBcb});
   }
  }
  const body={version:bNativeO22OwnedBaseVersion,accountId:context.accountId,runId:context.runId,assignment:a,assignmentHash:a.assignmentHash,
   actor:{operatorId:actor.operatorId,physicalSlot:actor.physicalSlot,carriageInstanceId:actor.carriageInstanceId,growth:actor.owned,stats:actor.stats,definitionHash:actor.definitionHash,profileVersion:actor.profileVersion},
   applicability,applicabilityHash,commander:{snapshot:a.commander,status:'VERIFIED_NO_ACTIVE_EFFECTS' as const},
   rawProfile,basicBudgets,defenseByBody,
   stage:'PREDEPARTURE_EFFECTIVE_BASE_WITH_CONDITIONAL_BCB_PROJECTIONS' as const,
   runtimeApplication:'ORIGINAL_BCB_OWNER_ONCE_AT_ACTUAL_ACTION_OR_DEFENSE_STATE' as const,
   geometry:null,nativeComposition:null,completeRuntimeSource:false as const};
  const snapshot={...body,sourceHash:await digest(body)};
  return {snapshot,atomicAssertions:original.atomicAssertions};
 }
 async prepare(context:Readonly<BDepartureCombatContext>):Promise<OriginalPrepared>{
  const captured=await this.captureOwnedBase(context);
  if(!this.physical)return fail('geometry_composition_source_unavailable');
  const prepared=await this.physical.prepare(structuredClone(context),structuredClone(captured.snapshot));
  const source=z.object({profile:z.object({definition:z.record(z.string(),z.json())}),rawProfiles:z.array(z.json())}).parse(prepared.capture.source);
  // Composer must pin the exact computed source, not merely claim it consumed it.
  if(json(source.profile.definition.bNativeO22OwnedBase)!==json(captured.snapshot)
   ||!source.rawProfiles.some(raw=>json(raw)===json(captured.snapshot.rawProfile))
   ||json(prepared.capture.namedDefinition)!==json(captured.snapshot.assignment.definition))return fail('owned_composition_conflict');
  return {capture:prepared.capture,assertions:{...prepared.assertions,formation:[...captured.atomicAssertions,...prepared.assertions.formation]}};
 }
}
export type BNativeO22OwnedBase=Awaited<ReturnType<PersistedBNativeO22OriginalSourceOwner['captureOwnedBase']>>['snapshot'];
export function createPersistedBNativeO22OriginalSourceOwner(db:D1Database,auth:SelectionPrincipal,physical?:BNativeO22PhysicalCompositionSource){
 return new PersistedBNativeO22OriginalSourceOwner(db,auth,physical);
}
