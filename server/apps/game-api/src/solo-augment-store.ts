import {prepareAugmentPurchase} from "./augment-purchase-approval";
import {AugmentLifestealOriginalStore,o22ApplicationOriginalSchema,readO22ApplicationOriginal} from './augment-lifesteal-original';
import {augmentUtilityDefinitionVersion} from './augment-utility-definition';
import type {ExtraApprovalPort} from "./solo-augment-selection";
import {CombatHostReportStore} from "./combat-host-report-store";
import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {readCombatAuthority} from "./combat-authority";
import {SoloAugmentSelection,soloSelectionIssueSchema,soloSelectionResultSchema,soloSelectSchema,soloRerollSchema} from "./solo-augment-selection";
const id=z.string().min(1).max(200),actorSchema=z.strictObject({accountId:id,environment:id});
const action=z.discriminatedUnion("operation",[z.strictObject({operation:z.literal("SELECT"),command:soloSelectSchema.strict()}),z.strictObject({operation:z.literal("REROLL"),command:soloRerollSchema,approval:z.literal("PURCHASE_ENTITLEMENT").optional()})]);
const storedResultSchema=soloSelectionResultSchema.extend({applicationOriginal:o22ApplicationOriginalSchema.optional()});
type Row={account_id:string;environment:string;issue_hash:string;start_snapshot_json:string;initial_json:string;checkpoint_json:string;write_revision:number};
const fail=(code:string):never=>{throw new ActivityError(409,`augment_store_${code}`);};
export interface AugmentSelectionGuard {
 prepare(context:{accountId:string;runId:string;stageId:string;startSnapshot:string;environment:string;candidateId:string;catalogVersion:string}):Promise<D1PreparedStatement[]>;
}
export interface AugmentIssuanceGuard {
 prepare(issue:z.infer<typeof soloSelectionIssueSchema>,startSnapshot:string):Promise<D1PreparedStatement[]>;
}
/** Internal run-grant persistence; legacy table names are retained for compatibility. Issuer must supply the pinned eligible catalog and real stage.
 * No public grant issuance, ad SSV, effect execution or payout is supplied. */
