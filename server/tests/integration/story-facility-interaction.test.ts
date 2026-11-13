import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {StoryFacilityStore} from "../../apps/game-api/src/story-facility-store";
const definition={version:"story.facility-definition.v2",definitionVersion:"test-interaction",nodeId:"node",facilityId:"prison",kind:"PRISONER_AREA",maxHp:600,interaction:{version:"story.interaction-plan.v1",targetId:"relay",kind:"RELAY"}};
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new StoryFacilityStore(f.env.DB);await store.initialize("a",r.runId,definition);return {...f,store,runId:r.runId};}
const cmd=(kind:string,eventId:string,expectedRevision:number,at:number,actionId="interact")=>({kind,eventId,expectedRevision,at,actionId,actorId:"operator-1",interactionTargetId:"relay"});
it("completes only after 1500 logical milliseconds and persists the original result",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","early",2,2499))).rejects.toThrow("interaction_not_ready");
 const done=await f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","done",2,2500));
 expect(done.state.interaction).toMatchObject({pending:null,completion:{actionId:"interact",actorId:"operator-1",completedAt:2500}});
 expect(await f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","done",2,2500))).toEqual(done);
 expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("cancels without retaining progress and never reuses a canceled action",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 const cancel={...cmd("INTERACTION_CANCEL","cancel",2,2000),reason:"RANGE_LEFT"};await f.store.commit("a",f.runId,"node",cancel);
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","late",3,2500))).rejects.toThrow("interaction_action_mismatch");
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","reuse",3,2500))).rejects.toThrow("interaction_action_reused");
 await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","retry",3,2500,"new"));
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","short",4,3000,"new"))).rejects.toThrow("interaction_not_ready");
 expect(await f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","finish",4,4000,"new"))).toMatchObject({hpLoss:0});
});
it("binds actor and target, rejects overlapping starts and does not cancel for damage",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 await expect(f.store.commit("a",f.runId,"node",{...cmd("INTERACTION_COMPLETE","other",2,2500),actorId:"operator-2"})).rejects.toThrow("interaction_action_mismatch");
 await expect(f.store.commit("a",f.runId,"node",{...cmd("INTERACTION_COMPLETE","target",2,2500),interactionTargetId:"other"})).rejects.toThrow("interaction_target_mismatch");
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","overlap",2,1500,"another"))).rejects.toThrow("interaction_busy");
 await expect(f.store.commit("a",f.runId,"node",{...cmd("INTERACTION_CANCEL","damage",2,1500),reason:"DAMAGE"})).rejects.toThrow();
 await f.store.commit("a",f.runId,"node",{kind:"DAMAGE",eventId:"hit-facility",expectedRevision:2,resolvedDamage:100});
 expect((await f.store.read("a",f.runId,"node")).state.interaction?.pending?.actionId).toBe("interact");
});
it("preserves pending state across recovery and cancels on combat termination",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 const restored=new StoryFacilityStore(f.env.DB);expect((await restored.read("a",f.runId,"node")).state.interaction?.pending).toMatchObject({startedAt:1000,completeAt:2500});
 const r=await restored.commit("a",f.runId,"node",{kind:"END_COMBAT",eventId:"end",expectedRevision:2});
 expect(r.state.interaction).toMatchObject({pending:null,completion:null,lastCancellation:{actionId:"interact",reason:"TERMINATED"}});
 await expect(restored.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","late",3,2500))).rejects.toThrow("facility_combat_ended");
});
it("rolls back completion with the receipt and recovers lost acknowledgements",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 f.sql.run("CREATE TRIGGER fail_interaction BEFORE INSERT ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","done",2,2500))).rejects.toThrow("injected");expect((await f.store.read("a",f.runId,"node")).state.interaction?.completion).toBeNull();
 f.sql.run("DROP TRIGGER fail_interaction");const batch=f.env.DB.batch.bind(f.env.DB);f.env.DB.batch=async statements=>{await batch(statements);throw new Error("lost");};
 const done=await f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","done",2,2500));expect(await f.store.lookup("a",f.runId,"node","done")).toEqual(done);
});

it("handles down cancellation, rejects clock regression and never completes twice",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","start",1,1000));
 await expect(f.store.commit("a",f.runId,"node",{...cmd("INTERACTION_CANCEL","back",2,999),reason:"DOWN"})).rejects.toThrow("facility_time_regressed");
 await f.store.commit("a",f.runId,"node",{...cmd("INTERACTION_CANCEL","down",2,1200),reason:"DOWN"});
 await f.store.commit("a",f.runId,"node",cmd("INTERACTION_START","retry",3,1500,"new"));
 await f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","done",4,3000,"new"));
 await expect(f.store.commit("a",f.runId,"node",cmd("INTERACTION_COMPLETE","twice",5,3001,"new"))).rejects.toThrow("interaction_already_completed");
});
it("keeps legacy definitions untouched and rejects unauthored interaction mappings",async()=>{
 const f=await fixture(),{interaction:omitted,...legacy}=definition;expect(omitted.targetId).toBe("relay");
 await f.store.initialize("a",f.runId,{...legacy,version:"story.facility-definition.v1",nodeId:"legacy"});
 expect((await f.store.read("a",f.runId,"legacy")).state).not.toHaveProperty("interaction");
 await expect(f.store.commit("a",f.runId,"legacy",cmd("INTERACTION_START","start",1,1000))).rejects.toThrow("facility_interaction_missing");
 await expect(f.store.initialize("a",f.runId,{...definition,nodeId:"invalid",kind:"PUMP"})).rejects.toThrow("facility_interaction_plan_invalid");
});
it("retains pending actions across wave holds but disallows completion while between waves",async()=>{
 const f=await fixture(),attacker={version:"story.facility-attacker.v1",actorId:"enemy-1",facilityId:"prison",entryCompletedAt:600,normalPeriodMs:2500,tier:"EARLY"},plan={version:"story.facility-wave-plan.v1",waves:[{waveId:"w1",kind:"BASIC",actorId:"enemy-1"},{waveId:"w2",kind:"BASIC",actorId:"enemy-2"}]};
 await f.store.initialize("a",f.runId,{...definition,nodeId:"waves"},attacker,plan);
 await f.store.commit("a",f.runId,"waves",cmd("INTERACTION_START","start",1,1000));
 await f.store.commit("a",f.runId,"waves",{kind:"WAVE_COMPLETE",eventId:"wc",expectedRevision:2,at:1500,waveId:"w1"});
 await expect(f.store.commit("a",f.runId,"waves",cmd("INTERACTION_COMPLETE","during-hold",3,2500))).rejects.toThrow("interaction_combat_inactive");
 const r=await f.store.commit("a",f.runId,"waves",{...cmd("INTERACTION_CANCEL","cancel",3,2600),reason:"RANGE_LEFT"});expect(r.state.interaction?.pending).toBeNull();
});
