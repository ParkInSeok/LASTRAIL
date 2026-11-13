import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readCombatAuthority} from './combat-authority';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {readPinnedBArrivalBoundary,readBArrivalDestination} from './combat-b-arrival-source';
import type {BArrivalOriginalAuthority} from './combat-b-arrival-report';

const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),positive=nat.min(1),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const bindingSchema=z.strictObject({accountId:id,runId:id,nodeId:id,mapRevision:positive,checkpointHash:z.string().regex(/^sha256:[a-f0-9]{64}$/u),
 originalEventId:id,sequence:positive,tick:positive,destinationEventId:id,departurePinHash:hash,nodeSourceHash:hash});
const inputSchema=z.strictObject({binding:bindingSchema,sourceRef:id});
const proofSchema=z.strictObject({binding:bindingSchema,sourceRef:id,sourceRevision:positive,clockCheckpointRef:id,
 clock:z.strictObject({authority:z.literal('LOCAL_LOGICAL'),stepTicks:positive,debtHalfTicks:nat,completedSteps:positive,speedHalfUnits:z.literal(2),paused:z.literal(false)})});
export const bOriginalArrivalVersion='combat.b-original-arrival-receipt.v1.20261020';
const receiptSchema=z.strictObject({version:z.literal(bOriginalArrivalVersion),sourceRef:id,sourceRevision:positive,originalEventId:id,
 checkpointHash:z.string().regex(/^sha256:[a-f0-9]{64}$/u),proofHash:hash,combatTruth:z.literal('NOT_PROVEN'),capturedAtMs:nat});
type Binding=z.infer<typeof bindingSchema>;
type Row={account_id:string;source_ref:string;input_hash:string;proof_json:string;receipt_json:string};
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(s:string):never=>{throw new ActivityError(409,'combat_b_arrival_'+s);};
const need=(v:boolean,s:string)=>{if(!v)fail(s);};

/** Independent SERVER result-verification owner. This port is internal only.
 * It must look up and verify the actual pause-aware original Unity clock journal,
 * checkpoint and accepted event, NOT echo client elapsed or a client success flag.
 * Assertions fence every mutable source used by that verification.
 * There is intentionally no default verifier, wall-time fallback or HTTP capture.
 */
export interface BOriginalArrivalVerifier {
 prepare(input:z.infer<typeof inputSchema>):Promise<{proof:unknown;assertions:readonly D1PreparedStatement[]}>;
}
/** Immutable verified-original receipts bridge ingestion and later consumption.
 * admin/runtime lifecycle owners must revoke a receipt when withdrawing its
 * original verification. Subsequent consumers fence that revocation atomically.
 * capturedAtMs is audit time only; it grants zero combat time.
 */