export class RunAugmentStore {
 constructor(private readonly db:D1Database,private readonly drawIndex?:(maximum:number)=>number,private readonly mode:"ORDINARY_SOLO"|"ORDINARY_COOP"="ORDINARY_SOLO",private readonly selectionGuard?:AugmentSelectionGuard,private readonly issuanceGuard?:AugmentIssuanceGuard){}
 private dependencies(extraApproval?:ExtraApprovalPort,approvalAccountId?:string){return {...(approvalAccountId?{approvalAccountId}:{}),...(this.drawIndex?{drawIndex:this.drawIndex}:{}),extraApproval:extraApproval??{authorize:async()=>fail("extra_approval_unavailable")}};}
 private row(runId:string,eventId:string){id.parse(runId);id.parse(eventId);return this.db.prepare("SELECT account_id,environment,issue_hash,start_snapshot_json,initial_json,checkpoint_json,write_revision FROM solo_augment_grants WHERE run_id=? AND event_id=?").bind(runId,eventId).first<Row>();}
 private async access(actorInput:unknown,runId:string,eventId:string){const actor=actorSchema.parse(actorInput),row=await this.row(runId,eventId);if(!row)return fail("grant_missing");if((this.mode==="ORDINARY_SOLO"&&row.account_id!==actor.accountId)||row.environment!==actor.environment)throw new ActivityError(403,"augment_store_access_forbidden");this.checkMode(row.start_snapshot_json);if(this.mode==="ORDINARY_COOP")await new CombatHostReportStore(this.db).read(actor.accountId,runId);return {actor,row};}
 private checkMode(snapshot:string){const pinned=z.object({admission:z.object({combatAuthority:z.unknown()})}).parse(JSON.parse(snapshot));if(readCombatAuthority(pinned.admission.combatAuthority).contract?.mode!==this.mode)return fail("mode_unsupported");}
 private async control(sender:string,runId:string,owner:string){return this.mode==="ORDINARY_COOP"?new CombatHostReportStore(this.db).prepareControl(sender,runId,{kind:"SHARED_GRANT",ownerAccountId:owner}):null;}
 private runGuard(runId:string,owner:string,snapshot:string){return this.db.prepare("INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND (?=1 OR r.account_id=?) AND r.status='ACTIVE' AND s.snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM combat_result_reviews WHERE run_id=?) THEN 1 ELSE 0 END").bind(runId,this.mode==="ORDINARY_COOP"?1:0,owner,snapshot,runId);}
 async issue(input:unknown){
  const c=soloSelectionIssueSchema.parse(input),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.row(c.runId,c.eventId);if(!r)return null;if(r.issue_hash!==hash)return fail("issue_conflict");this.checkMode(r.start_snapshot_json);return SoloAugmentSelection.restore(r.initial_json,this.dependencies()).read();};
  const old=await replay();if(old)return old;
  const run=await this.db.prepare("SELECT r.account_id,r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=?").bind(c.runId).first<{account_id:string;status:string;snapshot_json:string}>();
  if(!run||(this.mode==="ORDINARY_SOLO"&&run.account_id!==c.accountId))throw new ActivityError(403,"augment_store_access_forbidden");
  if(run.status!=="ACTIVE")return fail("run_not_active");
  this.checkMode(run.snapshot_json);
  const control=await this.control(c.accountId,c.runId,c.selectionOwner);
  const model=SoloAugmentSelection.issue(c,this.dependencies()),checkpoint=model.checkpoint();
  try{const assertions=this.issuanceGuard?await this.issuanceGuard.prepare(c,run.snapshot_json):[];
   if(this.issuanceGuard&&!assertions.length)return fail('issuance_verification_required');
   await this.db.batch([this.runGuard(c.runId,c.accountId,run.snapshot_json),...(control?[control.assertion]:[]),...assertions,
   this.db.prepare("INSERT INTO solo_augment_grants VALUES(?,?,?,?,?,?,?,?,?,1)").bind(c.runId,c.eventId,c.stageId,c.accountId,c.environment,hash,run.snapshot_json,checkpoint,checkpoint),
   this.db.prepare("DELETE FROM solo_augment_write_guard"),...(control?[control.cleanup]:[])]);
  }catch(error){const completed=await replay();if(completed)return completed;if(/valid=1|UNIQUE constraint failed/u.test(String(error)))return fail("state_conflict");throw error;}
  return model.read();
 }
 async read(actor:unknown,runId:string,eventId:string){const {row}=await this.access(actor,runId,eventId);return SoloAugmentSelection.restore(row.checkpoint_json,this.dependencies()).read();}
 async execute(actorInput:unknown,runId:string,eventId:string,requestId:string,input:unknown){
  id.parse(requestId);const c=action.parse(input),{actor,row}=await this.access(actorInput,runId,eventId);
  if(c.command.eventId!==eventId)return fail("event_mismatch");
  const hash=await sha256Hex(c.operation==="REROLL"&&c.approval?canonicalJson({actor,action:{operation:c.operation,command:c.command,approval:c.approval}}):canonicalJson({actor,action:{operation:c.operation,command:c.command}}));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM solo_augment_receipts WHERE run_id=? AND event_id=? AND request_id=?").bind(runId,eventId,requestId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return fail("request_conflict");
   const saved=storedResultSchema.parse(JSON.parse(r.response_json));
   if(saved.applicationOriginal)await readO22ApplicationOriginal(saved.applicationOriginal,{accountId:row.account_id,runId,eventId,environment:actor.environment},saved.snapshot);
   return saved;};
  const old=await replay();if(old)return old;
  if(await this.db.prepare("SELECT 1 FROM augment_ad_sessions WHERE run_id=? AND event_id=? AND status='AD_PENDING'").bind(runId,eventId).first())throw new ActivityError(409,"augment_ad_pending");
  if(row.write_revision>=Number.MAX_SAFE_INTEGER)return fail("revision_exhausted");
  // Shared input authority never transfers ownership of the original grant or purchase entitlement.
  if(this.mode==="ORDINARY_COOP"&&c.operation==="REROLL"&&c.command.mode==="EXTRA"&&!c.approval)return fail("extra_approval_unavailable");
  const control=await this.control(actor.accountId,runId,row.account_id);
  // Shared input delegation does not change the grant's original owner or economic identity.
  const modelActor={...actor,accountId:row.account_id};
  const extraAssertions:D1PreparedStatement[]=[];
  if(c.operation==="REROLL"&&c.approval&&c.command.mode!=="EXTRA")return fail("approval_not_applicable");
  const extraApproval:ExtraApprovalPort|undefined=c.operation==="REROLL"&&c.approval==="PURCHASE_ENTITLEMENT"?{authorize:async target=>{
   const prepared=await prepareAugmentPurchase(this.db,actor,target);extraAssertions.push(...prepared.assertions);return prepared.approval;
  }}:undefined;
  const model=SoloAugmentSelection.restore(row.checkpoint_json,this.dependencies(extraApproval,extraApproval?actor.accountId:undefined));
  const result=c.operation==="SELECT"?model.select(modelActor,requestId,c.command):await model.reroll(modelActor,requestId,c.command);
  try{
  // Saved receipt replay above preserves its original definition. A fresh O22
  // confirmation must include a producible original, never silently omit it.
  // Remove this restriction when a verified pinned legacy definition producer exists.
  if(this.mode==='ORDINARY_SOLO'&&result.outcome==='SELECTED'&&result.snapshot.selectedCandidateId==='O22'&&result.snapshot.catalogVersion!==augmentUtilityDefinitionVersion)
   return fail('application_definition_unsupported');
  const candidateId=c.operation==='SELECT'?c.command.candidateId:result.snapshot.slots[c.command.slotIndex]!.candidateId;
  const selectionAssertions=this.selectionGuard?await this.selectionGuard.prepare({accountId:row.account_id,runId,stageId:result.snapshot.stageId,startSnapshot:row.start_snapshot_json,environment:actor.environment,candidateId,catalogVersion:result.snapshot.catalogVersion}):[];
  if(this.selectionGuard&&!selectionAssertions.length)return fail('selection_verification_required');
  const application=this.mode==='ORDINARY_SOLO'&&result.outcome==='SELECTED'&&result.snapshot.selectedCandidateId==='O22'&&result.snapshot.catalogVersion===augmentUtilityDefinitionVersion
   ?await new AugmentLifestealOriginalStore(this.db).prepareSelected(row.account_id,{runId,eventId,environment:actor.environment,expectedStoreRevision:row.write_revision,
    expectedCatalogVersion:result.snapshot.catalogVersion,expectedStartSnapshotHash:await sha256Hex(row.start_snapshot_json)},model.checkpoint()):null;
  const response=application?{...result,applicationOriginal:application.original}:result;
  await this.db.batch([this.runGuard(runId,row.account_id,row.start_snapshot_json),...(control?[control.assertion]:[]),
   this.db.prepare("INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM solo_augment_grants WHERE run_id=? AND event_id=? AND checkpoint_json=? AND write_revision=?) THEN 1 ELSE 0 END").bind(runId,eventId,row.checkpoint_json,row.write_revision),
   ...extraAssertions,
   ...selectionAssertions,
   ...(application?.statements??[]),
   this.db.prepare("UPDATE solo_augment_grants SET checkpoint_json=?,write_revision=write_revision+1 WHERE run_id=? AND event_id=?").bind(model.checkpoint(),runId,eventId),
   this.db.prepare("INSERT INTO solo_augment_receipts VALUES(?,?,?,?,?)").bind(runId,eventId,requestId,hash,canonicalJson(response)),
   this.db.prepare("DELETE FROM solo_augment_write_guard"),...(control?[control.cleanup]:[])]);
  return response;
  }catch(error){const completed=await replay();if(completed)return completed;
   // Migration 0068 already atomically blocks an ad that starts after our
   // preflight read. Preserve its rollback and expose a recoverable conflict.
   if(/augment_ad_pending/u.test(String(error)))throw new ActivityError(409,'augment_ad_pending');
   if(/valid=1|UNIQUE constraint failed/u.test(String(error)))return fail("state_conflict");throw error;}
 }
}

// Existing solo callers keep the same default mode and API.
export {RunAugmentStore as SoloAugmentStore};
