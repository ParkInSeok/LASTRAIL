import {capturePredepartureEffectApplicability} from './combat-owned-train-profile';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {BSelectionStore,type SelectionPrincipal} from './combat-b-selection-store';
import {BRefitStore} from './combat-b-refit-store';
import {PersistedBSelectionCatalog,PersistedBRefitSource,bPhysicalSourceQuery,bPersistedSourceVersion} from './combat-b-persisted-sources';
import {bNamedOperatorDefinition} from './combat-b-named-operator-source';
import {calculateOwnedOperatorStats} from './combat-owned-profile';
import {readCommanderDeparture,commanderDepartureGuards} from './commander-departure';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
import {bPlacementRequestSchema} from './combat-b-placement';
import {prepareBDepartureCombatSnapshot,readBDepartureCombatPin,readBDepartureCombatPinForRun,type BDepartureCombatSourceOwner} from './combat-b-departure-combat-pin';
export const bDepartureAssignmentVersion='combat.b-departure-assignment.v1.20261006';
export const bPreparedDepartureVersion='departure-b-preparation-v1.20261006';
const json=(value:unknown)=>canonicalJson(z.json().parse(value)),digest=(value:unknown)=>sha256Hex(json(value));
const id=z.string().min(1).max(200),rev=z.number().int().positive().safe(),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const operator=z.strictObject({operatorId:id,skillTraining:z.number().int().min(1).max(5),promotion:z.number().int().min(0).max(3),revision:rev});
const training=z.strictObject({slot:z.number().int().min(0).max(4),operatorId:id,level:z.number().int().min(1).max(50),revision:rev});
const physical=z.strictObject({instanceId:id,carriageId:id,revision:rev});
const growthSchema=z.strictObject({operators:z.array(operator),training:z.array(training),physical:z.array(physical)});
const growthQuery=`SELECT json_object(
 'operators',json((SELECT json_group_array(json_object('operatorId',operator_id,'skillTraining',skill_training,'promotion',promotion,'revision',revision)) FROM (SELECT * FROM account_operators WHERE account_id=?1 ORDER BY operator_id))),
 'training',json((SELECT json_group_array(json_object('slot',slot_index,'operatorId',operator_id,'level',trained_level,'revision',revision)) FROM (SELECT * FROM operator_training_core_slots WHERE account_id=?1 ORDER BY slot_index))),
 'physical',json((SELECT value FROM (${bPhysicalSourceQuery})))) AS value`;
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_assignment_'+code);};
const exact=z.strictObject({numerator:z.string().regex(/^[0-9]+$/u),denominator:z.string().regex(/^[1-9][0-9]*$/u)});
const actorSchema=z.strictObject({operatorId:z.enum(['op_kyle','op_sable']),physicalSlot:z.number().int().min(0).max(4),carriageInstanceId:id,carriageDefinitionId:id,physicalRevision:rev,profileVersion:id,definitionHash:hash,
 owned:operator.extend({level:z.number().int().min(1).max(50),trainingSourceHash:hash}),stats:z.strictObject({maxHp:rev,attack:exact,defense:z.number().int().nonnegative().safe(),basicIntervalMs:rev}),skillMagnitude:exact,skills:z.json(),basic:z.json(),interception:z.json(),effectiveStats:z.null()});
const schema=z.strictObject({version:z.literal(bDepartureAssignmentVersion),sourceVersion:z.literal(bPersistedSourceVersion),accountId:id,runId:id,selectionRevision:rev,catalog:z.strictObject({version:id,revision:rev,status:z.literal('PERSISTED_ACCOUNT_SOURCE')}),selection:bPlacementRequestSchema,
 refitSnapshot:z.strictObject({revision:rev,definitionHash:hash,sourceHash:hash,refit:buffDepartureSchema}),growthSourceHash:hash,commander:z.json(),definition:z.json(),definitionHash:hash,actors:z.array(actorSchema).min(1).max(2),
 applicability:z.json(),predepartureStatsIncluded:z.literal(true),runtimeBindingIncluded:z.literal(false),effectiveCombatStatsIncluded:z.literal(false),canStart:z.literal(false)});
