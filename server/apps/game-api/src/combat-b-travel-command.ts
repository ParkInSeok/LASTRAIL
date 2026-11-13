import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {applyBTravelCommand,initialBTravelState,readPinnedBArrivalBoundary,readBArrivalDestination,bArrivalBoundaryReportSchema,bArrivalReportV2Version,verifyBArrivalBoundaryReport,type BTravelState} from './combat-b-arrival-source';
import {BArrivalReportStore} from './combat-b-arrival-report';
import type {BBoundaryArrivalReceipt} from './combat-b-arrival-source';
import {BOriginalArrivalStore,type BOriginalArrivalVerifier} from './combat-b-arrival-original';

export const bTravelCommandVersion='combat.b-travel-command.v1.20261031';
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe();
const base=z.object({version:z.literal(bTravelCommandVersion),requestId:id,destinationEventId:id,nodeId:id,mapRevision:nat.positive(),expectedRevision:nat});
const schema=z.discriminatedUnion('kind',[
 base.extend({kind:z.literal('TRAVEL_THROTTLE'),bps:nat.positive().max(13000)}).strict(),
 base.extend({kind:z.literal('PAUSE'),value:z.boolean()}).strict(),
 base.extend({kind:z.literal('RECOVER')}).strict(),base.extend({kind:z.literal('ARRIVE')}).strict()
]);
type Report=z.infer<typeof bArrivalBoundaryReportSchema>;
type Event={command:Report['travel']['commands'][number];state:BTravelState};
type Response={version:typeof bTravelCommandVersion;requestId:string;destinationEventId:string;nodeId:string;mapRevision:number;revision:number;acceptedAtMs:number;state:BTravelState;pauseIntent?:true};
type CurrentMap={version:typeof bTravelCommandVersion;requestId:string;status:'CURRENT_MAP';runStatus:string;mapRevision:number;mapState:Awaited<ReturnType<typeof readOriginalBRouteSource>>['state'];mapReceipt:unknown|null};
type Row={run_id:string;destination_event_id:string;revision:number;request_id:string;request_hash:string;accepted_at_ms:number;response_json:string;events_json:string;report_json:string|null};
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(s:string):never=>{throw new ActivityError(409,'combat_b_travel_'+s);};
const need=(v:boolean,s:string)=>{if(!v)fail(s);};
export class BTravelRevisionConflict extends ActivityError{
 constructor(readonly current:Response|null){super(409,'combat_b_travel_revision_conflict');}
}

/** Only collapse commands with no intervening movement; accepted receipts are never removed. */
export function compactBTravelEvents(initial:BTravelState,events:readonly Event[]){
 const compact:Event[]=[];
 for(const event of events){
  if(json(event.state)===json(compact.at(-1)?.state??initial))continue;
  const previous=compact.at(-1);
  if(previous&&event.command.kind!=='ADVANCE'&&previous.command.kind===event.command.kind)compact.pop();
  if(json(event.state)!==json(compact.at(-1)?.state??initial))compact.push(event);
 }
 return compact;
}

