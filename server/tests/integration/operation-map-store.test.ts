import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {plan} from "../helpers/operation-map-fixture";
import {OperationMapStore,type OperationMapVerifier} from "../../apps/game-api/src/operation-map-store";
import {EquipmentRewardBundleStore} from "../../apps/game-api/src/equipment-reward-bundles";
// Explicit synthetic adapter: validates persistence semantics, not actual arrival/gameplay.
function syntheticVerifier(db:D1Database,verify:(ctx:Parameters<OperationMapVerifier["prepare"]>[0])=>Promise<unknown>):OperationMapVerifier {
 return {prepare:async ctx=>({proof:await verify(ctx),assertions:[db.prepare("INSERT INTO operation_map_write_guard(valid) VALUES(1)")]})};
}
async function fixture(pin=true){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 if(pin){const s=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));s.encounterType="ORDINARY_12_NODE";s.admission.operationMapPlan=plan();s.departureResponse.encounterType=s.encounterType;s.departureResponse.admission.operationMapPlan=plan();f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(s)]);}
 const store=new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async ({command})=>({kind:command.kind,verificationId:"test-proof",holdIds:command.kind==="COMPLETE"?["reward"]:[]})));return {...f,store,runId:r.runId};}
const confirm={eventId:"choose",expectedRevision:1,kind:"CONFIRM",edgeId:"entry:n1-0"};
const arrive={eventId:"arrive",expectedRevision:2,kind:"ARRIVE",edgeId:"entry:n1-0",evidenceRef:"arrival-proof"};
it('records destination server time atomically without granting arrival or refreshing on replay',async()=>{
 const f=await fixture();await f.store.initialize('a',f.runId);
 await f.store.commit('a',f.runId,confirm);
 const read=()=>f.env.DB.prepare('SELECT * FROM operation_map_destination_times WHERE run_id=? AND event_id=?').bind(f.runId,confirm.eventId).first();
 const first=await read();expect(first).toMatchObject({run_id:f.runId,event_id:'choose',confirmed_at_ms:expect.any(Number)});
 expect(Number(first!.confirmed_at_ms)).toBeGreaterThan(0);
 await f.store.commit('a',f.runId,confirm);expect(await read()).toEqual(first);
 expect((await f.store.read('a',f.runId)).state.phase).toBe('TRAVELLING');
 await expect(f.store.commit('a',f.runId,{...confirm,eventId:'invalid',expectedRevision:2})).rejects.toThrow();
 expect(f.scalar('SELECT COUNT(*) FROM operation_map_destination_times')).toBe(1);
 expect(()=>f.sql.run('UPDATE operation_map_destination_times SET confirmed_at_ms=0')).toThrow('operation_map_destination_time_immutable');
 const broken=await fixture();await broken.store.initialize('a',broken.runId);
 broken.sql.run("CREATE TRIGGER test_time_write_failure BEFORE INSERT ON operation_map_destination_times BEGIN SELECT RAISE(ABORT,'test_time_write_failure'); END;");
 await expect(broken.store.commit('a',broken.runId,confirm)).rejects.toThrow('test_time_write_failure');
 expect((await broken.store.read('a',broken.runId)).revision).toBe(1);
 expect(await broken.store.lookup('a',broken.runId,'choose')).toBeNull();
 expect(broken.scalar('SELECT COUNT(*) FROM operation_map_destination_times')).toBe(0);
});
it("pins the original map, reveals all events and does not teleport on confirmation",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);const before=f.scalar("SELECT snapshot_json FROM run_snapshots");
 const r=await f.store.commit("a",f.runId,confirm);expect(r.state).toMatchObject({phase:"TRAVELLING",pendingEdgeId:"entry:n1-0",traversedEdgeIds:[]});
 const read=await f.store.initialize("a",f.runId);expect(read.revision).toBe(2);expect(read.view.currentNodeId).toBe("entry");expect(read.view.nodes).toHaveLength(34);expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(before);
 await expect(f.store.commit("a",f.runId,{...confirm,eventId:"again",expectedRevision:2})).rejects.toThrow("operation_map_phase_invalid");});
