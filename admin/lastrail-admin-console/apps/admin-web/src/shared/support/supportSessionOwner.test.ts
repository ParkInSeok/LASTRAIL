import {afterEach,describe,it,expect,vi} from 'vitest';
import {QueryClient} from '@tanstack/react-query';
import {ownSupportSession} from './supportSessionOwner';
import {SupportWriteSession} from './SupportWriteSession';
afterEach(()=>vi.useRealTimers());
describe('support uncertainty lifetime',()=>{
 it('retains the original owner after the default inactive cache lifetime',async()=>{
  vi.useFakeTimers();const client=new QueryClient({defaultOptions:{queries:{gcTime:300000}}});const create=vi.fn(()=>new SupportWriteSession({verify:async()=>{},write:async()=>{throw new Error('lost');}},()=> 'original-key'));
  const first=ownSupportSession(client,'actor','dev',create);first.review('ticket/t',{publicReply:'original'});await first.confirm();await vi.advanceTimersByTimeAsync(300001);
  const remount=ownSupportSession(client,'actor','dev',create);expect(remount).toBe(first);expect(remount.snapshot().phase).toBe('uncertain');expect(create).toHaveBeenCalledOnce();client.clear();
 });
});
