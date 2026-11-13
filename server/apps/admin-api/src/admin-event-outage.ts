import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from '../../game-api/src/activity-policy';
const day=86400000,maxTimestamp=8640000000000000;
const time=z.number().int().nonnegative().max(maxTimestamp),revision=z.number().int().positive().safe();
const id=z.string().trim().min(1).max(200);
const features=['PARTICIPATION','COMPLETION','CLAIM','AGGREGATION'] as const;
const proposalSchema=z.strictObject({
 environment:z.enum(['DEV','PRD']),roundId:id,revision,expectedRevision:revision,definitionHash:z.string().regex(/^[a-f0-9]{64}$/),
 targetScope:z.string().trim().min(1).max(1000),observedAt:time,claimOpensAt:time.nullable(),alreadyAppliedImpactIds:z.array(id).max(200),
 schedule:z.strictObject({startAt:time,participationEndsAt:time,completionGraceEndsAt:time,manualClaimEndsAt:time}),
 impacts:z.array(z.strictObject({impactId:id,incidentId:id,feature:z.enum(features),status:z.enum(['CONFIRMED','UNKNOWN','PLANNED']),
  startAt:time.nullable(),endAt:time.nullable(),evidenceRef:id.nullable()})).max(200)
});
export type EventOutageProposal=z.infer<typeof proposalSchema>;
type Interval={startAt:number;endAt:number};
function union(intervals:Interval[]):Interval[]{
 const merged:Interval[]=[];
 for(const interval of intervals.sort((a,b)=>a.startAt-b.startAt||a.endAt-b.endAt)){
  const last=merged.at(-1);
  if(last&&interval.startAt<=last.endAt)last.endAt=Math.max(last.endAt,interval.endAt);
  else merged.push({...interval});
 }
 return merged;
}
function subtract(intervals:Interval[],covered:Interval[]):Interval[]{
 return covered.reduce((remaining,cut)=>remaining.flatMap(i=>{
  if(cut.endAt<=i.startAt||cut.startAt>=i.endAt)return [i];
  return [...i.startAt<cut.startAt?[{startAt:i.startAt,endAt:cut.startAt}]:[],
   ...i.endAt>cut.endAt?[{startAt:cut.endAt,endAt:i.endAt}]:[]];
 }),intervals);
}
/** Read-only calculation over one caller-proposed environment/round/target scope.
 * CONFIRMED denotes the supplied evidence classification, never server verification.
 * Installed incident and event owners must independently validate it before any write.
 * Deliberately does not infer affected membership from logs, or extend run lifetimes.
 */
