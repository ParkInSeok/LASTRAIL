import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatDamageStore} from "../../apps/game-api/src/combat-damage-store";
async function fixture(hp=10){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new CombatDamageStore(f.env.DB);await store.initialize("a",r.runId,[{id:"eve",hp,maxHp:10},{id:"enemy",hp:100,maxHp:100}],{operators:[{actorId:"eve",expanded:true}],enemyTargetIds:["enemy"]});return {...f,store,runId:r.runId};}
const hit=(eventId="hit",expectedRevision=1,hitOrdinal=0)=>({eventId,expectedRevision,targetId:"enemy",sourceActorId:"eve",actionId:"basic-1",hitOrdinal,sourceKind:"BASIC",damage:{base:{numerator:"1",denominator:"1"},effects:[],defenseBps:0,incomingReductionBps:0}});
it("commits enemy HP and only one charge gain per original basic action",async()=>{
 const f=await fixture(),first=await f.store.commitOperatorHit("a",f.runId,hit());
 expect(first).toMatchObject({hpLoss:1,actorCharge:{actorId:"eve",charge:4,gained:4}});
 expect(await f.store.commitOperatorHit("a",f.runId,hit("next",2,1))).toMatchObject({hpAfter:98,actorCharge:{charge:4,gained:0}});
 expect(await f.store.commitOperatorHit("a",f.runId,hit())).toEqual(first);
 const read=await f.store.read("a",f.runId);expect(read.actorState?.operators[0]?.charge).toBe(4);
 expect(await f.store.lookup("a",f.runId,"hit")).toEqual(first);
});
it.each(["SKILL","SUMMON"])("does not grant basic charge for %s damage",async sourceKind=>{
 const f=await fixture();expect(await f.store.commitOperatorHit("a",f.runId,{...hit(),sourceKind})).toMatchObject({hpLoss:1,actorCharge:{charge:0,gained:0}});
});
it("keeps committed dead-source damage but grants no charge",async()=>{
 const f=await fixture(0);expect(await f.store.commitOperatorHit("a",f.runId,hit())).toMatchObject({hpLoss:1,actorCharge:{charge:0}});
});
it("allows a later real HP-loss hit after a zero-loss hit in the same action",async()=>{
 const f=await fixture(),zero=hit();zero.damage.base.numerator="0";
 expect(await f.store.commitOperatorHit("a",f.runId,zero)).toMatchObject({hpLoss:0,actorCharge:{charge:0}});
 expect(await f.store.commitOperatorHit("a",f.runId,hit("real",2,1))).toMatchObject({actorCharge:{charge:4,gained:4}});
});
it("rolls back HP and charge together when receipt persistence fails",async()=>{
 const f=await fixture(),before=await f.store.read("a",f.runId);
 f.sql.run("CREATE TRIGGER fail_actor_receipt BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commitOperatorHit("a",f.runId,hit())).rejects.toThrow();
 expect(await f.store.read("a",f.runId)).toEqual(before);
});
it("rejects unknown sources and allied targets without writes",async()=>{
 const f=await fixture();await expect(f.store.commitOperatorHit("a",f.runId,{...hit(),sourceActorId:"partner"})).rejects.toThrow("combat_damage_actor_invalid");
 await expect(f.store.commitOperatorHit("a",f.runId,{...hit(),targetId:"eve"})).rejects.toThrow("combat_damage_enemy_target_required");
 expect((await f.store.read("a",f.runId)).revision).toBe(1);
});
it("recovers lost responses and replays after termination without resetting charge",async()=>{
 const f=await fixture(),start=f.scalar("SELECT snapshot_json FROM run_snapshots"),base=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const r=await base(statements);if(lose){lose=false;throw new Error("response lost");}return r;};
 const first=await f.store.commitOperatorHit("a",f.runId,hit());
 await f.store.initialize("a",f.runId,[{id:"eve",hp:10,maxHp:10},{id:"enemy",hp:100,maxHp:100}],{operators:[{actorId:"eve",expanded:true}],enemyTargetIds:["enemy"]});
 expect((await f.store.read("a",f.runId)).actorState?.operators[0]?.charge).toBe(4);
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect(await f.store.commitOperatorHit("a",f.runId,hit())).toEqual(first);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(start);
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(1);
});
it("fences actor-state-only races in the same transaction",async()=>{
 const f=await fixture();
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_damage_states SET actor_state_json=replace(actor_state_json,'200','100')");};
 await expect(f.store.commitOperatorHit("a",f.runId,hit())).rejects.toThrow("combat_damage_state_conflict");
 const read=await f.store.read("a",f.runId);expect(read.revision).toBe(1);expect(read.actorState?.operators[0]?.capacity).toBe(100);
 expect(read.state.targets.find(t=>t.id==="enemy")?.hp).toBe(100);expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
});
it("preserves legacy checkpoints without adding actor state",async()=>{
 const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json(),store=new CombatDamageStore(f.env.DB);
 const targets=[{id:"eve",hp:10,maxHp:10},{id:"enemy",hp:100,maxHp:100}];await store.initialize("a",r.runId,targets);
 const before=await store.read("a",r.runId);expect(before).not.toHaveProperty("actorState");
 await expect(store.commitOperatorHit("a",r.runId,hit())).rejects.toThrow("combat_damage_actor_state_missing");
 await expect(store.initialize("a",r.runId,targets,{operators:[{actorId:"eve",expanded:false}],enemyTargetIds:["enemy"]})).rejects.toThrow("combat_damage_initialization_conflict");
 expect(await store.read("a",r.runId)).toEqual(before);
});
