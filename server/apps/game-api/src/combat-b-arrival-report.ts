import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {readBNativeNodeSource} from './combat-b-native-node-source';
import {mapReceipt} from './operation-map-state';
import type {BOriginalVerifiedObservationOwner} from './combat-b-original-accepted-broker';
import {bArrivalReportV2Version,bArrivalBoundaryReportSchema,bBoundaryArrivalObservationSchema,readPinnedBArrivalBoundary,verifyBArrivalBoundaryReport,readBArrivalDestination,type BBoundaryArrivalReceipt} from './combat-b-arrival-source';

export const bArrivalReportVersion='combat.b-arrival-report.v1.20261019';
export const bArrivalBodyLimit=1800000;
export class BArrivalBodyError extends Error {readonly status=413;readonly code='combat_b_arrival_body_too_large';constructor(){super('combat_b_arrival_body_too_large');}}
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),rev=nat.min(1),digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(s:string):never=>{throw new ActivityError(409,'combat_b_arrival_'+s);};
const need=(v:boolean,s:string)=>{if(!v)fail(s);};
const runtime=z.object({version:z.enum(['unity.combat-b.travel-native-local.v1.20261005','unity.combat-b.travel-native-local.v2.20261006']),bindingHash:digest,sequence:rev,waveSourceHash:hash,
 commands:z.array(z.object({kind:id,eventId:id}).passthrough()).min(1).max(10000),results:z.array(z.object({tick:nat,outcome:z.enum(['RUNNING','VICTORY','DEFEAT'])}).passthrough()).min(1).max(10000),
 state:z.object({direct:z.object({clock:z.object({completedSteps:nat,stepTicks:rev,paused:z.boolean()}).passthrough()}).passthrough()}).passthrough(),
 travel:z.object({version:z.literal('unity.combat-b.travel-motion-local.v1.20261005'),provenance:z.literal('EXPLICIT_ORIGINAL_RAIL_MODEL_LOCAL_NOT_OPERATING_B_SPEED'),origin:z.json(),nativeContextId:id,nodeId:id,nodeSourceHash:hash,bindingHash:digest,configurationHash:digest,stepTicks:rev,commandRevision:rev,sequence:rev,stopped:z.boolean(),stopReason:z.enum(['PAUSED','TERMINAL']).nullable(),motion:z.object({currentTick:nat}).passthrough(),routeSelectionSupported:z.literal(false),operatingSpeedSourceSupported:z.literal(false)}).passthrough()
}).passthrough();
export const bArrivalReportSchema=z.strictObject({version:z.literal(bArrivalReportVersion),requestId:id,reportOriginMapRevision:rev,destinationEventId:id,checkpointHash:digest,nodeSession:z.object({version:z.literal('unity.combat-b.node-controller-local.v1.20261005'),accountId:id,runId:id,localRevision:nat,node:z.json(),runtime}).passthrough()});
export const bArrivalMissingSources=[
 'ORIGINAL_TRAVEL_ARRIVAL_EVENT_ID_SEQUENCE_AND_CHECKPOINT',
 'ORIGINAL_TRAVEL_DESTINATION_RECEIPT_BINDING',
 'OPERATING_B_TRAVEL_CONFIGURATION_AND_ARRIVAL_BOUNDARY_OWNER'
] as const;
export type BArrivalReceipt={version:typeof bArrivalReportVersion;evidenceRef:string;requestId:string;nodeId:string;reportOriginMapRevision:number;checkpointHash:string;originalCommandId:string;commandRevision:number;nativeSequence:number;verificationLevel:'STRUCTURE_SOURCE_CHECKPOINT_CONSISTENCY';combatTruth:'NOT_PROVEN';arrivalAccepted:false;progressAuthorized:false;missingSources:string[]};
type Row={account_id:string;request_hash:string;report_json:string;receipt_json:string;evidence_ref:string};
/** Internal original clock/event owner, never a client flag or digest validator.
 * Must independently establish elapsed credit and accepted event, and fence that
 * original authority in the same transaction. Replay alone is insufficient. */
