import {guardedRestoreFixture} from "../helpers/combat-restore-fixture";
import {setCombatSqlClock} from "../helpers/combat-sql-clock";
import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatHostReportStore} from "../../apps/game-api/src/combat-host-report-store";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
async function fixture(coop=true){
 const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 // Test-only future coop START fixture; public coop admission remains unsupported.
 const snapshot=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
 if(coop){snapshot.admission.combatAuthority=captureCombatAuthority("ORDINARY_COOP");snapshot.departureResponse.admission.combatAuthority=snapshot.admission.combatAuthority;f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(snapshot)]);}
 const context={authority:captureCombatAuthority("ORDINARY_COOP"),runId:r.runId,participants:[{accountId:"a",role:"TRAIN"},{accountId:"b",role:"OPERATOR"}],activeAccountIds:["a","b"],hostAccountId:"a",hostGeneration:1,definitionVersion:snapshot.admission.combatPolicyRequirements.version,snapshotRevision:1,lastStateSequence:0,logicalTimeMs:0};
 const store=new CombatHostReportStore(f.env.DB);return {...f,store,context,runId:r.runId};
}
const command=(f:Awaited<ReturnType<typeof fixture>>)=>({requestId:"report-1",expectedRevision:1,report:{version:"combat.host-report.v1",runId:f.runId,hostGeneration:1,definitionVersion:f.context.definitionVersion,baseSnapshotRevision:1,stateSequence:1,logicalTimeMs:100}});
it("atomically stores report cursor and original receipt; does not reset on initialize",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const start=f.scalar("SELECT snapshot_json FROM run_snapshots"),c=command(f);
 const result=await f.store.commit("a",f.runId,c);expect(result).toMatchObject({revision:2,combatVerification:"NOT_PERFORMED"});
 expect(await f.store.commit("a",f.runId,c)).toEqual(result);await f.store.initialize(f.context);
 expect((await f.store.read("b",f.runId)).context.lastStateSequence).toBe(1);expect(f.scalar("SELECT count(*) FROM combat_host_report_receipts")).toBe(1);
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(start);
});
it("rejects solo initialization without reinterpreting its START",async()=>{const f=await fixture(false);await expect(f.store.initialize(f.context)).rejects.toThrow("combat_host_start_mismatch");});
it("rejects foreign read, peer report and conflicting retry",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const c=command(f);
 await expect(f.store.read("stranger",f.runId)).rejects.toThrow("combat_host_access_forbidden");
 await expect(f.store.commit("b",f.runId,c)).rejects.toThrow("combat_host_sender_forbidden");await f.store.commit("a",f.runId,c);
 await expect(f.store.commit("a",f.runId,{...c,report:{...c.report,logicalTimeMs:101}})).rejects.toThrow("combat_host_request_conflict");
});
it("rolls back cursor when receipt insertion fails",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const before=await f.store.read("a",f.runId);
 f.sql.run("CREATE TRIGGER fail_host_receipt BEFORE INSERT ON combat_host_report_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.commit("a",f.runId,command(f))).rejects.toThrow();expect(await f.store.read("a",f.runId)).toEqual(before);
});
it("recovers response loss after commit",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};
 expect(await f.store.commit("a",f.runId,command(f))).toMatchObject({revision:2});expect(f.scalar("SELECT count(*) FROM combat_host_report_receipts")).toBe(1);
});
it("fences concurrent context mutation",async()=>{
 const f=await fixture();await f.store.initialize(f.context);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_host_report_states SET revision=2");};
 await expect(f.store.commit("a",f.runId,command(f))).rejects.toThrow("combat_host_state_conflict");expect((await f.store.read("a",f.runId)).context.lastStateSequence).toBe(0);
});
it("fences termination but replays an earlier original receipt",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const c=command(f),first=await f.store.commit("a",f.runId,c);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 await expect(f.store.commit("a",f.runId,{...c,requestId:"next",expectedRevision:2,report:{...c.report,stateSequence:2}})).rejects.toThrow("combat_host_state_conflict");
 expect(await f.store.commit("a",f.runId,c)).toEqual(first);
});
it("queries the sender original result without advancing and retains the report evidence",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const c=command(f),r=await f.store.commit("a",f.runId,c);
 expect(await f.store.lookup("a",f.runId,c.requestId)).toEqual(r);expect(await f.store.lookup("b",f.runId,c.requestId)).toBeNull();
 expect(JSON.parse(String(f.scalar("SELECT report_json FROM combat_host_report_receipts")))).toEqual(c.report);
 expect((await f.store.read("a",f.runId)).revision).toBe(2);
});
it("rejects a changed START and never writes a new receipt",async()=>{
 const f=await fixture();await f.store.initialize(f.context);f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.changed',1)");
 await expect(f.store.commit("a",f.runId,command(f))).rejects.toThrow("combat_host_start_mismatch");expect(f.scalar("SELECT count(*) FROM combat_host_report_receipts")).toBe(0);
});
it("pauses new reports on server-observed disconnect without resetting the first recovery deadline",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const c=command(f),first=await f.store.commit("a",f.runId,c);
 const d={requestId:"disconnect",expectedRevision:2,disconnectedAccountId:"b",observedAt:1000};
 expect(await f.store.recordDisconnect(f.runId,d)).toMatchObject({revision:3,recovery:{startedAt:1000,deadlineAt:61000,disconnectedAccountIds:["b"]}});
 expect(await f.store.commit("a",f.runId,c)).toEqual(first);
 await expect(f.store.commit("a",f.runId,{...c,requestId:"later",expectedRevision:3,report:{...c.report,stateSequence:2}})).rejects.toThrow("combat_host_recovery_required");
 await f.store.recordDisconnect(f.runId,{...d,requestId:"both",expectedRevision:3,disconnectedAccountId:"a",observedAt:70000});
 expect((await f.store.read("a",f.runId)).recovery).toMatchObject({deadlineAt:61000,disconnectedAccountIds:["a","b"]});
 expect(await f.store.recordDisconnect(f.runId,d)).toMatchObject({revision:3});
});
it("rejects fabricated participant disconnect and rolls back failed recovery receipt",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const before=await f.store.read("a",f.runId);
 const d={requestId:"disconnect",expectedRevision:1,disconnectedAccountId:"stranger",observedAt:1000};
 await expect(f.store.recordDisconnect(f.runId,d)).rejects.toThrow("combat_host_participant_invalid");
 f.sql.run("CREATE TRIGGER fail_recovery_receipt BEFORE INSERT ON combat_host_recovery_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.recordDisconnect(f.runId,{...d,disconnectedAccountId:"b"})).rejects.toThrow();
 expect(await f.store.read("a",f.runId)).toEqual(before);
});
it("rejects a report racing a disconnect even if only recovery state changes",async()=>{
 const f=await fixture();await f.store.initialize(f.context);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_host_report_states SET recovery_json=?",[JSON.stringify({startedAt:1000,deadlineAt:61000,disconnectedAccountIds:["b"]})]);};
 await expect(f.store.commit("a",f.runId,command(f))).rejects.toThrow("combat_host_state_conflict");
 expect(f.scalar("SELECT count(*) FROM combat_host_report_receipts")).toBe(0);
});
it("recovers lost disconnect acknowledgement and replays original after termination",async()=>{
 const f=await fixture();await f.store.initialize(f.context);const batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};
 const d={requestId:"disconnect",expectedRevision:1,disconnectedAccountId:"a",observedAt:1000},first=await f.store.recordDisconnect(f.runId,d);
 expect(first).toMatchObject({revision:2,recovery:{deadlineAt:61000}});
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await f.store.recordDisconnect(f.runId,d)).toEqual(first);
 await expect(f.store.recordDisconnect(f.runId,{...d,observedAt:2000})).rejects.toThrow("combat_host_request_conflict");
 expect(f.scalar("SELECT count(*) FROM combat_host_recovery_receipts")).toBe(1);
});
it("fences a disconnect racing run termination",async()=>{
 const f=await fixture();await f.store.initialize(f.context);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE runs SET status='COMPLETED'");};
 await expect(f.store.recordDisconnect(f.runId,{requestId:"disconnect",expectedRevision:1,disconnectedAccountId:"b",observedAt:1000})).rejects.toThrow("combat_host_state_conflict");
 expect(await f.store.read("a",f.runId)).not.toHaveProperty("recovery");expect(f.scalar("SELECT count(*) FROM combat_host_recovery_receipts")).toBe(0);
});
async function resuming(disconnected="a",verifiedAt=2000){
 const f=await fixture();await f.store.initialize(f.context);
 await f.store.recordDisconnect(f.runId,{requestId:"down",expectedRevision:1,disconnectedAccountId:disconnected,observedAt:1000});
 setCombatSqlClock(f.sql,()=>verifiedAt);const store=new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async input=>({checkpointId:"cp",transactionCursor:"cursor",verifiedAt,restoredAccountIds:JSON.parse(input.nextContextJson).activeAccountIds})}));
 return {...f,resumeStore:store};
}
const resume=(mode="SOLO")=>({requestId:"resume",expectedRevision:2,mode});
it("requires real restoration integration by default and performs no transition",async()=>{
 const f=await resuming();await expect(f.store.resolveRecovery("b",f.runId,resume())).rejects.toThrow("combat_host_restore_unavailable");
 expect((await f.store.read("b",f.runId)).revision).toBe(2);
});
it("atomically selects survivor host, increments generation and preserves original state",async()=>{
 const f=await resuming(),start=f.scalar("SELECT snapshot_json FROM run_snapshots");
 const result=await f.resumeStore.resolveRecovery("b",f.runId,resume());expect(result).toMatchObject({revision:3,hostGeneration:2,activeAccountIds:["b"]});
 const read=await f.store.read("b",f.runId);expect(read).not.toHaveProperty("recovery");expect(read.context).toMatchObject({hostAccountId:"b",hostGeneration:2,lastStateSequence:0,logicalTimeMs:0,snapshotRevision:1,participants:f.context.participants});
 expect(await f.resumeStore.resolveRecovery("b",f.runId,resume())).toEqual(result);expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(start);
 await expect(f.resumeStore.resolveRecovery("a",f.runId,{...resume("REJOIN"),requestId:"late",expectedRevision:3})).rejects.toThrow();
});
it("lets restored coop win and rejects the stale solo command",async()=>{
 const f=await resuming();await f.resumeStore.resolveRecovery("a",f.runId,resume("REJOIN"));
 expect((await f.store.read("a",f.runId)).context.activeAccountIds).toEqual(["a","b"]);
 await expect(f.resumeStore.resolveRecovery("b",f.runId,{...resume(),requestId:"late"})).rejects.toThrow("combat_host_revision_conflict");
});
it("rejects late coop restoration without automatically switching to solo",async()=>{
 const f=await resuming("a",61000);await expect(f.resumeStore.resolveRecovery("a",f.runId,resume("REJOIN"))).rejects.toThrow("combat_host_rejoin_expired");
 expect(await f.store.read("b",f.runId)).toHaveProperty("recovery");
});
it("rejects disconnected sender solo choice and incomplete restoration evidence",async()=>{
 const f=await resuming();await expect(f.resumeStore.resolveRecovery("a",f.runId,resume())).rejects.toThrow("combat_host_survivor_invalid");
 const store=new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async()=>({checkpointId:"cp",transactionCursor:"cursor",verifiedAt:2000,restoredAccountIds:["a"]})}));
 await expect(store.resolveRecovery("b",f.runId,resume())).rejects.toThrow("combat_host_restore_incomplete");
});
it("rolls back transition when original receipt cannot be stored",async()=>{
 const f=await resuming(),before=await f.store.read("b",f.runId);
 f.sql.run("CREATE TRIGGER fail_resume BEFORE INSERT ON combat_host_resume_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.resumeStore.resolveRecovery("b",f.runId,resume())).rejects.toThrow();expect(await f.store.read("b",f.runId)).toEqual(before);
});
it("fences context changes while restoration verifier is running",async()=>{
 const f=await resuming();const store=new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async input=>{f.sql.run("UPDATE combat_host_report_states SET revision=3");return {checkpointId:"cp",transactionCursor:"cursor",verifiedAt:2000,restoredAccountIds:JSON.parse(input.nextContextJson).activeAccountIds};}}));
 await expect(store.resolveRecovery("b",f.runId,resume())).rejects.toThrow("combat_host_state_conflict");expect(await f.store.read("b",f.runId)).toHaveProperty("recovery");
});
it("recovers lost transition response and replays without rerunning restoration",async()=>{
 const f=await resuming(),batch=f.env.DB.batch.bind(f.env.DB);let lose=true;
 f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};
 const result=await f.resumeStore.resolveRecovery("b",f.runId,resume());
 expect(await f.store.resolveRecovery("b",f.runId,resume())).toEqual(result);
 expect(f.scalar("SELECT count(*) FROM combat_host_resume_receipts")).toBe(1);
});
it("commits only the winning transition when coop restore races solo continuation",async()=>{
 const f=await resuming();const racing=new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async input=>{
  await f.resumeStore.resolveRecovery("b",f.runId,resume());
  return {checkpointId:"cp",transactionCursor:"cursor",verifiedAt:2000,restoredAccountIds:JSON.parse(input.nextContextJson).activeAccountIds};
 }}));
 await expect(racing.resolveRecovery("a",f.runId,{...resume("REJOIN"),requestId:"rejoin"})).rejects.toThrow("combat_host_state_conflict");
 expect((await f.store.read("b",f.runId)).context.activeAccountIds).toEqual(["b"]);expect(f.scalar("SELECT count(*) FROM combat_host_resume_receipts")).toBe(1);
});
it("rejects old host reports after transfer and permits only the new host generation",async()=>{
 const f=await resuming();await f.resumeStore.resolveRecovery("b",f.runId,resume());const c=command(f);
 await expect(f.store.commit("a",f.runId,{...c,expectedRevision:3})).rejects.toThrow("combat_host_sender_forbidden");
 await expect(f.store.commit("b",f.runId,{...c,expectedRevision:3})).rejects.toThrow("combat_host_generation_mismatch");
 expect(await f.store.commit("b",f.runId,{...c,expectedRevision:3,report:{...c.report,hostGeneration:2}})).toMatchObject({revision:4});
});
async function soloRecovering(verifiedAt=4000){
 const f=await resuming();await f.resumeStore.resolveRecovery("b",f.runId,resume());
 await f.store.recordDisconnect(f.runId,{requestId:"solo-down",expectedRevision:3,disconnectedAccountId:"b",observedAt:3000});
 setCombatSqlClock(f.sql,()=>verifiedAt);const store=new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async input=>({checkpointId:"same-checkpoint",transactionCursor:"same-cursor",verifiedAt,restoredAccountIds:JSON.parse(input.nextContextJson).activeAccountIds})}));
 return {...f,soloStore:store};
}
it("restores only the survivor after their own disconnect and preserves the run lineage",async()=>{
 const f=await soloRecovering(),before=await f.store.read("b",f.runId);
 const result=await f.soloStore.resolveRecovery("b",f.runId,{requestId:"solo-rejoin",expectedRevision:4,mode:"REJOIN"});
 expect(result).toMatchObject({revision:5,hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]});
 const after=await f.store.read("b",f.runId);expect(after).not.toHaveProperty("recovery");
 expect(after.context.participants).toEqual(before.context.participants);expect(after.context.lastStateSequence).toBe(before.context.lastStateSequence);expect(after.context.logicalTimeMs).toBe(before.context.logicalTimeMs);
});
it("never restores the departed participant or allows a second solo conversion",async()=>{
 const f=await soloRecovering();
 await expect(f.soloStore.resolveRecovery("a",f.runId,{requestId:"departed",expectedRevision:4,mode:"REJOIN"})).rejects.toThrow("combat_host_survivor_invalid");
 await expect(f.soloStore.resolveRecovery("b",f.runId,{requestId:"again",expectedRevision:4,mode:"SOLO"})).rejects.toThrow("combat_host_already_solo");
 expect((await f.store.read("b",f.runId)).revision).toBe(4);
});
it("keeps expired solo recovery pending without inventing retention or new participants",async()=>{
 const f=await soloRecovering(63000);
 await expect(f.soloStore.resolveRecovery("b",f.runId,{requestId:"late",expectedRevision:4,mode:"REJOIN"})).rejects.toThrow("combat_host_rejoin_expired");
 expect((await f.store.read("b",f.runId)).context.activeAccountIds).toEqual(["b"]);expect(await f.store.read("b",f.runId)).toHaveProperty("recovery");
});
it("looks up original transition receipts without state changes or cross-account disclosure",async()=>{
 const f=await resuming();expect(await f.store.lookupResume("b",f.runId,"resume")).toBeNull();
 const result=await f.resumeStore.resolveRecovery("b",f.runId,resume());f.sql.run("UPDATE runs SET status='COMPLETED'");
 const before=await f.store.read("b",f.runId);
 expect(await f.store.lookupResume("b",f.runId,"resume")).toEqual(result);expect(await f.store.lookupResume("a",f.runId,"resume")).toBeNull();
 await expect(f.store.lookupResume("stranger",f.runId,"resume")).rejects.toThrow("combat_host_access_forbidden");
 expect(await f.store.read("b",f.runId)).toEqual(before);
});
it("authorizes original roles independently of being the host",async()=>{
 const f=await fixture();await f.store.initialize(f.context);
 await expect(f.store.prepareControl("a",f.runId,{kind:"TRAIN_COMBAT"})).resolves.toMatchObject({revision:1,sourceOwnership:"UNCHANGED"});
 await expect(f.store.prepareControl("b",f.runId,{kind:"OPERATOR_COMBAT"})).resolves.toMatchObject({revision:1});
 await expect(f.store.prepareControl("a",f.runId,{kind:"OPERATOR_COMBAT"})).rejects.toThrow("combat_control_role_forbidden");
 await expect(f.store.prepareControl("a",f.runId,{kind:"SHARED_PROGRESS"})).rejects.toThrow("combat_control_grant_required");
});
it("grants survivor both controls and shared progress but never the peer personal rights",async()=>{
 const f=await resuming();await f.resumeStore.resolveRecovery("b",f.runId,resume());
 for(const kind of ["TRAIN_COMBAT","OPERATOR_COMBAT","SHARED_PROGRESS"])await expect(f.store.prepareControl("b",f.runId,{kind})).resolves.toMatchObject({revision:3,sourceOwnership:"UNCHANGED"});
 await expect(f.store.prepareControl("b",f.runId,{kind:"PERSONAL_TRANSACTION",ownerAccountId:"a"})).rejects.toThrow("combat_control_personal_owner_forbidden");
 await expect(f.store.prepareControl("b",f.runId,{kind:"PERSONAL_TRANSACTION",ownerAccountId:"b"})).resolves.toMatchObject({revision:3});
 await expect(f.store.prepareControl("a",f.runId,{kind:"TRAIN_COMBAT"})).rejects.toThrow("combat_control_inactive_participant");
});
it("gates controls while recovering and after termination",async()=>{
 const f=await resuming();await expect(f.store.prepareControl("b",f.runId,{kind:"OPERATOR_COMBAT"})).rejects.toThrow("combat_host_recovery_required");
 f.sql.run("UPDATE runs SET status='COMPLETED'");await expect(f.store.prepareControl("b",f.runId,{kind:"OPERATOR_COMBAT"})).rejects.toThrow("combat_host_run_not_active");
});
it("composes the permission guard with effects and rejects a stale prepared permission",async()=>{
 const f=await fixture();await f.store.initialize(f.context);f.sql.run("CREATE TABLE test_control_effect(value INTEGER)");
 const allowed=await f.store.prepareControl("a",f.runId,{kind:"TRAIN_COMBAT"});
 await f.env.DB.batch([allowed.assertion,f.env.DB.prepare("INSERT INTO test_control_effect VALUES(1)"),allowed.cleanup]);
 expect(f.scalar("SELECT count(*) FROM test_control_effect")).toBe(1);
 const stale=await f.store.prepareControl("a",f.runId,{kind:"TRAIN_COMBAT"});
 await f.store.recordDisconnect(f.runId,{requestId:"pause-control",expectedRevision:1,disconnectedAccountId:"b",observedAt:1000});
 await expect(f.env.DB.batch([stale.assertion,f.env.DB.prepare("INSERT INTO test_control_effect VALUES(2)"),stale.cleanup])).rejects.toThrow();
 expect(f.scalar("SELECT count(*) FROM test_control_effect")).toBe(1);
});

