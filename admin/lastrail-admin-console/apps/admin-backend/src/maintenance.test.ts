import {describe,it,expect,vi} from "vitest";
import {buildServer} from "./server";
import {UpstreamHttpError} from "./upstream";
const config={adminApiBaseUrl:"https://admin.test",profile:"local" as const,host:"127.0.0.1",port:4174,localAccessSubject:"test"};
describe("maintenance read proxy",()=>{
 it("forwards draft command with exact key and preserves conflict",async()=>{const upstream=vi.fn(async()=>{throw new UpstreamHttpError(409,"maintenance_draft_revision_conflict");});const server=buildServer({config,upstream});try{const body={command:"SAVE_DRAFT",expectedDraftRevision:2};const result=await server.inject({method:"POST",url:"/api/maintenance-plans/p/commands",headers:{"Idempotency-Key":"exact-key"},payload:body});expect(result.statusCode).toBe(409);expect(result.headers["cache-control"]).toBe("no-store");const [path,args]=upstream.mock.calls[0] as unknown as [string,{body:unknown;idempotencyKey:string}];expect(path).toBe("/v1/admin/maintenance-plans/p/commands");expect(args.body).toEqual(body);expect(args.idempotencyKey).toBe("exact-key");}finally{await server.close();}});
 it("preserves preview timestamp, errors, no-store and rejects ambiguous query",async()=>{const upstream=vi.fn(async()=>{throw new UpstreamHttpError(503,"maintenance_plan_unavailable");});const server=buildServer({config,upstream});try{
 const result=await server.inject("/api/maintenance-plans/p/preview?at=250");expect(result.statusCode).toBe(503);expect(result.headers["cache-control"]).toBe("no-store");expect(result.json()).toEqual({error:"maintenance_plan_unavailable"});
 const args=upstream.mock.calls[0] as unknown as [string,{searchParams:URLSearchParams}];expect(args[0]).toBe("/v1/admin/maintenance-plans/p/preview");expect(args[1].searchParams.get("at")).toBe("250");
 for(const url of ["/api/maintenance-plans/p/preview?at=1&at=2","/api/maintenance-plans?environment=PRD","/api/maintenance-plans/p?at=1"])expect((await server.inject(url)).statusCode).toBe(400);
 expect((await server.inject({method:"POST",url:"/api/maintenance-plans/p/commands",payload:{command:"ACTIVATE"}})).statusCode).toBe(400);
 }finally{await server.close();}});
});
