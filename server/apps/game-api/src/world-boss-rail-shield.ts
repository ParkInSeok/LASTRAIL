import {exactCombatValueSchema} from './combat-exact-value';
import {z} from 'zod';
const number=z.number().int().nonnegative().safe(),id=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
export const railShieldVersion='world-boss.rail-personal-shield.v1.20261002';
export const personalLayerSchema=z.strictObject({sourceId:z.string().min(1).max(400),recipientId:id,amount:number,expiresAtMs:number});
export const railShieldSnapshotSchema=z.strictObject({version:z.literal(railShieldVersion),targetId:id,lastAtMs:number,protectedUntilMs:number,layer:personalLayerSchema.nullable()}).refine(s=>!s.layer||s.layer.recipientId===s.targetId);
/** Minimum LOCAL single-personal-layer owner. HP remains in CombatHealthStore.
 * Already resolved trial shield magnitude only; this is not ability/cast authorization.
 * Multiple layers/shared pools require their existing policy producers, not invented order. */
export class WorldBossRailShield {
 private constructor(private state:z.infer<typeof railShieldSnapshotSchema>){}
 static create(targetId:string,atMs:number,protectedUntilMs:number,layer:unknown){return new WorldBossRailShield(railShieldSnapshotSchema.parse({version:railShieldVersion,targetId,lastAtMs:atMs,protectedUntilMs,layer}));}
 static restore(raw:unknown){return new WorldBossRailShield(railShieldSnapshotSchema.parse(raw));}
 snapshot(){return structuredClone(this.state);}
 applyResolved(amount:string,atMs:number,hp:number){
  exactCombatValueSchema.shape.numerator.parse(amount);number.parse(atMs);number.parse(hp);
  if(atMs<this.state.lastAtMs)throw new Error('rail_health_clock_regression');
  const candidate=this.snapshot();candidate.lastAtMs=atMs;if(candidate.layer&&(candidate.layer.expiresAtMs<=atMs||candidate.layer.amount===0))candidate.layer=null;
  const protectedNow=atMs<candidate.protectedUntilMs,damage=hp===0||protectedNow?0n:BigInt(amount),available=BigInt(candidate.layer?.amount??0),absorbed=Number(damage<available?damage:available),remaining=damage-BigInt(absorbed),hpLoss=Number(remaining<BigInt(hp)?remaining:BigInt(hp));
  if(candidate.layer){candidate.layer.amount-=absorbed;if(candidate.layer.amount===0)candidate.layer=null;}this.state=candidate;
  return {protectedNow,absorbed,hpLoss,hpAfter:hp-hpLoss};
 }
}