import {afterEach,describe,it,expect,vi} from "vitest";
import {buildServer} from "./server";
const config={adminApiBaseUrl:"https://admin.test",profile:"local" as const,host:"127.0.0.1",port:4174,localAccessSubject:"local-test"};
describe("notice proxy",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("preserves exact command key and server conflicts without caching",async()=>{
  const fetcher=vi.fn(async()=>Response.json({error:"notice_revision_conflict"},{status:409}));vi.stubGlobal("fetch",fetcher);const server=buildServer({config});
  try{const result=await server.inject({method:"POST",url:"/api/notices/n/commands",headers:{"Idempotency-Key":"notice-key"},payload:{command:"PUBLISH",expectedRevision:1}});
   expect(result.statusCode).toBe(409);expect(result.json()).toEqual({error:"notice_revision_conflict"});expect(result.headers["cache-control"]).toBe("no-store");
   const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe("/v1/admin/notices/n/commands");expect(new Headers(init.headers).get("Idempotency-Key")).toBe("notice-key");
  }finally{await server.close();}
 });
 it("forwards pagination and rejects duplicate/unsupported query keys",async()=>{
  const upstream=vi.fn(async()=>({items:[]}));const server=buildServer({config,upstream});
  try{expect((await server.inject("/api/notices?limit=20&after=n")).statusCode).toBe(200);expect(upstream.mock.calls).toHaveLength(1);
   for(const url of ["/api/notices?limit=20&limit=10","/api/notices?environment=PRD","/api/notices/n?limit=20"])expect((await server.inject(url)).statusCode).toBe(400);
   expect((await server.inject({method:"POST",url:"/api/notices/n/commands",payload:{}})).statusCode).toBe(400);expect(upstream.mock.calls).toHaveLength(1);
  }finally{await server.close();}
 });
});
