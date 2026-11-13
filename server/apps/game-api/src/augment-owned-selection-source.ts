import {sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureAugmentSelectionSource} from './combat-augment-selection-source';
import {SoloAugmentSelection} from './solo-augment-selection';
/** Exact authoritative grant set, including inserts and immutable origin inputs. */
const fingerprint=`SELECT json_group_array(json_array(event_id,account_id,environment,stage_id,issue_hash,start_snapshot_json,initial_json,checkpoint_json,write_revision)) AS fingerprint
 FROM (SELECT * FROM solo_augment_grants WHERE run_id=? ORDER BY event_id)`;
/** Existing grant owner supplies acquisition facts, not runtime APPLIED. Used by
 * new selection eligibility; future node issuance can reuse this same source.
 * Missing/corrupt sources never become an empty owned set. No public endpoint. */
export async function captureOwnedAugmentSelections(db:D1Database,c:{accountId:string;runId:string;environment:string;startSnapshot:string}){
 if(!await db.prepare("SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=?")
  .bind(c.runId,c.accountId,c.startSnapshot).first())throw new ActivityError(409,'augment_owned_source_context_invalid');
 const before=await db.prepare(fingerprint).bind(c.runId).first<{fingerprint:string}>();
 if(!before)throw new ActivityError(503,'augment_owned_source_missing');
 const rows=await db.prepare('SELECT event_id,checkpoint_json,write_revision FROM solo_augment_grants WHERE run_id=? ORDER BY event_id').bind(c.runId)
  .all<{event_id:string;checkpoint_json:string;write_revision:number}>();
 const owned:{candidateId:string;eventId:string;catalogVersion:string;storeRevision:number;packetHash:string}[]=[];
 const startHash=await sha256Hex(c.startSnapshot);
 for(const row of rows.results){
  let selection:ReturnType<SoloAugmentSelection['read']>;
  try{selection=SoloAugmentSelection.restore(row.checkpoint_json,{extraApproval:{authorize:async()=>{throw Error('read_only');}}}).read();}
  catch{throw new ActivityError(503,'augment_owned_source_invalid');}
  const source=await captureAugmentSelectionSource(db,c.accountId,{runId:c.runId,eventId:row.event_id,environment:c.environment,expectedStoreRevision:row.write_revision,
   expectedCatalogVersion:selection.catalogVersion,expectedStartSnapshotHash:startHash});
  if(source.selection.state==='CONFIRMED')owned.push({candidateId:source.selection.selectedCandidateId!,eventId:source.eventId,catalogVersion:source.catalogVersion,storeRevision:source.storeRevision,packetHash:source.packetHash});
 }
 return {version:'combat.owned-augment-selections.v1.20261019',owned,applied:false as const,
  assertions:[db.prepare(`INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN (${fingerprint})=? THEN 1 ELSE 0 END`).bind(c.runId,before.fingerprint)]};
}
