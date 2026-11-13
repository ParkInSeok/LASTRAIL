import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { CombatUltimateState, snapshotSchema } from "./combat-ultimate-state";
const id=z.string().min(1).max(200);
export const damageActorsSchema=z.strictObject({version:z.literal("combat.damage-actors.v1"),operators:z.array(snapshotSchema).min(1).max(5),enemyTargetIds:z.array(id).min(1).max(1000)}).refine(s=>new Set(s.operators.map(o=>o.actorId)).size===s.operators.length && new Set(s.enemyTargetIds).size===s.enemyTargetIds.length && s.operators.every(o=>!s.enemyTargetIds.includes(o.actorId)));
const definition=z.strictObject({operators:z.array(z.strictObject({actorId:id,expanded:z.boolean()})).min(1).max(5),enemyTargetIds:z.array(id)});
export function initializeDamageActors(input:unknown,targets:readonly {id:string}[]){
 const d=definition.parse(input);
 const state=damageActorsSchema.parse({version:"combat.damage-actors.v1",operators:d.operators.map(o=>CombatUltimateState.start(o.actorId,o.expanded).snapshot()).sort((a,b)=>a.actorId<b.actorId?-1:1),enemyTargetIds:[...d.enemyTargetIds].sort()});
 if([...state.operators.map(o=>o.actorId),...state.enemyTargetIds].some(id=>!targets.some(t=>t.id===id)))throw new ActivityError(409,"combat_damage_actor_invalid");
 return state;
}
export function creditDamageActor(state:z.infer<typeof damageActorsSchema>,targets:readonly {id:string;hp:number}[],hit:{sourceActorId:string;targetId:string;actionId:string;hitOrdinal:number;sourceKind:"BASIC"|"SKILL"|"SUMMON"},hpLoss:number){
 const index=state.operators.findIndex(o=>o.actorId===hit.sourceActorId),source=targets.find(t=>t.id===hit.sourceActorId);
 if(index<0||!source)throw new ActivityError(409,"combat_damage_actor_invalid");
 if(!state.enemyTargetIds.includes(hit.targetId))throw new ActivityError(409,"combat_damage_enemy_target_required");
 const actor=CombatUltimateState.restore(state.operators[index]);
 const receipt=actor.credit({actionId:hit.actionId,hitOrdinal:hit.hitOrdinal,sourceActorId:hit.sourceActorId,sourceAlive:source.hp>0,sourceKind:hit.sourceKind,mode:"ORDINARY",resolvedDamage:hpLoss});
 state.operators[index]=actor.snapshot();
 return {actorId:hit.sourceActorId,charge:actor.snapshot().charge,gained:receipt.replayed?0:receipt.gained};
}