export interface BArrivalOriginalAuthority {
 prepare(input:{accountId:string;runId:string;nodeId:string;mapRevision:number;checkpointHash:string;originalEventId:string;sequence:number;tick:number;destinationEventId:string;departurePinHash:string;nodeSourceHash:string}):Promise<{
  binding:Parameters<BArrivalOriginalAuthority['prepare']>[0];assertions:readonly D1PreparedStatement[];
 }>;
}
async function authorizeArrival(owner:BArrivalOriginalAuthority|undefined,p:Awaited<ReturnType<typeof readPinnedBArrivalBoundary>>,v:Awaited<ReturnType<typeof verifyBArrivalBoundaryReport>>){
 if(!owner)return fail('original_time_credit_and_accepted_event_owner_missing');
 const b=v.report,input={accountId:b.travel.accountId,runId:b.travel.runId,nodeId:b.travel.nodeId,mapRevision:b.reportOriginMapRevision,checkpointHash:b.checkpointHash,originalEventId:v.originalEventId,sequence:v.sequence,tick:v.tick,destinationEventId:b.destinationEventId,departurePinHash:p.departurePinHash,nodeSourceHash:p.nodeSourceHash};
 const proof=await owner.prepare(input);
 need(json(proof.binding)===json(input)&&proof.assertions.length>0&&proof.assertions.length<=120&&proof.assertions.every(s=>typeof s.bind==='function'),'original_authority_conflict');
 return proof.assertions;
}
/** Existing native capture supplies accepted commands, not an accepted arrival.
 * This durable boundary deliberately cannot manufacture that missing producer. */
