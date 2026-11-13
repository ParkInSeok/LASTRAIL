import {EquipmentEffectStateStore} from './equipment-effect-state';
import type {EquipmentPlacementStore} from './equipment-placement';
import {multishotCatalogBinding} from './equipment-multishot-catalog';
import {calculateEquipmentCoreStats,equipmentCoreStatVersion} from './equipment-run-stat-owner';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureMiraEffectiveSource,type MiraEffectiveSource} from './combat-b-mira-effective-source';
import type {MiraBasicSource} from './combat-b-mira-basic-source';
import {parseMultishotDefinition,selectHighestMultishot,multishotDefinitionVersion,type MultishotDefinition} from './equipment-multishot-definition';

export const equippedMultishot12SourceVersion='equipment.multishot-equipped-12-source.v1.20261015';
const hash=z.string().regex(/^[a-f0-9]{64}$/u),id=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const requestExtension=z.object({expectedBindingHash:hash.optional()}).passthrough();
const bindingSchema=z.strictObject({version:z.literal('equipment.multishot-isolated-binding.v1.20261015'),
  sourceStatus:z.literal('ISOLATED_LOCAL_FIXTURE'),effectId:z.string().regex(/^EN(0[1-9]|1[0-6])$/u),
  definitionVersion:z.string().startsWith('LOCAL_').max(128),catalogVersion:z.string().startsWith('LOCAL_').max(128),
  logicalDefinitionVersion:z.literal(multishotDefinitionVersion),bindingHash:hash});
/** Historical isolated fixture port only. Formal EN17 uses the separate code-owned binding.
 * This port must never receive a public/client-provided catalog or alias live EN02 semantics. */
export interface IsolatedMultishotBindingSource{read():Promise<unknown>;}
const json=(value:unknown)=>canonicalJson(z.json().parse(value)),digest=(value:unknown)=>sha256Hex(json(value));
function fail(code:string):never{throw new ActivityError(409,'equipment_multishot_'+code);}
async function readBinding(source?:IsolatedMultishotBindingSource){
  const raw=source?await source.read():null;if(raw===null)return null;
  const result=bindingSchema.safeParse(raw);if(!result.success)return fail('binding_invalid');
  const {bindingHash,...body}=result.data;if(await digest(body)!==bindingHash)return fail('binding_invalid');
  return result.data;
}
const metadataSchema=z.strictObject({original_hash:hash,payload_hash:hash,source_ref:id,snapshot_json:z.string()});

async function resolveSlots(db:D1Database,originalComponent:MiraEffectiveSource,inputSlots:MiraEffectiveSource['equipment']['slots'],binding:{effectId:string;definitionVersion:string}|null,formal:boolean,initialBlocked:Array<{instanceId:string;reason:string}>){
  const blocked:Array<{instanceId:string;reason:string}>=initialBlocked.map(value=>({...value}));
  const candidates:Array<{instanceId:string;definitionSnapshot:MultishotDefinition}>=[];
  const slots=await Promise.all(inputSlots.map(async slot=>{
    if(slot===null)return null;
    const {instance,effect}=slot;
    if(formal&&(!['HP','ATK','DEF'].includes(instance.category_id)||instance.definition_version!==equipmentCoreStatVersion))blocked.push({instanceId:slot.instanceId,reason:'UNSUPPORTED_CORE_CATEGORY_OR_DEFINITION'});
    if(instance.star>2)blocked.push({instanceId:slot.instanceId,reason:'EQUIPMENT_STAR_OUTSIDE_APPROVED_TRIAL'});
    if(!effect){blocked.push({instanceId:slot.instanceId,reason:'effect_source_missing'});return {...slot,currentStar:instance.star,originalHash:null,payloadHash:null,sourceRef:null};}
    const raw=await db.prepare(`SELECT l.original_hash,l.payload_hash,l.source_ref,l.snapshot_json FROM run_equipment_effect_heads h JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision JOIN run_equipment_instances e ON e.run_id=h.run_id AND e.instance_id=h.instance_id WHERE h.run_id=? AND h.instance_id=? AND h.revision=? AND e.revision=?`)
      .bind(originalComponent.runId,slot.instanceId,effect.effectRevision,instance.revision).first();
    const metadata=metadataSchema.safeParse(raw);if(!metadata.success)return fail('effect_evidence_invalid');
    let saved:unknown;try{saved=JSON.parse(metadata.data.snapshot_json);}catch{return fail('effect_evidence_invalid');}
    if(json(saved)!==json(effect.snapshot))return fail('effect_evidence_changed');
    if(effect.snapshot.uniqueEffects.length)blocked.push({instanceId:slot.instanceId,reason:'UNSUPPORTED_COMPANION_UNIQUE_EFFECT'});
    const enchantment=effect.snapshot.enchantment;
    if(enchantment){
      if(!binding)blocked.push({instanceId:slot.instanceId,reason:'MULTISHOT_CATALOG_BINDING_UNCONFIRMED'});
      else if(enchantment.effectId!==binding.effectId||enchantment.definitionVersion!==binding.definitionVersion)blocked.push({instanceId:slot.instanceId,reason:'UNSUPPORTED_COMPANION_ENCHANTMENT'});
      else{
        let definition:MultishotDefinition;
        try{definition=parseMultishotDefinition(enchantment.definitionSnapshot);}catch{return fail('logical_definition_invalid');}
        if(definition.appliedStar!==enchantment.appliedStar||definition.appliedStar>2)return fail('logical_definition_invalid');
        candidates.push({instanceId:slot.instanceId,definitionSnapshot:definition});
      }
    }
    return {...slot,currentStar:instance.star,originalHash:metadata.data.original_hash,payloadHash:metadata.data.payload_hash,sourceRef:metadata.data.source_ref};
  }));
  blocked.sort((a,b)=>a.instanceId<b.instanceId?-1:a.instanceId>b.instanceId?1:a.reason<b.reason?-1:a.reason>b.reason?1:0);
  return {slots,blocked,selection:blocked.length?null:selectHighestMultishot(candidates)};
}

