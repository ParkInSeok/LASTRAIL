import {describe,expect,it} from "vitest";
import {validateAdPolicyDraft} from "./adPolicyDraft";
describe("policy draft rules",()=>{
 it("requires explicit value and nonempty reason",()=>expect(validateAdPolicyDraft({adsEnabled:null,reason:" "},false)).toHaveLength(2));
 it("accepts OFF without claiming readiness",()=>expect(validateAdPolicyDraft({adsEnabled:false,reason:"local"},false)).toEqual([]));
 it("blocks ON when server capability is unavailable",()=>expect(validateAdPolicyDraft({adsEnabled:true,reason:"local"},false)).toHaveLength(1));
 it("bounds reason at the server boundary",()=>{expect(validateAdPolicyDraft({adsEnabled:false,reason:"x".repeat(500)},false)).toEqual([]);expect(validateAdPolicyDraft({adsEnabled:false,reason:"x".repeat(501)},false)).toHaveLength(1);});
});
