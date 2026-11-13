import {afterEach,describe,expect,it,vi} from "vitest";
import {onRequest} from "../../../functions/api/[[path]]";
describe("notice Pages proxy",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("forwards command identity and conflict with no-store",async()=>{
  const fetcher=vi.fn(async()=>Response.json({error:"notice_revision_conflict"},{status:409}));vi.stubGlobal("fetch",fetcher);
  const response=await onRequest({request:new Request("https://admin.test/api/notices/n/commands",{method:"POST",headers:{"Idempotency-Key":"notice-key"},body:"{}"}),env:{ADMIN_API_BASE_URL:"https://upstream.test"},params:{path:["notices","n","commands"]}});
  const [url,init]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];expect(url.pathname).toBe("/v1/admin/notices/n/commands");expect(new Headers(init.headers).get("Idempotency-Key")).toBe("notice-key");expect(response.status).toBe(409);expect(response.headers.get("cache-control")).toBe("no-store");
 });
});
