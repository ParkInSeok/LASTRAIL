import {qualifyBCompletedProgress} from './combat-b-completed-progress';
import {readBOperationBinding,bOperationFail} from './combat-b-operation-binding';
import {BOperationProgressStore} from './combat-b-operation-progress';
import { qualifyObjectiveFinish, type ObjectiveMode } from "./recurring-policy";
const stories = ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show", "opn_05_night_boarders", "opn_06_broken_line"];
export async function objectiveFinishEvent(db: D1Database, input: { accountId: string; runId: string; operationId: string; contentVersion: string | null; outcome: string; completedWaves: number; fullCompletion: boolean }) {
  const original=await db.prepare('SELECT s.snapshot_json,m.state_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id LEFT JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=?').bind(input.runId,input.accountId).first<{snapshot_json:string;state_json:string|null}>();
  const b=original?await readBOperationBinding(JSON.parse(original.snapshot_json)):null;
  if(b){
    if(input.operationId!==b.operationId||input.contentVersion!==b.contentVersion)return bOperationFail('finish_binding_conflict');
    const progress=await new BOperationProgressStore(db).read(input.accountId,input.runId);
    if(input.completedWaves!==progress.completedWaves||input.fullCompletion!==progress.fullCompletion)return bOperationFail('finish_progress_conflict');
    if(input.outcome==='VICTORY'&&!progress.fullCompletion)return bOperationFail('objectives_incomplete');
    if(!['VICTORY','FAILURE','ABANDONED','INVALID','CANCELLED'].includes(input.outcome))return bOperationFail('outcome_invalid');
    const qualification=qualifyBCompletedProgress(input.outcome,{completedWaves:progress.completedWaves,completedNodes:progress.completedNodes,fullCompletion:progress.fullCompletion});
    // The same finish batch fences the immutable start, map revision and both journals.
    return db.prepare(`INSERT INTO objective_events(account_id,kind,source_id,occurred_at,mode,binding_version,status,ends,waves,source_json,b_completed_waves,b_completed_nodes,b_progress_policy_version)
      SELECT ?1,'FINISH',?2,CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER),'STORY_B_12',?3,CASE WHEN s.snapshot_json=?8 AND m.start_snapshot_json=?8 AND m.revision=?9 AND m.state_json=?12 AND f.outcome=?13 AND (SELECT count(*) FROM b_operation_wave_receipts WHERE run_id=?2)=?10 AND (SELECT count(*) FROM b_operation_node_receipts WHERE run_id=?2)=?11 THEN ?4 ELSE json('B-operation-finish-source-conflict') END,?5,0,?7,?6,?14,?15
      FROM runs r JOIN run_final_results f ON f.run_id=r.id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id
      WHERE r.id=?2 AND r.account_id=?1
      ON CONFLICT(account_id,kind,source_id) DO NOTHING`).bind(input.accountId,input.runId,b.version,qualification.status,qualification.ends,qualification.completedWaves,JSON.stringify({...input,completedNodes:progress.completedNodes,bindingHash:b.bindingHash,objectiveProgress:qualification}),original!.snapshot_json,progress.mapRevision,progress.completedWaves,progress.completedNodes,original!.state_json,input.outcome,qualification.completedNodes,qualification.policyVersion);
  }
  const binding = await db.prepare("SELECT mode,version FROM objective_operation_bindings WHERE operation_id=? AND content_version=?").bind(input.operationId, input.contentVersion).first<{ mode: ObjectiveMode; version: string }>();
  // The existing six story identities are not ordinary category IDs. Known source
  // mappings cannot be overridden to bypass the pending story-defeat contract.
  const known = stories.includes(input.operationId) ? "STORY_15" : input.operationId === "raid_raider_king_engine" ? "STORY_BOSS" : null;
  const mode: ObjectiveMode = known ?? binding?.mode ?? "MAPPING_MISSING";
  const qualification = qualifyObjectiveFinish(mode, input.outcome, input.completedWaves, input.fullCompletion);
  return db.prepare(`INSERT INTO objective_events(account_id,kind,source_id,occurred_at,mode,binding_version,status,ends,waves,source_json)
    SELECT ?1,'FINISH',?2,CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER),?3,?4,?5,?6,?7,?8
    FROM runs r JOIN run_final_results f ON f.run_id=r.id WHERE r.id=?2 AND r.account_id=?1
    ON CONFLICT(account_id,kind,source_id) DO NOTHING`)
    .bind(input.accountId, input.runId, mode, known ? "existing-story-identities-v1" : binding?.version ?? null,
      qualification.status, qualification.ends, qualification.waves, JSON.stringify(input));
}
export function objectiveLoginEvent(db: D1Database, accountId: string) {
  return db.prepare(`INSERT INTO objective_events(account_id,kind,source_id,occurred_at,mode,binding_version,status,ends,waves,source_json)
    SELECT ?,'LOGIN',CAST(CAST((clock.now-68400000)/86400000 AS INTEGER) AS TEXT),clock.now,'LOGIN','server-login-v1','ELIGIBLE',0,0,'{}'
    FROM (SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now) clock
    WHERE 1 ON CONFLICT(account_id,kind,source_id) DO NOTHING`).bind(accountId);
}
