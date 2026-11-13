import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatDamageStore} from "../../apps/game-api/src/combat-damage-store";
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json(),store=new CombatDamageStore(f.env.DB);await store.initialize("a",r.runId,[{id:"eve",hp:10,maxHp:10},{id:"enemy",hp:100,maxHp:100}],{operators:[{actorId:"eve",expanded:true}],enemyTargetIds:["enemy"]});return {...f,store,runId:r.runId};}
const hit=(eventId="numeric",expectedRevision=1)=>({eventId,expectedRevision,targetId:"enemy",sourceActorId:"eve",actionId:"basic",hitOrdinal:0,sourceKind:"BASIC",damage:{version:"combat.numeric-defense-budget.v1",base:{numerator:"101",denominator:"1"},effects:[],defense:{numerator:"1000",denominator:"1"},weakening:[],penetration:[],reductions:{equipment:[],guardStance:[],independent:[]}}});
it("atomically persists exact numeric DEF calculation, one final floor, HP and own basic charge",async()=>{
 const f=await fixture(),r=await f.store.commitNumericOperatorHit("a",f.runId,hit());
 expect(r).toMatchObject({version:"combat.operator-hit-receipt.v2",hpBefore:100,hpLoss:50,hpAfter:50,calculation:{version:"combat.numeric-defense-budget.v1",exact:{numerator:"101",denominator:"2"},ordinaryBeforeShield:"50"},actorCharge:{gained:4,charge:4}});
 expect(await f.store.lookup("a",f.runId,"numeric")).toEqual(r);
 expect(await f.store.commitNumericOperatorHit("a",f.runId,{...hit("second",2),hitOrdinal:1})).toMatchObject({hpAfter:0,actorCharge:{gained:0,charge:4}});
});
it("preserves a zero-loss receipt and allows later first real HP hit to earn charge",async()=>{
 const f=await fixture(),c=hit();c.damage.base.numerator="1";
 expect(await f.store.commitNumericOperatorHit("a",f.runId,c)).toMatchObject({hpLoss:0,actorCharge:{gained:0,charge:0}});
 expect(await f.store.commitNumericOperatorHit("a",f.runId,{...hit("real",2),hitOrdinal:1})).toMatchObject({actorCharge:{gained:4,charge:4}});
});
it("rolls back HP, charge and receipts when persistence fails",async()=>{
 const f=await fixture(),before=await f.store.read("a",f.runId);f.sql.run("CREATE TRIGGER fail_numeric BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commitNumericOperatorHit("a",f.runId,hit())).rejects.toThrow();expect(await f.store.read("a",f.runId)).toEqual(before);expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
});
it("replays original numeric receipts after lost response, terminal state and does not alter START",async()=>{
 const f=await fixture(),before=f.scalar("SELECT snapshot_json FROM run_snapshots"),batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const result=await batch(statements);if(lose){lose=false;throw new Error("lost");}return result;};
 const result=await f.store.commitNumericOperatorHit("a",f.runId,hit());f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await f.store.commitNumericOperatorHit("a",f.runId,hit())).toEqual(result);expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(before);expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(1);
});
it("keeps legacy and numeric inputs/receipts explicit and rejects event-content changes",async()=>{
 const f=await fixture(),legacy={eventId:"old",expectedRevision:1,targetId:"enemy",damage:{base:{numerator:"1",denominator:"1"},effects:[],defenseBps:0,incomingReductionBps:0}};
 const old=await f.store.commitUnshieldedDirectDamage("a",f.runId,legacy);expect(old.version).toBe("combat.calculated-damage-receipt.v1");
 const c=hit("new",2);const direct={eventId:c.eventId,expectedRevision:c.expectedRevision,targetId:c.targetId,damage:c.damage};
 expect(await f.store.commitNumericUnshieldedDirectDamage("a",f.runId,direct)).toMatchObject({version:"combat.calculated-damage-receipt.v2",hpLoss:50});
 await expect(f.store.commitNumericUnshieldedDirectDamage("a",f.runId,legacy)).rejects.toThrow();await expect(f.store.commitUnshieldedDirectDamage("a",f.runId,direct)).rejects.toThrow();
 expect(await f.store.commitUnshieldedDirectDamage("a",f.runId,legacy)).toEqual(old);
 await expect(f.store.commitNumericUnshieldedDirectDamage("a",f.runId,{...direct,damage:{...direct.damage,defense:{numerator:"2000",denominator:"1"}}})).rejects.toThrow("combat_damage_event_conflict");
});
it("fences a terminal race after numeric calculation",async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 await expect(f.store.commitNumericOperatorHit("a",f.runId,hit())).rejects.toThrow("combat_damage_state_conflict");expect((await f.store.read("a",f.runId)).revision).toBe(1);expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
});
