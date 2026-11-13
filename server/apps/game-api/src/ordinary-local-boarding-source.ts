import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { readOrdinaryFiveStatOrigin } from './combat-ordinary-stat-owner';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryRailFromOriginal } from './combat-ordinary-rail-source';
import { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';
import { readOrdinaryRoofFromOriginal } from './combat-ordinary-roof-source';
import { readDepartureMap } from './operation-map-departure';
import { capturePublishedOperationMap } from './operation-map-publisher';
import { ordinaryNodeExecutionOriginSchema } from './ordinary-node-execution-origin';
import { BScoutGunnerSource } from './combat-b-scout-gunner-source';
import { BBoardingEventSourceOwner } from './combat-b-boarding-events';

const id = z.string().min(1).max(200);
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (): never => { throw new Error('ordinary_local_boarding_origin_conflict'); };

/** Local QA source preparation only. Native boarding owns actual registration, destination, socket occupancy and phase.
 * This cannot add enemies to an already-running node or authorize the gunner-only live host. */
export async function prepareOrdinaryLocalBoardingSource(
  original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>, nodeIdInput: unknown, executionOriginInput: unknown,
) {
  const nodeId = id.parse(nodeIdInput), executionOrigin = ordinaryNodeExecutionOriginSchema.parse(executionOriginInput);
  await readOrdinaryFiveStatOrigin(original.origin, original.originHash);
  const start = z.object({ runId: id, operationPreviewId: id, operationId: id,
    risk: z.enum(['I', 'II', 'III', 'IV', 'V']), operationMapContent: z.unknown(),
    admission: z.object({ version: id }).passthrough() }).passthrough().parse(JSON.parse(original.startSnapshotJson));
  if (start.runId !== original.origin.runId) return fail();
  const plan = readDepartureMap(start);
  if (!plan || !plan.nodes.some(n => n.nodeId === nodeId && n.visitDepth === 1 && n.eventKind === 'COMBAT')) return fail();
  const publication = await capturePublishedOperationMap(start.operationMapContent, plan);
  const train = await readOrdinaryTrainFromOriginal(original);
  const rail = await readOrdinaryRailFromOriginal(original, train);
  if (!rail || !rail.packet.physicalConsist) return fail();
  const roof = await readOrdinaryRoofFromOriginal(original, rail);
  if (!roof) return fail();
  const startSnapshotHash = await sha256Hex(original.startSnapshotJson);
  const contextId = 'local-boarding:' + (await sha256Hex(json([startSnapshotHash, nodeId]))).slice(0, 32);
  // Domain-separated derivation from the committed node origin, never a new random roll or seed=1 fallback.
  const seed = await sha256Hex(json(['ORDINARY_LOCAL_BOARDING_V1', startSnapshotHash, nodeId, executionOrigin.determinism]));
  const owner = await BScoutGunnerSource.create({ nativeContextId: contextId, seed, plan,
    origin: { accountId: original.origin.accountId, runId: original.origin.runId,
      operationPreviewId: start.operationPreviewId, operationId: start.operationId, selectedRisk: start.risk,
      departureVersion: start.admission.version, baseSnapshotHash: startSnapshotHash,
      definitionVersion: plan.definitionVersion, planHash: await sha256Hex(json(plan)), publicationHash: publication.contentHash } });
  const generated = await owner.generateNode(nodeId);
  const boarding = await new BBoardingEventSourceOwner(owner).pin(generated, generated.sourceHash);
  const packet = { version: 'ordinary.local-boarding-source.v1.20261107',
    scope: 'LOCAL_QA_APPROVED_BOARDING_FULL_WAVES_NOT_EXISTING_GUNNER_SUBSTITUTION',
    accountId: original.origin.accountId, runId: original.origin.runId, nodeId, startSnapshotHash,
    executionOrigin, railSourceHash: rail.sourceHash, roofSourceHash: roof.sourceHash,
    physicalHostIds: [...rail.packet.physicalConsist.instanceIds], boarding,
    placementOwner: 'EXISTING_NATIVE_BOARDING_LEAST_ASSIGNED_LIVING_OPERATOR_CAR_THEN_FIXED_ORDER',
    registration: 'PLANNED_UNTIL_ACTUAL_ENTRANCE_NO_PREASSIGNED_SLOT_OR_ROOFTOP',
    geometryRequired: ['HOST_PHYSICAL_ID', 'CARRIAGE_FRAME', 'APPROACH_PATH', 'LEFT_RIGHT_CLIMB_SOCKET',
      'ROOF_PATH_AND_DESTINATION', 'FOOT_EMISSION_HIT_AND_HIT_VOLUME', 'GENERATION_AND_PHASE', 'SUPPORTED_TARGET_KIND_AND_AREA'],
    reentry: 'ORIGINAL_NODE_START_NEW_ATTEMPT_NO_MID_COMBAT_CONTINUATION',
    canStart: false, runtimeSupported: false };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}
