import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {OperationMapStore} from './operation-map-store';
import type {EquipmentSafeMaintenanceOwner} from './equipment-departure-source';
export const equipmentSafeVersion='equipment.safe-maintenance.v1.20261005';
const id=z.string().min(1).max(200),rev=z.number().int().nonnegative().safe();
const command=z.discriminatedUnion('kind',[
 z.strictObject({eventId:id,kind:z.literal('ENTER'),expectedMapRevision:rev.positive(),expectedRevision:rev,nodeId:id,reason:z.enum(['BETWEEN_NODES','SAFE_EVENT'])}),
 z.strictObject({eventId:id,kind:z.literal('EXIT'),expectedMapRevision:rev.positive(),expectedRevision:rev,nodeId:id})
]);
const evidence=z.strictObject({sourceId:id,sourceRevision:rev.positive(),combatStopped:z.literal(true),travelStopped:z.literal(true),residualHazards:z.literal(0),safeEventId:id.nullable()});
const state=z.strictObject({version:z.literal(equipmentSafeVersion),revision:rev.positive(),mapRevision:rev.positive(),nodeId:id,mode:z.enum(['SAFE','CLOSED']),reason:z.enum(['BETWEEN_NODES','SAFE_EVENT']),evidence});
type State=z.infer<typeof state>;
const fail=(s:string):never=>{throw new ActivityError(409,'equipment_safe_'+s);};
/** Trusted original runtime observation, never client flags. Statements fence original
 * clock, hazard and safe-event owners. No clock/HP state is created by this journal.
 * No operating adapter is installed; LOCAL observation fixtures prove SQL only. */
