import { z } from "zod";

const positive = z.number().int().positive().max(Number.MAX_SAFE_INTEGER);
const weight = z.number().int().min(0).max(10000);
const identifier = z.string().min(1).max(256);
const schema = z.strictObject({
  version: identifier,
  weightsBps: z.tuple([weight, weight, weight]).refine(weights => weights.reduce((sum, n) => sum + n, 0) === 10000),
  challengeLifetimeMs: positive,
  callbackGraceMs: positive,
  maxAdsPerDay: positive,
  dayStartUtcMinutes: z.number().int().min(0).max(1439),
  adUnit: identifier,
  appId: identifier,
  placement: z.literal("OFFLINE_SUPPLY"),
  environment: z.enum(["LOCAL", "DEV", "PRD"]),
  rewardItem: identifier,
  rewardAmount: z.string().regex(/^[1-9][0-9]*$/u).max(32),
});

export type OfflineAdPolicy = z.infer<typeof schema>;
const gameSchema=schema.pick({version:true,weightsBps:true,placement:true,environment:true});
export type OfflineRewardPolicy=z.infer<typeof gameSchema>;
export function parseOfflineRewardPolicy(value:unknown):OfflineRewardPolicy{
  const p=z.union([schema,gameSchema]).safeParse(value);
  if(!p.success)throw new Error('ad_reward_policy_unconfigured');return p.data;
}

/** No launch defaults: parsing a policy never activates or approves it. */
export function parseOfflineAdPolicy(value: unknown): OfflineAdPolicy {
  const parsed = schema.safeParse(value);
  if (!parsed.success) throw new Error("ad_reward_policy_unconfigured");
  return parsed.data;
}

/** Display possibilities only, from the intent's pinned policy; not equal odds. */
export function getOfflineAdDisplayMultipliers(policy: OfflineRewardPolicy): Array<2 | 3 | 4> {
  return getOfflineAdProbabilities(policy).filter(entry => entry.weightBps > 0).map(entry => entry.multiplier);
}

/** Exact pinned selection weights, including impossible outcomes for disclosure. */
export function getOfflineAdProbabilities(policy: OfflineRewardPolicy): Array<{ multiplier: 2 | 3 | 4; weightBps: number }> {
  const validated = parseOfflineRewardPolicy(policy);
  return ([2, 3, 4] as const).map((multiplier, index) => ({ multiplier, weightBps: validated.weightsBps[index]! }));
}

export function selectOfflineAdMultiplier(policy: OfflineRewardPolicy, draw: number): 2 | 3 | 4 {
  const validated = parseOfflineRewardPolicy(policy);
  if (!Number.isInteger(draw) || draw < 0 || draw >= 10000) throw new Error("ad_reward_draw_invalid");
  return draw < validated.weightsBps[0] ? 2 : draw < validated.weightsBps[0] + validated.weightsBps[1] ? 3 : 4;
}

/** Call only for an eligible VERIFIED intent; persist chosen result atomically.
 * Losing a concurrent commit must read the stored winner, never expose this draw.
 */
export function drawOfflineAdMultiplier(policy: OfflineRewardPolicy): 2 | 3 | 4 {
  const sample = new Uint32Array(1);
  do { crypto.getRandomValues(sample); } while (sample[0]! >= 4294960000);
  return selectOfflineAdMultiplier(policy, sample[0]! % 10000);
}

const eligibleIds = new Set(["cur_credits", "cur_machine_parts", "cur_tactical_data", "cur_operator_exp"]);

/** All approved offline resource categories use the same multiplier. Input has
 * already been rounded once by the existing offline calculator and persisted.
 * This function neither rereads elapsed time nor adds a separate base payout.
 */
export function multiplyOfflineSnapshot(base: ReadonlyArray<{ id: string; quantity: number }>, multiplier: 2 | 3 | 4) {
  const ids = new Set<string>();
  if (![2, 3, 4].includes(multiplier) || base.length === 0 || base.length > 4) throw new Error("ad_reward_snapshot_invalid");
  return base.map(line => {
    if (!eligibleIds.has(line.id) || ids.has(line.id) || !Number.isSafeInteger(line.quantity) || line.quantity < 0 ||
        !Number.isSafeInteger(line.quantity * multiplier)) throw new Error("ad_reward_snapshot_invalid");
    ids.add(line.id);
    return { id: line.id, quantity: line.quantity * multiplier };
  });
}
