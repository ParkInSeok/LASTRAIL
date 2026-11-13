import {z} from "zod";
import {readCombatAuthority} from "./combat-authority";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {ActivityError} from "./activity-policy";
import {facilityId,facilityDefinition,facilityState,facilityCommand,facilityReceipt,initializeFacility,transitionFacility} from "./story-facility-state";
function storedState(state:z.infer<typeof facilityState>){const {attack,waves,interaction,...required}=state;return {...required,...(attack===undefined?{}:{attack}),...(waves===undefined?{}:{waves}),...(interaction===undefined?{}:{interaction})};}
type Row={revision:number;initial_hash:string;start_snapshot_json:string;state_json:string};
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** Internal solo-owner persistence seam, not a raw client damage/restore API.
 * Explicit supported definition required: no legacy facility default, no coop membership inference.
 * The mode executor owns attacks/occupancy/objective checks; this store owns CAS and original results.
 */
export class StoryFacilityStore {
 constructor(private readonly db:D1Database){}
 private async run(accountId:string,runId:string){
  facilityId.parse(accountId);facilityId.parse(runId);
  const r=await this.db.prepare("SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?").bind(runId,accountId).first<{status:string;snapshot_json:string}>();
  if(!r)return fail("facility_run_not_found");return r;
 }
 private row(runId:string,nodeId:string){facilityId.parse(nodeId);return this.db.prepare("SELECT revision,initial_hash,start_snapshot_json,state_json FROM story_facility_states WHERE run_id=? AND node_id=?").bind(runId,nodeId).first<Row>();}
 async read(accountId:string,runId:string,nodeId:string){await this.run(accountId,runId);const row=await this.row(runId,nodeId);if(!row)return fail("facility_state_missing");return {revision:row.revision,state:facilityState.parse(JSON.parse(row.state_json))};}
 async lookup(accountId:string,runId:string,nodeId:string,eventId:string){
  await this.run(accountId,runId);facilityId.parse(nodeId);facilityId.parse(eventId);
  const r=await this.db.prepare("SELECT response_json FROM story_facility_receipts WHERE run_id=? AND node_id=? AND event_id=?").bind(runId,nodeId,eventId).first<{response_json:string}>();return r?facilityReceipt.parse(JSON.parse(r.response_json)):null;
 }
 async initialize(accountId:string,runId:string,input:unknown,attacker?:unknown,wavePlan?:unknown){
  const definition=facilityDefinition.parse(input),run=await this.run(accountId,runId),state=initializeFacility(definition,attacker,wavePlan);
  const hash=await sha256Hex(canonicalJson({definition,startSnapshot:run.snapshot_json,...(state.attack?{attack:state.attack}:{}),...(state.waves?{waves:state.waves}:{})})),old=await this.row(runId,definition.nodeId);
  if(old){if(old.initial_hash!==hash)return fail("facility_initialization_conflict");return this.read(accountId,runId,definition.nodeId);}
  if(run.status!=="ACTIVE")return fail("facility_run_not_active");
  const snapshot=z.object({admission:z.object({combatAuthority:z.unknown()})}).safeParse(JSON.parse(run.snapshot_json));
  if(!snapshot.success||readCombatAuthority(snapshot.data.admission.combatAuthority).contract?.mode!=="ORDINARY_SOLO")return fail("facility_mode_unsupported");
  try{await this.db.batch([
   this.db.prepare("INSERT INTO story_facility_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND s.snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM story_facility_states WHERE run_id=? AND node_id=?) THEN 1 ELSE 0 END").bind(runId,accountId,run.snapshot_json,runId,definition.nodeId),
   this.db.prepare("INSERT INTO story_facility_states VALUES(?,?,1,?,?,?)").bind(runId,definition.nodeId,hash,run.snapshot_json,canonicalJson(storedState(state))),
   this.db.prepare("DELETE FROM story_facility_write_guard")]);
  }catch(error){if((await this.row(runId,definition.nodeId))?.initial_hash!==hash)throw error;}
  return this.read(accountId,runId,definition.nodeId);
 }
 async commit(accountId:string,runId:string,nodeId:string,input:unknown){
  facilityId.parse(nodeId);const c=facilityCommand.parse(input),run=await this.run(accountId,runId),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM story_facility_receipts WHERE run_id=? AND node_id=? AND event_id=?").bind(runId,nodeId,c.eventId).first<{request_hash:string;response_json:string}>();if(!r)return null;if(r.request_hash!==hash)return fail("facility_event_conflict");return facilityReceipt.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  if(run.status!=="ACTIVE")return fail("facility_run_not_active");const row=await this.row(runId,nodeId);if(!row)return fail("facility_state_missing");
  if(row.revision!==c.expectedRevision)return fail("facility_revision_conflict");if(row.start_snapshot_json!==run.snapshot_json)return fail("facility_start_changed");
  const result=facilityReceipt.parse({version:"story.facility-receipt.v1",eventId:c.eventId,revision:row.revision+1,...transitionFacility(JSON.parse(row.state_json),c)});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO story_facility_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN story_facility_states f ON f.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND s.snapshot_json=? AND f.node_id=? AND f.revision=? AND f.state_json=?) THEN 1 ELSE 0 END").bind(runId,accountId,row.start_snapshot_json,nodeId,row.revision,row.state_json),
   this.db.prepare("UPDATE story_facility_states SET revision=?,state_json=? WHERE run_id=? AND node_id=? AND revision=?").bind(result.revision,canonicalJson(storedState(result.state)),runId,nodeId,row.revision),
   this.db.prepare("INSERT INTO story_facility_receipts VALUES(?,?,?,?,?)").bind(runId,nodeId,c.eventId,hash,canonicalJson({...result,state:storedState(result.state)})),
   this.db.prepare("DELETE FROM story_facility_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("facility_state_conflict");throw error;}
  return result;
 }
}
