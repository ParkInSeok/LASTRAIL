import {interactionPlan,interactionState,interactionCommandFields,interactionCancelReason,initializeInteraction,transitionInteraction,terminateInteraction} from "./story-facility-interaction";
import {facilityWaveState,facilityWaveCommandFields,initializeFacilityWaves,transitionFacilityWave} from "./story-facility-waves";
import {facilityAttackSnapshot,facilityAttackCommandFields,initializeFacilityAttack,advanceFacilityAttack,stopFacilityAttack} from "./story-facility-attack";
import {z} from "zod";
import {ActivityError} from "./activity-policy";
export const facilityId=z.string().min(1).max(200);
const nat=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const definitionFields={definitionVersion:facilityId,nodeId:facilityId,facilityId,kind:z.enum(["PUMP","PRISONER_AREA","PROTECTED_AREA","BRIDGE"]),maxHp:z.literal(600)};
export const facilityDefinition=z.discriminatedUnion("version",[
 z.strictObject({version:z.literal("story.facility-definition.v1"),...definitionFields}),
 z.strictObject({version:z.literal("story.facility-definition.v2"),...definitionFields,interaction:interactionPlan})
]).superRefine((d,ctx)=>{if(d.version==="story.facility-definition.v2"&&({PRISONER_AREA:"RELAY",PROTECTED_AREA:"RALLY",BRIDGE:"AUX_RECORD",PUMP:null}[d.kind]!==d.interaction.kind))ctx.addIssue({code:"custom",message:"facility_interaction_plan_invalid"});});
export const facilityState=z.strictObject({version:z.literal("story.facility-state.v1"),definition:facilityDefinition,hp:nat.max(600),status:z.enum(["ACTIVE","DISABLED","RESTORED"]),postCombat:z.boolean(),firstStopEventId:facilityId.nullable(),optionalFailed:z.boolean(),lastEventId:facilityId.nullable(),attack:facilityAttackSnapshot.optional(),waves:facilityWaveState.optional(),interaction:interactionState.optional()}).superRefine((s,ctx)=>{
 if((s.definition.version==="story.facility-definition.v2")!==(s.interaction!==undefined)||(s.definition.version==="story.facility-definition.v2"&&s.interaction&&(s.definition.interaction.targetId!==s.interaction.plan.targetId||s.definition.interaction.kind!==s.interaction.plan.kind))||(s.postCombat&&s.interaction?.pending))ctx.addIssue({code:"custom",message:"facility_interaction_binding_invalid"});
 if(s.waves&&s.attack){const current=s.waves.plan.waves[s.waves.index];
  if((s.waves.phase==="ACTIVE"&&current?.kind==="BASIC"&&current.actorId!==s.attack.definition.actorId)||((s.waves.phase!=="ACTIVE"||current?.kind==="DC01")&&s.attack.phase==="FACILITY"))ctx.addIssue({code:"custom",message:"facility_wave_binding_invalid"});
 }
 if(s.waves&&(!s.attack||(s.postCombat&&s.waves.phase!=="COMPLETE")))ctx.addIssue({code:"custom",message:"facility_wave_binding_invalid"});
 if(s.attack&&(s.attack.definition.facilityId!==s.definition.facilityId||(s.hp===0&&s.attack.phase==="FACILITY")||(s.postCombat&&s.attack.phase==="FACILITY")))ctx.addIssue({code:"custom",message:"facility_attack_target_state_invalid"});
 if(s.optionalFailed!==(s.firstStopEventId!==null)||((s.status==="ACTIVE")!==(s.hp>0))||(s.hp===0&&!s.optionalFailed)||(s.hp>0&&s.optionalFailed)||(s.status==="RESTORED"&&!s.postCombat))ctx.addIssue({code:"custom",message:"facility_state_invalid"});
});
const base={eventId:facilityId,expectedRevision:nat.min(1).max(Number.MAX_SAFE_INTEGER-1)};
export const facilityCommand=z.discriminatedUnion("kind",[
 z.strictObject({...base,...interactionCommandFields,kind:z.enum(["INTERACTION_START","INTERACTION_COMPLETE"])}),
 z.strictObject({...base,...interactionCommandFields,kind:z.literal("INTERACTION_CANCEL"),reason:interactionCancelReason}),
 z.strictObject({...base,...facilityAttackCommandFields}),
 z.strictObject({...base,...facilityWaveCommandFields}),
 z.strictObject({...base,kind:z.literal("DAMAGE"),resolvedDamage:nat}),
 z.strictObject({...base,kind:z.literal("END_COMBAT")}),
 z.strictObject({...base,kind:z.literal("RESTORE")})]);
