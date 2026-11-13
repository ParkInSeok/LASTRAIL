import { canonicalJson, sha256Hex } from "@lastrail/common";
import { bindCombatPolicyRequirements } from "./combat-policy-binding";
import { captureCombatPolicyRequirements } from "./combat-policy-catalog";

/** Definition intake only. The roster below is an index, never an account formation or START profile. */
export async function publishCombatDefinitionContract() {
  const policy = captureCombatPolicyRequirements();
  const binding = bindCombatPolicyRequirements({
    version: "departure-v4", combatPolicyRequirements: policy,
    formation: { operators: policy.scope.roster.map((operatorId, slot) => ({ operatorId, slot })) },
  }, policy.scope.ordinaryRisk);
  if (binding.status !== "EXECUTION_UNAVAILABLE" || binding.operators.length !== policy.scope.roster.length) {
    throw new Error("combat_definition_source_invalid");
  }
  const definition = {
    policy,
    // Bind once through the existing approved owner of Kyle/Reina and catalog kits.
    operators: binding.operators.map(({ slot, ...operator }) => { void slot; return operator; }),
    requiredEffectKinds: binding.requiredEffectKinds,
    effectiveStatProfiles: null,
    startSnapshot: null,
    effectRuntime: null,
    missingRuntimeInputs: [
      "ENTITY_ID_OWNER_FACTION_PLACEMENT_AND_CAPABILITIES",
      "HP_ATTACK_GROWTH_EQUIPMENT_RESEARCH_AND_MODIFIER_PROVENANCE",
      "ELIGIBLE_SOURCE_OWNER_TARGET_IDS_AND_EFFECT_GROUPS",
      "ACCEPTANCE_TARGET_LOCK_ACTION_QUEUE_AND_EFFECT_EVENT_IDENTITY",
      "VITALS_SHIELD_DOWN_RECOVERY_COOLDOWN_AND_ULTIMATE_STATE",
      "ENEMY_WAVE_TARGET_RANGE_AND_OPERATION_DEFINITIONS",
      "PINNED_START_CHECKPOINT_AND_RESULT_REPLAY_BINDING",
    ],
  };
  return {
    schemaVersion: "combat.definition-intake.v1",
    definitionVersion: policy.version,
    contentHash: `sha256:${await sha256Hex(canonicalJson(definition))}`,
    purpose: "DEFINITION_INTAKE_ONLY",
    canExecute: false,
    support: { definitions: "AVAILABLE", runtimeBinding: "MISSING", executionEvidence: "NOT_RUN" },
    blockingReasons: ["effective_stat_profiles_missing", "effect_runtime_binding_missing",
      "operation_definitions_missing", "start_checkpoint_replay_binding_missing", ...binding.blockingReasons],
    definition,
  };
}

export type CombatDefinitionContract = Awaited<ReturnType<typeof publishCombatDefinitionContract>>;

/** A content digest detects corruption; exact producer equality also rejects unapproved, rehashed data.
 * This current-publication reader is deliberately NOT a historical START snapshot reader.
 */
export async function readCombatDefinitionContract(input: unknown): Promise<CombatDefinitionContract> {
  const expected = await publishCombatDefinitionContract();
  let equal = false;
  try { equal = canonicalJson(input as Parameters<typeof canonicalJson>[0]) === canonicalJson(expected); }
  catch { /* External data is invalid, not executable. */ }
  if (!equal) throw new Error("combat_definition_contract_mismatch");
  return expected;
}

/** No handler/owned-stat evidence exists in this intake contract. Never promote it to execution. */
export function requireExecutableCombatDefinition(contract: CombatDefinitionContract): never {
  void contract;
  throw new Error("combat_definition_execution_unavailable");
}
