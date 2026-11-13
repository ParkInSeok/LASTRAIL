import type { Database } from "sql.js";
/** TEST ONLY: seed a previously committed historical run, never use the retired public producer. */
export function seedHistoricalStart(db: Database, previewId: string, idempotencyKey: string) {
 const statement=db.prepare("SELECT * FROM operation_previews WHERE id=?");
 let row: ReturnType<typeof statement.getAsObject>;
 try { row=statement.getAsObject([previewId]); } finally { statement.free(); }
 if(!row.account_id) throw new Error("historical fixture preview missing");
 const metadata=JSON.parse(String(row.preview_json)), raid=metadata.encounterType==="RAID_BOSS";
 const runId=`run_history_${previewId}`,now=Date.now();
 const snapshot={runId,operationPreviewId:previewId,operationId:metadata.operationId,actId:metadata.actId,
  regionId:metadata.regionId,encounterType:metadata.encounterType,riskLevel:row.risk,risk:row.risk,
  balanceVersion:String(row.balance_config_version),contentVersion:metadata.contentVersion,
  ordinary:raid?null:{totalWaves:15,startWaveNumber:1},
  raid:raid?{raidId:metadata.raidId,bossId:metadata.bossId,bossPhaseRefs:metadata.bossPhaseRefs}:null,
  raidId:raid?metadata.raidId:null,bossId:raid?metadata.bossId:null,bossPhaseRefs:raid?metadata.bossPhaseRefs:[],
  allowedOperatorSlots:metadata.allowedOperatorSlots,startAugmentPackageRef:metadata.startAugmentPackageRef,
  startTacticPackageRef:metadata.startTacticPackageRef,runVersionSnapshot:null,serverStartedAt:new Date(now).toISOString()};
 db.run("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,?,?,?,?,'ACTIVE',?)",
  [runId,row.account_id!,previewId,row.mutation_id!,row.balance_config_version!,now]);
 db.run("INSERT INTO run_snapshots(run_id,snapshot_json,created_at) VALUES(?,?,?)",[runId,JSON.stringify(snapshot),now]);
 db.run("INSERT INTO idempotency_records(id,account_id,idempotency_key,operation,result_ref,created_at) VALUES(?,?,?,'run_start',?,?)",
  [`idem_history_${previewId}`,row.account_id!,idempotencyKey,runId,now]);
 db.run("UPDATE operation_previews SET status='CONSUMED' WHERE id=?",[previewId]);
}
