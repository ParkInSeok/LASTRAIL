import {expect,it} from "vitest";
import {validateHostReport} from "../../apps/game-api/src/combat-host-report";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
const context=()=>({authority:captureCombatAuthority("ORDINARY_COOP"),runId:"run",participants:[{accountId:"train",role:"TRAIN"},{accountId:"operator",role:"OPERATOR"}],hostAccountId:"operator",hostGeneration:2,definitionVersion:"defs-1",snapshotRevision:4,lastStateSequence:8,logicalTimeMs:1000});
const report=()=>({version:"combat.host-report.v1",runId:"run",hostGeneration:2,definitionVersion:"defs-1",baseSnapshotRevision:4,stateSequence:9,logicalTimeMs:1100});
it("accepts an operator-role host envelope without claiming combat verification",()=>{
 expect(validateHostReport(context(),"operator",report())).toEqual({status:"ENVELOPE_VALID",combatVerification:"NOT_PERFORMED",report:report()});
});
it.each(["stranger","train"])("rejects non-host sender %s",sender=>{
 expect(()=>validateHostReport(context(),sender,report())).toThrow("combat_host_sender_forbidden");
});
it.each([
 ["runId","other","combat_host_run_mismatch"],
 ["hostGeneration",1,"combat_host_generation_mismatch"],
 ["definitionVersion","other","combat_host_definition_mismatch"],
 ["baseSnapshotRevision",3,"combat_host_snapshot_mismatch"],
 ["stateSequence",8,"combat_host_sequence_invalid"],
 ["stateSequence",10,"combat_host_sequence_invalid"],
 ["logicalTimeMs",999,"combat_host_time_regressed"],
])("rejects mismatched %s=%s",(field,value,error)=>{
 expect(()=>validateHostReport(context(),"operator",{...report(),[field]:value})).toThrow(error);
});
it.each(["ORDINARY_SOLO","WORLD_BOSS_SOLO","WORLD_BOSS_COOP"])("rejects host reports for %s",mode=>{
 expect(()=>validateHostReport({...context(),authority:captureCombatAuthority(mode)},"operator",report())).toThrow("combat_host_mode_unsupported");
});
it("rejects self promotion, result claims and malformed numbers",()=>{
 for(const extra of [{hostAccountId:"train"},{verified:true},{reward:100},{stateSequence:1.5},{logicalTimeMs:Infinity}])
  expect(()=>validateHostReport(context(),"operator",{...report(),...extra})).toThrow();
});
it("requires distinct server-owned participants and an assigned participant host",()=>{
 expect(()=>validateHostReport({...context(),hostAccountId:"stranger"},"stranger",report())).toThrow();
 expect(()=>validateHostReport({...context(),participants:[context().participants[0],context().participants[0]]},"train",report())).toThrow();
});
it("does not advance context on valid or rejected validation",()=>{
 const c=context(),before=structuredClone(c);validateHostReport(c,"operator",report());
 expect(()=>validateHostReport(c,"operator",{...report(),hostGeneration:3})).toThrow();expect(c).toEqual(before);
});
