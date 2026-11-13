import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {ExactCombatValue,exactCombatValueSchema} from './combat-exact-value';
import {EquipmentInventoryStore} from './equipment-inventory-start';
import {EquipmentEffectStateStore} from './equipment-effect-state';
import type {EquipmentFinalHealthSource} from './equipment-health-placement';
import type {PreparedEquipmentMerge} from './equipment-merge';
export const equipmentCoreStatVersion='equipment.run-hp-atk-def.v1.20261005';
const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const baseSchema=z.strictObject({maxHp:z.number().int().positive().safe(),attack:exactCombatValueSchema,defense:exactCombatValueSchema});
const itemSchema=z.strictObject({categoryId:z.enum(['HP','ATK','DEF']),star:z.number().int().min(1).max(5),setTag:z.enum(['SET01','SET02','SET03','SET04','SET05','SET06'])});
const bps=[0,1000,2500,6000,15000,35000] as const;
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),digest=(v:unknown)=>sha256Hex(json(v));
function fail(code:string):never{throw new ActivityError(409,'equipment_stat_'+code);}
/** Basic+static set group only. Exact ATK/DEF preserve original damage rounding. */
export function calculateEquipmentCoreStats(rawBase:unknown,rawItems:unknown){
 const base=baseSchema.parse(rawBase),items=z.array(itemSchema).max(3).parse(rawItems),group={HP:0,ATK:0,DEF:0},tags=new Map<string,number>();
 for(const i of items){group[i.categoryId]+=bps[i.star]!;tags.set(i.setTag,(tags.get(i.setTag)??0)+1);}
 if((tags.get('SET01')??0)>=1)group.ATK+=400;if((tags.get('SET04')??0)>=1)group.HP+=600;if((tags.get('SET04')??0)>=2)group.DEF+=1000;
 const scale=(v:unknown,b:number)=>ExactCombatValue.from(v).multiply(ExactCombatValue.from({numerator:String(10000+b),denominator:'10000'}));
 const hp=scale({numerator:String(base.maxHp),denominator:'1'},group.HP),maxHp=Number(hp.floor());if(!Number.isSafeInteger(maxHp)||maxHp<1)return fail('range_exceeded');
 return {maxHp,exactMaxHp:hp.snapshot(),attack:scale(base.attack,group.ATK).snapshot(),defense:scale(base.defense,group.DEF).snapshot(),additiveBps:group,rounding:'HP_INTEGER_FLOOR_AT_HP_BOUNDARY_ATK_DEF_EXACT_UNTIL_ORIGINAL_DAMAGE_OWNER' as const};
}
const actorOrigin=z.strictObject({wearerKind:z.enum(['operator','turret']),wearerId:id,targetId:id,base:baseSchema});
const originSchema=z.strictObject({version:z.literal(equipmentCoreStatVersion),accountId:id,runId:id,stage:z.literal('ORIGINAL_PRE_EQUIPMENT_RUN_BASE'),actors:z.array(actorOrigin).min(1).max(1000)}).refine(o=>new Set(o.actors.map(a=>a.wearerKind+':'+a.wearerId)).size===o.actors.length&&new Set(o.actors.map(a=>a.targetId)).size===o.actors.length);
type Assignment=Parameters<EquipmentFinalHealthSource['prepare']>[0]['assignments'][number];
/** Original START mapping/base and safe-maintenance authority; never client maxima.
 * No operating adapter exists yet. A provider must pin original provenance and supply
 * same-batch SQL proofs for run/account/base/target and safe maintenance. */
