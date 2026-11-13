import {describe,it,expect,vi} from 'vitest';
import {buildServer} from './server';
import {UpstreamHttpError,createUpstreamClient} from './upstream';
const config={adminApiBaseUrl:'https://admin.test',profile:'local' as const,host:'127.0.0.1',port:4174,localAccessSubject:'test'};
describe('support processing local proxy',()=>{
 for(const path of ['support/tickets/t/processing','support/processor-grants/p'])it(`retains identity/conflict and rejects query for ${path}`,async()=>{
  const upstream=vi.fn(async()=>{throw new UpstreamHttpError(409,'revision_conflict');});const server=buildServer({config,upstream});try{
   const r=await server.inject({method:'POST',url:'/api/'+path,headers:{'Idempotency-Key':'exact-key'},payload:{expectedRevision:3}});expect(r.statusCode).toBe(409);expect(r.headers['cache-control']).toBe('no-store');const args=upstream.mock.calls[0] as unknown as [string,{idempotencyKey:string}];expect(args[0]).toBe('/v1/admin/'+path);expect(args[1].idempotencyKey).toBe('exact-key');expect((await server.inject('/api/'+path+'?environment=PRD')).statusCode).toBe(400);
  }finally{await server.close();}
 });
 it('upstream forwards key and preserves revoked permission',async()=>{const fetcher=vi.fn(async()=>Response.json({error:'ADMIN_PERMISSION_DENIED'},{status:403}));vi.stubGlobal('fetch',fetcher);try{await expect(createUpstreamClient(config)('/v1/admin/support/tickets/t/processing',{method:'POST',body:{},idempotencyKey:'original-key',requestId:'test'})).rejects.toMatchObject({status:403,errorCode:'ADMIN_PERMISSION_DENIED'});const [,args]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(new Headers(args.headers).get('Idempotency-Key')).toBe('original-key');}finally{vi.unstubAllGlobals();}});
});
