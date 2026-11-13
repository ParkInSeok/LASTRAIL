import { canonicalJson, sha256Hex } from '@lastrail/common';
import { bScoutGunnerDefinition } from './combat-b-scout-gunner-source';
import { operationRiskDefinition } from './combat-b-operation-risk';
import { ExactCombatValue } from './combat-exact-value';
import { exactRailArtilleryHazardVersion } from './rail-artillery-hazard';
import { firstEncounterObjectiveDefinition } from './combat-first-encounter-objective';
import { firstEnemyTrainSpatialDefinition } from './combat-first-enemy-spatial';

export const firstPlayableProfileVersion = 'combat.first-playable-profile.v1.20261027';

/** Frozen learning override; never passes two gunners through the mixed B wave allocator. */
export async function firstPlayableProfile() {
  const catalog = await bScoutGunnerDefinition();
  const gunner = catalog.types.find(type => type.definitionId === 'enemy_spike_cart_gunner');
  if (!gunner) throw new Error('first_playable_gunner_definition_missing');
  const scaling = await operationRiskDefinition();
  const base = (value: number) => ExactCombatValue.from({ numerator: String(value), denominator: '1' });
  const definition = {
    version: firstPlayableProfileVersion,
    profileId: 'first_battle_forward_gunners_single_artillery',
    approval: 'FIRST-PLAYABLE-NUMERIC-BASELINE-v1.20261027',
    scope: 'FIRST_TWO_ENCOUNTERS_ONLY',
    enemies: {
      definition: gunner, count: 2, baseHpEach: 240, baseHpTotal: 480, defense: 53,
      hpOverride: 'LEARNING_PROFILE_ONLY', spawn: 'ONCE_AT_SHARED_FIRST_ENCOUNTER_ENTRY',
      reinforcements: 0, placement: 'ONE_FRONT_ENEMY_TRAIN',
      attackCycle: 'PRESERVE_ORIGINAL_FULL_FIRST_CYCLE_AND_LAUNCH_INTERVAL',
    },
    artillery: {
      appearanceRef: 'rail_artillery_emplacement_v1', count: 1, baseHp: 1000,
      hazardVersion: 'rail.artillery-hazard.v1.20261023', kind: 'SINGLE',
      baseRawDamageIntent: 50, damageApplicationsPerEvent: 1,
      damageOwner: 'EXISTING_TRAIN_DAMAGE_OWNER', perCarriageDamage: false,
      operatorDirectAttack: false, dangerLengthMm: 6000,
      launchAfterPreviewMs: 7500, impactAfterPreviewMs: 8000, flightMs: 500,
      destroyedBeforeLaunch: 'CANCEL_UNLAUNCHED_NO_REISSUE', destroyedAfterLaunch: 'KEEP_LAUNCHED_HAZARD',
      sameTickOrder: 'EXISTING_EVENT_ORDER',
    },
    minimumThreatClearToPreviewMs: 2000,
    previewRequires: ['BOTH_GUNNERS_DEFEATED_AND_REMAINING_THREATS_RESOLVED', 'STRAIGHT_SEGMENT',
      'AUTHORED_GEOMETRY_AND_SPEED_COUNTERPLAY_VERIFIED', 'SHARED_COMBAT_CLOCK', 'FIVE_ACTORS_READY'],
    scaling: {
      definitionVersion: scaling.version, definitionHash: scaling.definitionHash,
      hp: 'RISK_ONCE_NO_PROGRESS', rawDamage: 'RISK_ONCE_PROGRESS_ONCE_BEFORE_EXISTING_DEFENSE',
      precision: scaling.policy.precision,
      // These are definition alternatives, not evidence that a run selected one.
      variants: scaling.risks.flatMap(risk => scaling.progress.map(progress => {
        const hp = ExactCombatValue.from(risk.hp);
        const damage = ExactCombatValue.from(risk.damage).multiply(ExactCombatValue.from(progress.damage));
        return { risk: risk.id, progress: progress.id, hpMultiplier: risk.hp,
          riskDamageMultiplier: risk.damage, progressDamageMultiplier: progress.damage,
          gunnerHp: base(240).multiply(hp).snapshot(), launcherHp: base(1000).multiply(hp).snapshot(),
          gunnerRawDamage: ExactCombatValue.from(gunner.rawAction.value).multiply(damage).snapshot(),
          artilleryRawDamage: base(50).multiply(damage).snapshot() };
      })),
    },
    persistence: 'ORIGINAL_START_AND_EXISTING_CHECKPOINT_NO_RESPAWN_OR_HP_RESET',
    completion: 'SLICE_ONLY_NOT_FULL_LEARNING_VICTORY_OR_REWARD_AUTHORITY',
  };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}

/** New runs use the exact SINGLE wire; historical numeric-profile bytes stay unchanged. */
export async function firstPlayableProfileV2() {
  const { definitionHash: previousHash, ...previous } = await firstPlayableProfile();
  void previousHash;
  const definition = { ...previous, version: 'combat.first-playable-profile.v2.20261027',
    artillery: { ...previous.artillery, hazardVersion: exactRailArtilleryHazardVersion } };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}

/** Reference the shared objective once; historical profiles keep their original completion contract. */
export async function firstPlayableProfileV3() {
  const { definitionHash: previousHash, ...previous } = await firstPlayableProfileV2();
  void previousHash;
  const { version, definitionHash } = await firstEncounterObjectiveDefinition();
  const definition = { ...previous, version: 'combat.first-playable-profile.v3.20261028',
    completion: { version, definitionHash } };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}

/** Resolve the frozen risk/progress table by its existing definition hash instead of copying 15 rows. */
export async function firstPlayableProfileV4() {
  const { definitionHash: previousHash, ...previous } = await firstPlayableProfileV3();
  const { variants, ...scaling } = previous.scaling;
  void previousHash; void variants;
  const spatial = await firstEnemyTrainSpatialDefinition();
  const definition = { ...previous, version: 'combat.first-playable-profile.v4.20261028',
    enemies: { ...previous.enemies, spatial: { version: spatial.version, definitionHash: spatial.definitionHash } },
    scaling };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}