export const facilityReceipt=z.strictObject({version:z.literal("story.facility-receipt.v1"),eventId:facilityId,revision:nat.min(2),hpLoss:nat.max(600),trainTransfer:z.literal(0),state:facilityState});
export function initializeFacility(input:unknown,attacker?:unknown,wavePlan?:unknown){
 const definition=facilityDefinition.parse(input),attack=attacker===undefined?undefined:initializeFacilityAttack(attacker,definition.facilityId);
 if(wavePlan!==undefined&&!attack)throw new ActivityError(409,"facility_wave_attacker_required");
 return facilityState.parse({version:"story.facility-state.v1",definition,hp:600,status:"ACTIVE",postCombat:false,firstStopEventId:null,optionalFailed:false,lastEventId:null,...(definition.version==="story.facility-definition.v2"?{interaction:initializeInteraction(definition.interaction)}:{}),...(attack?{attack}:{}),...(wavePlan===undefined?{}:{waves:initializeFacilityWaves(wavePlan,attack)})});
}
/** Domain transition only; mode executor must validate hit provenance, objectives and interaction eligibility. */
export function transitionFacility(input:unknown,command:unknown){
 const state=facilityState.parse(input),c=facilityCommand.parse(command);let hpLoss=0;
 if("at" in c&&state.interaction&&c.at<Math.max(state.interaction.lastObservedAt,state.attack?.lastObservedAt??0,state.waves?.lastObservedAt??0))throw new ActivityError(409,"facility_time_regressed");
 if("interactionTargetId" in c){
  if(state.postCombat)throw new ActivityError(409,"facility_combat_ended");
  if(!state.interaction)throw new ActivityError(409,"facility_interaction_missing");
  if(c.kind!=="INTERACTION_CANCEL"&&state.waves&&state.waves.phase!=="ACTIVE")throw new ActivityError(409,"interaction_combat_inactive");
  state.interaction=transitionInteraction(state.interaction,c);
 }else if("waveId" in c){
  if(state.postCombat)throw new ActivityError(409,"facility_combat_ended");
  if(!state.waves)throw new ActivityError(409,"facility_wave_plan_missing");
  const next=transitionFacilityWave(state.waves,state.attack,state.hp,c);state.waves=next.waves;state.attack=next.attack;
 }else if("at" in c){
  if(state.waves&&(state.waves.phase!=="ACTIVE"||state.waves.plan.waves[state.waves.index]?.kind!=="BASIC"))throw new ActivityError(409,"facility_wave_attack_forbidden");
  if(state.postCombat)throw new ActivityError(409,"facility_combat_ended");
  if(!state.attack)throw new ActivityError(409,"facility_attack_state_missing");
  const step=advanceFacilityAttack(state.attack,c);state.attack=step.state;if(state.waves)state.waves.lastObservedAt=c.at;
  hpLoss=Math.min(step.damage,state.hp);state.hp-=hpLoss;
  if(state.hp===0&&state.firstStopEventId===null){state.status="DISABLED";state.firstStopEventId=c.eventId;state.optionalFailed=true;state.attack=stopFacilityAttack(state.attack);}
 }else if(c.kind==="DAMAGE"){
  if(state.attack)throw new ActivityError(409,"facility_attack_command_required");
  if(state.postCombat)throw new ActivityError(409,"facility_combat_ended");
  hpLoss=Math.min(c.resolvedDamage,state.hp);state.hp-=hpLoss;
  if(state.hp===0&&state.firstStopEventId===null){state.status="DISABLED";state.firstStopEventId=c.eventId;state.optionalFailed=true;}
 }else if(c.kind==="END_COMBAT"){
  if(state.waves&&state.waves.phase!=="COMPLETE")throw new ActivityError(409,"facility_waves_incomplete");
  if(state.postCombat)throw new ActivityError(409,"facility_combat_ended");state.postCombat=true;
  if(state.interaction)state.interaction=terminateInteraction(state.interaction);
  if(state.attack){state.attack.phase="ENDED";state.attack.pending=null;state.attack.normalNextAt=null;}
 }else{
  if(!state.postCombat)throw new ActivityError(409,"facility_post_combat_required");
  if(state.status!=="DISABLED")throw new ActivityError(409,"facility_restore_not_required");
  // Completion of approved post-combat recovery is not a combat heal or optional-objective reset.
  state.status="RESTORED";
 }
 state.lastEventId=c.eventId;
 return {state:facilityState.parse(state),hpLoss,trainTransfer:0 as const};
}
