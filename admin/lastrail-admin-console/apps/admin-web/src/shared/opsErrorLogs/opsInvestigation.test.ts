import { afterEach, expect, it, vi } from 'vitest';
import { mapOpsErrorLogSummaryResponse, opsErrorLogReadFilterFields } from './opsErrorLogContract';
import { adminApi } from '../api/client';
import {opsGroupsSchema,opsInvestigationReadSchema,assertOpsReceipt} from './opsInvestigationContract';
afterEach(()=>vi.unstubAllGlobals());
it('does not turn missing summary counts into zero',()=>{expect(mapOpsErrorLogSummaryResponse({}).recentErrorCount).toBe('미공급');});
it('includes phase, contract and period filter contract',()=>{expect(opsErrorLogReadFilterFields).toEqual(expect.arrayContaining(['phase','contractVersion','occurredFrom','occurredTo']));});
it('uses identical summary filters and does not fetch after successful investigation command',async()=>{
 const calls:Array<[string,RequestInit|undefined]>=[];vi.stubGlobal('fetch',async(path:string,init?:RequestInit)=>{calls.push([path,init]);return Response.json({accepted:true,contractVersion:'ops-investigation.v1.20261019',permission:'OPS_ERROR_LOG_WRITE',auditId:'audit',investigation:{errorLogId:'event',status:'acknowledged',version:1,reason:'investigate',fixBuild:null,evidence:null,verificationResult:null,updatedBy:'operator',updatedAt:1,resolvedAt:null}})});
 await adminApi.opsErrorLogSummary({environment:'DEV',phase:'save'});expect(calls[0][0]).toBe('/api/ops/error-logs/summary?environment=DEV&phase=save');
 const result=await (adminApi as any).opsErrorLogInvestigationCommand('event',{status:'acknowledged',expectedVersion:0,reason:'investigate',fixBuild:null,evidence:null,verificationResult:null},'original-key');expect(result.investigation.version).toBe(1);expect(calls).toHaveLength(2);expect(new Headers(calls[1][1]?.headers).get('Idempotency-Key')).toBe('original-key');
});

it('preserves whole-filter summary metadata and known/unknown occurrence separation',()=>{
 const period={occurredFrom:'2026-10-01T00:00:00.000Z',occurredTo:null},filters={phase:'save'};
 expect(mapOpsErrorLogSummaryResponse({eventCount:2,occurrenceCount:null,occurrenceUnknownEventCount:2,affectedAccountCount:1,unattributedEventCount:1,scope:'FILTERED_ALL',statusScope:'ORIGINAL_EVENT',filters,period})).toMatchObject({eventCount:2,occurrenceCount:null,occurrenceUnknownEventCount:2,scope:'FILTERED_ALL',statusScope:'ORIGINAL_EVENT',filters,period});
});

it('preserves unknown group dimensions and all-event totals independently from the displayed page',()=>{
 const item={environment:'DEV',service:'game_api',errorCode:'save_failure',phase:null,serverVersion:null,appVersion:null,appVersionCode:null,contractVersion:null,eventCount:1,occurrenceCount:null,occurrenceUnknownEventCount:1,affectedAccountCount:0,unattributedEventCount:1,firstOccurredAt:'2026-10-01T00:00:00.000Z',lastOccurredAt:'2026-10-01T00:00:00.000Z'};
 const result=opsGroupsSchema.parse({contractVersion:'ops-investigation.v1.20261019',collectionAvailable:true,scope:'FILTERED_ALL',statusScope:'ORIGINAL_EVENT',filters:{phase:'save'},period:{occurredFrom:null,occurredTo:null},serverCalculatedAt:'2026-10-01T00:00:00.000Z',items:[item],totalGroupCount:51,pageItemCount:1,nextCursor:'1'});
 expect(result.totalGroupCount).toBe(51);expect(result.items[0].phase).toBeNull();expect(result.items[0].occurrenceCount).toBeNull();expect(result.items[0].occurrenceUnknownEventCount).toBe(1);
});
it('keeps recurrence target/other/unknown and the target outside its first page distinct',()=>{
 const state={errorLogId:'event',status:'resolved',version:3,reason:'verified',fixBuild:{appVersion:null,appVersionCode:null,serverVersion:'fixed'},evidence:'QA',verificationResult:'PASS',updatedBy:'operator',updatedAt:1,resolvedAt:1};
 const result=opsInvestigationReadSchema.parse({contractVersion:'ops-investigation.v1.20261019',investigation:state,recurrence:{available:true,totalEventCount:51,targetBuildRecurred:true,targetBuildEventCount:1,otherBuildEventCount:25,unknownBuildEventCount:25,items:Array.from({length:50},(_,i)=>({id:'earlier_'+i,occurredAt:i+2,appVersion:null,appVersionCode:null,serverVersion:i<25?'other':null,targetBuild:i<25?0:null})),hasMore:true,latestTargetEventId:'outside_page'}});
 expect(result.recurrence).toMatchObject({latestTargetEventId:'outside_page',hasMore:true,otherBuildEventCount:25,unknownBuildEventCount:25});expect(opsInvestigationReadSchema.parse({...result,recurrence:{available:false,reason:'ORIGINAL_EVENT_RETAINED_OUT'}}).recurrence).toEqual({available:false,reason:'ORIGINAL_EVENT_RETAINED_OUT'});
});
it('preserves 409 current through the browser API error and rejects another event receipt',async()=>{
 const current={errorLogId:'event',version:3};vi.stubGlobal('fetch',async()=>Response.json({error:'ops_investigation_version_conflict',current},{status:409}));const command={status:'acknowledged' as const,expectedVersion:0,reason:'investigate',fixBuild:null,evidence:null,verificationResult:null};
 await expect(adminApi.opsErrorLogInvestigationCommand('event',command,'original')).rejects.toMatchObject({status:409,errorCode:'ops_investigation_version_conflict',current});
 expect(()=>assertOpsReceipt({accepted:true,contractVersion:'ops-investigation.v1.20261019',permission:'OPS_ERROR_LOG_WRITE',auditId:'audit',investigation:{errorLogId:'different_event',status:'acknowledged',version:1,reason:'investigate',fixBuild:null,evidence:null,verificationResult:null,updatedBy:'operator',updatedAt:1,resolvedAt:null}},'event',command)).toThrow('OPS_INVESTIGATION_RECEIPT_MISMATCH');
});