/** Actual account sources, independent of A formation/loadout. No issuance or START. */
export async function prepareBDepartureAssignment(db:D1Database,auth:SelectionPrincipal,runId:string){
 const catalog=new PersistedBSelectionCatalog(),selection=await new BSelectionStore(db,catalog).captureForDeparture(auth),refit=await new BRefitStore(db,new PersistedBRefitSource(),catalog).captureForDeparture(auth);
 if(selection.catalog.status!=='PERSISTED_ACCOUNT_SOURCE'||refit.refit.sourceStatus!=='PERSISTED_ACCOUNT_SOURCE'||refit.refit.selectionRevision!==selection.selectionRevision||json(refit.refit.cars.map(({slot,carriageId,occupant})=>({slot,carriageId,occupant})))!==json(selection.selection.placements))return fail('selection_conflict');
 const selected=selection.selection.placements.flatMap(p=>p.occupant.kind==='OPERATOR'?[p.occupant.operatorId]:[]);
 if(selected.some(o=>o!=='op_kyle'&&o!=='op_sable'))return fail('operator_unsupported');
 const row=await db.prepare(growthQuery).bind(auth.accountId).first<{value:string}>();let state:z.infer<typeof growthSchema>;
 try{state=growthSchema.parse(JSON.parse(row?.value??'null'));}catch{return fail('growth_invalid');}
 // Original five training-core completeness is a growth rule, not a five-selected-operator requirement.
 if(state.training.length!==5||new Set(state.training.map(t=>t.slot)).size!==5||new Set(state.training.map(t=>t.operatorId)).size!==5||state.training.some(t=>!state.operators.some(o=>o.operatorId===t.operatorId)))return fail('training_unavailable');
 const definition=await bNamedOperatorDefinition(),commander=await readCommanderDeparture(db,auth.accountId);
 if(commander.snapshot&&Object.keys(commander.snapshot.ranks).length>0)return fail('commander_effects_unsupported');
 const trainingSourceHash=await digest(state.training);
 const actors=selected.map(operatorId=>{const owned=state.operators.find(o=>o.operatorId===operatorId),original=definition.operators.find(o=>o.operatorId===operatorId)!,car=refit.refit.cars.find(c=>c.occupant.kind==='OPERATOR'&&c.occupant.operatorId===operatorId)!;
  if(!owned)return fail('operator_not_owned');const instance=state.physical.find(i=>i.instanceId===car.instanceId&&i.carriageId===car.carriageId);if(!instance)return fail('physical_unavailable');
  const level=state.training.find(t=>t.operatorId===operatorId)?.level??Math.min(...state.training.map(t=>t.level)),{defense,...base}=original.base;
  return {operatorId,physicalSlot:car.slot,carriageInstanceId:car.instanceId,carriageDefinitionId:car.carriageId,physicalRevision:instance.revision,profileVersion:original.profileVersion,definitionHash:definition.definitionHash,owned:{...owned,level,trainingSourceHash},stats:calculateOwnedOperatorStats(base,definition.originalBase.growth,level,owned.promotion,defense),skillMagnitude:{numerator:String(10000+definition.originalBase.growth.skillPerRankBps*(owned.skillTraining-1)),denominator:'10000'},skills:original.skills,basic:original.basicRange,interception:definition.interception.abilities.find(a=>a.operatorId===operatorId)!,effectiveStats:null};
 });
 const body=schema.parse({version:bDepartureAssignmentVersion,sourceVersion:bPersistedSourceVersion,accountId:auth.accountId,runId,selectionRevision:selection.selectionRevision,catalog:selection.catalog,selection:selection.selection,refitSnapshot:{revision:refit.revision,definitionHash:refit.definitionHash,sourceHash:refit.sourceHash,refit:refit.refit},growthSourceHash:await digest(state),commander:commander.snapshot,definition,definitionHash:definition.definitionHash,actors,applicability:{...capturePredepartureEffectApplicability(),commander:{status:'VERIFIED_NO_ACTIVE_EFFECTS'},runtimeEffects:'NOT_PROVEN'},predepartureStatsIncluded:true,runtimeBindingIncluded:false,effectiveCombatStatsIncluded:false,canStart:false});
 const assignment={...body,assignmentHash:await digest(body)};
 return {assignment,atomicAssertions:[...selection.atomicAssertions,...refit.atomicAssertions,...commanderDepartureGuards(db,auth.accountId,commander),db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN (SELECT value FROM (${growthQuery}))=?2 THEN 1 ELSE 0 END`).bind(auth.accountId,row!.value)]};
}
export async function preparePersistedBDepartureSnapshot(db:D1Database,auth:SelectionPrincipal,runId:string,baseJson:string,owner:BDepartureCombatSourceOwner){
 const base=z.record(z.string(),z.json()).parse(JSON.parse(baseJson));if(base.runId!==runId||Object.hasOwn(base,'bCombatPin'))return fail('snapshot_conflict');
 const old=z.record(z.string(),z.json()).parse(base.admission),response=z.record(z.string(),z.json()).parse(base.departureResponse);
 if(Object.hasOwn(old,'bAssignment')||Object.hasOwn(base,'bAssignment'))return fail('snapshot_conflict');
 const captured=await prepareBDepartureAssignment(db,auth,runId);
 const admission={version:bPreparedDepartureVersion,combatAuthority:old.combatAuthority,operationMapPlan:old.operationMapPlan,commanderTraits:captured.assignment.commander,bAssignment:captured.assignment};
 const next={...base,admission,departureResponse:{...response,admission}};
 const prepared=await prepareBDepartureCombatSnapshot(auth.accountId,runId,JSON.stringify(next),owner);
 return {...prepared,atomicAssertions:[...captured.atomicAssertions,...prepared.atomicAssertions]};
}
export async function readBDepartureAssignment(snapshotJson:string,trust:{accountId:string;runId:string;expectedPinHash?:string}){
 const base=z.record(z.string(),z.json()).parse(JSON.parse(snapshotJson));const pin=await readBDepartureCombatPin(snapshotJson,trust);
 const admission=z.record(z.string(),z.json()).parse(base.admission),raw=admission.bAssignment;if(raw===undefined)return null;
 if(!pin||admission.version!==bPreparedDepartureVersion)return fail('snapshot_conflict');
 const parsed=schema.extend({assignmentHash:hash}).safeParse(raw);if(!parsed.success)return fail('snapshot_conflict');
 const {assignmentHash,...body}=parsed.data;if(body.accountId!==trust.accountId||body.runId!==trust.runId||assignmentHash!==await digest(body))return fail('snapshot_conflict');
 return {...parsed.data,origin:pin.origin,departurePinHash:pin.pinHash,runtimeProfileHash:pin.runtimeProfileHash};
}
export async function readBDepartureAssignmentForRun(db:D1Database,accountId:string,runId:string){
 const pin=await readBDepartureCombatPinForRun(db,accountId,runId),row=await db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?').bind(runId).first<{snapshot_json:string}>();
 if(!row||await sha256Hex(row.snapshot_json)!==pin.startSnapshotHash)return fail('source_stale');
 const original=await readBDepartureAssignment(row.snapshot_json,{accountId,runId});if(!original)return fail('source_unavailable');
 const body={...original,startSnapshotHash:pin.startSnapshotHash},packet={...body,packetHash:await digest(body)};
 if(json(await readBDepartureCombatPinForRun(db,accountId,runId))!==json(pin))return fail('source_stale');return packet;
}