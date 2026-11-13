import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {prepareOwnedCombatProfile} from './combat-owned-profile';
import {readCommanderDeparture,commanderDepartureGuards} from './commander-departure';
import {captureOwnedCombatBaseDefinitions} from './combat-owned-base-definitions';
import {captureCombatPolicyRequirements} from './combat-policy-catalog';
import {captureBNamedOperatorSource,bNamedOperatorDefinition} from './combat-b-named-operator-source';
import {equipmentCoreStatVersion,equipmentRunStatOriginSchema,type OriginalRunEquipmentStatContext} from './equipment-run-stat-owner';
export const equipmentDeparturePinVersion='equipment.departure-base-pin.v1.20261005';
const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),digest=(v:unknown)=>sha256Hex(json(v));
function fail(code:string):never{throw new ActivityError(409,'equipment_departure_'+code);}
const admissionSchema=z.object({version:id,formationRevision:z.number().int().positive().safe(),loadoutRevision:z.number().int().positive().safe(),formation:z.object({operators:z.array(z.object({slot:z.number().int(),operatorId:id}))}),ownedState:z.object({operators:z.array(z.object({operatorId:id,skillTraining:z.number().int(),promotion:z.number().int(),revision:z.number().int()})),trainingCoreSlots:z.array(z.object({slot:z.number().int(),operatorId:id,trainedLevel:z.number().int(),revision:z.number().int()}))})});
const pinSchema=z.strictObject({version:z.literal(equipmentDeparturePinVersion),origin:equipmentRunStatOriginSchema,originHash:hash,operationPreviewId:id,operationId:id,risk:z.enum(['I','II','III','IV','V']),admissionHash:hash,source:z.json(),sourceHash:hash,definitions:z.json(),definitionHash:hash,pinHash:hash});
export type EquipmentDeparturePin=z.infer<typeof pinSchema>;
export interface EquipmentDepartureSource {prepare(accountId:string,runId:string,snapshotJson:string):Promise<{snapshotJson:string;response:Record<string,unknown>;assertions:D1PreparedStatement[]}>;}
/** Captures actual pre-equipment permanent operator bases at NEW departure only.
 * Original raw ownership/growth SQL is fenced in the caller's run-creation batch. */
