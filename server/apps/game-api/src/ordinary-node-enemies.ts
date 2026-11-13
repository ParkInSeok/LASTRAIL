import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { readFirstBattleEncounterFromOriginal, resolveFirstBattleProfile } from './combat-first-battle-source';
import { firstPlayableProfileV3 } from './combat-first-playable-profile';
import { firstObjectiveEncounterIdentities } from './combat-first-objective-verifier';
import { firstEnemyTrainSpatialDefinition, firstEnemyTrainInstanceId } from './combat-first-enemy-spatial';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import type { OrdinaryNodeStartOwner } from './ordinary-node-attempt';
import { firstPlayableScalingSchema } from './operation-map-state';

const fail = (): never => { throw new ActivityError(409, 'ordinary_node_enemy_source_conflict'); };

/** Initial issuance only, inside verified ARRIVE. Restart reads the saved node snapshot instead. */
export async function prepareOrdinaryNodeEnemies(original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>,
  context: Parameters<OrdinaryNodeStartOwner['prepare']>[0]) {
  const parsed = firstPlayableScalingSchema.safeParse(context.firstPlayableScaling);
  if (!parsed.success || original.startSnapshotJson !== context.startSnapshot) return fail();
  const { bindingHash, ...binding } = parsed.data;
  const source = await readFirstBattleEncounterFromOriginal(original);
  if (!source || !('selectedRisk' in source.packet)) return fail();
  const selectedRisk = source.packet.selectedRisk;
  const profile = await resolveFirstBattleProfile(source);
  if (!profile || binding.accountId !== context.accountId || binding.runId !== context.runId
    || binding.accountId !== original.origin.accountId || binding.runId !== original.origin.runId
    || binding.nodeId !== context.nodeId || binding.encounterId !== context.arrivalEventId
    || binding.progressRevision !== context.mapRevision || binding.sourceHash !== source.sourceHash
    || binding.startSnapshotHash !== await sha256Hex(context.startSnapshot)
    || bindingHash !== await sha256Hex(canonicalJson(z.json().parse(binding)))
    || binding.profileId !== profile.profileId || binding.profileVersion !== profile.version
    || binding.profileHash !== profile.definitionHash || binding.riskDefinitionVersion !== profile.scaling.definitionVersion
    || binding.riskDefinitionHash !== profile.scaling.definitionHash) return fail();
  const scaling = (await firstPlayableProfileV3()).scaling;
  const progress = binding.visitDepth <= 4 ? 'EARLY' : binding.visitDepth <= 8 ? 'MIDDLE' : 'LATE';
  const applied = scaling.variants.find(v => v.risk === selectedRisk && v.progress === progress);
  if (scaling.definitionHash !== binding.riskDefinitionHash || !applied
    || canonicalJson(applied) !== canonicalJson(binding.applied)) return fail();
  const identities = await firstObjectiveEncounterIdentities({ accountId: context.accountId, runId: context.runId,
    encounterId: context.arrivalEventId, sourceHash: source.sourceHash });
  if (profile.enemies.count !== identities.issuedGunnerIds.length) return fail();
  let spatial: { physicalInstanceId: string; definition: Awaited<ReturnType<typeof firstEnemyTrainSpatialDefinition>> } | undefined;
  if ('spatial' in profile.enemies) {
    const definition = await firstEnemyTrainSpatialDefinition();
    const physicalInstanceId = await firstEnemyTrainInstanceId({ accountId: context.accountId, runId: context.runId,
      encounterId: context.arrivalEventId, sourceHash: source.sourceHash });
    if (canonicalJson(profile.enemies.spatial) !== canonicalJson({ version: definition.version, definitionHash: definition.definitionHash })
      || canonicalJson(binding.enemySpatial ?? null) !== canonicalJson({ version: definition.version,
        definitionHash: definition.definitionHash, geometryDefinitionHash: definition.geometry.definitionHash, physicalInstanceId })) return fail();
    spatial = { physicalInstanceId, definition };
  } else if (binding.enemySpatial) return fail();
  return {
    version: spatial ? 'ordinary.node-enemies.v2.20261031' : 'ordinary.node-enemies.v1.20261031', scalingBindingHash: bindingHash,
    ...(spatial ? { spatial } : {}),
    // The first firing deadline depends on actual targetability; do not manufacture an entry-time target or timer.
    actionDefinition: profile.enemies.definition,
    gunners: identities.issuedGunnerIds.map(instanceId => ({ instanceId,
      hp: applied.gunnerHp, maxHp: applied.gunnerHp, defense: profile.enemies.defense,
      rawDamage: applied.gunnerRawDamage })),
    artillery: { hazardId: identities.issuedHazardId, hp: applied.launcherHp, maxHp: applied.launcherHp,
      rawDamage: applied.artilleryRawDamage, definition: profile.artillery,
      minimumThreatClearToPreviewMs: profile.minimumThreatClearToPreviewMs, previewRequires: profile.previewRequires },
  };
}