/** One immutable accepted-input stream per destination. Map state remains MapStore-owned. */
export class BTravelCommandStore {
 constructor(private readonly db:D1Database,private readonly clock:()=>number=Date.now){}
 private async finish(accountId:string,runId:string,row:Row):Promise<Response&{arrival?:BBoundaryArrivalReceipt}>{
  const response=JSON.parse(row.response_json) as Response;
  if(!row.report_json)return response;
  const report=bArrivalBoundaryReportSchema.parse(JSON.parse(row.report_json));
  const authority=new BOriginalArrivalStore(this.db,new BTravelArrivalVerifier(this.db));
  await authority.capture({sourceRef:row.request_id,binding:await travelBinding(this.db,accountId,runId,report)});
  const arrival=await new BArrivalReportStore(this.db,authority).submit(accountId,runId,report) as BBoundaryArrivalReceipt;
  return {...response,arrival};
 }
 async submit(accountId:string,runId:string,input:unknown):Promise<(Response&{arrival?:BBoundaryArrivalReceipt})|CurrentMap>{
  const c=schema.parse(input),requestHash=await sha256Hex(json(c));
  const current=await readOriginalBRouteSource(this.db,accountId,runId);
  const replay=async()=>{
   const row=await this.db.prepare('SELECT * FROM b_travel_commands WHERE run_id=? AND request_id=?').bind(runId,c.requestId).first<Row>();
   if(!row)return null;need(row.request_hash===requestHash,'request_conflict');return row;
  };
  const old=await replay();if(old)return this.finish(accountId,runId,old);
  if(current.row.status==='FINISH_PENDING')return fail('transaction_pending');
  if(c.kind==='RECOVER'&&(current.row.status!=='ACTIVE'||current.state.phase!=='TRAVELLING'||current.row.revision!==c.mapRevision)){
   const receipt=await this.db.prepare("SELECT response_json FROM operation_map_receipts WHERE run_id=? AND json_extract(response_json,'$.revision')=? LIMIT 1").bind(runId,current.row.revision).first<{response_json:string}>();
   return {version:bTravelCommandVersion,requestId:c.requestId,status:'CURRENT_MAP',runStatus:current.row.status,mapRevision:current.row.revision,mapState:current.state,mapReceipt:receipt?JSON.parse(receipt.response_json):null};
  }
  const p=await readPinnedBArrivalBoundary(this.db,accountId,runId,c.nodeId),destination=await readBArrivalDestination(this.db,p,c.destinationEventId);
  need(p.x.row.status==='ACTIVE'&&p.x.state.phase==='TRAVELLING'&&p.x.row.revision===c.mapRevision,'map_conflict');
  const origin=await this.db.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=? AND event_id=?').bind(runId,c.destinationEventId).first<{confirmed_at_ms:number}>();
  if(!origin)return fail('time_source_missing');
  const head=await this.db.prepare('SELECT revision,journal_json FROM b_travel_heads WHERE run_id=? AND destination_event_id=?').bind(runId,c.destinationEventId).first<{revision:number;journal_json:string}>();
  const last=head?await this.db.prepare('SELECT * FROM b_travel_commands WHERE run_id=? AND destination_event_id=? AND revision=?').bind(runId,c.destinationEventId,head.revision).first<Row>():null;
  need(!head||!!last,'head_conflict');
  if((last?.revision??0)!==c.expectedRevision)throw new BTravelRevisionConflict(last?JSON.parse(last.response_json) as Response:null);
  need(!last?.report_json,'arrival_already_prepared');
  const acceptedAtMs=nat.parse(this.clock()),previousAt=last?.accepted_at_ms??origin.confirmed_at_ms;
  need(acceptedAtMs>=previousAt,'clock_regressed');
  const initial=initialBTravelState(p.source.definition.configuration.throttleCruiseBps);
  let state:BTravelState=last?(JSON.parse(last.response_json) as Response).state:initial;
  const events:Event[]=[],prior:Event[]=head?JSON.parse(head.journal_json):[];
  need(prior.length<=1000&&json(prior.at(-1)?.state??initial)===json(state),'head_conflict');
  const eventPrefix='TRAVEL:'+await sha256Hex(json({runId,requestId:c.requestId}));
  const apply=(command:Event['command'])=>{state=applyBTravelCommand(p,state,command);events.push({command,state:{...state}});};
  // Recovery discards the unconfirmed interval. A paused interval supplies no time credit.
  if(c.kind!=='RECOVER'&&!state.paused){
   const elapsed=nat.parse((acceptedAtMs-previousAt)*10000);
   // Use the existing executor's bounded step batches, never a new speed or distance rule.
   let remainder=elapsed;
   do{const span=Math.min(remainder,p.source.definition.stepTicks*10000);apply({kind:'ADVANCE',eventId:eventPrefix+':'+events.length,elapsed:span,maximumSteps:10000});remainder-=span;
    if(prior.length+events.length>=1000&&remainder>0&&state.distanceMm<p.boundary.distanceMm)return fail('history_capacity');
   }while(remainder>0&&state.distanceMm<p.boundary.distanceMm);
  }
  const pauseAtArrival=c.kind==='PAUSE'&&c.value&&state.distanceMm>=p.boundary.distanceMm;
  const issuesArrival=c.kind==='ARRIVE'||pauseAtArrival;
  if(issuesArrival)need(state.distanceMm>=p.boundary.distanceMm&&!state.paused,'boundary_not_reached');
  else{
   need(state.distanceMm<p.boundary.distanceMm,'arrival_report_required');
   if(c.kind==='TRAVEL_THROTTLE'){need(c.bps!==state.throttleBps,'unchanged_input');apply({kind:c.kind,eventId:eventPrefix+':input',bps:c.bps});}
   else{const value=c.kind==='RECOVER'?true:c.value;if(c.kind==='PAUSE')need(value!==state.paused,'unchanged_input');apply({kind:'PAUSE',eventId:eventPrefix+':input',value});}
  }
  const all=compactBTravelEvents(initial,[...prior,...events]);need(all.length<=1000,'history_capacity');
  if(!issuesArrival&&c.kind!=='RECOVER'&&!(c.kind==='PAUSE'&&!c.value)){
   const remaining=BigInt(p.boundary.distanceMm-state.distanceMm)*100000000000n-BigInt(state.remainderUnits);
   const minimumStep=BigInt(p.source.definition.stepTicks)*BigInt(p.source.definition.configuration.baseSpeedMillimetersPerSecond)*BigInt(p.source.definition.configuration.throttleMinimumBps);
   const completionBatches=(remaining+minimumStep*10000n-1n)/(minimumStep*10000n);
   // Leave space for recovery/resume and all remaining movement at the slowest allowed speed.
   need(BigInt(all.length)+completionBatches+2n<=1000n,'history_capacity');
  }
  let report:Report|null=null;
  if(issuesArrival){
   const commandId=all.at(-1)!.command.eventId;
   const eventId='B_ARRIVE:'+await sha256Hex(json({runId,nodeId:c.nodeId,destinationEventId:c.destinationEventId,boundarySourceHash:p.source.sourceHash,commandId,tick:state.tick}));
   const travel={version:'combat.b-arrival-travel-observation.v1.20261020',accountId,runId,nodeId:c.nodeId,nativeContextId:p.nativeContextId,boundarySourceHash:p.source.sourceHash,nodeSourceHash:p.nodeSourceHash,
    commands:all.map(e=>e.command),results:all.map(e=>e.state),arrival:{eventId,commandId,commandSequence:all.length,tick:state.tick}};
   report=bArrivalBoundaryReportSchema.parse({version:bArrivalReportV2Version,requestId:c.requestId,reportOriginMapRevision:c.mapRevision,destinationEventId:c.destinationEventId,checkpointHash:'sha256:'+await sha256Hex(json(travel)),travel});
   await verifyBArrivalBoundaryReport(p,report);
  }
  need(c.expectedRevision<Number.MAX_SAFE_INTEGER,'revision_exhausted');
  const response:Response={version:bTravelCommandVersion,requestId:c.requestId,destinationEventId:c.destinationEventId,nodeId:c.nodeId,mapRevision:c.mapRevision,revision:c.expectedRevision+1,acceptedAtMs,state,...(pauseAtArrival?{pauseIntent:true as const}:{})};
  const row:Row={run_id:runId,destination_event_id:c.destinationEventId,revision:response.revision,request_id:c.requestId,request_hash:requestHash,accepted_at_ms:acceptedAtMs,response_json:json(response),events_json:json(events),report_json:report?json(report):null};
  try{await this.db.batch([
   this.db.prepare(`INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN operation_map_states m ON m.run_id=r.id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND m.revision=? AND m.state_json=? AND m.plan_json=? AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json)
    AND EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?)
    AND COALESCE((SELECT MAX(revision) FROM b_travel_commands WHERE run_id=? AND destination_event_id=?),0)=?
    AND NOT EXISTS(SELECT 1 FROM b_arrival_reports WHERE run_id=? AND json_extract(receipt_json,'$.observation.destination.eventId')=?) THEN 1 ELSE 0 END`)
    .bind(runId,accountId,c.mapRevision,p.x.row.state_json,p.x.row.plan_json,p.x.row.snapshot_json,runId,c.destinationEventId,destination.response_json,runId,c.destinationEventId,c.expectedRevision,runId,c.destinationEventId),
   this.db.prepare('INSERT INTO b_travel_commands VALUES(?,?,?,?,?,?,?,?,?)').bind(row.run_id,row.destination_event_id,row.revision,row.request_id,row.request_hash,row.accepted_at_ms,row.response_json,row.events_json,row.report_json),
   this.db.prepare('INSERT INTO b_travel_heads VALUES(?,?,?,?) ON CONFLICT(run_id,destination_event_id) DO UPDATE SET revision=excluded.revision,journal_json=excluded.journal_json').bind(runId,c.destinationEventId,row.revision,json(all)),
   this.db.prepare('DELETE FROM operation_map_write_guard')
  ]);}catch(error){const winner=await replay();if(winner)return this.finish(accountId,runId,winner);if(/CHECK constraint|UNIQUE constraint/.test(String(error)))return fail('state_conflict');throw error;}
  return this.finish(accountId,runId,row);
 }
}

