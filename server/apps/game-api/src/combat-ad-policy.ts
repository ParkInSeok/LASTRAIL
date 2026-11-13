import { z } from "zod";
const positive=z.number().int().positive().max(Number.MAX_SAFE_INTEGER);
const id=z.string().min(1).max(256);
const schema=z.strictObject({version:id,weightsBps:z.tuple([z.literal(6000),z.literal(3000),z.literal(1000)]),
  challengeLifetimeMs:positive,callbackGraceMs:positive,maxAdsPerDay:positive,
  dayStartUtcMinutes:z.number().int().min(0).max(1439),adUnit:id,appId:id,
  placement:z.literal("COMBAT_CLEAR"),environment:z.enum(["LOCAL","DEV","PRD"]),
  rewardItem:id,rewardAmount:z.string().regex(/^[1-9][0-9]*$/u).max(32)});
export type CombatAdPolicy=z.infer<typeof schema>;
const gameSchema=schema.pick({version:true,weightsBps:true,placement:true,environment:true});
export function parseCombatRewardPolicy(value:unknown){
 const p=z.union([schema,gameSchema]).safeParse(value);
 if(!p.success)throw new Error('ad_reward_policy_unconfigured');return p.data;
}
export function parseCombatAdPolicy(value:unknown):CombatAdPolicy {
  const parsed=schema.safeParse(value);
  if(!parsed.success) throw new Error("ad_reward_policy_unconfigured");
  return parsed.data;
}