export async function previewEventOutage(environment:string,input:unknown){
 if(environment!=='DEV'&&environment!=='PRD')throw new ActivityError(503,'event_environment_unavailable');
 const parsed=proposalSchema.safeParse(input);if(!parsed.success)throw new ActivityError(400,'contract_violation');
 const proposal=parsed.data,{schedule,impacts}=proposal;
 if(environment!==proposal.environment)throw new ActivityError(409,'event_environment_conflict');
 if(proposal.expectedRevision!==proposal.revision)throw new ActivityError(409,'event_revision_conflict');
 const {startAt,participationEndsAt:T,completionGraceEndsAt:G,manualClaimEndsAt:C}=schedule;
 if(!(startAt<T&&T<G&&G<=C))throw new ActivityError(400,'event_schedule_invalid');
 if(proposal.claimOpensAt!==null&&(proposal.claimOpensAt<startAt||proposal.claimOpensAt>=C))throw new ActivityError(400,'event_claim_window_invalid');
 const applied=new Set(proposal.alreadyAppliedImpactIds);
 if(new Set(impacts.map(i=>i.impactId)).size!==impacts.length||applied.size!==proposal.alreadyAppliedImpactIds.length||
  [...applied].some(key=>!impacts.some(i=>i.impactId===key&&i.status==='CONFIRMED')))throw new ActivityError(400,'event_impact_identity_invalid');
 for(const impact of impacts){
  if(impact.startAt!==null&&impact.endAt!==null&&impact.endAt<=impact.startAt)throw new ActivityError(400,'event_impact_interval_invalid');
  if(impact.status==='CONFIRMED'&&(impact.startAt===null||impact.endAt===null||impact.evidenceRef===null||impact.endAt>proposal.observedAt))
   throw new ActivityError(400,'event_confirmed_impact_evidence_required');
 }
 const windows={PARTICIPATION:[startAt,T],COMPLETION:[T,G],CLAIM:[proposal.claimOpensAt,C],AGGREGATION:[startAt,proposal.observedAt]} as const;
 const grouped=Object.fromEntries(features.map(feature=>{
  const [lower,upper]=windows[feature];
  if(lower===null)return [feature,[]];
  const clipped=(isApplied:boolean)=>union(impacts.filter(i=>i.feature===feature&&i.status==='CONFIRMED'&&applied.has(i.impactId)===isApplied).map(i=>({
   startAt:Math.max(lower,i.startAt!),endAt:Math.min(upper,i.endAt!)
  })).filter(i=>i.endAt>i.startAt));
  return [feature,subtract(clipped(false),clipped(true))];
 })) as Record<typeof features[number],Interval[]>;
 const duration=Object.fromEntries(features.map(feature=>[feature,grouped[feature].reduce((n,i)=>n+(i.endAt-i.startAt),0)])) as Record<typeof features[number],number>;
 const newT=T+duration.PARTICIPATION;
 const newG=Math.max(G+duration.COMPLETION,duration.PARTICIPATION>0?newT+day:G);
 const newC=Math.max(C+duration.CLAIM,duration.PARTICIPATION>0?newT+7*day:C,newG>G?newG+6*day:C);
 if(newC>maxTimestamp||newG>maxTimestamp||newT>maxTimestamp)throw new ActivityError(400,'event_deadline_out_of_range');
 return {contractVersion:'admin.event-outage-preview.v1.20261020',environment,roundId:proposal.roundId,revision:proposal.revision,
  definitionHash:proposal.definitionHash,targetScope:proposal.targetScope,sourceAuthorityVerified:false,
  previewHash:await sha256Hex(canonicalJson(proposal)),originalSchedule:schedule,
  recommendedSchedule:{startAt,participationEndsAt:newT,completionGraceEndsAt:newG,manualClaimEndsAt:newC},
  confirmedImpactDurationsMs:{...duration,CLAIM:proposal.claimOpensAt===null?null:duration.CLAIM},
  confirmedImpactIntervals:{...grouped,CLAIM:proposal.claimOpensAt===null?null:grouped.CLAIM},
  impactSource:'CALLER_PROPOSED_EVIDENCE_NOT_AUTHENTICATED_INCIDENT_RECORDS',
  recommendationComplete:proposal.claimOpensAt!==null&&impacts.every(i=>i.status==='CONFIRMED'),
  excludedAlreadyAppliedImpactIds:[...applied],unconfirmedImpactDurationMs:null,
  unconfirmedImpactCount:impacts.filter(i=>i.status!=='CONFIRMED').length,
  aggregationAction:'REPLAY_ORIGINAL_AUTHORITY_EVENTS_NO_FORCED_EXTENSION',
  reopenReviewRequired:newT>T&&proposal.observedAt>=T,
  recoveryTimingReviewRequired:proposal.observedAt>=G&&newG>G||proposal.observedAt>=C&&newC>C,
  overlapAndContentSupport:'NOT_OBSERVED_REVIEW_REQUIRED',executeSupported:false,persisted:false,applied:false,reviewRequired:true,
  revisionStatus:'PROPOSED_ORIGINAL_REVISION_NOT_VERIFIED_AGAINST_PERSISTED_OWNER',
  accountingStatus:'PROPOSED_APPLIED_IMPACT_IDENTITIES_NOT_VERIFIED_AGAINST_PERSISTED_OWNER',
  writePermission:{permission:'EVENT_CONFIG_WRITE',roles:['OWNER','OPERATOR'],currentAuthorityVerified:false},
  missingSources:['CURRENT_PERSISTED_OPERATING_EVENT_REVISION','AUTHENTICATED_INCIDENT_SCOPE_AND_INTERVALS','ORIGINAL_EVENT_EXTENSION_COMMAND_OWNER'],
  preservation:{originalEligibleRunSet:true,originalRunExpiry:true,settledRights:true,unsettledRights:true,unknownPaymentOriginalKey:true},
  compensation:{separateRights:true,amountsChanged:false,payoutExecuted:false},
  closeJobContract:'MUST_RECHECK_CURRENT_PERSISTED_SCHEDULE_REVISION_AT_COMMIT'};
}
/** Pure decision aid only. Caller must acquire current persisted values and
 * atomically fence the actual close transaction; this does not provide that fence. */
export function inspectEventCloseRevision(input:{jobRevision:number;currentRevision:number;now:number;deadline:number}){
 const value=z.strictObject({jobRevision:revision,currentRevision:revision,now:time,deadline:time}).parse(input);
 const revisionMatches=value.jobRevision===value.currentRevision;
 return {revisionMatches,due:revisionMatches&&value.now>=value.deadline,executionAuthorized:false};
}
