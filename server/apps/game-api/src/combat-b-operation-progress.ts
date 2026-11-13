import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {readBOperationBinding,bOperationFail,type BOperationBinding,bOperationRuntime} from './combat-b-operation-binding';
import {captureOperationMap,projectOperationMap,type OperationMapPlan} from './operation-map-plan';
import {readMapProgress,type mapProgress} from './operation-map-state';
export const bOperationWaveCommandSchema=z.strictObject({eventId:z.string().min(1).max(200),nodeId:z.string().min(1).max(200),waveOrdinal:z.number().int().positive().max(128),expectedMapRevision:z.number().int().positive().safe(),evidenceRef:z.string().min(1).max(200)});
const waveSchema=bOperationWaveCommandSchema;
export interface BOperationWaveVerifier {
 /** Verify ACTUAL completed wave objectives and residual hazards at original solo authority.
  * Fence every original evidence row using operation_map_write_guard assertions, in the
  * same caller batch as the receipt. No client completed boolean, current catalog or
  * source rewriting. No production adapter is registered in this implementation. */
 prepare(context:{accountId:string;runId:string;startSnapshot:string;binding:BOperationBinding;command:z.infer<typeof waveSchema>}):Promise<{verificationId:string;assertions:D1PreparedStatement[]}>;
}
async function original(db:D1Database,accountId:string,runId:string){const row=await db.prepare('SELECT r.status,s.snapshot_json,m.revision,m.state_json,m.plan_json,m.start_snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=?').bind(runId,accountId).first<{status:string;snapshot_json:string;revision:number;state_json:string;plan_json:string;start_snapshot_json:string}>();if(!row)return bOperationFail('run_not_found');
 if(row.snapshot_json!==row.start_snapshot_json)return bOperationFail('snapshot_changed');const binding=await readBOperationBinding(JSON.parse(row.snapshot_json));if(!binding)return bOperationFail('version_unsupported');
 const plan=captureOperationMap(JSON.parse(row.plan_json)),snapshot=JSON.parse(row.snapshot_json);
 if(canonicalJson(plan)!==canonicalJson(snapshot.admission.operationMapPlan))return bOperationFail('map_conflict');return {...row,binding,plan,state:readMapProgress(plan,JSON.parse(row.state_json))};}
