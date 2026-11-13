import { canonicalJson, sha256Hex } from '@lastrail/common';

// Approved requirements only. No ordinary START consumes this until physical/basic/zone contracts and handlers are supplied.
const definition = {
  version: 'combat.nyx-approved-requirements.v1.20261107',
  operatorId: 'op_nyx',
  references: ['44#extended-operators-vex-nyx-iris-v01', '44#def-shield-effect-mapping-v12',
    '44#extended-operators-vex-nyx-iris-v01/2026-11-04-team-armor-weakening'],
  base: { hp: 880, attack: 47, defense: 53, basicIntervalMs: 1800, level: 1, promotion: 0, skillRank: 1 },
  active: { cooldownMs: 16000, windupMs: 500, recoveryMs: 300, durationMs: 6000,
    firstTickAfterCreationMs: 1000, tickIntervalMs: 1000, ticks: 6, maximumTargetsPerTick: 2,
    totalTickAttackBps: 6000, newBasicPeriodIncreaseBps: 1500, armorWeakeningPercentagePoints: 8 },
  ultimate: { cost: 100, windupMs: 600, recoveryMs: 400, durationMs: 8000,
    firstTickAfterCreationMs: 1000, tickIntervalMs: 1000, ticks: 8, maximumTargetsPerTick: 3,
    totalTickAttackBps: 12000, newBasicPeriodIncreaseBps: 2500, armorWeakeningPercentagePoints: 15 },
  zone: { maximumActivePerCaster: 1, followsTarget: false,
    replaceAt: 'NEW_ZONE_CREATION_SUCCESS', selection: 'FOCUS_THEN_STABLE_ID',
    damageBudget: 'DIVIDE_ONE_TOTAL_TICK_BUDGET_BY_CURRENT_SELECTED_COUNT', emptyTick: 'DISCARD',
    delay: 'STRONGEST_ONLY_AT_NEW_BASIC_RESERVATION_NO_EXISTING_SCHEDULE_REWRITE',
    delayImmunity: 'BOSS_OR_CONTROL_IMMUNE',
    endAt: ['CASTER_DOWN', 'NODE_END', 'ZONE_REMOVAL', 'REPLACEMENT', 'ORIGINAL_EXPIRY'],
    naturalExpiryOrder: 'LAST_TICK_THEN_EXPIRY_UNLESS_HIGHER_PRIORITY_END',
    passive: { precedingConsecutiveHpLossTicks: 2, selectedShareBonusBps: 1000,
      resetAt: ['ZERO_HP_LOSS', 'TARGET_LEAVES_ZONE', 'ZONE_REPLACEMENT'],
      apply: 'ONCE_AFTER_TARGET_BUDGET_DIVISION' },
    armorWeakening: { target: 'ALL_LIVING_ELIGIBLE_ZONE_MEMBERS_INDEPENDENT_OF_DAMAGE_TARGET_CAP',
      stack: 'STRONGEST_SOURCE_ONLY', maximumPercentagePoints: 15, minimumEffectiveArmorPercentagePoints: 0,
      immunity: 'EXPLICIT_ARMOR_WEAKENING_IMMUNITY_NOT_CONTROL_IMMUNITY',
      appliesTo: 'ALLIED_AND_OWN_ARMOR_APPLYING_ATTACKS',
      numericDefenseMapping: 'UNRESOLVED_DO_NOT_CONVERT_TO_DEF_PERCENT_REDUCTION' } },
  execution: { status: 'REQUIREMENTS_ONLY', canStart: false,
    missing: ['APPROVED_BASIC_PHYSICAL_AND_TIMING_CONTRACT', 'APPROVED_EXTERIOR_ZONE_CONTRACT',
      'ARMOR_PERCENTAGE_POINT_TO_CURRENT_DEF_CONTRACT', 'NYX_REGISTERED_GEOMETRY', 'SHARED_GAME_HANDLERS'] },
};

/** Independent immutable definition bytes for the next versioned intake; never patches retained five-actor snapshots. */
export async function captureNyxApprovedSource() {
  const packet = structuredClone(definition);
  return { packet, sourceHash: await sha256Hex(canonicalJson(packet)) };
}

