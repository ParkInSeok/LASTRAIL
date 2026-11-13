import {afterEach,expect,it,vi} from "vitest";
import {onRequest} from "../../../functions/api/[[path]]";
afterEach(()=>vi.unstubAllGlobals());
const call=(method:string,path:string,key?:string)=>onRequest({request:new Request("https://admin.invalid/api/"+path,{method,headers:key?{"Idempotency-Key":key,"content-type":"application/json"}:{},...(method==="POST"?{body:JSON.stringify({expectedVersion:0,reason:"investigate"})}:{})}),env:{ADMIN_API_BASE_URL:"https://synthetic.invalid"},params:{path:path.split("?")[0].split("/")}});
it("Pages OPS preserves original key, current, status and no-store",async()=>{
 const current={errorLogId:"event",version:3};const fetcher=vi.fn(async()=>Response.json({error:"ops_investigation_version_conflict",current},{status:409}));vi.stubGlobal("fetch",fetcher);const response=await call("POST","ops/error-logs/event/investigation","original-key");expect(response.status).toBe(409);expect((await response.json()).current).toEqual(current);expect(response.headers.get("cache-control")).toBe("no-store");const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe("/v1/admin/ops/error-logs/event/investigation");expect(new Headers(init.headers).get("Idempotency-Key")).toBe("original-key");
});
it("Pages OPS rejects missing key, query, malformed event and unsupported write route",async()=>{
 const fetcher=vi.fn(async()=>Response.json({}));vi.stubGlobal("fetch",fetcher);
 for(const [path,key] of [["ops/error-logs/event/investigation",undefined],["ops/error-logs/event/investigation?actor=x","original"],["ops/error-logs/bad%20id/investigation","original"],["ops/error-logs/groups","original"]])expect((await call("POST",path!,key)).status).toBe(400);expect(fetcher).not.toHaveBeenCalled();
});

it("actual Pages bootstrap does not fabricate the pending OPS WRITE grant",async()=>{
 for(const role of ["OWNER","OPERATOR","VIEWER"]){vi.stubGlobal("fetch",async()=>Response.json({principal:{id:"synthetic",role,displayName:"Synthetic"}}));const response=await call("GET","bootstrap");expect(response.status).toBe(200);expect((await response.json()).admin.permissions.includes("OPS_ERROR_LOG_WRITE")).toBe(false);}
});
