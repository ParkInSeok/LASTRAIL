import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from '../../game-api/src/activity-policy';
export const adminRewardTrackingVersion='admin.reward-review-preview.v2.20261020';
export const rewardDraftContractVersion='admin.reward-draft.v1.20261020';
export const rewardManagementWritePermission='REWARD_MANAGEMENT_WRITE';
const id=z.string().min(1).max(128),positive=z.number().int().positive().safe();
export const rewardTrackingProposalSchema=z.strictObject({
 environment:z.enum(['DEV','PRD']),batchId:id,revision:positive,title:z.string().trim().min(1).max(200),
 type:z.enum(['REPORTER_THANKS','GLOBAL_COMPENSATION','DAMAGE_RESTORATION','EVENT']),incidentRef:id,
 policyVersion:id.nullable(),targetAsOf:z.number().int().nonnegative().safe().nullable(),
 targetCondition:z.string().trim().min(1).max(1000).nullable(),
 accountIds:z.array(id).min(1).max(100),
 rewardLines:z.array(z.strictObject({itemId:id,quantity:positive})).min(1).max(20)
  .refine(rows=>new Set(rows.map(r=>r.itemId)).size===rows.length)
});
const missingSources=['ORIGINAL_OPERATIONAL_REWARD_POLICY_AND_TARGET_SNAPSHOT','ORIGINAL_OPERATIONAL_REWARD_ENTITLEMENT_ISSUER',
 'ORIGINAL_REVIEW_EXECUTE_STOP_DELTA_IMPLEMENTATION','ORIGINAL_BATCH_JOB_AND_TRANSACTION_RECOVERY_OWNER'] as const;
export function rewardTrackingCapabilities(environment:string){
 return {contractVersion:adminRewardTrackingVersion,environment,permissions:{read:'SUPPORT_READ',saveDraft:rewardManagementWritePermission,
  review:rewardManagementWritePermission,execute:rewardManagementWritePermission,stop:rewardManagementWritePermission,delta:rewardManagementWritePermission},
  writeRoles:['OWNER','OPERATOR'],supportedWriteCommands:['SAVE_DRAFT'],
  draftWriteSupported:true,draftWriteRoleMapping:'OWNER_OPERATOR',draftContractVersion:rewardDraftContractVersion,
  draftCommandRoute:'/v1/admin/reward-tracking/drafts/:batchId/commands',draftReadRoute:'/v1/admin/reward-tracking/drafts/:batchId',
  previewSupported:true,executionSupported:false,reviewApprovalSupported:false,scheduledExecutionSupported:false,
  originalAccountReceiptRoute:'/v1/admin/support/accounts/:accountId',
  missingSources:[...missingSources],maximumExplicitPreviewTargets:100,
  existingSources:{rewardTransactions:'ORIGINAL_GAME_REWARD_RECEIPTS_NOT_OPERATIONAL_BATCHES',
   launchEntitlements:'FINITE_LOGIN_FIRST_CLEAR_SOURCES_NOT_COMPENSATION_ENTITLEMENTS',
   supportGrants:'SUPPORT_TICKET_PROCESS_PERMISSION_NOT_REWARD_GRANTS'},
  grantAndClaimAxes:'KEEP_ORIGINAL_SOURCE_SEMANTICS_NO_OPERATIONAL_STATUS_INFERENCE'};
}
/** Read-only proposal preparation. SUPPORT_READ permits inspecting existing
 * account facts, not reviewing/executing payouts. The separately authorized draft
 * writer does not supply a compensation issuer; never substitute support processor grants,
 * raw wallet mutation, launch entitlements, or an unrelated recurring mail owner.
 * Replace missing-source flags only when their actual authority is installed.
 */
export class AdminRewardTracking{
 constructor(private readonly db:D1Database,private readonly environment:string){
  if(environment!=='DEV'&&environment!=='PRD')throw new ActivityError(503,'reward_tracking_environment_unavailable');
 }
 async capabilities(principalId:string){
  await this.readAccounts(principalId,[]);
  return rewardTrackingCapabilities(this.environment);
 }
 private async readAccounts(principalId:string,accountIds:string[]){
  id.parse(principalId);
  const placeholders=accountIds.map(()=>'?').join(',');
  // Current principal permission and account observations share one SQL snapshot.
  // No display names, email addresses, support bodies or full account dumps.
  const row=await this.db.prepare(`SELECT
   (SELECT json_group_array(json_object('accountId',id,'status',status,'updatedAt',updated_at))
    FROM (SELECT id,status,updated_at FROM accounts WHERE ${accountIds.length?`id IN (${placeholders})`:'0'} ORDER BY id)) AS accounts_json
   FROM admin_principals WHERE id=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR','VIEWER')`)
   .bind(...accountIds,principalId).first<{accounts_json:string}>();
  if(!row)throw new ActivityError(403,'ADMIN_PERMISSION_DENIED');
  return z.array(z.object({accountId:id,status:z.string(),updatedAt:z.number().int().safe()})).parse(JSON.parse(row.accounts_json));
 }
 async preview(principalId:string,input:unknown){
  const parsed=rewardTrackingProposalSchema.safeParse(input);
  if(!parsed.success)throw new ActivityError(400,'contract_violation');
  const proposal=parsed.data;
  if(proposal.environment!==this.environment)throw new ActivityError(409,'reward_tracking_environment_conflict');
  const targets=[...new Set(proposal.accountIds)].sort(),accounts=await this.readAccounts(principalId,targets);
  const found=new Set(accounts.map(a=>a.accountId));
  const observations=targets.map(accountId=>({accountId,account:accounts.find(a=>a.accountId===accountId)??null,
   eligibility:'UNRESOLVED' as const,priorEntitlement:'NOT_OBSERVED' as const,grantStatus:'NOT_OBSERVED' as const,claimStatus:'NOT_OBSERVED' as const}));
  const reviewBody={contractVersion:adminRewardTrackingVersion,proposal,observations};
  const unavailable=[...missingSources,...proposal.policyVersion===null?['POLICY_VERSION_UNCONFIRMED']:[],
   ...proposal.targetAsOf===null?['TARGET_AS_OF_UNCONFIRMED']:[],...proposal.targetCondition===null?['TARGET_CONDITION_UNCONFIRMED']:[]];
  return {contractVersion:adminRewardTrackingVersion,environment:this.environment,batchId:proposal.batchId,revision:proposal.revision,
   reviewHash:await sha256Hex(canonicalJson(reviewBody)),proposal,observations,observedAt:Date.now(),
   reviewApproved:false,executable:false,targetSnapshotStatus:'PROPOSED_ACCOUNTS_ONLY_NOT_CONFIRMED_MEMBERSHIP',
   targets:{requestedCount:proposal.accountIds.length,uniqueProposedCount:targets.length,duplicateInputCount:proposal.accountIds.length-targets.length,
    observedAccountCount:accounts.length,missingAccountCount:targets.filter(id=>!found.has(id)).length,
    confirmedEligibleCount:null,unresolvedEligibilityCount:targets.length,duplicateEntitlementCount:null,policyExcludedCount:null},
   proposedTotals:proposal.rewardLines.map(line=>({itemId:line.itemId,quantity:(BigInt(line.quantity)*BigInt(targets.length)).toString()})),
   totalScope:'UNIQUE_PROPOSED_ACCOUNTS_NOT_CONFIRMED_PAYOUT',executionState:'NOT_CREATED',unavailable,
   grantsOnRead:false,writesOnPreview:false,
   originalAccountReceiptRoute:'/v1/admin/support/accounts/:accountId'};
 }
}
