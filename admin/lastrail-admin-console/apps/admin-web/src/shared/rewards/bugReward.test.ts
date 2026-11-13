import {describe,it,expect} from 'vitest';
import {estimateBugReward, parseBugRewardDraft} from './bugReward';
const draft=(grade:string,contribution='FIRST',paid:unknown=0)=>parseBugRewardDraft({grade,contribution,previousGratitudePaid:paid});
describe('approved v2 independent reward estimates',()=>{
 it.each([['MINOR',250,125],['NORMAL',750,375],['MAJOR',1500,750],['CRITICAL',2500,1250]])('%s uses latest FREE amounts', (grade,first,additional)=>{
  expect(estimateBugReward(draft(String(grade))).gratitude.target).toBe(first);
  expect(estimateBugReward(draft(String(grade),'JOINT_FIRST')).gratitude.target).toBe(first);
  expect(estimateBugReward(draft(String(grade),'ADDITIONAL')).gratitude.target).toBe(additional);
 });
 it('estimates upward difference without clawback or duplicate rewards',()=>{
  expect(estimateBugReward(draft('MAJOR','FIRST',750)).gratitude.difference).toBe(750);
  expect(estimateBugReward(draft('CRITICAL','FIRST',1500)).gratitude.difference).toBe(1000);
  expect(estimateBugReward(draft('MINOR','FIRST',750)).gratitude.difference).toBe(0);
  expect(estimateBugReward(draft('CRITICAL','DUPLICATE')).gratitude.target).toBe(0);
  expect(estimateBugReward(draft('NORMAL','ADDITIONAL',750)).gratitude.difference).toBe(0);
 });
 it('preserves missing ledger data and separates all-player amounts from combined illustrations',()=>{
  const major=estimateBugReward(draft('MAJOR','FIRST',null));
  expect(major.gratitude.difference).toBeNull();
  expect(major.allPlayer.policyAmount).toBe(1500);
  expect(major.firstReporterCombinedIllustration).toBe(3000);
  expect(estimateBugReward(draft('CRITICAL')).firstReporterCombinedIllustration).toBe(5000);
  expect(estimateBugReward(draft('NORMAL')).allPlayer.policyAmount).toBeNull();
  expect(estimateBugReward(draft('CRITICAL','DUPLICATE')).firstReporterCombinedIllustration).toBeNull();
  expect(major.executionEnabled).toBe(false);
  expect(major.damageRestoration).toBe('SEPARATE_REVIEW');
 });
 it.each([{grade:'OTHER',contribution:'FIRST',previousGratitudePaid:0},{grade:'MAJOR',contribution:'OTHER',previousGratitudePaid:0},...[-1,0.5,NaN,Infinity,'750',undefined,Number.MAX_SAFE_INTEGER+1].map(previousGratitudePaid=>({grade:'MAJOR',contribution:'FIRST',previousGratitudePaid}))])('rejects invalid boundary input %j',value=>expect(()=>parseBugRewardDraft(value)).toThrow());
});
