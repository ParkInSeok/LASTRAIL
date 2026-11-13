import {describe,expect,it} from "vitest";
import type {AuditLogsResponse} from "@lastrail/admin-contracts";
import {recentPolicyHistory} from "./adPolicyHistory";
type Item=AuditLogsResponse["items"][number];
const item=(id:number,extra:Partial<Item>={}):Item=>({id:String(id),principalId:"actor",principalDisplayName:"운영자",role:"OWNER",permission:"RUNTIME_POLICY_WRITE",action:"RUNTIME_POLICY_UPDATE",resourceType:"runtime_policy",resourceId:"DEV",requestId:"req",confirmationId:null,payloadHash:null,changeSummary:{before:{adsEnabled:true},after:{adsEnabled:false},reason:"점검"},result:"SUCCESS",errorCode:null,createdAt:new Date(id*1000).toISOString(),...extra});
describe("recent policy history",()=>{
 it("shows newest five successful environment changes without mutating input",()=>{
  const rows=[item(1),item(8,{resourceId:"PRD"}),item(2),item(3),item(4),item(5),item(6),item(9,{result:"FAILURE"}),item(10,{action:"OTHER"})];const original=rows.map(row=>row.id);
  expect(recentPolicyHistory(rows,"DEV").map(row=>row.id)).toEqual(["6","5","4","3","2"]);expect(rows.map(row=>row.id)).toEqual(original);
 });
 it("does not turn unknown historical fields into OFF",()=>{expect(recentPolicyHistory([item(1,{changeSummary:{before:null,after:{adsEnabled:"false"}}})],"DEV")[0]).toMatchObject({before:"미확인",after:"미확인",reason:"사유 미확인"});});
 it("retains actor reason and actual transition",()=>{expect(recentPolicyHistory([item(1)],"DEV")[0]).toMatchObject({actor:"운영자",before:"ON",after:"OFF",reason:"점검"});});
});
