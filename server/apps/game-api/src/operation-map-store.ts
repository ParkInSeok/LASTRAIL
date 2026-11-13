import {destinationResponseVersion,prepareDestinationResponse} from './operation-map-destination-response';
import {prepareBTravelInitial} from './combat-b-arrival-source';
import {prepareFirstPlayableScaling} from './combat-first-playable-scaling';
import {prepareOrdinaryAutomationCompletion} from './ordinary-run-automation';
import {bOperationNodeStatements} from './combat-b-operation-progress';
import {OrdinaryNodeAttemptStore,type OrdinaryNodeStartOwner} from './ordinary-node-attempt';
import {verifyPinnedOperationMapContent} from "./operation-map-publisher";
import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {readDepartureMap,operationMapStartStatements} from "./operation-map-departure";
import {captureOperationMap,projectOperationMap,type OperationMapPlan} from "./operation-map-plan";
import {mapId,mapCommand,mapProof,mapProgress,mapReceipt,mapFail,readMapProgress,transitionMap} from "./operation-map-state";
type Row={revision:number;plan_json:string;start_snapshot_json:string;state_json:string};
export interface OperationMapVerifier {
 /** Must validate original arrival/objectives/residual hazards or settled transaction facts.
  * Prepare only: no resource spend/release or progress publication before the caller commits.
  * Fence every inspected arrival, objective and transaction source using caller-DB statements
  * against operation_map_write_guard(valid CHECK(valid=1)). Assertions run in the same batch
  * as state and receipt writes; unconditional guards are synthetic test fixtures only.
  * No client completion boolean is evidence. No production adapter is installed yet. */
 prepare(context:{accountId:string;runId:string;startSnapshot:string;plan:OperationMapPlan;state:z.infer<typeof mapProgress>;command:Exclude<z.infer<typeof mapCommand>,{kind:"CONFIRM"}>}):Promise<{proof:unknown;assertions:D1PreparedStatement[]}>;
}
/** Solo persistence seam. Public destination intent only; verified completion stays internal. */
export class OperationMapStore {
 constructor(private readonly db:D1Database,private readonly verifier?:OperationMapVerifier,private readonly nodeStartOwner?:OrdinaryNodeStartOwner){}
 private async run(accountId:string,runId:string){mapId.parse(accountId);mapId.parse(runId);const r=await this.db.prepare("SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?").bind(runId,accountId).first<{status:string;snapshot_json:string}>();if(!r)return mapFail("run_not_found");return r;}
 private row(runId:string){return this.db.prepare("SELECT revision,plan_json,start_snapshot_json,state_json FROM operation_map_states WHERE run_id=?").bind(runId).first<Row>();}
 async read(accountId:string,runId:string){await this.run(accountId,runId);const row=await this.row(runId);if(!row)return mapFail("state_missing");const plan=captureOperationMap(JSON.parse(row.plan_json));await verifyPinnedOperationMapContent(JSON.parse(row.start_snapshot_json),plan);const state=readMapProgress(plan,JSON.parse(row.state_json));return {revision:row.revision,state,view:projectOperationMap(plan,state.traversedEdgeIds)};}
 async lookup(accountId:string,runId:string,eventId:string){await this.run(accountId,runId);mapId.parse(eventId);const r=await this.db.prepare("SELECT response_json FROM operation_map_receipts WHERE run_id=? AND event_id=?").bind(runId,eventId).first<{response_json:string}>();return r?mapReceipt.parse(JSON.parse(r.response_json)):null;}
 async initialize(accountId:string,runId:string){
  const run=await this.run(accountId,runId),old=await this.row(runId);
  if(old){if(old.start_snapshot_json!==run.snapshot_json)return mapFail("start_changed");return this.read(accountId,runId);}
  if(run.status!=="ACTIVE")return mapFail("run_not_active");
  const snapshot=JSON.parse(run.snapshot_json);
  if(snapshot.encounterType!=="ORDINARY_12_NODE")return mapFail("profile_unsupported");
  const plan=readDepartureMap(snapshot);await verifyPinnedOperationMapContent(snapshot,plan);
  try{await this.db.batch(operationMapStartStatements(this.db,accountId,runId,run.snapshot_json));}catch(error){if((await this.row(runId))?.start_snapshot_json!==run.snapshot_json)throw error;}
  return this.read(accountId,runId);
 }
 async commit(accountId:string,runId:string,input:unknown,replyVersion?:typeof destinationResponseVersion){
  const c=mapCommand.parse(input);if(replyVersion!==undefined&&(replyVersion!==destinationResponseVersion||c.kind!=='CONFIRM'))return mapFail('destination_version_conflict');
  const run=await this.run(accountId,runId),hash=await sha256Hex(canonicalJson(z.json().parse(replyVersion?{command:c,responseVersion:replyVersion}:c)));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM operation_map_receipts WHERE run_id=? AND event_id=?").bind(runId,c.eventId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return mapFail("event_conflict");return mapReceipt.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  if(run.status!=="ACTIVE")return mapFail("run_not_active");const row=await this.row(runId);if(!row)return mapFail("state_missing");if(row.revision!==c.expectedRevision)return mapFail("revision_conflict");if(row.revision>=Number.MAX_SAFE_INTEGER)return mapFail("revision_exhausted");if(row.start_snapshot_json!==run.snapshot_json)return mapFail("start_changed");
  const originalPlan=readDepartureMap(JSON.parse(row.start_snapshot_json));
  if(!originalPlan||canonicalJson(originalPlan)!==canonicalJson(JSON.parse(row.plan_json)))return mapFail("content_plan_conflict");
  const plan=captureOperationMap(JSON.parse(row.plan_json));await verifyPinnedOperationMapContent(JSON.parse(row.start_snapshot_json),plan);const state=readMapProgress(plan,JSON.parse(row.state_json));let proof:z.infer<typeof mapProof>|null=null;let assertions:D1PreparedStatement[]=[];
  const currentNodeId=projectOperationMap(plan,state.traversedEdgeIds).currentNodeId;
  if(c.kind!=="CONFIRM"){
   if(!this.verifier?.prepare)return mapFail("verification_required");
   // Copy adapter inputs: adapters cannot mutate the state or command used for the commit.
   const prepared=await this.verifier.prepare({accountId,runId,startSnapshot:row.start_snapshot_json,plan:structuredClone(plan),state:structuredClone(state),command:structuredClone(c)});
   if(!prepared||!Array.isArray(prepared.assertions)||prepared.assertions.length===0)return mapFail("verification_invalid");
   const result=mapProof.safeParse(prepared.proof);
   assertions=[...prepared.assertions];
   if(!result.success||result.data.kind!==c.kind)return mapFail("verification_invalid");proof=result.data;
  }
  const nextState=transitionMap(plan,state,c,proof);
  if(c.kind==='COMPLETE'){
   const attempt=await this.db.prepare('SELECT generation FROM ordinary_node_attempts WHERE run_id=? AND node_id=?').bind(runId,c.nodeId).first<{generation:number}>();
   if(attempt?(c.attemptGeneration!==attempt.generation||proof?.attemptGeneration!==attempt.generation):c.attemptGeneration!==undefined)return mapFail('attempt_conflict');
   assertions.push(attempt
    ?this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_node_attempts WHERE run_id=? AND node_id=? AND generation=?) THEN 1 ELSE 0 END').bind(runId,c.nodeId,attempt.generation)
    :this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM ordinary_node_attempts WHERE run_id=? AND node_id=?) THEN 1 ELSE 0 END').bind(runId,c.nodeId));
  }
  const scaling=c.kind==='ARRIVE'?await prepareFirstPlayableScaling(this.db,{accountId,runId,eventId:c.eventId,
   revision:row.revision+1,startSnapshot:row.start_snapshot_json,plan,state:nextState}):undefined;
  if(scaling)assertions.push(scaling.assertion);
  const nodeStart=c.kind==='ARRIVE'&&this.nodeStartOwner
   ?await new OrdinaryNodeAttemptStore(this.db).prepareArrival(accountId,runId,projectOperationMap(plan,nextState.traversedEdgeIds).currentNodeId,this.nodeStartOwner,{eventId:c.eventId,...(scaling?{firstPlayableScaling:scaling.binding}:{})}):undefined;
  if(nodeStart)assertions.push(...nodeStart.assertions);
  const destination=replyVersion?await prepareDestinationResponse({accountId,runId,eventId:c.eventId,revision:row.revision+1,plan,startSnapshot:row.start_snapshot_json,state:nextState}):undefined;
  const confirmedAtMs=Date.now();
  const automation=c.kind==='COMPLETE'&&['COMBAT','ELITE','BOSS'].includes(plan.nodes.find(n=>n.nodeId===c.nodeId)?.eventKind??'')
   ?await prepareOrdinaryAutomationCompletion(this.db,accountId,runId,proof?.ultimateAuto,proof?.actions):undefined;
  if(automation)assertions.push(...automation.statements);
  const travelInitial=c.kind==='CONFIRM'?await prepareBTravelInitial({startSnapshot:row.start_snapshot_json,accountId,runId,edgeId:c.edgeId,eventId:c.eventId,mapRevision:row.revision+1,acceptedAtMs:confirmedAtMs,plan}):undefined;
  const receipt=mapReceipt.parse({version:"operation.map-receipt.v1",eventId:c.eventId,revision:row.revision+1,state:nextState,verificationId:proof?.verificationId??null,...(destination?{destination}:{}),...(scaling?{firstPlayableScaling:scaling.binding}:{}),...(nodeStart?{nodeStart:nodeStart.binding}:{}),...(travelInitial?{travelInitial}:{}),...(automation?{ordinaryAutomation:automation.state}:{})});
  if(c.kind==="CONFIRM"){
   const pending=await this.db.prepare("SELECT 1 FROM run_equipment_reward_items ri JOIN run_equipment_reward_bundles rb ON rb.bundle_id=ri.bundle_id WHERE rb.run_id=? AND rb.node_id=? AND ri.status='PENDING' LIMIT 1").bind(runId,currentNodeId).first();
   if(pending)return mapFail("reward_pending");
  }
  const bNodes=c.kind==="COMPLETE"?await bOperationNodeStatements(this.db,row.start_snapshot_json,runId,plan,state,c.nodeId,c.eventId,proof!.verificationId):[];
  try{await this.db.batch([
   ...assertions,...bNodes,
   this.db.prepare("INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN operation_map_states m ON m.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND EXISTS(SELECT 1 FROM accounts a WHERE a.id=r.account_id AND a.status='ACTIVE') AND s.snapshot_json=? AND m.revision=? AND m.state_json=? AND m.plan_json=? AND (?<>'CONFIRM' OR NOT EXISTS(SELECT 1 FROM run_equipment_reward_items ri JOIN run_equipment_reward_bundles rb ON rb.bundle_id=ri.bundle_id WHERE rb.run_id=r.id AND rb.node_id=? AND ri.status='PENDING'))) THEN 1 ELSE 0 END").bind(runId,accountId,row.start_snapshot_json,row.revision,row.state_json,row.plan_json,c.kind,currentNodeId),
   this.db.prepare("UPDATE operation_map_states SET revision=?,state_json=? WHERE run_id=? AND revision=?").bind(receipt.revision,canonicalJson(receipt.state),runId,row.revision),
   this.db.prepare("INSERT INTO operation_map_receipts VALUES(?,?,?,?)").bind(runId,c.eventId,hash,canonicalJson(z.json().parse(receipt))),
   // Same server observation in the receipt and transaction; this is not travel credit.
   ...(c.kind==='CONFIRM'?[this.db.prepare('INSERT INTO operation_map_destination_times VALUES(?,?,?)').bind(runId,c.eventId,confirmedAtMs)]:[]),
   this.db.prepare("DELETE FROM operation_map_write_guard")
  ]);}catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return mapFail("state_conflict");throw error;}
  return receipt;
 }
}