export class BArrivalReportStore{
 constructor(private readonly db:D1Database,private readonly authority?:BArrivalOriginalAuthority){}
 private async recover(runId:string,row:Row){
  if(await this.db.prepare('SELECT 1 FROM b_arrival_report_revocations WHERE run_id=? AND evidence_ref=?').bind(runId,row.evidence_ref).first())return fail('revoked');
  need(await sha256Hex(row.report_json)===row.request_hash,'stored_report_conflict');
  return {report:z.union([bArrivalReportSchema,bArrivalBoundaryReportSchema]).parse(JSON.parse(row.report_json)),receipt:JSON.parse(row.receipt_json) as BArrivalReceipt|BBoundaryArrivalReceipt};
 }
 async read(accountId:string,runId:string,evidenceRef:string){
  id.parse(evidenceRef);await readOriginalBRouteSource(this.db,accountId,runId);
  const row=await this.db.prepare('SELECT * FROM b_arrival_reports WHERE run_id=? AND account_id=? AND evidence_ref=?').bind(runId,accountId,evidenceRef).first<Row>();
  return row?this.recover(runId,row):null;
 }
 async submit(accountId:string,runId:string,input:unknown):Promise<BArrivalReceipt|BBoundaryArrivalReceipt>{
  if(z.object({version:z.literal(bArrivalReportV2Version)}).safeParse(input).success)return this.submitBoundary(accountId,runId,input);
  const b=bArrivalReportSchema.parse(input),text=json(b);if(new TextEncoder().encode(text).length>bArrivalBodyLimit)throw new BArrivalBodyError();
  const x=await readOriginalBRouteSource(this.db,accountId,runId),requestHash=await sha256Hex(text);
  const replay=async()=>{const row=await this.db.prepare('SELECT * FROM b_arrival_reports WHERE run_id=? AND account_id=? AND request_id=?').bind(runId,accountId,b.requestId).first<Row>();if(!row)return null;need(row.request_hash===requestHash,'request_conflict');return (await this.recover(runId,row)).receipt;};
  const old=await replay();if(old)return old;
  need(x.row.status==='ACTIVE'&&x.row.revision===b.reportOriginMapRevision&&x.state.phase==='TRAVELLING','state_conflict');
  const p=await readBNativeNodeSource(this.db,accountId,runId),n=b.nodeSession,r=n.runtime,t=r.travel,clock=r.state.direct.clock,last=r.results.at(-1)!;
  need(n.accountId===accountId&&n.runId===runId,'account_conflict');
  need(json(n.node)===json(p.originalNodeSource)&&r.waveSourceHash===p.originalNodeSource.sourceHash&&t.nodeSourceHash===r.waveSourceHash&&t.nodeId===p.nodeId&&t.nativeContextId===p.nativeContextId&&json(t.origin)===json(p.originalNodeSource.origin),'source_conflict');
  need(b.checkpointHash==='sha256:'+await sha256Hex(json(r)),'checkpoint_hash_conflict');
  need(r.commands.length===r.results.length&&t.commandRevision===r.commands.length&&t.sequence===r.sequence&&t.bindingHash===r.bindingHash&&t.stepTicks===clock.stepTicks&&t.motion.currentTick===clock.completedSteps&&last.tick===clock.completedSteps&&new Set(r.commands.map(c=>c.eventId)).size===r.commands.length,'cursor_conflict');
  need(r.results.every((v,i)=>i===0||v.tick>=r.results[i-1]!.tick),'sequence_conflict');
  need(t.stopped===(clock.paused||last.outcome!=='RUNNING')&&t.stopReason===(clock.paused?'PAUSED':last.outcome!=='RUNNING'?'TERMINAL':null),'stop_conflict');
  const destination=await this.db.prepare('SELECT response_json FROM operation_map_receipts WHERE run_id=? AND event_id=?').bind(runId,b.destinationEventId).first<{response_json:string}>();
  if(!destination)return fail('destination_missing');
  const receipt=mapReceipt.parse(JSON.parse(destination.response_json));
  need(receipt.eventId===b.destinationEventId&&receipt.revision===b.reportOriginMapRevision&&receipt.verificationId===null&&json(receipt.state)===json(x.state)&&receipt.state.pendingEdgeId===x.state.pendingEdgeId,'destination_conflict');
  need(x.plan.edges.some(e=>e.edgeId===x.state.pendingEdgeId&&e.toNodeId===p.nodeId),'destination_conflict');
  const evidenceRef='B_ARRIVAL:'+crypto.randomUUID(),result:BArrivalReceipt={version:bArrivalReportVersion,evidenceRef,requestId:b.requestId,nodeId:p.nodeId,reportOriginMapRevision:b.reportOriginMapRevision,checkpointHash:b.checkpointHash,originalCommandId:r.commands.at(-1)!.eventId,commandRevision:t.commandRevision,nativeSequence:r.sequence,verificationLevel:'STRUCTURE_SOURCE_CHECKPOINT_CONSISTENCY',combatTruth:'NOT_PROVEN',arrivalAccepted:false,progressAuthorized:false,missingSources:[...bArrivalMissingSources]};
  try{await this.db.batch([
   this.db.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END").bind(runId,accountId,x.row.snapshot_json,x.row.revision,x.row.state_json,x.row.plan_json),
   this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END').bind(runId,b.destinationEventId,destination.response_json),
   this.db.prepare('INSERT INTO b_arrival_reports VALUES(?,?,?,?,?,?,?)').bind(runId,accountId,evidenceRef,b.requestId,requestHash,text,json(result)),
   this.db.prepare('DELETE FROM operation_map_write_guard')
  ]);}catch(e){const recovered=await replay();if(recovered)return recovered;if(String(e).includes('CHECK constraint failed'))return fail('state_conflict');throw e;}
  return result;
 }
 private async submitBoundary(accountId:string,runId:string,input:unknown):Promise<BBoundaryArrivalReceipt>{
  const b=bArrivalBoundaryReportSchema.parse(input),text=json(b);if(new TextEncoder().encode(text).length>bArrivalBodyLimit)throw new BArrivalBodyError();
  await readOriginalBRouteSource(this.db,accountId,runId);const requestHash=await sha256Hex(text);
  const replay=async()=>{const row=await this.db.prepare('SELECT * FROM b_arrival_reports WHERE run_id=? AND account_id=? AND request_id=?').bind(runId,accountId,b.requestId).first<Row>();if(!row)return null;need(row.request_hash===requestHash,'request_conflict');return (await this.recover(runId,row)).receipt as BBoundaryArrivalReceipt;};
  const old=await replay();if(old)return old;
  const p=await readPinnedBArrivalBoundary(this.db,accountId,runId,b.travel.nodeId),v=await verifyBArrivalBoundaryReport(p,b),destination=await readBArrivalDestination(this.db,p,b.destinationEventId),x=p.x;
  const authorityAssertions=await authorizeArrival(this.authority,p,v);
  const evidenceRef='B_ARRIVAL:'+crypto.randomUUID();
  const observation=bBoundaryArrivalObservationSchema.parse({version:'combat.b-boundary-arrival-observation.v1.20261020',accountId,runId,nodeId:b.travel.nodeId,mapRevision:b.reportOriginMapRevision,
   nativeContextId:p.nativeContextId,departurePinHash:p.departurePinHash,nodeSourceHash:p.nodeSourceHash,operationBindingHash:x.binding.bindingHash,evidenceRef,originalEventId:v.originalEventId,sequence:v.sequence,sourceRevision:v.sequence,checkpointHash:b.checkpointHash,
   destination:{eventId:b.destinationEventId,mapRevision:b.reportOriginMapRevision,receiptHash:await sha256Hex(destination.response_json),edgeId:p.boundary.edgeId},arrival:{eventId:v.originalEventId,sequence:v.sequence,tick:v.tick},travelStopped:true,combatTruth:'NOT_PROVEN'});
  const result:BBoundaryArrivalReceipt={version:bArrivalReportV2Version,evidenceRef,requestId:b.requestId,arrivalAccepted:true,progressAuthorized:true,combatTruth:'NOT_PROVEN',verificationLevel:'ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY',observation,missingSources:[]};
  try{await this.db.batch([
   ...authorityAssertions,
   this.db.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END").bind(runId,accountId,x.row.snapshot_json,x.row.revision,x.row.state_json,x.row.plan_json),
   this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END').bind(runId,b.destinationEventId,destination.response_json),
   this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM b_arrival_reports WHERE run_id=? AND json_extract(receipt_json,\'$.observation.destination.eventId\')=?) THEN 1 ELSE 0 END').bind(runId,b.destinationEventId),
   this.db.prepare('INSERT INTO b_arrival_reports VALUES(?,?,?,?,?,?,?)').bind(runId,accountId,evidenceRef,b.requestId,requestHash,text,json(result)),this.db.prepare('DELETE FROM operation_map_write_guard')
  ]);}catch(e){const recovered=await replay();if(recovered)return recovered;if(String(e).includes('CHECK constraint failed'))return fail('state_or_original_event_conflict');throw e;}
  return result;
 }
}
/** Runtime dispatch is installed now; current producer versions fail closed. A future
 * supported producer needs a distinct validated version and same-batch source fences. */
