import { canonicalJson } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { OperationMapStore } from './operation-map-store';
import { mapReceipt } from './operation-map-state';

/** Shared merchant eligibility read. Callers must fence the returned bytes in their write batch. */
export async function prepareMerchantMap(db: D1Database, accountId: string, runId: string, nodeId: string) {
  const map = await new OperationMapStore(db).read(accountId, runId);
  const node = map.view.nodes.find(item => item.nodeId === nodeId);
  if (map.state.phase !== 'ACTIVE' || map.view.currentNodeId !== nodeId || node?.eventKind !== 'MERCHANT') {
    throw new ActivityError(409, 'equipment_merchant_node_unavailable');
  }
  const mapRow = await db.prepare(`SELECT revision,plan_json,start_snapshot_json,state_json
    FROM operation_map_states WHERE run_id=?`).bind(runId)
    .first<{ revision: number; plan_json: string; start_snapshot_json: string; state_json: string }>();
  if (!mapRow || mapRow.revision !== map.revision
    || canonicalJson(JSON.parse(mapRow.state_json)) !== canonicalJson(map.state)) {
    throw new ActivityError(409, 'equipment_merchant_state_conflict');
  }
  const receipts = await db.prepare(`SELECT event_id,response_json FROM operation_map_receipts
    WHERE run_id=? AND json_extract(response_json,'$.revision')=?`).bind(runId, map.revision)
    .all<{ event_id: string; response_json: string }>();
  const arrivals = receipts.results.filter(row => {
    const receipt = mapReceipt.safeParse(JSON.parse(row.response_json));
    return receipt.success && receipt.data.eventId === row.event_id && receipt.data.verificationId !== null
      && canonicalJson(receipt.data.state) === canonicalJson(map.state);
  });
  if (arrivals.length !== 1) throw new ActivityError(409, 'equipment_merchant_arrival_unverified');
  return { mapRow, arrival: arrivals[0]!, visitDepth: node.visitDepth };
}
