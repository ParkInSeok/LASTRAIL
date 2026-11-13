import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readPinnedMiraBasicSource,type MiraBasicSource} from './combat-b-mira-basic-source';
import {readEquipmentDeparturePin} from './equipment-departure-source';
import {EquipmentInventoryStore} from './equipment-inventory-start';
import {EquipmentEffectStateStore} from './equipment-effect-state';
import {calculateEquipmentCoreStats,equipmentCoreStatVersion} from './equipment-run-stat-owner';

export const miraEffectiveSourceVersion='combat.mira-effective-source.v1.20261015';
const hash=z.string().regex(/^[a-f0-9]{64}$/u),id=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const requestSchema=z.strictObject({runId:id,expectedInventoryRevision:z.number().int().positive().safe(),expectedDeparturePinHash:hash});
const json=(value:unknown)=>canonicalJson(z.json().parse(value)),digest=(value:unknown)=>sha256Hex(json(value));
function fail(code:string):never{throw new ActivityError(409,'combat_mira_effective_'+code);}
type Inventory=NonNullable<Awaited<ReturnType<EquipmentInventoryStore['readInventory']>>>;
type Item=Inventory['items'][number];

/** Read actual run-owned equipment only. This does not prepare maintenance writes or
 * bypass RunEquipmentStatOwner's safe-maintenance/proposed-assignment boundary. */
async function equipmentEvidence(db:D1Database,accountId:string,runId:string,inventory:Inventory){
  const equipped=inventory.items.filter(item=>item.location_kind==='EQUIPPED'&&item.wearer_kind==='operator'&&item.wearer_id==='op_mira');
  const sources=await Promise.all(equipped.map(async item=>({item,effect:await new EquipmentEffectStateStore(db).read(accountId,runId,item.instance_id)})));
  const blocked:Array<{instanceId:string;reason:string}>=[];
  for(const {item,effect} of sources){
    let reason:string|null=null;
    if(!effect)reason='effect_source_missing';
    else if(effect.inventoryRevision!==inventory.revision||effect.instanceRevision!==item.revision)return fail('source_changed');
    else if(item.definition_version!==equipmentCoreStatVersion)reason='definition_unsupported';
    else if(effect.snapshot.uniqueEffects.length||effect.snapshot.enchantment)reason='effect_handler_unavailable';
    else if(!['HP','ATK','DEF'].includes(item.category_id))reason='category_unsupported';
    if(reason)blocked.push({instanceId:item.instance_id,reason});
  }
  return {sources,blocked};
}

/** Partial static source, not final effective stats or a live D producer. The original
 * Mira packet/hash must be independently trusted. Bind it to the actual immutable run
 * departure pin, not current account growth. Research is UNKNOWN, never a fabricated
 * empty ledger. Existing research/catalog ownership must be supplied before completion. */
