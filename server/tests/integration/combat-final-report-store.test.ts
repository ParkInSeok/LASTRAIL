import {sha256Hex} from "@lastrail/common";
import {readFileSync} from "node:fs";
import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
import {CombatHostReportStore} from "../../apps/game-api/src/combat-host-report-store";
import {CombatFinalReportStore} from "../../apps/game-api/src/combat-final-report-store";
async function fixture(){
 const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 // Test-only future coop snapshot, never a production admission bypass.
 const s=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));s.admission.combatAuthority=captureCombatAuthority("ORDINARY_COOP");s.departureResponse.admission.combatAuthority=s.admission.combatAuthority;f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(s)]);
 const context={authority:s.admission.combatAuthority,runId:r.runId,participants:[{accountId:"a",role:"TRAIN"},{accountId:"b",role:"OPERATOR"}],activeAccountIds:["a","b"],hostAccountId:"a",hostGeneration:2,definitionVersion:s.admission.combatPolicyRequirements.version,snapshotRevision:1,lastStateSequence:1,logicalTimeMs:100};
 await new CombatHostReportStore(f.env.DB).initialize(context);
 return {...f,runId:r.runId,context,store:new CombatFinalReportStore(f.env.DB)};
}
const report=(f:Awaited<ReturnType<typeof fixture>>)=>({version:"combat.final-report.v1",runId:f.runId,hostGeneration:2,definitionVersion:f.context.definitionVersion,originHostAccountId:"a",finalRecordId:"final",recordSha256:"a".repeat(64)});
it("deduplicates evidence across original submitters and never finalizes or rewards",async()=>{
 const f=await fixture(),snapshot=f.scalar("SELECT snapshot_json FROM run_snapshots"),c={requestId:"submit",report:report(f)};
 const first=await f.store.submit("a",f.runId,c);expect(await f.store.submit("b",f.runId,c)).toEqual(first);expect(await f.store.submit("a",f.runId,c)).toEqual(first);
 expect(first).toMatchObject({status:"REQUIRES_VERIFICATION",rewardsAuthorized:false});expect(f.scalar("SELECT count(*) FROM combat_final_evidence")).toBe(1);expect(f.scalar("SELECT count(*) FROM combat_final_submissions")).toBe(2);
 expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);expect(f.scalar("SELECT count(*) FROM run_reward_previews")).toBe(0);expect(f.scalar("SELECT status FROM runs")).toBe("ACTIVE");expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(snapshot);
});
it("freezes server participant scope independently of the participant relaying a joint clear",async()=>{
 const f=await fixture(),c={requestId:"joint",report:report(f)};
 const first=await f.store.submit("b",f.runId,c);
 expect(first.participationEvidence).toEqual({originActiveAccountIds:["a","b"],observedActiveAccountIds:["a","b"],originHostGeneration:2,observedHostGeneration:2});
 expect(await f.store.submit("a",f.runId,c)).toEqual(first);
 expect(first.rewardsAuthorized).toBe(false);
});
it("preserves departed and survivor scope as evidence without paying either account",async()=>{
 const f=await fixture(),next={...f.context,hostGeneration:3,hostAccountId:"b",activeAccountIds:["b"]};
 f.sql.run("INSERT INTO combat_host_lineage VALUES(?,?,?,'RESUME',?)",[f.runId,3,2,JSON.stringify(next)]);
 f.sql.run("UPDATE combat_host_report_states SET context_json=?",[JSON.stringify(next)]);
 const current=await f.store.submit("a",f.runId,{requestId:"survivor",report:{...report(f),hostGeneration:3,originHostAccountId:"b",finalRecordId:"survivor"}});
 expect(current.participationEvidence).toEqual({originActiveAccountIds:["b"],observedActiveAccountIds:["b"],originHostGeneration:3,observedHostGeneration:3});
 const old=await f.store.submit("b",f.runId,{requestId:"old",report:{...report(f),finalRecordId:"old"}});
 expect(old.participationEvidence).toEqual({originActiveAccountIds:["a","b"],observedActiveAccountIds:["b"],originHostGeneration:2,observedHostGeneration:3});
 expect(old.status).toBe("HISTORICAL_EVIDENCE_ONLY");
 expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("rejects conflicting record contents and foreign submitters",async()=>{
 const f=await fixture(),c={requestId:"submit",report:report(f)};await f.store.submit("a",f.runId,c);
 await expect(f.store.submit("b",f.runId,{...c,report:{...c.report,recordSha256:"b".repeat(64)}})).rejects.toThrow("combat_final_evidence_conflict");
 await expect(f.store.submit("stranger",f.runId,c)).rejects.toThrow("combat_final_sender_forbidden");
 await expect(f.store.submit("a",f.runId,{...c,report:{...c.report,finalRecordId:"other"}})).rejects.toThrow("combat_final_request_conflict");
});
it("rolls back evidence if submission receipt fails",async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_final BEFORE INSERT ON combat_final_submissions BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.submit("b",f.runId,{requestId:"submit",report:report(f)})).rejects.toThrow();expect(f.scalar("SELECT count(*) FROM combat_final_evidence")).toBe(0);
});
it("recovers an acknowledgement lost after commit",async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let lose=true;f.env.DB.batch=async statements=>{const r=await batch(statements);if(lose){lose=false;throw new Error("lost");}return r;};
 const first=await f.store.submit("b",f.runId,{requestId:"submit",report:report(f)});expect(await f.store.lookup("b",f.runId,"submit")).toEqual(first);expect(await f.store.lookup("a",f.runId,"submit")).toBeNull();
});
it("preserves late old-generation evidence without reopening a terminated run",async()=>{
 const f=await fixture();f.sql.run("UPDATE combat_host_report_states SET context_json=?",[JSON.stringify({...f.context,hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]})]);f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect(await f.store.submit("b",f.runId,{requestId:"late",report:report(f)})).toMatchObject({status:"HISTORICAL_EVIDENCE_ONLY",rewardsAuthorized:false});expect(f.scalar("SELECT status FROM runs")).toBe("COMPLETED");expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("fences context changes while evidence is being persisted",async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_host_report_states SET revision=2");};
 await expect(f.store.submit("a",f.runId,{requestId:"submit",report:report(f)})).rejects.toThrow("combat_final_state_conflict");expect(f.scalar("SELECT count(*) FROM combat_final_evidence")).toBe(0);
});

