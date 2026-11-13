import {describe,it,expect,vi} from 'vitest';
import {SupportWriteSession} from './SupportWriteSession';
import {AdminApiError} from '../api/client';
describe('support command ownership and recovery',()=>{
 it('blocks duplicate commands and replays exact body/key after lost response',async()=>{
  const write=vi.fn().mockRejectedValueOnce(new Error('lost')).mockResolvedValueOnce({revision:4});
  const session=new SupportWriteSession({verify:async()=>{},write},()=> 'original-key');
  session.review('/ticket',{expectedRevision:3,publicReply:'answer'});await session.confirm();
  expect(session.snapshot().phase).toBe('uncertain');session.review('/other',{publicReply:'replacement'});await session.confirm();expect(write).toHaveBeenCalledTimes(1);
  await session.retry();expect(write).toHaveBeenCalledTimes(2);expect(write.mock.calls[1]).toEqual(write.mock.calls[0]);expect(session.snapshot().phase).toBe('saved');
 });
 it('checks current identity before POST and purges sensitive body on revoked access',async()=>{
  const write=vi.fn();const s=new SupportWriteSession({verify:async()=>{throw new AdminApiError({status:403,path:'/bootstrap',errorCode:'ADMIN_PERMISSION_DENIED'});},write});
  s.review('/ticket',{publicReply:'private'});await s.confirm();expect(write).not.toHaveBeenCalled();expect(JSON.stringify(s.snapshot())).not.toContain('private');expect(s.snapshot().phase).toBe('revoked');
 });
 it('releases authoritative revision conflicts without silently changing request revision',async()=>{
  const s=new SupportWriteSession({verify:async()=>{},write:async()=>{throw new AdminApiError({status:409,path:'/ticket',errorCode:'revision_conflict'});}});
  s.review('/ticket',{expectedRevision:3});await s.confirm();expect(s.snapshot().phase).toBe('rejected');
 });
 it('calls one write while a request is already pending',async()=>{
  let done:(v:unknown)=>void=()=>{};const write=vi.fn(()=>new Promise(resolve=>{done=resolve;}));const s=new SupportWriteSession({verify:async()=>{},write});
  s.review('/ticket',{publicReply:'answer'});const first=s.confirm();await Promise.resolve();await s.confirm();expect(write).toHaveBeenCalledTimes(1);done({revision:1});await first;
 });
 it('unbinds only its subscription',()=>{const s=new SupportWriteSession({verify:async()=>{},write:async()=>({})});const listener=vi.fn(),other=vi.fn();const off=s.subscribe(listener);s.subscribe(other);off();s.review('/t',{});expect(listener).not.toHaveBeenCalled();expect(other).toHaveBeenCalledOnce();});
 it('does not disable independent OWNER management after processing grant denial',async()=>{const write=vi.fn().mockRejectedValueOnce(new AdminApiError({status:403,path:'/processing',errorCode:'ADMIN_PERMISSION_DENIED'})).mockResolvedValueOnce({result:{revision:1}});const s=new SupportWriteSession({verify:async()=>{},write});s.review('ticket/t',{publicReply:'private'});await s.confirm();expect(s.locked).toBe(false);expect(JSON.stringify(s.snapshot())).not.toContain('private');s.review('grant/target',{action:'GRANT'});await s.confirm();expect(write).toHaveBeenCalledTimes(2);expect(s.snapshot().phase).toBe('saved');});
});
