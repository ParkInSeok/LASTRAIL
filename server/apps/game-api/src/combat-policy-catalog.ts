// Approved 44/34 policy requirements, not an execution capability declaration.
// Historical CE03 fixtures and their hashed execution versions are intentionally separate.
import { ultimateStateVersion } from "./combat-ultimate-state";
import { nyxApprovedDefinitionV2 } from './combat-nyx-approved-source';
const catalog = {
  version: "combat.policy-catalog.2026-09-22.1",
  status: "REQUIRED_NOT_EXECUTABLE",
  references: ["ordinary-story-design-baseline-complete-v01", "six-skills-detail-v02",
    "damage-shield-hazard-detail-v03", "combat-effect-group-map-v04", "combat-closeout-f1-f3-v01", "world-boss-effects-coop-v03"],
  scope: { roster: ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"], ordinaryRisk: "I", expansion: "EXPLICIT_POLICY_REQUIRED" },
  ultimate: { version: ultimateStateVersion, cost: 100, baseCapacity: 100, expandedCapacity: 200,
    expansionSource: "O11", initialCharge: 0, perBasicAction: 4, consumeAt: "CAST_ACCEPTANCE",
    postAcceptanceRefund: false, ready: "CHARGE_AT_LEAST_COST_AND_ACTOR_PREFLIGHT" },
  actionRules: { firstActiveReadyMs: 0, activeAutomation: "ALWAYS", ultimateAutoInitiallyEnabled: false,
    sharedActorQueue: true, autoPriority: "ULTIMATE_BEFORE_ACTIVE", consumeAt: "CAST_ACCEPTANCE",
    casterAttackSnapshotAt: "FIRST_EFFECT", basicChargeFromSkills: false, fixedIntervalsUseAttackSpeed: false,
    preserveCooldownAcrossNodes: true, preserveCommittedProjectiles: true },
  skills: [
    { id: "op_eve.active", effect: "SENTRY_CREATE", cooldownMs: 18000, windupMs: 500, recoveryMs: 300,
      hpFromCasterMaxHpBps: 5000, minimumHp: 1, damageBps: 4000, intervalMs: 800, lifeMs: 12000,
      firstShot: "AT_SPAWN", maximumInstances: 1, target: "OWN_LIVE_CAR_DEFENSE_ZONE", armorBps: 0,
      shield: 0, inheritCasterDefense: false, downBehavior: "PAUSE_FIRE_KEEP_EXPIRY_NO_CATCH_UP", endAt: "HP_ZERO_CAR_DESTROYED_NODE_OR_RUN_END" },
    { id: "op_eve.ultimate", effect: "SENTRY_ENHANCE_OR_CREATE", cost: 100, windupMs: 500, recoveryMs: 300,
      damageBps: 6000, intervalMs: 400, enhancementMs: 8000, fallbackLifeMs: 12000,
      maximumInstances: 1, alreadyEnhanced: "REJECT_WITHOUT_SPEND", existingHpAndLife: "PRESERVE",
      nextScheduledShot: "PRESERVE", enhancementExpiry: "MIN_ENHANCEMENT_AND_DEVICE_EXPIRY" },
    { id: "op_sera.active", effect: "HEAL_ONE", cooldownMs: 12000, windupMs: 400, recoveryMs: 300,
      healBps: 16000, target: "LIVING_INJURED_LOWEST_HP_RATIO_SLOT_ID_INCLUDING_SELF",
      lockTargetAt: "ACCEPTANCE", retarget: false, overheal: "DISCARD" },
    { id: "op_sera.ultimate", effect: "HEAL_AND_RECOVERY", cost: 100, windupMs: 600, recoveryMs: 400,
      healBps: 20000, target: "SNAPSHOT_LIVING_INJURED_AND_ONE_ELIGIBLE_DOWN",
      recoveryReductionMs: 4000, minimumRemainingMs: 1000, recoveryLimit: "ONCE_PER_DOWN_EVENT_ACROSS_CASTERS",
      recoveryTarget: "LONGEST_REMAINING_SLOT_ID", markReduction: "ONLY_WHEN_ACTUALLY_REDUCED", retarget: false },
    { id: "op_mira.active", effect: "FIXED_TARGET_BURST", cooldownMs: 10000, windupMs: 300, recoveryMs: 300,
      damageBps: 4500, shots: 6, intervalMs: 200, firstShot: "WINDUP_END", invalidTarget: "CANCEL_REMAINING_NO_RETARGET",
      source: "DIRECT_SKILL", perShotCastTrigger: false },
    { id: "op_mira.ultimate", effect: "CYCLING_TARGET_BURST", cost: 100, windupMs: 500, recoveryMs: 400,
      damageBps: 4000, shots: 16, intervalMs: 250, firstShot: "WINDUP_END", target: "FOCUS_FIRST_THEN_STABLE_ORDER_PER_CYCLE",
      noTargets: "CANCEL_REMAINING", focusChange: "NEXT_CYCLE", newEnemy: "NEXT_SHOT", source: "DIRECT_SKILL", perShotCastTrigger: false },
  ],
  passives: [
    { operatorId: "op_kyle", source: "SELF_HP_STRICTLY_BELOW_HALF", group: "ARMOR_PERCENTAGE_POINTS", valueBps: 500 },
    { operatorId: "op_reina", source: "DAMAGE_TO_VALID_MARKED_TARGET", group: "NON_EQUIPMENT_OUTGOING", valueBps: 1000 },
    { operatorId: "op_eve", source: "OWN_SENTRY", group: "NON_EQUIPMENT_OUTGOING", valueBps: 1000 },
    { operatorId: "op_sera", source: "OWN_HEAL", group: "HEAL_MAGNITUDE", valueBps: 1000 },
    { operatorId: "op_mira", source: "OWN_BASIC", group: "NON_EQUIPMENT_OUTGOING", valueBps: 1000 },
  ],
  effectRules: { equipmentDirectDamage: "ADDITIVE_WITHIN_GROUP_SEPARATE_MULTIPLIER",
    nonEquipmentOutgoingCapBps: 3000, strongestMarkCapBps: 2000, automaticMagnitudeApplications: 1,
    unknownSourceOrGroup: "PUBLICATION_BLOCKED", ordinaryFloor: "ONCE_AFTER_DEFENSE_BEFORE_SHIELD",
    worldScoreFloor: "ONCE_AFTER_EXACT_VALID_HIT_SUM", worldNumericEncoding: "CONTRACT_PENDING",
    roleOwnership: "EXPLICIT_SOURCE_OWNER_REQUIRED_NO_PARTNER_DUPLICATION" },
};
// New requirements are a separate immutable identity. The legacy object above is retained
// byte-for-byte for pinned departure-v4 snapshots; never reinterpret it using today's rules.
const currentCatalog={
 ...catalog,version:"combat.policy-catalog.2026-09-29.1",
 references:[...catalog.references,"numeric-defense-runtime-binding-audit-v13","def-shield-effect-mapping-v12","skill-haste-curve-approved-v08","attack-speed-curve-cap-approved-v09"],
 skills:catalog.skills.map(skill=>{if(skill.id!=="op_eve.active")return {...skill};const {armorBps,...rest}=skill;void armorBps;return {...rest,defense:0};}),
 passives:catalog.passives.map(passive=>passive.operatorId==="op_kyle"?
  {operatorId:passive.operatorId,source:passive.source,group:"CONDITIONAL_FLAT_DEF_AFTER_STAT_MULTIPLIERS",valueFlat:83}:{...passive}),
 numericDefense:{budgetVersion:"combat.numeric-defense-budget.v1",scope:"ORDINARY_AND_STORY_ONLY",k:1000,
  operatorBase:{op_kyle:250,op_reina:53,op_eve:53,op_sera:87,op_mira:111},operatorLevelGrowth:"FIXED",
  weakening:"STRONGEST",weakeningCapBps:3000,penetration:"ADDITIVE",penetrationCapBps:6000,
  equipmentReductionCapBps:5000,guardStance:"STRONGEST",independentReduction:"MULTIPLICATIVE_ONCE_PER_GROUP",
  unknownDefinition:"PUBLICATION_BLOCKED_NO_DEFAULT_DEF",legacyArmorConversion:"FORBIDDEN"},
 equipmentTiming:{skillHasteByStarBps:[500,1200,3000,7000,15000],attackSpeedByStarBps:[500,1200,3000,7000,15000],
  sameWearerGroup:"ADDITIVE",skillHasteCapBps:null,equipmentAttackSpeedCapBps:null,
  cooldown:"BASE_DIVIDED_BY_ONE_PLUS_HASTE_THEN_ELIGIBLE_REDUCTION_GROUPS",rounding:"CEIL_FINAL_TO_LOGICAL_TICK",
  preserveOtherGroupCaps:true,affectsUltimateCharge:false,affectsCommanderCooldown:false,affectsFixedIntervals:false,affectsWindupOrRecovery:false},
 shieldRules:{directRecipientMaxHpCapBps:null,sameSourceIdentity:"CASTER_ABILITY_EFFECT",sameSource:"REPLACE_IF_NEW_AMOUNT_AT_LEAST_REMAINING_OTHERWISE_NO_CHANGE",
  preserveExplicitExceptions:true,generateBeforeRecipientModifiers:true,offensiveDamageCritPenetrationModifiers:false},
};
export function captureCombatPolicyRequirements() { return structuredClone(currentCatalog); }
export const nyxPolicyVersion = 'combat.policy-catalog.nyx.2026-11-07.1';
export const nyxFormationOrder = ['op_reina', 'op_kyle', 'op_eve', 'op_sera', 'op_nyx'];
export function captureNyxCombatPolicyRequirements() {
 const source = nyxApprovedDefinitionV2(), previous = captureCombatPolicyRequirements();
 return { ...previous, version: nyxPolicyVersion, references: [...previous.references, ...source.references],
  scope: { ...previous.scope, roster: [...nyxFormationOrder] },
  skills: [...previous.skills.filter(s=>!s.id.startsWith('op_mira.')),
   {id:'op_nyx.active',effect:'FIXED_ZONE_CREATE',...source.active},
   {id:'op_nyx.ultimate',effect:'FIXED_ZONE_REPLACE_ON_CREATION',...source.ultimate}],
  passives:[...previous.passives.filter(p=>p.operatorId!=='op_mira'),
   {operatorId:'op_nyx',source:'OWN_ZONE_DAMAGE',group:'NYX_CONSECUTIVE_ZONE_SHARE',valueBps:source.zone.passive.selectedShareBonusBps}],
  numericDefense:{...previous.numericDefense,operatorBase:{...previous.numericDefense.operatorBase,op_nyx:source.base.defense}},
  nyxRequirements: source };
}
/** Explicit requested formation only; historical/default catalog stays byte-identical. */
export function captureCombatPolicyForFormation(operators: readonly {operatorId:string;slot:number}[]) {
 if (!operators.some(o=>o.operatorId==='op_nyx')) return captureCombatPolicyRequirements();
 const sorted=[...operators].sort((a,b)=>a.slot-b.slot);
 if(sorted.length!==5||sorted.some((o,i)=>o.slot!==i||o.operatorId!==nyxFormationOrder[i]))throw new Error('nyx_formation_unsupported');
 return captureNyxCombatPolicyRequirements();
}
/** Lookup only. Not a capability declaration and never a fallback for an unknown version. */
export function readPinnedCombatPolicyRequirements(version:string){
 if(version===catalog.version)return structuredClone(catalog);
 if(version===currentCatalog.version)return captureCombatPolicyRequirements();
 if(version===nyxPolicyVersion)return captureNyxCombatPolicyRequirements();
 return null;
}
