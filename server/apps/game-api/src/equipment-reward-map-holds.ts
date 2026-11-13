import {canonicalJson, sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {EquipmentRewardBundleStore, claimReceipt, declineReceipt} from './equipment-reward-bundles';
import {mapReceipt} from './operation-map-state';
import type {OperationMapVerifier} from './operation-map-store';

const fail = (reason: string): never => { throw new ActivityError(409, 'equipment_reward_hold_' + reason); };
type BundleRow = {bundle_id:string; source_ref:string; source_version:string; payload_hash:string};
const itemsQuery = `SELECT json_group_array(json_object('itemId',item_id,'instanceId',instance_id,'position',sort_index,'status',status,'body',item_json)) AS value
 FROM (SELECT * FROM run_equipment_reward_items WHERE run_id=? AND bundle_id=? ORDER BY sort_index)`;
const receiptsQuery = `SELECT json_group_array(json_object('id',command_id,'kind',command_kind,'hash',request_hash,'body',response_json)) AS value
 FROM (SELECT * FROM run_equipment_receipts WHERE run_id=? AND command_kind IN ('CLAIM_ITEMS','DECLINE_REMAINING')
 AND json_extract(response_json,'$.bundleId')=? ORDER BY command_id)`;

/** Links existing fixed rewards to map holds. Does not issue rewards, simulate combat,
 * or treat result-consistency authorization as reward authorization. */
export class EquipmentRewardMapHolds {
 constructor(private readonly db:D1Database) {}

 private async capture(accountId:string,runId:string,nodeId:string) {
  const row=await this.db.prepare('SELECT bundle_id,source_ref,source_version,payload_hash FROM run_equipment_reward_bundles WHERE run_id=? AND node_id=?')
   .bind(runId,nodeId).first<BundleRow>();
  if(!row)return null;
  const bundle=await new EquipmentRewardBundleStore(this.db).read(accountId,runId,row.bundle_id);
  if(bundle.sourceRef!==row.source_ref||bundle.sourceVersion!==row.source_version) return fail('source_changed');
  if(bundle.items.some(i=>!['PENDING','CLAIMED','FORFEITED'].includes(i.status)))return fail('item_state_invalid');
  const items=(await this.db.prepare(itemsQuery).bind(runId,row.bundle_id).first<{value:string}>())!.value;
  // The bundle read and raw capture must describe the same observation, including status.
  const raw=JSON.parse(items) as {itemId:string;instanceId:string;position:number;status:string;body:string}[];
  if(raw.length!==bundle.items.length||raw.some((r,i)=>r.position!==i||r.itemId!==bundle.items[i]!.itemId||r.instanceId!==bundle.items[i]!.instanceId||r.status!==bundle.items[i]!.status))return fail('source_changed');
  const assertions=[this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN
   EXISTS(SELECT 1 FROM run_equipment_reward_bundles WHERE run_id=? AND node_id=? AND bundle_id=? AND source_ref=? AND source_version=? AND payload_hash=?)
   AND (${itemsQuery})=? THEN 1 ELSE 0 END`).bind(runId,nodeId,row.bundle_id,row.source_ref,row.source_version,row.payload_hash,runId,row.bundle_id,items)];
  return {row,bundle,assertions};
 }

 async completion(accountId:string,runId:string,nodeId:string,evidenceRef:string) {
  const captured=await this.capture(accountId,runId,nodeId);
  if(!captured)return {holdIds:[],assertions:[this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN
   NOT EXISTS(SELECT 1 FROM run_equipment_reward_bundles WHERE run_id=? AND node_id=?) THEN 1 ELSE 0 END`).bind(runId,nodeId)]};
  if(captured.row.source_ref!==evidenceRef)return fail('source_conflict');
  return {holdIds:captured.bundle.items.some(i=>i.status==='PENDING')?[captured.row.bundle_id]:[],assertions:captured.assertions};
 }

 async release(context:Parameters<OperationMapVerifier['prepare']>[0],nodeId:string) {
  const c=context.command;
  if(c.kind!=='RELEASE_HOLD')return null;
  const captured=await this.capture(context.accountId,context.runId,nodeId);
  if(!captured||captured.row.bundle_id!==c.holdId)return null;
  if(!context.state.holdIds.includes(c.holdId)||!['READY','FINAL_NODE_COMPLETE'].includes(context.state.phase))return fail('not_active');
  if(captured.bundle.items.some(i=>i.status==='PENDING'))return fail('pending');
  const link=await this.db.prepare('SELECT evidence_ref,map_revision FROM b_result_node_links WHERE run_id=? AND node_id=?')
   .bind(context.runId,nodeId).first<{evidence_ref:string;map_revision:number}>();
  if(!link||link.evidence_ref!==captured.row.source_ref)return fail('completion_missing');
  const completed=await this.db.prepare("SELECT event_id,response_json FROM operation_map_receipts WHERE run_id=? AND json_extract(response_json,'$.revision')=?")
   .bind(context.runId,link.map_revision+1).first<{event_id:string;response_json:string}>();
  if(!completed||!mapReceipt.parse(JSON.parse(completed.response_json)).state.holdIds.includes(c.holdId))return fail('completion_missing');
  const receipts=(await this.db.prepare(receiptsQuery).bind(context.runId,c.holdId).first<{value:string}>())!.value;
  const rows=JSON.parse(receipts) as {id:string;kind:string;hash:string;body:string}[];
  const covered=new Map<string,string>();let referenced=false;
  for(const row of rows){
   const claimed=row.kind==='CLAIM_ITEMS';
   const receipt=claimed?claimReceipt.parse(JSON.parse(row.body)):declineReceipt.parse(JSON.parse(row.body));
   const ids='claimedItemIds' in receipt?receipt.claimedItemIds:receipt.declinedItemIds;
   if(receipt.commandId!==row.id||receipt.bundleId!==c.holdId||receipt.inventoryRevision<2||new Set(ids).size!==ids.length)return fail('receipt_invalid');
   const expectedHash=await sha256Hex(canonicalJson({commandId:row.id,bundleId:c.holdId,itemIds:[...ids].sort(),expectedInventoryRevision:receipt.inventoryRevision-1}));
   if(expectedHash!==row.hash)return fail('receipt_invalid');
   for(const itemId of ids){if(covered.has(itemId))return fail('receipt_invalid');covered.set(itemId,claimed?'CLAIMED':'FORFEITED');}
   if(row.id===c.evidenceRef)referenced=true;
  }
  if(!referenced||covered.size!==captured.bundle.items.length||captured.bundle.items.some(i=>covered.get(i.itemId)!==i.status))return fail('receipt_missing');
  return {proof:{kind:'RELEASE_HOLD' as const,verificationId:c.evidenceRef,holdIds:[]},assertions:[...captured.assertions,
   this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN
    EXISTS(SELECT 1 FROM b_result_node_links WHERE run_id=? AND node_id=? AND evidence_ref=? AND map_revision=?)
    AND EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?)
    AND (${receiptsQuery})=? THEN 1 ELSE 0 END`).bind(context.runId,nodeId,link.evidence_ref,link.map_revision,context.runId,completed.event_id,completed.response_json,context.runId,c.holdId,receipts)]};
 }
}
