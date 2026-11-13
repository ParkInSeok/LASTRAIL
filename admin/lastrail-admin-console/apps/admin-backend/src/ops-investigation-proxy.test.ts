import {afterEach,expect,it,vi} from "vitest";
import {buildServer} from "./server";
import {createUpstreamClient} from "./upstream";
const config={profile:"local" as const,host:"127.0.0.1",port:0,adminApiBaseUrl:"https://synthetic.invalid",localAccessSubject:"test"};
afterEach(()=>vi.unstubAllGlobals());
it("local OPS BFF forwards original key/body and preserves conflict current, 403 and 503",async()=>{
 const current={errorLogId:"event",version:3};let status=409;const fetcher=vi.fn(async()=>Response.json(status===409?{error:"ops_investigation_version_conflict",current}:{error:status===403?"ADMIN_PERMISSION_DENIED":"ops_investigation_outcome_unknown"},{status}));vi.stubGlobal("fetch",fetcher);
 const server=buildServer({config,upstream:createUpstreamClient(config)});try{
 for(const nextStatus of [409,403,503]){status=nextStatus;const response=await server.inject({method:"POST",url:"/api/ops/error-logs/event/investigation",headers:{"Idempotency-Key":"original-key"},payload:{status:"acknowledged",expectedVersion:0,reason:"investigate",fixBuild:null,evidence:null,verificationResult:null}});expect(response.statusCode).toBe(status);expect(response.headers["cache-control"]).toBe("no-store");if(status===409)expect(response.json().current).toEqual(current);}
 const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe("/v1/admin/ops/error-logs/event/investigation");expect(new Headers(init.headers).get("Idempotency-Key")).toBe("original-key");expect(JSON.parse(init.body as string).expectedVersion).toBe(0);
 expect((await server.inject({method:"POST",url:"/api/ops/error-logs/event/investigation?actor=x",headers:{"Idempotency-Key":"original-key"},payload:{}})).statusCode).toBe(400);
 expect((await server.inject({method:"POST",url:"/api/ops/error-logs/event/investigation",payload:{}})).statusCode).toBe(400);expect(fetcher).toHaveBeenCalledTimes(3);
 }finally{await server.close();}
});
it("local OPS list/summary/groups forward the same filters including period and phase",async()=>{
 const fetcher=vi.fn(async()=>Response.json({collectionAvailable:true,items:[]}));vi.stubGlobal("fetch",fetcher);const server=buildServer({config,upstream:createUpstreamClient(config)});try{for(const suffix of ["","/summary","/groups"]){const response=await server.inject({method:"GET",url:"/api/ops/error-logs"+suffix+"?environment=DEV&phase=save&contractVersion=v1&occurredFrom=2026-10-01&limit=50"});expect(response.statusCode).toBe(200);expect(response.headers["cache-control"]).toBe("no-store");}for(const [url] of fetcher.mock.calls as unknown as Array<[URL]>){expect(url.searchParams.get("phase")).toBe("save");expect(url.searchParams.get("occurredFrom")).toBe("2026-10-01");}}finally{await server.close();}
});

it("local OPS preserves nested invalid-filter error codes",async()=>{
 vi.stubGlobal("fetch",async()=>Response.json({error:{code:"invalid_log_filter"}},{status:400}));const server=buildServer({config,upstream:createUpstreamClient(config)});try{const response=await server.inject({method:"GET",url:"/api/ops/error-logs/summary?occurredFrom=invalid"});expect(response.statusCode).toBe(400);expect(response.json().error).toBe("invalid_log_filter");}finally{await server.close();}
});

it("actual local bootstrap does not fabricate the pending OPS WRITE grant",async()=>{
 for(const role of ["OWNER","OPERATOR","VIEWER"]){const server=buildServer({config,upstream:async()=>({principal:{id:"synthetic",accessSubject:"synthetic",displayName:"Synthetic",role}})});try{const result=await server.inject({method:"GET",url:"/api/bootstrap"});expect(result.statusCode).toBe(200);expect(result.json().admin.permissions.includes("OPS_ERROR_LOG_WRITE")).toBe(false);}finally{await server.close();}}
});
