import {describe,it,expect} from 'vitest';
import {maintenanceDocumentSchema,maintenanceApplication,type MaintenanceDraft} from '../../packages/common/src/maintenance-plan-policy';
import {previewMaintenanceRequest} from '../../packages/common/src/maintenance-request-preview';
const draft:MaintenanceDraft={maintenanceId:'test',draftRevision:1,planState:'DRAFT',updatedAt:0,document:maintenanceDocumentSchema.parse({applicationId:'LASTRAIL',serviceScope:'GAME',webMode:null,title:'TEST_ONLY',summary:'request preview',noticeId:null,drainStartsAt:1000,startsAt:2000,estimatedEndAt:3000,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15})};
const read=(at:number,request:unknown,plan:MaintenanceDraft=draft)=>previewMaintenanceRequest(plan,at,request);
describe('hypothetical maintenance request decisions',()=>{
 it.each(['NEW_RUN','NEW_AD','NEW_PURCHASE'] as const)('blocks %s at drain, start and past estimated end',operation=>{
  expect(read(999,{operation}).decision).toBe('NOT_BLOCKED_BY_MAINTENANCE');
  for(const at of [1000,1999,2000,3000,9000])expect(read(at,{operation}).decision).toBe('BLOCKED_BY_MAINTENANCE');
 });
 it('continues only server-approved pre-drain runs and holds them at maintenance',()=>{
  expect(read(1000,{operation:'EXISTING_RUN_WRITE',admittedAt:999}).decision).toBe('NOT_BLOCKED_BY_MAINTENANCE');
  expect(read(1999,{operation:'EXISTING_RUN_WRITE',admittedAt:999}).decision).toBe('NOT_BLOCKED_BY_MAINTENANCE');
  for(const admittedAt of [1000,1001,1999])expect(read(1999,{operation:'EXISTING_RUN_WRITE',admittedAt}).decision).toBe('BLOCKED_BY_MAINTENANCE');
  expect(read(2000,{operation:'EXISTING_RUN_WRITE',admittedAt:999}).decision).toBe('BLOCKED_BY_MAINTENANCE');
 });
 it.each(['RESULT_READ','TRANSACTION_RECOVERY'] as const)('does not block %s or grant business authority',operation=>{
  for(const at of [999,1000,2000,9000])expect(read(at,{operation})).toMatchObject({kind:'HYPOTHETICAL_REQUEST',applied:false,decision:'NOT_BLOCKED_BY_MAINTENANCE',authorization:'EXISTING_CHECKS_REQUIRED',draftRevision:1,automaticCompletion:false});
  expect(maintenanceApplication()).toMatchObject({applied:false,status:'UNCONNECTED'});
 });
 it('blocks ordinary writes only at the maintenance boundary',()=>{
  expect(read(1999,{operation:'GAME_WRITE'}).decision).toBe('NOT_BLOCKED_BY_MAINTENANCE');
  expect(read(2000,{operation:'GAME_WRITE'}).decision).toBe('BLOCKED_BY_MAINTENANCE');
 });
 it('does not affect game operations for a WEB-only draft',()=>{
  const web={...draft,document:maintenanceDocumentSchema.parse({...draft.document,serviceScope:'WEB',webMode:'MAINTENANCE',drainStartsAt:null,existingRunPolicy:null})};
  expect(read(9000,{operation:'NEW_RUN'},web)).toMatchObject({effectiveGameMode:'NORMAL',decision:'NOT_BLOCKED_BY_MAINTENANCE'});
 });
 it('handles emergency start without inventing a drain period',()=>{
  const emergency={...draft,document:maintenanceDocumentSchema.parse({...draft.document,drainStartsAt:null,existingRunPolicy:'EMERGENCY_PRESERVE'})};
  expect(read(1999,{operation:'NEW_RUN'},emergency).decision).toBe('NOT_BLOCKED_BY_MAINTENANCE');
  expect(read(2000,{operation:'NEW_RUN'},emergency).decision).toBe('BLOCKED_BY_MAINTENANCE');
 });
 it.each([{operation:'UNKNOWN'},{operation:'EXISTING_RUN_WRITE'},{operation:'EXISTING_RUN_WRITE',admittedAt:-1},{operation:'EXISTING_RUN_WRITE',admittedAt:2001},{operation:'NEW_RUN',admittedAt:0},{operation:'RESULT_READ',authorized:true},null])('rejects invalid or ambiguous request %j',request=>{
  expect(()=>read(2000,request)).toThrow();
 });
 it.each([-1,NaN,Infinity,1.5,Number.MAX_SAFE_INTEGER+1])('rejects invalid evaluation time %s',at=>expect(()=>read(at,{operation:'NEW_RUN'})).toThrow());
 it('rejects invalid draft metadata and schedule rather than projecting normal',()=>{
  expect(()=>read(2000,{operation:'NEW_RUN'},{...draft,draftRevision:0})).toThrow();
  expect(()=>read(2000,{operation:'NEW_RUN'},{...draft,document:{...draft.document,startsAt:NaN}})).toThrow();
 });
 it('leaves the input draft unchanged',()=>{
  const before=JSON.stringify(draft);read(2000,{operation:'NEW_RUN'});expect(JSON.stringify(draft)).toBe(before);
 });
});
