import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {eventCosmeticKind,eventCosmeticReplacementVersion,eventCosmeticReplacementBundle} from './event-cosmetic-replacement';
const id=z.string().trim().min(1).max(128);
const observationSchema=z.strictObject({
 environment:z.enum(['DEV','PRD']),roundId:id,previousRoundId:id,rewardRowId:id,accountId:id,cosmeticId:id,
 definitionVersion:id,definitionHash:z.string().regex(/^[a-f0-9]{64}$/),replacementTableVersion:id.nullable(),
 cosmeticKind:eventCosmeticKind.optional(),
 ownership:z.strictObject({accountId:id,cosmeticId:id,state:z.enum(['OWNED','UNOWNED','UNKNOWN','RESULT_UNKNOWN'])}),
 originalRequestState:z.enum(['NOT_OBSERVED','RESULT_UNKNOWN'])
});
export type EventRerunObservation=z.infer<typeof observationSchema>;
function readObservation(input:unknown){
 const parsed=observationSchema.safeParse(input);
 if(!parsed.success)throw new ActivityError(400,'contract_violation');
 const value=parsed.data;
 if(value.roundId===value.previousRoundId)throw new ActivityError(409,'event_rerun_requires_new_round');
 if(value.ownership.accountId!==value.accountId||value.ownership.cosmeticId!==value.cosmeticId)
  throw new ActivityError(409,'event_rerun_ownership_scope_mismatch');
 return value;
}
/** Policy projection only. Supplied observations, definition hashes and table
 * versions are not authenticated producer evidence. This cannot authorize a grant.
 *
 * Existing recurring-grants.ts uses account_items quantities through
 * activity-grant-plan.ts for PASS cosmetics. That additive item path provides no
 * permanent cosmetic ownership semantics, event right or frozen duplicate branch.
 * Do not create a competing cosmetic ledger or use an item count as its substitute.
 * Replace the unsupported boundary only when the real cosmetic and event owners
 * can atomically serialize first-grant choice with all other acquisition paths.
 */
export async function inspectEventRerunOwnership(input:unknown){
 const value=readObservation(input);
 const recoveryRequired=value.originalRequestState==='RESULT_UNKNOWN'||value.ownership.state==='RESULT_UNKNOWN';
 const branchPreview=recoveryRequired?'RECOVER_ORIGINAL':value.ownership.state==='OWNED'?'REPLACEMENT':
  value.ownership.state==='UNOWNED'?'COSMETIC':'UNDETERMINED';
 const replacementPolicyReady=value.replacementTableVersion===eventCosmeticReplacementVersion&&value.cosmeticKind!==undefined;
 const replacementPreview=replacementPolicyReady?await eventCosmeticReplacementBundle(eventCosmeticReplacementVersion,value.cosmeticKind!):null;
 return {
  contractVersion:'event.rerun-ownership-policy.v1.20261020',
  environment:value.environment,roundId:value.roundId,previousRoundId:value.previousRoundId,
  accountId:value.accountId,rewardRowId:value.rewardRowId,cosmeticId:value.cosmeticId,
  definitionVersion:value.definitionVersion,definitionHash:value.definitionHash,
  proposedReplacementTableVersion:value.replacementTableVersion,
  // Definition/item changes must not mint a second identity for the same right.
  proposedRightIdentity:await sha256Hex(canonicalJson({environment:value.environment,roundId:value.roundId,
   rewardRowId:value.rewardRowId,accountId:value.accountId})),
  permanentCosmeticIdentity:await sha256Hex(canonicalJson({environment:value.environment,accountId:value.accountId,cosmeticId:value.cosmeticId})),
  branchPreview,sourceAuthorityVerified:false,executeSupported:false,applied:false,receiptWritten:false,
  replacementDefinitionAvailable:true,approvedReplacementTableVersion:eventCosmeticReplacementVersion,replacementPreview,
  replacementPolicyReady,publicationSupported:false,progressCopyAllowed:false,reviewCopyAllowed:false,
  accountFirstClearRegrantAllowed:false,newRoundContributionRequired:true,
  oldRoundRightsPreserved:true,oldGraceRunsRemainBoundToOriginalRound:true,
  originalReceiptStatus:'NOT_LOADED_FROM_AUTHORITY',originalOutcomeRecoveryRequired:recoveryRequired,
  missingSources:['ORIGINAL_PERMANENT_COSMETIC_OWNERSHIP_AND_CONCURRENT_ACQUISITION_OWNER',
   'ORIGINAL_EVENT_ACHIEVEMENT_RIGHT_AND_FIRST_GRANT_RECEIPT_OWNER',
   ...(!replacementPolicyReady?['ORIGINAL_REPLACEMENT_TABLE_VERSION_AND_COSMETIC_KIND_BINDING']:[]),
   'ORIGINAL_WHOLE_COSMETIC_CATALOG_AND_ALIAS_BINDING','ORIGINAL_EVENT_FREE_CRYSTAL_SOURCE_AND_ACTIVE_WALLET_POLICY',
   'ORIGINAL_REWARD_ISSUERS_AND_ATOMIC_ITEM_RESULT_RECOVERY'],
  requirements:['PERMANENT_SKIN_RIGHT_COUNTS_WITHOUT_OPERATOR_UNLOCK',
   'FIRST_GRANT_BRANCH_SERIALIZED_WITH_ALL_COSMETIC_ACQUISITIONS',
   'FREEZE_BRANCH_AND_TABLE_IN_ORIGINAL_RECEIPT_REPLAY_WITHOUT_RESELECTION',
   'MANUAL_AUTO_AND_ACK_RECOVERY_USE_SAME_ORIGINAL_RIGHT',
   'NO_COSMETIC_PLUS_REPLACEMENT_DOUBLE_GRANT',
   'PREVIOUS_ROUND_SETTLEMENT_AND_NEW_ROUND_RIGHTS_REMAIN_SEPARATE',
   'UNKNOWN_ORIGINAL_RESULT_RECOVERY_BEFORE_NEW_BRANCH']
 };
}
/** No production issuer or permanent ownership adapter is installed.
 * A caller-provided table version/ownership cache must never unlock this boundary.
 * This deliberately creates no prepared SQL, receipt or payout placeholder. */
export async function prepareEventRerunFirstGrant(input:unknown):Promise<never>{
 readObservation(input);
 throw new ActivityError(503,'event_rerun_original_grant_owner_unavailable');
}