export interface OriginalRunEquipmentStatContext { prepare(accountId:string,runId:string):Promise<{origin:unknown;originalHash:string;assertions:D1PreparedStatement[]}>; }
export class RunEquipmentStatOwner implements EquipmentFinalHealthSource {
 constructor(private readonly db:D1Database,private readonly original:OriginalRunEquipmentStatContext){}
 async capture(accountId:string,runId:string,assignments:Assignment[]){return this.captureCandidate(accountId,runId,assignments);}
 /** Trusted original merge candidate only; validation and SQL writers remain owned by
  * EquipmentMergeStore. No new client-controlled equipment or maxima are accepted. */
 async prepareMerged(context:{accountId:string;runId:string;expectedStatVersion:string;merge:PreparedEquipmentMerge}){
  if(context.expectedStatVersion!==equipmentCoreStatVersion)return fail('version_conflict');
  const plan=context.merge,transition=plan.projection;if(!transition||!plan.statements.length||transition.accountId!==context.accountId||transition.runId!==context.runId)return fail('merge_candidate_invalid');
  const inventory=await new EquipmentInventoryStore(this.db).readInventory(context.accountId,context.runId);if(!inventory)return fail('run_inventory_unavailable');
  const primary=inventory.items.find(i=>i.instance_id===transition.primary.instance_id),secondary=inventory.items.find(i=>i.instance_id===transition.secondary.instance_id);
  if(!primary||!secondary||json(primary)!==json(transition.primary)||json(secondary)!==json(transition.secondary)||plan.receipt.inventoryRevision!==inventory.revision+1||plan.receipt.instanceRevision!==primary.revision+1||plan.receipt.star!==primary.star+1||plan.receipt.resultInstanceId!==primary.instance_id||plan.receipt.consumedInstanceId!==secondary.instance_id||transition.snapshot.equipment.star!==plan.receipt.star||transition.snapshot.equipment.definitionId!==primary.definition_id||transition.snapshot.equipment.definitionVersion!==primary.definition_version)return fail('merge_candidate_invalid');
  const affected=new Map<string,Assignment>();
  for(const i of [primary,secondary])if(i.location_kind==='EQUIPPED')affected.set(i.wearer_kind+':'+i.wearer_id,{wearerKind:i.wearer_kind as 'operator'|'turret',wearerId:i.wearer_id!,slots:[null,null,null]});
  if(!affected.size)return fail('merge_no_equipped_wearer');
  for(const i of inventory.items){if(i.instance_id===secondary.instance_id||i.location_kind!=='EQUIPPED')continue;const a=affected.get(i.wearer_kind+':'+i.wearer_id);if(a)a.slots[i.slot_index!]={instanceId:i.instance_id,definitionId:i.definition_id,definitionVersion:i.definition_version,categoryId:i.category_id,setTag:i.set_tag,star:i.instance_id===primary.instance_id?plan.receipt.star:i.star};}
  const assignments=[...affected.entries()].sort(([a],[b])=>a<b?-1:a>b?1:0).map(([,a])=>a);
  const p=await this.captureCandidate(context.accountId,context.runId,assignments,plan);
  return {statVersion:equipmentCoreStatVersion,assertions:p.assertions,targets:p.snapshot.actors.map(a=>({wearerKind:a.wearerKind,wearerId:a.wearerId,targetId:a.targetId,maxHp:a.stats.maxHp}))};
 }
 private async captureCandidate(accountId:string,runId:string,assignments:Assignment[],merge?:PreparedEquipmentMerge){
  const proof=await this.original.prepare(accountId,runId),origin=originSchema.parse(proof.origin);if(origin.accountId!==accountId||origin.runId!==runId||await digest(origin)!==proof.originalHash||!proof.assertions.length)return fail('original_context_invalid');
  const inventory=await new EquipmentInventoryStore(this.db).readInventory(accountId,runId);if(!inventory)return fail('run_inventory_unavailable');
  if(assignments.length<1||assignments.length>2||new Set(assignments.map(a=>a.wearerKind+':'+a.wearerId)).size!==assignments.length)return fail('assignments_invalid');
  const assertions=[...proof.assertions,this.db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_inventory WHERE run_id=? AND account_id=? AND revision=?) AND (SELECT count(*) FROM run_equipment_instances WHERE run_id=?)=? THEN 1 ELSE 0 END`).bind(runId,accountId,inventory.revision,runId,inventory.items.length)];
  // Guard full original inventory, including displaced items, not just new slots.
  for(const i of inventory.items)assertions.push(this.db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_instances WHERE run_id=? AND instance_id=? AND source_item_id=? AND definition_id=? AND definition_version=? AND category_id=? AND set_tag=? AND star=? AND revision=? AND locked=? AND location_kind=? AND wearer_kind IS ? AND wearer_id IS ? AND slot_index IS ?) THEN 1 ELSE 0 END`).bind(runId,i.instance_id,i.source_item_id,i.definition_id,i.definition_version,i.category_id,i.set_tag,i.star,i.revision,i.locked,i.location_kind,i.wearer_kind,i.wearer_id,i.slot_index));
  const seen=new Set<string>(),actors=[];
  for(const assignment of assignments){
   const a=origin.actors.find(v=>v.wearerKind===assignment.wearerKind&&v.wearerId===assignment.wearerId);if(!a||assignment.slots.length!==3)return fail('wearer_not_bound');const inputs=[],sources=[];
   for(const slot of assignment.slots){if(!slot)continue;if(seen.has(slot.instanceId))return fail('duplicate_instance');seen.add(slot.instanceId);const i=inventory.items.find(v=>v.instance_id===slot.instanceId);if(!i||i.definition_id!==slot.definitionId||i.definition_version!==slot.definitionVersion||i.category_id!==slot.categoryId||i.set_tag!==slot.setTag||(i.instance_id===merge?.projection?.primary.instance_id?merge.receipt.star:i.star)!==slot.star)return fail('item_changed');if(i.definition_version!==equipmentCoreStatVersion)return fail('definition_unsupported');
    const effect=await new EquipmentEffectStateStore(this.db).read(accountId,runId,i.instance_id);if(!effect)return fail('effect_source_missing');if(effect.inventoryRevision!==inventory.revision||effect.instanceRevision!==i.revision)return fail('effect_changed');const snapshot=i.instance_id===merge?.projection?.primary.instance_id?merge.projection.snapshot:effect.snapshot;if(snapshot.uniqueEffects.length||snapshot.enchantment)return fail('effect_handler_unavailable');
    const input=itemSchema.safeParse({categoryId:i.category_id,star:slot.star,setTag:i.set_tag});if(!input.success)return fail('category_unsupported');inputs.push(input.data);sources.push({instanceId:i.instance_id,instanceRevision:i.instance_id===merge?.projection?.primary.instance_id?merge.receipt.instanceRevision:i.revision,effectRevision:i.instance_id===merge?.projection?.primary.instance_id?merge.receipt.effectRevision:effect.effectRevision,snapshot});
    assertions.push(this.db.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM run_equipment_effect_heads h JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision WHERE h.run_id=? AND h.instance_id=? AND h.revision=? AND json(l.snapshot_json)=json(?)) THEN 1 ELSE 0 END`).bind(runId,i.instance_id,effect.effectRevision,json(effect.snapshot)));
   }
   actors.push({wearerKind:a.wearerKind,wearerId:a.wearerId,targetId:a.targetId,base:a.base,slots:assignment.slots,sources,stats:calculateEquipmentCoreStats(a.base,inputs)});
  }
  const body={version:equipmentCoreStatVersion,accountId,runId,originalContext:origin,originalContextHash:proof.originalHash,inventoryRevision:inventory.revision,phase:'PROPOSED_ASSIGNMENT_NOT_COMMITTED',actors,fullRuntimeSupported:false as const};const snapshot={...body,packetHash:await digest(body)};
  return {snapshot,assertions};
 }
 async prepare(context:Parameters<EquipmentFinalHealthSource['prepare']>[0]){if(context.expectedStatVersion!==equipmentCoreStatVersion)return fail('version_conflict');const p=await this.capture(context.accountId,context.runId,context.assignments);return {statVersion:equipmentCoreStatVersion,assertions:p.assertions,targets:p.snapshot.actors.map(a=>({wearerKind:a.wearerKind,wearerId:a.wearerId,targetId:a.targetId,maxHp:a.stats.maxHp})),configuration:{sourceVersion:equipmentCoreStatVersion,sourceHash:p.snapshot.packetHash,observedInventoryRevision:p.snapshot.inventoryRevision,actors:p.snapshot.actors.map(a=>({wearerKind:a.wearerKind,wearerId:a.wearerId,targetId:a.targetId,slots:a.slots,stats:a.stats}))}};}
}
export type RunEquipmentStatSnapshot=Awaited<ReturnType<RunEquipmentStatOwner['capture']>>['snapshot'];
/** Trusted original snapshot + separately pinned hash, no current effect or policy lookup. */
export async function readPinnedRunEquipmentStats(input:unknown,original:RunEquipmentStatSnapshot,originalHash:unknown){const h=hash.safeParse(originalHash);const text=json(input);if(new TextEncoder().encode(text).length>512*1024)return fail('capacity');if(!h.success||h.data!==original.packetHash||text!==json(original))return fail('original_snapshot_conflict');const {packetHash,...body}=original;if(await digest(body)!==packetHash)return fail('original_snapshot_conflict');return structuredClone(original);}
export {originSchema as equipmentRunStatOriginSchema};