/** New approvals never rewrite v1. Physical publication and shared handler verification are separate prerequisites. */
export function nyxApprovedDefinitionV2() {
  const old = structuredClone(definition);
  const { armorWeakeningPercentagePoints: activeOld, ...active } = old.active;
  const { armorWeakeningPercentagePoints: ultimateOld, ...ultimate } = old.ultimate;
  const { armorWeakening: zoneOld, ...zone } = old.zone;
  void activeOld; void ultimateOld; void zoneOld;
  const packet = { ...old, version: 'combat.nyx-approved-requirements.v2.20261107',
    references: ['44#extended-operators-vex-nyx-iris-v01', '44#def-shield-effect-mapping-v12',
      '44#nyx-mitigation-zone-approved-20261107', '44#nyx-basic-trial-approved-20261107'],
    basic: { abilityId: 'op_nyx.basic', attackForm: 'PROJECTILE', modes: ['MELEE', 'RANGED'],
      launchSource: 'ACTUAL_CATALYST_HAND_EMISSION', damage: 'ORIGINAL_SINGLE_BASIC_DAMAGE_AND_GROWTH',
      timing: { baseCycleMs: 1800, windupMs: 300, recoveryMs: 300 },
      range: { physicalAdjacentCars: 2, maximumDistanceMm: 24000 },
      ballistics: { speedMmPerSecond: 48000, radiusMm: 60, maximumFlightDistanceMm: 24000, maximumFlightMs: 500 },
      eligibility: 'INTERSECTION_OF_ORIGINAL_TARGET_RANGE_LOS_AND_WORLD_DISTANCE',
      modesShareOpportunity: true, trajectory: 'FIXED_DIRECTION_NON_HOMING_SWEPT_SPHERE' },
    active: { ...active, mitigationRateReductionBps: 800 }, ultimate: { ...ultimate, mitigationRateReductionBps: 1500 },
    zone: { ...zone, targetScope: 'ONE_SELECTED_ZONE_ALLIED_INTRUDERS_OR_ONE_ENEMY_CARRIAGE',
      sourceKind: 'NYX_ZONE', effectKinds: {
        mitigation: 'MITIGATION_RATE_REDUCTION', newBasicPeriod: 'NEW_BASIC_PERIOD_INCREASE' },
      mitigationRateReduction: { displayName: '피해 감쇄율 감소', maximumBps: 1500,
        target: 'ALL_LIVING_ELIGIBLE_ZONE_MEMBERS_INDEPENDENT_OF_DAMAGE_TARGET_CAP', stack: 'STRONGEST_SOURCE_ONLY',
        order: 'AFTER_CURRENT_DEF_WEAKENING_AND_ATTACK_PENETRATION_BEFORE_SEPARATE_REDUCTIONS_SHIELDS_HP',
        formula: 'MAX_ZERO_D_EFF_OVER_1000_PLUS_D_EFF_MINUS_BPS_OVER_10000',
        changesTargetDefense: false, immunity: 'EXPLICIT_EFFECT_IMMUNITY_NOT_CONTROL_IMMUNITY' } },
    execution: { status: 'REQUIREMENTS_ONLY', canStart: false,
      missing: ['REGISTERED_EXTERIOR_ZONE_BOUNDARY_GENERATION_SELECTION_RANGE_LOS', 'NYX_REGISTERED_GEOMETRY', 'SHARED_GAME_HANDLERS'] } };
  return packet;
}

export async function captureNyxApprovedSourceV2() {
  const packet = nyxApprovedDefinitionV2();
  return { packet, sourceHash: await sha256Hex(canonicalJson(packet)) };
}

export function nyxApprovedDefinitionV3() {
  const previous = nyxApprovedDefinitionV2();
  return { ...previous, version: 'combat.nyx-approved-requirements.v3.20261107',
    references: [...previous.references, '44#nyx-zone-selection-distance-approved-20261107'],
    zone: { ...previous.zone, selectionGeometry: {
      maximumDistanceMm: 48000, origin: 'ACTUAL_NYX_FOOT', target: 'SELECTED_ZONE_REPRESENTATIVE_CENTER',
      metric: 'WORLD_THREE_DIMENSIONAL_EUCLIDEAN_INCLUSIVE', lineOfSight: 'AUTHORITATIVE_AT_ACCEPTANCE',
      acceptance: 'CHECK_DISTANCE_LOS_ELIGIBILITY_AND_OWNER_GENERATION_THEN_LOCK_ONE_ZONE',
      creation: 'RECHECK_LOCKED_ZONE_VALIDITY_AND_ORIGINAL_OWNER_GENERATION_NO_RETARGET',
      failedCreation: 'PRESERVE_EXISTING_ZONE', movementAfterAcceptance: 'NO_DISTANCE_RESELECTION',
    } },
    execution: { ...previous.execution,
      missing: ['REGISTERED_ZONE_CENTER_BOUNDARY_FRAME_GENERATION_AND_LOS', 'NYX_REGISTERED_GEOMETRY', 'SHARED_GAME_HANDLERS'] } };
}
export async function captureNyxApprovedSourceV3() {
  const packet = nyxApprovedDefinitionV3();
  return { packet, sourceHash: await sha256Hex(canonicalJson(packet)) };
}
