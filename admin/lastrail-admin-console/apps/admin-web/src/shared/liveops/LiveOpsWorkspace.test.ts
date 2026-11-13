import {describe,it,expect,vi} from 'vitest';
import {LiveOpsWorkspace} from './LiveOpsWorkspace';
import {newNoticeDocument} from '../operations/noticeContract';
import {AdminApiError} from '../api/client';
const doc={...newNoticeDocument(),title:'공지',summary:'설명',localizedBody:{ko:'본문'}};
const row={noticeId:'n',revision:1,status:'DRAFT',document:doc,updatedAt:1};
const envelope={environment:'DEV',serverTime:1,permissions:{canEdit:true,canPublish:true,canWithdraw:true}};
const list=()=>({...envelope,contractVersion:'admin-notice-list-v1',items:[row],nextAfter:null});
const detail=(revision=1)=>({...envelope,contractVersion:'admin-notice-v1',notice:{...row,revision,effectiveStatus:'DRAFT'},publication:null});
function fixture(){const port={list:vi.fn().mockResolvedValue(list()),detail:vi.fn().mockResolvedValue(detail())};return {port,m:new LiveOpsWorkspace('actor','DEV',port)};}
function deferred(){let resolve!:(v:unknown)=>void;const promise=new Promise(resolveIt=>resolve=resolveIt);return {promise,resolve};}
describe('liveops scoped workspace information ownership',()=>{
 it('reuses valid list/detail across close and return with no navigation GET',async()=>{const {m,port}=fixture();await m.open('NOTICE');await m.select('n');m.closeDetail();await m.select('n');await m.open('NOTICE');expect(port.list).toHaveBeenCalledTimes(1);expect(port.detail).toHaveBeenCalledTimes(1);expect(m.snapshot().list.phase).toBe('ready');});
 it('deduplicates concurrent logical reads',async()=>{const {m,port}=fixture();const d=deferred();port.list.mockReturnValue(d.promise);const a=m.open('NOTICE'),b=m.open('NOTICE');d.resolve(list());await Promise.all([a,b]);expect(port.list).toHaveBeenCalledTimes(1);});
 it('refresh is explicit and old data is unavailable when refresh fails',async()=>{const {m,port}=fixture();await m.open('NOTICE');port.list.mockRejectedValue(new Error('offline'));await m.open('NOTICE',true);expect(port.list).toHaveBeenCalledTimes(2);expect(m.snapshot().list.phase).toBe('error');expect(m.snapshot().list.data).toBeUndefined();});
 it('related state change invalidates exactly its resource and list without automatic GET',async()=>{const {m,port}=fixture();await m.open('NOTICE');await m.select('n');m.invalidate('NOTICE','n');expect(port.list).toHaveBeenCalledTimes(1);expect(port.detail).toHaveBeenCalledTimes(1);expect(m.snapshot().detail.phase).toBe('idle');await m.select('n');expect(port.detail).toHaveBeenCalledTimes(2);});
 it('blocks old response after refresh and invalidation',async()=>{const {m,port}=fixture();await m.open('NOTICE');const old=deferred();port.detail.mockReturnValueOnce(old.promise).mockResolvedValueOnce(detail(3));const pending=m.select('n');m.invalidate('NOTICE','n');await m.select('n');old.resolve(detail(1));await pending;expect(m.snapshot().detail.data?.revision).toBe(3);});
 it('rejects foreign environment or wrong resource responses',async()=>{const {m,port}=fixture();port.list.mockResolvedValue({...list(),environment:'PRD'});await m.open('NOTICE');expect(m.snapshot().list.phase).toBe('error');port.list.mockResolvedValue(list());await m.open('NOTICE',true);port.detail.mockResolvedValue({...detail(),notice:{...detail().notice,noticeId:'other'}});await m.select('n');expect(m.snapshot().detail.phase).toBe('error');});
 it('403 revokes all cached sensitive data and suppresses late results',async()=>{const {m,port}=fixture();await m.open('NOTICE');const late=deferred();port.detail.mockReturnValueOnce(late.promise);const pending=m.select('n');port.list.mockRejectedValue(new AdminApiError({status:403,path:'/api/notices',errorCode:'forbidden'}));await m.open('NOTICE',true);late.resolve(detail());await pending;expect(m.snapshot().phase).toBe('revoked');expect(m.snapshot().detail.data).toBeUndefined();expect(m.snapshot().list.data).toBeUndefined();});
 it('unsubscribe and disposal stop notifications and requests',async()=>{const {m,port}=fixture();const listener=vi.fn();const off=m.subscribe(listener);await m.open('NOTICE');off();const count=listener.mock.calls.length;m.dispose();await m.open('NOTICE');await m.select('n');expect(listener).toHaveBeenCalledTimes(count);expect(port.list).toHaveBeenCalledTimes(1);expect(m.snapshot().phase).toBe('disposed');});
 it('keeps minimal bounded projections and immutable nested data',async()=>{const {m}=fixture();await m.open('NOTICE');expect(m.snapshot().list.data?.items[0]).not.toHaveProperty('document');await m.select('n');expect(Object.isFrozen((m.snapshot().detail.data?.document as typeof doc).localizedBody)).toBe(true);});
});


describe('liveops confirmed response projection',()=>{
 it('records receipt revision without a follow-up GET and rejects old receipt rollback',async()=>{const {m,port}=fixture();await m.open('NOTICE');await m.select('n');const receipt={accepted:true,contractVersion:'admin-notice-command-v1',environment:'DEV',noticeId:'n',revision:2,status:'PUBLISHED',command:'PUBLISH',publishedRevision:2,auditId:'audit',requestId:'request'};m.acceptReceipt('NOTICE',receipt);expect(m.snapshot().confirmed?.revision).toBe(2);expect(m.snapshot().detail.phase).toBe('idle');expect(port.list).toHaveBeenCalledTimes(1);expect(port.detail).toHaveBeenCalledTimes(1);m.acceptReceipt('NOTICE',{...receipt,revision:1});expect(m.snapshot().confirmed?.revision).toBe(2);});
});

describe('liveops observed revision boundaries',()=>{
 it('invalidates detail when explicit list refresh observes a newer revision',async()=>{const {m,port}=fixture();await m.open('NOTICE');await m.select('n');port.list.mockResolvedValue({...list(),items:[{...row,revision:2}]});await m.open('NOTICE',true);expect(m.snapshot().detail.data).toBeUndefined();port.detail.mockResolvedValue(detail(2));await m.select('n');expect(port.detail).toHaveBeenCalledTimes(2);expect(m.snapshot().detail.data?.revision).toBe(2);});
 it('rejects detail older than an already confirmed response',async()=>{const {m,port}=fixture();await m.open('NOTICE');m.acceptReceipt('NOTICE',{accepted:true,contractVersion:'admin-notice-command-v1',environment:'DEV',noticeId:'n',revision:2,status:'PUBLISHED',command:'PUBLISH',publishedRevision:2,auditId:'audit',requestId:'request'});port.detail.mockResolvedValue(detail(1));await m.select('n');expect(m.snapshot().detail.phase).toBe('error');expect(m.snapshot().detail.data).toBeUndefined();expect(m.snapshot().confirmed?.revision).toBe(2);});
});