async function travelBinding(db:D1Database,accountId:string,runId:string,report:Report){
 // A saved report's source hashes are immutable START references; capture independently verifies them.
 const x=await readOriginalBRouteSource(db,accountId,runId);
 if(!x.pin)return fail('source_missing');
 return {accountId,runId,nodeId:report.travel.nodeId,mapRevision:report.reportOriginMapRevision,checkpointHash:report.checkpointHash,
  originalEventId:report.travel.arrival.eventId,sequence:report.travel.arrival.commandSequence,tick:report.travel.arrival.tick,destinationEventId:report.destinationEventId,departurePinHash:x.pin.pinHash,nodeSourceHash:report.travel.nodeSourceHash};
}

/** Authority comes only from this server's immutable accepted stream, never a submitted client history. */
export class BTravelArrivalVerifier implements BOriginalArrivalVerifier{
 constructor(private readonly db:D1Database){}
 async prepare(input:Parameters<BOriginalArrivalVerifier['prepare']>[0]){
  const b=input.binding,row=await this.db.prepare('SELECT * FROM b_travel_commands WHERE run_id=? AND request_id=?').bind(b.runId,input.sourceRef).first<Row>();
  if(!row?.report_json)return fail('accepted_arrival_missing');
  const report=bArrivalBoundaryReportSchema.parse(JSON.parse(row.report_json));
  need(json(await travelBinding(this.db,b.accountId,b.runId,report))===json(b),'arrival_binding_conflict');
  const p=await readPinnedBArrivalBoundary(this.db,b.accountId,b.runId,b.nodeId);
  await verifyBArrivalBoundaryReport(p,report);
  const state=report.travel.results.at(-1)!;
  return {proof:{binding:b,sourceRef:input.sourceRef,sourceRevision:row.revision,clockCheckpointRef:report.checkpointHash,
   clock:{authority:'LOCAL_LOGICAL',stepTicks:p.source.definition.stepTicks,debtHalfTicks:state.debtTicks*2,completedSteps:state.tick,speedHalfUnits:2,paused:false}},
   assertions:[this.db.prepare('INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM b_travel_commands WHERE run_id=? AND request_id=? AND report_json=? AND revision=?) AND (SELECT MAX(revision) FROM b_travel_commands WHERE run_id=? AND destination_event_id=?)=? THEN 1 ELSE 0 END').bind(b.runId,input.sourceRef,row.report_json,row.revision,b.runId,b.destinationEventId,row.revision)]};
 }
}
