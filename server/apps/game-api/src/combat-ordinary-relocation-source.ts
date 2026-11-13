import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { ordinaryBasicV3Version, ordinaryBasicV4Version, ordinaryBasicV5Version, readOrdinaryBasicFromOriginal } from './combat-ordinary-basic-source';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import { reinaBarrageDefinition } from './reina-barrage';
import { nyxApprovedDefinitionV2 } from './combat-nyx-approved-source';
import { ordinaryKitV3Version } from './combat-ordinary-kit-source';
import { captureNyxZoneSelectionSource } from './combat-nyx-zone-selection';

export const ordinaryRelocationVersion = 'combat.ordinary-relocation.v1.20261026';
type Basic = Awaited<ReturnType<typeof readOrdinaryBasicFromOriginal>>;
type Original = Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>;
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (reason: string): never => { throw new ActivityError(409, `ordinary_relocation_${reason}`); };

/** Frozen START policy, not a second movement/action state owner or save ledger. */
export async function captureOrdinaryRelocationSource(basic: Basic) {
  return captureRelocation(basic, false);
}
async function captureRelocation(basic: Basic, nyx: boolean) {
  if (!(nyx ? [ordinaryBasicV4Version, ordinaryBasicV5Version] : [ordinaryBasicV3Version]).includes(basic.packet.version)) return fail('basic_version_unsupported');
  const packet = { version: ordinaryRelocationVersion, approval: 'ROOF-MOVE-BASIC-RESUME-v1.20261026',
    accountId: basic.packet.accountId, runId: basic.packet.runId, originHash: basic.packet.originHash,
    basicVersion: basic.packet.version, basicHash: basic.basicHash, scope: 'ORDINARY_SOLO_FIVE',
    admission: { owner: 'EXISTING_SPATIAL_OWNER_PATH_OCCUPANCY_LIFE_AND_LOCKS',
      input: 'ACCEPTED_RESERVED_RELOCATION_ONLY', failure: 'KEEP_EXISTING_ATTACK', automaticChase: false },
    departure: { idle: 'MOVE_IMMEDIATELY_IF_ORIGINALLY_ELIGIBLE',
      windup: 'KEEP_CURRENT_OPPORTUNITY_RECHECK_COMMIT_THEN_FINISH_RECOVERY',
      commitOrRecovery: 'PRESERVE_COMMIT_ONCE_AND_FINISH_REMAINING_RECOVERY',
      multiShot: 'KEEP_ORIGINAL_REMAINING_SHOTS_THEN_RECOVERY',
      inFlight: 'DO_NOT_WAIT_FOR_IMPACT_OR_REATTACH_PROJECTILES',
      nextOpportunity: 'DO_NOT_START_WHILE_ACCEPTED_RELOCATION_PENDING',
      cancellation: 'EXISTING_CANCEL_AND_RESERVATION_RELEASE_NO_ATTACK_CLOCK_OR_CHARGE_RESET',
      priority: 'EXISTING_DOWN_AND_FORCED_INTERRUPTION_RULES' },
    moving: { newBasicOrActive: false, missedOpportunitiesAccumulate: false, cooldownsContinue: true,
      nextAttack: 'PRESERVE_ORIGINAL_COMBAT_TIME',
      eveBasic: 'FOLLOW_OWNER_WITHOUT_NEW_BASIC', eveActive: 'SEPARATE_ORIGINAL_DRONE_POLICY' },
    arrival: { owner: 'SPATIAL_OWNER_CONFIRMED_LEGAL_ANCHOR_AND_OCCUPANCY_NOT_ANIMATION',
      order: ['UPDATE_CONFIRMED_POSITION_CARRIAGE_EXISTING_SUPPORT_AND_MUZZLE',
        'EXISTING_HIGHER_PRIORITY_ARBITRATION_INCLUDING_PENDING_ULTIMATE',
        'CHECK_ORIGINAL_NEXT_ATTACK_AND_BASIC_ELIGIBILITY', 'ONE_NEW_WINDUP_WHEN_DUE_AND_ELIGIBLE'],
      early: 'WAIT_REMAINING_NEXT_ATTACK', late: 'NO_CATCHUP_OR_IMMEDIATE_COMMIT',
      target: 'RESELECT_FROM_CONFIRMED_POSITION_IF_TARGET_CARRIAGE_OR_GENERATION_CHANGED',
      supportScope: 'EXISTING_ABILITY_CONTRACT_NO_IMPLICIT_SCOPE_MIGRATION' },
    checkpoint: { owner: 'EXISTING_SINGLE_UNITY_CHECKPOINT',
      spatial: ['POSITION', 'PATH_PROGRESS', 'ACCEPTED_DESTINATION', 'RESERVATION'],
      action: ['OPPORTUNITY', 'MODE', 'PHASE', 'REMAINDER', 'NEXT_ATTACK', 'TARGET_GENERATION', 'COMMIT'],
      consistency: 'SAME_CHECKPOINT_BOUNDARY_AND_CROSS_REFERENCES',
      restore: 'ACCEPTED_MOVEMENT_AND_ACTION_CONTINUATION_NOT_UI_OR_UNCONSUMED_INPUT_REPLAY',
      idempotency: 'NO_SECOND_OPPORTUNITY_FROM_REPEATED_RESERVATION_OR_ARRIVAL',
      isolation: 'ORIGINAL_ACCOUNT_RUN_SOURCE_VERSION_AND_HASH',
      contractReference: 'START_RELOCATION_HASH_NO_DUPLICATE_STATIC_PACKET' },
    runtimeSupported: false, canStart: false,
    missing: ['ACTUAL_SPATIAL_RESERVATION_AND_ACTION_CHECKPOINT_BINDING', 'PRODUCT_PLAYER_RESTORE_VERIFICATION'] };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV2Version = 'combat.ordinary-relocation.v2.20261026';
type Kit = Awaited<ReturnType<typeof readOrdinaryKitFromOriginal>>;

/** Explicit audited scope mapping; missing scope is never inferred from an effect name. */
export async function captureOrdinaryRelocationSourceV2(basic: Basic, kit: Kit) {
  return captureScopes(basic, kit, false);
}
async function captureScopes(basic: Basic, kit: Kit, nyx: boolean) {
  if ((kit.packet.version===ordinaryKitV3Version)!==nyx) return fail('version_scope_conflict');
  if (basic.packet.kitHash !== kit.kitHash || basic.packet.accountId !== kit.packet.accountId
    || basic.packet.runId !== kit.packet.runId || basic.packet.originHash !== kit.packet.originHash) return fail('kit_conflict');
  const previous = await captureRelocation(basic, nyx);
  let skillScopes = [
    { abilityId: 'op_kyle.active', basis: 'UNCLASSIFIED', effectRetention: 'CONFIRMED_SHIELD_TARGET_REMAINDER_EXPIRY_PRESERVED_TAUNT_SCOPE_UNRESOLVED', reference: '44#combat-closeout-f1-f3-v01/F2' },
    { abilityId: 'op_kyle.ultimate', basis: 'GLOBAL_LIVING_ROSTER', effectRetention: 'CONFIRMED_TARGET_SHIELD_REMAINDER_AND_EXPIRY', reference: 'combat-shield-budget.approvedKyleShieldAbilities' },
    { abilityId: 'op_reina.active', basis: 'UNCLASSIFIED', effectRetention: 'ORIGINAL_DIRECT_DAMAGE_TARGET_CONTRACT_REQUIRED', reference: 'combat-policy-binding.retainedKits' },
    { abilityId: 'op_reina.ultimate', basis: 'UNCLASSIFIED', effectRetention: 'ORIGINAL_DIRECT_DAMAGE_TARGET_CONTRACT_REQUIRED', reference: 'combat-policy-binding.retainedKits' },
    { abilityId: 'op_eve.active', basis: 'INSTALL_CURRENT_LIVE_CARRIAGE_THEN_FIXED', effectRetention: 'ORIGINAL_DEVICE_CARRIAGE_HP_LIFE_SHOT_SCHEDULE_NO_FOLLOW', reference: '44#six-skills-detail-v02/3' },
    { abilityId: 'op_eve.ultimate', basis: 'EXISTING_DEVICE_OR_INSTALL_CURRENT_LIVE_CARRIAGE', effectRetention: 'EXISTING_DEVICE_CARRIAGE_HP_LIFE_AND_NEXT_SHOT_PRESERVED', reference: '44#six-skills-detail-v02/3' },
    { abilityId: 'op_sera.active', basis: 'FORMATION_ROSTER_ORIGINAL_SLOT_TIEBREAK', effectRetention: 'ACCEPTANCE_TARGET_LOCK_CONFIRMED_HEAL_NOT_REVERSED_OR_REAPPLIED', reference: '44#six-skills-detail-v02/4' },
    { abilityId: 'op_sera.ultimate', basis: 'FORMATION_ROSTER_ORIGINAL_SLOT_TIEBREAK', effectRetention: 'ACCEPTANCE_RECIPIENT_SET_AND_DOWN_EVENT_ONCE_PRESERVED', reference: '44#six-skills-detail-v02/4' },
    { abilityId: 'op_mira.active', basis: 'CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE', effectRetention: 'RECHECK_UNFIRED_SHOTS_KEEP_COMMITTED_PROJECTILES', reference: '44#six-skills-detail-v02/5' },
    { abilityId: 'op_mira.ultimate', basis: 'UNCLASSIFIED', effectRetention: 'ORIGINAL_ELIGIBLE_TARGET_CYCLE_KEEP_COMMITTED_PROJECTILES', reference: '44#six-skills-detail-v02/5' },
  ];
  if(nyx) skillScopes=[...skillScopes.filter(s=>!s.abilityId.startsWith('op_mira.')),
    ...['op_nyx.active','op_nyx.ultimate'].map(abilityId=>({abilityId,basis:'FIXED_SELECTED_LOCAL_ZONE_REQUIRES_REGISTERED_RANGE_LOS',
      effectRetention:'ORIGINAL_ZONE_GENERATION_TICKS_EXPIRY_AND_PASSIVE_NO_FOLLOW_REPLACE_ONLY_ON_CREATION',reference:'44#nyx-mitigation-zone-approved-20261107'}))];
  const abilities = kit.packet.operators.flatMap(o => o.abilities);
  if (abilities.length !== skillScopes.length || skillScopes.some(s => !abilities.some(a => a.id === s.abilityId))) return fail('ability_scope_incomplete');
  const packet = { ...previous.packet, version: ordinaryRelocationV2Version, kitHash: kit.kitHash,
    supportScopeApproval: 'RELOCATION-SUPPORT-SCOPE-v1.20261026',
    arrival: { ...previous.packet.arrival, supportScope: 'EXPLICIT_PER_ABILITY_MAPPING_UNCLASSIFIED_MIGRATION_FORBIDDEN' },
    basicScopes: basic.packet.profiles.map(p => ({ abilityId: p.abilityId, basis: 'CURRENT_CARRIAGE_INSTANCE_AND_PHYSICAL_ADJACENCY',
      physicalAdjacentCars: p.range.physicalAdjacentCars, eligibility: 'ORIGINAL_MODE_TARGET_WORLD_RANGE_AND_LOS_INTERSECTION',
      reference: { basicVersion: basic.packet.version, basicHash: basic.basicHash } })),
    skillScopes,
    passiveScopes: kit.packet.operators.map(o => ({ operatorId: o.operatorId, source: o.passive.source,
      basis: 'ORIGINAL_OWNER_EVENT_FILTER_NOT_AURA', migration: 'NONE' })),
    unclassifiedAbilityIds: skillScopes.filter(s => s.basis === 'UNCLASSIFIED').map(s => s.abilityId),
    crossing: { owner: 'SPATIAL_OWNER_ACTUAL_PASSAGE_BOUNDARY_SAME_GEOMETRY_REVISION',
      membership: 'ONE_SIDE_ONLY_NOT_DESTINATION_OR_VIEW_POSITION',
      immutable: ['FORMATION_SLOT', 'CARD_ORDER', 'EQUIPMENT_OWNER', 'DAMAGE_ATTRIBUTION'],
      eventOrder: 'EXISTING_AUTHORITY_ORDER_SAME_OBSERVATION_REVISION_FOR_ALL_CONSUMERS' },
    ongoingEffects: { committedProjectile: 'KEEP_ATTACK_SNAPSHOT_ATTRIBUTION_PATH_AND_LIFETIME_NO_RELAUNCH',
      confirmedInstantHeal: 'KEEP_APPLIED_HP_NO_REVERSAL_OR_REHEAL',
      attachedShieldOrHot: 'KEEP_TARGET_REMAINDER_EXPIRY_AND_TICK_UNLESS_ORIGINAL_MAINTAINED_RANGE_CONDITION',
      aura: 'ORIGINAL_EVALUATION_TICK_DIFF_CURRENT_RECIPIENTS_NO_DURATION_RESET_OR_IMMEDIATE_HEAL_DUPLICATION',
      installed: 'KEEP_ORIGINAL_CARRIAGE_FOLLOW_ONLY_WHEN_ORIGINAL_ABILITY_EXPLICITLY_REQUIRES',
      parameters: 'NO_NEW_COMMON_TICK_MAGNITUDE_CAP_OR_FOLLOW_DISTANCE',
      application: 'ONLY_EXISTING_EXPLICIT_EFFECT_TYPES_AND_ORIGINAL_EXCEPTION_RULES' },
    scopeCheckpoint: { boundary: 'SAME_POSITION_PATH_REVISION_CARRIAGE_EFFECT_TARGET_REMAINDER_EXPIRY_NEXT_TICK_EVENT_SEQUENCE',
      restore: 'NO_NEW_ACQUISITION_RETRIGGER_NO_MISSED_TICK_CATCHUP_KEEP_ORIGINAL_OFFLINE_POLICY',
      reconcile: 'CURRENT_SPATIAL_AURA_RECIPIENTS_WITHOUT_DUPLICATE_APPLICATION',
      display: 'EXISTING_OWNER_NOTIFICATIONS_NO_CARD_REORDER_EQUIPMENT_REFETCH_OR_NEW_GET' },
    missing: [...previous.packet.missing, 'UNCLASSIFIED_ABILITY_SCOPE', 'ACTUAL_CROSSING_AND_EFFECT_CONSUMER_BINDING'] };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV3Version = 'combat.ordinary-relocation.v3.20261026';

/** Approved selection scope only; effect execution remains with the existing Unity owners. */
export async function captureOrdinaryRelocationSourceV3(basic: Basic, kit: Kit) {
  return captureScopesV3(basic, kit, false);
}
async function captureScopesV3(basic: Basic, kit: Kit, nyx: boolean) {
  const previous = await captureScopes(basic, kit, nyx);
  const reference = '44#four-unclassified-skill-scope-approved-20261026';
  const skillScopes = previous.packet.skillScopes.map(scope => {
    if (scope.abilityId === 'op_kyle.active') return { ...scope, reference,
      basis: 'COMPOSITE_SELF_SHIELD_AND_CURRENT_CARRIAGE_FRONT_THREATS',
      shield: { basis: 'SELF', parameters: 'ORIGINAL_MAX_HP_18_PERCENT_6000_MS',
        independentOfSurvivingTauntTargets: true },
      taunt: { basis: 'CURRENT_CARRIAGE_FRONT_THREATS', origin: 'ACTUAL_CASTER_BODY_ANCHOR',
        destination: 'ACTUAL_ENEMY_HIT_ANCHOR', maximumDistanceMm: 12000, maximumTargets: 2,
        eligibility: 'IN_CURRENT_CARRIAGE_OR_ATTACKING_IT_OR_ITS_ROOF_OPERATOR_ORIGINAL_LOS_AND_TAUNT_IMMUNITY',
        directions: 'FRONT_AND_REAR_ELIGIBLE_THREATS_EXCLUDE_ARTILLERY',
        order: ['DISTANCE_ASCENDING', 'ORIGINAL_COMBAT_ORDER', 'STABLE_ID'], minimumAcceptanceTargets: 1,
        noCandidates: 'NO_ACCEPTANCE_OR_CONSUMPTION', lock: 'ACCEPTANCE_TARGET_SET_THROUGH_ORIGINAL_350_MS_WINDUP',
        commit: 'CASTER_LIFE_ACTION_ELIGIBILITY_AND_EACH_TARGET_GENERATION_DISTANCE_LOS_IMMUNITY',
        replacementAtCommit: false, allTargetsLost: 'SELF_SHIELD_IF_CASTER_CAN_COMMIT_NO_REFUND',
        duration: 'ORIGINAL_4000_MS_IMMUNITY_AND_FORCED_REMOVAL_EXCEPTIONS' },
      effectRetention: 'ATTACHED_SHIELD_AND_TAUNT_KEEP_TARGET_REMAINDER_EXPIRY_NO_MOVEMENT_RESELECT_OR_RESET',
      checkpointFields: ['LOCKED_TAUNT_TARGET_GENERATIONS', 'SHIELD_REMAINDER_AND_EXPIRY', 'COMMIT_STATE'] };
    if (scope.abilityId === 'op_reina.active' || scope.abilityId === 'op_reina.ultimate') return { ...scope, reference,
      basis: 'CURRENT_CARRIAGE_PHYSICAL_ADJACENCY_PRIMARY_TARGET',
      primaryTarget: { origin: 'ACTUAL_PISTOL_MUZZLE_AT_COMMIT', destination: 'ACTUAL_TARGET_HIT_ANCHOR',
        maximumDistanceMm: 24000, physicalAdjacentCars: 1, eligibility: 'INTERSECT_ORIGINAL_LOS_AND_OCCLUSION',
        acceptance: 'LOCK_EXISTING_FOCUS_TARGET_OWNER_ELIGIBLE_TARGET',
        commit: 'RECHECK_SAME_TARGET_GENERATION_DISTANCE_LOS', replacementAtCommit: false,
        lostTarget: 'ORIGINAL_MISFIRE_NO_POST_ACCEPTANCE_REFUND' },
      damageRecipients: 'CURRENT_ORDINARY_DIRECT_DAMAGE_CONTRACT_UNSUPPLIED',
      inheritBasicBallistics: false, parameters: 'ORIGINAL_KIT_DAMAGE_COOLDOWN_COST_WINDUP_RECOVERY',
      effectRetention: 'ORIGINAL_CONFIRMED_DAMAGE_AND_MARK_LIFETIME_NO_MOVEMENT_REAPPLICATION_OR_CANCELLATION',
      checkpointFields: ['ORIGINAL_LOCKED_TARGET_GENERATION', 'COMMIT_STATE'] };
    if (scope.abilityId === 'op_mira.ultimate') return { ...scope, reference,
      basis: 'CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE',
      basicReference: { abilityId: 'op_mira.basic', version: basic.packet.version, basicHash: basic.basicHash },
      targeting: { origin: 'EACH_SHOT_ACTUAL_CURRENT_MUZZLE', destination: 'ACTUAL_TARGET_HIT_ANCHOR',
        maximumDistanceMm: 24000, eligibility: 'INTERSECT_CURRENT_CARRIAGE_ORIGINAL_BASIC_SUPPORT_AND_LOS_OCCLUSION',
        evaluation: 'EACH_SHOT_CURRENT_CANDIDATES', minimumAcceptanceTargets: 1, maximumShots: 16,
        cycle: 'ORIGINAL_FOCUS_FIRST_UNFIRED_FIRST_RESET_AFTER_ALL_FIRED',
        newEnemy: 'ELIGIBLE_NEXT_SHOT', focusChange: 'NEXT_CYCLE', outOfRange: 'EXCLUDE_FOR_THIS_SHOT_CONTINUE_OTHER_ELIGIBLE',
        emptyCandidates: 'CANCEL_REMAINING_SHOTS_THEN_RECOVERY', chaseOrPauseUntilReturn: false },
      parameters: 'ORIGINAL_ATK_SNAPSHOT_40_PERCENT_500_MS_WINDUP_250_MS_INTERVAL_400_MS_FINAL_RECOVERY',
      inheritBasicBallistics: false, inheritBasicDamageChargeOrAttackSpeedFilters: false,
      effectRetention: 'KEEP_COMMITTED_PROJECTILES_CONSUME_ONCE_NO_REPLAY_OF_PAST_SHOTS',
      checkpointFields: ['SHOT_INDEX', 'CYCLE_UNFIRED_TARGET_SET', 'NEXT_SHOT_TIME', 'COMMITTED_PROJECTILES'] };
    return scope;
  });
  const packet = { ...previous.packet, version: ordinaryRelocationV3Version,
    supportScopeApproval: 'FOUR-SKILL-SCOPE-v1.20261026', skillScopes,
    unclassifiedAbilityIds: skillScopes.filter(s => s.basis === 'UNCLASSIFIED').map(s => s.abilityId),
    missing: [...previous.packet.missing.filter(reason => reason !== 'UNCLASSIFIED_ABILITY_SCOPE'),
      'REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT', 'ACTUAL_SKILL_GEOMETRY_EFFECT_AND_CHECKPOINT_BINDING'] };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV4Version = 'combat.ordinary-relocation.v4.20261026';

/** Recipient definitions extend the pinned kit; they do not execute damage on the server. */
export async function captureOrdinaryRelocationSourceV4(basic: Basic, kit: Kit) {
  return captureRecipients(basic, kit, false);
}
async function captureRecipients(basic: Basic, kit: Kit, barrage: boolean, nyx = false) {
  const previous = await captureScopesV3(basic, kit, nyx);
  const skillScopes = previous.packet.skillScopes.map(scope => {
    if (scope.abilityId !== 'op_reina.active' && scope.abilityId !== 'op_reina.ultimate') return scope;
    const ultimate = scope.abilityId === 'op_reina.ultimate';
    const ability = kit.packet.operators.flatMap(o => o.abilities).find(a => a.id === scope.abilityId);
    if (ultimate && barrage) {
      if (!('primaryTarget' in scope) || json(ability) !== json(reinaBarrageDefinition())) return fail('damage_definition_conflict');
      return { ...scope, parameters: 'COMMIT_LOCKED_FOUR_IMPACTS', barrage: reinaBarrageDefinition(),
        reference: '44#reina-barrage-damage-detail-approved-20261102',
        primaryTarget: { ...scope.primaryTarget!, lostTarget: 'CANCEL_BEFORE_COMMIT_NO_CHARGE_CONSUMPTION' },
        damageRecipients: 'COMMIT_FIXED_SET_PER_IMPACT_ELIGIBILITY_AND_COEFFICIENTS_IN_BARRAGE',
        effectRetention: 'KEEP_COMMITTED_AFTER_CASTER_DOWN_CANCEL_ON_ORIGINAL_FRAME_LOSS',
        checkpointFields: ['ORIGINAL_CAST_ATTEMPT_FRAME_TARGET_GENERATIONS_ATTACK_AND_IMPACT_PROGRESS'],
        restore: 'ORIGINAL_NODE_START_NO_MID_BARRAGE_RESTORE' };
    }
    if (!ability || ability.effect !== 'DIRECT_DAMAGE' || ability.damageBps !== (ultimate ? 100000 : 40000)) return fail('damage_definition_conflict');
    return { ...scope, damageRecipients: {
      approval: 'REINA-SKILL-DAMAGE-SHAPE-v1.20261026',
      reference: '44#reina-skill-damage-shape-approved-20261026',
      effect: ability.effect, primaryDamageBps: ability.damageBps, maximumTargets: ultimate ? 4 : 1,
      primaryScopeReference: 'THIS_SKILL_PRIMARY_TARGET',
      additional: ultimate ? { damageBps: 30000, maximumTargets: 3, radiusMm: 3000,
        center: 'PRIMARY_TARGET_HIT_ANCHOR_AT_COMMIT', dimension: 'THREE_DIMENSIONAL_HIT_ANCHOR',
        distanceComparison: 'LESS_THAN_OR_EQUAL', maximumMuzzleDistanceMm: 24000, physicalAdjacentCars: 1,
        eligibility: 'OTHER_LIVING_ORIGINALLY_ELIGIBLE_ENEMIES_EXCLUDE_ARTILLERY_AND_INELIGIBLE_STRUCTURES',
        muzzleLosRequired: true, centerSolidOcclusionCheckRequired: true, expandByCollider: false,
        order: ['CENTER_DISTANCE_ASCENDING', 'ORIGINAL_COMBAT_ORDER', 'STABLE_ID'] } : null,
      redistributeUnusedDamage: false,
      selection: { revision: 'SAME_OBSERVATION_REVISION_BEFORE_ANY_DAMAGE',
        primary: 'ORIGINAL_ACCEPTANCE_LOCKED_IDENTITY_GENERATION_RECHECK_LIFE_SCOPE_LOS_AT_COMMIT',
        primaryLost: 'WHOLE_CAST_MISFIRE_NO_REPLACEMENT_OR_REFUND',
        primaryDiesAfterSelection: 'KEEP_SELECTED_ADDITIONAL_TARGETS', immunePrimaryMayAnchor: true,
        replaceImmuneTargets: false, uniqueEntities: true, order: 'PRIMARY_FIXED_FIRST_THEN_SELECTED_ADDITIONAL_ORIGINAL_AUTHORITY_ORDER' },
      execution: { timing: 'INSTANT_COMMIT_DIRECT_DAMAGE', attackSnapshot: 'ONCE_ORIGINAL_EFFECTIVE_ATK',
        perTarget: 'ORIGINAL_IMMUNITY_DEFENSE_SHIELD_CRITICAL_DAMAGE_AND_DEATH_PIPELINE',
        passiveMark: 'EACH_TARGET_OWN_ORIGINAL_EFFECTIVE_DAMAGE_CONDITION',
        newStatusKnockbackOrMark: false, visualCallbackDealsDamage: false,
        timingAndConsumption: 'ORIGINAL_KIT_COOLDOWN_WINDUP_RECOVERY_CHARGE_AND_ACCEPTANCE_CONSUMPTION' },
      modifiers: { primary: 'ORIGINAL_SKILL_DAMAGE_FILTERS_AND_TRAINING',
        additionalRankScaling: 'SAME_ORIGINAL_ULTIMATE_RANK_MULTIPLIER',
        rankChangesRadiusOrCap: false, inheritBasicEn17OrPiercing: false,
        eligibility: 'ONLY_ORIGINALLY_APPROVED_SKILL_MODIFIERS' },
      checkpoint: { owner: 'EXISTING_EFFECT_OWNER', identity: 'ORIGINAL_CAST_ID_AND_TARGET_GENERATION_SUBEVENT',
        fields: ['LOCKED_PRIMARY_GENERATION', 'COMMIT_REVISION_AND_SELECTED_TARGET_GENERATIONS',
          'ORIGINAL_ATTACK_SNAPSHOT', 'PER_TARGET_PROCESSING_PROGRESS'],
        restore: 'RESUME_PROGRESS_NO_RESELECT_OR_REPEAT_DAMAGE',
        resultContribution: 'ORIGINAL_SUBEVENT_ONCE_NO_PRIMARY_AND_SPREAD_DOUBLE_COUNT' },
    } };
  });
  const packet = { ...previous.packet, version: ordinaryRelocationV4Version, skillScopes,
    missing: previous.packet.missing.filter(reason => reason !== 'REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT') };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV5Version = 'combat.ordinary-relocation.v5.20261026';

/** Fixed-target ultimate arrival is separate from Mira's physical BASIC projectile. */
export async function captureOrdinaryRelocationSourceV5(basic: Basic, kit: Kit) {
  return captureMiraFlight(basic, kit, false);
}
async function captureMiraFlight(basic: Basic, kit: Kit, barrage: boolean) {
  const previous = await captureRecipients(basic, kit, barrage);
  const ability = kit.packet.operators.flatMap(o => o.abilities).find(a => a.id === 'op_mira.ultimate');
  if (!ability || ability.effect !== 'CYCLING_TARGET_BURST' || ability.damageBps !== 4000 || ability.shots !== 16
    || ability.intervalMs !== 250 || ability.windupMs !== 500 || ability.recoveryMs !== 400) return fail('flight_definition_conflict');
  const skillScopes = previous.packet.skillScopes.map(scope => scope.abilityId !== ability.id ? scope : {
    ...scope, parameters: 'ORIGINAL_KIT_TIMING_AND_DAMAGE_FIRST_ACTUAL_SHOT_ATK_SNAPSHOT',
    projectile: { approval: 'MIRA-ULTIMATE-FLIGHT-v1.20261026', reference: '44#mira-ultimate-flight-approved-20261026',
      mode: 'FIXED_TARGET_DELAYED_ARRIVAL', flightMs: 120,
      stepRounding: 'CEILING_TO_ORIGINAL_COMBAT_STEP_MINIMUM_ONE_STEP',
      deadline: 'ACTUAL_SHOT_TICK_PLUS_ROUNDED_FLIGHT', clock: 'ORIGINAL_COMBAT_TIME_PAUSE_AND_SPEED',
      parameters: { damageBps: ability.damageBps, shots: ability.shots, intervalMs: ability.intervalMs,
        windupMs: ability.windupMs, recoveryMs: ability.recoveryMs },
      launch: 'THIS_SKILL_CURRENT_TARGETING_SCOPE_AND_ORIGINAL_CYCLE_RECHECK_EACH_UNFIRED_SHOT',
      physicalCollisionAfterLaunch: false, speedMmPerSecond: null, collisionRadiusMm: null,
      interceptionPiercingOrRicochet: false, visualWidthAffectsDamage: false,
      arrival: { target: 'SAME_LIVING_TARGET_GENERATION_IN_SAME_BATTLE', rangeExit: 'KEEP_COMMITTED_SHOT',
        newOcclusion: 'NO_POST_LAUNCH_PHYSICAL_RECHECK', lostTarget: 'MISS_NO_RETARGET',
        casterDown: 'CANCEL_UNFIRED_KEEP_COMMITTED', battleEnded: 'NO_DAMAGE',
        pipeline: 'AT_ARRIVAL_ORIGINAL_IMMUNITY_DEFENSE_SHIELD_HIT_DAMAGE_AND_LIFESTEAL_FILTERS',
        targetsPerShot: 1, expiry: 'ROUNDED_IMPACT_DEADLINE_NO_BASIC_500_MS_LIFETIME',
        processing: 'LOGICAL_DUE_EVENT_ONCE_NOT_RENDER_FRAMES' },
      snapshot: { attack: 'FIRST_ACTUAL_SHOT_ONCE_SHARED_BY_ALL_16', modifiers: 'ORIGINAL_PER_SHOT_EVALUATION_BOUNDARIES',
        attackSpeedChangesIntervalOrFlight: false, inheritBasicChargeOrEn17: false },
      actionLifetime: 'ORIGINAL_FINAL_SHOT_RECOVERY_SEPARATE_FROM_COMMITTED_SHOTS_NO_INDEFINITE_BASIC_LOCK',
      checkpoint: { actionOwner: 'EXISTING_ACTION_OWNER_RESERVATION_SEQUENCE_AND_CYCLE',
        shotOwner: 'EXISTING_PROJECTILE_EFFECT_OWNER_IMMUTABLE_SHOT_AND_IMPACT_PROGRESS',
        identity: 'RUN_CAST_ID_SHOT_INDEX_AND_TARGET_GENERATION',
        fields: ['REMAINING_IMPACT_TIME', 'SHOT_SEQUENCE', 'ORIGINAL_ATTACK_SNAPSHOT', 'CURRENT_CYCLE', 'IMPACT_PROCESSING_STATE'],
        restore: 'KEEP_REMAINING_DEADLINE_SEQUENCE_SNAPSHOT_AND_CYCLE_NO_RESTART_OR_REPLAY',
        unknownResult: 'RECOVER_ORIGINAL_EVENT_NO_AUTO_RECAST_OR_REFUND',
        visuals: 'ONLY_CURRENT_INFLIGHT_SHOTS_NO_PAST_FLASH_REPLAY' },
    },
  });
  const packet = { ...previous.packet, version: ordinaryRelocationV5Version, skillScopes };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV6Version = 'combat.ordinary-relocation.v6.20261026';

/** Both device modes share this projectile; Eve's BASIC carrier remains independent. */
export async function captureOrdinaryRelocationSourceV6(basic: Basic, kit: Kit) {
  return captureDeviceFlight(basic, kit, false);
}
async function captureDeviceFlight(basic: Basic, kit: Kit, barrage: boolean, nyx = false) {
  const previous = nyx ? await captureRecipients(basic, kit, barrage, true) : await captureMiraFlight(basic, kit, barrage);
  const abilities = kit.packet.operators.flatMap(o => o.abilities);
  const active = abilities.find(a => a.id === 'op_eve.active'), ultimate = abilities.find(a => a.id === 'op_eve.ultimate');
  if (!active || !ultimate || active.effect !== 'SENTRY_CREATE' || ultimate.effect !== 'SENTRY_ENHANCE_OR_CREATE'
    || active.damageBps !== 4000 || active.intervalMs !== 800 || active.lifeMs !== 12000 || active.maximumInstances !== 1
    || active.hpFromCasterMaxHpBps !== 5000 || ultimate.damageBps !== 6000 || ultimate.intervalMs !== 400
    || ultimate.enhancementMs !== 8000 || ultimate.nextScheduledShot !== 'PRESERVE') return fail('device_definition_conflict');
  const skillScopes = previous.packet.skillScopes.map(scope => {
    if (scope.abilityId === ultimate.id) return { ...scope, projectileReference: 'op_eve.active.projectile' };
    if (scope.abilityId !== active.id) return scope;
    return { ...scope, projectile: { approval: 'EVE-ACTIVE-DRONE-FLIGHT-v1.20261026',
      reference: '44#eve-active-drone-flight-approved-20261026', mode: 'SINGLE_UNGUIDED_PHYSICAL_PROJECTILE',
      origin: 'ACTUAL_DEVICE_MUZZLE', destination: 'ACTUAL_ENEMY_HIT_ANCHOR_AT_LAUNCH',
      speedMmPerSecond: 48000, radiusMm: 50, maximumFlightDistanceMm: 18000, maximumFlightMs: 375,
      lifetimeRounding: 'CEILING_TO_ORIGINAL_COMBAT_STEP_WITHOUT_EXTENDING_DISTANCE_CAP',
      eligibility: 'ORIGINAL_DEVICE_CARRIAGE_DEFENSE_ZONE_TARGET_TYPE_LOS_AND_18000_MM_INTERSECTION',
      excludeArtillery: true, enhancementChangesBallisticsOrShotCount: false,
      collision: { sweep: 'PREVIOUS_TO_CURRENT_SWEPT_SPHERE', firstSolidOcclusion: 'TERMINATE',
        firstEligibleEnemy: 'ONE_DAMAGE_THEN_TERMINATE', friendlyBlocking: false,
        interceptionEligibility: 'ORIGINAL_TARGET_TYPE_DEVICE_DEFENSE_ZONE_AND_LAUNCH_AREA_18000_MM_BOUNDARY',
        retargetOnOriginalDeath: false, afterOriginalDeath: 'CONTINUE_STRAIGHT_CAN_HIT_OTHER_ELIGIBLE_ENEMY',
        ties: 'EXISTING_AUTHORITY_COLLISION_ORDER', homingPiercingExplosionOrRicochet: false },
      schedule: { firstAttempt: 'AT_ORIGINAL_DEVICE_CREATION_EFFECT_COMMIT',
        normal: { damageBps: active.damageBps, intervalMs: active.intervalMs },
        enhanced: { damageBps: ultimate.damageBps, intervalMs: ultimate.intervalMs, enhancementMs: ultimate.enhancementMs },
        selection: 'LEGAL_FOCUS_THEN_CURRENT_OR_ANNOUNCED_CARRIAGE_ATTACKERS_THEN_ORIGINAL_ORDER_AND_ID',
        ineligibleMuzzleAlignmentLosOrTarget: 'CONSUME_OPPORTUNITY_NO_CATCHUP', shotsPerOpportunity: 1,
        addBasicWindupRecoveryOrConsumeBasicOpportunity: false, basicCharge: false,
        preserveNextShotOnEnhance: true, mode: 'MODE_AT_THIS_SHOT_THEN_ORIGINAL_NEXT_INTERVAL_RULE',
        intervalBoundary: 'CREATION_INCLUSIVE_EXPIRY_EXCLUSIVE', exactExpiryTickFires: false },
      snapshot: { attack: 'ORIGINAL_DEVICE_CREATION_ATK', coefficient: 'PIN_NORMAL_OR_ENHANCED_AT_LAUNCH',
        enhancementAfterLaunch: 'NO_RESCALING_OF_COMMITTED_SHOTS',
        impact: 'ORIGINAL_AT_ARRIVAL_DEFENSE_SHIELD_MODIFIER_AND_EFFECT_FILTERS' },
      device: { maximumInstances: active.maximumInstances, lifeMs: active.lifeMs,
        hpFromCasterMaxHpBps: active.hpFromCasterMaxHpBps, minimumHp: active.minimumHp,
        enhancementHpAndLife: ultimate.existingHpAndLife, movement: 'PRESERVE_ORIGINAL_DEVICE_CARRIAGE_AND_EXPLICIT_SKILL_EXCEPTIONS',
        destroyed: 'NO_BASIC_CARRIER_SUBSTITUTION_OR_HP_LIFE_RESTORE' },
      lifetime: { casterDown: 'PAUSE_NEW_SHOTS_KEEP_COMMITTED', deviceDestroyedOrExpired: 'KEEP_COMMITTED_UNTIL_ORIGINAL_SHOT_EXPIRY',
        battleEnd: 'EXISTING_COMMON_TERMINATION_POLICY',
        ownership: 'SHOT_OWNS_ONLY_IMMUTABLE_ATTRIBUTION_NOT_RELEASED_DEVICE_OR_RESPONSE_OBJECT' },
      attribution: 'EVE_OPERATOR_DEVICE_INSTANCE_GENERATION_ORIGINAL_SKILL_AND_SHOT_ID',
      checkpoint: { deviceOwner: 'EXISTING_DEVICE_OWNER',
        deviceFields: ['INSTANCE_GENERATION', 'CARRIAGE', 'HP', 'EXPIRY', 'ENHANCEMENT_END', 'NEXT_SHOT', 'ORIGINAL_ATK', 'SEQUENCE'],
        shotOwner: 'EXISTING_PROJECTILE_OWNER',
        shotFields: ['LAUNCH_POSITION_DIRECTION', 'REMAINING_DISTANCE_AND_TIME', 'COEFFICIENT', 'ATTRIBUTION', 'PROCESSING_STATE'],
        restore: 'NO_NEW_DEVICE_SPAWN_SHOT_ENHANCEMENT_REFRESH_OR_DUPLICATE_HIT' },
    } };
  });
  const packet = { ...previous.packet, version: ordinaryRelocationV6Version, skillScopes };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export const ordinaryRelocationV7Version = 'combat.ordinary-relocation.v7.20261102';
export const ordinaryRelocationV8Version = 'combat.ordinary-relocation.v8.20261107';
export const ordinaryRelocationV9Version = 'combat.ordinary-relocation.v9.20261107';
export const ordinaryRelocationV10Version = 'combat.ordinary-relocation.v10.20261107';
export async function captureOrdinaryRelocationSourceV10(basic: Basic, kit: Kit) {
  const previous = await captureOrdinaryRelocationSourceV9(basic, kit);
  const zoneSelectionSource = await captureNyxZoneSelectionSource();
  const packet = { ...previous.packet, version: ordinaryRelocationV10Version, zoneSelectionSource,
    missing: previous.packet.missing.map(reason => reason === 'REGISTERED_EXTERIOR_ZONE_BOUNDARY_GENERATION_SELECTION_RANGE_LOS'
      ? 'REGISTERED_ZONE_CENTER_BOUNDARY_FRAME_GENERATION_AND_LOS' : reason) };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}
export async function captureOrdinaryRelocationSourceV9(basic: Basic, kit: Kit) {
  if (basic.packet.version !== ordinaryBasicV5Version) return fail('basic_version_unsupported');
  const previous = await captureDeviceFlight(basic, kit, true, true);
  const packet = { ...previous.packet, version: ordinaryRelocationV9Version,
    supportScopeApproval: 'NYX-MITIGATION-ZONE-v1.20261107', nyxZone: nyxApprovedDefinitionV2().zone,
    reentry: 'EXISTING_CURRENT_NODE_RESTART_FROM_ORIGINAL_NODE_START',
    midCombatPersistence: false,
    missing: ['ACTUAL_SPATIAL_RESERVATION_AND_CURRENT_ATTEMPT_ACTION_BINDING',
      'ACTUAL_CROSSING_AND_EFFECT_CONSUMER_BINDING', 'ACTUAL_SKILL_GEOMETRY_AND_EFFECT_BINDING',
      'REGISTERED_EXTERIOR_ZONE_BOUNDARY_GENERATION_SELECTION_RANGE_LOS'] };
  // Retain live ownership/idempotency fields; none of this packet authorizes reconnect continuation.
  const restrictToCurrentAttempt = (value: unknown): void => {
    if (!value || typeof value !== 'object') return;
    if (Array.isArray(value)) { value.forEach(restrictToCurrentAttempt); return; }
    const record = value as Record<string, unknown>;
    if (Object.hasOwn(record, 'restore')) {
      record.restore = 'RESTART_FROM_ORIGINAL_NODE_START_NO_MID_COMBAT_RESTORE';
      record.persistence = 'NONE_CURRENT_ATTEMPT_MEMORY_ONLY';
    }
    Object.values(record).forEach(restrictToCurrentAttempt);
  };
  restrictToCurrentAttempt(packet);
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}
export async function captureOrdinaryRelocationSourceV8(basic: Basic, kit: Kit) {
  if (basic.packet.version !== ordinaryBasicV4Version) return fail('basic_version_unsupported');
  const previous = await captureDeviceFlight(basic, kit, true, true);
  const packet={...previous.packet,version:ordinaryRelocationV8Version,
    supportScopeApproval:'NYX-MITIGATION-ZONE-v1.20261107',nyxZone:nyxApprovedDefinitionV2().zone,
    missing:[...previous.packet.missing,'REGISTERED_EXTERIOR_ZONE_BOUNDARY_GENERATION_SELECTION_RANGE_LOS']};
  return {packet,relocationHash:await sha256Hex(json(packet))};
}
export async function captureOrdinaryRelocationSourceV7(basic: Basic, kit: Kit) {
  const previous = await captureDeviceFlight(basic, kit, true);
  const packet = { ...previous.packet, version: ordinaryRelocationV7Version };
  return { packet, relocationHash: await sha256Hex(json(packet)) };
}

export async function readOrdinaryRelocationFromOriginal(original: Original, basic?: Basic, kit?: Kit) {
  const snapshot = JSON.parse(original.startSnapshotJson) as { ordinaryRelocationSource?: unknown };
  if (snapshot.ordinaryRelocationSource === undefined) return fail('source_unavailable');
  try {
    const stored = z.object({ packet: z.object({ version: z.string() }) }).parse(snapshot.ordinaryRelocationSource);
    if (![ordinaryRelocationVersion, ordinaryRelocationV2Version, ordinaryRelocationV3Version, ordinaryRelocationV4Version, ordinaryRelocationV5Version, ordinaryRelocationV6Version, ordinaryRelocationV7Version, ordinaryRelocationV8Version, ordinaryRelocationV9Version, ordinaryRelocationV10Version].includes(stored.packet.version)) return fail('source_invalid');
    const sourceKit = kit ?? await readOrdinaryKitFromOriginal(original);
    const sourceBasic = basic ?? await readOrdinaryBasicFromOriginal(original, sourceKit);
    const expected = stored.packet.version === ordinaryRelocationVersion ? await captureOrdinaryRelocationSource(sourceBasic)
      : stored.packet.version === ordinaryRelocationV2Version ? await captureOrdinaryRelocationSourceV2(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV3Version ? await captureOrdinaryRelocationSourceV3(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV4Version ? await captureOrdinaryRelocationSourceV4(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV5Version ? await captureOrdinaryRelocationSourceV5(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV6Version ? await captureOrdinaryRelocationSourceV6(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV7Version ? await captureOrdinaryRelocationSourceV7(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV8Version ? await captureOrdinaryRelocationSourceV8(sourceBasic, sourceKit)
      : stored.packet.version === ordinaryRelocationV9Version ? await captureOrdinaryRelocationSourceV9(sourceBasic, sourceKit)
      : await captureOrdinaryRelocationSourceV10(sourceBasic, sourceKit);
    if (json(expected) !== json(snapshot.ordinaryRelocationSource)) return fail('source_invalid');
    return expected;
  } catch { return fail('source_invalid'); }
}

export async function readOrdinaryRunRelocationSource(db: D1Database, accountId: string, runId: string) {
  return readOrdinaryRelocationFromOriginal(await readOrdinaryRunStatOrigin(db, accountId, runId));
}
