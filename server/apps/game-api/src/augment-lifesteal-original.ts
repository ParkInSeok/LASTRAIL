import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {captureUtilitySelectionDefinition,bindUtilitySelectionDefinition,readPinnedUtilitySelectionDefinition,type UtilitySelectionDefinition} from './augment-utility-source';
import {captureAugmentSelectionSource} from './combat-augment-selection-source';
import {SoloAugmentSelection} from './solo-augment-selection';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const request=z.strictObject({runId:id,eventId:id,environment:id,expectedStoreRevision:z.number().int().positive().safe(),expectedCatalogVersion:id,expectedStartSnapshotHash:hash});
const envelope=z.strictObject({version:z.literal('combat.o22-application-original.v1.20261019'),applicationId:id,
 accountId:id,runId:id,grantEventId:id,environment:id,effectId:z.literal('O22'),
 status:z.literal('SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED'),combatOwner:z.literal('UNITY_LOCAL'),applied:z.literal(false),definition:z.unknown()});
type Original=Omit<z.infer<typeof envelope>,'definition'>&{definition:UtilitySelectionDefinition};
export {envelope as o22ApplicationOriginalSchema};
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const fail=(code:string):never=>{throw new ActivityError(409,'o22_original_'+code);};
/** Recovery validation using only the saved original; no current catalog/HP read. */
export async function readO22ApplicationOriginal(input:unknown,trust:{accountId:string;runId:string;eventId:string;environment:string},selection?:unknown):Promise<Original>{
 try{
  const saved=envelope.parse(input),original=saved.definition as UtilitySelectionDefinition;
  if(saved.accountId!==trust.accountId||saved.runId!==trust.runId||saved.grantEventId!==trust.eventId||saved.environment!==trust.environment)throw Error('scope');
  const definition=await readPinnedUtilitySelectionDefinition(original,original,original.packetHash,trust);
  if(definition.selectedDefinition?.id!=='O22'||definition.selectionSource.selection.state!=='CONFIRMED'
   ||selection!==undefined&&json(selection)!==json(definition.selectionSource.selection))throw Error('selection');
  const requestHash=await sha256Hex(json({...trust,expectedStoreRevision:definition.selectionSource.storeRevision,
   expectedCatalogVersion:definition.definitionCatalog.version,expectedStartSnapshotHash:definition.startSnapshotHash}));
  if(saved.applicationId!=='o22:'+requestHash)throw Error('application');
  return {...saved,definition};
 }catch{throw new ActivityError(503,'o22_original_storage_invalid');}
}
/** Server-owned acquisition original from the real confirmed grant. This is NOT
 * a catalog issuer, runtime installation acknowledgement or damage authority.
 * Unity's original action owner must install/pin it before emitting APPLIED and
 * bind node, actor generation, action sequence and healing proof to its journal.
 * Internal composition only; never expose this as an arbitrary public grant API. */
