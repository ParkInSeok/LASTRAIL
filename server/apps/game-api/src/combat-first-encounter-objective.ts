import { canonicalJson, sha256Hex } from '@lastrail/common';

/** Shared objective definition. Issuing this reference is not evidence of completion. */
export async function firstEncounterObjectiveDefinition() {
  const definition = {
    version: 'combat.first-objective.v1.20261028', approval: 'ORDINARY-FIRST-ENCOUNTER-OBJECTIVE-v1.20261028',
    scope: 'FIRST_TWO_ENCOUNTERS_ONLY',
    required: ['TWO_ORIGINAL_GUNNERS_DEFEATED', 'ORIGINAL_SINGLE_ARTILLERY_RESOLVED', 'TRAIN_ALIVE'],
    artillery: { resolvedOutcomes: ['HIT', 'MISSED', 'CANCELLED_BEFORE_LAUNCH'],
      unresolvedLaunchedProjectiles: 0, absentHazardIsResolved: false,
      cancellationAuthority: 'EXISTING_TRAIN_FUNCTION_BEFORE_LAUNCH',
      destructionAfterLaunch: 'WAIT_FOR_ORIGINAL_PROJECTILE_RESOLUTION',
      launcherDestructionRequired: false, exhaustedLauncherMayFireAgain: false,
      automaticDestructionOrKillCredit: false, operatorDirectAttack: false },
    survival: { trainHpMustBePositiveAfterDamageCommit: true, defeatOrder: 'EXISTING_EVENT_AND_TERMINAL_OWNER' },
    evidence: { owner: 'EXISTING_CHECKPOINT_OWNER',
      required: ['ORIGINAL_GUNNER_IDENTITIES_AND_DEFEATS', 'ORIGINAL_HAZARD_TERMINAL_EVENT',
        'SIX_TARGET_FINAL_STATE', 'UNRESOLVED_PROJECTILES', 'COMBAT_CLOCK', 'CHECKPOINT_REVISION',
        'ORIGINAL_NODE_OBJECTIVE_DEFINITION'], clientCompletedBooleanAccepted: false },
    nodeCompletion: 'ONLY_IF_PUBLISHED_NODE_REQUIRED_OBJECTIVES_EXACTLY_MATCH_AND_NO_OTHER_THREATS',
    additionalObjectives: 'KEEP_NODE_IN_PROGRESS', operationVictoryAndRewards: 'EXISTING_UPPER_LEVEL_AUTHORITY',
    timeOrRailEndCompletesObjective: false,
  };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}
