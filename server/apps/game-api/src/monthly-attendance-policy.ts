import { z } from "zod";
import { ActivityError, activityId } from "./activity-policy";
import type { ActivityGrantReward } from "./activity-grant-plan";

const dayMs = 86400000, serviceShift = 5 * 3600000;
export function attendanceMonth(now: number) {
  const date = new Date(now + serviceShift), year = date.getUTCFullYear(), month = date.getUTCMonth();
  const startAt = Date.UTC(year, month, 1) - serviceShift, endAt = Date.UTC(year, month + 1, 1) - serviceShift;
  return { cycleId: `${year}-${String(month + 1).padStart(2, "0")}`, startAt, endAt, dayIndex: Math.floor((now - startAt) / dayMs) };
}
export const monthlyPolicySchema = z.strictObject({
  kind: z.literal("MONTHLY_ATTENDANCE"), version: z.literal("monthly-attendance-v1"),
  startAt: z.number().int().nonnegative(), endAt: z.number().int().positive(),
  // No real elite-ticket ID/consumption contract was found. Null is fail-closed;
  // an authorized policy publisher must supply the actual binding, not a guess.
  eliteTicketItemId: activityId.nullable(),
}).refine(p => p.eliteTicketItemId !== "cur_command_ticket" && p.startAt < p.endAt && attendanceMonth(p.startAt).startAt === p.startAt && attendanceMonth(p.endAt).startAt === p.endAt);
export type MonthlyPolicy = z.infer<typeof monthlyPolicySchema>;
export function parseMonthlyPolicy(raw: string): MonthlyPolicy {
  try { return monthlyPolicySchema.parse(JSON.parse(raw)); } catch { throw new ActivityError(503, "monthly_attendance_policy_invalid"); }
}
export function monthlyCells(policy: MonthlyPolicy): Array<{ cell: number; rewards: ActivityGrantReward[]; unavailableReason: string | null }> {
  const ids = ["cur_credits", "cur_machine_parts", "cur_operator_exp", "cur_tactical_data"];
  const amounts = [[1000, 50, 100, 25], [1500, 75, 150, 40], [2000, 100, 200, 55], [2500, 125, 250, 70]];
  return Array.from({ length: 28 }, (_, i) => {
    const week = Math.floor(i / 7), slot = i % 7;
    let rewards: ActivityGrantReward[];
    if (slot === 0) rewards = [{ kind: "CRYSTAL", quantity: 50 }];
    else if (slot === 6) rewards = week < 3 ? [{ kind: "CRYSTAL", quantity: [100, 150, 200][week]! }]
      : policy.eliteTicketItemId ? [{ id: policy.eliteTicketItemId, quantity: 1 }] : [];
    else { const type = slot === 5 ? 0 : slot - 1; rewards = [{ kind: "CURRENCY", id: ids[type]!, quantity: amounts[week]![type]! }]; }
    return { cell: i + 1, rewards, unavailableReason: i === 27 && !policy.eliteTicketItemId ? "elite_ticket_binding_missing" : null };
  });
}
