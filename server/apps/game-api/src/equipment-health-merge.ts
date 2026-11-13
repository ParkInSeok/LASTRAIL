import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {EquipmentInventoryStore} from './equipment-inventory-start';
import {EquipmentMergeStore,equipmentMergeCommandSchema,equipmentMergeReceiptSchema,equipmentMergeRequestValue,type PreparedEquipmentMerge} from './equipment-merge';
import {CombatHealthStore,combatHealthReceiptSchema} from './combat-health-store';
const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
export const equipmentHealthMergeVersion='equipment.health-merge.v1.20261005';
const commandSchema=z.strictObject({version:z.literal(equipmentHealthMergeVersion),merge:equipmentMergeCommandSchema,expectedHealthRevision:z.number().int().min(1).max(Number.MAX_SAFE_INTEGER-2),expectedStatVersion:id});
const targetSchema=z.strictObject({wearerKind:z.enum(['operator','turret']),wearerId:id,targetId:id,maxHp:z.number().int().min(1).max(Number.MAX_SAFE_INTEGER)});
const receiptSchema=z.strictObject({version:z.literal(equipmentHealthMergeVersion),commandId:id,statVersion:id,merge:equipmentMergeReceiptSchema,targets:z.array(targetSchema).min(1).max(2),health:z.array(combatHealthReceiptSchema).min(1).max(2)});
export interface EquipmentMergedHealthSource {
 prepareMerged(context:{accountId:string;runId:string;expectedStatVersion:string;merge:PreparedEquipmentMerge}):Promise<{statVersion:string;targets:unknown[];assertions:D1PreparedStatement[]}>;
}
/** One coordinator over original merge and HP owners; no duplicated merge/HP rules.
 * Internal only. Original safety, source and health verification remain mandatory. */
export class EquipmentHealthMergeStore {
 constructor(private readonly db:D1Database,private readonly equipment:EquipmentMergeStore,private readonly health:CombatHealthStore,private readonly source:EquipmentMergedHealthSource){}
 async merge(accountId:string,runId:string,raw:unknown){
  const parsed=commandSchema.safeParse(raw);if(!parsed.success)throw new ActivityError(400,'equipment_health_merge_command_invalid');
  const command=parsed.data,requestHash=await sha256Hex(canonicalJson({...command,merge:equipmentMergeRequestValue(command.merge)}));
  if(!await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId,runId))throw new ActivityError(409,'equipment_inventory_legacy_unsupported');
  const eventId=async(target:z.infer<typeof targetSchema>)=>'eqmergehp:'+await sha256Hex(canonicalJson({runId,requestHash,wearerKind:target.wearerKind,wearerId:target.wearerId,targetId:target.targetId}));
  const replay=async()=>{
   const row=await this.db.prepare('SELECT request_hash,response_json,response_hash FROM run_equipment_health_merge_links WHERE run_id=? AND command_id=?').bind(runId,command.merge.commandId).first<{request_hash:string;response_json:string;response_hash:string}>();
   if(!row)return null;if(row.request_hash!==requestHash)throw new ActivityError(409,'equipment_health_merge_request_conflict');
   try{
    const receipt=receiptSchema.parse(JSON.parse(row.response_json));
    if(await sha256Hex(canonicalJson(receipt))!==row.response_hash||receipt.commandId!==command.merge.commandId||receipt.merge.commandId!==command.merge.commandId||receipt.statVersion!==command.expectedStatVersion||receipt.targets.length!==receipt.health.length||new Set(receipt.targets.map(t=>t.targetId)).size!==receipt.targets.length||new Set(receipt.targets.map(t=>t.wearerKind+':'+t.wearerId)).size!==receipt.targets.length)throw new Error('linked receipt mismatch');
    for(const [n,t]of receipt.targets.entries()){const h=receipt.health[n]!;if(h.eventId!==await eventId(t)||h.targetId!==t.targetId||h.change.kind!=='MAX_HP_REBASE'||h.change.maxHp!==t.maxHp||h.revision!==command.expectedHealthRevision+n+1)throw new Error('linked target mismatch');}
    const eq=await this.equipment.prepareMerge(accountId,runId,command.merge),hp=await this.health.prepareChanges(accountId,runId,receipt.health.map((h,n)=>({eventId:h.eventId,targetId:h.targetId,expectedRevision:command.expectedHealthRevision+n,change:h.change})));
    if(eq.statements.length||hp.statements.length||canonicalJson(eq.receipt)!==canonicalJson(receipt.merge)||canonicalJson(hp.receipts)!==canonicalJson(receipt.health))throw new Error('linked original missing');
    return receipt;
   }catch{throw new ActivityError(503,'equipment_health_merge_receipt_invalid');}
  };
  const original=await replay();if(original)return original;
  const eq=await this.equipment.prepareMerge(accountId,runId,command.merge);if(!eq.statements.length||!eq.projection)throw new ActivityError(409,'equipment_health_merge_partial_replay');
  const affected=new Set([eq.projection.primary,eq.projection.secondary].filter(i=>i.location_kind==='EQUIPPED').map(i=>i.wearer_kind+':'+i.wearer_id));if(!affected.size)throw new ActivityError(409,'equipment_health_merge_equipped_required');
  const proof=await this.source.prepareMerged({accountId,runId,expectedStatVersion:command.expectedStatVersion,merge:eq}),targets=z.array(targetSchema).min(1).max(2).safeParse(proof?.targets);
  if(!targets.success||proof.statVersion!==command.expectedStatVersion||!Array.isArray(proof.assertions)||!proof.assertions.length||targets.data.length!==affected.size||new Set(targets.data.map(t=>t.targetId)).size!==targets.data.length||new Set(targets.data.map(t=>t.wearerKind+':'+t.wearerId)).size!==affected.size||targets.data.some(t=>!affected.has(t.wearerKind+':'+t.wearerId)))throw new ActivityError(409,'equipment_health_merge_stat_invalid');
  const ordered=targets.data.sort((a,b)=>{const x=a.wearerKind+':'+a.wearerId,y=b.wearerKind+':'+b.wearerId;return x<y?-1:x>y?1:0;}),changes=[];
  for(const [n,t]of ordered.entries())changes.push({eventId:await eventId(t),targetId:t.targetId,expectedRevision:command.expectedHealthRevision+n,change:{kind:'MAX_HP_REBASE' as const,maxHp:t.maxHp}});
  const hp=await this.health.prepareChanges(accountId,runId,changes);if(!hp.statements.length)throw new ActivityError(409,'equipment_health_merge_partial_replay');
  const receipt=receiptSchema.parse({version:equipmentHealthMergeVersion,commandId:command.merge.commandId,statVersion:proof.statVersion,merge:eq.receipt,targets:ordered,health:hp.receipts}),responseJson=canonicalJson(receipt),responseHash=await sha256Hex(responseJson);
  try{await this.db.batch([...proof.assertions,...eq.statements,...hp.statements,this.db.prepare('DELETE FROM equipment_placement_write_guard'),this.db.prepare('INSERT INTO run_equipment_health_merge_links(run_id,command_id,request_hash,response_json,response_hash) VALUES(?,?,?,?,?)').bind(runId,command.merge.commandId,requestHash,responseJson,responseHash)]);}
  catch(error){const completed=await replay();if(completed)return completed;if(/valid=1|UNIQUE constraint failed|equipment_merge_material_invalid|equipment_effect_revision_invalid/iu.test(String(error)))throw new ActivityError(409,'equipment_health_merge_state_conflict');throw error;}
  return receipt;
 }
}
