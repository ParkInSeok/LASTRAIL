/** Fence the source already read by the finish owner, without inventing expiry or result policy. */
export function prepareRunFinishCommitGuard(db:D1Database,source:{runId:string;accountId:string;status:string;
 balanceVersion:number;snapshotJson:string|null}){
 return db.prepare(`INSERT INTO run_finish_write_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id LEFT JOIN run_snapshots s ON s.run_id=r.id
   WHERE r.id=? AND r.account_id=? AND a.status='ACTIVE' AND r.status=?
     AND r.status IN ('ACTIVE','FINISH_PENDING') AND r.balance_config_version=? AND s.snapshot_json IS ?
     AND NOT EXISTS(SELECT 1 FROM run_final_results f WHERE f.run_id=r.id)) THEN 1 ELSE 0 END`)
   .bind(source.runId,source.accountId,source.status,source.balanceVersion,source.snapshotJson);
}