/** Read-only fixed capture foundation. Current DB ownership/slots/effects are validated
 * by the component owner. This legacy fixture capture does not commit a formal trial START.
 * A selected LOCAL fixture result is NOT operating support, admission or a live effect. */
export async function captureEquippedMultishot12Source(db:D1Database,accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown,bindingSource?:IsolatedMultishotBindingSource){
 return captureSource(db,accountId,input,originalMira,expectedMiraHash,bindingSource,false);
}
async function captureSource(db:D1Database,accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown,bindingSource:IsolatedMultishotBindingSource|undefined,formal:boolean){
  const parsed=requestExtension.safeParse(input);if(!parsed.success)throw new ActivityError(400,'equipment_multishot_request_invalid');
  const {expectedBindingHash,...request}=parsed.data;
  const originalComponent=await captureMiraEffectiveSource(db,accountId,request,originalMira,expectedMiraHash),binding=formal?await multishotCatalogBinding():await readBinding(bindingSource);
  if(expectedBindingHash&&binding?.bindingHash!==expectedBindingHash)return fail('binding_conflict');
  const {slots,blocked,selection}=await resolveSlots(db,originalComponent,originalComponent.equipment.slots,binding,formal,originalComponent.equipment.blocked.filter(value=>value.reason!=='effect_handler_unavailable'));
  if(json(formal?await multishotCatalogBinding():await readBinding(bindingSource))!==json(binding))return fail('binding_changed');
  const current=await captureMiraEffectiveSource(db,accountId,request,originalMira,expectedMiraHash);
  if(current.packetHash!==originalComponent.packetHash)return fail('source_changed');
  const body={version:formal?'equipment.multishot-equipped-12-source.v2.20261015':equippedMultishot12SourceVersion,accountId,runId:originalComponent.runId,wearerId:'op_mira',
    originalComponent,originalComponentHash:originalComponent.packetHash,departurePinHash:originalComponent.departurePinHash,
    inventoryRevision:originalComponent.equipment.inventoryRevision,inventoryHash:originalComponent.equipment.inventoryHash,
    catalogBinding:binding,slots,selection,blocked,
    status:blocked.length?'UNSUPPORTED':selection?(formal?'FORMAL_CATALOG_SELECTED':'ISOLATED_FIXTURE_SELECTED'):'VERIFIED_NO_ENCHANTMENT',
    operatingCatalogBound:false as const,effectiveStats:null,
    startBinding:'READ_ONLY_VIEW_NOT_COMMITTED_START_EXTENSION',
    missing:['APPROVED_FORMAL_MULTISHOT_ID_AND_CATALOG','VERSIONED_SCHEMA_DB_WRITER_READER_COMPATIBILITY',
      'IMMUTABLE_START_SELECTION_COMMIT','EQUIPPED_TEST_RESEARCH_APPLICABILITY','COMPANION_EFFECT_AND_FINAL_STATIC_STATS',
      'NATIVE_FIRE_TARGETS_HP_AND_RESTORE_INTEGRATION'],runtimeSupported:false as const,canStart:false as const};
  const packet={...body,packetHash:await digest(body)};
  if(new TextEncoder().encode(json(packet)).length>1024*1024)return fail('capacity');
  return structuredClone(packet);
}
export type EquippedMultishot12Source=Awaited<ReturnType<typeof captureEquippedMultishot12Source>>;
/** Independent frozen original/hash only; no current inventory, catalog or policy reads. */
export async function readPinnedEquippedMultishot12Source(input:unknown,original:EquippedMultishot12Source,expectedHash:unknown,accountId:string):Promise<EquippedMultishot12Source>{
  if(!hash.safeParse(expectedHash).success||!id.safeParse(accountId).success)return fail('packet_invalid');
  let candidate:string;try{candidate=json(input);}catch{return fail('packet_invalid');}
  if(new TextEncoder().encode(candidate).length>1024*1024)return fail('capacity');
  const {packetHash,...body}=original;
  if(original.accountId!==accountId||packetHash!==expectedHash||await digest(body)!==packetHash||candidate!==json(original))return fail('original_packet_conflict');
  return structuredClone(original);
}
export const equippedMultishotLocalProfile='mira-equipped-multishot-local.v1.20261015';
/** Explicit LOCAL-only research/critical applicability. Does not modify the empty
 * profile, original generic component, or any ordinary/cooperative run admission. */
