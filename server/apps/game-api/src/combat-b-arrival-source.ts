import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {captureOperationMap,type OperationMapPlan} from './operation-map-plan';
import {readBDepartureCombatPin,type BDepartureCombatContext} from './combat-b-departure-combat-pin';
import {mapReceipt} from './operation-map-state';

const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),rev=nat.min(1),hash=z.string().regex(/^[a-f0-9]{64}$/u),digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),fail=(s:string):never=>{throw new ActivityError(409,'combat_b_arrival_'+s);},need=(v:boolean,s:string)=>{if(!v)fail(s);};
export const bArrivalBoundarySourceVersion='combat.b-arrival-boundaries.v1.20261020';
export const bArrivalReportV2Version='combat.b-arrival-report.v2.20261020';
const boundarySchema=z.strictObject({edgeId:id,fromNodeId:id,toNodeId:id,distanceMm:rev});
// Exact existing rail-journey model, without A's offers/effects. Values MUST come
// from the operating source owner: no CE03 speed or invented B node distance.
const definitionSchema=z.strictObject({definitionId:id,definitionVersion:id,stepTicks:rev,
 executorVersion:z.literal('combat.b-arrival-linear-fixed-step.v1.20261020'),speedHalfUnits:z.literal(2),
 configuration:z.strictObject({version:z.literal('rail-journey-v1'),selectionScaleBps:rev.max(10000),recoveryMs:rev.max(10000),baseSpeedMillimetersPerSecond:rev.max(1000000),throttleMinimumBps:rev.max(10000),throttleMaximumBps:z.number().int().min(10000).max(13000),throttleCruiseBps:rev.max(13000),offers:z.array(z.never()).length(0)}),
 boundaries:z.array(boundarySchema).min(1).max(1000)
}).refine(d=>d.configuration.throttleCruiseBps>=d.configuration.throttleMinimumBps&&d.configuration.throttleCruiseBps<=d.configuration.throttleMaximumBps);
const sourceSchema=z.strictObject({version:z.literal(bArrivalBoundarySourceVersion),origin:z.record(z.string(),z.json()),definition:definitionSchema,sourceHash:hash});
export async function prepareBArrivalBoundarySource(context:BDepartureCombatContext,plan:OperationMapPlan,input:unknown){
 const definition=definitionSchema.parse(input),p=captureOperationMap(plan);
 need(context.planHash===await sha256Hex(json(p)),'boundary_plan_conflict');
 need(definition.boundaries.length===p.edges.length&&new Set(definition.boundaries.map(b=>b.edgeId)).size===p.edges.length,'boundary_coverage_conflict');
 for(const b of definition.boundaries)need(p.edges.some(e=>e.edgeId===b.edgeId&&e.fromNodeId===b.fromNodeId&&e.toNodeId===b.toNodeId),'boundary_edge_conflict');
 const body={version:bArrivalBoundarySourceVersion,origin:context,definition};return sourceSchema.parse({...body,sourceHash:await sha256Hex(json(body))});
}
/** Reads only original START pin. New definitions never retrofit historical runs.
 * Caller must also fence x.row snapshot/map bytes in the consuming transaction. */
