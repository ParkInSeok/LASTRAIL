import {afterEach,describe,it,expect,vi} from "vitest";
import {buildServer} from "./server";
const config={adminApiBaseUrl:"https://admin.test",profile:"local" as const,host:"127.0.0.1",port:4174,localAccessSubject:"local-test"};
describe("local runtime policy proxy",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("forwards exact key and preserves CAS failure",async()=>{
  const fetcher=vi.fn(async()=>Response.json({error:"runtime_policy_revision_conflict"},{status:409}));vi.stubGlobal("fetch",fetcher);
  const server=buildServer({config});
  try{
   const response=await server.inject({method:"POST",url:"/api/runtime-policy",headers:{"Idempotency-Key":"same-key"},payload:{environment:"DEV",expectedRuntimePolicyRevision:1,adsEnabled:false,reason:"local"}});
   expect(response.statusCode).toBe(409);expect(response.json()).toEqual({error:"runtime_policy_revision_conflict"});expect(response.headers['cache-control']).toBe('no-store');
   const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe('/v1/admin/runtime-policy');expect(new Headers(init.headers).get('Idempotency-Key')).toBe('same-key');
  }finally{await server.close();}
 });
 it("reads without an environment override and rejects query parameters",async()=>{
  const fetcher=vi.fn(async()=>Response.json({accepted:true}));vi.stubGlobal("fetch",fetcher);const server=buildServer({config});
  try{expect((await server.inject({method:"GET",url:"/api/runtime-policy"})).statusCode).toBe(200);expect((await server.inject({method:"GET",url:"/api/runtime-policy?environment=PRD"})).statusCode).toBe(400);expect(fetcher).toHaveBeenCalledTimes(1);}finally{await server.close();}
 });
});
