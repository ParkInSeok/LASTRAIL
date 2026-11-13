import {expect,it} from 'vitest';
import {previewEventDefinition,type EventDefinitionDraft} from '../../apps/admin-api/src/admin-event-definition';
const day=86400000,start=1800000000000;
const draft=():EventDefinitionDraft=>({environment:'DEV',roundId:'round-a',revision:1,policyVersion:null,title:'Anniversary',theme:'Anniversary',
 schedule:{publishAt:start,startAt:start,endAt:start+14*day,claimUntil:null,giftOpenAt:start},
 rewards:[{rightId:'gift',kind:'GIFT',condition:'Visit',producerId:null,threshold:null,
 itemId:null,quantity:1,perAccountCount:1,policyVersion:null}]});
it('validates a draft without turning missing authority into scheduling or review approval',async()=>{
 const result=await previewEventDefinition('DEV',draft());
 expect(result).toMatchObject({definitionValid:true,reviewApproved:false,scheduleSupported:false,persisted:false});
 expect(result.writePermission).toEqual({permission:'EVENT_CONFIG_WRITE',roles:['OWNER','OPERATOR'],currentAuthorityVerified:false});
 expect(result.blockers).toContain('ORIGINAL_EVENT_REVIEW_AND_PUBLICATION_OWNER_UNAVAILABLE');
 expect(result.blockers).toContain('ORIGINAL_EVENT_SETTLEMENT_OWNER_UNAVAILABLE');
 expect(result.blockers).toContain('REWARD_SOURCE_OR_ITEM_UNRESOLVED:gift');
});
it('accepts 14,21,28 day initial rounds but rejects invalid order, date-only and outside gifts',async()=>{
 for(const days of [14,21,28]){const d=draft();d.schedule.endAt=start+days*day;expect((await previewEventDefinition('DEV',d)).definitionValid).toBe(true);}
 for(const mutate of [(d:ReturnType<typeof draft>)=>d.schedule.endAt=start+13*day,
 (d:ReturnType<typeof draft>)=>d.schedule.publishAt=start+1,
 (d:ReturnType<typeof draft>)=>d.schedule.giftOpenAt=d.schedule.endAt+1]){
  const d=draft();mutate(d);expect((await previewEventDefinition('DEV',d)).definitionValid).toBe(false);
 }
 await expect(previewEventDefinition('DEV',{...draft(),schedule:{...draft().schedule,startAt:'2026-10-20'}})).rejects.toThrow();
});
it('binds review hash to environment, revision and complete reward definition; totals remain item specific',async()=>{
 const d=draft();d.rewards[0]!.itemId='coin';
 d.rewards.push({...d.rewards[0]!,rightId:'gift2',itemId:'gem',quantity:2});
 const first=await previewEventDefinition('DEV',d),again=await previewEventDefinition('DEV',d);
 expect(first.definitionHash).toBe(again.definitionHash);
 expect(first.proposedTotals).toEqual([{itemId:'coin',quantity:'1'},{itemId:'gem',quantity:'2'}]);
 expect((await previewEventDefinition('DEV',{...d,revision:2})).definitionHash).not.toBe(first.definitionHash);
 await expect(previewEventDefinition('PRD',d)).rejects.toThrow();
 await expect(previewEventDefinition('DEV',{...d,reviewApproved:true})).rejects.toThrow();
});
it('rejects duplicate rights and nonincreasing cumulative thresholds without rejecting overlap as a hard rule',async()=>{
 const d=draft();d.rewards=[{...d.rewards[0]!,kind:'CUMULATIVE',threshold:5},
 {...d.rewards[0]!,rightId:'next',kind:'CUMULATIVE',threshold:4}];
 expect((await previewEventDefinition('DEV',d)).issues).toContain('CUMULATIVE_THRESHOLDS_NOT_INCREASING');
 d.rewards[1]!.rightId='gift';
 expect((await previewEventDefinition('DEV',d)).issues).toContain('DUPLICATE_RIGHT_ID');
});
it('uses approved 24 hour completion and seven day manual deadlines without claiming settlement execution',async()=>{
 const d=draft(),result=await previewEventDefinition('DEV',d);
 expect(result.timePolicy).toMatchObject({participationEndsAt:d.schedule.endAt,completionGraceEndsAt:d.schedule.endAt+day,
  manualClaimEndsAt:d.schedule.endAt+7*day,existingRunExpiryPreserved:true,authorityEventTimeRequired:true,
  lateOriginalReplayPreserved:true,manualAndAutomaticShareOriginalRight:true,settlementExecuted:false});
 d.schedule.claimUntil=d.schedule.endAt+day;
 expect((await previewEventDefinition('DEV',d)).issues).toContain('MANUAL_CLAIM_DEADLINE_MUST_EQUAL_END_PLUS_7_DAYS');
 d.schedule.giftOpenAt=d.schedule.endAt;
 expect((await previewEventDefinition('DEV',d)).issues).toContain('GIFT_OUTSIDE_PARTICIPATION_PERIOD');
});