it("rejects old starts, foreign owners and distant edges",async()=>{const old=await fixture(false);await expect(old.store.initialize("a",old.runId)).rejects.toThrow("operation_map_profile_unsupported");
 const f=await fixture();await f.store.initialize("a",f.runId);await expect(f.store.read("b",f.runId)).rejects.toThrow("operation_map_run_not_found");await expect(f.store.commit("a",f.runId,{...confirm,edgeId:"n5-0:n6-0"})).rejects.toThrow("operation_map_edge_ineligible");});
it("fails closed without arrival verification and checks matching proof kind",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 await expect(new OperationMapStore(f.env.DB).commit("a",f.runId,arrive)).rejects.toThrow("operation_map_verification_required");
 await expect(new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async()=>({kind:"COMPLETE",verificationId:"bad",holdIds:[]}))).commit("a",f.runId,arrive)).rejects.toThrow("operation_map_verification_invalid");expect((await f.store.read("a",f.runId)).revision).toBe(2);});
it("separates verified arrival, node completion and original transaction holds",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);await f.store.commit("a",f.runId,arrive);
 expect((await f.store.read("a",f.runId)).view.currentNodeId).toBe("n1-0");
 await f.store.commit("a",f.runId,{kind:"COMPLETE",nodeId:"n1-0",eventId:"complete",expectedRevision:3,evidenceRef:"objective-proof"});
 const next={kind:"CONFIRM",edgeId:"n1-0:n2-0",eventId:"next",expectedRevision:4};await expect(f.store.commit("a",f.runId,next)).rejects.toThrow("operation_map_hold_active");
 await f.store.commit("a",f.runId,{kind:"RELEASE_HOLD",holdId:"reward",eventId:"release",expectedRevision:4,evidenceRef:"transaction-proof"});await f.store.commit("a",f.runId,{...next,expectedRevision:5});expect(f.scalar("SELECT status FROM runs")).toBe("ACTIVE");expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);});
it("replays original receipts after termination and rejects conflicting reuse",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);const r=await f.store.commit("a",f.runId,confirm);f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await f.store.commit("a",f.runId,confirm)).toEqual(r);expect(await f.store.lookup("a",f.runId,"choose")).toEqual(r);await expect(f.store.commit("a",f.runId,{...confirm,edgeId:"entry:n1-1"})).rejects.toThrow("operation_map_event_conflict");});
it("rolls back progress when receipt insertion fails and fences concurrent revision",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);f.sql.run("CREATE TRIGGER fail_map BEFORE INSERT ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");await expect(f.store.commit("a",f.runId,confirm)).rejects.toThrow("injected");expect((await f.store.read("a",f.runId)).revision).toBe(1);f.sql.run("DROP TRIGGER fail_map");f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE operation_map_states SET revision=2");};await expect(f.store.commit("a",f.runId,confirm)).rejects.toThrow("operation_map_state_conflict");expect(f.scalar("SELECT count(*) FROM operation_map_receipts")).toBe(0);});
it("recovers a lost acknowledgement and keeps the plan immutable",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);const batch=f.env.DB.batch.bind(f.env.DB);let lose=true;f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};const r=await f.store.commit("a",f.runId,confirm);expect(await f.store.lookup("a",f.runId,"choose")).toEqual(r);expect(()=>f.sql.run("UPDATE operation_map_states SET plan_json='{}'")).toThrow();});
it("fences terminal and snapshot races",async()=>{for(const mutation of ["UPDATE runs SET status='COMPLETED'","UPDATE run_snapshots SET snapshot_json='{}'"]){const f=await fixture();await f.store.initialize("a",f.runId);f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run(mutation);};await expect(f.store.commit("a",f.runId,confirm)).rejects.toThrow("operation_map_state_conflict");expect((await f.store.read("a",f.runId)).revision).toBe(1);}});

