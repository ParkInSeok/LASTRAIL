import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { captureOwnedCombatBaseDefinitions } from './combat-owned-base-definitions';
import { miraBasicDefinition } from './combat-b-mira-basic-source';
import { readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { nyxApprovedDefinitionV2 } from './combat-nyx-approved-source';
import { ordinaryKitV3Version } from './combat-ordinary-kit-source';

export const ordinaryBasicVersion = 'combat.ordinary-basic.v1.20261026';
type Kit = Awaited<ReturnType<typeof readOrdinaryKitFromOriginal>>;
type Original = Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>;
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const digest = (value: unknown) => sha256Hex(json(value));
const fail = (reason: string): never => { throw new ActivityError(409, `ordinary_basic_${reason}`); };

/** Internal START producer: consumes the already validated kit. Frozen v1;
 * changes require a new version and retained reader, never a historical backfill. */
export async function captureOrdinaryBasicSource(kit: Kit) {
  return captureBasic(kit, false);
}
async function captureBasic(kit: Kit, nyx: boolean) {
  if ((kit.packet.version === ordinaryKitV3Version) !== nyx) return fail('version_scope_conflict');
  const base = captureOwnedCombatBaseDefinitions(), mira = await miraBasicDefinition();
  let profiles = [
    { operatorId: 'op_reina', abilityId: 'op_reina.basic', kind: 'BASIC', modes: ['MELEE', 'RANGED'],
      attackForm: 'PROJECTILE', launchSource: 'ACTUAL_WEAPON_MUZZLE',
      originalBasicIntervalMs: base.operators.op_reina.basicIntervalMs,
      range: { physicalAdjacentCars: 1, maximumDistanceMm: 24000 },
      ballistics: { speedMmPerSecond: 72000, radiusMm: 35, maximumFlightDistanceMm: 24000,
        maximumFlightMs: { numerator: 1000, denominator: 3 } },
      timing: { baseCycleMs: 1400, windupMs: 180, recoveryMs: 220 },
      approval: 'BASIC-REINA-SERA-v1.20261026' },
    { operatorId: 'op_sera', abilityId: 'op_sera.basic', kind: 'BASIC', modes: ['MELEE', 'RANGED'],
      attackForm: 'PROJECTILE', launchSource: 'ACTUAL_FRONT_HAND_EMISSION',
      originalBasicIntervalMs: base.operators.op_sera.basicIntervalMs,
      range: { physicalAdjacentCars: 1, maximumDistanceMm: 18000 },
      ballistics: { speedMmPerSecond: 36000, radiusMm: 80, maximumFlightDistanceMm: 18000,
        maximumFlightMs: { numerator: 500, denominator: 1 } },
      timing: { baseCycleMs: 1200, windupMs: 240, recoveryMs: 260 },
      approval: 'BASIC-REINA-SERA-v1.20261026' },
    { operatorId: 'op_mira', abilityId: mira.ability.id, kind: 'BASIC', modes: ['RANGED'],
      attackForm: mira.ability.attackForm, launchSource: 'ACTUAL_WEAPON_MUZZLE',
      originalBasicIntervalMs: mira.ability.originalBasicIntervalMs,
      range: { physicalAdjacentCars: mira.ability.range.physicalAdjacentCars,
        maximumDistanceMm: mira.ability.range.maximumDistanceMm },
      ballistics: { speedMmPerSecond: mira.ballistics.speedMmPerSecond, radiusMm: mira.ballistics.radiusMm,
        maximumFlightDistanceMm: mira.ballistics.maximumFlightDistanceMm,
        maximumFlightMs: { numerator: mira.ballistics.maximumFlightMs, denominator: 1 } },
      timing: null, approval: 'BASIC-MIRA-EVE-A-v1.20261026' },
  ];
  if (nyx) {
    const source = nyxApprovedDefinitionV2().basic;
    profiles = profiles.filter(p=>p.operatorId!=='op_mira');
    profiles.push({ operatorId:'op_nyx',abilityId:source.abilityId,kind:'BASIC',modes:source.modes,
      attackForm:source.attackForm,launchSource:source.launchSource,originalBasicIntervalMs:source.timing.baseCycleMs,
      range:source.range,ballistics:{...source.ballistics,maximumFlightMs:{numerator:source.ballistics.maximumFlightMs,denominator:1}},
      timing:source.timing,approval:'NYX-BASIC-TRIAL-v1.20261107' });
  }
  if (profiles.some(p => !kit.packet.operators.some(o => o.operatorId === p.operatorId)
    || kit.packet.operators.some(o => o.abilities.some(a => a.id === p.abilityId))
    || (p.timing !== null && p.originalBasicIntervalMs !== p.timing.baseCycleMs))) return fail('definition_conflict');
  const packet = { version: ordinaryBasicVersion, accountId: kit.packet.accountId, runId: kit.packet.runId,
    originHash: kit.packet.originHash, kitHash: kit.kitHash, scope: 'ORDINARY_SOLO_FIVE_APPROVED_BASIC',
    baseVersion: base.version, profiles,
    damagePolicy: 'ORIGINAL_DAMAGE_COEFFICIENT_COUNT_AND_GENERAL_CRITICAL_RESEARCH_MODIFIERS',
    opportunityPolicy: 'ONE_ORIGINAL_BASIC_OPPORTUNITY_PRESERVE_ORIGINAL_EXTRA_PROJECTILE_RULES',
    spatialPolicy: { target: 'ORIGINAL_ENEMY_ELIGIBILITY_EXCLUDING_TRAIN_ARTILLERY',
      range: 'INTERSECTION_WITH_ORIGINAL_DOMAIN_LOS_AND_WORLD_DISTANCE_FROM_MUZZLE_TO_HIT_ANCHOR',
      geometry: 'SAME_COMMIT_REVISION_ACTUAL_MUZZLE_HIT_SHAPE_CARRIAGE_AND_OCCLUSION',
      trajectory: 'FIXED_DIRECTION_NON_HOMING_SWEPT_SPHERE',
      lifetime: 'CEIL_TO_ORIGINAL_STEP_WITHOUT_EXTENDING_PATH_DISTANCE',
      attackEvaluation: 'AT_FIRE', targetDefenseEvaluation: 'AT_IMPACT' },
    timingPolicy: { appliesTo: ['op_reina.basic', 'op_sera.basic'],
      cycle: 'READY_OPPORTUNITY_START_TO_NEXT_START_INCLUDES_WINDUP_AND_RECOVERY',
      scaling: 'EFFECTIVE_CYCLE_TIMES_BASE_PHASE_OVER_BASE_CYCLE',
      snapshot: 'FREEZE_CYCLE_TARGET_MODE_AND_GENERATION_AT_OPPORTUNITY_START',
      quantization: 'ORIGINAL_STEP_CUMULATIVE_BOUNDARIES_NOT_INDEPENDENT_PHASE_ROUNDING',
      commit: 'ONCE_RECHECK_LIFE_RANGE_LOS_NO_RETARGET_TRANSFER',
      cancellation: 'NO_FIRE_HIT_OR_CHARGE_RECOVER_WITHOUT_ADVANCING_ORIGINAL_NEXT_ATTACK',
      priorityAndSameTickOrder: 'EXISTING_ACTION_OWNER',
      restore: 'RETAIN_PHASE_REMAINDER_TARGET_GENERATION_ATTACK_ID_COMMIT_AND_INFLIGHT_PROJECTILES' },
    seraSeparation: 'ONE_ATTACK_PROJECTILE_NOT_INSTANT_HEAL_OR_OVERHEAL_LIFE_PROJECTILE_NO_CRYSTAL_MULTISHOT',
    eveCarrier: { approval: 'BASIC-MIRA-EVE-A-v1.20261026', role: 'OWNER_BASIC_LAUNCH_MEDIUM',
      independentAutoFire: false, independentHp: false, additionalDamageOrCharge: false,
      activeDrone: 'SEPARATE_ORIGINAL_ACTIVE_ULTIMATE_OWNERSHIP_AND_LIFETIME', ballistics: null, socket: null },
    missing: ['ACTUAL_GEOMETRY_AND_SOCKETS', 'KYLE_BASIC_PROFILE', 'EVE_BASIC_BALLISTICS', 'MIRA_ACTION_TIMING',
      'UNITY_ACTION_CHECKPOINT_AND_RESULT_BINDING'],
    runtimeSupported: false, canStart: false };
  return { packet, basicHash: await digest(packet) };
}

export const ordinaryBasicV2Version = 'combat.ordinary-basic.v2.20261026';

/** Additive v2. The v1 producer above remains the retained historical definition. */
export async function captureOrdinaryBasicSourceV2(kit: Kit) {
  return captureBasicV2(kit, false);
}
async function captureBasicV2(kit: Kit, nyx: boolean) {
  const previous = await captureBasic(kit, nyx), base = captureOwnedCombatBaseDefinitions();
  const additions = [
    { operatorId: 'op_kyle', abilityId: 'op_kyle.basic', kind: 'BASIC', modes: ['MELEE', 'RANGED'],
      attackForm: 'MODE_DEPENDENT_CONTACT_OR_PROJECTILE', launchSource: 'ACTUAL_HAND_EMISSION_FOR_RANGED',
      originalBasicIntervalMs: base.operators.op_kyle.basicIntervalMs,
      range: { appliesTo: 'RANGED', physicalAdjacentCars: 0, maximumDistanceMm: 18000 },
      ballistics: { speedMmPerSecond: 30000, radiusMm: 100, maximumFlightDistanceMm: 18000,
        maximumFlightMs: { numerator: 600, denominator: 1 } },
      melee: { maximumDistanceMm: 1800, rangeOrigin: 'ACTOR_COMBAT_BODY_TO_TARGET_HIT_ANCHOR',
        physicalAdjacentCars: 0, contactRadiusMm: 120, contactSource: 'ACTUAL_FIST_ANCHOR_AT_COMMIT',
        hit: 'SINGLE_SELECTED_TARGET_SHAPE_OVERLAP_AND_LOS_REQUIRED_NO_REMOTE_HIT',
        damageEvaluation: 'ATTACK_AND_DEFENSE_AT_SAME_COMMIT',
        surface: 'SAME_ACCESSIBLE_ROOF_AND_ORIGINAL_MELEE_ELIGIBILITY' },
      timing: { baseCycleMs: 1800, windupMs: 300, recoveryMs: 350 },
      approval: 'BASIC-KYLE-EVE-v1.20261026' },
    { operatorId: 'op_eve', abilityId: 'op_eve.basic', kind: 'BASIC', modes: ['MELEE', 'RANGED'],
      attackForm: 'PROJECTILE', launchSource: 'ACTUAL_BASIC_CARRIER_MUZZLE',
      originalBasicIntervalMs: base.operators.op_eve.basicIntervalMs,
      range: { physicalAdjacentCars: 1, maximumDistanceMm: 24000 },
      ballistics: { speedMmPerSecond: 60000, radiusMm: 45, maximumFlightDistanceMm: 24000,
        maximumFlightMs: { numerator: 400, denominator: 1 } },
      timing: { baseCycleMs: 2000, windupMs: 250, recoveryMs: 300 },
      approval: 'BASIC-KYLE-EVE-v1.20261026' },
  ];
  if (additions.some(p => !kit.packet.operators.some(o => o.operatorId === p.operatorId)
    || kit.packet.operators.some(o => o.abilities.some(a => a.id === p.abilityId))
    || p.originalBasicIntervalMs !== p.timing.baseCycleMs)) return fail('definition_conflict');
  const packet = { ...previous.packet, version: ordinaryBasicV2Version,
    profiles: [...previous.packet.profiles, ...additions],
    spatialPolicy: { ...previous.packet.spatialPolicy, appliesTo: 'PROJECTILE_MODES_ONLY' },
    timingPolicy: { ...previous.packet.timingPolicy,
      appliesTo: [...previous.packet.timingPolicy.appliesTo, 'op_kyle.basic', 'op_eve.basic'],
      restore: 'RETAIN_MODE_PHASE_REMAINDER_NEXT_ATTACK_TARGET_GENERATION_ATTACK_ID_COMMIT_PROJECTILES_AND_CARRIER_ID' },
    kyleModePolicy: { selection: 'AT_READY_MELEE_IF_WITHIN_RANGE_AND_ACCESSIBLE_ELSE_ELIGIBLE_RANGED_ELSE_IDLE',
      gap: 'INACCESSIBLE_GAP_OR_OTHER_TRAIN_REQUIRES_ORIGINAL_RANGED_ELIGIBILITY',
      reselection: 'NEXT_READY_ONLY_NO_MID_WINDUP_SWITCH_OR_CYCLE_RESET',
      interception: 'SEPARATE_ORIGINAL_INTERCEPTION_TIMING_AND_BASIC_REMAINDER_PAUSE_RESUME',
      ranged: 'SINGLE_NON_HOMING_NON_PIERCING_ORIGINAL_AUGMENT_BOUNDARY_NO_SCIENCE_TAG' },
    eveCarrier: { ...previous.packet.eveCarrier, approval: 'BASIC-KYLE-EVE-v1.20261026',
      ballistics: { profileAbilityId: 'op_eve.basic' },
      attribution: 'EVE_ORIGINAL_BASIC_ATTACK_CRITICAL_LIFESTEAL_AND_CHARGE_EVENT_ONCE',
      placement: 'OWNER_OR_CARRIAGE_ALLOWED_AREA_NO_TARGET_CHASE_RANGE_EXTENSION',
      eligibility: 'OWNER_EVE_ORIGINAL_SUPPORT_DOMAIN_AND_ACTUAL_MUZZLE_RANGE_AND_LOS',
      commit: 'ONE_AT_WINDUP_END_NO_EXTRA_ALIGNMENT_DELAY_OR_GLOVE_ATTACK',
      unavailableMuzzle: 'FAIL_AND_RECOVER_NO_BODY_CENTER_FALLBACK',
      down: 'NO_NEW_BASIC_COMMANDS_ACTIVE_DRONE_CANNOT_SUBSTITUTE_KEEP_EXISTING_PROJECTILE_LIFETIME' },
    missing: ['ACTUAL_GEOMETRY_AND_SOCKETS', 'MIRA_ACTION_TIMING', 'UNITY_ACTION_CHECKPOINT_AND_RESULT_BINDING'] };
  return { packet, basicHash: await digest(packet) };
}

export const ordinaryBasicV3Version = 'combat.ordinary-basic.v3.20261026';
export const ordinaryBasicV4Version = 'combat.ordinary-basic.v4.20261107';
export const ordinaryBasicV5Version = 'combat.ordinary-basic.v5.20261107';
export async function captureOrdinaryBasicSourceV5(kit: Kit) {
  const previous = await captureOrdinaryBasicSourceV4(kit);
  const packet = { ...previous.packet, version: ordinaryBasicV5Version,
    timingPolicy: { ...previous.packet.timingPolicy,
      restore: 'RESTART_FROM_ORIGINAL_NODE_START_NO_MID_COMBAT_RESTORE' },
    missing: previous.packet.missing.map(reason => reason === 'UNITY_ACTION_CHECKPOINT_AND_RESULT_BINDING'
      ? 'UNITY_CURRENT_ATTEMPT_ACTION_AND_RESULT_BINDING' : reason) };
  return { packet, basicHash: await digest(packet) };
}
export async function captureOrdinaryBasicSourceV4(kit: Kit) {
  const previous = await captureBasicV2(kit, true);
  const packet = { ...previous.packet,version:ordinaryBasicV4Version,
    profiles: [...previous.packet.profiles].sort((a,b)=>kit.packet.operators.findIndex(o=>o.operatorId===a.operatorId)-kit.packet.operators.findIndex(o=>o.operatorId===b.operatorId)),
    timingPolicy:{...previous.packet.timingPolicy,appliesTo:[...previous.packet.timingPolicy.appliesTo,'op_nyx.basic']},
    missing:previous.packet.missing.filter(reason=>reason!=='MIRA_ACTION_TIMING') };
  return {packet,basicHash:await digest(packet)};
}

/** Only the approved Mira timing/real-emission contract changes from v2. */
export async function captureOrdinaryBasicSourceV3(kit: Kit) {
  const previous = await captureOrdinaryBasicSourceV2(kit);
  const mira = previous.packet.profiles.find(p => p.operatorId === 'op_mira');
  if (!mira || mira.originalBasicIntervalMs !== 1800) return fail('definition_conflict');
  const packet = { ...previous.packet, version: ordinaryBasicV3Version,
    profiles: previous.packet.profiles.map(p => p.operatorId === 'op_mira'
      ? { ...p, timing: { baseCycleMs: 1800, windupMs: 280, recoveryMs: 320 },
        approval: 'BASIC-MIRA-TIMING-v1.20261026' } : p),
    timingPolicy: { ...previous.packet.timingPolicy,
      appliesTo: [...previous.packet.timingPolicy.appliesTo, 'op_mira.basic'] },
    miraEmissionPolicy: { primaryShotsPerOpportunity: 1,
      originalCountSources: ['BasicCombatSimulation.ResolveBasic', 'BMiraFirstProjectileOwner.Launch'],
      secondary: 'PRESERVE_ORIGINAL_EQUIPMENT_AUGMENT_BRANCHES_AND_SUBSHOT_IDS_NOT_BASE_BURST',
      skillSeparation: 'ACTIVE_AND_ULTIMATE_KEEP_THEIR_OWN_SHOT_COUNTS_AND_COMMANDS',
      presentation: 'ACTION_OWNER_EMISSION_ONLY_FLASH_SOUND_AND_TRAIL_ONCE_PER_OPPORTUNITY_SUBSHOT',
      viewAuthority: 'NO_ANIMATION_CALLBACK_DAMAGE_OR_FAKE_BURST',
      restore: 'RESUME_EXISTING_PROJECTILES_AND_PHASE_NO_PAST_FLASH_DAMAGE_OR_CHARGE_REPLAY',
      generalRuntimeVerified: false },
    missing: ['ACTUAL_GEOMETRY_AND_SOCKETS', 'UNITY_ACTION_CHECKPOINT_AND_RESULT_BINDING',
      'GENERAL_MIRA_EMISSION_AND_ORIGINAL_SECONDARY_BRANCH_VERIFICATION'] };
  return { packet, basicHash: await digest(packet) };
}

export async function readOrdinaryBasicFromOriginal(original: Original, kit?: Kit) {
  const snapshot = JSON.parse(original.startSnapshotJson) as { ordinaryBasicSource?: unknown };
  if (snapshot.ordinaryBasicSource === undefined) return fail('source_unavailable');
  try {
    const stored = z.object({ packet: z.object({ version: z.string() }) }).parse(snapshot.ordinaryBasicSource);
    const producer = stored.packet.version === ordinaryBasicVersion ? captureOrdinaryBasicSource
      : stored.packet.version === ordinaryBasicV2Version ? captureOrdinaryBasicSourceV2
        : stored.packet.version === ordinaryBasicV3Version ? captureOrdinaryBasicSourceV3
          : stored.packet.version === ordinaryBasicV4Version ? captureOrdinaryBasicSourceV4
          : stored.packet.version === ordinaryBasicV5Version ? captureOrdinaryBasicSourceV5 : null;
    if (!producer) return fail('source_invalid');
    const expected = await producer(kit ?? await readOrdinaryKitFromOriginal(original));
    if (json(expected) !== json(snapshot.ordinaryBasicSource)) return fail('source_invalid');
    return expected;
  } catch { return fail('source_invalid'); }
}

export async function readOrdinaryRunBasicSource(db: D1Database, accountId: string, runId: string) {
  return readOrdinaryBasicFromOriginal(await readOrdinaryRunStatOrigin(db, accountId, runId));
}
