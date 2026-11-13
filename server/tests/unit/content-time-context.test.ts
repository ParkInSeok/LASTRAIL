import {expect,it} from 'vitest';
import {contentTimeContext,monthlyTimeContext} from '../../apps/game-api/src/content-time-context';
it('uses the existing service day at its exact boundary, independently of device midnight',()=>{
 const end=Date.parse('2026-10-21T19:00:00Z');
 expect(monthlyTimeContext(end-1).boundaries).toContainEqual({kind:'SERVICE_DAY_END',at:end});
 expect(monthlyTimeContext(end).boundaries).toContainEqual({kind:'SERVICE_DAY_END',at:end+86400000});
});
it('keeps participation and claim deadlines distinct without granting rights or inventing grace',()=>{
 const value=contentTimeContext('ACTIVITY','p',20,[{kind:'PARTICIPATION_END',at:10},{kind:'CLAIM_END',at:30}]);
 expect(value).toEqual({version:'data.time-resume.v1.20261021',owner:'ACTIVITY',sourceVersion:'p',serverTime:20,boundaries:[{kind:'PARTICIPATION_END',at:10},{kind:'CLAIM_END',at:30}]});
});
