import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {StoryFacilityStore} from "../../apps/game-api/src/story-facility-store";
const definition={version:"story.facility-definition.v1",definitionVersion:"test-facility",nodeId:"node",facilityId:"pump",kind:"PUMP",maxHp:600};
const attacker={version:"story.facility-attacker.v1",actorId:"scout-1",facilityId:"pump",entryCompletedAt:600,normalPeriodMs:2500,tier:"EARLY"};
async function fixture(tier="EARLY"){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new StoryFacilityStore(f.env.DB);await store.initialize("a",r.runId,definition,{...attacker,tier});return {...f,store,runId:r.runId};}
const command=(kind:string,eventId:string,expectedRevision:number,at:number,actionId="attack-1")=>({kind,eventId,expectedRevision,at,actionId,actorId:"scout-1"});
it("pins first impact after entry, requires full warning and persists next reservation",async()=>{
 const f=await fixture();expect((await f.store.read("a",f.runId,"node")).state.attack).toMatchObject({nextImpactAt:6600});
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","early",1,4599))).rejects.toThrow("facility_attack_not_ready");
 await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","begin",1,4600));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","early-hit",2,6599))).rejects.toThrow("facility_attack_not_ready");
 const hit=await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",2,6600));expect(hit).toMatchObject({hpLoss:100,state:{hp:500,attack:{nextImpactAt:12600,recoveryUntil:6900,pending:null}}});
 expect(await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",2,6600))).toEqual(hit);
 await f.store.initialize("a",f.runId,definition,attacker);expect((await f.store.read("a",f.runId,"node")).state.hp).toBe(500);
});
it("cancels the pending attack on death and never infers a hit from the animation",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","begin",1,4600));
 await f.store.commit("a",f.runId,"node",command("ATTACK_DEATH","death",2,6599));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",3,6600))).rejects.toThrow("facility_attack_inactive");
 expect((await f.store.read("a",f.runId,"node")).state).toMatchObject({hp:600,attack:{phase:"DEAD",pending:null}});
});
it("never catches up missed attacks or shortens a late warning",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","late-begin",1,20000));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","early",2,21999))).rejects.toThrow("facility_attack_not_ready");
 const hit=await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","late-hit",2,30000));expect(hit).toMatchObject({hpLoss:100,state:{hp:500,attack:{nextImpactAt:36000}}});
});
it("stops at zero, preserves optional failure and reserves a full normal period",async()=>{
 const f=await fixture("MIDDLE");for(let i=0;i<6;i++){const at=6600+i*6000;await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN",`b${i}`,1+i*2,at-2000,`attack${i}`));await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE",`h${i}`,2+i*2,at,`attack${i}`));}
 expect((await f.store.read("a",f.runId,"node")).state).toMatchObject({hp:0,optionalFailed:true,firstStopEventId:"h5",attack:{phase:"FRONTLINE",pending:null,normalNextAt:39100}});
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","after-stop",13,40600))).rejects.toThrow("facility_attack_inactive");
});
it("binds actions to the original actor and rolls back schedule with HP",async()=>{
 const f=await fixture();await expect(f.store.commit("a",f.runId,"node",{...command("ATTACK_BEGIN","wrong",1,4600),actorId:"other"})).rejects.toThrow("facility_attack_actor_mismatch");
 await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","begin",1,4600));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","wrong-action",2,6600,"other"))).rejects.toThrow("facility_attack_action_mismatch");
 f.sql.run("CREATE TRIGGER fail_attack BEFORE INSERT ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");await expect(f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",2,6600))).rejects.toThrow("injected");
 expect((await f.store.read("a",f.runId,"node")).state).toMatchObject({hp:600,attack:{pending:{actionId:"attack-1",impactAt:6600}}});
});

it("does not recycle action IDs, regress logical time or bypass the bound attacker",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","b",1,4600));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","double",2,4601,"new"))).rejects.toThrow("facility_attack_busy");
 await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","h",2,6600));
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","reuse",3,10600))).rejects.toThrow("facility_attack_action_reused");
 await expect(f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","regress",3,6599,"new"))).rejects.toThrow("facility_attack_time_regressed");
 await expect(f.store.commit("a",f.runId,"node",{kind:"DAMAGE",eventId:"bypass",expectedRevision:3,resolvedDamage:600})).rejects.toThrow("facility_attack_command_required");
});
it("restores a pending warning exactly and cancels it when combat ends",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","begin",1,4600));
 const restored=new StoryFacilityStore(f.env.DB);expect((await restored.read("a",f.runId,"node")).state.attack?.pending).toEqual({actionId:"attack-1",beganAt:4600,impactAt:6600});
 await restored.commit("a",f.runId,"node",{kind:"END_COMBAT",eventId:"end",expectedRevision:2});
 expect((await restored.read("a",f.runId,"node")).state).toMatchObject({hp:600,attack:{phase:"ENDED",pending:null}});
 await expect(restored.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",3,6600))).rejects.toThrow("facility_combat_ended");
});
it("never adds an attacker to historical state implicitly and rejects definition changes",async()=>{
 const f=await fixture();await expect(f.store.initialize("a",f.runId,definition,{...attacker,actorId:"new"})).rejects.toThrow("facility_initialization_conflict");
 await f.store.initialize("a",f.runId,{...definition,nodeId:"legacy"});
 expect((await f.store.read("a",f.runId,"legacy")).state).not.toHaveProperty("attack");
 await expect(f.store.commit("a",f.runId,"legacy",command("ATTACK_BEGIN","b",1,4600))).rejects.toThrow("facility_attack_state_missing");
});
it("returns the original attack result after a lost acknowledgement",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",command("ATTACK_BEGIN","begin",1,4600));
 const batch=f.env.DB.batch.bind(f.env.DB);f.env.DB.batch=async statements=>{await batch(statements);throw new Error("lost");};
 const hit=await f.store.commit("a",f.runId,"node",command("ATTACK_COMPLETE","hit",2,6600));
 expect(await f.store.lookup("a",f.runId,"node","hit")).toEqual(hit);expect((await f.store.read("a",f.runId,"node")).state.hp).toBe(500);
});