it("rejects a peer impersonating the old host and refuses unknown generations",async()=>{
 const f=await fixture();f.sql.run("UPDATE combat_host_report_states SET context_json=?",[JSON.stringify({...f.context,hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]})]);
 await expect(f.store.submit("b",f.runId,{requestId:"forged",report:{...report(f),originHostAccountId:"b"}})).rejects.toThrow("combat_final_origin_mismatch");
 await expect(f.store.submit("a",f.runId,{requestId:"unknown",report:{...report(f),hostGeneration:1}})).rejects.toThrow("combat_final_lineage_unavailable");
 expect(f.scalar("SELECT count(*) FROM combat_final_evidence")).toBe(0);
});

it("backfills only the current assignment and preserves previously stored evidence replay",async()=>{
 const f=await fixture(),c={requestId:"old",report:report(f)},original=await f.store.submit("a",f.runId,c);
 // Simulate the pre-0056 schema with an already advanced current generation.
 f.sql.run("DROP TABLE combat_host_lineage");
 f.sql.run("UPDATE combat_host_report_states SET context_json=?",[JSON.stringify({...f.context,hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]})]);
 const snapshot=f.scalar("SELECT snapshot_json FROM run_snapshots");
 f.sql.run(readFileSync("packages/db/migrations/0056_combat_host_lineage.sql","utf8"));
 expect(f.scalar("SELECT count(*) FROM combat_host_lineage")).toBe(1);
 expect(f.scalar("SELECT host_generation FROM combat_host_lineage")).toBe(3);
 expect(f.scalar("SELECT provenance FROM combat_host_lineage")).toBe("MIGRATION_CURRENT");
 expect(await f.store.submit("a",f.runId,c)).toEqual(original);
 expect(await f.store.submit("b",f.runId,{...c,requestId:"relay"})).toEqual(original);
 await expect(f.store.submit("b",f.runId,{requestId:"new-old",report:{...report(f),finalRecordId:"unseen"}})).rejects.toThrow("combat_final_lineage_unavailable");
 expect(f.scalar("SELECT snapshot_json FROM run_snapshots")).toBe(snapshot);
});

