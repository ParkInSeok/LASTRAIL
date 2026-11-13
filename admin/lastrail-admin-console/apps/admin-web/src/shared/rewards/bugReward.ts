/** Local review input only. Never a payment command or authoritative eligibility record. */
export const BUG_REWARD_POLICY_VERSION = 'bug-report-reward-v2.20261019' as const;
export const ALL_PLAYER_POLICY_VERSION = 'major-bug-all-player-reward-v2.20261019' as const;
const amounts = Object.freeze({MINOR:250,NORMAL:750,MAJOR:1500,CRITICAL:2500});
export type BugGrade = keyof typeof amounts;
export type BugContribution = 'FIRST' | 'JOINT_FIRST' | 'ADDITIONAL' | 'DUPLICATE';
export type BugRewardDraft = Readonly<{
 grade: BugGrade;
 contribution: BugContribution;
 /** Confirmed same incident/account gratitude total; null means unknown, never zero. */
 previousGratitudePaid: number | null;
}>;
const contributions: readonly string[] = ['FIRST','JOINT_FIRST','ADDITIONAL','DUPLICATE'];
export function parseBugRewardDraft(input: unknown): BugRewardDraft {
 if(!input || typeof input!=='object')throw new Error('보상 검토 입력이 필요합니다.');
 const value=input as Record<string,unknown>;
 if(typeof value.grade!=='string'||!Object.hasOwn(amounts,value.grade))throw new Error('승인된 등급이 아닙니다.');
 if(typeof value.contribution!=='string'||!contributions.includes(value.contribution))throw new Error('기여 유형을 확인하세요.');
 const paid=value.previousGratitudePaid;
 // No rounding rule has been approved; reject fractions rather than silently rounding.
 if(paid!==null&&(typeof paid!=='number'||!Number.isSafeInteger(paid)||paid<0))throw new Error('확인된 기지급 정수 또는 미확인이 필요합니다.');
 return Object.freeze({grade:value.grade as BugGrade,contribution:value.contribution as BugContribution,previousGratitudePaid:paid});
}
export function estimateBugReward(input: BugRewardDraft) {
 const draft=parseBugRewardDraft(input),base=amounts[draft.grade];
 const target=draft.contribution==='DUPLICATE'?0:draft.contribution==='ADDITIONAL'?base/2:base;
 const allPlayer=draft.grade==='MAJOR'||draft.grade==='CRITICAL'?base:null;
 return Object.freeze({
  currency:'FREE_CRYSTAL' as const,
  gratitude:Object.freeze({policyVersion:BUG_REWARD_POLICY_VERSION,cap:base,target,difference:draft.previousGratitudePaid===null?null:Math.max(0,target-draft.previousGratitudePaid)}),
  allPlayer:Object.freeze({policyVersion:ALL_PLAYER_POLICY_VERSION,policyAmount:allPlayer,eligibility:'UNRESOLVED' as const}),
  firstReporterCombinedIllustration:allPlayer!==null&&(draft.contribution==='FIRST'||draft.contribution==='JOINT_FIRST')?base+allPlayer:null,
  damageRestoration:'SEPARATE_REVIEW' as const,
  executionEnabled:false as const,
 });
}
