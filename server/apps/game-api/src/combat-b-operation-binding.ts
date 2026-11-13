import {bCompletedProgressVersion} from './combat-b-completed-progress';
import {readCombatAuthority} from './combat-authority';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureOperationMap} from './operation-map-plan';
import {capturePublishedOperationMap} from './operation-map-publisher';
import {bInterceptAbilitySourceVersion} from './combat-b-intercept-ability';
export const bOperationContentVersion='act1.opn01.b12.content.v1.20261004';
export const bOperationBindingVersion='operation.b-story12-binding.v1.20261004';
export const bOperationProfileVersion='opn_01_last_departure.b12.profile.v1.20261004';
const digest=z.string().regex(/^[a-f0-9]{64}$/u),id=z.string().min(1).max(200);
const bindingSchema=z.strictObject({version:z.literal(bOperationBindingVersion),operationId:z.literal('opn_01_last_departure'),contentVersion:z.literal(bOperationContentVersion),profileVersion:z.literal(bOperationProfileVersion),mode:z.literal('STORY_B_12'),sourceVersion:z.literal(bInterceptAbilitySourceVersion),mapContentHash:digest,rewardReferenceHash:digest,catalogRevision:z.number().int().positive().safe(),nodeWaves:z.array(z.strictObject({nodeId:id,waves:z.number().int().min(0).max(128).nullable(),originalContractVersion:id.nullable()})).min(1).max(78),bindingHash:digest});
export type BOperationBinding=z.infer<typeof bindingSchema>;
export function bOperationFail(code:string):never{throw new ActivityError(409,'combat_b_operation_'+code);}
/** Immutable version binding; publication is metadata, never an execution capability. */
export async function createBOperationBinding(publication:unknown,rewardReference:unknown,catalogRevision:number){
 const raw=z.record(z.string(),z.json()).parse(publication),plan=captureOperationMap(raw.plan);
 const content=await capturePublishedOperationMap(publication,plan);
 const nodeWaves=plan.nodes.map(n=>{const e=content.encounters.find(e=>e.ref===n.encounterProfileRef)!;
  if(n.eventKind==='COMBAT')return {nodeId:n.nodeId,waves:2,originalContractVersion:bOperationBindingVersion};
  if(n.eventKind!=='ELITE'&&n.eventKind!=='BOSS')return {nodeId:n.nodeId,waves:0,originalContractVersion:bOperationBindingVersion};
  // Elite/boss wave semantics must come from their immutable original definition.
  const original=z.strictObject({version:id,completedWaves:z.number().int().min(0).max(128)}).safeParse(e.definition.completedWaveContract);
  return {nodeId:n.nodeId,waves:original.success?original.data.completedWaves:null,originalContractVersion:original.success?original.data.version:null};});
 const base={version:bOperationBindingVersion,operationId:'opn_01_last_departure',contentVersion:bOperationContentVersion,profileVersion:bOperationProfileVersion,mode:'STORY_B_12',sourceVersion:bInterceptAbilitySourceVersion,mapContentHash:content.contentHash,rewardReferenceHash:await sha256Hex(canonicalJson(z.json().parse(rewardReference))),catalogRevision,nodeWaves};
 return bindingSchema.parse({...base,bindingHash:await sha256Hex(canonicalJson(base))});
}
/** Restore only original bytes. Never resolve today's catalog or accept a client mode. */
export async function readBOperationBinding(input:unknown):Promise<BOperationBinding|null>{
 const s=z.record(z.string(),z.json()).parse(input),newVersion=s.contentVersion===bOperationContentVersion;
 if(!newVersion&&!Object.hasOwn(s,'bOperationBinding')){if(typeof s.contentVersion==='string'&&s.contentVersion.startsWith('act1.opn01.b12.'))return bOperationFail('version_unsupported');return null;}
 const parsed=bindingSchema.safeParse(s.bOperationBinding);if(!parsed.success)return bOperationFail('binding_invalid');const b=parsed.data;
 if(!newVersion||s.operationId!==b.operationId||s.encounterType!=='ORDINARY_12_NODE')return bOperationFail('binding_conflict');
 const admission=z.record(z.string(),z.json()).parse(s.admission),authority=readCombatAuthority(admission.combatAuthority);
 if(authority.contract?.mode!=='ORDINARY_SOLO')return bOperationFail('mode_unsupported');
 const p=await capturePublishedOperationMap(s.operationMapContent,admission.operationMapPlan);
 const rebuilt=await createBOperationBinding(p,s.rewardReference,b.catalogRevision);
 if(canonicalJson(rebuilt)!==canonicalJson(b))return bOperationFail('binding_conflict');return b;
}
/** Internal catalog registration only; no migration seeds or public write endpoint. */
export class BOperationCatalog {
 constructor(private readonly db:D1Database){}
 async read(){const row=await this.db.prepare('SELECT c.revision,c.publication_json FROM b_operation_catalog_active a JOIN b_operation_catalog c ON c.revision=a.revision WHERE a.singleton_id=1').first<{revision:number;publication_json:string}>();if(!row)return bOperationFail('catalog_unavailable');
  const raw=JSON.parse(row.publication_json),plan=captureOperationMap(raw.plan),publication=await capturePublishedOperationMap(raw,plan);return {revision:row.revision,publication};}
 guard(value:{revision:number;publication:unknown}){return this.db.prepare('INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM b_operation_catalog_active a JOIN b_operation_catalog c ON c.revision=a.revision WHERE a.singleton_id=1 AND c.revision=? AND c.publication_json=?) THEN 1 ELSE 0 END').bind(value.revision,canonicalJson(z.json().parse(value.publication)));}
}
export const bOperationRuntime={runtimeSupported:false,canStart:false,missing:['APPROVED_NAMED_GEOMETRY','ALL_REACHABLE_NODE_EXECUTORS','ORIGINAL_V3_SOURCE_START_CAS'],defeatObjectiveSupported:true,objectiveProgressPolicyVersion:bCompletedProgressVersion} as const;