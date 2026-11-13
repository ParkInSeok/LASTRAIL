import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {RunAugmentStore} from './solo-augment-store';
import {readOriginalAugmentSelection} from './combat-augment-selection-source';
import {prepareAugmentNodeIssuance} from './augment-node-selection-guard';
import {augmentUtilityDefinitionVersion,captureAugmentUtilityDefinitions} from './augment-utility-definition';
import {OperationMapStore} from './operation-map-store';
import {mapReceipt} from './operation-map-state';
import {readCombatAuthority} from './combat-authority';
const id=z.string().min(1).max(200);
const contextSchema=z.strictObject({accountId:id,environment:id,runId:id,stageId:id});
type Context=z.infer<typeof contextSchema>;
const catalogSchema=z.strictObject({catalogVersion:z.literal(augmentUtilityDefinitionVersion),eligibleIds:z.array(id).max(1000)})
 .refine(c=>new Set(c.eligibleIds).size===c.eligibleIds.length);
/** Server-composed structural support/formation provider, never client flags.
 * Proof must fence its original version, mode, capabilities and formation in the
 * issuing DB transaction. Empty is valid only after complete source evaluation.
 * No operating provider is installed by this orchestration class. */
export interface AugmentEligibleCatalogSource {
 prepare(context:Context&{startSnapshot:string}):Promise<{catalogVersion:string;eligibleIds:string[];assertions:D1PreparedStatement[]}>;
}
const noAwardPrefix='augment-no-award:';
function noAward(receipt:z.infer<typeof mapReceipt>){
 if(!receipt.verificationId?.startsWith(noAwardPrefix))throw new ActivityError(409,'augment_no_award_receipt_conflict');
 return {outcome:'NO_AWARD' as const,catalogVersion:receipt.verificationId.slice(noAwardPrefix.length),mapReceipt:receipt};
}
/** Ordinary solo only; the original grant owns selection, rerolls and receipts.
 * Verified zero candidates complete the original map node in one transaction. */
export class AugmentNodeIssuer {
 constructor(private readonly db:D1Database,private readonly catalog?:AugmentEligibleCatalogSource){}
 async issue(input:Context){
  const c=contextSchema.parse(input);
  if(!await this.db.prepare("SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE'").bind(c.accountId).first())throw new ActivityError(403,'augment_issuer_access_forbidden');
  const eventId='node:'+await sha256Hex(canonicalJson(c));
  const replay=async()=>{
   const completed=await new OperationMapStore(this.db).lookup(c.accountId,c.runId,eventId);
   if(completed)return noAward(completed);
   const row=await this.db.prepare('SELECT event_id,write_revision,start_snapshot_json FROM solo_augment_grants WHERE run_id=? AND stage_id=?')
    .bind(c.runId,c.stageId).first<{event_id:string;write_revision:number;start_snapshot_json:string}>();
   if(!row)return null;
   const selection=await new RunAugmentStore(this.db).read({accountId:c.accountId,environment:c.environment},c.runId,row.event_id);
   return readOriginalAugmentSelection(this.db,c.accountId,{runId:c.runId,eventId:row.event_id,environment:c.environment,expectedStoreRevision:row.write_revision,
    expectedCatalogVersion:selection.catalogVersion,expectedStartSnapshotHash:await sha256Hex(row.start_snapshot_json)});
  };
  const old=await replay();if(old)return old;
  try{
   const run=await this.db.prepare('SELECT s.snapshot_json FROM run_snapshots s JOIN runs r ON r.id=s.run_id WHERE r.id=? AND r.account_id=?')
    .bind(c.runId,c.accountId).first<{snapshot_json:string}>();
   if(!run)throw new ActivityError(403,'augment_issuer_owner');
   const snapshot=z.object({admission:z.object({combatAuthority:z.unknown()})}).parse(JSON.parse(run.snapshot_json));
   if(readCombatAuthority(snapshot.admission.combatAuthority).contract?.mode!=='ORDINARY_SOLO')throw new ActivityError(409,'augment_issuer_mode_unsupported');
   const node=await prepareAugmentNodeIssuance(this.db,{...c,startSnapshot:run.snapshot_json});
   if(!this.catalog)throw new ActivityError(503,'augment_catalog_source_required');
   const prepared=await this.catalog.prepare({...c,startSnapshot:run.snapshot_json});
   if(!prepared.assertions?.length)throw new ActivityError(503,'augment_catalog_proof_required');
   const catalog=catalogSchema.parse({catalogVersion:prepared.catalogVersion,eligibleIds:prepared.eligibleIds});
   const policy=(await captureAugmentUtilityDefinitions()).eligibility,owned=new Set(node.owned.map(x=>x.candidateId));
   const eligibleIds=catalog.eligibleIds.filter(candidate=>!(policy.singleOwnedPerRun.includes(candidate)&&owned.has(candidate))
    &&!(policy.exclusivePair.includes(candidate)&&policy.exclusivePair.some(other=>other!==candidate&&owned.has(other))));
   if(!eligibleIds.length){
    const verificationId=noAwardPrefix+catalog.catalogVersion;
    const command={eventId,expectedRevision:node.revision,kind:'COMPLETE' as const,nodeId:c.stageId,evidenceRef:verificationId};
    const receipt=await new OperationMapStore(this.db,{prepare:async actual=>{
     if(actual.accountId!==c.accountId||actual.runId!==c.runId||actual.startSnapshot!==run.snapshot_json||canonicalJson(z.json().parse(actual.command))!==canonicalJson(command))throw new ActivityError(409,'augment_issuer_context_changed');
     return {proof:{kind:'COMPLETE',verificationId,holdIds:[]},assertions:[...node.assertions,...prepared.assertions,this.db.prepare('DELETE FROM solo_augment_write_guard')]};
    }}).commit(c.accountId,c.runId,command);
    return noAward(receipt);
   }
   const issue={...c,eventId,selectionOwner:c.accountId,catalogVersion:catalog.catalogVersion,eligibleIds};
   return await new RunAugmentStore(this.db,undefined,'ORDINARY_SOLO',undefined,{prepare:async(actual,start)=>{
    if(start!==run.snapshot_json||canonicalJson(actual)!==canonicalJson(issue))throw new ActivityError(409,'augment_issuer_context_changed');
    return [...node.assertions,...prepared.assertions];
   }}).issue(issue);
  }catch(error){const winner=await replay();if(winner)return winner;throw error;}
 }
}
