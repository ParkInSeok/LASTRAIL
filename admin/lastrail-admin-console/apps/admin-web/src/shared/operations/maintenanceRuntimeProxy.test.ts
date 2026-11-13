import {afterEach,describe,it,expect,vi} from 'vitest';
import {onRequest} from '../../../functions/api/[[path]]';
afterEach(()=>vi.unstubAllGlobals());
describe('maintenance runtime Pages transport',()=>{
 it('preserves immediate write key, delegated identity and no-store conflict without adding a key to review',async()=>{const fetcher=vi.fn<typeof fetch>(async()=>Response.json({error:'maintenance_schedule_conflict'},{status:409}));vi.stubGlobal('fetch',fetcher);for(const route of ['immediate-apply','immediate-review']){const r=await onRequest({request:new Request('https://admin.test/api/maintenance/'+route,{method:'POST',headers:{'Idempotency-Key':'original','cf-access-jwt-assertion':'test'},body:'{}'}),env:{ADMIN_API_BASE_URL:'https://upstream.test'},params:{path:['maintenance',route]}});expect(r.status).toBe(409);expect(r.headers.get('cache-control')).toBe('no-store');const headers=new Headers(fetcher.mock.calls.at(-1)?.[1]?.headers);expect(headers.get('x-lastrail-user-access-jwt')).toBe('test');expect(headers.get('Idempotency-Key')).toBe(route==='immediate-apply'?'original':null);}});
 it('rejects immediate apply without key and current-state with query',async()=>{const fetcher=vi.fn();vi.stubGlobal('fetch',fetcher);for(const request of [new Request('https://admin.test/api/maintenance/immediate-apply',{method:'POST',body:'{}'}),new Request('https://admin.test/api/maintenance/current-state?environment=PRD')]){const path=new URL(request.url).pathname.split('/').slice(2);expect((await onRequest({request,env:{ADMIN_API_BASE_URL:'https://upstream.test'},params:{path}})).status).toBe(400);}expect(fetcher).not.toHaveBeenCalled();});
 it('preserves initialization identity, delegated authentication and conflict',async()=>{
  const fetcher=vi.fn(async()=>Response.json({error:'idempotency_conflict'},{status:409}));vi.stubGlobal('fetch',fetcher);
  const response=await onRequest({request:new Request('https://admin.test/api/maintenance-runtime/initialize',{method:'POST',headers:{'Idempotency-Key':'original-key','cf-access-jwt-assertion':'test-assertion'},body:JSON.stringify({reason:'test-only'})}),env:{ADMIN_API_BASE_URL:'https://upstream.test'},params:{path:['maintenance-runtime','initialize']}});
  const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe('/v1/admin/maintenance-runtime/initialize');expect(new Headers(init.headers).get('Idempotency-Key')).toBe('original-key');expect(new Headers(init.headers).get('x-lastrail-user-access-jwt')).toBe('test-assertion');expect(response.status).toBe(409);expect(response.headers.get('cache-control')).toBe('no-store');
 });
 it('marks current stored runtime reads as no-store',async()=>{
  vi.stubGlobal('fetch',vi.fn(async()=>Response.json({policy:{revision:1}})));
  const response=await onRequest({request:new Request('https://admin.test/api/maintenance-runtime'),env:{ADMIN_API_BASE_URL:'https://upstream.test'},params:{path:['maintenance-runtime']}});expect(response.headers.get('cache-control')).toBe('no-store');
 });
});