export async function captureFormalEquippedMultishot12Source(db:D1Database,accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown){
 const parsed=z.object({profile:z.literal(equippedMultishotLocalProfile),environment:z.literal('LOCAL')}).passthrough().safeParse(input);
 if(!parsed.success)throw new ActivityError(400,'equipment_multishot_trial_request_invalid');
 const {profile,environment,...request}=parsed.data;
 const source=await captureSource(db,accountId,request,originalMira,expectedMiraHash,undefined,true);
 const {unsupported,core,effectiveStats}=formalComposition(source);
 const {packetHash,...originalBody}=source;void packetHash;
 const policy={policyVersion:'mira-equipped-controlled-trial-approved-20261015',policySource:'44_COMBAT_DETAIL_CONTRACTS.md#mira-equipped-controlled-trial-approved-20261015'};
 const body={...originalBody,profile,environment,catalogBound:true,operatingCatalogBound:false,
  selection:unsupported.length?null:source.selection,status:unsupported.length?'UNSUPPORTED':source.status,
  research:{...policy,status:'NOT_APPLICABLE',modifiers:null,ledgerEvidence:null},
  critical:{...policy,status:'NOT_APPLICABLE',chanceBps:null,rngDraws:0},
  effectiveStats,coreStats:core,staticStatsReady:effectiveStats!==null,unsupported,
  passive:source.originalComponent.passive,startBinding:'LOCAL_TRIAL_CANDIDATE_REQUIRES_ATOMIC_COMMIT',
  missing:['NATIVE_EQUIPPED_FIRE_TARGETS_HP_AND_RESTORE_INTEGRATION'],runtimeSupported:false as const,canStart:false as const};
 const packet={...body,packetHash:await digest(body)};if(new TextEncoder().encode(json(packet)).length>1024*1024)return fail('capacity');return structuredClone(packet);
}
export type FormalEquippedMultishot12Source=Awaited<ReturnType<typeof captureFormalEquippedMultishot12Source>>;

function formalComposition(source:Pick<Awaited<ReturnType<typeof captureSource>>,'slots'|'blocked'|'originalComponent'>){
 const unsupported=source.blocked.map(x=>x.reason),items=source.slots.filter(x=>x!==null).map(x=>x.instance);
 const tags=new Map<string,number>();for(const i of items)tags.set(i.set_tag,(tags.get(i.set_tag)??0)+1);
 // Only the static set subset actually computed by the shared core is supported.
 // Other set stages are explicit blockers rather than ignored modifiers.
 if([...tags].some(([tag,count])=>tag==='SET01'?count>1:tag==='SET04'?count>2:true))unsupported.push('UNSUPPORTED_SET_EFFECT');
 const commander=source.originalComponent.commander.snapshot;
 if(commander!==null){const ranks=z.object({ranks:z.record(z.string(),z.number())}).safeParse(commander);if(!ranks.success||Object.keys(ranks.data.ranks).length)unsupported.push('UNSUPPORTED_COMMANDER_EFFECT');}
 const permanent=source.originalComponent.originalMira.actor.owned.permanentStats;
 if(!permanent)unsupported.push('ORIGINAL_STATIC_STATS_UNAVAILABLE');
 const core=unsupported.length?null:calculateEquipmentCoreStats(source.originalComponent.base,items.map(i=>({categoryId:i.category_id,star:i.star,setTag:i.set_tag})));
 const effectiveStats=core&&permanent?{maxHp:core.maxHp,attack:core.attack,defense:core.defense,basicIntervalMs:permanent.basicIntervalMs}:null;
 return {unsupported,core,effectiveStats};
}
export const preparedMultishotSourceVersion='equipment.multishot-prepared-source.v1.20261016';
type PreparedPreview=Extract<Awaited<ReturnType<EquipmentPlacementStore['preparePreviewMove']>>,{status:'PREPARED'}>;
/** Internal prepared owner objects only, never client-supplied assignments or effects.
 * Physical instances and effect revisions remain original evidence; proposed slots
 * and proposed revisions are labelled explicitly and are never persisted here. */
