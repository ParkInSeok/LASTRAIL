import {afterEach,describe,expect,it,vi} from "vitest";
import {onRequest} from "../../../functions/api/[[path]]";
describe("runtime policy Admin proxy",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("preserves command identity and no-store using only the Admin credential",async()=>{
  const fetcher=vi.fn(async()=>Response.json({accepted:true},{headers:{"cache-control":"no-store"}}));vi.stubGlobal("fetch",fetcher);
  const request=new Request("https://admin.test/api/runtime-policy",{method:"POST",headers:{"Idempotency-Key":"same-key","content-type":"application/json","authorization":"Bearer game-not-forwarded"},body:"{}"});
  const response=await onRequest({request,env:{ADMIN_API_BASE_URL:"https://admin-api.test",ADMIN_API_TOKEN:"admin-test"},params:{path:["runtime-policy"]}});
  const [url,options]=fetcher.mock.calls[0] as unknown as [URL,RequestInit];
  expect(url.pathname).toBe("/v1/admin/runtime-policy");
  expect(new Headers(options.headers).get("Idempotency-Key")).toBe("same-key");
  expect(new Headers(options.headers).get("authorization")).toBe("Bearer admin-test");
  expect(response.headers.get("cache-control")).toBe("no-store");
 });
});