it("archives exact final record bytes from either participant without granting rewards",async()=>{
 const f=await fixture(),body=' {"value":1}\n',c={requestId:"body-ref",report:{...report(f),recordSha256:await sha256Hex(body)}};
 await f.store.submit("a",f.runId,c);
 expect(await f.store.readRecord("b",f.runId,"final")).toBeNull();
 const result=await f.store.attachRecord("b",f.runId,"final",body);
 expect(result).toMatchObject({version:"combat.final-record-storage.v1",recordSha256:c.report.recordSha256,combatVerification:"NOT_PERFORMED",rewardsAuthorized:false});
 expect(await f.store.attachRecord("a",f.runId,"final",body)).toEqual(result);
 expect(await f.store.readRecord("a",f.runId,"final")).toEqual({...result,recordText:body});
 expect(f.scalar("SELECT count(*) FROM combat_final_record_bodies")).toBe(1);
 expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
 await expect(f.store.attachRecord("a",f.runId,"final",body.trim())).rejects.toThrow("combat_final_record_hash_mismatch");
 await expect(f.store.readRecord("stranger",f.runId,"final")).rejects.toThrow("combat_final_sender_forbidden");
});
it("requires a stored reference and rolls back failed body writes",async()=>{
 const f=await fixture(),body="record";
 await expect(f.store.attachRecord("a",f.runId,"absent",body)).rejects.toThrow("combat_final_evidence_missing");
 await f.store.submit("a",f.runId,{requestId:"ref",report:{...report(f),recordSha256:await sha256Hex(body)}});
 f.sql.run("CREATE TRIGGER fail_body BEFORE INSERT ON combat_final_record_bodies BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.store.attachRecord("b",f.runId,"final",body)).rejects.toThrow("injected");expect(await f.store.readRecord("a",f.runId,"final")).toBeNull();
});
it("recovers lost body acknowledgements and retains evidence after run termination",async()=>{
 const f=await fixture(),body="final bytes";await f.store.submit("a",f.runId,{requestId:"ref",report:{...report(f),recordSha256:await sha256Hex(body)}});
 f.sql.run("UPDATE runs SET status='COMPLETED'");const batch=f.env.DB.batch.bind(f.env.DB);f.env.DB.batch=async statements=>{await batch(statements);throw new Error("lost");};
 const result=await f.store.attachRecord("b",f.runId,"final",body);
 expect(await f.store.readRecord("a",f.runId,"final")).toEqual({...result,recordText:body});
 expect(()=>f.sql.run("UPDATE combat_final_record_bodies SET record_text='other'")).toThrow("immutable_final_record_body");
 expect(()=>f.sql.run("DELETE FROM combat_final_record_bodies")).toThrow("immutable_final_record_body");
 expect(f.scalar("SELECT status FROM runs")).toBe("COMPLETED");
});

it("fences stale body persistence, refuses outsiders and preserves Unicode text",async()=>{
 const f=await fixture(),body="\ufeff\ud55c\uae00";
 await f.store.submit("a",f.runId,{requestId:"ref",report:{...report(f),recordSha256:await sha256Hex(body)}});
 await expect(f.store.attachRecord("stranger",f.runId,"final",body)).rejects.toThrow("combat_final_sender_forbidden");
 await expect(f.store.attachRecord("a",f.runId,"final","\ud800")).rejects.toThrow("invalid_utf8_text");
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE combat_host_report_states SET revision=revision+1");};
 await expect(f.store.attachRecord("a",f.runId,"final",body)).rejects.toThrow("combat_final_state_conflict");
 expect(await f.store.readRecord("a",f.runId,"final")).toBeNull();
 await f.store.attachRecord("a",f.runId,"final",body);expect((await f.store.readRecord("b",f.runId,"final"))?.recordText).toBe(body);
});
