import { z } from "zod";

export const frontlinePolicyId = "ce03.frontline-first-v1";
export const frontlineSimulationVersion = "ce03.execution.2026-09-13.001";
export const frontlinePolicySha256 = "08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052";
export const localTargetingOptionsSchema = z.strictObject({ targetingPolicyId: z.literal(frontlinePolicyId).optional() });
export type LocalTargetingOptions = z.infer<typeof localTargetingOptionsSchema>;

export const frontlineExecutionPolicy = {
  version: frontlineSimulationVersion,
  orderedStages: ["P1.expire-recover-spawn", "P1.commands", "P1.schedule", "P1.sequential-effects", "P1.terminal-wave", "P1.checkpoint"],
  targetTiePolicy: frontlinePolicyId, sameTickTerminalPolicy: "P1.failure-priority", interruptPolicy: "P3",
  statusReapplicationPolicy: "P4.status", shieldLayerPolicy: "P4.shield", packageCombinationPolicy: "P5",
  damageRoundingPolicy: "P5.final-floor", recoveryPolicy: "P6", waveActivationPolicy: "P6.r2",
};

// Exact UTF-8/LF text is a new source pin. R1/R2 still define unmodified rules.
export const frontlinePolicySource = `CE03 frontline-first targeting policy v1
Identity: ce03.frontline-first-v1
Execution: ce03.execution.2026-09-13.001
Scope: enemy general basic attacks whose target category is Operators.
Keep existing valid-taunt selection and precedence unchanged.
Otherwise choose living, available non-train/non-device operators in logical frontSlots if any; otherwise choose eligible rearSlots.
This overrides both ordinary front_ally and rear_ally authored preferences, not explicit SkillTargets.
Keep logical slot ascending, ordinal actor ID tie break and existing rotating target cursor.
RecoverOperators runs before new selection; recovered front actors affect the next selection only.
Protected living operators remain eligible. No eligible operators means wait; do not fall back to the train.
Keep valid reserved target/source/due/sequence; target death uses existing full-windup reselection and no second cooldown.
Keep existing source-death handling, explicit skills, exterior targeting and every other execution policy unchanged.
Use formation data rather than screen coordinates; producer default frontSlots=[3,4], rearSlots=[0,1,2].
Old prepared profiles and accepted runs do not opt in. New identity must be selected during fresh model construction.
New checkpoint policy/schema identity must reject legacy restoration through the new factory and vice versa; no silent migration.
`;
