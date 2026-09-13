import { z } from "zod";
import { frontlineExecutionPolicy, frontlinePolicyId, localTargetingOptionsSchema } from "./ce03-targeting-policy";

export const individualUltimatePolicyId = "ce03.per-operator-ultimate-v1";
export const individualSimulationVersion = "ce03.execution.2026-09-13.002";
export const individualExecutionPolicySha256 = "68beb942a695ea761aa0f2aaf5677d7eee603aeeb3de078ffca148a27d824b50";
export const individualExecutionPolicy = { ...frontlineExecutionPolicy, version: individualSimulationVersion,
  orderedStages: frontlineExecutionPolicy.orderedStages.map(s => s === "P1.commands" ? "U1.manual-auto-commands" : s) };
export const generalActivePolicyId = "ce03.general-active-auto-v1";
export const generalAutoSimulationVersion = "ce03.execution.2026-09-13.003";
export const generalAutoExecutionPolicy = { ...individualExecutionPolicy, version: generalAutoSimulationVersion,
  orderedStages: individualExecutionPolicy.orderedStages.map(s => s === "U1.manual-auto-commands" ? "A1.manual-ultimate-auto-active-auto-commands" : s) };
export const generalAutoExecutionPolicySha256 = "266d1e98ef39c5596f0f8cf9f1bd78ce77207c943d1f3192fe7e5b3fd2c9e8d4";
export const localCombatOptionsSchema = localTargetingOptionsSchema.extend({ ultimatePolicyId: z.literal(individualUltimatePolicyId).optional(),
  activePolicyId: z.literal(generalActivePolicyId).optional() })
  .refine(o => o.ultimatePolicyId === undefined || o.targetingPolicyId === frontlinePolicyId, "individual ultimates require explicit frontline policy")
  .refine(o => o.activePolicyId === undefined || o.ultimatePolicyId === individualUltimatePolicyId, "automatic actives require individual ultimates");
export type LocalCombatOptions = z.infer<typeof localCombatOptionsSchema>;
export function captureCombatOptions(options: LocalCombatOptions) {
  return localCombatOptionsSchema.parse({ ...(options.targetingPolicyId === undefined ? {} : { targetingPolicyId: options.targetingPolicyId }),
    ...(options.ultimatePolicyId === undefined ? {} : { ultimatePolicyId: options.ultimatePolicyId }),
    ...(options.activePolicyId === undefined ? {} : { activePolicyId: options.activePolicyId }) });
}

export const individualUltimateSchema = z.strictObject({ version: z.literal("per-operator-basic-hp-hit-v1"), scope: z.literal("LOCAL_QA_OPT_IN"),
  eligibility: z.literal("ALL_FORMATION_OPERATORS"), chargeSource: z.literal("OWN_LIVING_BASIC_HP_LOSS"), perHit: z.literal(4), maximum: z.literal(100),
  overrides: z.literal("ULTIMATE_FIRST_READY_AND_COOLDOWN"), consumeAt: z.literal("CAST_ACCEPTANCE"),
  auto: z.strictObject({ defaultEnabled: z.literal(false), scope: z.literal("ULTIMATES_ONLY"),
    decisionStage: z.literal("AFTER_MANUAL_COMMANDS_BEFORE_BASIC_SCHEDULE"), order: z.literal("FORMATION_SLOT_THEN_ACTOR_ID"),
    eligibility: z.literal("READY_TARGETABLE_NO_PENDING_ACTION"), checkpoint: z.literal("RUN_LOCAL"), disable: z.literal("KEEP_ACCEPTED_CASTS") }) });
export const individualUltimatePolicy = individualUltimateSchema.parse({ version: "per-operator-basic-hp-hit-v1", scope: "LOCAL_QA_OPT_IN",
  eligibility: "ALL_FORMATION_OPERATORS", chargeSource: "OWN_LIVING_BASIC_HP_LOSS", perHit: 4, maximum: 100,
  overrides: "ULTIMATE_FIRST_READY_AND_COOLDOWN", consumeAt: "CAST_ACCEPTANCE",
  auto: { defaultEnabled: false, scope: "ULTIMATES_ONLY", decisionStage: "AFTER_MANUAL_COMMANDS_BEFORE_BASIC_SCHEDULE",
    order: "FORMATION_SLOT_THEN_ACTOR_ID", eligibility: "READY_TARGETABLE_NO_PENDING_ACTION", checkpoint: "RUN_LOCAL", disable: "KEEP_ACCEPTED_CASTS" } });

export const generalActiveAutoSchema = z.strictObject({ version: z.literal("general-active-auto-v1"), scope: z.literal("LOCAL_QA_OPT_IN"),
  eligibility: z.literal("ALL_FORMATION_OPERATOR_ACTIVES"), enabled: z.literal("ALWAYS"),
  readiness: z.literal("AUTHORED_FIRST_READY_AND_COOLDOWN"), consumeAt: z.literal("CAST_ACCEPTANCE"),
  decisionStage: z.literal("AFTER_MANUAL_AND_ULTIMATE_AUTO_BEFORE_BASIC_SCHEDULE"),
  order: z.literal("FORMATION_SLOT_THEN_ACTOR_ID"), preflight: z.literal("READY_TARGETABLE_NO_PENDING_ACTION"),
  queue: z.literal("SHARED_ACTOR_QUEUE"), trainSkills: z.literal("MANUAL_ONLY"), failedCandidate: z.literal("SKIP_WITHOUT_JOURNAL") });
