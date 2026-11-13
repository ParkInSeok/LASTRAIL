import { z } from 'zod';
import { ordinaryCombatDefinitionMatches } from './ordinary-obstacle-sequence';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { firstBattleEncounterSourceV4Version, firstBattleEncounterSourceV5Version, firstBattleEncounterSourceV6Version, firstBattleEncounterSourceV7Version, firstBattleEncounterSourceV8Version, readFirstBattleEncounterFromOriginal, resolveFirstBattleProfile } from './combat-first-battle-source';
import { firstPlayableProfileV3 } from './combat-first-playable-profile';
import { firstEnemyTrainSpatialDefinition, firstEnemyTrainInstanceId } from './combat-first-enemy-spatial';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { projectOperationMap, type OperationMapPlan } from './operation-map-plan';
import { capturePublishedOperationMap } from './operation-map-publisher';
import { firstPlayableScalingSchema, type mapProgress } from './operation-map-state';

const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (): never => { throw new ActivityError(409, 'first_playable_scaling_source_conflict'); };

/** Prepare only. The map owner commits this binding with verified arrival, progress and its original receipt. */
export async function prepareFirstPlayableScaling(db: D1Database, input: { accountId: string; runId: string;
  eventId: string; revision: number; startSnapshot: string; plan: OperationMapPlan; state: z.infer<typeof mapProgress> }) {
  const snapshot = JSON.parse(input.startSnapshot);
  // Older runs have no approved scaling contract; they are never upgraded at arrival.
  if (![firstBattleEncounterSourceV4Version, firstBattleEncounterSourceV5Version, firstBattleEncounterSourceV6Version, firstBattleEncounterSourceV7Version, firstBattleEncounterSourceV8Version]
    .includes(snapshot.firstBattleEncounterSource?.packet?.version)) return undefined;
  const original = await readOrdinaryRunStatOrigin(db, input.accountId, input.runId);
  if (original.startSnapshotJson !== input.startSnapshot) return fail();
  const source = await readFirstBattleEncounterFromOriginal(original);
  if (!source || !('selectedRisk' in source.packet)) return fail();
  const definition = await resolveFirstBattleProfile(source);
  if (!definition) return fail();
  const nodeId = projectOperationMap(input.plan, input.state.traversedEdgeIds).currentNodeId;
  const node = input.plan.nodes.find(n => n.nodeId === nodeId);
  if (!node || node.encounterProfileRef !== definition.profileId) return undefined;
  if (snapshot.encounterType !== 'ORDINARY_12_NODE' || input.state.phase !== 'ACTIVE' || input.state.pendingEdgeId
    || input.state.holdIds.length || node.eventKind !== 'COMBAT' || node.visitDepth < 1 || node.visitDepth > 11) return fail();
  const publication = await capturePublishedOperationMap(snapshot.operationMapContent, input.plan);
  const entry = publication.encounters.find(e => e.ref === node.encounterProfileRef);
  if (!entry || entry.version !== definition.version || !ordinaryCombatDefinitionMatches(entry.definition, definition)) return fail();
  const row = await db.prepare(`SELECT p.id,p.risk,p.objective,p.balance_config_version,p.admission_version,
    i.request_id,i.response_json,i.response_hash FROM runs r JOIN operation_previews p ON p.id=r.preview_id
    JOIN ordinary_combat_activation_receipts i ON i.run_id=r.id JOIN ordinary_combat_phases c ON c.run_id=r.id
    WHERE r.id=? AND r.account_id=? AND p.account_id=r.account_id AND p.status='CONSUMED'
    AND r.balance_config_version=p.balance_config_version AND c.account_id=r.account_id
    AND c.phase='RUNNING' AND c.origin_hash=? AND c.start_snapshot_json=?`)
    .bind(input.runId, input.accountId, original.originHash, input.startSnapshot).first<{
      id: string; risk: string; objective: string; balance_config_version: number; admission_version: string;
      request_id: string; response_json: string; response_hash: string }>();
  if (!row || row.risk !== source.packet.selectedRisk || row.id !== snapshot.operationPreviewId
    || row.objective !== snapshot.operationId || String(row.balance_config_version) !== String(snapshot.balanceVersion)
    || row.admission_version !== snapshot.admission.version || await sha256Hex(row.response_json) !== row.response_hash) return fail();
  const receipt = JSON.parse(row.response_json);
  if (receipt.runId !== input.runId || receipt.phase !== 'RUNNING' || json(receipt.firstBattleEncounterBinding)
    !== json({ version: source.packet.version, originHash: original.originHash, sourceHash: source.sourceHash })) return fail();
  const progress = node.visitDepth <= 4 ? 'EARLY' : node.visitDepth <= 8 ? 'MIDDLE' : 'LATE';
  const resolved = (await firstPlayableProfileV3()).scaling;
  if (definition.scaling.definitionVersion !== resolved.definitionVersion || definition.scaling.definitionHash !== resolved.definitionHash) return fail();
  const applied = resolved.variants.find(v => v.risk === row.risk && v.progress === progress);
  if (!applied) return fail();
  const spatial = [firstBattleEncounterSourceV7Version, firstBattleEncounterSourceV8Version].includes(source.packet.version) ? await firstEnemyTrainSpatialDefinition() : null;
  const enemySpatial = spatial ? { version: spatial.version, definitionHash: spatial.definitionHash,
    geometryDefinitionHash: spatial.geometry.definitionHash, physicalInstanceId: await firstEnemyTrainInstanceId({
      accountId: input.accountId, runId: input.runId, encounterId: input.eventId, sourceHash: source.sourceHash }) } : undefined;
  const payload = { version: enemySpatial ? 'combat.first-playable-scaling.v2.20261028' : 'combat.first-playable-scaling.v1.20261027', accountId: input.accountId, runId: input.runId,
    encounterId: input.eventId, nodeId, visitDepth: node.visitDepth, progressRevision: input.revision,
    progressStateHash: await sha256Hex(json(input.state)), startSnapshotHash: await sha256Hex(input.startSnapshot),
    initializationResponseHash: row.response_hash, sourceHash: source.sourceHash, profileId: definition.profileId,
    profileVersion: definition.version, profileHash: definition.definitionHash,
    riskDefinitionVersion: definition.scaling.definitionVersion, riskDefinitionHash: definition.scaling.definitionHash,
    applied, hpRiskApplications: 1, damageRiskApplications: 1, damageProgressApplications: 1,
    ...(enemySpatial ? { enemySpatial } : {}) };
  const binding = firstPlayableScalingSchema.parse({ ...payload, bindingHash: await sha256Hex(json(payload)) });
  const assertion = db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM runs r JOIN operation_previews p ON p.id=r.preview_id
    JOIN ordinary_combat_activation_receipts i ON i.run_id=r.id
    JOIN ordinary_combat_phases c ON c.run_id=i.run_id WHERE p.id=? AND p.account_id=? AND p.status='CONSUMED'
    AND r.id=? AND r.account_id=p.account_id AND r.balance_config_version=p.balance_config_version
    AND p.risk=? AND p.objective=? AND p.balance_config_version=? AND p.admission_version=?
    AND i.request_id=? AND i.response_json=? AND i.response_hash=? AND c.account_id=?
    AND c.phase='RUNNING' AND c.origin_hash=? AND c.start_snapshot_json=?) THEN 1 ELSE 0 END`)
    .bind(row.id, input.accountId, input.runId, row.risk, row.objective, row.balance_config_version, row.admission_version,
      row.request_id, row.response_json, row.response_hash, input.accountId, original.originHash, input.startSnapshot);
  return { binding, assertion };
}
