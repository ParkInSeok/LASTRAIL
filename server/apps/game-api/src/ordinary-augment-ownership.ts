import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {SoloAugmentSelection} from './solo-augment-selection';
import {readO22ApplicationOriginal} from './augment-lifesteal-original';

// One SQL snapshot of the run's real selections, including confirmed absence.
// Acquisition is not an APPLIED receipt; the current combat owner installs it.
export async function readOrdinaryAugmentOwnership(db:D1Database,accountId:string,runId:string){
 const fail=():never=>{throw new ActivityError(409,'ordinary_augment_original_unavailable');};
 const row=await db.prepare(`SELECT r.status,s.snapshot_json,
  (SELECT json_group_array(json_object('account',g.account_id,'event',g.event_id,'environment',g.environment,'start',g.start_snapshot_json,'revision',g.write_revision,'checkpoint',g.checkpoint_json)) FROM solo_augment_grants g WHERE g.run_id=r.id) grants,
  (SELECT json_group_array(json_object('account',o.account_id,'event',o.grant_event_id,'effect',o.effect_id,'hash',o.original_hash,'original',o.original_json)) FROM run_augment_effect_originals o WHERE o.run_id=r.id) originals,
  (SELECT count(*) FROM run_augment_history h WHERE h.run_id=r.id) legacy
  FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`)
  .bind(runId,accountId).first<{status:string;snapshot_json:string;grants:string;originals:string;legacy:number}>();
 if(!row||row.status!=='ACTIVE'||row.legacy!==0)return fail();
 const selected:{effectId:string;eventId:string;revision:number;checkpointHash:string}[]=[];
 const originals=JSON.parse(row.originals) as {account:string;event:string;effect:string;hash:string;original:string}[];
 let o22Original:Awaited<ReturnType<typeof readO22ApplicationOriginal>>|null=null;
 for(const grant of JSON.parse(row.grants) as {account:string;event:string;environment:string;start:string;revision:number;checkpoint:string}[]){
  if(grant.account!==accountId||grant.start!==row.snapshot_json)return fail();
  const selection=SoloAugmentSelection.restore(grant.checkpoint,{extraApproval:{authorize:async()=>fail()}}).read();
  if(selection.state!=='CONFIRMED'||selection.runId!==runId||selection.eventId!==grant.event||!selection.selectedCandidateId)return fail();
  selected.push({effectId:selection.selectedCandidateId,eventId:grant.event,revision:grant.revision,checkpointHash:await sha256Hex(grant.checkpoint)});
  if(selection.selectedCandidateId==='O22'){
   const proof=originals.filter(o=>o.effect==='O22'&&o.event===grant.event&&o.account===accountId);
   if(proof.length!==1||o22Original||await sha256Hex(proof[0]!.original)!==proof[0]!.hash)return fail();
   o22Original=await readO22ApplicationOriginal(JSON.parse(proof[0]!.original),{accountId,runId,eventId:grant.event,environment:grant.environment},selection);
  }
 }
 if(originals.some(o=>o.effect!=='O22')||originals.length!==(o22Original?1:0))return fail();
 selected.sort((a,b)=>a.eventId.localeCompare(b.eventId));
 const body={version:'ordinary.augment-ownership.v1.20261109',accountId,runId,startSnapshotHash:await sha256Hex(row.snapshot_json),selected,o22Original};
 return {...body,packetHash:await sha256Hex(canonicalJson(body))};
}

export async function ordinaryAugmentOwnershipAssertion(db:D1Database,source:Awaited<ReturnType<typeof readOrdinaryAugmentOwnership>>){
 const originalHash=source.o22Original?await sha256Hex(canonicalJson(source.o22Original)):null;
 return db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN
  (SELECT count(*) FROM solo_augment_grants WHERE run_id=?)=?
  AND NOT EXISTS(SELECT 1 FROM json_each(?) s LEFT JOIN solo_augment_grants g ON g.run_id=? AND g.event_id=json_extract(s.value,'$.eventId')
   WHERE g.event_id IS NULL OR g.account_id<>? OR g.write_revision<>json_extract(s.value,'$.revision'))
  AND (SELECT count(*) FROM run_augment_effect_originals WHERE run_id=?)=?
  AND (? IS NULL OR EXISTS(SELECT 1 FROM run_augment_effect_originals WHERE run_id=? AND account_id=? AND effect_id='O22' AND original_hash=?))
  AND NOT EXISTS(SELECT 1 FROM run_augment_history WHERE run_id=?) THEN 1 ELSE 0 END`)
  .bind(source.runId,source.selected.length,canonicalJson(source.selected),source.runId,source.accountId,
   source.runId,source.o22Original?1:0,originalHash,source.runId,source.accountId,originalHash,source.runId);
}
