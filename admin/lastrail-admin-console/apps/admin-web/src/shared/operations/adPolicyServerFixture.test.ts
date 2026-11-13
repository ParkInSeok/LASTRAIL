import {readFileSync} from "node:fs";
import {describe,it,expect} from "vitest";
import {mapAdPolicy,mapAdPolicyWrite} from "./adPolicyContract";
const fixture=JSON.parse(readFileSync("D:/Project/LASTRAIL/QA/AdminRuntimePolicy-20260921/http-fixtures.json","utf8")).samples;
describe("actual local Hono policy response compatibility",()=>{
 it("consumes UNKNOWN and OFF without changing their meaning",()=>{expect(mapAdPolicy(fixture.unknown.body,"DEV").adsEnabled).toBeNull();expect(mapAdPolicy(fixture.off.body,"DEV").adsEnabled).toBe(false);});
 it("accepts original and replay receipts as request outcomes",()=>{const command={environment:"DEV" as const,adsEnabled:false,expectedRuntimePolicyRevision:null,reason:"fixture"};expect(mapAdPolicyWrite(fixture.saved.body,command)).toEqual(mapAdPolicyWrite(fixture.replay.body,command));});
});
