import { z } from "zod";

const safeInt = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const positive = z.number().int().min(1).max(2147483647);
export const activityId = z.string().regex(/^[A-Za-z0-9_-]{1,100}$/u);
const rewards = z.array(z.union([
  z.object({ kind: z.literal("ITEM").optional(), id: activityId, quantity: positive }).strict(),
  z.object({ kind: z.literal("CRYSTAL"), quantity: positive }).strict(),
])).max(100).refine((lines) => new Set(lines.map((line) => line.kind === "CRYSTAL" ? "__crystal__" : line.id)).size === lines.length, "Duplicate reward item");
const window = { startAt: safeInt, endAt: safeInt, claimUntil: safeInt };
export const dailyResetUtcMinutes = 1140; // User-approved 04:00 Asia/Seoul = previous UTC day 19:00.
const attendanceSchema = z.object({
  kind: z.literal("ATTENDANCE"), ...window,
  resetOffsetMinutes: z.literal(dailyResetUtcMinutes),
  counting: z.literal("DISTINCT_DAYS"),
  dailyRewards: rewards,
  milestones: z.array(z.object({ days: positive, rewards }).strict()).max(366),
  recovery: z.union([
    z.object({ itemId: activityId, quantity: positive, maxPerCampaign: positive, missedDaysOnly: z.literal(true) }).strict(),
    z.object({ kind: z.literal("CRYSTAL"), walletPolicyVersion: activityId, quantity: positive, maxPerCampaign: positive, missedDaysOnly: z.literal(true) }).strict(),
  ]).nullable(),
}).strict();
const passSchema = z.object({
  kind: z.literal("PASS"), ...window,
  levels: z.array(z.object({ xp: positive, free: rewards, premium: rewards }).strict()).min(1).max(1000),
  runXp: z.object({ VICTORY: positive.optional(), FAILURE: positive.optional() }).strict(),
  purchase: z.object({
    provider: z.enum(["GOOGLE_PLAY", "APP_STORE"]),
    appId: z.string().min(1).max(200), productId: z.string().min(1).max(200),
  }).strict().nullable(),
}).strict();
export const activityPolicySchema = z.discriminatedUnion("kind", [attendanceSchema, passSchema])
  .refine((policy) => policy.startAt < policy.endAt && policy.endAt <= policy.claimUntil && policy.claimUntil <= 8640000000000000,
    "Invalid activity window")
  .refine((policy) => policy.kind === "ATTENDANCE"
    ? new Set(policy.milestones.map((milestone) => milestone.days)).size === policy.milestones.length
    : policy.levels.every((level, i) => i === 0 || level.xp > policy.levels[i - 1]!.xp), "Duplicate milestone or nonascending XP")
  .refine((policy) => policy.kind !== "PASS" || [policy.startAt, policy.endAt, policy.claimUntil]
    .every((time) => (time - dailyResetUtcMinutes * 60000) % 86400000 === 0), "Pass boundary must be 04:00 KST");

export type ActivityPolicy = z.infer<typeof activityPolicySchema>;
export type ItemReward = { kind?: "ITEM" | undefined; id: string; quantity: number };
export type Reward = ItemReward | { kind: "CRYSTAL"; quantity: number };
export type PassPolicy = Extract<ActivityPolicy, { kind: "PASS" }>;

export class ActivityError extends Error {
  constructor(readonly status: 400 | 403 | 409 | 503, readonly code: string) { super(code); }
}

export function parseActivityPolicy(value: string): ActivityPolicy {
  try { return activityPolicySchema.parse(JSON.parse(value)); }
  catch { throw new ActivityError(503, "activity_policy_invalid"); }
}

// Offset is minutes after UTC midnight, not a client timezone.
export function attendanceDay(now: number, resetOffsetMinutes: number): number {
  return Math.floor((now - resetOffsetMinutes * 60000) / 86400000);
}

export function passLevel(policy: PassPolicy, xp: number): number {
  return policy.levels.filter((level) => xp >= level.xp).length;
}

export function requireWindow(policy: ActivityPolicy, now: number, claimOnly = false): void {
  if (now < policy.startAt || now >= (claimOnly ? policy.claimUntil : policy.endAt)) {
    throw new ActivityError(409, "activity_outside_window");
  }
}