/** Journals are derived evidence; MapStore alone owns node progress. */
export class BOperationProgressStore {
 constructor(private readonly db:D1Database,private readonly verifier?:BOperationWaveVerifier){}
 async read(accountId:string,runId:string){const row=await original(this.db,accountId,runId);
  const waves=await this.db.prepare("SELECT node_id,wave_ordinal,json_extract(response_json,'$.bindingHash') AS binding_hash FROM b_operation_wave_receipts WHERE run_id=? ORDER BY node_id,wave_ordinal").bind(runId).all<{node_id:string;wave_ordinal:number;binding_hash:string}>();
  const nodes=await this.db.prepare('SELECT node_id,binding_hash FROM b_operation_node_receipts WHERE run_id=?').bind(runId).all<{node_id:string;binding_hash:string}>();
  const path=row.state.traversedEdgeIds.map(edge=>row.plan.edges.find(e=>e.edgeId===edge)!.toNodeId),pathSet=new Set(path);
  if(nodes.results.some(n=>!pathSet.has(n.node_id)||n.binding_hash!==row.binding.bindingHash)||waves.results.some(w=>!pathSet.has(w.node_id)||w.binding_hash!==row.binding.bindingHash))return bOperationFail('progress_conflict');
  for(const nodeId of path){const completed=waves.results.filter(w=>w.node_id===nodeId),required=row.binding.nodeWaves.find(n=>n.nodeId===nodeId)!.waves;
   if(required===null&&completed.length||required!==null&&(completed.length>required||completed.some((w,i)=>w.wave_ordinal!==i+1)))return bOperationFail('progress_conflict');
   if(nodes.results.some(n=>n.node_id===nodeId)&&(required===null||completed.length!==required))return bOperationFail('progress_conflict');}
  const fullCompletion=row.state.phase==='FINAL_NODE_COMPLETE'&&row.state.holdIds.length===0&&path.length===12&&nodes.results.length===12&&path.every(id=>nodes.results.some(n=>n.node_id===id));
  return {version:'operation.b-progress.v1.20261004',bindingHash:row.binding.bindingHash,mapRevision:row.revision,completedWaves:waves.results.length,completedNodes:nodes.results.length,fullCompletion,...bOperationRuntime};
 }
 async completeWave(accountId:string,runId:string,input:unknown){const c=waveSchema.parse(input),row=await original(this.db,accountId,runId),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const saved=await this.db.prepare('SELECT request_hash,response_json FROM b_operation_wave_receipts WHERE run_id=? AND event_id=?').bind(runId,c.eventId).first<{request_hash:string;response_json:string}>();if(!saved)return null;if(saved.request_hash!==hash)return bOperationFail('event_conflict');return JSON.parse(saved.response_json);};
  const old=await replay();if(old)return old;
  if(row.status!=='ACTIVE'||row.state.phase!=='ACTIVE'||row.state.holdIds.length||row.revision!==c.expectedMapRevision||projectOperationMap(row.plan,row.state.traversedEdgeIds).currentNodeId!==c.nodeId)return bOperationFail('wave_state_conflict');
  const required=row.binding.nodeWaves.find(n=>n.nodeId===c.nodeId)?.waves;if(required===null||required===undefined)return bOperationFail('wave_contract_unsupported');if(c.waveOrdinal>required)return bOperationFail('wave_order_invalid');
  const prior=await this.db.prepare('SELECT count(*) AS n FROM b_operation_wave_receipts WHERE run_id=? AND node_id=?').bind(runId,c.nodeId).first<{n:number}>();if(c.waveOrdinal!==prior!.n+1)return bOperationFail('wave_order_invalid');
  if(!this.verifier)return bOperationFail('verification_required');const proof=await this.verifier.prepare({accountId,runId,startSnapshot:row.snapshot_json,binding:structuredClone(row.binding),command:structuredClone(c)});
  if(!proof||!proof.assertions?.length||!z.string().min(1).max(200).safeParse(proof.verificationId).success)return bOperationFail('verification_invalid');
  const response={version:'operation.b-wave-receipt.v1.20261004',eventId:c.eventId,nodeId:c.nodeId,waveOrdinal:c.waveOrdinal,bindingHash:row.binding.bindingHash,verificationId:proof.verificationId};
  try{await this.db.batch([...proof.assertions,this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) AND (SELECT count(*) FROM b_operation_wave_receipts WHERE run_id=? AND node_id=?)=? THEN 1 ELSE 0 END`).bind(runId,accountId,row.snapshot_json,row.revision,row.state_json,row.plan_json,runId,c.nodeId,c.waveOrdinal-1),this.db.prepare('INSERT INTO b_operation_wave_receipts VALUES(?,?,?,?,?,?)').bind(runId,c.eventId,c.nodeId,c.waveOrdinal,hash,canonicalJson(response)),this.db.prepare('DELETE FROM operation_map_write_guard')]);}catch(error){const committed=await replay();if(committed)return committed;if(/valid=1|UNIQUE constraint/iu.test(String(error)))return bOperationFail('wave_state_conflict');throw error;}return response;
 }
}
/** Called ONLY by verified MapStore COMPLETE, within its original transaction. */
export async function bOperationNodeStatements(db:D1Database,snapshotText:string,runId:string,plan:OperationMapPlan,state:z.infer<typeof mapProgress>,nodeId:string,eventId:string,verificationId:string){
 const binding=await readBOperationBinding(JSON.parse(snapshotText));if(!binding)return [];
 if(canonicalJson(plan)!==canonicalJson(JSON.parse(snapshotText).admission.operationMapPlan)||projectOperationMap(plan,state.traversedEdgeIds).currentNodeId!==nodeId)return bOperationFail('map_conflict');
 const expected=binding.nodeWaves.find(n=>n.nodeId===nodeId)?.waves;if(expected===null||expected===undefined)return bOperationFail('wave_contract_unsupported');
 return [db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN (SELECT count(*) FROM b_operation_wave_receipts WHERE run_id=? AND node_id=?)=? AND NOT EXISTS(SELECT 1 FROM b_operation_wave_receipts WHERE run_id=? AND node_id=? AND wave_ordinal>?) THEN 1 ELSE 0 END').bind(runId,nodeId,expected,runId,nodeId,expected),db.prepare('INSERT INTO b_operation_node_receipts VALUES(?,?,?,?,?)').bind(runId,nodeId,eventId,verificationId,binding.bindingHash)];
}