import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from '../../game-api/src/activity-policy';
export const eventDefinitionPreviewVersion='admin.event-definition-preview.v1.20261020';
const id=z.string().trim().min(1).max(128),positive=z.number().int().positive().safe();
const timestamp=z.number().int().nonnegative().max(8640000000000000);
export const eventDefinitionDraftSchema=z.strictObject({
 environment:z.enum(['DEV','PRD']),roundId:id,revision:positive,policyVersion:id.nullable(),
 title:z.string().trim().min(1).max(200),theme:z.string().trim().min(1).max(200),
 schedule:z.strictObject({publishAt:timestamp,startAt:timestamp,endAt:timestamp,claimUntil:timestamp.nullable(),giftOpenAt:timestamp.nullable()}),
 rewards:z.array(z.strictObject({rightId:id,kind:z.enum(['PARTICIPATION','STORY','CUMULATIVE','GIFT']),
  condition:z.string().trim().min(1).max(1000),producerId:id.nullable(),threshold:positive.nullable(),
  itemId:id.nullable(),quantity:positive,perAccountCount:positive,policyVersion:id.nullable()})).min(1).max(100)
});
export type EventDefinitionDraft=z.infer<typeof eventDefinitionDraftSchema>;
/** Proposal validation only. Caller input never proves catalogue, producer,
 * asset readiness, eligibility, review approval or command authority.
 * Replace these unavailable boundaries only with installed original owners.
 * No new round is persisted, and no clock-driven runtime state is inferred.
 */
export async function previewEventDefinition(environment:string,input:unknown){
 if(environment!=='DEV'&&environment!=='PRD')throw new ActivityError(503,'event_environment_unavailable');
 const parsed=eventDefinitionDraftSchema.safeParse(input);
 if(!parsed.success)throw new ActivityError(400,'contract_violation');
 const definition=parsed.data;
 if(definition.environment!==environment)throw new ActivityError(409,'event_environment_conflict');
 const {schedule,rewards}=definition,issues:string[]=[];
 const duration=schedule.endAt-schedule.startAt,day=86400000;
 if(schedule.publishAt>schedule.startAt||schedule.startAt>=schedule.endAt||
  (schedule.claimUntil!==null&&schedule.endAt>schedule.claimUntil))issues.push('SCHEDULE_ORDER_INVALID');
 if(duration<14*day||duration>28*day)issues.push('INITIAL_DURATION_OUTSIDE_14_TO_28_DAYS');
 const completionGraceEndsAt=schedule.endAt+day,manualClaimEndsAt=schedule.endAt+7*day;
 if(manualClaimEndsAt>8640000000000000)issues.push('DEADLINE_OUTSIDE_TIMESTAMP_RANGE');
 if(schedule.claimUntil!==null&&schedule.claimUntil!==manualClaimEndsAt)issues.push('MANUAL_CLAIM_DEADLINE_MUST_EQUAL_END_PLUS_7_DAYS');
 if(schedule.giftOpenAt!==null&&(schedule.giftOpenAt<schedule.startAt||schedule.giftOpenAt>=schedule.endAt))issues.push('GIFT_OUTSIDE_PARTICIPATION_PERIOD');
 if(new Set(rewards.map(r=>r.rightId)).size!==rewards.length)issues.push('DUPLICATE_RIGHT_ID');
 let priorThreshold=0;
 const totals=new Map<string,bigint>();
 for(const row of rewards){
  if(row.kind==='CUMULATIVE'){
   if(row.threshold===null||row.threshold<=priorThreshold)issues.push('CUMULATIVE_THRESHOLDS_NOT_INCREASING');
   if(row.threshold!==null)priorThreshold=row.threshold;
  }else if(row.threshold!==null)issues.push(`THRESHOLD_WITHOUT_CUMULATIVE_KIND:${row.rightId}`);
  if(row.itemId!==null)totals.set(row.itemId,(totals.get(row.itemId)??0n)+BigInt(row.quantity)*BigInt(row.perAccountCount));
 }
 const blockers=['ORIGINAL_EVENT_REVIEW_AND_PUBLICATION_OWNER_UNAVAILABLE',
  'ORIGINAL_EVENT_PROGRESS_AND_ENTITLEMENT_ISSUER_UNAVAILABLE','ORIGINAL_REWARD_CATALOGUE_ASSET_AND_CLIENT_SUPPORT_UNAVAILABLE',
  'ORIGINAL_AUTHORITY_COMPLETION_EVENT_AND_DEDUP_OWNER_UNAVAILABLE','ORIGINAL_EVENT_SETTLEMENT_OWNER_UNAVAILABLE',
  'CURRENCY_REWARD_POLICY_UNRESOLVED','COSMETIC_DUPLICATE_AND_UNOWNED_OPERATOR_POLICY_UNVERIFIED',
  ...definition.policyVersion===null?['EVENT_POLICY_VERSION_UNRESOLVED']:[],
  ...rewards.filter(r=>r.itemId===null||r.producerId===null||r.policyVersion===null).map(r=>`REWARD_SOURCE_OR_ITEM_UNRESOLVED:${r.rightId}`)];
 return {contractVersion:eventDefinitionPreviewVersion,environment,roundId:definition.roundId,revision:definition.revision,
  definition,definitionHash:await sha256Hex(canonicalJson({contractVersion:eventDefinitionPreviewVersion,definition})),
  definitionValid:issues.length===0,issues:[...new Set(issues)],blockers,
  writePermission:{permission:'EVENT_CONFIG_WRITE',roles:['OWNER','OPERATOR'],currentAuthorityVerified:false},
  reviewApproved:false,scheduleSupported:false,persisted:false,writesOnPreview:false,
  overlapStatus:'NOT_OBSERVED_WARNING_ONLY',runtimeState:'NOT_OBSERVED',displayTimezone:'Asia/Seoul',timestampUnit:'MILLISECONDS',
  timePolicy:{policyVersion:'event.close-claim-settlement.v1.20261020',participationEndsAt:schedule.endAt,
   completionGraceEndsAt,manualClaimEndsAt,participationEndExclusive:true,completionGraceEndExclusive:true,manualClaimEndExclusive:true,
   existingRunExpiryPreserved:true,authorityEventTimeRequired:true,lateOriginalReplayPreserved:true,
   originalStartEventBindingRequired:true,preEventStartNotAutomaticallyGraceEligible:true,
   manualAndAutomaticShareOriginalRight:true,automaticSettlementScope:'ACHIEVED_UNCLAIMED_RIGHTS_ONLY',
   failedOrUnknownRightsPreserved:true,settlementExecuted:false},
  proposedTotals:[...totals].sort(([a],[b])=>a.localeCompare(b)).map(([itemId,quantity])=>({itemId,quantity:quantity.toString()})),
  totalScope:'PER_ACCOUNT_PROPOSED_MAXIMUM_NOT_VALIDATED_ENTITLEMENT'};
}
