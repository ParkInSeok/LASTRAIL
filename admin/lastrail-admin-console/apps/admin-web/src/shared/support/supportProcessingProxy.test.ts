import {afterEach,describe,it,expect,vi} from 'vitest';
import {onRequest} from '../../../functions/api/[[path]]';
describe('support Pages proxy',()=>{
 afterEach(()=>vi.unstubAllGlobals());
 for(const path of ['support/tickets/t/processing','support/processor-grants/p'])it(`retains command identity/error and no-store for ${path}`,async()=>{
  const fetcher=vi.fn(async()=>Response.json({error:'ADMIN_PERMISSION_DENIED'},{status:403}));vi.stubGlobal('fetch',fetcher);
  const r=await onRequest({request:new Request('https://admin.test/api/'+path,{method:'POST',headers:{'Idempotency-Key':'original-key'},body:'{}'}),env:{ADMIN_API_BASE_URL:'https://upstream.test'},params:{path:path.split('/')}});
  const [,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(new Headers(init.headers).get('Idempotency-Key')).toBe('original-key');expect(r.status).toBe(403);expect(r.headers.get('cache-control')).toBe('no-store');
 });
});
