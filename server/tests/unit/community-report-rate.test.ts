import {describe,it,expect} from 'vitest';
import {evaluateCommunityReportRate} from '../../apps/game-api/src/community-report-rate';
describe('approved report rolling windows',()=>{
 it('allows below both limits and exact old lower bounds',()=>{expect(evaluateCommunityReportRate([0,1,2],60002)).toBeNull();expect(evaluateCommunityReportRate(Array.from({length:20},(_,i)=>i),86400019)).toBeNull();});
 it('uses rolling minute3 and day20 with largest ceil release',()=>{expect(evaluateCommunityReportRate([1000,1001,1002],1003)).toEqual({retryAfter:60});const history=Array.from({length:20},(_,i)=>i*60000);expect(evaluateCommunityReportRate(history,19*60000)).toEqual({retryAfter:85260});});
 it('rejects invalid server event ordering sources instead of resetting quota',()=>{expect(()=>evaluateCommunityReportRate([NaN],1000)).toThrow();expect(()=>evaluateCommunityReportRate([1001],1000)).toThrow();});
});