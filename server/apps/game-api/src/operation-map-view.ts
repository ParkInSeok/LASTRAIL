import {verifyPinnedOperationMapContent} from "./operation-map-publisher";
import {canonicalJson} from '@lastrail/common';
import {captureOperationMap,projectOperationMap} from './operation-map-plan';
import {readDepartureMap} from './operation-map-departure';
import {mapId,mapFail,readMapProgress} from './operation-map-state';
import {z} from 'zod';
const attemptReferences=z.array(z.strictObject({nodeId:mapId,generation:z.number().int().positive().safe(),
 snapshotHash:z.string().regex(/^[a-f0-9]{64}$/u),startMatches:z.literal(1)}));
/** Display-only, one consistent DB read. Does not authorize commands or create missing state. */
export async function readOperationMapView(db:D1Database,accountId:string,runId:string){
 mapId.parse(accountId);mapId.parse(runId);
 const row=await db.prepare(`SELECT r.status,m.revision,m.plan_json,m.state_json,
 m.start_snapshot_json,s.snapshot_json,
 (SELECT json_group_array(json_object('nodeId',n.node_id,'generation',n.generation,
   'snapshotHash',n.snapshot_hash,'startMatches',n.start_snapshot_json=s.snapshot_json))
  FROM ordinary_node_attempts n WHERE n.run_id=r.id) AS attempt_references FROM runs r
 LEFT JOIN operation_map_states m ON m.run_id=r.id
 LEFT JOIN run_snapshots s ON s.run_id=r.id
 WHERE r.id=? AND r.account_id=?`).bind(runId,accountId).first<{
  status:string;revision:number|null;plan_json:string|null;state_json:string|null;start_snapshot_json:string|null;snapshot_json:string|null;attempt_references:string;
 }>();
 if(!row)return mapFail('run_not_found');
 if(row.revision===null||!row.plan_json||!row.state_json||!row.start_snapshot_json)return mapFail('state_missing');
 if(row.start_snapshot_json!==row.snapshot_json)return mapFail('start_changed');
 const plan=captureOperationMap(JSON.parse(row.plan_json));
 const original=readDepartureMap(JSON.parse(row.start_snapshot_json));
 if(!original||canonicalJson(original)!==canonicalJson(plan))return mapFail('content_plan_conflict');
 await verifyPinnedOperationMapContent(JSON.parse(row.start_snapshot_json),plan);
 const state=readMapProgress(plan,JSON.parse(row.state_json));
 const view=projectOperationMap(plan,state.traversedEdgeIds);
 const references=attemptReferences.safeParse(JSON.parse(row.attempt_references));
 if(!references.success)return mapFail('attempt_source_conflict');
 const attempt=references.data.find(a=>a.nodeId===view.currentNodeId);
 const currentAttempt=attempt&&row.status==='ACTIVE'&&state.phase==='ACTIVE'
  ?{version:'ordinary.current-attempt.v1.20261102' as const,accountId,runId,nodeId:attempt.nodeId,
    generation:attempt.generation,mapRevision:row.revision,snapshotHash:attempt.snapshotHash}:null;
 return {version:'operation.map-status.v1' as const,runId,runStatus:row.status,revision:row.revision,
  progress:{phase:state.phase,pendingEdgeId:state.pendingEdgeId,blockedByPendingTransactions:state.holdIds.length>0},
  view,currentAttempt};
}