it("preserves immutable host generations across solo transfer",async()=>{
 const f=await resuming();await f.resumeStore.resolveRecovery("b",f.runId,resume());
 expect(f.scalar("SELECT count(*) FROM combat_host_lineage")).toBe(2);
 expect(JSON.parse(String(f.scalar("SELECT context_json FROM combat_host_lineage WHERE host_generation=1"))).hostAccountId).toBe("a");
 expect(JSON.parse(String(f.scalar("SELECT context_json FROM combat_host_lineage WHERE host_generation=2"))).hostAccountId).toBe("b");
 expect(f.scalar("SELECT previous_generation FROM combat_host_lineage WHERE host_generation=2")).toBe(1);
 expect(()=>f.sql.run("UPDATE combat_host_lineage SET provenance='INITIAL'")).toThrow("immutable_host_lineage");
 expect(()=>f.sql.run("DELETE FROM combat_host_lineage")).toThrow("immutable_host_lineage");
});
it("rolls back the transition when lineage persistence fails",async()=>{
 const f=await resuming(),before=await f.store.read("b",f.runId);
 f.sql.run("CREATE TRIGGER fail_lineage BEFORE INSERT ON combat_host_lineage BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.resumeStore.resolveRecovery("b",f.runId,resume())).rejects.toThrow("injected");
 expect(await f.store.read("b",f.runId)).toEqual(before);expect(await f.store.lookupResume("b",f.runId,"resume")).toBeNull();
 expect(f.scalar("SELECT count(*) FROM combat_host_lineage")).toBe(1);
});

it("rolls back initial state if its assignment cannot be saved",async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_initial_lineage BEFORE INSERT ON combat_host_lineage BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.initialize(f.context)).rejects.toThrow("injected");
 expect(f.scalar("SELECT count(*) FROM combat_host_report_states")).toBe(0);
});
