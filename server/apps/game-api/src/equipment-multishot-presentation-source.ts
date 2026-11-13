import {EquipmentPlacementStore} from './equipment-placement';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {EquipmentInventoryStore} from './equipment-inventory-start';
import {EquipmentEffectStateStore} from './equipment-effect-state';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import type {MiraBasicSource} from './combat-b-mira-basic-source';
import {captureFormalEquippedMultishot12Source,capturePreparedEquippedMultishot12Source,preparedMultishotSourceVersion,equippedMultishotLocalProfile,type FormalEquippedMultishot12Source} from './equipment-multishot-equipped-source';
import {EquippedMultishotTrialStartStore} from './equipment-multishot-trial-start';
import {parseMultishotDefinition} from './equipment-multishot-definition';

export const multishotPresentationVersion='equipment.multishot-presentation.v1.20261016';
type Basis='CURRENT_VERIFIED_EQUIPMENT'|'ORIGINAL_LOCAL_TRIAL_START'|'PROPOSED_PLACEMENT_NOT_COMMITTED';
type RowStatus='APPLIED'|'LOWER_EFFECT'|'DUPLICATE_HIGHEST'|'NO_ENCHANTMENT'|'SOURCE_UNAVAILABLE';
function invalid():never{throw new ActivityError(409,'equipment_multishot_presentation_source_invalid');}
type EffectSummary={status:string;coefficientBps:number|null};
function compareVerifiedEffect(before:EffectSummary|null,after:EffectSummary){
 if(!before||before.status==='SOURCE_UNAVAILABLE'||after.status==='SOURCE_UNAVAILABLE')return 'SOURCE_UNAVAILABLE';
 const a=before.coefficientBps,b=after.coefficientBps;
 return a===b?'UNCHANGED':a===null?'GAINED':b===null?'REMOVED':b>a?'INCREASED':'DECREASED';
}
/** Reads authoritative owners only. The View does not select an effect, infer
 * activation from nearby enemies, or recompute procedure values from current star.
 * This class neither prepares placement writes nor authorizes a replacement preview. */
