import {z} from "zod";
import {canonicalJson} from "@lastrail/common";
import {readCombatAuthority} from "./combat-authority";
import {captureOperationMap} from "./operation-map-plan";
import {initialMapProgress,mapFail} from "./operation-map-state";
/** Reads only server-owned pinned metadata. Absence is historical, never a generated default. */
export function readDepartureMap(input:unknown){
 const p=z.object({encounterType:z.string().optional(),admission:z.object({operationMapPlan:z.unknown().optional(),combatAuthority:z.unknown().optional()}).passthrough()}).parse(input);
 const hasPlan=Object.hasOwn(p.admission,"operationMapPlan");
 if(p.encounterType!=="ORDINARY_12_NODE"){if(hasPlan)return mapFail("profile_unsupported");return null;}
 if(!hasPlan)return mapFail("plan_missing");
 if(readCombatAuthority(p.admission.combatAuthority).contract?.mode!=="ORDINARY_SOLO")return mapFail("mode_unsupported");
 return captureOperationMap(p.admission.operationMapPlan);
}
/** Compose after runs/run_snapshots insertion in the same D1 batch; never commit separately.
 * Snapshot bytes are shared with START. This seam does not prove catalog support/readiness.
 */
export function operationMapStartStatements(db:D1Database,accountId:string,runId:string,snapshotJson:string){
 const plan=readDepartureMap(JSON.parse(snapshotJson));if(!plan)return [];
 return [
  db.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND s.snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM operation_map_states WHERE run_id=?) THEN 1 ELSE 0 END").bind(runId,accountId,snapshotJson,runId),
  db.prepare("INSERT INTO operation_map_states VALUES(?,1,?,?,?)").bind(runId,canonicalJson(plan),snapshotJson,canonicalJson(initialMapProgress())),
  db.prepare("DELETE FROM operation_map_write_guard")
 ];
}
