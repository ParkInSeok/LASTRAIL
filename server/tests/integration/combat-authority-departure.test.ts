import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {describeCombatRequirements} from "../../apps/game-api/src/combat-readiness";
it("pins ordinary solo authority in quotes and original START without granting public readiness",async()=>{
 const f=await setup(true),q=await(await f.preview()).json();
 const raw=String(f.scalar("SELECT preview_json FROM operation_previews"));
 const expected={mode:"ORDINARY_SOLO",combatOwner:"UNITY_LOCAL",transactionOwner:"SERVER"};
 expect(JSON.parse(raw).admission.combatAuthority).toMatchObject(expected);
 expect(describeCombatRequirements(raw,"I")).toMatchObject({canStart:false,authority:{status:"PINNED",contract:expected}});
 const r=await(await f.start(q.operationPreviewId,"authority-start")).json();expect(r.admission.combatAuthority).toMatchObject(expected);
 expect(await(await f.start(q.operationPreviewId,"authority-start")).json()).toEqual(r);
});
it("does not inject current ownership into old requirements",()=>{
 expect(describeCombatRequirements(JSON.stringify({admission:{version:"departure-v3"}}),"I")).toMatchObject({authority:{status:"HISTORICAL",contract:null},canStart:false});
});
it("rejects tampered mode before debit or run creation",async()=>{
 const f=await setup(true),q=await(await f.preview()).json(),fuel=f.scalar("SELECT fuel FROM fuel_state");
 const preview=JSON.parse(String(f.scalar("SELECT preview_json FROM operation_previews")));
 preview.admission.combatAuthority.mode="WORLD_BOSS_SOLO";
 f.sql.run("UPDATE operation_previews SET preview_json=?",[JSON.stringify(preview)]);
 expect(describeCombatRequirements(JSON.stringify(preview),"I").blockingReasons).toContain("combat_authority_contract_invalid");
 expect(await(await f.start(q.operationPreviewId)).json()).toMatchObject({errorCode:"departure_quote_changed"});
 expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(fuel);expect(f.scalar("SELECT count(*) FROM runs")).toBe(0);
});
it("replays committed pre-split START bytes without policy injection",async()=>{
 const f=await setup(true),q=await(await f.preview()).json();await f.start(q.operationPreviewId,"old-authority");
 const stored=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
 delete stored.admission.combatAuthority;delete stored.departureResponse.admission.combatAuthority;
 const original=JSON.stringify(stored);f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[original]);
 expect(await(await f.start(q.operationPreviewId,"old-authority")).json()).toEqual(stored.departureResponse);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(original);
});
it("does not accept client-selected combat authority in preview requests",async()=>{
 const f=await setup(true);
 const {body}=await import("../helpers/departure-fixture");
 const response=await f.preview({...body,combatAuthority:{mode:"ORDINARY_COOP"}});
 expect(response.status).toBe(400);expect(f.scalar("SELECT count(*) FROM operation_previews")).toBe(0);
});
