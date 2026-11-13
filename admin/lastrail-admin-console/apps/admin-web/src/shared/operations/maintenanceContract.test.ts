import {afterEach,describe,it,expect,vi} from "vitest";
import {adminApi} from "../api/client";
import {maintenanceDocumentSchema,maintenancePreviewSchema,parseMaintenanceTime} from "./maintenanceContract";
const document={applicationId:"lastrail",serviceScope:"GAME_AND_WEB",webMode:"MAINTENANCE",title:"점검 계획",summary:"계획만 조회",noticeId:null,drainStartsAt:100,startsAt:200,estimatedEndAt:300,existingRunPolicy:"DRAIN_THEN_PRESERVE",statusFreshnessSeconds:30,retryAfterSeconds:60};
const base={environment:"DEV",serverTime:250,application:{applied:false,status:"UNCONNECTED",currentPolicyRevision:null,blockReasons:["request_gates_unconnected"]},permissions:{canSaveDraft:true,canApply:false}};
const preview={...base,contractVersion:"admin-maintenance-preview-v1",preview:{kind:"HYPOTHETICAL_SCHEDULE",applied:false,maintenanceId:"p",draftRevision:2,at:250,effectiveGameMode:"MAINTENANCE",effectiveWebMode:"MAINTENANCE",estimatedEndAt:300,automaticCompletion:false,existingRunPolicy:"DRAIN_THEN_PRESERVE",title:"계획",summary:"가상"}};
describe("maintenance read boundary",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("accepts explicit planned preservation and rejects contradictory targets",()=>{
  expect(maintenanceDocumentSchema.safeParse(document).success).toBe(true);
  for(const delta of [{serviceScope:"GAME"},{serviceScope:"WEB"},{drainStartsAt:null},{drainStartsAt:201},{estimatedEndAt:200},{existingRunPolicy:"EMERGENCY_PRESERVE"},{title:"<b>x</b>"}])expect(maintenanceDocumentSchema.safeParse({...document,...delta}).success).toBe(false);
 });
 it("requires preview to remain hypothetical and unapplied",()=>{
  expect(maintenancePreviewSchema.safeParse(preview).success).toBe(true);
  for(const delta of [{applied:true},{automaticCompletion:true},{kind:"LIVE"},{effectiveGameMode:"UNKNOWN"}])expect(maintenancePreviewSchema.safeParse({...preview,preview:{...preview.preview,...delta}}).success).toBe(false);
  expect(maintenancePreviewSchema.safeParse({...preview,permissions:{canSaveDraft:true,canApply:true}}).success).toBe(false);
 });
 it("interprets requested dates in Korea time independently of browser timezone",()=>{expect(parseMaintenanceTime("2026-09-22T10:30")).toBe(Date.parse("2026-09-22T01:30:00Z"));for(const value of ["","invalid","2026-99-99T00:00","1960-01-01T00:00"])expect(parseMaintenanceTime(value)).toBeNull();});
 it.each([{environment:"PRD"},{preview:{...preview.preview,maintenanceId:"other"}},{preview:{...preview.preview,draftRevision:3}},{preview:{...preview.preview,at:251}}])("rejects stale/mismatched preview %j",async delta=>{vi.stubGlobal("fetch",vi.fn(async()=>Response.json({...preview,...delta})));await expect(adminApi.maintenancePreview("DEV","p",2,250)).rejects.toThrow();});
 it("uses only GET and preserves exact preview timestamp",async()=>{const fetcher=vi.fn(async()=>Response.json(preview));vi.stubGlobal("fetch",fetcher);await adminApi.maintenancePreview("DEV","p",2,250);const [url,init]=fetcher.mock.calls[0] as unknown as [string,RequestInit];expect(url).toBe("/api/maintenance-plans/p/preview?at=250");expect(init.method).toBeUndefined();});
 it("rejects wrong detail identity and wrong list environment",async()=>{vi.stubGlobal("fetch",vi.fn(async()=>Response.json({...base,contractVersion:"admin-maintenance-plan-v1",plan:{maintenanceId:"other",draftRevision:1,planState:"DRAFT",document,updatedAt:100}})));await expect(adminApi.maintenancePlan("DEV","p")).rejects.toThrow();vi.stubGlobal("fetch",vi.fn(async()=>Response.json({...base,environment:"PRD",contractVersion:"admin-maintenance-plan-list-v1",items:[],nextAfter:null})));await expect(adminApi.maintenancePlans("DEV")).rejects.toThrow();});
});
