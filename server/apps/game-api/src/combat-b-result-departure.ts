import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readBDepartureAssignment,bPreparedDepartureVersion} from './combat-b-departure-assignment';
import {bPlacementVersion} from './combat-b-placement';
import {calculateOwnedOperatorStats} from './combat-owned-profile';
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),positive=nat.min(1);
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const need=(value:boolean):void=>{if(!value)throw new ActivityError(409,'combat_b_result_departure_formation_conflict');};
type Formation={trainId:string;slots:{carriageId:string;operatorId:string|null}[]};
const definitionSchema=z.object({definitionHash:z.string(),originalBase:z.object({growth:z.object({hpPerLevelBps:nat,attackPerLevelBps:nat,skillPerRankBps:nat,promotionHpBps:z.array(nat).length(4),promotionAttackBps:z.array(nat).length(4)})}),operators:z.array(z.object({operatorId:id,profileVersion:id,base:z.object({hp:positive,attack:positive,defense:nat,basicIntervalMs:positive}),skills:z.json(),basicRange:z.json()})),interception:z.object({abilities:z.array(z.object({operatorId:id}).passthrough())})});
/** Original immutable departure authority only. Numeric slot/actor/hash equality is
 * a negative consistency check, never proof of actual B executor/effective binding.
 * The current producer supplies permanent stats and explicitly NO effective profile. */
export async function verifyBResultDeparture(snapshotJson:string,trust:{accountId:string;runId:string},reported:Formation):Promise<string[]>{
 const snapshot=z.record(z.string(),z.json()).parse(JSON.parse(snapshotJson)),admission=z.record(z.string(),z.json()).safeParse(snapshot.admission);
 if(!admission.success)return ['ORIGINAL_DEPARTURE_FORMATION'];
 const raw=admission.data;
 if(raw.version===bPreparedDepartureVersion||Object.hasOwn(raw,'bAssignment')){
  need(raw.version===bPreparedDepartureVersion);
  const a=await readBDepartureAssignment(snapshotJson,trust);need(a!==null);if(!a)throw new ActivityError(409,'combat_b_result_departure_formation_conflict');
  const selections=a.selection.placements,cars=a.refitSnapshot.refit.cars,actors=a.actors;
  need(a.selection.version===bPlacementVersion&&a.selection.mountedTurrets.length===0&&a.refitSnapshot.refit.sourceStatus==='PERSISTED_ACCOUNT_SOURCE'&&a.refitSnapshot.refit.accountId===trust.accountId&&a.refitSnapshot.refit.selectionRevision===a.selectionRevision);
  need(new Set(selections.map(p=>p.slot)).size===selections.length&&new Set(cars.map(c=>c.slot)).size===cars.length&&new Set(cars.map(c=>c.instanceId)).size===cars.length&&cars.length===selections.length);
  need(selections.every(p=>{const c=cars.find(c=>c.slot===p.slot);return !!c&&c.carriageId===p.carriageId&&json(c.occupant)===json(p.occupant);}));
  const selected=selections.flatMap(p=>p.occupant.kind==='OPERATOR'?[p.occupant.operatorId]:[]);
  need(new Set(selected).size===selected.length&&actors.length===selected.length&&new Set(actors.map(v=>v.operatorId)).size===actors.length);
  const definition=z.record(z.string(),z.json()).parse(a.definition),{definitionHash,...body}=definition;
  need(definitionHash===a.definitionHash&&await sha256Hex(json(body))===a.definitionHash);
  const d=definitionSchema.parse(definition);
  for(const actor of actors){const p=selections.find(p=>p.slot===actor.physicalSlot),c=cars.find(c=>c.slot===actor.physicalSlot),original=d.operators.find(o=>o.operatorId===actor.operatorId),intercept=d.interception.abilities.find(i=>i.operatorId===actor.operatorId);
   need(!!p&&p.occupant.kind==='OPERATOR'&&p.occupant.operatorId===actor.operatorId&&!!c&&c.instanceId===actor.carriageInstanceId&&c.carriageId===actor.carriageDefinitionId&&actor.owned.operatorId===actor.operatorId&&actor.definitionHash===a.definitionHash&&!!original&&!!intercept);
   if(!original||!intercept)throw new ActivityError(409,'combat_b_result_departure_formation_conflict');
   const {defense,...base}=original.base;
   need(actor.profileVersion===original.profileVersion&&json(actor.stats)===json(calculateOwnedOperatorStats(base,d.originalBase.growth,actor.owned.level,actor.owned.promotion,defense))&&json(actor.skills)===json(original.skills)&&json(actor.basic)===json(original.basicRange)&&json(actor.interception)===json(intercept)&&json(actor.skillMagnitude)===json({numerator:String(10000+d.originalBase.growth.skillPerRankBps*(actor.owned.skillTraining-1)),denominator:'10000'})&&actor.effectiveStats===null);
  }
  const claimed=reported.slots.flatMap(s=>s.operatorId?[s.operatorId]:[]);
  need(new Set(claimed).size===claimed.length&&claimed.length===selected.length&&claimed.every(o=>selected.includes(o)));
  // Existing native slots have no authoritative physicalSlot/instance revision or
  // assignment/profile provenance. Even matching carriage strings cannot prove it.
  return ['B_RUNTIME_ASSIGNMENT_PROVENANCE','B_RUNTIME_PHYSICAL_PLACEMENT','B_RUNTIME_EFFECTIVE_PROFILE'];
 }
 // Historical A admission behavior is preserved, including missing old snapshots.
 const old=z.object({formation:z.object({operators:z.array(z.object({slot:nat,operatorId:id}))}).passthrough(),loadout:z.object({trainId:id,carriages:z.array(z.object({slot:nat,carriageId:id}))}).passthrough(),ownedState:z.object({operators:z.array(z.object({operatorId:id})),train:z.object({trainId:id})}).passthrough()}).passthrough().safeParse(raw);
 if(!old.success)return ['ORIGINAL_DEPARTURE_FORMATION'];
 const a=old.data;need(a.loadout.trainId===reported.trainId&&a.ownedState.train.trainId===reported.trainId&&a.loadout.carriages.length===reported.slots.length&&a.loadout.carriages.every(c=>reported.slots[c.slot]?.carriageId===c.carriageId)&&a.formation.operators.length===reported.slots.filter(s=>s.operatorId).length&&a.formation.operators.every(o=>reported.slots[o.slot]?.operatorId===o.operatorId&&a.ownedState.operators.some(p=>p.operatorId===o.operatorId)));
 return [];
}