import { describe,expect,it } from 'vitest';
import { evaluateCommunityCreationRate } from '../../apps/game-api/src/community-creation-rate';
const at=4000000;
describe('approved creation rolling rate',()=>{
 it('allows the exact comment minimum interval boundary',()=>{expect(evaluateCommunityCreationRate('COMMENT',[at-10000],at)).toBeNull();expect(evaluateCommunityCreationRate('COMMENT',[at-9999],at)).toEqual({retryAfter:1});});
 it('keeps the exact 60 minute lower bound outside the window',()=>{expect(evaluateCommunityCreationRate('COMMENT',Array(60).fill(at-3600000),at)).toBeNull();expect(evaluateCommunityCreationRate('COMMENT',Array(60).fill(at-3599999),at)).toEqual({retryAfter:1});});
 it('uses the latest release of every binding limit and rounds seconds upwards',()=>{const events=[...Array(59).fill(at-1800000),at-1];expect(evaluateCommunityCreationRate('COMMENT',events,at)).toEqual({retryAfter:1800});});
 it('keeps post and comment approved numbers independent',()=>{expect(evaluateCommunityCreationRate('POST',[at-10000],at)).toEqual({retryAfter:50});expect(evaluateCommunityCreationRate('COMMENT',[at-10000],at)).toBeNull();expect(evaluateCommunityCreationRate('POST',Array(10).fill(at-100000),at)).toEqual({retryAfter:3500});});
 it('does not depend on stored event order',()=>{expect(evaluateCommunityCreationRate('COMMENT',[at-5000,at-20000],at)).toEqual({retryAfter:5});});
 it('does not bypass corrupt or future server event times',()=>{for(const events of [[at+1],[NaN],[-1],[1.5]])expect(()=>evaluateCommunityCreationRate('COMMENT',events,at)).toThrow('community_rate_source_invalid');});
});