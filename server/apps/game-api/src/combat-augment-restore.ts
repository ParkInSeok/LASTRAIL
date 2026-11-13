import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
/** One SQLite statement captures the committed sources consistently. This is an
 * internal restoration journal, never a client payload or proof of combat restoration. */
const journalSql=`SELECT json_group_array(json_array(kind,item_key,payload)) AS snapshot_json FROM (
 SELECT 'GRANT' AS kind,event_id AS item_key,json_object('checkpoint',checkpoint_json,'revision',write_revision,'owner',account_id,'environment',environment,'start',start_snapshot_json) AS payload FROM solo_augment_grants WHERE run_id=?
 UNION ALL SELECT 'SESSION',id,json_object('session',session_json,'status',status,'deadline',callback_deadline) FROM augment_ad_sessions WHERE run_id=?
 UNION ALL SELECT 'APPROVAL',id,json_object('event',event_id,'slot',slot_index,'account',account_id,'environment',environment,'target',target_json,'cause',cause,'order',order_id,'revision',entitlement_revision,'time',approved_at,'intent',ad_intent_id,'transaction',provider_transaction_id) FROM augment_extra_approvals WHERE run_id=?
 UNION ALL SELECT 'RESULT',r.intent_id,json_object('transaction',r.provider_transaction_id,'result',r.result_json) FROM augment_ad_results r JOIN augment_ad_sessions s ON s.id=r.intent_id WHERE s.run_id=?
 UNION ALL SELECT 'COMMAND',event_id||':'||request_id,json_object('event',event_id,'request',request_id,'hash',request_hash,'result',response_json) FROM solo_augment_receipts WHERE run_id=?
 ORDER BY kind,item_key,payload
)`;
export type AugmentRestoreJournal={version:'combat.augment-restore.v1';hasEntries:boolean;cursor:string;snapshotJson:string};
export async function prepareAugmentRestore(db:D1Database,runId:string){
 const args=[runId,runId,runId,runId,runId];
 const row=await db.prepare(journalSql).bind(...args).first<{snapshot_json:string}>();
 if(!row)throw new ActivityError(409,'combat_host_augment_journal_missing');
 const entries=z.array(z.tuple([z.string(),z.string(),z.string()])).parse(JSON.parse(row.snapshot_json));
 if(entries.some(([kind,,payload])=>kind==='SESSION'&&z.object({status:z.string()}).parse(JSON.parse(payload)).status==='AD_PENDING'))throw new ActivityError(409,'combat_host_augment_pending');
 const journal:AugmentRestoreJournal={version:'combat.augment-restore.v1',hasEntries:entries.length>0,cursor:'augment_'+await sha256Hex(canonicalJson({runId,snapshot:row.snapshot_json})),snapshotJson:row.snapshot_json};
 const assertion=db.prepare(`INSERT INTO combat_host_write_guard(valid) SELECT CASE WHEN snapshot_json=? THEN 1 ELSE 0 END FROM (${journalSql})`).bind(row.snapshot_json,...args);
 return {journal,assertion};
}
