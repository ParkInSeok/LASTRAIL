import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';

/** Approved first trial and EnemySpatial805 measurements; not an executable combat proof. */
export async function firstEnemyTrainSpatialDefinition() {
  const definition = {
    version: 'combat.first-enemy-spatial.v1.20261028',
    approval: 'ORDINARY-ENEMY-TRAIN-SPATIAL-v1.20261028',
    geometry: {
      version: 'ordinary.enemy-train-authoring.v1.20261028',
      definitionHash: '2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1',
      lengthMm: 12000, widthMm: 3000, heightMm: 3200,
    },
    units: 'MILLIMETERS',
    placement: { gapFromPlayerFrontMm: 4000, gunnerRearOffsetsMm: [2000, 6000],
      footAnchorYmm: 3213, measuredRoofYmm: 3212.5 },
    motion: { source: 'PLAYER_ACTUAL_MOTION', relativeSpeedMmPerSecond: 0 },
    muzzle: null, hitVolume: null,
    executionVerified: false,
    targeting: 'PRESERVE_ORIGINAL_RANGE_AND_BODY_OCCLUSION',
    railBoundary: 'CHECK_ENEMY_FRONT_AND_EXISTING_END_MARGIN_WITH_ACTUAL_MOTION',
    restoration: 'EXISTING_CHECKPOINT_POSITIONS_HP_AND_PROJECTILES_TAKE_PRECEDENCE',
  };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}

/** Caller must supply the original accepted ARRIVE scope; this function grants no issuance authority. */
export async function firstEnemyTrainInstanceId(input: unknown) {
  const id = z.string().min(1).max(200);
  const origin = z.strictObject({ accountId: id, runId: id, encounterId: id,
    sourceHash: z.string().regex(/^[a-f0-9]{64}$/u) }).parse(input);
  const definition = await firstEnemyTrainSpatialDefinition();
  return (await sha256Hex(canonicalJson({ version: 'combat.first-enemy-instance.v1.20261028',
    ...origin, role: 'enemy-train', definitionHash: definition.definitionHash }))).slice(0, 32);
}


export const firstEnemyTrainBodyId = 'enemy_train_first_battle';

/** New catalog identity wraps the frozen spatial source; never changes old START/profile hashes. */
export async function firstEnemyTrainBodyDefinition() {
  const spatial = await firstEnemyTrainSpatialDefinition();
  const definition = {
    version: 'combat.first-enemy-body.v1.20261102', definitionId: firstEnemyTrainBodyId,
    role: 'TRAIN_BODY_OCCLUDER',
    spatialReference: { version: spatial.version, definitionHash: spatial.definitionHash },
    geometryReference: { version: spatial.geometry.version, definitionHash: spatial.geometry.definitionHash },
    assetPath: 'Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab',
    assetHash: '5844aaa8e930bc2b97c0fd831fa0acddf2b7cf60900b1585670c5f2f5e992d02',
  };
  return { ...definition, definitionHash: await sha256Hex(canonicalJson(definition)) };
}
