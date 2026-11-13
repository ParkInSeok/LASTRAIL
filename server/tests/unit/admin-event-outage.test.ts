import {expect,it} from 'vitest';
import {previewEventOutage,type EventOutageProposal,inspectEventCloseRevision} from '../../apps/admin-api/src/admin-event-outage';
const day=86400000,T=20*day;
const proposal=():EventOutageProposal=>({environment:'DEV',roundId:'r',revision:2,expectedRevision:2,definitionHash:'a'.repeat(64),
 targetScope:'all confirmed affected players',observedAt:T+8*day,alreadyAppliedImpactIds:[],
 claimOpensAt:day,schedule:{startAt:day,participationEndsAt:T,completionGraceEndsAt:T+day,manualClaimEndsAt:T+7*day},impacts:[]});
function impact(id:string,feature:'PARTICIPATION'|'COMPLETION'|'CLAIM'|'AGGREGATION',startAt:number,endAt:number){
 return {impactId:id,incidentId:id,feature,status:'CONFIRMED' as const,startAt,endAt,evidenceRef:'original:'+id};
}
it('unions overlapping actual intervals and clips them to the original opportunity window',async()=>{
 const p=proposal();p.impacts=[impact('a','PARTICIPATION',T-3*day,T-day),impact('b','PARTICIPATION',T-2*day,T+day)];
 const result=await previewEventOutage('DEV',p);
 expect(result.confirmedImpactDurationsMs.PARTICIPATION).toBe(3*day);
 expect(result.recommendedSchedule).toEqual({startAt:day,participationEndsAt:T+3*day,completionGraceEndsAt:T+4*day,manualClaimEndsAt:T+10*day});
 expect(result).toMatchObject({executeSupported:false,persisted:false,sourceAuthorityVerified:false,reopenReviewRequired:true});
});
it('does not invent a claim opening or count already applied impact identities again',async()=>{
 const p=proposal();p.impacts=[impact('a','PARTICIPATION',T-day,T),impact('b','CLAIM',T,T+day)];
 p.alreadyAppliedImpactIds=['a'];p.claimOpensAt=null;
 const result=await previewEventOutage('DEV',p);
 expect(result.confirmedImpactDurationsMs.CLAIM).toBeNull();
 expect(result.recommendationComplete).toBe(false);
 expect(result.excludedAlreadyAppliedImpactIds).toEqual(['a']);
 expect(result.recommendedSchedule).toEqual(p.schedule);
 expect(result.unconfirmedImpactDurationMs).toBeNull();
});
it('subtracts prior covered intervals even if a new incident overlaps them, and leaves input unchanged',async()=>{
 const p=proposal();p.impacts=[impact('old','PARTICIPATION',T-3*day,T-day),impact('new','PARTICIPATION',T-2*day,T)];
 p.alreadyAppliedImpactIds=['old'];const original=structuredClone(p);
 const result=await previewEventOutage('DEV',p);
 expect(result.confirmedImpactDurationsMs.PARTICIPATION).toBe(day);
 expect(p).toEqual(original);
 await expect(previewEventOutage('DEV',{...p,alreadyAppliedImpactIds:['missing']})).rejects.toThrow();
 await expect(previewEventOutage('DEV',{...p,impacts:[p.impacts[0],p.impacts[0]]})).rejects.toThrow();
});
it('completion-only loss preserves T and original runs while ensuring six days after extended G',async()=>{
 const p=proposal();p.impacts=[impact('a','COMPLETION',T,T+day)];
 const result=await previewEventOutage('DEV',p);
 expect(result.recommendedSchedule).toMatchObject({participationEndsAt:T,completionGraceEndsAt:T+2*day,manualClaimEndsAt:T+8*day});
 expect(result.preservation).toMatchObject({originalEligibleRunSet:true,originalRunExpiry:true,settledRights:true,unknownPaymentOriginalKey:true});
});
it('claim-only loss changes C only; aggregation delay and unknown/planned durations do not force extension',async()=>{
 const p=proposal();p.impacts=[impact('a','CLAIM',T+day,T+2*day),impact('b','AGGREGATION',T,T+5*day),
 {...impact('c','PARTICIPATION',day,T),status:'PLANNED'},
 {...impact('d','PARTICIPATION',day,T),status:'UNKNOWN',endAt:null,evidenceRef:null}];
 const result=await previewEventOutage('DEV',p);
 expect(result.recommendedSchedule).toMatchObject({participationEndsAt:T,completionGraceEndsAt:T+day,manualClaimEndsAt:T+8*day});
 expect(result.unconfirmedImpactCount).toBe(2);
 const aggregation=proposal();aggregation.impacts=[impact('b','AGGREGATION',T,T+5*day)];
 expect((await previewEventOutage('DEV',aggregation)).recommendedSchedule).toEqual(aggregation.schedule);
});
it('preserves longer original deadlines and binds content/revision into the preview hash',async()=>{
 const p=proposal();p.schedule.completionGraceEndsAt=T+3*day;p.schedule.manualClaimEndsAt=T+15*day;
 p.impacts=[impact('a','PARTICIPATION',T-day,T)];
 const result=await previewEventOutage('DEV',p);
 expect(result.recommendedSchedule).toMatchObject({completionGraceEndsAt:T+3*day,manualClaimEndsAt:T+15*day});
 expect((await previewEventOutage('DEV',p)).previewHash).toBe(result.previewHash);
 await expect(previewEventOutage('DEV',{...p,expectedRevision:1})).rejects.toThrow();
 await expect(previewEventOutage('PRD',p)).rejects.toThrow();
 await expect(previewEventOutage('DEV',{...p,impacts:[{...p.impacts[0],endAt:null}]})).rejects.toThrow();
});
it('rejects old close jobs before considering time; a matching revision only establishes due time, not execution',()=>{
 expect(inspectEventCloseRevision({jobRevision:1,currentRevision:2,now:T+10*day,deadline:T})).toEqual({revisionMatches:false,due:false,executionAuthorized:false});
 expect(inspectEventCloseRevision({jobRevision:2,currentRevision:2,now:T-1,deadline:T})).toMatchObject({revisionMatches:true,due:false});
 expect(inspectEventCloseRevision({jobRevision:2,currentRevision:2,now:T,deadline:T})).toMatchObject({revisionMatches:true,due:true,executionAuthorized:false});
});
it('preserves one millisecond in distant intervals without unsafe absolute timestamp intermediates',async()=>{
 const p=proposal();p.observedAt=8000000000000001;
 p.impacts=[impact('a','AGGREGATION',day,2000000000000000+day),impact('b','AGGREGATION',8000000000000000,8000000000000001)];
 const result=await previewEventOutage('DEV',p);
 expect(result.confirmedImpactDurationsMs.AGGREGATION).toBe(2000000000000001);
 expect(result).toMatchObject({applied:false,persisted:false,executeSupported:false,reviewRequired:true});
});
