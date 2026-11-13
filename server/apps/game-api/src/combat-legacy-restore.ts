import {ActivityError} from './activity-policy';
const presentSql=`SELECT EXISTS(SELECT 1 FROM run_tactical_ledger WHERE run_id=?)
 OR EXISTS(SELECT 1 FROM run_augment_history WHERE run_id=?) AS present`;
/** Compatibility boundary, not a complete transaction inventory.
 * Legacy v0005 rows have no installed replay/translation contract for ordinary coop.
 * Preserve them and reject NEW restoration; existing recovery receipts replay first.
 * Replace this absence guard only when versioned original-record interpretation,
 * application-cursor validation and same-batch source fencing are implemented.
 */
export async function prepareLegacyRestoreBoundary(db:D1Database,runId:string){
 const row=await db.prepare(presentSql).bind(runId,runId).first<{present:number}>();
 if(!row||row.present!==0)throw new ActivityError(409,'combat_host_legacy_transactions_unverified');
 return db.prepare(`INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN present=0 THEN 1 ELSE 0 END FROM (${presentSql})`).bind(runId,runId);
}
