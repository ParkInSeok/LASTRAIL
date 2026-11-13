import {expect,it} from "vitest";
import {classifyFinalReport} from "../../apps/game-api/src/combat-final-report";
import {validateHostReport} from "../../apps/game-api/src/combat-host-report";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
const context=()=>({authority:captureCombatAuthority("ORDINARY_COOP"),runId:"run",participants:[{accountId:"a",role:"TRAIN"},{accountId:"b",role:"OPERATOR"}],activeAccountIds:["a","b"],hostAccountId:"a",hostGeneration:2,definitionVersion:"defs",snapshotRevision:4,lastStateSequence:8,logicalTimeMs:1000});
const final=()=>({version:"combat.final-report.v1",runId:"run",hostGeneration:2,definitionVersion:"defs",originHostAccountId:"a",finalRecordId:"record",recordSha256:"a".repeat(64)});
it.each(["a","b"])("permits original participant %s to submit the same record without ACK or payment",sender=>{
 expect(classifyFinalReport(context(),sender,final())).toMatchObject({status:"REQUIRES_VERIFICATION",sourceVerification:"NOT_PERFORMED",combatVerification:"NOT_PERFORMED",rewardsAuthorized:false,report:final()});
});
it("allows old-host evidence after transfer but never applies it as current state",()=>{
 expect(classifyFinalReport({...context(),hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]},"b",final())).toMatchObject({status:"HISTORICAL_EVIDENCE_ONLY",rewardsAuthorized:false});
});
it("allows survivor host state reports without departed peer ACK",()=>{
 const c={...context(),hostAccountId:"b",hostGeneration:3,activeAccountIds:["b"]};
 expect(validateHostReport(c,"b",{version:"combat.host-report.v1",runId:"run",hostGeneration:3,definitionVersion:"defs",baseSnapshotRevision:4,stateSequence:9,logicalTimeMs:1100}).status).toBe("ENVELOPE_VALID");
});
it("rejects departed host as current context and preserves original participants",()=>{
 expect(()=>classifyFinalReport({...context(),activeAccountIds:["b"]},"b",final())).toThrow();
 const c=context(),before=structuredClone(c);classifyFinalReport(c,"b",final());expect(c).toEqual(before);
});
it.each([
 ["runId","foreign"], ["definitionVersion","other"], ["hostGeneration",3], ["originHostAccountId","b"], ["originHostAccountId","stranger"]
])("rejects mismatched final %s",(key,value)=>{
 expect(()=>classifyFinalReport(context(),"b",{...final(),[key]:value})).toThrow();
});
it("rejects foreign submitters, reward claims and malformed record references",()=>{
 expect(()=>classifyFinalReport(context(),"stranger",final())).toThrow();
 for(const extra of [{rewardAccountId:"b"},{verified:true},{recordSha256:"bad"},{peerAck:true}])expect(()=>classifyFinalReport(context(),"b",{...final(),...extra})).toThrow();
});
it.each(["ORDINARY_SOLO","WORLD_BOSS_SOLO","WORLD_BOSS_COOP"])("does not apply survivor report policy to %s",mode=>{
 expect(()=>classifyFinalReport({...context(),authority:captureCombatAuthority(mode)},"b",final())).toThrow();
});
it("rejects forged current participants",()=>{
 for(const activeAccountIds of [[],["a","a"],["a","stranger"]])expect(()=>classifyFinalReport({...context(),activeAccountIds},"a",final())).toThrow();
});
