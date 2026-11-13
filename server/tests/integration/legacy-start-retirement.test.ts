import { expect, it } from "vitest";
import { setup, body } from "../helpers/departure-fixture";
it.each([null, "", "departure-v2", "departure-v3", "departure-v999"])("rejects a new public START for admission version %s without writes", async version => {
 const f=await setup(true),q=await(await f.preview(body,"q")).json();
 f.sql.run("UPDATE operation_previews SET admission_version=?",[version]);
 const before=f.scalar("SELECT fuel FROM fuel_state");
 const result=await f.post("/v1/runs/start",{operationPreviewId:q.operationPreviewId},"start");
 expect(result.status).toBe(409);
 expect(await result.json()).toMatchObject({errorCode:"departure_quote_changed"});
 expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
 expect(f.scalar("SELECT count(*) FROM idempotency_records WHERE operation='run_start'")).toBe(0);
 expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(before);
 expect(f.scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
});
it("replays a committed old public START before checking its obsolete quote version",async()=>{
 const f=await setup(true),q=await(await f.preview(body,"q")).json();
 const first=await(await f.start(q.operationPreviewId,"start")).json();
 f.sql.run("UPDATE operation_previews SET admission_version=NULL");
 const before=f.scalar("SELECT snapshot_json FROM run_snapshots");
 expect(await(await f.post("/v1/runs/start",{operationPreviewId:q.operationPreviewId},"start")).json()).toEqual(first);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(before);
 expect(f.scalar("SELECT count(*) FROM runs")).toBe(1);
});