export async function readPinnedBArrivalBoundary(db:D1Database,accountId:string,runId:string,nodeId:string){
 const x=await readOriginalBRouteSource(db,accountId,runId),raw=x.pin?.source.profile.definition.bArrivalBoundarySource;
 if(!x.pin||!raw)return fail('operating_boundary_source_missing');
 const source=sourceSchema.parse(raw),rebuilt=await prepareBArrivalBoundarySource(x.pin.origin,x.plan,source.definition);
 need(json(source)===json(rebuilt),'boundary_source_conflict');
 const boundary=source.definition.boundaries.find(b=>b.edgeId===x.state.pendingEdgeId&&b.toNodeId===nodeId);
 if(!boundary)return fail('boundary_destination_conflict');
 const nodeSourceHash=await sha256Hex(json({origin:x.pin.origin,boundarySourceHash:source.sourceHash,boundary}));
 return {x,source,boundary,nativeContextId:x.pin.source.contextId,departurePinHash:x.pin.pinHash,nodeSourceHash};
}
const command=z.discriminatedUnion('kind',[
 z.strictObject({kind:z.literal('ADVANCE'),eventId:id,elapsed:nat,maximumSteps:nat.max(10000)}),
 z.strictObject({kind:z.literal('PAUSE'),eventId:id,value:z.boolean()}),
 z.strictObject({kind:z.literal('TRAVEL_THROTTLE'),eventId:id,bps:rev.max(13000)})
]);
const result=z.strictObject({tick:nat,distanceMm:nat,remainderUnits:nat,debtTicks:nat,paused:z.boolean(),throttleBps:rev});
export type BTravelState=z.infer<typeof result>;
export const initialBTravelState=(throttleBps:number):BTravelState=>({tick:0,distanceMm:0,remainderUnits:0,debtTicks:0,paused:false,throttleBps});
export async function prepareBTravelInitial(input:{startSnapshot:string;accountId:string;runId:string;edgeId:string;eventId:string;mapRevision:number;acceptedAtMs:number;plan:OperationMapPlan}){
 const pin=await readBDepartureCombatPin(input.startSnapshot,{accountId:input.accountId,runId:input.runId});
 const raw=pin?.source.profile.definition.bArrivalBoundarySource;if(!pin||!raw)return undefined;
 const source=sourceSchema.parse(raw),rebuilt=await prepareBArrivalBoundarySource(pin.origin,input.plan,source.definition);
 need(json(source)===json(rebuilt),'boundary_source_conflict');
 const boundary=source.definition.boundaries.find(b=>b.edgeId===input.edgeId);if(!boundary)return fail('boundary_destination_conflict');
 return {version:'combat.b-travel-command.v1.20261031',requestId:input.eventId,destinationEventId:input.eventId,nodeId:boundary.toNodeId,mapRevision:input.mapRevision,revision:0,acceptedAtMs:input.acceptedAtMs,
  state:initialBTravelState(source.definition.configuration.throttleCruiseBps)};
}
export function applyBTravelCommand(p:Awaited<ReturnType<typeof readPinnedBArrivalBoundary>>,state:BTravelState,input:z.infer<typeof command>):BTravelState{
 const c=p.source.definition.configuration,step=BigInt(p.source.definition.stepTicks),den=100000000000n;
 let tick=BigInt(state.tick),distance=BigInt(state.distanceMm)*den+BigInt(state.remainderUnits),debt=BigInt(state.debtTicks),paused=state.paused,throttle=state.throttleBps;
 const boundary=BigInt(p.boundary.distanceMm)*den;
 need(distance<boundary,'commands_after_arrival');
 if(input.kind==='PAUSE')paused=input.value;
 else if(input.kind==='TRAVEL_THROTTLE'){need(!paused,'throttle_paused');need(input.bps>=c.throttleMinimumBps&&input.bps<=c.throttleMaximumBps,'throttle_conflict');throttle=input.bps;}
 else if(!paused){
  debt+=BigInt(input.elapsed);const perStep=step*BigInt(c.baseSpeedMillimetersPerSecond)*BigInt(throttle),untilBoundary=(boundary-distance+perStep-1n)/perStep;
  const steps=[debt/step,BigInt(input.maximumSteps),untilBoundary].reduce((a,b)=>a<b?a:b);
  tick+=steps;debt-=steps*step;distance+=steps*perStep;
 }
 return result.parse({tick:Number(tick),distanceMm:Number(distance/den),remainderUnits:Number(distance%den),debtTicks:Number(debt),paused,throttleBps:throttle});
}
export const bArrivalBoundaryReportSchema=z.strictObject({version:z.literal(bArrivalReportV2Version),requestId:id,reportOriginMapRevision:rev,destinationEventId:id,checkpointHash:digest,
 travel:z.strictObject({version:z.literal('combat.b-arrival-travel-observation.v1.20261020'),accountId:id,runId:id,nodeId:id,nativeContextId:id,boundarySourceHash:hash,nodeSourceHash:hash,
 commands:z.array(command).min(1).max(1000),results:z.array(result).min(1).max(1000),
 arrival:z.strictObject({eventId:id,commandId:id,commandSequence:rev,tick:rev})})});