export async function captureMiraEffectiveSource(db:D1Database,accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown){
  const parsed=requestSchema.safeParse(input);
  if(!parsed.success||!id.safeParse(accountId).success)throw new ActivityError(400,'combat_mira_effective_request_invalid');
  const req=parsed.data,mira=await readPinnedMiraBasicSource(originalMira,originalMira,expectedMiraHash,accountId);
  const readRun=()=>db.prepare(`SELECT r.status,a.status AS account_status,s.snapshot_json FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`)
    .bind(req.runId,accountId).first<{status:string;account_status:string;snapshot_json:string}>();
  const run=await readRun();
  if(!run||run.status!=='ACTIVE'||run.account_status!=='ACTIVE')return fail('run_unavailable');
  const pin=await readEquipmentDeparturePin(run.snapshot_json,{accountId,runId:req.runId,expectedPinHash:req.expectedDeparturePinHash});
  if(!pin)return fail('departure_pin_missing');
  const source=z.object({ownedProfile:z.json(),commanderSnapshot:z.json().nullable()}).parse(pin.source);
  const origin=pin.origin.actors.find(actor=>actor.wearerKind==='operator'&&actor.wearerId==='op_mira');
  const permanent=mira.actor.owned.permanentStats;
  if(!origin||!permanent||json(source.ownedProfile)!==json(mira.ownedProfile)
    ||json(source.commanderSnapshot)!==json(mira.modifierSources.commander)
    ||json(origin.base)!==json({maxHp:permanent.maxHp,attack:permanent.attack,defense:{numerator:String(permanent.defense),denominator:'1'}}))return fail('departure_source_conflict');
  const inventoryOwner=new EquipmentInventoryStore(db),inventory=await inventoryOwner.readInventory(accountId,req.runId);
  if(!inventory)return fail('inventory_unavailable');
  if(inventory.revision!==req.expectedInventoryRevision)return fail('source_changed');
  const evidence=await equipmentEvidence(db,accountId,req.runId,inventory);
  const slots:Array<{instanceId:string;instance:Item;effect:Awaited<ReturnType<EquipmentEffectStateStore['read']>>}|null>=[null,null,null];
  for(const {item,effect} of evidence.sources)slots[item.slot_index!]={instanceId:item.instance_id,instance:item,effect};
  const coreStats=evidence.blocked.length?null:calculateEquipmentCoreStats(origin.base,evidence.sources.map(({item})=>({categoryId:item.category_id,star:item.star,setTag:item.set_tag})));
  const currentEvidence=await equipmentEvidence(db,accountId,req.runId,inventory);
  const currentInventory=await inventoryOwner.readInventory(accountId,req.runId);
  if(json(currentEvidence)!==json(evidence)||json(currentInventory)!==json(inventory)||json(await readRun())!==json(run))return fail('source_changed');
  const equipment={owner:'EquipmentInventoryStore/EquipmentEffectStateStore',inventoryRevision:inventory.revision,
    inventoryHash:await digest(inventory),inventoryEmpty:inventory.items.length===0,equippedEmpty:evidence.sources.length===0,
    slots,status:evidence.blocked.length?'UNSUPPORTED_COMPONENT':'VERIFIED_CORE_COMPONENT',blocked:evidence.blocked};
  const passive=mira.definition.passive;
  if(!('valueBps' in passive))return fail('passive_source_invalid');
  const body={version:miraEffectiveSourceVersion,accountId,runId:req.runId,originalMira:mira,originalMiraHash:mira.packetHash,
    departurePinHash:pin.pinHash,departureOriginHash:pin.originHash,runSnapshotHash:await digest(run.snapshot_json),
    base:origin.base,equipment,coreStats,
    research:{status:'SOURCE_OWNER_UNAVAILABLE',modifiers:null,revision:null,sourceHash:null,
      reason:'NO_APPROVED_ACTIVE_NODE_CATALOG_AND_NO_ACCOUNT_RESEARCH_LEDGER_OWNER'},
    commander:{snapshot:source.commanderSnapshot,sourceHash:await digest(mira.modifierSources.commanderSource),
      stage:'ORIGINAL_DEPARTURE_PIN_NOT_CURRENT_ACCOUNT_RELOOKUP'},
    passive:{effect:{sourceId:'passive:op_mira:OWN_BASIC',group:passive.group,valueBps:passive.valueBps},
      appliedToStaticStats:false,applicationOwner:'ORIGINAL_MODE_COMBAT_FIRE_OWNER_ONCE',
      nonEquipmentOutgoingCapBps:mira.definition.originalPolicy.effectRules.nonEquipmentOutgoingCapBps},
    effectiveStats:null,statStage:'VERIFIED_EQUIPMENT_CORE_COMPONENT_NOT_FINAL_EFFECTIVE_STATS',
    missing:['AUTHORITATIVE_RESEARCH_LEDGER_AND_APPLICABILITY','FULL_EQUIPMENT_TIMING_AND_CONDITIONAL_EFFECT_OWNER',
      'FINAL_STATIC_EFFECTIVE_STAT_COMPOSITION','ORIGINAL_FIRE_D_CRITICAL_OPPORTUNITY_AND_TARGET_SPECIFIC_EFFECTS'],
    runtimeSupported:false as const,canStart:false as const};
  const packet={...body,packetHash:await digest(body)};
  if(new TextEncoder().encode(json(packet)).length>768*1024)return fail('capacity');
  return structuredClone(packet);
}
export type MiraEffectiveSource=Awaited<ReturnType<typeof captureMiraEffectiveSource>>;
/** Frozen independent original/hash restore only; no current equipment/DEF/account reads. */
export async function readPinnedMiraEffectiveSource(input:unknown,original:MiraEffectiveSource,expectedHash:unknown,accountId:string):Promise<MiraEffectiveSource>{
  if(!hash.safeParse(expectedHash).success||!id.safeParse(accountId).success)return fail('packet_invalid');
  let candidate:string;try{candidate=json(input);}catch{return fail('packet_invalid');}
  if(new TextEncoder().encode(candidate).length>768*1024)return fail('capacity');
  const {packetHash,...body}=original;
  if(original.accountId!==accountId||packetHash!==expectedHash||await digest(body)!==packetHash||candidate!==json(original))return fail('original_packet_conflict');
  return structuredClone(original);
}