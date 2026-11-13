import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatDamageStore} from "../../apps/game-api/src/combat-damage-store";
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();const store=new CombatDamageStore(f.env.DB);await store.initialize("a",r.runId,[{id:"target",hp:10,maxHp:10}]);return {...f,store,runId:r.runId};}
const input=(eventId="hit",expectedRevision=1)=>({eventId,expectedRevision,targetId:"target",damage:{base:{numerator:"19",denominator:"10"},effects:[],defenseBps:1000,incomingReductionBps:0}});
it("calculates once, floors after defense and stores calculation evidence with HP",async()=>{
 const f=await fixture(),r=await f.store.commitUnshieldedDirectDamage("a",f.runId,input());
 expect(r).toMatchObject({version:"combat.calculated-damage-receipt.v1",hpBefore:10,hpLoss:1,hpAfter:9,calculation:{exact:{numerator:"171",denominator:"100"},ordinaryBeforeShield:"1"}});
 expect(await f.store.lookup("a",f.runId,"hit")).toEqual(r);
 expect((await f.store.read("a",f.runId)).state.ledger[0]).toMatchObject({hpLoss:1,hpAfter:9});
});
it("caps arbitrarily large exact damage at current HP without unsafe Number conversion",async()=>{
 const f=await fixture(),c=input();c.damage.base={numerator:"900719925474099300000",denominator:"1"};c.damage.defenseBps=0;
 expect(await f.store.commitUnshieldedDirectDamage("a",f.runId,c)).toMatchObject({hpLoss:10,hpAfter:0});
});
it("records sub-unit zero loss and preserves the original result after subsequent hits",async()=>{
 const f=await fixture(),c=input();c.damage.base={numerator:"3",denominator:"5"};const first=await f.store.commitUnshieldedDirectDamage("a",f.runId,c);
 expect(first).toMatchObject({hpLoss:0,hpAfter:10});await f.store.commitUnshieldedDirectDamage("a",f.runId,input("later",2));
 expect(await f.store.commitUnshieldedDirectDamage("a",f.runId,c)).toEqual(first);
 expect((await f.store.read("a",f.runId)).revision).toBe(3);
});
it("rejects changed calculation under the same key even when both floor to the same HP loss",async()=>{
 const f=await fixture(),c=input();await f.store.commitUnshieldedDirectDamage("a",f.runId,c);c.damage.base.numerator="18";
 await expect(f.store.commitUnshieldedDirectDamage("a",f.runId,c)).rejects.toThrow("combat_damage_event_conflict");
});
it("rolls back calculated damage when the receipt fails and rejects malformed effects",async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_calculated_receipt BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commitUnshieldedDirectDamage("a",f.runId,input())).rejects.toThrow();
 expect((await f.store.read("a",f.runId)).state.targets[0]?.hp).toBe(10);
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(0);
 const c=input();await expect(f.store.commitUnshieldedDirectDamage("a",f.runId,{...c,damage:{...c.damage,effects:[{sourceId:"x",group:"UNKNOWN",valueBps:10}]}})).rejects.toThrow();
});
it("applies a repeated source once all the way through persisted HP",async()=>{
 const f=await fixture(),c=input(),effect={sourceId:"owned-gear-instance",group:"EQUIPMENT_DIRECT",valueBps:5000};
 const r=await f.store.commitUnshieldedDirectDamage("a",f.runId,{...c,damage:{...c.damage,base:{numerator:"4",denominator:"1"},effects:[effect,effect],defenseBps:0}});
 expect(r).toMatchObject({hpLoss:6,hpAfter:4});
 expect((await f.store.read("a",f.runId)).state.targets[0]?.hp).toBe(4);
});
it("recovers the calculated original after a committed response is lost",async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("response lost");}return r;};
 const r=await f.store.commitUnshieldedDirectDamage("a",f.runId,input());
 expect(r).toMatchObject({hpLoss:1,calculation:{ordinaryBeforeShield:"1"}});
 expect(await f.store.lookup("a",f.runId,"hit")).toEqual(r);
 expect((await f.store.read("a",f.runId)).revision).toBe(2);
 expect(f.scalar("SELECT count(*) FROM combat_damage_receipts")).toBe(1);
});
