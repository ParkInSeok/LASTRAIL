import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { readOrdinaryFiveStatOrigin } from './combat-ordinary-stat-owner';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { firstPlayableProfile, firstPlayableProfileV2, firstPlayableProfileV3, firstPlayableProfileV4 } from './combat-first-playable-profile';

export const firstBattleEncounterSourceVersion = 'combat.first-battle-encounter-source.v1.20261026';
export const firstBattleEncounterSourceV2Version = 'combat.first-battle-encounter-source.v2.20261026';
export const firstBattleEncounterSourceV3Version = 'combat.first-battle-encounter-source.v3.20261027';
export const firstBattleEncounterSourceV4Version = 'combat.first-battle-encounter-source.v4.20261027';
export const firstBattleEncounterSourceV5Version = 'combat.first-battle-encounter-source.v5.20261027';
export const firstBattleEncounterSourceV6Version = 'combat.first-battle-encounter-source.v6.20261028';
export const firstBattleEncounterSourceV7Version = 'combat.first-battle-encounter-source.v7.20261028';
export const firstBattleEncounterSourceV8Version = 'combat.first-battle-encounter-source.v8.20261028';
const json = (value: unknown) => canonicalJson(z.json().parse(value));
type Original = Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>;

/** Frozen non-executable contract. A later issued definition needs its own version;
 * support roster and first-clear rewards are not first-account eligibility proofs. */