export class BArrivalConsistencyOwner implements BOriginalVerifiedObservationOwner{
 constructor(private readonly db:D1Database,private readonly authority?:BArrivalOriginalAuthority){}
 async prepare(c:Parameters<BOriginalVerifiedObservationOwner['prepare']>[0]):ReturnType<BOriginalVerifiedObservationOwner['prepare']>{
  if(c.kind!=='ARRIVE')return {status:'UNSUPPORTED',missingSources:[c.kind+'_ORIGINAL_ACCEPTED_EVENT']};
  if(!c.evidenceRef)return {status:'UNSUPPORTED',missingSources:['ORIGINAL_ARRIVAL_REPORT_REFERENCE']};
  const saved=await new BArrivalReportStore(this.db).read(c.accountId,c.runId,c.evidenceRef);
  if(!saved)return {status:'UNSUPPORTED',missingSources:['ORIGINAL_ARRIVAL_REPORT_REFERENCE']};
  if(saved.receipt.version===bArrivalReportV2Version){
   const observation=bBoundaryArrivalObservationSchema.parse(saved.receipt.observation);
   need(observation.nodeId===c.nodeId&&observation.mapRevision===c.mapRevision,'scope_conflict');
   const p=await readPinnedBArrivalBoundary(this.db,c.accountId,c.runId,c.nodeId);
   const verified=await verifyBArrivalBoundaryReport(p,saved.report);
   const authorityAssertions=await authorizeArrival(this.authority,p,verified);
   const destination=await readBArrivalDestination(this.db,p,observation.destination.eventId);
   need(observation.destination.receiptHash===await sha256Hex(destination.response_json),'destination_conflict');
   const assertions=[
    ...authorityAssertions,
    this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM b_arrival_reports WHERE run_id=? AND account_id=? AND evidence_ref=? AND report_json=? AND receipt_json=?) AND NOT EXISTS(SELECT 1 FROM b_arrival_report_revocations WHERE run_id=? AND evidence_ref=?) THEN 1 ELSE 0 END').bind(c.runId,c.accountId,c.evidenceRef,json(saved.report),json(saved.receipt),c.runId,c.evidenceRef),
    this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END').bind(c.runId,observation.destination.eventId,destination.response_json)
   ];return {status:'VERIFIED',observation,assertions};
  }
  need(saved.receipt.nodeId===c.nodeId&&saved.receipt.reportOriginMapRevision===c.mapRevision,'scope_conflict');
  return {status:'UNSUPPORTED',missingSources:[...bArrivalMissingSources]};
 }
}
