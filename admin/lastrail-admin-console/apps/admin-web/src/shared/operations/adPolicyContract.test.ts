import {describe,it,expect} from "vitest";
import {mapAdPolicy, mapAdPolicyWrite} from "./adPolicyContract";
const policy={accepted:true,contractVersion:"admin-runtime-policy-v1",environment:"DEV",policyStatus:"CONFIGURED",adsEnabled:false,runtimePolicyRevision:2,serverTime:100,permission:"RUNTIME_POLICY_READ",permissionMode:"ROLE_PERMISSION",canWrite:true,canEnable:false,enableBlockReason:"ad_enable_readiness_unavailable"};
describe("Admin policy boundary",()=>{
 it("accepts configured OFF and unknown without inventing a default",()=>{
  expect(mapAdPolicy(policy,"DEV").adsEnabled).toBe(false);
  expect(mapAdPolicy({...policy,policyStatus:"UNKNOWN",adsEnabled:null,runtimePolicyRevision:null},"DEV").adsEnabled).toBeNull();
 });
 it.each([{environment:"PRD"},{contractVersion:"runtime-policy-v1"},{runtimePolicyRevision:1.5},{runtimePolicyRevision:0},{runtimePolicyRevision:Number.MAX_SAFE_INTEGER+1},{adsEnabled:null},{canWrite:"true"},{permission:"BALANCE_READ"},{serverTime:Number.MAX_SAFE_INTEGER}])("rejects incompatible read %j",change=>expect(()=>mapAdPolicy({...policy,...change},"DEV")).toThrow());
 it("does not accept unknown with a stale value",()=>expect(()=>mapAdPolicy({...policy,policyStatus:"UNKNOWN"},"DEV")).toThrow());
 it("validates write receipt against the exact submitted request",()=>{
  const command={environment:"DEV" as const,expectedRuntimePolicyRevision:2,adsEnabled:false,reason:"test"};
  const receipt={accepted:true,contractVersion:"admin-runtime-policy-write-v1",environment:"DEV",adsEnabled:false,runtimePolicyRevision:3,auditId:"audit",requestId:"request"};
  expect(mapAdPolicyWrite(receipt,command).auditId).toBe("audit");
  for(const change of [{environment:"PRD"},{adsEnabled:true},{runtimePolicyRevision:4},{auditId:""}])expect(()=>mapAdPolicyWrite({...receipt,...change},command)).toThrow();
 });
});
