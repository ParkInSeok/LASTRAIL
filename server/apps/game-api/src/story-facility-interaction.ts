import {z} from "zod";
import {ActivityError} from "./activity-policy";
const id=z.string().min(1).max(200),time=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const fail=(code:string):never=>{throw new ActivityError(409,code);};
export const interactionPlan=z.strictObject({version:z.literal("story.interaction-plan.v1"),targetId:id,kind:z.enum(["RELAY","RALLY","AUX_RECORD"])});
export const interactionCancelReason=z.enum(["RANGE_LEFT","DOWN","TERMINATED"]);
export const interactionState=z.strictObject({version:z.literal("story.interaction-state.v1"),plan:interactionPlan,lastObservedAt:time,usedActionIds:z.array(id).max(100000),pending:z.strictObject({actionId:id,actorId:id,startedAt:time,completeAt:time}).nullable(),completion:z.strictObject({actionId:id,actorId:id,completedAt:time}).nullable(),lastCancellation:z.strictObject({actionId:id,actorId:id,reason:interactionCancelReason}).nullable()}).superRefine((s,ctx)=>{
 if(new Set(s.usedActionIds).size!==s.usedActionIds.length||(s.pending&&s.completion)||(s.pending&&(s.pending.completeAt-s.pending.startedAt!==1500||s.pending.startedAt>s.lastObservedAt||!s.usedActionIds.includes(s.pending.actionId)))||(s.completion&&(s.completion.completedAt>s.lastObservedAt||!s.usedActionIds.includes(s.completion.actionId)))||(s.lastCancellation&&!s.usedActionIds.includes(s.lastCancellation.actionId)))ctx.addIssue({code:"custom",message:"interaction_state_invalid"});
});
export const interactionCommandFields={at:time,actionId:id,actorId:id,interactionTargetId:id};
export function initializeInteraction(input:unknown){return interactionState.parse({version:"story.interaction-state.v1",plan:interactionPlan.parse(input),lastObservedAt:0,usedActionIds:[],pending:null,completion:null,lastCancellation:null});}
/** Trusted mode-executor commands only. Alive/range/control and shared skill occupancy must be
 * checked outside this persistence model at acceptance/completion; at is logical combat time.
 */
export function transitionInteraction(input:unknown,c:{kind:"INTERACTION_START"|"INTERACTION_COMPLETE"|"INTERACTION_CANCEL";at:number;actionId:string;actorId:string;interactionTargetId:string;reason?:z.infer<typeof interactionCancelReason>}){
 const state=interactionState.parse(input);
 if(c.interactionTargetId!==state.plan.targetId)return fail("interaction_target_mismatch");
 if(c.at<state.lastObservedAt)return fail("interaction_time_regressed");
 if(state.completion)return fail("interaction_already_completed");
 if(c.kind==="INTERACTION_START"){
  if(state.pending)return fail("interaction_busy");
  if(state.usedActionIds.includes(c.actionId))return fail("interaction_action_reused");
  const completeAt=c.at+1500;if(!Number.isSafeInteger(completeAt))return fail("interaction_time_exhausted");
  state.pending={actionId:c.actionId,actorId:c.actorId,startedAt:c.at,completeAt};state.usedActionIds.push(c.actionId);
 }else{
  if(!state.pending||state.pending.actionId!==c.actionId||state.pending.actorId!==c.actorId)return fail("interaction_action_mismatch");
  if(c.kind==="INTERACTION_COMPLETE"){
   if(c.at<state.pending.completeAt)return fail("interaction_not_ready");state.completion={actionId:c.actionId,actorId:c.actorId,completedAt:c.at};
  }else state.lastCancellation={actionId:c.actionId,actorId:c.actorId,reason:interactionCancelReason.parse(c.reason)};
  state.pending=null;
 }
 state.lastObservedAt=c.at;return interactionState.parse(state);
}
export function terminateInteraction(input:unknown){const state=interactionState.parse(input);if(state.pending){state.lastCancellation={actionId:state.pending.actionId,actorId:state.pending.actorId,reason:"TERMINATED"};state.pending=null;}return interactionState.parse(state);}
