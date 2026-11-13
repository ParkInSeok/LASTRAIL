import {expect,it} from "vitest";
import {setup,body} from "../helpers/departure-fixture";
import {readLocalDepartureSelection} from "../../apps/game-api/src/departure";
it("accepts nullable legacy selection at the current atomic core while public combat remains gated",async()=>{
 const f=await setup(true,false),q=await(await f.preview({...body,expectedUltimateRevision:0},"q")).json();
 const rejected=await f.post("/v1/runs/start",{operationPreviewId:q.operationPreviewId},"public");
 expect(await rejected.json()).toMatchObject({errorCode:"combat_not_ready"});
 expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
 const started=await f.start(q.operationPreviewId,"core");expect(started.status).toBe(200);
 const result=await started.json();expect(result).toMatchObject({ultimateOperatorId:null,admission:{ultimateSelection:{operatorId:null,revision:0}}});
 expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
 expect(f.scalar("SELECT count(*) FROM runs")).toBe(1);
});
it("replays the original result after legacy selection changes without rebuilding the snapshot",async()=>{
 const f=await setup(true,false),q=await(await f.preview({...body,expectedUltimateRevision:0},"q")).json();
 const first=await f.start(q.operationPreviewId,"core");expect(first.status).toBe(200);const response=await first.json();
 const original=f.scalar("SELECT snapshot_json FROM run_snapshots");
 f.sql.run("UPDATE account_formation SET ultimate_operator_id='op1',ultimate_revision=1");
 expect(await(await f.start(q.operationPreviewId,"core")).json()).toEqual(response);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(original);
 expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
});
it("keeps the explicit historical leader producer guard and permits individual-policy selection reads",async()=>{
 const f=await setup(true,false);
 await expect(readLocalDepartureSelection(f.env.DB,"a",{...body,expectedUltimateRevision:0})).rejects.toThrow("ultimate_selection_required");
 const request={operationId:body.operationId,risk:body.risk,expectedFormationRevision:1,expectedLoadoutRevision:1};
 expect(await readLocalDepartureSelection(f.env.DB,"a",request,{allOperatorUltimates:true})).toMatchObject({ultimateSelection:{operatorId:null,revision:0}});
});