export const bBoundaryArrivalObservationSchema=z.strictObject({version:z.literal('combat.b-boundary-arrival-observation.v1.20261020'),accountId:id,runId:id,nodeId:id,mapRevision:rev,
 nativeContextId:id,departurePinHash:hash,nodeSourceHash:hash,operationBindingHash:hash,evidenceRef:id,originalEventId:id,sequence:rev,sourceRevision:rev,checkpointHash:digest,
 destination:z.strictObject({eventId:id,mapRevision:rev,receiptHash:hash,edgeId:id}),arrival:z.strictObject({eventId:id,sequence:rev,tick:rev}),travelStopped:z.literal(true),combatTruth:z.literal('NOT_PROVEN')});
export type BBoundaryArrivalObservation=z.infer<typeof bBoundaryArrivalObservationSchema>;
export type BBoundaryArrivalReceipt={version:typeof bArrivalReportV2Version;evidenceRef:string;requestId:string;arrivalAccepted:true;progressAuthorized:true;combatTruth:'NOT_PROVEN';verificationLevel:'ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY';observation:BBoundaryArrivalObservation;missingSources:[]};

/** Deterministic travel-only replay of the source-pinned original fixed step model.
 * Checks source bindings, supplied time-credit arithmetic, every command/result
 * and the FIRST boundary crossing. Client elapsed is NOT independently proven:
 * the consuming store requires the separate original clock/accepted-event owner.
 * This version supports explicit linear rail travel only; no combat truth claim. */
export async function verifyBArrivalBoundaryReport(p:Awaited<ReturnType<typeof readPinnedBArrivalBoundary>>,input:unknown){
 const b=bArrivalBoundaryReportSchema.parse(input),t=b.travel,c=p.source.definition.configuration;
 need(t.accountId===p.x.pin!.origin.accountId&&t.runId===p.x.pin!.origin.runId&&t.nodeId===p.boundary.toNodeId&&t.nativeContextId===p.nativeContextId&&t.boundarySourceHash===p.source.sourceHash&&t.nodeSourceHash===p.nodeSourceHash,'boundary_report_scope_conflict');
 need(b.reportOriginMapRevision===p.x.row.revision&&p.x.state.phase==='TRAVELLING'&&p.x.row.status==='ACTIVE','state_conflict');
 need(b.checkpointHash==='sha256:'+await sha256Hex(json(t)),'checkpoint_hash_conflict');
 need(t.commands.length===t.results.length&&new Set(t.commands.map(v=>v.eventId)).size===t.commands.length,'command_conflict');
 let state:BTravelState=initialBTravelState(c.throttleCruiseBps);
 for(let i=0;i<t.commands.length;i++){
  state=applyBTravelCommand(p,state,t.commands[i]!);
  need(json(state)===json(t.results[i]),'travel_result_conflict');
 }
 need(state.distanceMm>=p.boundary.distanceMm&&!state.paused,'boundary_not_reached');
 const tick=state.tick;
 const commandId=t.commands.at(-1)!.eventId,originalEventId='B_ARRIVE:'+await sha256Hex(json({runId:t.runId,nodeId:t.nodeId,destinationEventId:b.destinationEventId,boundarySourceHash:p.source.sourceHash,commandId,tick:Number(tick)}));
 need(t.arrival.eventId===originalEventId&&t.arrival.commandId===commandId&&t.arrival.commandSequence===t.commands.length&&t.arrival.tick===Number(tick),'arrival_event_conflict');
 return {report:b,originalEventId,tick:Number(tick),sequence:t.commands.length};
}
export async function readBArrivalDestination(db:D1Database,p:Awaited<ReturnType<typeof readPinnedBArrivalBoundary>>,eventId:string){
 const row=await db.prepare('SELECT response_json FROM operation_map_receipts WHERE run_id=? AND event_id=?').bind(p.x.pin!.origin.runId,eventId).first<{response_json:string}>();if(!row)return fail('destination_missing');
 const r=mapReceipt.parse(JSON.parse(row.response_json));
 need(r.eventId===eventId&&r.revision===p.x.row.revision&&r.verificationId===null&&json(r.state)===json(p.x.state),'destination_conflict');return row;
}
