import { z } from "zod";
import { ActivityError, activityId } from "./activity-policy";
import { attendanceMonth } from "./monthly-attendance-policy";
import type { ActivityGrantReward } from "./activity-grant-plan";
export const objectiveDayMs = 86400000;
export function objectivePeriods(at: number) {
  const month = attendanceMonth(at), dayStart = month.startAt + month.dayIndex * objectiveDayMs;
  const weekday = new Date(dayStart + 5 * 3600000).getUTCDay();
  return { ...month, dayStart, dayEnd: dayStart + objectiveDayMs,
    weekStart: dayStart - ((weekday + 6) % 7) * objectiveDayMs,
    passWeek: month.dayIndex < 28 ? Math.floor(month.dayIndex / 7) : null };
}
export type ObjectiveMode = "ORDINARY_15" | "STORY_15" | "STORY_BOSS" | "WORLD" | "MAPPING_MISSING";
export function qualifyObjectiveFinish(mode: ObjectiveMode, outcome: string, waves: number, fullCompletion = false) {
  const result = (status: string, ends = 0, countedWaves = 0) => ({ status, ends, waves: countedWaves });
  if (mode === "MAPPING_MISSING") return result("OPERATION_BINDING_MISSING");
  if (mode === "WORLD") return result("WORLD_SEPARATE_TRACK");
  if (outcome === "ABANDONED" || outcome === "INVALID" || outcome === "CANCELLED") return result("NOT_ELIGIBLE");
  if (!Number.isInteger(waves) || waves < 0 || waves > 15) return result("INVALID");
  if (mode === "STORY_BOSS") {
    if (waves !== 0) return result("INVALID");
    return outcome === "VICTORY" && fullCompletion ? result("ELIGIBLE", 1) : result("NOT_ELIGIBLE");
  }
  if (outcome === "VICTORY") return waves === 15 && fullCompletion ? result("ELIGIBLE", 1, 15) : result("INVALID");
  if (outcome !== "FAILURE") return result("INVALID");
  if (mode === "STORY_15") return result("UNSUPPORTED_STORY_DEFEAT");
  return waves === 15 ? result("INVALID") : waves === 0 ? result("NOT_ELIGIBLE") : result("ELIGIBLE", 1, waves);
}
export function pass50Rewards() {
  const pattern: ActivityGrantReward[] = [{ kind: "CRYSTAL", quantity: 50 }, { kind: "CURRENCY", id: "cur_credits", quantity: 750 },
    { kind: "CURRENCY", id: "cur_machine_parts", quantity: 75 }, { kind: "CURRENCY", id: "cur_operator_exp", quantity: 350 },
    { kind: "CURRENCY", id: "cur_tactical_data", quantity: 35 }, { kind: "CURRENCY", id: "cur_credits", quantity: 750 },
    { kind: "CURRENCY", id: "cur_machine_parts", quantity: 75 }, { kind: "CURRENCY", id: "cur_operator_exp", quantity: 350 },
    { kind: "CURRENCY", id: "cur_tactical_data", quantity: 35 }, { kind: "CRYSTAL", quantity: 100 }];
  return Array.from({ length: 50 }, (_, i) => ({ level: i + 1, xp: (i + 1) * 100,
    free: [pattern[i % 10]!], premium: [{ ...pattern[i % 10]!, quantity: pattern[i % 10]!.quantity * 5 }],
    cosmeticSlot: i === 29 ? "FRAME" : i === 39 ? "OPERATOR_SKIN" : i === 49 ? "TRAIN_SKIN" : null }));
}
const currency = (id: string, quantity: number): ActivityGrantReward => ({ kind: "CURRENCY", id, quantity });
const crystal = (quantity: number): ActivityGrantReward => ({ kind: "CRYSTAL", quantity });
export const ordinaryQuestRewards: Record<string, ActivityGrantReward[]> = {
  D2: [crystal(10), currency("cur_credits", 100), currency("cur_machine_parts", 5)],
  D3: [crystal(10), currency("cur_operator_exp", 50), currency("cur_tactical_data", 5)],
  W1: [crystal(50), currency("cur_credits", 500)], W2: [crystal(50), currency("cur_machine_parts", 50)],
  W3: [crystal(50), currency("cur_operator_exp", 300), currency("cur_tactical_data", 20)],
};
const definitionSchema = z.strictObject({ version: z.literal("recurring-v1"),
  cosmetics: z.strictObject({ FRAME: activityId.nullable(), OPERATOR_SKIN: activityId.nullable(), TRAIN_SKIN: activityId.nullable() }),
  purchase: z.strictObject({ provider: z.enum(["GOOGLE_PLAY", "APP_STORE"]), appId: z.string().min(1).max(200), productId: z.string().min(1).max(200) }).nullable(),
});
export type RecurringDefinition = { id: string; system: "PASS50" | "QUESTS"; start_at: number; end_at: number; policy_json: string };
export function parseRecurringDefinition(d: RecurringDefinition) {
  try {
    const p = definitionSchema.parse(JSON.parse(d.policy_json));
    if (d.start_at >= d.end_at) throw new Error("window");
    const first = objectivePeriods(d.start_at), last = objectivePeriods(d.end_at);
    if (d.system === "PASS50" ? first.startAt !== d.start_at || first.endAt !== d.end_at
      : first.weekStart !== d.start_at || last.weekStart !== d.end_at) throw new Error("boundaries");
    return p;
  } catch { throw new ActivityError(503, "recurring_policy_invalid"); }
}