it("finishes exactly twelve visits without finishing the run or awarding rewards",async()=>{const f=await fixture();const store=new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async({command})=>({kind:command.kind,verificationId:"proof",holdIds:[]})));await store.initialize("a",f.runId);let revision=1,from="entry";
 for(let depth=1;depth<=12;depth++){const to=depth===12?"boss":`n${depth}-0`,edgeId=`${from}:${to}`;await store.commit("a",f.runId,{kind:"CONFIRM",edgeId,eventId:`c${depth}`,expectedRevision:revision++});await store.commit("a",f.runId,{kind:"ARRIVE",edgeId,eventId:`a${depth}`,expectedRevision:revision++,evidenceRef:"arrival"});await store.commit("a",f.runId,{kind:"COMPLETE",nodeId:to,eventId:`f${depth}`,expectedRevision:revision++,evidenceRef:"objective"});from=to;}
 const result=await store.read("a",f.runId);expect(result.state.phase).toBe("FINAL_NODE_COMPLETE");expect(result.state.traversedEdgeIds).toHaveLength(12);await expect(store.commit("a",f.runId,{...confirm,eventId:"thirteenth",expectedRevision:revision})).rejects.toThrow("operation_map_phase_invalid");expect(f.scalar("SELECT status FROM runs")).toBe("ACTIVE");expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("fences changes during verification and isolates adapter input mutation",async()=>{const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 const store=new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async ctx=>{ctx.state.traversedEdgeIds.push("invented");ctx.command.eventId="changed";ctx.plan.edges.length=0;return {kind:"ARRIVE",verificationId:"proof",holdIds:[]};}));expect((await store.commit("a",f.runId,arrive)).state.traversedEdgeIds).toEqual(["entry:n1-0"]);
 const racing=new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async()=>{f.sql.run("UPDATE operation_map_states SET revision=4");return {kind:"COMPLETE",verificationId:"proof",holdIds:[]};}));await expect(racing.commit("a",f.runId,{kind:"COMPLETE",nodeId:"n1-0",eventId:"race",expectedRevision:3,evidenceRef:"objective"})).rejects.toThrow("operation_map_state_conflict");expect(await store.lookup("a",f.runId,"race")).toBeNull();
});
it("rejects missing plans and unsupported modes without creating progress",async()=>{for(const variant of ["missing","coop"]){const f=await fixture(),s=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));if(variant==="missing")delete s.admission.operationMapPlan;else s.admission.combatAuthority.mode="ORDINARY_COOP";f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(s)]);await expect(f.store.initialize("a",f.runId)).rejects.toThrow();expect(f.scalar("SELECT count(*) FROM operation_map_states")).toBe(0);}});