export class MultishotPresentationSource{
 constructor(private readonly db:D1Database,private readonly placement?:EquipmentPlacementStore){}
 private project(source:FormalEquippedMultishot12Source,basis:Basis,startHash:string|null){
  if(source.version!==(basis==='PROPOSED_PLACEMENT_NOT_COMMITTED'?preparedMultishotSourceVersion:'equipment.multishot-equipped-12-source.v2.20261015')||source.slots.length!==3||source.wearerId!=='op_mira'||!source.catalogBound)return invalid();
  const available=source.staticStatsReady&&source.status!=='UNSUPPORTED'&&!source.blocked.length&&!source.unsupported.length;
  const selection=available?source.selection:null;
  const definitions=source.slots.map(slot=>slot?.effect?.snapshot.enchantment?.effectId==='EN17'
   ?parseMultishotDefinition(slot.effect.snapshot.enchantment.definitionSnapshot):null);
  if(available){
   if(selection){const index=source.slots.findIndex(slot=>slot?.instanceId===selection.instanceId);
    if(index<0||definitions[index]?.coefficientBps!==selection.definitionSnapshot.coefficientBps)return invalid();
   }else if(definitions.some(x=>x!==null))return invalid();
  }
  const coefficientBps=selection?.definitionSnapshot.coefficientBps??null;
  const slots=source.slots.map((slot,index)=>{
   if(!slot)return null;
   const definition=definitions[index],enchantment=slot.effect?.snapshot.enchantment;
   let status:RowStatus='SOURCE_UNAVAILABLE';
   if(available){
    if(!enchantment)status='NO_ENCHANTMENT';
    else if(!definition||coefficientBps===null)return invalid();
    else if(slot.instanceId===selection?.instanceId)status='APPLIED';
    else if(definition.coefficientBps===coefficientBps)status='DUPLICATE_HIGHEST';
    else if(definition.coefficientBps<coefficientBps)status='LOWER_EFFECT';
    else return invalid();
   }
   return {slotIndex:index,instanceId:slot.instanceId,currentStar:slot.currentStar,
    appliedStar:definition?.appliedStar??null,coefficientBps:definition?.coefficientBps??null,
    effectId:enchantment?.effectId??null,definitionVersion:enchantment?.definitionVersion??null,
    instanceRevision:slot.instance.revision,effectRevision:slot.effect?.effectRevision??null,
    originalHash:slot.originalHash,payloadHash:slot.payloadHash,sourceRef:slot.sourceRef,
    status,basicStatsRemainEquipped:available?true:null};
  });
  return structuredClone({version:multishotPresentationVersion,basis,accountId:source.accountId,runId:source.runId,
   wearerId:source.wearerId,profile:source.profile,environment:source.environment,
   status:available?(selection?'VERIFIED_SELECTED':'VERIFIED_NO_ENCHANTMENT'):'SOURCE_UNAVAILABLE',
   selectedInstanceId:selection?.instanceId??null,coefficientBps,slots,
   reasons:[...new Set([...source.blocked.map(x=>x.reason),...source.unsupported])],
   sourceHash:source.packetHash,startHash,inventoryRevision:source.inventoryRevision,inventoryHash:source.inventoryHash,
   originalMiraHash:source.originalComponent.originalMiraHash,departurePinHash:source.departurePinHash,
   effectiveStats:available?source.effectiveStats:null,
   attackScope:'VERIFIED_MIRA_PROJECTILE_BASIC_ONLY',supportScope:'ISOLATED_LOCAL_TRIAL_ONLY',
   runtimeSupported:false as const,canStart:false as const});
 }
 async stored(accountId:string,runId:string,instanceId:string){
  if(!z.string().regex(/^[\x21-\x7e]{1,128}$/u).safeParse(instanceId).success)throw new ActivityError(400,'equipment_multishot_presentation_request_invalid');
  const owner=new EquipmentInventoryStore(this.db),inventory=await owner.readInventory(accountId,runId);
  if(!inventory)return invalid();const instance=inventory.items.find(x=>x.instance_id===instanceId);
  if(!instance)throw new ActivityError(409,'equipment_instance_not_found');
  if(instance.location_kind!=='STORED')throw new ActivityError(409,'equipment_multishot_presentation_not_stored');
  const effect=await new EquipmentEffectStateStore(this.db).read(accountId,runId,instanceId);
  if((effect&&(effect.inventoryRevision!==inventory.revision||effect.instanceRevision!==instance.revision))
   ||canonicalJson(await owner.readInventory(accountId,runId))!==canonicalJson(inventory))throw new ActivityError(409,'equipment_multishot_presentation_source_changed');
  const enchantment=effect?.snapshot.enchantment,definition=enchantment?.effectId==='EN17'?parseMultishotDefinition(enchantment.definitionSnapshot):null;
  return {version:multishotPresentationVersion,basis:'CURRENT_VERIFIED_STORED_ITEM',accountId,runId,instanceId,
   locationKind:'STORED',status:!effect?'SOURCE_UNAVAILABLE':definition?'NOT_EQUIPPED':'NO_MULTISHOT',
   currentStar:instance.star,appliedStar:definition?.appliedStar??null,coefficientBps:definition?.coefficientBps??null,
   effectId:enchantment?.effectId??null,definitionVersion:enchantment?.definitionVersion??null,
   instanceRevision:instance.revision,effectRevision:effect?.effectRevision??null,inventoryRevision:inventory.revision,
   sourceHash:await sha256Hex(canonicalJson({inventory,effect})),hashBasis:'VERIFIED_STORED_INVENTORY_AND_EFFECT_READ',
   selectionStatus:'DETERMINED_ONLY_AFTER_VERIFIED_WEARER_CAPTURE',runtimeSupported:false as const,canStart:false as const};
 }
 async current(accountId:string,request:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown){
  return this.project(await captureFormalEquippedMultishot12Source(this.db,accountId,request,originalMira,expectedMiraHash),'CURRENT_VERIFIED_EQUIPMENT',null);
 }
 async departure(accountId:string,runId:string,trialId:string,expectedStartHash?:string){
  const original=await new EquippedMultishotTrialStartStore(this.db).read(accountId,runId,trialId,expectedStartHash);
  return original?this.project(original.source,'ORIGINAL_LOCAL_TRIAL_START',original.startHash):null;
 }
 async compareCurrentToDeparture(accountId:string,request:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown,reference:unknown){
  const parsed=z.strictObject({trialId:z.string().regex(/^[\x21-\x7e]{1,128}$/u),expectedStartHash:z.string().regex(/^[a-f0-9]{64}$/u)}).safeParse(reference);
  if(!parsed.success)throw new ActivityError(400,'equipment_multishot_presentation_request_invalid');
  const current=await this.current(accountId,request,originalMira,expectedMiraHash);
  const departure=await this.departure(accountId,current.runId,parsed.data.trialId,parsed.data.expectedStartHash);
  if(departure&&(departure.originalMiraHash!==current.originalMiraHash||departure.departurePinHash!==current.departurePinHash))return invalid();
  const effectChange=compareVerifiedEffect(departure,current);
  return {version:multishotPresentationVersion,kind:'DEPARTURE_VS_CURRENT_NOT_REPLACEMENT_PREVIEW',departure,current,effectChange};
 }
 async previewPlacement(accountId:string,request:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown,command:unknown){
  if(!this.placement)throw new ActivityError(409,'equipment_multishot_preview_placement_source_required');
  const run=z.object({runId:z.string().regex(/^[\x21-\x7e]{1,128}$/u),profile:z.literal(equippedMultishotLocalProfile),environment:z.literal('LOCAL')}).passthrough().safeParse(request);
  if(!run.success)throw new ActivityError(400,'equipment_multishot_presentation_request_invalid');
  const prepared=await this.placement.preparePreviewMove(accountId,run.data.runId,command);
  if(prepared.status==='ALREADY_COMMITTED')return {version:'equipment.multishot-placement-preview.v1.20261016',status:'ALREADY_COMMITTED' as const,committed:true,receipt:prepared.receipt,before:null,after:null,effectChange:null};
  const source=await captureFormalEquippedMultishot12Source(this.db,accountId,request,originalMira,expectedMiraHash);
  const candidate=await capturePreparedEquippedMultishot12Source(this.db,accountId,source,prepared);
  const before=this.project(source,'CURRENT_VERIFIED_EQUIPMENT',null),after=this.project(candidate,'PROPOSED_PLACEMENT_NOT_COMMITTED',null);
  const effectChange=compareVerifiedEffect(before,after);
  await prepared.assertUnchanged();
  return {version:'equipment.multishot-placement-preview.v1.20261016',status:'PREVIEW' as const,committed:false,
   receipt:null,placementProposal:prepared.receipt,proof:prepared.proof,before,after,effectChange,
   proposedInventoryRevision:prepared.receipt.inventoryRevision,boundInventoryRevision:source.inventoryRevision,
   candidateSourceHash:candidate.packetHash,proposedAssignments:candidate.proposedAssignments};
 }

}
