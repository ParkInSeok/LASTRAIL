import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOriginalBRouteSource,bRouteEvidenceVersion,BRouteSourceError,type BRouteEvidenceOwner,type BRouteEvidenceContext} from './combat-b-route-adapter';
import {readBNativeNodeSource} from './combat-b-native-node-source';
import {bBoundaryArrivalObservationSchema,readPinnedBArrivalBoundary,readBArrivalDestination} from './combat-b-arrival-source';
import type {OriginalSafeRuntimeSource} from './equipment-safe-maintenance';
import type {BRouteEvidenceProvider} from './combat-b-route-routes';
export const bOriginalObservationVersion='combat.b-original-accepted-observation.v1.20261006';
const id=z.string().min(1).max(200),rev=z.number().int().positive().safe(),hash=z.string().regex(/^[a-f0-9]{64}$/u),nativeHash=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const event=z.strictObject({eventId:id,sequence:rev});
export const bOriginalObservationSchema=z.strictObject({
 version:z.literal(bOriginalObservationVersion),accountId:id,runId:id,nodeId:id,mapRevision:rev,
 authorityMode:z.literal('ORDINARY_SOLO'),sourceVerification:z.literal('ORIGINAL_OWNER_VERIFIED'),verificationOwner:z.literal('SERVER'),
 nativeContextId:id,departurePinHash:hash,nodeSourceHash:hash,operationBindingHash:hash,nativeBindingHash:nativeHash,
 originalEventId:id,checkpointHash:nativeHash,sourceRevision:rev,sequence:rev,evidenceRef:id,
 registeredHazardOwners:z.array(id).min(1).max(32),hazards:z.array(z.strictObject({ownerId:id,pending:z.boolean()})).min(1).max(32),unknownHazards:z.array(id).max(32),
 combatStopped:z.boolean(),travelStopped:z.boolean(),
 arrival:event.nullable(),waves:z.array(event.extend({ordinal:rev,hazardsSettled:z.literal(true)})).max(2),completion:event.nullable(),holdIds:z.array(id).max(1000),
 holdSettlements:z.array(event.extend({holdId:id})).max(1000)
}).superRefine((v,c)=>{const unique=(a:string[])=>new Set(a).size===a.length;if(!unique(v.registeredHazardOwners)||!unique(v.hazards.map(h=>h.ownerId))||!unique(v.holdIds)||!unique(v.holdSettlements.map(h=>h.holdId))||new Set(v.waves.map(w=>w.ordinal)).size!==v.waves.length)c.addIssue({code:'custom',message:'duplicate original owner/event'});
 if(v.hazards.length!==v.registeredHazardOwners.length||v.registeredHazardOwners.some(o=>!v.hazards.some(h=>h.ownerId===o)))c.addIssue({code:'custom',message:'incomplete original hazard registry'});
 if([v.arrival,...v.waves,v.completion,...v.holdSettlements].some(e=>e&&e.sequence>v.sequence))c.addIssue({code:'custom',message:'future original sequence'});
});
export type BOriginalObservation=z.infer<typeof bOriginalObservationSchema>;
export const bResultObservationVersion='combat.b-original-accepted-observation.v2.20261006';
export const bResultObservationSchema=z.strictObject({...bOriginalObservationSchema.shape,version:z.literal(bResultObservationVersion),sourceVerification:z.literal('RESULT_CONSISTENCY_VERIFIED'),combatTruth:z.literal('NOT_PROVEN')}).superRefine((v,c)=>{const {combatTruth,...body}=v;void combatTruth;const result=bOriginalObservationSchema.safeParse({...body,version:bOriginalObservationVersion,sourceVerification:'ORIGINAL_OWNER_VERIFIED'});if(!result.success)for(const issue of result.error.issues)c.addIssue({code:'custom',path:issue.path,message:issue.message});});
const observationSchemas=z.union([bOriginalObservationSchema,bResultObservationSchema]);
type SafeContext=Parameters<OriginalSafeRuntimeSource['prepare']>[0];
/** INTERNAL result-verification owner only. Authentication/checksum/client LOCAL
 * observation/diagnostic receipt are NOT implementations of this source.
 * Original identity, authority assignment, accepted event/checkpoint/native binding,
 * registry and every inspected clock/hazard/transaction read MUST be checked by this owner
 * and fenced by returned statements in the caller's SAME original commit batch. */
