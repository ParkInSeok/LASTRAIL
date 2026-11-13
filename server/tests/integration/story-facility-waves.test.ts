import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {StoryFacilityStore} from "../../apps/game-api/src/story-facility-store";
const definition={version:"story.facility-definition.v1",definitionVersion:"test",nodeId:"node",facilityId:"pump",kind:"PUMP",maxHp:600};
const attacker={version:"story.facility-attacker.v1",actorId:"scout-1",facilityId:"pump",entryCompletedAt:600,normalPeriodMs:2500,tier:"EARLY"};
const plan={version:"story.facility-wave-plan.v1",waves:[{waveId:"w1",kind:"BASIC",actorId:"scout-1"},{waveId:"w2",kind:"BASIC",actorId:"scout-2"},{waveId:"dc01",kind:"DC01"}]};
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new StoryFacilityStore(f.env.DB);await store.initialize("a",r.runId,definition,attacker,plan);return {...f,store,runId:r.runId};}
const cmd=(kind:string,eventId:string,expectedRevision:number,at:number,waveId:string)=>({kind,eventId,expectedRevision,at,waveId});
it("keeps HP and cancels old warnings while switching to the second pinned attacker",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",{kind:"ATTACK_BEGIN",eventId:"b",expectedRevision:1,at:4600,actorId:"scout-1",actionId:"a"});
 await f.store.commit("a",f.runId,"node",{kind:"ATTACK_COMPLETE",eventId:"h",expectedRevision:2,at:6600,actorId:"scout-1",actionId:"a"});
 await f.store.commit("a",f.runId,"node",{kind:"ATTACK_BEGIN",eventId:"pending",expectedRevision:3,at:10600,actorId:"scout-1",actionId:"pending"});
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","complete",4,11000,"w1"));
 expect((await f.store.read("a",f.runId,"node")).state).toMatchObject({hp:500,attack:{phase:"ENDED",pending:null},waves:{phase:"BETWEEN"}});
 await expect(f.store.commit("a",f.runId,"node",cmd("WAVE_START","early",5,12999,"w2"))).rejects.toThrow("facility_wave_interval_pending");
 const started=await f.store.commit("a",f.runId,"node",cmd("WAVE_START","start",5,13000,"w2"));
 expect(started.state).toMatchObject({hp:500,attack:{definition:{actorId:"scout-2",entryCompletedAt:13600},nextImpactAt:19600},waves:{index:1,phase:"ACTIVE"}});
 expect(await f.store.commit("a",f.runId,"node",cmd("WAVE_START","start",5,13000,"w2"))).toEqual(started);
 await expect(f.store.commit("a",f.runId,"node",{kind:"ATTACK_BEGIN",eventId:"old",expectedRevision:6,at:17600,actorId:"scout-1",actionId:"old"})).rejects.toThrow("facility_attack_actor_mismatch");
});
it("requires both basic waves and DC01 before post-combat, without assigning DC01 facility attacks",async()=>{
 const f=await fixture();await expect(f.store.commit("a",f.runId,"node",{kind:"END_COMBAT",eventId:"skip",expectedRevision:1})).rejects.toThrow("facility_waves_incomplete");
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c1",1,1000,"w1"));await f.store.commit("a",f.runId,"node",cmd("WAVE_START","s2",2,3000,"w2"));
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c2",3,4000,"w2"));await f.store.commit("a",f.runId,"node",cmd("WAVE_START","s3",4,6000,"dc01"));
 await expect(f.store.commit("a",f.runId,"node",{kind:"END_COMBAT",eventId:"skip2",expectedRevision:5})).rejects.toThrow("facility_waves_incomplete");
 await expect(f.store.commit("a",f.runId,"node",{kind:"ATTACK_BEGIN",eventId:"elite",expectedRevision:5,at:7000,actorId:"scout-2",actionId:"elite"})).rejects.toThrow("facility_wave_attack_forbidden");
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c3",5,7000,"dc01"));expect((await f.store.commit("a",f.runId,"node",{kind:"END_COMBAT",eventId:"end",expectedRevision:6})).state.postCombat).toBe(true);
});
it("rejects skipped/repeated waves and rolls back a failed transition",async()=>{
 const f=await fixture();await expect(f.store.commit("a",f.runId,"node",cmd("WAVE_START","skip",1,5000,"dc01"))).rejects.toThrow();
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c1",1,1000,"w1"));
 await expect(f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","again",2,1001,"w1"))).rejects.toThrow("facility_wave_state_invalid");
 f.sql.run("CREATE TRIGGER fail_wave BEFORE INSERT ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commit("a",f.runId,"node",cmd("WAVE_START","s2",2,3000,"w2"))).rejects.toThrow("injected");expect((await f.store.read("a",f.runId,"node")).state.waves).toMatchObject({index:0,phase:"BETWEEN"});
});
it("never resets a disabled facility when another basic wave starts",async()=>{
 const f=await fixture();for(let i=0;i<6;i++){const at=6600+i*6000;await f.store.commit("a",f.runId,"node",{kind:"ATTACK_BEGIN",eventId:`b${i}`,expectedRevision:1+2*i,at:at-2000,actorId:"scout-1",actionId:`a${i}`});await f.store.commit("a",f.runId,"node",{kind:"ATTACK_COMPLETE",eventId:`h${i}`,expectedRevision:2+2*i,at,actorId:"scout-1",actionId:`a${i}`});}
 await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c1",13,37000,"w1"));
 const r=await f.store.commit("a",f.runId,"node",cmd("WAVE_START","s2",14,39000,"w2"));
 expect(r.state).toMatchObject({hp:0,optionalFailed:true,firstStopEventId:"h5",attack:{phase:"FRONTLINE",normalNextAt:42100}});
});
it("requires explicit new wave plans and leaves old snapshots unchanged",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId,{...definition,nodeId:"legacy"},attacker);
 expect((await f.store.read("a",f.runId,"legacy")).state).not.toHaveProperty("waves");
 await expect(f.store.commit("a",f.runId,"legacy",cmd("WAVE_COMPLETE","c",1,1000,"w1"))).rejects.toThrow("facility_wave_plan_missing");
 await expect(f.store.initialize("a",f.runId,definition,attacker,{...plan,waves:plan.waves.slice(0,2)})).rejects.toThrow("facility_initialization_conflict");
});