export const generalActiveAutoPolicy = generalActiveAutoSchema.parse({ version: "general-active-auto-v1", scope: "LOCAL_QA_OPT_IN",
  eligibility: "ALL_FORMATION_OPERATOR_ACTIVES", enabled: "ALWAYS", readiness: "AUTHORED_FIRST_READY_AND_COOLDOWN", consumeAt: "CAST_ACCEPTANCE",
  decisionStage: "AFTER_MANUAL_AND_ULTIMATE_AUTO_BEFORE_BASIC_SCHEDULE", order: "FORMATION_SLOT_THEN_ACTOR_ID",
  preflight: "READY_TARGETABLE_NO_PENDING_ACTION", queue: "SHARED_ACTOR_QUEUE", trainSkills: "MANUAL_ONLY", failedCandidate: "SKIP_WITHOUT_JOURNAL" });

export const generalActiveAutoSource = `CE03 general active automation policy v1
Identity: ce03.general-active-auto-v1
Execution: ce03.execution.2026-09-13.003
This additive policy supersedes only the individual-ultimate-v1 sentence keeping general active skills manual. Its other charge/consumption/restore rules remain.
All five formation operator general ACTIVE skills are always automatic. No general AUTO toggle. The upper AUTO controls only five ultimates and defaults false. The three train skills remain manual.
At each executed fixed tick process queued manual commands, then eligible ultimate AUTO candidates if enabled, then eligible general ACTIVE candidates, then existing basic scheduling.
Within each automatic phase sort by logical formation slot ascending then ordinal actor ID. Use the existing shared actor queue/casting/basic reservation/recovery locks and at most one accepted automatic reservation per actor at the decision stage.
Before enqueueing, validate alive/available caster, authored firstReady/cooldown, targets and absence of pending actions. Skip candidates without valid targets or readiness without rejected journal entries. Never consume cooldown on a skipped candidate.
Preserve authored ACTIVE effects, cooldowns, target rules, windup, recovery and consumption at CAST_ACCEPTANCE. Earlier accepted casts are not preempted. Manual ultimates received before the decision boundary take precedence over both automatic phases; later inputs belong to the next boundary.
Use a shared checkpointed automatic command sequence with deterministic IDs for both automatic phases. Preserve actual cooldown timestamps across down/recovery and restore. Paused and terminal combat make no automatic decisions.
Use a distinct configuration/checkpoint identity from legacy and ultimate-only .002. Persist individual charge, ultimate AUTO flag and automatic sequence with existing skill/queue state; do not reinterpret old checkpoints or prepared profiles.
Remove general ACTIVE manual buttons and show their actual remaining cooldown below world HP. No cooldown countdown invented from availability or target absence. Ultimate AUTO OFF never disables general ACTIVE automation.
`;

export const individualUltimateSource = `CE03 individual ultimate policy v1
Identity: ce03.per-operator-ultimate-v1
Execution: ce03.execution.2026-09-13.002
Each formation operator gains4 charge only from its own resolved basic hit with actual HpLoss>0 while the source is alive; maximum100.
No charge from skill hits, shield-only absorption, zero HP loss, train/device/enemy attacks or dead-source committed hits. Never copy shared charge to each actor.
Preserve authored abilities and firstReady/cooldown metadata. Effective charge-only overrides firstReady and cooldown checks; do not add a45-second AND gate.
All formation operators can manually request their own ultimate. Spend only that actor's charge once at CAST_ACCEPTANCE; failed requests do not spend. Existing accepted cast cancellation/refund rules remain.
Charges persist across down/recovery, pause and checkpoint restoration.
AUTO defaults false for new runs and is stored in the current run checkpoint. A toggle does not execute a combat step.
At each executed fixed tick process manual commands first, then AUTO candidates in logical slot ascending and ordinal actor ID order, before basic scheduling.
An AUTO candidate must be alive, available, charge100, have valid skill targets, and have no queued/casting command, basic reservation or recovery lock. Skip ineligible candidates without recording failed commands.
One automatic reservation per eligible actor per tick; deterministic automatic command IDs use a checkpointed sequence. Existing actor queue prevents manual/AUTO duplicates.
AUTO OFF stops future choices but preserves accepted casts. Pause and terminal states do not auto-cast. General active skills and the three train manual skills are never automated.
New template selectedUltimateOperatorId is null. Historical owned ultimateSelection is a nullable revision fact, not an eligibility gate. Legacy profiles retain their old selected leader and shared gauge.
New configuration/checkpoint identity stores policy, exact actor charge map, AUTO flag and automatic sequence. Strict old/new restoration separation; no silent migration.
`;