export interface OriginalSafeRuntimeSource {
 prepare(context:{accountId:string;runId:string;nodeId:string;mapRevision:number;reason:State['reason']}):Promise<{evidence:unknown;assertions:D1PreparedStatement[]}>;
}
/** Derived eligibility journal. Original MapStore owns progress; runtime owns time/hazards. */
export class RunSafeMaintenanceStore implements EquipmentSafeMaintenanceOwner {
 constructor(private readonly db:D1Database,private readonly source?:OriginalSafeRuntimeSource){}
 private async run(accountId:string,runId:string){id.parse(accountId);id.parse(runId);const r=await this.db.prepare('SELECT r.status,s.snapshot_json,a.status AS account_status FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?').bind(runId,accountId).first<{status:string;snapshot_json:string;account_status:string}>();if(!r)return fail('run_not_found');return r;}
 async read(accountId:string,runId:string){await this.run(accountId,runId);const r=await this.db.prepare('SELECT state_json FROM equipment_safe_states WHERE run_id=?').bind(runId).first<{state_json:string}>();return r?state.parse(JSON.parse(r.state_json)):null;}
 private fence(accountId:string,runId:string,snapshot:string,mapRevision:number,previous:State|null){return this.db.prepare(`INSERT INTO equipment_safe_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=? AND m.start_snapshot_json=s.snapshot_json AND m.revision=?) AND ((? IS NULL AND NOT EXISTS(SELECT 1 FROM equipment_safe_states WHERE run_id=?)) OR EXISTS(SELECT 1 FROM equipment_safe_states WHERE run_id=? AND state_json=?)) THEN 1 ELSE 0 END`).bind(runId,accountId,snapshot,mapRevision,previous?canonicalJson(previous):null,runId,runId,previous?canonicalJson(previous):null);}
 private async observation(accountId:string,runId:string,nodeId:string,mapRevision:number,reason:State['reason']){if(!this.source)return fail('source_unavailable');const p=await this.source.prepare({accountId,runId,nodeId,mapRevision,reason});const proof=evidence.safeParse(p?.evidence);if(!proof.success||!p.assertions?.length)return fail('evidence_invalid');if(reason==='SAFE_EVENT'&&!proof.data.safeEventId)return fail('evidence_invalid');return {evidence:proof.data,assertions:[...p.assertions]};}
 private async eligible(accountId:string,runId:string,nodeId:string,revision:number,reason:State['reason']){const m=await new OperationMapStore(this.db).read(accountId,runId);if(m.revision!==revision||m.view.currentNodeId!==nodeId)return fail('map_conflict');if(reason==='BETWEEN_NODES'){if(m.state.phase!=='READY'||m.state.traversedEdgeIds.length===0)return fail('not_safe');}else{const n=m.view.nodes.find(x=>x.nodeId===nodeId);if(m.state.phase!=='ACTIVE'||!n||['COMBAT','ELITE','BOSS'].includes(n.eventKind))return fail('not_safe');}return m;}
 async commit(accountId:string,runId:string,input:unknown){const c=command.parse(input),run=await this.run(accountId,runId),hash=await sha256Hex(canonicalJson(c));
 const replay=async()=>{const r=await this.db.prepare('SELECT request_hash,response_json FROM equipment_safe_receipts WHERE run_id=? AND event_id=?').bind(runId,c.eventId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return fail('event_conflict');return state.parse(JSON.parse(r.response_json));};const old=await replay();if(old)return old;
 if(run.status!=='ACTIVE'||run.account_status!=='ACTIVE')return fail('run_not_active');const prior=await this.read(accountId,runId);if((prior?.revision??0)!==c.expectedRevision)return fail('revision_conflict');if(c.expectedRevision>=Number.MAX_SAFE_INTEGER)return fail('revision_exhausted');
 let next:State;let proofs:D1PreparedStatement[]=[];
 if(c.kind==='ENTER'){// A map advance invalidates previous eligibility; a newly proven eligible node may replace it under the same journal CAS.
 if(prior?.mode==='SAFE'&&prior.mapRevision===c.expectedMapRevision)return fail('already_safe');await this.eligible(accountId,runId,c.nodeId,c.expectedMapRevision,c.reason);const p=await this.observation(accountId,runId,c.nodeId,c.expectedMapRevision,c.reason);proofs=p.assertions;next=state.parse({version:equipmentSafeVersion,revision:c.expectedRevision+1,mapRevision:c.expectedMapRevision,nodeId:c.nodeId,mode:'SAFE',reason:c.reason,evidence:p.evidence});}
 else{if(!prior||prior.mode!=='SAFE'||prior.nodeId!==c.nodeId||prior.mapRevision!==c.expectedMapRevision)return fail('not_safe');next=state.parse({...prior,revision:prior.revision+1,mode:'CLOSED'});}
 try{await this.db.batch([...proofs,this.fence(accountId,runId,run.snapshot_json,c.expectedMapRevision,prior),this.db.prepare('INSERT INTO equipment_safe_states(run_id,state_json) VALUES(?,?) ON CONFLICT(run_id) DO UPDATE SET state_json=excluded.state_json').bind(runId,canonicalJson(next)),this.db.prepare('INSERT INTO equipment_safe_receipts VALUES(?,?,?,?)').bind(runId,c.eventId,hash,canonicalJson(next)),this.db.prepare('DELETE FROM equipment_safe_write_guard')]);}catch(err){const done=await replay();if(done)return done;if(/valid=1|UNIQUE constraint/iu.test(String(err)))return fail('state_conflict');throw err;}return next;
 }
 async prepare(accountId:string,runId:string){const run=await this.run(accountId,runId);if(run.status!=='ACTIVE'||run.account_status!=='ACTIVE')return fail('run_not_active');const s=await this.read(accountId,runId);if(!s||s.mode!=='SAFE')return fail('not_safe');await this.eligible(accountId,runId,s.nodeId,s.mapRevision,s.reason);const p=await this.observation(accountId,runId,s.nodeId,s.mapRevision,s.reason);if(canonicalJson(p.evidence)!==canonicalJson(s.evidence))return fail('source_changed');return {assertions:[...p.assertions,this.fence(accountId,runId,run.snapshot_json,s.mapRevision,s)]};}
}