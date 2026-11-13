import {z} from "zod";
import {ActivityError} from "./activity-policy";
const id=z.string().min(1).max(200),time=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const fail=(code:string):never=>{throw new ActivityError(409,code);};
function later(at:number,delay:number){const next=at+delay;if(!Number.isSafeInteger(next))return fail("facility_attack_time_exhausted");return next;}
export const facilityAttackerDefinition=z.strictObject({version:z.literal("story.facility-attacker.v1"),actorId:id,facilityId:id,entryCompletedAt:time,normalPeriodMs:time.min(1),tier:z.enum(["EARLY","MIDDLE"])});
export const facilityAttackSnapshot=z.strictObject({version:z.literal("story.facility-attack.v1"),definition:facilityAttackerDefinition,phase:z.enum(["FACILITY","DEAD","FRONTLINE","ENDED"]),lastObservedAt:time,nextImpactAt:time,recoveryUntil:time,normalNextAt:time.nullable(),pending:z.strictObject({actionId:id,beganAt:time,impactAt:time}).nullable(),usedActionIds:z.array(id).max(100000)}).superRefine((s,ctx)=>{
 if(s.lastObservedAt<s.definition.entryCompletedAt||new Set(s.usedActionIds).size!==s.usedActionIds.length||(s.phase!=="FACILITY"&&s.pending!==null)||((s.phase==="FRONTLINE")!==(s.normalNextAt!==null))||(s.pending&&(s.pending.impactAt-s.pending.beganAt!==2000||s.pending.beganAt>s.lastObservedAt||!s.usedActionIds.includes(s.pending.actionId))))ctx.addIssue({code:"custom",message:"facility_attack_state_invalid"});
});
export const facilityAttackCommandFields={kind:z.enum(["ATTACK_BEGIN","ATTACK_COMPLETE","ATTACK_DEATH"]),at:time,actorId:id,actionId:id};
export function initializeFacilityAttack(input:unknown,facilityId:string){
 const definition=facilityAttackerDefinition.parse(input);if(definition.facilityId!==facilityId)return fail("facility_attack_target_mismatch");
 return facilityAttackSnapshot.parse({version:"story.facility-attack.v1",definition,phase:"FACILITY",lastObservedAt:definition.entryCompletedAt,nextImpactAt:later(definition.entryCompletedAt,6000),recoveryUntil:definition.entryCompletedAt,normalNextAt:null,pending:null,usedActionIds:[]});
}
/** Shared timing contract for an explicitly bound, unmodified first-test attacker.
 * Caller owns logical clock/hold, actual actor validity, shared telegraph grant and event ordering.
 */
export function advanceFacilityAttack(input:unknown,c:{kind:"ATTACK_BEGIN"|"ATTACK_COMPLETE"|"ATTACK_DEATH";at:number;actorId:string;actionId:string}){
 const state=facilityAttackSnapshot.parse(input);let damage=0;
 if(c.actorId!==state.definition.actorId)return fail("facility_attack_actor_mismatch");
 if(c.at<state.lastObservedAt)return fail("facility_attack_time_regressed");
 if(state.phase!=="FACILITY")return fail("facility_attack_inactive");
 if(c.kind==="ATTACK_DEATH"){state.phase="DEAD";state.pending=null;}
 else if(c.kind==="ATTACK_BEGIN"){
  if(state.pending)return fail("facility_attack_busy");
  if(state.usedActionIds.includes(c.actionId))return fail("facility_attack_action_reused");
  if(c.at<state.nextImpactAt-2000||c.at<state.recoveryUntil)return fail("facility_attack_not_ready");
  state.pending={actionId:c.actionId,beganAt:c.at,impactAt:later(c.at,2000)};state.usedActionIds.push(c.actionId);
 }else{
  if(!state.pending||state.pending.actionId!==c.actionId)return fail("facility_attack_action_mismatch");
  if(c.at<state.pending.impactAt)return fail("facility_attack_not_ready");
  damage=state.definition.tier==="EARLY"?100:110;
  state.pending=null;state.nextImpactAt=later(c.at,6000);state.recoveryUntil=later(c.at,300);
 }
 state.lastObservedAt=c.at;return {state:facilityAttackSnapshot.parse(state),damage};
}
export function stopFacilityAttack(input:unknown){
 const state=facilityAttackSnapshot.parse(input);
 if(state.phase==="FACILITY"){state.phase="FRONTLINE";state.pending=null;state.normalNextAt=later(state.lastObservedAt,state.definition.normalPeriodMs);}
 return facilityAttackSnapshot.parse(state);
}
