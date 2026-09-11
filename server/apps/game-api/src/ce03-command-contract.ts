import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { id, pos } from "./ce03-local-schema";

// Existing executable IDs stay unchanged. Seven NEW local selection keys below
// name accepted roles only: they do not claim an existing ability or effect.
export const localSupportCatalog = Object.freeze({ rulesVersion: "ce03-support-selection-v1", catalogVersion: "ce03-local-support-roles-v1",
  skillIds: Object.freeze(["support_fire", "barrier", "emergency_repair", "ce03.role.suppressive_barrage", "ce03.role.medical_support",
    "ce03.role.smoke", "ce03.role.weakness_scan", "ce03.role.firepower_boost", "ce03.role.electronic_disruption", "ce03.role.emergency_protection"]) });
export const localLeaderChargePolicy = Object.freeze({ version: "basic-hp-hit-v1", scope: "LOCAL_QA_OPT_IN", perHit: 4,
  overrides: "ULTIMATE_FIRST_READY_AND_COOLDOWN" } as const);
export const leaderPolicySchema = z.strictObject({ version: z.literal("basic-hp-hit-v1"), scope: z.literal("LOCAL_QA_OPT_IN"),
  perHit: z.literal(4), overrides: z.literal("ULTIMATE_FIRST_READY_AND_COOLDOWN") });
export const commandContractSchema = z.strictObject({ version: z.literal("ce03-command-contract-v1"),
  support: z.strictObject({ rulesVersion: id, catalogVersion: id, revision: pos,
    slots: z.array(z.strictObject({ slot: z.number().int().min(0).max(2), skillId: id })).length(3) }),
  supportChannel: z.literal("SHARED_TRAIN_MANUAL_CAST"), leader: leaderPolicySchema,
}).superRefine((c, ctx) => {
  if (new Set(c.support.slots.map(s => s.skillId)).size !== 3 || c.support.slots.some((s, i) => s.slot !== i))
    ctx.addIssue({ code: "custom", message: "ordered distinct support slots required" });
});
export type Ce03CommandContract = z.infer<typeof commandContractSchema>;
export function createCommandContract(support: unknown, leader: unknown): Ce03CommandContract {
  const value = commandContractSchema.safeParse({ version: "ce03-command-contract-v1", support, leader, supportChannel: "SHARED_TRAIN_MANUAL_CAST" });
  if (!value.success) throw new ActivityError(409, "ce03_command_contract_unavailable");
  return value.data;
}