export async function captureFirstBattleEncounterSource(input: unknown, originHash: string) {
  const origin = await readOrdinaryFiveStatOrigin(input, originHash);
  const packet = {
    version: firstBattleEncounterSourceVersion,
    approval: 'FIRST-BATTLE-ENCOUNTER-SOURCE-v1.20261026',
    accountId: origin.accountId, runId: origin.runId, originHash,
    formationRevision: origin.formationRevision,
    status: 'DEFINITION_NOT_ISSUED', eligibility: 'NOT_EVALUATED', definition: null,
    canStart: false, runtimeSupported: false,
    sequence: ['SINGLE_APPROACH', 'SINGLE_RAIL_ARTILLERY', 'INDEPENDENT_BRANCH', 'ONE_SIDE_BOARDING', 'TWO_THREATS'],
    references: { enemies: null, artillery: null, rail: null, branch: null, threatBudget: null,
      entryConditions: null, resolutionConditions: null, eligibilityPolicy: null, missedTriggerPolicy: null },
    progressOwner: 'EXISTING_ENCOUNTER_CHECKPOINT',
    progressContents: ['CURRENT_STAGE', 'ISSUED_THREAT_IDENTITIES_AND_STATE', 'ORIGINAL_TIMERS', 'BRANCH_SELECTION'],
    progressPolicy: 'RESUME_WITHOUT_REISSUING_STAGE_ENTRY_NO_STAGE_GET',
    updatePolicy: 'NEW_RUNS_ONLY_RETAIN_ORIGINAL_VERSION',
    terminalPolicy: 'EXISTING_TERMINAL_RESULT_PATH_STAGE_RESOLUTION_IS_NOT_VICTORY',
    missing: ['APPROVED_ENCOUNTER_DEFINITION_AND_REFERENCES', 'FIRST_ACCOUNT_ELIGIBILITY_AND_UNLOCK_POLICY',
      'ACTUAL_GEOMETRY_AND_SPEED_WINDOW_VALIDATION', 'EXISTING_MISSED_TRIGGER_POLICY'],
  };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** Approved requirements only; no account unlocks or executable geometry are fabricated. */
export async function captureFirstBattleEncounterSourceV2(input: unknown, originHash: string) {
  const previous = await captureFirstBattleEncounterSource(input, originHash);
  const packet = { ...previous.packet, version: firstBattleEncounterSourceV2Version,
    entryRequirements: {
      approval: 'FIRST-BATTLE-ENTRY-REQUIREMENTS-v1.20261026',
      required: ['SPEED_CONTROL', 'BRANCH_SELECTION', 'CONNECTED_ENCOUNTER_DEFINITION', 'REQUIRED_ASSETS'],
      optional: ['SHIELD_USE', 'TRAIN_FIRE_USE', 'ULTIMATE_USE'],
      optionalUnlockOrChargeRequired: false, additionalOperatorRequirements: [],
      ordinaryAdmission: 'PRESERVE_ACTUAL_OWNED_FORMATION_PROGRESS_COST_AND_SUPPORTED_ROSTER',
      validation: 'AUTHORITATIVE_CONFIRMATION_REUSE_EXISTING_PREVIEW_NO_ADDITIONAL_GET',
      temporaryCooldown: 'EXISTING_PREPARATION_AND_EXECUTION_CONTRACT_NOT_PERMANENT_UNSUPPORTED',
      requiredCounterplay: 'AT_LEAST_ONE_ACTUAL_SPEED_OR_ROUTE_RESPONSE_WITHOUT_OPTIONAL_FEATURES',
      unavailableDefinitionReason: 'BATTLE_NOT_READY_NOT_FORMATION_FAULT',
      grantPolicy: 'NO_FREE_GRANTS_LOANS_UNLOCKS_OR_CHARGE',
    },
    missing: previous.packet.missing.map(reason => reason === 'FIRST_ACCOUNT_ELIGIBILITY_AND_UNLOCK_POLICY'
      ? 'ACTUAL_FIRST_ACCOUNT_ADMISSION_AND_REQUIRED_FEATURE_EXECUTION_PROOF' : reason),
  };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureFirstBattleEncounterSourceV3(input: unknown, originHash: string) {
  const previous = await captureFirstBattleEncounterSourceV2(input, originHash);
  const definition = await firstPlayableProfile();
  const packet = { ...previous.packet, version: firstBattleEncounterSourceV3Version,
    status: 'PLACEMENT_PROFILE_ISSUED_RUNTIME_UNBOUND', definition,
    references: { ...previous.packet.references,
      enemies: { definitionId: definition.enemies.definition.definitionId,
        version: definition.enemies.definition.version, typeHash: definition.enemies.definition.typeHash },
      artillery: { appearanceRef: definition.artillery.appearanceRef, hazardVersion: definition.artillery.hazardVersion },
    },
    appliedScaling: null,
    missing: ['AUTHORITATIVE_RUN_RISK_AND_PROGRESS_BINDING', 'ACTUAL_RAIL_GEOMETRY_SPEED_AND_PLACEMENT',
      'ACTUAL_FIRST_ACCOUNT_ADMISSION_AND_REQUIRED_FEATURE_EXECUTION_PROOF',
      'REMAINING_LEARNING_STAGES_AND_EXISTING_MISSED_TRIGGER_POLICY'],
  };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureFirstBattleEncounterSourceV4(input: unknown, originHash: string, selectedRisk: unknown) {
  const previous = await captureFirstBattleEncounterSourceV3(input, originHash);
  const packet = { ...previous.packet, version: firstBattleEncounterSourceV4Version,
    selectedRisk: z.enum(['I', 'II', 'III', 'IV', 'V']).parse(selectedRisk),
    scalingPolicy: { approval: 'FIRST-PLAYABLE-PROGRESS-SCALING-v1.20261027',
      binding: 'VERIFIED_MAP_ARRIVAL_RECEIPT_BEFORE_ENCOUNTER_ACTORS',
      scope: 'PUBLISHED_MATCHING_PROFILE_AT_ACTIVE_DEPTH_1_TO_11',
      lifetime: 'SAME_ENCOUNTER_GUNNERS_AND_ARTILLERY', historicalBackfill: false },
  };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureFirstBattleEncounterSourceV5(input: unknown, originHash: string, selectedRisk: unknown) {
  const previous = await captureFirstBattleEncounterSourceV4(input, originHash, selectedRisk);
  const definition = await firstPlayableProfileV2();
  const packet = { ...previous.packet, version: firstBattleEncounterSourceV5Version, definition,
    references: { ...previous.packet.references, artillery: { appearanceRef: definition.artillery.appearanceRef,
      hazardVersion: definition.artillery.hazardVersion } } };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureFirstBattleEncounterSourceV6(input: unknown, originHash: string, selectedRisk: unknown) {
  const previous = await captureFirstBattleEncounterSourceV5(input, originHash, selectedRisk);
  const packet = { ...previous.packet, version: firstBattleEncounterSourceV6Version, definition: await firstPlayableProfileV3() };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureFirstBattleEncounterSourceV7(input: unknown, originHash: string, selectedRisk: unknown) {
  const previous = await captureFirstBattleEncounterSourceV6(input, originHash, selectedRisk);
  // Two populated references duplicate the definition; the rest are null placeholders, not owned facts.
  const { references, ...previousPacket } = previous.packet;
  void references;
  const packet = { ...previousPacket, version: firstBattleEncounterSourceV7Version,
    definition: await firstPlayableProfileV4() };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** Static v7 body remains available for the entire lifetime of runs that reference it. */
export async function firstBattleStaticDefinition(input: unknown, originHash: string, selectedRisk: unknown) {
  const source = await captureFirstBattleEncounterSourceV7(input, originHash, selectedRisk);
  const { accountId, runId, originHash: originalHash, formationRevision, selectedRisk: risk, ...body } = source.packet;
  void accountId; void runId; void originalHash; void formationRevision; void risk;
  const definition = { version: 'combat.first-battle-static.v1.20261028', body };
  return { ...definition, definitionHash: await sha256Hex(json(definition)) };
}

export async function captureFirstBattleEncounterSourceV8(input: unknown, originHash: string, selectedRisk: unknown) {
  const origin = await readOrdinaryFiveStatOrigin(input, originHash);
  const risk = z.enum(['I', 'II', 'III', 'IV', 'V']).parse(selectedRisk);
  const definition = await firstBattleStaticDefinition(origin, originHash, risk);
  const packet = { version: firstBattleEncounterSourceV8Version, accountId: origin.accountId, runId: origin.runId,
    originHash, formationRevision: origin.formationRevision, selectedRisk: risk,
    definitionRef: { version: definition.version, definitionHash: definition.definitionHash } };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** Use only a source already checked against the original START by the reader below. */
export async function resolveFirstBattleProfile(source: NonNullable<Awaited<ReturnType<typeof readFirstBattleEncounterFromOriginal>>>) {
  return 'definition' in source.packet ? source.packet.definition : firstPlayableProfileV4();
}

/** Absence is historical, never a request to backfill today's learning definition. */
export async function readFirstBattleEncounterFromOriginal(original: Original) {
  try {
    const snapshot = z.record(z.string(), z.unknown()).parse(JSON.parse(original.startSnapshotJson));
    if (!Object.hasOwn(snapshot, 'firstBattleEncounterSource')) return null;
    const stored = z.object({ packet: z.object({ version: z.string() }) }).parse(snapshot.firstBattleEncounterSource);
    const producer = stored.packet.version === firstBattleEncounterSourceV8Version
      ? (input: unknown, hash: string) => captureFirstBattleEncounterSourceV8(input, hash, snapshot.risk)
      : stored.packet.version === firstBattleEncounterSourceV7Version
      ? (input: unknown, hash: string) => captureFirstBattleEncounterSourceV7(input, hash, snapshot.risk)
      : stored.packet.version === firstBattleEncounterSourceV6Version
      ? (input: unknown, hash: string) => captureFirstBattleEncounterSourceV6(input, hash, snapshot.risk)
      : stored.packet.version === firstBattleEncounterSourceV5Version
      ? (input: unknown, hash: string) => captureFirstBattleEncounterSourceV5(input, hash, snapshot.risk)
      : stored.packet.version === firstBattleEncounterSourceV4Version
      ? (input: unknown, hash: string) => captureFirstBattleEncounterSourceV4(input, hash, snapshot.risk)
      : stored.packet.version === firstBattleEncounterSourceVersion ? captureFirstBattleEncounterSource
      : stored.packet.version === firstBattleEncounterSourceV2Version ? captureFirstBattleEncounterSourceV2
      : stored.packet.version === firstBattleEncounterSourceV3Version ? captureFirstBattleEncounterSourceV3 : null;
    if (!producer) throw new Error('unsupported source version');
    const expected = await producer(original.origin, original.originHash);
    if (json(snapshot.firstBattleEncounterSource) !== json(expected)) throw new Error('source mismatch');
    return expected;
  } catch { throw new ActivityError(409, 'first_battle_source_invalid'); }
}