export interface BOriginalVerifiedObservationOwner{
 prepare(context:Readonly<{accountId:string;runId:string;nodeId:string;mapRevision:number;kind:BRouteEvidenceContext['kind']|'SAFE_BETWEEN_NODES';evidenceRef:string|null}>):Promise<
 {status:'VERIFIED';observation:unknown;assertions:readonly D1PreparedStatement[]}|{status:'UNSUPPORTED';missingSources:string[]}>;
}
type PreparedObservation=Awaited<ReturnType<BOriginalVerifiedObservationOwner['prepare']>>;
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_broker_'+code);};
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
/** Translates server-checked ORIGINAL report facts to existing map/wave/safe owners. Owns
 * no simulation, HP, clock, reward, checkpoint or second acceptance journal. */
export class BOriginalAcceptedBroker implements BRouteEvidenceOwner,OriginalSafeRuntimeSource{
 constructor(private readonly db:D1Database,private readonly original?:BOriginalVerifiedObservationOwner){}
 private async capture(c:{accountId:string;runId:string;nodeId:string;mapRevision:number},kind:BRouteEvidenceContext['kind']|'SAFE_BETWEEN_NODES',evidenceRef:string|null,prepared?:PreparedObservation){
 if(!this.original)throw new BRouteSourceError(['ORIGINAL_RESULT_VERIFICATION_OWNER_AND_CHECKPOINT_FENCE']);
 const x=await readOriginalBRouteSource(this.db,c.accountId,c.runId);if(x.row.status!=='ACTIVE'||x.row.revision!==c.mapRevision)return fail('state_conflict');
 const packet=await readBNativeNodeSource(this.db,c.accountId,c.runId);if(packet.nodeId!==c.nodeId||packet.mapRevision!==c.mapRevision)return fail('node_conflict');
 const supplied=prepared??await this.original.prepare({...c,kind,evidenceRef});if(supplied.status==='UNSUPPORTED')throw new BRouteSourceError([...supplied.missingSources]);
 const obs=observationSchemas.parse(supplied.observation),assertions=[...supplied.assertions];
 if(!assertions.length||assertions.length>126||assertions.some(s=>typeof s.bind!=='function'))return fail('source_fence_missing');
 if(obs.accountId!==c.accountId||obs.runId!==c.runId||obs.nodeId!==c.nodeId||obs.mapRevision!==c.mapRevision||obs.nativeContextId!==packet.nativeContextId||obs.departurePinHash!==packet.departurePinHash||obs.nodeSourceHash!==packet.originalNodeSource.sourceHash||obs.operationBindingHash!==x.binding.bindingHash||(evidenceRef!==null&&obs.evidenceRef!==evidenceRef))return fail('origin_conflict');
 const settled=obs.unknownHazards.length===0&&!obs.hazards.some(h=>h.pending);
 return {obs,x,packet,settled,assertions};
 }
 prepare(c:BRouteEvidenceContext):ReturnType<BRouteEvidenceOwner['prepare']>;
 prepare(c:SafeContext):ReturnType<OriginalSafeRuntimeSource['prepare']>;
 async prepare(c:BRouteEvidenceContext|SafeContext):Promise<Awaited<ReturnType<BRouteEvidenceOwner['prepare']>>|Awaited<ReturnType<OriginalSafeRuntimeSource['prepare']>>>{
 if(!('kind' in c)){
 if(c.reason!=='BETWEEN_NODES')throw new BRouteSourceError(['ORIGINAL_ACCEPTED_SAFE_EVENT_PERMISSION_OWNER']);
 const p=await this.capture(c,'SAFE_BETWEEN_NODES',null);if(p.x.state.phase!=='READY'||!p.obs.combatStopped||!p.obs.travelStopped||!p.settled||!p.obs.completion)return fail('not_safe');
 return {evidence:{sourceId:'B_VERIFIED:'+await sha256Hex(json(p.obs)),sourceRevision:p.obs.sourceRevision,combatStopped:true,travelStopped:true,residualHazards:0,safeEventId:null},assertions:[...p.assertions,this.guard(c,p.x,'equipment_safe_write_guard')]};
 }
 // ARRIVE also serves noncombat nodes. Its original travel proof does not
 // require a combat wave, nor grant completion, hazard settlement or safe mode.
 let prepared:PreparedObservation|undefined;
 if(c.kind==='ARRIVE'&&this.original){
  prepared=await this.original.prepare({...c,evidenceRef:c.evidenceRef});
  if(prepared.status==='UNSUPPORTED')throw new BRouteSourceError([...prepared.missingSources]);
  const arrival=bBoundaryArrivalObservationSchema.safeParse(prepared.observation);
  if(arrival.success)return this.prepareBoundaryArrival(c,arrival.data,prepared.assertions);
 }
 const p=await this.capture(c,c.kind,c.evidenceRef,prepared),o=p.obs;
 if(c.version!==bRouteEvidenceVersion||c.bindingHash!==o.operationBindingHash||c.nativeContextId!==o.nativeContextId||c.departurePinHash!==o.departurePinHash)return fail('origin_conflict');
 let accepted:z.infer<typeof event>|undefined|null;
 if(c.kind==='ARRIVE'){if(p.x.state.phase!=='TRAVELLING'||!o.travelStopped)return fail('arrival_not_accepted');accepted=o.arrival;}
 else if(c.kind==='WAVE_COMPLETE')accepted=o.waves.find(w=>w.ordinal===c.waveOrdinal);
 else if(c.kind==='COMPLETE'){const expected=p.packet.originalNodeSource.waves.length;if(p.x.state.phase!=='ACTIVE'||o.waves.length!==expected||o.waves.some((w,i)=>w.ordinal!==i+1)||!o.combatStopped||!o.travelStopped||!p.settled)return fail('completion_not_settled');accepted=o.completion;}
 else accepted=o.holdSettlements.find(h=>h.holdId===c.holdId);
 if(!accepted)throw new BRouteSourceError([c.kind+'_ORIGINAL_ACCEPTED_EVENT']);
 const eligibility={ARRIVE:'ARRIVAL_ACCEPTED',WAVE_COMPLETE:'WAVE_AND_HAZARDS_SETTLED',COMPLETE:'OBJECTIVES_AND_HAZARDS_SETTLED',RELEASE_HOLD:'HOLD_TRANSACTION_SETTLED'}[c.kind];
 return {status:'SUPPORTED' as const,proof:{...c,originalEventId:accepted.eventId,sourceRevision:o.sourceRevision,originalSequence:accepted.sequence,sourceHash:await sha256Hex(json(o)),eligibility,holdIds:c.kind==='COMPLETE'?[...o.holdIds]:[]},assertions:[...p.assertions,this.guard(c,p.x,'operation_map_write_guard')]};
 }
 private async prepareBoundaryArrival(c:BRouteEvidenceContext,o:z.infer<typeof bBoundaryArrivalObservationSchema>,assertions:readonly D1PreparedStatement[]){
  const p=await readPinnedBArrivalBoundary(this.db,c.accountId,c.runId,c.nodeId),x=p.x;
  if(c.kind!=='ARRIVE'||x.row.status!=='ACTIVE'||x.state.phase!=='TRAVELLING'||x.row.revision!==c.mapRevision)return fail('state_conflict');
  if(!assertions.length||assertions.length>126||assertions.some(s=>typeof s.bind!=='function'))return fail('source_fence_missing');
  if(c.version!==bRouteEvidenceVersion||o.accountId!==c.accountId||o.runId!==c.runId||o.nodeId!==c.nodeId||o.mapRevision!==c.mapRevision||o.evidenceRef!==c.evidenceRef||o.nativeContextId!==p.nativeContextId||c.nativeContextId!==p.nativeContextId||o.departurePinHash!==p.departurePinHash||c.departurePinHash!==p.departurePinHash||o.nodeSourceHash!==p.nodeSourceHash||o.operationBindingHash!==x.binding.bindingHash||c.bindingHash!==x.binding.bindingHash||o.originalEventId!==o.arrival.eventId||o.arrival.sequence!==o.sequence)return fail('origin_conflict');
  const destination=await readBArrivalDestination(this.db,p,o.destination.eventId);
  if(o.destination.mapRevision!==c.mapRevision||o.destination.edgeId!==p.boundary.edgeId||o.destination.receiptHash!==await sha256Hex(destination.response_json))return fail('destination_conflict');
  return {status:'SUPPORTED' as const,proof:{...c,originalEventId:o.arrival.eventId,sourceRevision:o.sourceRevision,originalSequence:o.arrival.sequence,sourceHash:await sha256Hex(json(o)),eligibility:'ARRIVAL_ACCEPTED',holdIds:[]},assertions:[...assertions,this.guard(c,x,'operation_map_write_guard')]};
 }
 private guard(c:{accountId:string;runId:string},x:Awaited<ReturnType<typeof readOriginalBRouteSource>>,table:'operation_map_write_guard'|'equipment_safe_write_guard'){
 return this.db.prepare(`INSERT INTO ${table}(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN operation_previews p ON p.id=r.preview_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND r.preview_id=? AND p.account_id=? AND p.objective=? AND p.risk=? AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END`).bind(c.runId,c.accountId,x.row.preview_id,x.row.preview_account,x.row.objective,x.row.risk,x.row.snapshot_json,x.row.revision,x.row.state_json,x.row.plan_json);
 }
}
/** Internal composition only. Default game routes remain unsupported unless a
 * real original verification owner is explicitly installed by the server. */
export function createBOriginalAcceptedRouteProvider(original:BOriginalVerifiedObservationOwner):BRouteEvidenceProvider{
 return {forAuthenticatedContext:async c=>new BOriginalAcceptedBroker(c.env.DB,original)};
}
