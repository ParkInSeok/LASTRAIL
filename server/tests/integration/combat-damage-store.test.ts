import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatDamageStore} from "../../apps/game-api/src/combat-damage-store";
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 const store=new CombatDamageStore(f.env.DB);await store.initialize("a",r.runId,[{id:"eve",hp:10,maxHp:10}]);
 return {...f,store,runId:r.runId};}
it("commits HP and ledger with zero-loss receipts and restores original results after later events",async()=>{
 const f=await fixture();const start=f.scalar("SELECT snapshot_json FROM run_snapshots");
 const command={eventId:"hit-1",expectedRevision:1,targetId:"eve",hpLoss:4};
 const first=await f.store.commit("a",f.runId,command);
 expect(first).toMatchObject({revision:2,hpBefore:10,hpLoss:4,hpAfter:6});
 await f.store.commit("a",f.runId,{...command,eventId:"zero",expectedRevision:2,hpLoss:0});
 expect(await f.store.commit("a",f.runId,command)).toEqual(first);
 const restored=await new CombatDamageStore(f.env.DB).read("a",f.runId);
 expect(restored).toMatchObject({revision:3,state:{targets:[{id:"eve",hp:6,maxHp:10}],ledger:[{eventId:"hit-1"},{eventId:"zero"}]}});
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(2);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(start);
});
it("rejects event conflicts, stale revisions, foreign ownership and impossible loss",async()=>{
 const f=await fixture(),c={eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4};await f.store.commit("a",f.runId,c);
 await expect(f.store.commit("a",f.runId,{...c,hpLoss:3})).rejects.toThrow("combat_damage_event_conflict");
 await expect(f.store.commit("a",f.runId,{...c,eventId:"stale"})).rejects.toThrow("combat_damage_revision_conflict");
 await expect(f.store.read("other",f.runId)).rejects.toThrow("combat_run_not_found");
 await expect(f.store.commit("a",f.runId,{...c,eventId:"bad",expectedRevision:2,hpLoss:7})).rejects.toThrow("combat_damage_loss_invalid");
});
it("rolls back checkpoint when receipt persistence fails",async()=>{
 const f=await fixture(),before=await f.store.read("a",f.runId);
 f.sql.run("CREATE TRIGGER fail_damage_receipt BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commit("a",f.runId,{eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4})).rejects.toThrow();
 expect(await f.store.read("a",f.runId)).toEqual(before);expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
});
it("fences terminal races but replays an earlier receipt after termination",async()=>{
 const f=await fixture(),c={eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4};const original=await f.store.commit("a",f.runId,c);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 await expect(f.store.commit("a",f.runId,{...c,eventId:"late",expectedRevision:2})).rejects.toThrow("combat_damage_state_conflict");
 expect((await f.store.read("a",f.runId)).revision).toBe(2);
 expect(await f.store.commit("a",f.runId,c)).toEqual(original);
});

it("queries original receipts without advancing state and never resets a damaged run on initialize",async()=>{
 const f=await fixture(),c={eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4};
 expect(await f.store.lookup("a",f.runId,"missing")).toBeNull();
 const original=await f.store.commit("a",f.runId,c);
 expect(await f.store.lookup("a",f.runId,"hit")).toEqual(original);
 await f.store.initialize("a",f.runId,[{id:"eve",hp:10,maxHp:10}]);
 expect((await f.store.read("a",f.runId)).state.targets[0]?.hp).toBe(6);
 await expect(f.store.initialize("a",f.runId,[{id:"eve",hp:9,maxHp:10}])).rejects.toThrow("combat_damage_initialization_conflict");
 await expect(f.store.lookup("other",f.runId,"hit")).rejects.toThrow("combat_run_not_found");
});
it("recovers a response lost after the transaction committed",async()=>{
 const f=await fixture(),base=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const result=await base(statements);if(lose){lose=false;throw new Error("response lost");}return result;};
 const result=await f.store.commit("a",f.runId,{eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4});
 expect(result).toMatchObject({revision:2,hpAfter:6});
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(1);
});
it("rejects a revision changed at commit time and preserves the competing checkpoint",async()=>{
 const f=await fixture();
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_damage_states SET revision=2");};
 await expect(f.store.commit("a",f.runId,{eventId:"hit",expectedRevision:1,targetId:"eve",hpLoss:4})).rejects.toThrow("combat_damage_state_conflict");
 expect((await f.store.read("a",f.runId)).state.targets[0]?.hp).toBe(10);
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
});