export async function capturePreparedEquippedMultishot12Source(db:D1Database,accountId:string,before:FormalEquippedMultishot12Source,prepared:PreparedPreview){
 await prepared.assertUnchanged();
 const projection=prepared.projection;
 if(projection.accountId!==accountId||projection.runId!==before.runId||before.accountId!==accountId||await digest(projection.inventory)!==before.inventoryHash)return fail('preview_source_conflict');
 if(projection.assignments.length!==1||projection.assignments[0]?.wearerKind!=='operator'||projection.assignments[0].wearerId!=='op_mira')return fail('preview_wearer_unsupported');
 const assignment=projection.assignments[0];
 const binding=await multishotCatalogBinding();if(binding.bindingHash!==before.catalogBinding?.bindingHash)return fail('binding_changed');
 const readInputs=async()=>{
  const result:MiraEffectiveSource['equipment']['slots']=[null,null,null];const seen=new Set<string>();
  if(assignment.slots.length!==3)return fail('preview_assignment_invalid');
  for(const [index,slot] of assignment.slots.entries()){
   if(!slot)continue;if(seen.has(slot.instanceId))return fail('preview_assignment_invalid');seen.add(slot.instanceId);
   const item=projection.inventory.items.find(i=>i.instance_id===slot.instanceId);
   if(!item||json(slot)!==json({instanceId:item.instance_id,definitionId:item.definition_id,definitionVersion:item.definition_version,categoryId:item.category_id,setTag:item.set_tag,star:item.star}))return fail('preview_assignment_invalid');
   const effect=await new EquipmentEffectStateStore(db).read(accountId,before.runId,item.instance_id);
   if(effect&&(effect.inventoryRevision!==projection.inventory.revision||effect.instanceRevision!==item.revision))return fail('preview_source_conflict');
   result[index]={instanceId:item.instance_id,instance:structuredClone(item),effect};
  }return result;
 };
 const resolved=await resolveSlots(db,before.originalComponent,await readInputs(),binding,true,[]);
 const second=await resolveSlots(db,before.originalComponent,await readInputs(),binding,true,[]);
 if(json(second)!==json(resolved))return fail('preview_source_conflict');
 const actual=await captureFormalEquippedMultishot12Source(db,accountId,{runId:before.runId,expectedInventoryRevision:before.inventoryRevision,expectedDeparturePinHash:before.departurePinHash,profile:before.profile,environment:'LOCAL',expectedBindingHash:binding.bindingHash},before.originalComponent.originalMira,before.originalComponent.originalMiraHash);
 if(actual.packetHash!==before.packetHash)return fail('preview_source_conflict');
 const {unsupported,core,effectiveStats}=formalComposition({...resolved,originalComponent:before.originalComponent});
 const {packetHash,...body}=before;void packetHash;
 const selection=unsupported.length?null:resolved.selection;
 const candidate={...body,version:preparedMultishotSourceVersion,...resolved,selection,
  status:unsupported.length?'UNSUPPORTED':selection?'FORMAL_CATALOG_SELECTED':'VERIFIED_NO_ENCHANTMENT',unsupported,
  coreStats:core,effectiveStats,staticStatsReady:effectiveStats!==null,
  sourcePhase:'PROPOSED_ASSIGNMENT_NOT_COMMITTED',startBinding:'PROPOSED_ASSIGNMENT_NOT_COMMITTED',
  originalCurrentSourceHash:before.packetHash,proposedAssignments:structuredClone(projection.assignments),
  boundInventoryRevision:projection.inventory.revision,proposedInventoryRevision:prepared.receipt.inventoryRevision};
 await prepared.assertUnchanged();
 return {...candidate,packetHash:await digest(candidate)};
}
