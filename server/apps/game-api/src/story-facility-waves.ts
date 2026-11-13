import {z} from "zod";
import {ActivityError} from "./activity-policy";
import {facilityAttackSnapshot,initializeFacilityAttack,stopFacilityAttack} from "./story-facility-attack";
const id=z.string().min(1).max(200),time=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const fail=(code:string):never=>{throw new ActivityError(409,code);};
const wave=z.discriminatedUnion("kind",[z.strictObject({waveId:id,kind:z.literal("BASIC"),actorId:id}),z.strictObject({waveId:id,kind:z.literal("DC01")})]);
export const facilityWavePlan=z.strictObject({version:z.literal("story.facility-wave-plan.v1"),waves:z.array(wave).min(2).max(3)}).superRefine((p,ctx)=>{
 const actors=p.waves.flatMap(w=>w.kind==="BASIC"?[w.actorId]:[]);
 if(p.waves[0]?.kind!=="BASIC"||p.waves[1]?.kind!=="BASIC"||(p.waves.length===3&&p.waves[2]?.kind!=="DC01")||new Set(p.waves.map(w=>w.waveId)).size!==p.waves.length||new Set(actors).size!==actors.length)ctx.addIssue({code:"custom",message:"facility_wave_plan_invalid"});
});
export const facilityWaveState=z.strictObject({version:z.literal("story.facility-waves.v1"),plan:facilityWavePlan,index:z.number().int().min(0).max(2),phase:z.enum(["ACTIVE","BETWEEN","COMPLETE"]),lastObservedAt:time,completedWaveIds:z.array(id).max(3)}).superRefine((s,ctx)=>{
 const count=s.phase==="ACTIVE"?s.index:s.index+1;
 if(s.index>=s.plan.waves.length||s.completedWaveIds.length!==count||s.completedWaveIds.some((v,i)=>v!==s.plan.waves[i]?.waveId)||(s.phase==="COMPLETE"&&count!==s.plan.waves.length)||(s.phase==="BETWEEN"&&count>=s.plan.waves.length))ctx.addIssue({code:"custom",message:"facility_wave_state_invalid"});
});
export const facilityWaveCommandFields={kind:z.enum(["WAVE_START","WAVE_COMPLETE"]),at:time,waveId:id};
export function initializeFacilityWaves(input:unknown,attack:unknown){
 const plan=facilityWavePlan.parse(input),a=facilityAttackSnapshot.parse(attack),first=plan.waves[0];
 if(first?.kind!=="BASIC"||first.actorId!==a.definition.actorId)return fail("facility_wave_actor_mismatch");
 return facilityWaveState.parse({version:"story.facility-waves.v1",plan,index:0,phase:"ACTIVE",lastObservedAt:a.definition.entryCompletedAt,completedWaveIds:[]});
}
/** Internal transition after the mode executor has validated mandatory enemies, residual hazards and holds.
 * Plan registration is not proof of actual wave completion, spawn validity or DC01 investment.
 */
export function transitionFacilityWave(input:unknown,attacker:unknown,hp:number,c:{kind:"WAVE_START"|"WAVE_COMPLETE";at:number;waveId:string}){
 const waves=facilityWaveState.parse(input);let attack=facilityAttackSnapshot.parse(attacker);
 if(c.at<Math.max(waves.lastObservedAt,attack.lastObservedAt))return fail("facility_wave_time_regressed");
 if(c.kind==="WAVE_COMPLETE"){
  if(waves.phase!=="ACTIVE")return fail("facility_wave_state_invalid");
  if(waves.plan.waves[waves.index]?.waveId!==c.waveId)return fail("facility_wave_id_mismatch");
  waves.completedWaveIds.push(c.waveId);waves.phase=waves.index+1===waves.plan.waves.length?"COMPLETE":"BETWEEN";
  attack.phase="ENDED";attack.pending=null;attack.normalNextAt=null;attack.lastObservedAt=c.at;
 }else{
  if(waves.phase!=="BETWEEN")return fail("facility_wave_state_invalid");
  if(c.at-waves.lastObservedAt<2000)return fail("facility_wave_interval_pending");
  const next=waves.plan.waves[waves.index+1];if(!next||next.waveId!==c.waveId)return fail("facility_wave_id_mismatch");
  if(next.kind==="BASIC"){
   const entryCompletedAt=c.at+600;if(!Number.isSafeInteger(entryCompletedAt))return fail("facility_attack_time_exhausted");
   attack=initializeFacilityAttack({...attack.definition,actorId:next.actorId,entryCompletedAt},attack.definition.facilityId);
   if(hp===0)attack=stopFacilityAttack(attack);
  }
  waves.index++;waves.phase="ACTIVE";
 }
 waves.lastObservedAt=c.at;return {waves:facilityWaveState.parse(waves),attack:facilityAttackSnapshot.parse(attack)};
}
