import {describe,it,expect,vi} from 'vitest';import {reviewOutageExtension,OutageExtensionDraft,type OutageEvidence} from './outageExtension';
const day=86400000,base={environment:'DEV' as const,roundId:'round',revision:3,start:0,T:14*day,G:15*day,C:21*day};
const evidence=(change:Partial<OutageEvidence>={}):OutageEvidence=>({incidentId:'incident',environment:'DEV',roundId:'round',feature:'PARTICIPATION',startedAt:100,restoredAt:200,confirmed:true,targetConfirmed:true,...change});
describe('approved outage extension local review',()=>{
 it('unions overlapping incidents by feature without double extension',()=>{const r=reviewOutageExtension(base,base,[evidence(),evidence({incidentId:'other',startedAt:150,restoredAt:250}),evidence({feature:'CLAIM',startedAt:150,restoredAt:250})]);expect(r.duration.PARTICIPATION).toBe(150);expect(r.duration.CLAIM).toBe(100);expect(r.executionAvailable).toBe(false);});
 it('unknown actual or target evidence stays unknown instead of estimated zero',()=>{expect(reviewOutageExtension(base,base,[evidence({restoredAt:null})]).duration.PARTICIPATION).toBeNull();expect(reviewOutageExtension(base,base,[evidence({targetConfirmed:false})]).duration.PARTICIPATION).toBeNull();});
 it('preserves all old deadlines and G-only extension requires six claim days',()=>{const result=reviewOutageExtension(base,{...base,G:base.G+2*day,C:base.C},[evidence({feature:'COMPLETION'})]);expect(result.issues).toContain('유예 종료 이후 최소 6일의 수령 기간이 필요합니다.');expect(reviewOutageExtension(base,{...base,C:base.C-1},[]).issues).toContain('기존 마감을 앞당길 수 없습니다.');});
 it('new participation end preserves approved 24h and 7d minimums',()=>{const r=reviewOutageExtension(base,{...base,T:base.T+day},[evidence()]);expect(r.issues).toContain('새 참여 종료 기준 24시간 유예와 7일 수령을 보장해야 합니다.');});
 it('rejects other scope and keeps aggregation-only recovery separate',()=>{const wrong=reviewOutageExtension(base,base,[evidence({environment:'PRD'})]);expect(wrong.issues).toContain('다른 환경 또는 회차의 장애 근거입니다.');expect(reviewOutageExtension(base,base,[evidence({feature:'AGGREGATION'})]).requiresContributionRecovery).toBe(true);});
 it('invalidates local review on changes; unsubscribe/dispose release lifetime',()=>{const m=new OutageExtensionDraft(base);const fn=vi.fn(),off=m.subscribe(fn);m.setEvidence([evidence()]);m.markReviewed();m.setProposal({...base,C:base.C+day});expect(m.snapshot().reviewed).toBe(false);const count=fn.mock.calls.length;off();m.dispose();expect(fn).toHaveBeenCalledTimes(count);expect(m.snapshot().phase).toBe('disposed');expect(m.snapshot().evidence).toEqual([]);});
});

it('cannot change hidden start or approve deadline extension without the matching impact',()=>{
 expect(reviewOutageExtension(base,{...base,start:NaN},[]).issues.length).toBeGreaterThan(0);
 expect(reviewOutageExtension(base,{...base,start:base.start+1},[]).issues.length).toBeGreaterThan(0);
 expect(reviewOutageExtension(base,{...base,C:base.C+day},[]).issues.length).toBeGreaterThan(0);
 expect(reviewOutageExtension(base,{...base,G:base.G+day,C:base.C+day},[]).issues.length).toBeGreaterThan(0);
 expect(reviewOutageExtension(base,{...base,G:base.G+day,C:base.C+day},[evidence({feature:'COMPLETION'})]).issues).toEqual([]);
});