it("supports the two-wave plan without inventing a DC01 wave",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId,{...definition,nodeId:"basic"},attacker,{...plan,waves:plan.waves.slice(0,2)});
 await f.store.commit("a",f.runId,"basic",cmd("WAVE_COMPLETE","c1",1,1000,"w1"));await f.store.commit("a",f.runId,"basic",cmd("WAVE_START","s2",2,3000,"w2"));await f.store.commit("a",f.runId,"basic",cmd("WAVE_COMPLETE","c2",3,4000,"w2"));
 expect((await f.store.commit("a",f.runId,"basic",{kind:"END_COMBAT",eventId:"end",expectedRevision:4})).state.waves).toMatchObject({phase:"COMPLETE",completedWaveIds:["w1","w2"]});
});
it("rejects malformed plans and wrong first actor before creating a checkpoint",async()=>{
 const f=await fixture();for(const waves of [[plan.waves[0]], [plan.waves[0],plan.waves[0]], [plan.waves[0],plan.waves[1],{waveId:"dc",kind:"DC01",actorId:"elite"}]])await expect(f.store.initialize("a",f.runId,{...definition,nodeId:"invalid"},attacker,{...plan,waves})).rejects.toThrow();
 await expect(f.store.initialize("a",f.runId,{...definition,nodeId:"invalid"},{...attacker,actorId:"wrong"},plan)).rejects.toThrow("facility_wave_actor_mismatch");
 expect(f.scalar("SELECT count(*) FROM story_facility_states")).toBe(1);
});
it("restores the same active wave after response loss and rejects changed retries",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node",cmd("WAVE_COMPLETE","c1",1,1000,"w1"));
 const batch=f.env.DB.batch.bind(f.env.DB);f.env.DB.batch=async statements=>{await batch(statements);throw new Error("lost");};
 const command=cmd("WAVE_START","s2",2,3000,"w2"),r=await f.store.commit("a",f.runId,"node",command);
 const restored=new StoryFacilityStore(f.env.DB);expect(await restored.lookup("a",f.runId,"node","s2")).toEqual(r);
 expect((await restored.read("a",f.runId,"node")).state.waves).toMatchObject({index:1,phase:"ACTIVE",completedWaveIds:["w1"]});
 await expect(restored.commit("a",f.runId,"node",{...command,at:3001})).rejects.toThrow("facility_event_conflict");
});