export class AugmentLifestealOriginalStore {
 constructor(private readonly db:D1Database){}
 /** Called by the verified selection owner BEFORE its CAS commit. Caller must
  * keep all original selection assertions, checkpoint and receipt statements. */
 async prepareSelected(accountId:string,input:unknown,checkpoint:string){
  const c=request.parse(input),before=await captureAugmentSelectionSource(this.db,accountId,c);
  const selected=SoloAugmentSelection.restore(checkpoint,{extraApproval:{authorize:async()=>fail('approval_unavailable')}}).read();
  if(before.selection.state!=='SELECTING'||selected.state!=='CONFIRMED'||selected.selectedCandidateId!=='O22'
   ||selected.runId!==before.runId||selected.eventId!==before.eventId||selected.stageId!==before.stageId||selected.catalogVersion!==before.catalogVersion
   ||selected.grantRevision!==before.selection.grantRevision+1||json(selected.slots)!==json(before.selection.slots))return fail('candidate_conflict');
  const {packetHash,...body}=before;void packetHash;
  const next={...body,selection:selected,storeRevision:c.expectedStoreRevision+1,checkpointHash:await sha256Hex(checkpoint)};
  const definition=await bindUtilitySelectionDefinition({...next,packetHash:await sha256Hex(json(next))});
  const requestHash=await sha256Hex(json({accountId,...c,expectedStoreRevision:c.expectedStoreRevision+1}));
  const original:Original={version:'combat.o22-application-original.v1.20261019',applicationId:'o22:'+requestHash,accountId,runId:c.runId,grantEventId:c.eventId,environment:c.environment,effectId:'O22',status:'SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED',combatOwner:'UNITY_LOCAL',applied:false,definition};
  const encoded=json(original);
  return {original,statements:[
   this.db.prepare("INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE') THEN 1 ELSE 0 END").bind(accountId),
   this.db.prepare('INSERT INTO run_augment_effect_originals VALUES(?,?,?,?,?,?,?)').bind(c.runId,'O22',c.eventId,accountId,requestHash,await sha256Hex(encoded),encoded)]};
 }
 async capture(accountId:string,input:unknown):Promise<Original>{
  id.parse(accountId);const c=request.parse(input),requestHash=await sha256Hex(json({accountId,...c}));
  if(!await this.db.prepare('SELECT 1 FROM runs WHERE id=? AND account_id=?').bind(c.runId,accountId).first())return fail('owner');
  const replay=async():Promise<Original|null>=>{
   const row=await this.db.prepare("SELECT account_id,grant_event_id,request_hash,original_hash,original_json FROM run_augment_effect_originals WHERE run_id=? AND effect_id='O22'").bind(c.runId)
    .first<{account_id:string;grant_event_id:string;request_hash:string;original_hash:string;original_json:string}>();
   if(!row)return null;
   if(row.account_id!==accountId||row.grant_event_id!==c.eventId||row.request_hash!==requestHash)return fail('already_bound');
   try{
    if(await sha256Hex(row.original_json)!==row.original_hash)throw Error('hash');
    const saved=await readO22ApplicationOriginal(JSON.parse(row.original_json),{accountId,runId:c.runId,eventId:c.eventId,environment:c.environment});
    const definition=saved.definition;
    if(definition.selectedDefinition?.id!=='O22'||definition.selectionSource.selection.state!=='CONFIRMED'
     ||definition.startSnapshotHash!==c.expectedStartSnapshotHash||definition.definitionCatalog.version!==c.expectedCatalogVersion
     ||definition.selectionSource.storeRevision!==c.expectedStoreRevision)throw Error('source');
    return {...saved,definition};
   }catch{throw new ActivityError(503,'o22_original_storage_invalid');}
  };
  const old=await replay();if(old)return old;
  try{
   const definition=await captureUtilitySelectionDefinition(this.db,accountId,c);
   if(definition.selectedDefinition?.id!=='O22'||definition.selectionSource.selection.state!=='CONFIRMED')return fail('not_confirmed');
   const row=await this.db.prepare('SELECT checkpoint_json,start_snapshot_json FROM solo_augment_grants WHERE run_id=? AND event_id=? AND write_revision=?').bind(c.runId,c.eventId,c.expectedStoreRevision).first<{checkpoint_json:string;start_snapshot_json:string}>();
   if(!row||await sha256Hex(row.checkpoint_json)!==definition.selectionSource.checkpointHash||await sha256Hex(row.start_snapshot_json)!==c.expectedStartSnapshotHash)return fail('source_changed');
   const result:Original={version:'combat.o22-application-original.v1.20261019',applicationId:'o22:'+requestHash,accountId,runId:c.runId,grantEventId:c.eventId,environment:c.environment,effectId:'O22',status:'SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED',combatOwner:'UNITY_LOCAL',applied:false,definition};
   const encoded=json(result);
   await this.db.batch([
    this.db.prepare(`INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(
     SELECT 1 FROM solo_augment_grants g JOIN runs r ON r.id=g.run_id JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id
     WHERE g.run_id=? AND g.event_id=? AND g.account_id=? AND r.account_id=? AND g.environment=? AND g.write_revision=?
     AND g.checkpoint_json=? AND g.start_snapshot_json=? AND s.snapshot_json=g.start_snapshot_json AND r.status='ACTIVE' AND a.status='ACTIVE')
     AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) THEN 1 ELSE 0 END`)
     .bind(c.runId,c.eventId,accountId,accountId,c.environment,c.expectedStoreRevision,row.checkpoint_json,row.start_snapshot_json,c.runId),
    this.db.prepare('INSERT INTO run_augment_effect_originals VALUES(?,?,?,?,?,?,?)').bind(c.runId,'O22',c.eventId,accountId,requestHash,await sha256Hex(encoded),encoded),
    this.db.prepare('DELETE FROM solo_augment_write_guard')]);
   return result;
  }catch(error){const committed=await replay();if(committed)return committed;throw error;}
 }
}