export class BOriginalArrivalStore implements BArrivalOriginalAuthority{
 constructor(private readonly db:D1Database,private readonly verifier?:BOriginalArrivalVerifier){}
 private async recovered(runId:string,row:Row){
  if(await this.db.prepare('SELECT 1 FROM b_arrival_original_revocations WHERE run_id=? AND source_ref=?').bind(runId,row.source_ref).first())return fail('original_revoked');
  const proof=proofSchema.parse(JSON.parse(row.proof_json)),receipt=receiptSchema.parse(JSON.parse(row.receipt_json));
  need(proof.binding.accountId===row.account_id&&proof.binding.runId===runId&&proof.sourceRef===row.source_ref&&receipt.sourceRef===row.source_ref
   &&receipt.sourceRevision===proof.sourceRevision&&receipt.originalEventId===proof.binding.originalEventId&&receipt.checkpointHash===proof.binding.checkpointHash
   &&receipt.proofHash===await sha256Hex(row.proof_json),'original_storage_conflict');
  return {proof,receipt};
 }
 private async pinned(binding:Binding){
  const p=await readPinnedBArrivalBoundary(this.db,binding.accountId,binding.runId,binding.nodeId);
  need(p.x.row.status==='ACTIVE'&&p.x.state.phase==='TRAVELLING'&&p.x.row.revision===binding.mapRevision&&p.departurePinHash===binding.departurePinHash&&p.nodeSourceHash===binding.nodeSourceHash,'original_scope_conflict');
  const authority=readCombatAuthority(z.object({admission:z.object({combatAuthority:z.unknown()})}).parse(JSON.parse(p.x.row.snapshot_json)).admission.combatAuthority);
  need(authority.status==='PINNED'&&authority.contract.mode==='ORDINARY_SOLO'&&authority.contract.combatClock==='LOCAL_LOGICAL','original_clock_scope_unsupported');
  const destination=await readBArrivalDestination(this.db,p,binding.destinationEventId);
  return {p,destination};
 }
 private sourceGuards(binding:Binding,source:Awaited<ReturnType<BOriginalArrivalStore['pinned']>>){
  const {p,destination}=source,x=p.x;
  return [
   this.db.prepare(`INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN operation_previews v ON v.id=r.preview_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id
    WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND r.preview_id=? AND v.account_id=? AND v.objective=? AND v.risk=? AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=? AND m.state_json=? AND m.plan_json=?) THEN 1 ELSE 0 END`)
    .bind(binding.runId,binding.accountId,x.row.preview_id,x.row.preview_account,x.row.objective,x.row.risk,x.row.snapshot_json,x.row.revision,x.row.state_json,x.row.plan_json),
   this.db.prepare('INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END').bind(binding.runId,binding.destinationEventId,destination.response_json)
  ];
 }
 async capture(input:unknown){
  const c=inputSchema.parse(input),b=c.binding,inputHash=await sha256Hex(json(c));
  await readOriginalBRouteSource(this.db,b.accountId,b.runId);
  const replay=async()=>{
   const row=await this.db.prepare('SELECT * FROM b_arrival_originals WHERE run_id=? AND account_id=? AND source_ref=?').bind(b.runId,b.accountId,c.sourceRef).first<Row>();
   if(!row)return null;need(row.input_hash===inputHash,'original_source_conflict');
   return (await this.recovered(b.runId,row)).receipt;
  };
  const old=await replay();if(old)return old;
  if(!this.verifier)return fail('original_verifier_missing');
  let source:Awaited<ReturnType<BOriginalArrivalStore['pinned']>>;
  try{source=await this.pinned(b);}catch(error){const winner=await replay();if(winner)return winner;throw error;}
  const verified=await this.verifier.prepare(structuredClone(c)),proof=proofSchema.parse(verified.proof);
  need(json(proof.binding)===json(b)&&proof.sourceRef===c.sourceRef,'original_verification_binding_conflict');
  need(proof.clock.completedSteps===b.tick&&proof.clock.stepTicks===source.p.source.definition.stepTicks&&proof.clock.speedHalfUnits===source.p.source.definition.speedHalfUnits,'original_clock_conflict');
  need(verified.assertions.length>0&&verified.assertions.length<=112&&verified.assertions.every(s=>typeof s.bind==='function'),'original_verification_fence_missing');
  const proofJson=json(proof),receipt=receiptSchema.parse({version:bOriginalArrivalVersion,sourceRef:c.sourceRef,sourceRevision:proof.sourceRevision,originalEventId:b.originalEventId,checkpointHash:b.checkpointHash,proofHash:await sha256Hex(proofJson),combatTruth:'NOT_PROVEN',capturedAtMs:Date.now()});
  try{await this.db.batch([
   ...verified.assertions,...this.sourceGuards(b,source),
   this.db.prepare('INSERT INTO b_arrival_originals VALUES(?,?,?,?,?,?,?,?)').bind(b.runId,b.accountId,c.sourceRef,b.originalEventId,b.destinationEventId,inputHash,proofJson,json(receipt)),
   this.db.prepare('DELETE FROM operation_map_write_guard')
  ]);}catch(error){
   const winner=await replay();if(winner)return winner;
   if(/CHECK constraint failed|UNIQUE constraint failed/u.test(String(error)))return fail('original_capture_conflict');
   throw error;
  }
  return receipt;
 }
 async prepare(input:Parameters<BArrivalOriginalAuthority['prepare']>[0]){
  const binding=bindingSchema.parse(input);
  const row=await this.db.prepare('SELECT * FROM b_arrival_originals WHERE run_id=? AND account_id=? AND original_event_id=?').bind(binding.runId,binding.accountId,binding.originalEventId).first<Row>();
  if(!row)return fail('original_time_credit_and_accepted_event_owner_missing');
  const {proof}=await this.recovered(binding.runId,row);
  need(json(proof.binding)===json(binding),'original_source_conflict');
  const source=await this.pinned(binding);
  return {binding,assertions:[
   ...this.sourceGuards(binding,source),
   this.db.prepare(`INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM b_arrival_originals WHERE run_id=? AND account_id=? AND source_ref=? AND proof_json=? AND receipt_json=?)
    AND NOT EXISTS(SELECT 1 FROM b_arrival_original_revocations WHERE run_id=? AND source_ref=?) THEN 1 ELSE 0 END`)
    .bind(binding.runId,binding.accountId,row.source_ref,row.proof_json,row.receipt_json,binding.runId,row.source_ref)
  ]};
 }
 /** Internal withdrawal by the original verification owner. Never rolls back a
  * previously committed map transition; blocks subsequent proof consumption. */
 async revoke(accountId:string,runId:string,sourceRef:string,reason:string){
  [accountId,runId,sourceRef,reason].forEach(v=>id.parse(v));
  const row=await this.db.prepare('SELECT 1 FROM b_arrival_originals WHERE run_id=? AND account_id=? AND source_ref=?').bind(runId,accountId,sourceRef).first();
  if(!row)return fail('original_source_missing');
  await this.db.prepare('INSERT INTO b_arrival_original_revocations(run_id,source_ref,reason) VALUES(?,?,?) ON CONFLICT(run_id,source_ref) DO NOTHING').bind(runId,sourceRef,reason).run();
  return {revoked:true as const,sourceRef};
 }
}
