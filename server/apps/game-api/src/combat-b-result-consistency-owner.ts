import {EquipmentRewardMapHolds} from './equipment-reward-map-holds';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {BResultReportStore,bResultReportSchema} from './combat-b-result-report';
import {readOriginalBRouteSource} from './combat-b-route-adapter';
import {readBNativeNodeSource} from './combat-b-native-node-source';
import {mapReceipt} from './operation-map-state';
import {bResultObservationVersion,type BOriginalVerifiedObservationOwner} from './combat-b-original-accepted-broker';
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
/** Ordinary solo consistency verification. Unity owns combat execution; server owns
 * report scope, immutable acceptance and original receipt lineage, not full replay. */
export class BResultConsistencyOwner implements BOriginalVerifiedObservationOwner{
 constructor(private readonly db:D1Database){}
 async prepare(c:Parameters<BOriginalVerifiedObservationOwner['prepare']>[0]):ReturnType<BOriginalVerifiedObservationOwner['prepare']>{
 if(c.kind==='ARRIVE'||c.kind==='RELEASE_HOLD')return {status:'UNSUPPORTED',missingSources:[c.kind+'_ORIGINAL_ACCEPTED_EVENT']};
 let ref=c.evidenceRef;
 if(c.kind==='SAFE_BETWEEN_NODES'){const link=await this.db.prepare('SELECT evidence_ref,map_revision FROM b_result_node_links WHERE run_id=? AND node_id=?').bind(c.runId,c.nodeId).first<{evidence_ref:string;map_revision:number}>();if(!link)return {status:'UNSUPPORTED',missingSources:['ORIGINAL_COMPLETION_RECEIPT_REPORT_LINK']};ref=link.evidence_ref;}
 if(!ref)return {status:'UNSUPPORTED',missingSources:['ORIGINAL_RESULT_RECEIPT_REFERENCE']};
 const saved=await new BResultReportStore(this.db).read(c.accountId,c.runId,ref);if(!saved)return {status:'UNSUPPORTED',missingSources:['ORIGINAL_RESULT_RECEIPT_REFERENCE']};
 if(!saved.receipt.progressAuthorized)return {status:'UNSUPPORTED',missingSources:[...saved.receipt.missingSources]};
 const report=bResultReportSchema.parse(saved.report),x=await readOriginalBRouteSource(this.db,c.accountId,c.runId),p=await readBNativeNodeSource(this.db,c.accountId,c.runId);
 if(x.row.revision!==c.mapRevision||p.nodeId!==c.nodeId||json(report.nodeSession.node)!==json(p.originalNodeSource)||c.mapRevision<report.reportOriginMapRevision)throw new ActivityError(409,'combat_b_result_receipt_scope_conflict');
 const rows=await this.db.prepare('SELECT event_id,response_json FROM operation_map_receipts WHERE run_id=? AND CAST(json_extract(response_json,\'$.revision\') AS INTEGER)>? AND CAST(json_extract(response_json,\'$.revision\') AS INTEGER)<=? ORDER BY CAST(json_extract(response_json,\'$.revision\') AS INTEGER)').bind(c.runId,report.reportOriginMapRevision,c.mapRevision).all<{event_id:string;response_json:string}>();
 if(rows.results.length!==c.mapRevision-report.reportOriginMapRevision)throw new ActivityError(409,'combat_b_result_receipt_chain_conflict');
 for(let i=0;i<rows.results.length;i++){const receipt=mapReceipt.parse(JSON.parse(rows.results[i]!.response_json));if(receipt.revision!==report.reportOriginMapRevision+i+1||receipt.state.pendingEdgeId||!receipt.state.traversedEdgeIds.length||x.plan.edges.find(e=>e.edgeId===receipt.state.traversedEdgeIds.at(-1))?.toNodeId!==c.nodeId)throw new ActivityError(409,'combat_b_result_receipt_chain_conflict');}
 const table=c.kind==='SAFE_BETWEEN_NODES'?'equipment_safe_write_guard':'operation_map_write_guard',r=report.nodeSession.runtime;
 const assertions=[this.db.prepare(`INSERT INTO ${table}(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM b_result_reports WHERE run_id=? AND account_id=? AND evidence_ref=? AND report_json=? AND receipt_json=?) THEN 1 ELSE 0 END`).bind(c.runId,c.accountId,ref,saved.storage.reportJson,json(saved.receipt))];
 for(const chunk of saved.storage.chunks)assertions.push(this.db.prepare(`INSERT INTO ${table}(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM b_result_journal_chunks WHERE run_id=? AND evidence_ref=? AND ordinal=? AND text_json=?) THEN 1 ELSE 0 END`).bind(c.runId,ref,chunk.ordinal,chunk.text));
 for(const row of rows.results)assertions.push(this.db.prepare(`INSERT INTO ${table}(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END`).bind(c.runId,row.event_id,row.response_json));
 if(c.kind==='COMPLETE')assertions.push(this.db.prepare('INSERT INTO b_result_node_links VALUES(?,?,?,?)').bind(c.runId,c.nodeId,ref,c.mapRevision));
 const rewardHolds=c.kind==='COMPLETE'?await new EquipmentRewardMapHolds(this.db).completion(c.accountId,c.runId,c.nodeId,ref):null;
 if(rewardHolds)assertions.push(...rewardHolds.assertions);
 const journal=saved.receipt.originalJournal;
 const waves=journal?journal.waves.map((w,i)=>({eventId:w.eventRef,sequence:w.rawSequence,ordinal:i+1,hazardsSettled:true})):await Promise.all(r.waveEntry.events.map(async w=>({eventId:'B_RESULT_EVENT:'+await sha256Hex(json({ref,kind:w.kind,sequence:w.sequence,ordinal:w.waveOrdinal})),sequence:w.sequence,ordinal:w.waveOrdinal,hazardsSettled:true})));
 const owners=['WAVE_ENTRY','ENEMY_ACTION','ENEMY_ADMISSION','GUNNER_PROJECTILE','INTERCEPTION'];
 return {status:'VERIFIED',observation:{version:bResultObservationVersion,accountId:c.accountId,runId:c.runId,nodeId:c.nodeId,mapRevision:c.mapRevision,authorityMode:'ORDINARY_SOLO',sourceVerification:'RESULT_CONSISTENCY_VERIFIED',verificationOwner:'SERVER',combatTruth:'NOT_PROVEN',nativeContextId:p.nativeContextId,departurePinHash:p.departurePinHash,nodeSourceHash:p.originalNodeSource.sourceHash,operationBindingHash:x.binding.bindingHash,nativeBindingHash:r.bindingHash,originalEventId:saved.receipt.originalEventId,checkpointHash:report.checkpointHash,sourceRevision:saved.receipt.commandRevision,sequence:r.sequence,evidenceRef:ref,registeredHazardOwners:owners,hazards:owners.map(ownerId=>({ownerId,pending:false})),unknownHazards:[],combatStopped:true,travelStopped:true,arrival:null,waves,completion:{eventId:journal?.terminal.eventRef??saved.receipt.originalEventId,sequence:journal?.terminal.pointSequence??r.sequence},holdIds:rewardHolds?.holdIds??[],holdSettlements:[]},assertions};
 }
}