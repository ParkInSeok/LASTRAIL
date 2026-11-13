import {afterEach,describe,it,expect,vi} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {MemoryRouter} from 'react-router-dom';
import {QueryClient,QueryClientProvider,useQuery} from '@tanstack/react-query';
import {useAdmin} from '../auth/AdminContext';
import {AdminApiError,adminApi} from '../api/client';
import {SupportProcessingPanel} from './SupportProcessingPanel';
vi.mock('../auth/AdminContext',()=>({useAdmin:vi.fn()}));
vi.mock('@tanstack/react-query',async()=>({...await vi.importActual<typeof import('@tanstack/react-query')>('@tanstack/react-query'),useQuery:vi.fn()}));
const ticket={ticketId:'test',status:'IN_REVIEW',revision:3,publicReply:'<script>reply</script>',history:[{revision:2,status:'IN_REVIEW',principalId:'operator',createdAt:'2026-10-01T00:00:00Z',publicReply:'answer',internalNote:'SECRET_INTERNAL'}],historyTruncated:false};
function render(role='OWNER',environment='dev',error?:Error){
 vi.mocked(useAdmin).mockReturnValue({bootstrap:{environment,admin:{id:'test-owner',displayName:'owner',role,permissions:['SUPPORT_READ']}},isLoading:false,error:null} as never);
 vi.mocked(useQuery).mockImplementation((options:any)=>(options.queryKey.at(-2)==='ticket'?{data:{ticket},error,isFetching:false}:{}) as never);
 return renderToStaticMarkup(<QueryClientProvider client={new QueryClient()}><MemoryRouter><SupportProcessingPanel ticketId="test"/></MemoryRouter></QueryClientProvider>);
}
afterEach(()=>{vi.clearAllMocks();vi.unstubAllGlobals();});
describe('approved support display boundaries',()=>{
 it('shows OWNER grant management despite denied processing permission',()=>{const html=render('OWNER','dev',new AdminApiError({status:403,path:'/processing',errorCode:'ADMIN_PERMISSION_DENIED'}));expect(html).toContain('문의 처리 권한 관리');expect(html).toContain('대상 운영자 ID');expect(html).not.toContain('SECRET_INTERNAL');expect(html).not.toContain('고객 답변 ·');});
 it('separates escaped public reply from internal history',()=>{const html=render();expect(html).toContain('&lt;script&gt;reply&lt;/script&gt;');expect(html).not.toContain('<script>reply');expect(html).toContain('고객에게 표시되지 않습니다');expect(html).toContain('운영 처리 이력');expect(html).toContain('SECRET_INTERNAL');expect(html.split('고객 표시 미리보기')[1].split('</div>')[0]).not.toContain('SECRET_INTERNAL');});
 it('does not expose grant management to OPERATOR and makes PRD read-only',()=>{expect(render('OPERATOR')).not.toContain('권한 관리 · OWNER');const html=render('OWNER','prd');expect(html).toContain('PRD에서는 조회만');expect(html).toContain('<fieldset disabled=""');});
 it('sends exact body/key and rejects mismatched receipt revision',async()=>{const fetcher=vi.fn().mockResolvedValueOnce(Response.json({contractVersion:'support-ticket-process-v1',revision:4})).mockResolvedValueOnce(Response.json({contractVersion:'support-ticket-process-v1',revision:8}));vi.stubGlobal('fetch',fetcher);const body={expectedRevision:3,status:'CLOSED' as const,publicReply:'final'};await adminApi.supportProcess('test',body,'key');expect(fetcher.mock.calls[0][1].headers['Idempotency-Key']).toBe('key');expect(JSON.parse(fetcher.mock.calls[0][1].body)).toEqual(body);await expect(adminApi.supportProcess('test',body,'key')).rejects.toThrow();});
});
