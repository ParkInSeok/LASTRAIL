import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {StoryFacilityStore} from "../../apps/game-api/src/story-facility-store";
const definition={version:"story.facility-definition.v1",definitionVersion:"test-approved-facility",nodeId:"node-4",facilityId:"pump",kind:"PUMP",maxHp:600};
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new StoryFacilityStore(f.env.DB);await store.initialize("a",r.runId,definition);return {...f,store,runId:r.runId};}
const hit=(eventId:string,expectedRevision:number,resolvedDamage=100)=>({eventId,expectedRevision,kind:"DAMAGE",resolvedDamage});
it("preserves facility HP across initialization/recovery without touching other targets",async()=>{
 const f=await fixture(),snapshot=f.scalar("SELECT snapshot_json FROM run_snapshots");
 for(let i=1;i<=3;i++)await f.store.commit("a",f.runId,"node-4",hit(`h${i}`,i));
 await f.store.initialize("a",f.runId,definition);
 for(let i=4;i<=5;i++)await f.store.commit("a",f.runId,"node-4",hit(`h${i}`,i));
 expect((await f.store.read("a",f.runId,"node-4")).state).toMatchObject({hp:100,status:"ACTIVE",optionalFailed:false});
 expect(f.scalar("SELECT count(*) FROM combat_damage_states")).toBe(0);expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(snapshot);
});
it("caps overkill, latches first stop and preserves failure after post-combat restoration",async()=>{
 const f=await fixture();for(let i=1;i<=5;i++)await f.store.commit("a",f.runId,"node-4",hit(`h${i}`,i,110));
 const stopped=await f.store.commit("a",f.runId,"node-4",hit("stop",6,110));
 expect(stopped).toMatchObject({hpLoss:50,trainTransfer:0,state:{hp:0,status:"DISABLED",optionalFailed:true,firstStopEventId:"stop"}});
 expect(await f.store.commit("a",f.runId,"node-4",hit("stop",6,110))).toEqual(stopped);
 await expect(f.store.commit("a",f.runId,"node-4",{eventId:"early",expectedRevision:7,kind:"RESTORE"})).rejects.toThrow("facility_post_combat_required");
 await f.store.commit("a",f.runId,"node-4",{eventId:"end",expectedRevision:7,kind:"END_COMBAT"});
 const restored=await f.store.commit("a",f.runId,"node-4",{eventId:"restore",expectedRevision:8,kind:"RESTORE"});
 expect(restored.state).toMatchObject({hp:0,status:"RESTORED",optionalFailed:true,firstStopEventId:"stop",postCombat:true});
 expect(f.scalar("SELECT status FROM runs")).toBe("ACTIVE");expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("rejects healing/shields, unauthorized access and conflicting initialization",async()=>{
 const f=await fixture();for(const kind of ["HEAL","SHIELD","TAUNT","REBUILD"])await expect(f.store.commit("a",f.runId,"node-4",{eventId:kind,expectedRevision:1,kind})).rejects.toThrow();
 await expect(f.store.read("b",f.runId,"node-4")).rejects.toThrow("facility_run_not_found");
 await expect(f.store.initialize("a",f.runId,{...definition,facilityId:"other"})).rejects.toThrow("facility_initialization_conflict");
});
it("stores zero damage and returns immutable results after termination",async()=>{
 const f=await fixture(),c=hit("zero",1,0),r=await f.store.commit("a",f.runId,"node-4",c);expect(r.hpLoss).toBe(0);
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await f.store.commit("a",f.runId,"node-4",c)).toEqual(r);expect(await f.store.lookup("a",f.runId,"node-4","zero")).toEqual(r);
 await expect(f.store.commit("a",f.runId,"node-4",hit("later",2))).rejects.toThrow("facility_run_not_active");
 await expect(f.store.commit("a",f.runId,"node-4",hit("zero",1,1))).rejects.toThrow("facility_event_conflict");
});
it("rolls back state with receipts and fences concurrent changes",async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_facility BEFORE INSERT ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commit("a",f.runId,"node-4",hit("fail",1))).rejects.toThrow("injected");expect((await f.store.read("a",f.runId,"node-4")).state.hp).toBe(600);
 f.sql.run("DROP TRIGGER fail_facility");f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE story_facility_states SET revision=2");};
 await expect(f.store.commit("a",f.runId,"node-4",hit("race",1))).rejects.toThrow("facility_state_conflict");expect(f.scalar("SELECT count(*) FROM story_facility_receipts")).toBe(0);
});
it("recovers lost acknowledgements and keeps different nodes independent",async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let lose=true;f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};
 const r=await f.store.commit("a",f.runId,"node-4",hit("one",1));expect(await f.store.lookup("a",f.runId,"node-4","one")).toEqual(r);
 await f.store.initialize("a",f.runId,{...definition,nodeId:"node-5"});expect((await f.store.read("a",f.runId,"node-5")).state.hp).toBe(600);expect((await f.store.read("a",f.runId,"node-4")).state.hp).toBe(500);
});

it("refuses implicit legacy/coop/world initialization instead of inventing facility support",async()=>{
 for(const mode of [null,"ORDINARY_COOP","WORLD_BOSS_SOLO"] as const){
  const f=await fixture(),snapshot=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
  if(mode===null)delete snapshot.admission.combatAuthority;else snapshot.admission.combatAuthority=captureCombatAuthority(mode);
  f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(snapshot)]);
  await expect(f.store.initialize("a",f.runId,{...definition,nodeId:"new"})).rejects.toThrow("facility_mode_unsupported");
  expect(f.scalar("SELECT count(*) FROM story_facility_states")).toBe(1);
 }
});
it("fences terminal races and original snapshot changes without rewriting saved results",async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 await expect(f.store.commit("a",f.runId,"node-4",hit("race-end",1))).rejects.toThrow("facility_state_conflict");
 expect((await f.store.read("a",f.runId,"node-4")).state.hp).toBe(600);
 f.sql.run("UPDATE runs SET status='ACTIVE'");f.sql.run("UPDATE run_snapshots SET snapshot_json='{}'");
 await expect(f.store.commit("a",f.runId,"node-4",hit("changed",1))).rejects.toThrow("facility_start_changed");
});
it("does not reopen combat or refill HP after recovery and rejects corrupt failure history",async()=>{
 const f=await fixture();await f.store.commit("a",f.runId,"node-4",hit("stop",1,600));
 await f.store.commit("a",f.runId,"node-4",{eventId:"end",expectedRevision:2,kind:"END_COMBAT"});
 await f.store.commit("a",f.runId,"node-4",{eventId:"restore",expectedRevision:3,kind:"RESTORE"});
 await expect(f.store.commit("a",f.runId,"node-4",hit("late",4))).rejects.toThrow("facility_combat_ended");
 await expect(f.store.commit("a",f.runId,"node-4",{eventId:"twice",expectedRevision:4,kind:"RESTORE"})).rejects.toThrow("facility_restore_not_required");
 const state=(await f.store.read("a",f.runId,"node-4")).state;f.sql.run("UPDATE story_facility_states SET state_json=?",[JSON.stringify({...state,optionalFailed:false})]);
 await expect(f.store.read("a",f.runId,"node-4")).rejects.toThrow("facility_state_invalid");
});