export class RunEquipmentDepartureProducer implements EquipmentDepartureSource {
 constructor(private readonly db:D1Database){}
 async prepare(accountId:string,runId:string,snapshotJson:string){
  id.parse(accountId);id.parse(runId);const base=z.record(z.string(),z.json()).parse(JSON.parse(snapshotJson));
  if(base.runId!==runId||Object.hasOwn(base,'equipmentDeparturePin'))return fail('context_invalid');
  const response=z.record(z.string(),z.json()).parse(base.departureResponse);
  if(Object.hasOwn(response,'equipmentDeparturePin')||Object.entries(response).some(([k,v])=>json(v)!==json(base[k])))return fail('context_invalid');
  const admission=admissionSchema.parse(base.admission),owned=await prepareOwnedCombatProfile(this.db,accountId,{expectedFormationRevision:admission.formationRevision,expectedLoadoutRevision:admission.loadoutRevision}),profile=owned.profile;
  if(profile.operators.length!==5||admission.ownedState.operators.length!==5||json(admission.formation.operators)!==json(profile.operators.map(p=>({slot:p.slot,operatorId:p.operatorId}))))return fail('selection_conflict');
  const commander=await readCommanderDeparture(this.db,accountId);if(commander.snapshot&&Object.keys(commander.snapshot.ranks).length)return fail('modifier_source_unavailable');if(json(commander.snapshot)!==json((base.admission as Record<string,unknown>).commanderTraits))return fail('selection_conflict');
  const selectedCoreSources=profile.operators[0]!.sources.training.filter(c=>profile.operators.some(p=>p.operatorId===c.operatorId));if(admission.ownedState.trainingCoreSlots.length!==selectedCoreSources.length||new Set(admission.ownedState.trainingCoreSlots.map(c=>c.slot)).size!==selectedCoreSources.length)return fail('selection_conflict');
  const requested=profile.operators.filter(p=>p.operatorId==='op_kyle'||p.operatorId==='op_sable').map(p=>p.operatorId);
  const named=requested.length?await captureBNamedOperatorSource(this.db,accountId,{operatorIds:requested,expectedFormationRevision:profile.formationRevision,expectedLoadoutRevision:profile.loadoutRevision,expectedOwnedSourceHash:profile.sourceHash}):null;
  const actors=profile.operators.map(p=>{
   const n=named?.actors.find(a=>a.operatorId===p.operatorId),stats=n?.permanentStats??p.permanentStats;
   if(!stats||p.level===null)return fail('base_unavailable');
   const o=admission.ownedState.operators.find(o=>o.operatorId===p.operatorId);if(!o||o.promotion!==p.promotion||o.skillTraining!==p.skillTraining||o.revision!==p.sources.operator.revision)return fail('selection_conflict');
   for(const c of admission.ownedState.trainingCoreSlots){const source=p.sources.training.find(s=>s.operatorId===c.operatorId&&s.sourceId.endsWith(':'+c.slot));if(!source||source.revision!==c.revision||source.level!==c.trainedLevel)return fail('selection_conflict');}
   return {wearerKind:'operator' as const,wearerId:p.operatorId,targetId:p.operatorId,base:{maxHp:stats.maxHp,attack:stats.attack,defense:{numerator:String(stats.defense),denominator:'1'}}};
  });
  const definitions={ownedBase:captureOwnedCombatBaseDefinitions(),policy:captureCombatPolicyRequirements(),named:await bNamedOperatorDefinition()};
  if(profile.definitionHash!=='sha256:'+await digest(definitions.ownedBase)||profile.policyHash!=='sha256:'+await digest(definitions.policy))return fail('definition_changed');
  const origin=equipmentRunStatOriginSchema.parse({version:equipmentCoreStatVersion,accountId,runId,stage:'ORIGINAL_PRE_EQUIPMENT_RUN_BASE',actors}),source={ownedProfile:profile,namedSource:named,commanderSnapshot:commander.snapshot,stage:'VERIFIED_PRE_EQUIPMENT_PERMANENT_BASE',targetIdentity:'ORIGINAL_SELECTED_OPERATOR_ACTOR_ID',runtimeSupported:false};
  const payload={version:equipmentDeparturePinVersion,origin,originHash:await digest(origin),operationPreviewId:base.operationPreviewId,operationId:base.operationId,risk:base.risk,admissionHash:await digest(base.admission),source,sourceHash:await digest(source),definitions,definitionHash:await digest(definitions)};
  const pin=pinSchema.parse({...payload,pinHash:await digest(payload)});if(new TextEncoder().encode(json(pin)).length>512*1024)return fail('capacity');
  const updated={...response,equipmentDeparturePin:pin};return {snapshotJson:JSON.stringify({...base,equipmentDeparturePin:pin,departureResponse:updated}),response:updated,assertions:[...owned.assertions,...commanderDepartureGuards(this.db,accountId,commander)]};
 }
}
/** Trusted original SQL or independent expected hash; no current owned/definition lookup. */
export async function readEquipmentDeparturePin(snapshotJson:string,trust:{accountId:string;runId:string;expectedPinHash?:string}){
 const t=z.strictObject({accountId:id,runId:id,expectedPinHash:hash.optional()}).parse(trust),base=z.record(z.string(),z.json()).parse(JSON.parse(snapshotJson)),response=z.record(z.string(),z.json()).parse(base.departureResponse);
 if(base.equipmentDeparturePin===undefined||base.equipmentDeparturePin===null){if(response.equipmentDeparturePin!==undefined&&response.equipmentDeparturePin!==null)return fail('pin_conflict');return null;}
 if(new TextEncoder().encode(json(base.equipmentDeparturePin)).length>512*1024)return fail('capacity');
 const p=pinSchema.parse(base.equipmentDeparturePin),{pinHash,...body}=p;
 if(p.origin.accountId!==t.accountId||p.origin.runId!==t.runId||base.runId!==t.runId||p.operationPreviewId!==base.operationPreviewId||p.operationId!==base.operationId||p.risk!==base.risk||json(p)!==json(response.equipmentDeparturePin)||json(base.admission)!==json(response.admission)||response.runId!==base.runId||response.operationPreviewId!==base.operationPreviewId||response.operationId!==base.operationId||response.risk!==base.risk||p.admissionHash!==await digest(base.admission)||p.sourceHash!==await digest(p.source)||p.definitionHash!==await digest(p.definitions)||p.originHash!==await digest(p.origin)||pinHash!==await digest(body)||(t.expectedPinHash!==undefined&&t.expectedPinHash!==pinHash))return fail('pin_conflict');
 return structuredClone(p);
}
export interface EquipmentSafeMaintenanceOwner {
 /** Original runtime owner must prove paused combat/travel, eligible maintenance or
  * safe event and exact revision in this batch. No operating owner exists yet. */
 prepare(accountId:string,runId:string):Promise<{assertions:D1PreparedStatement[]}>;
}
/** Original persisted producer pin only; cannot backfill legacy from current ownership. */
export class PinnedRunEquipmentStatContext implements OriginalRunEquipmentStatContext {
 constructor(private readonly db:D1Database,private readonly maintenance?:EquipmentSafeMaintenanceOwner){}
 async prepare(accountId:string,runId:string){
  const row=await this.db.prepare('SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?').bind(runId,accountId).first<{status:string;snapshot_json:string}>();
  if(!row)return fail('run_not_found');const pin=await readEquipmentDeparturePin(row.snapshot_json,{accountId,runId});if(!pin)return fail('legacy_source_unavailable');
  if(row.status!=='ACTIVE')return fail('run_not_active');if(!this.maintenance)return fail('maintenance_source_unavailable');
  const safe=await this.maintenance.prepare(accountId,runId);if(!safe?.assertions?.length)return fail('maintenance_source_unavailable');
  return {origin:pin.origin,originalHash:pin.originHash,assertions:[...safe.assertions,this.db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=?) THEN 1 ELSE 0 END`).bind(runId,accountId,row.snapshot_json)]};
 }
}