it("rejects an original evidence change between verification and map publication",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 f.sql.run("CREATE TABLE map_test_evidence (revision INTEGER NOT NULL)");f.sql.run("INSERT INTO map_test_evidence VALUES(1)");
 const proof={kind:"ARRIVE",verificationId:"original-evidence",holdIds:[]};
 // Prepared verification must fence the exact observed source at publication.
 const store=new OperationMapStore(f.env.DB,{prepare:async()=>({proof,assertions:[f.env.DB.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN revision=1 THEN 1 ELSE 0 END FROM map_test_evidence")]})});
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE map_test_evidence SET revision=2");};
 await expect(store.commit("a",f.runId,arrive)).rejects.toThrow("operation_map_state_conflict");
 expect((await f.store.read("a",f.runId)).revision).toBe(2);expect(await f.store.lookup("a",f.runId,"arrive")).toBeNull();
});

it("rejects empty prepared guards without publishing progress",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 const store=new OperationMapStore(f.env.DB,{prepare:async()=>({proof:{kind:"ARRIVE",verificationId:"unchecked",holdIds:[]},assertions:[]})});
 await expect(store.commit("a",f.runId,arrive)).rejects.toThrow("operation_map_verification_invalid");
 expect((await f.store.read("a",f.runId)).revision).toBe(2);expect(await store.lookup("a",f.runId,"arrive")).toBeNull();
});
it("rolls back source assertions with a failed receipt and replays without re-verification",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 let verified=0;const store=new OperationMapStore(f.env.DB,syntheticVerifier(f.env.DB,async()=>{verified++;return {kind:"ARRIVE",verificationId:"proof",holdIds:[]};}));
 f.sql.run("CREATE TRIGGER fail_map_proof BEFORE INSERT ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'injected-proof'); END");
 await expect(store.commit("a",f.runId,arrive)).rejects.toThrow("injected-proof");
 expect((await store.read("a",f.runId)).revision).toBe(2);expect(f.scalar("SELECT count(*) FROM operation_map_write_guard")).toBe(0);
 f.sql.run("DROP TRIGGER fail_map_proof");
 const original=await store.commit("a",f.runId,arrive);expect(verified).toBe(2);
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect(await new OperationMapStore(f.env.DB).commit("a",f.runId,arrive)).toEqual(original);
 expect(verified).toBe(2);expect(f.scalar("SELECT count(*) FROM operation_map_write_guard")).toBe(0);
});
it("keeps a transaction hold when its verified source disappears before release",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);await f.store.commit("a",f.runId,arrive);
 await f.store.commit("a",f.runId,{kind:"COMPLETE",nodeId:"n1-0",eventId:"complete",expectedRevision:3,evidenceRef:"objective-proof"});
 f.sql.run("CREATE TABLE map_test_transaction (id TEXT PRIMARY KEY, settled INTEGER NOT NULL)");f.sql.run("INSERT INTO map_test_transaction VALUES('original',1)");
 const store=new OperationMapStore(f.env.DB,{prepare:async()=>({proof:{kind:"RELEASE_HOLD",verificationId:"original-transaction",holdIds:[]},assertions:[f.env.DB.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM map_test_transaction WHERE id='original' AND settled=1) THEN 1 ELSE 0 END")]})});
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("DELETE FROM map_test_transaction");};
 await expect(store.commit("a",f.runId,{kind:"RELEASE_HOLD",holdId:"reward",eventId:"release",expectedRevision:4,evidenceRef:"transaction-proof"})).rejects.toThrow("operation_map_state_conflict");
 expect((await store.read("a",f.runId)).state.holdIds).toEqual(["reward"]);expect(await store.lookup("a",f.runId,"release")).toBeNull();
});

it("blocks the next destination while current-node equipment reward items remain pending",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 await f.store.commit("a",f.runId,arrive);
 await f.store.commit("a",f.runId,{kind:"COMPLETE",nodeId:"n1-0",eventId:"complete",expectedRevision:3,evidenceRef:"objective"});
 const item={itemId:"node-reward-1",instanceId:"reserved-1",definitionId:"gear-a",definitionVersion:"fixture-v1",categoryId:"weapon",setTag:"SET01" as const,star:1};
 const reward=new EquipmentRewardBundleStore(f.env.DB,{prepare:async()=>({sourceVersion:"fixture-v1",items:[item],
  assertions:[f.env.DB.prepare("INSERT INTO equipment_reward_write_guard(valid) VALUES(1)")]})},
  {prepare:async()=>({assertions:[f.env.DB.prepare("INSERT INTO equipment_reward_write_guard(valid) VALUES(1)")]})});
 const bundle=await reward.publishBase("a",f.runId,"n1-0","original-node-result");
 await f.store.commit("a",f.runId,{kind:"RELEASE_HOLD",holdId:"reward",eventId:"release",expectedRevision:4,evidenceRef:"synthetic"});
 const next={kind:"CONFIRM",edgeId:"n1-0:n2-0",eventId:"next",expectedRevision:5};
 await expect(f.store.commit("a",f.runId,next)).rejects.toThrow("operation_map_reward_pending");
 expect((await f.store.read("a",f.runId)).revision).toBe(5);
 await reward.declineRemaining("a",f.runId,{commandId:"decline",bundleId:bundle.bundleId,itemIds:[item.itemId],expectedInventoryRevision:1});
 expect((await f.store.commit("a",f.runId,next)).state.phase).toBe("TRAVELLING");
});

it("fences a new pending node reward appearing between destination check and commit",async()=>{
 const f=await fixture();await f.store.initialize("a",f.runId);await f.store.commit("a",f.runId,confirm);
 await f.store.commit("a",f.runId,arrive);
 await f.store.commit("a",f.runId,{kind:"COMPLETE",nodeId:"n1-0",eventId:"complete",expectedRevision:3,evidenceRef:"objective"});
 await f.store.commit("a",f.runId,{kind:"RELEASE_HOLD",holdId:"reward",eventId:"release",expectedRevision:4,evidenceRef:"synthetic"});
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;
  f.sql.run("INSERT INTO run_equipment_reward_bundles VALUES('race-bundle',?,'n1-0','source','fixture-v1',?)",[f.runId,"0".repeat(64)]);
  f.sql.run("INSERT INTO run_equipment_reward_items VALUES('race-item',?,'race-bundle','race-instance',0,'PENDING','{}')",[f.runId]);};
 await expect(f.store.commit("a",f.runId,{kind:"CONFIRM",edgeId:"n1-0:n2-0",eventId:"race-next",expectedRevision:5}))
  .rejects.toThrow("operation_map_state_conflict");
 expect((await f.store.read("a",f.runId)).revision).toBe(5);
 expect(await f.store.lookup("a",f.runId,"race-next")).toBeNull();
});
