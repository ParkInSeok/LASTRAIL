import {z} from "zod";
import {ActivityError} from "./activity-policy";
export const hostRetentionMs=24*60*60*1000;
const id=z.string().min(1).max(200),time=z.number().int().nonnegative().safe();
export const hostRetention=z.strictObject({version:z.literal("combat.host-retention.v1"),firstStoredAt:time.max(Number.MAX_SAFE_INTEGER-hostRetentionMs),deadlineAt:time,lastChangedAt:time,suspended:z.boolean()})
 .refine(s=>s.deadlineAt===s.firstStoredAt+hostRetentionMs&&s.lastChangedAt>=s.firstStoredAt);
export const suspendCommand=z.strictObject({requestId:id,expectedRevision:z.number().int().positive().max(Number.MAX_SAFE_INTEGER-1),observedAt:time.max(Number.MAX_SAFE_INTEGER-hostRetentionMs)});
export const suspensionReceipt=z.strictObject({version:z.literal("combat.host-suspend-receipt.v1"),requestId:id,revision:z.number().int().positive().safe(),retention:hostRetention});
export function suspendHost(previous:unknown,observedAt:number){
 const old=previous===null?null:hostRetention.parse(previous);
 if(old&&observedAt<old.lastChangedAt)throw new ActivityError(409,"combat_host_observation_regressed");
 return hostRetention.parse({version:"combat.host-retention.v1",firstStoredAt:old?.firstStoredAt??observedAt,deadlineAt:old?.deadlineAt??observedAt+hostRetentionMs,lastChangedAt:observedAt,suspended:true});
}
export function resumeStoredHost(input:unknown,verifiedAt:number){
 const s=hostRetention.parse(input);
 if(verifiedAt<s.lastChangedAt)throw new ActivityError(409,"combat_host_observation_regressed");
 if(s.suspended&&verifiedAt>=s.deadlineAt)throw new ActivityError(409,"combat_host_retention_expired");
 return hostRetention.parse({...s,suspended:false,lastChangedAt:verifiedAt});
}
