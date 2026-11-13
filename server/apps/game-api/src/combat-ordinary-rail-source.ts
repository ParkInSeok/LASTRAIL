import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { trainSelection } from './train-loadout';
import { captureOrdinaryTrialPhysicalDefinition } from './combat-ordinary-physical-source';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import type { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';

const id = z.string().min(1).max(128), revision = z.number().int().positive().safe();
const hash = z.string().regex(/^[a-f0-9]{64}$/u);
const selection = z.array(z.strictObject({ carriageId: id, slot: z.number().int().min(0).max(3), level: revision, revision }))
  .min(2).max(4).refine(rows => trainSelection.shape.carriages.safeParse(rows.map(({ carriageId, slot }) => ({ carriageId, slot }))).success);
const originSchema = z.strictObject({ accountId: id, runId: id, trainId: id, ordinaryOriginHash: hash,
  ownedSourceHash: z.string().regex(/^sha256:[a-f0-9]{64}$/u), loadoutRevision: revision, selectedCarriages: selection });
const admissionSchema = z.object({ version: z.literal('departure-v4'), loadoutRevision: revision, loadout: trainSelection,
  ownedState: z.object({ train: z.object({ trainId: id, level: revision, revision }),
    carriages: z.array(z.object({ carriageId: id, level: revision, revision })).min(2).max(4)
      .refine(rows => new Set(rows.map(row => row.carriageId)).size === rows.length) }) });
const json = (value: unknown) => canonicalJson(z.json().parse(value));

/** New ordinary trial, not a replacement for historical shot geometry or speed effects. */
export function ordinaryRailTrialDefinition() {
  return { version: 'combat.ordinary-rail-trial.v1.20261027', approval: 'ORDINARY-RAIL-FIRST-PROFILE-v1.20261027',
    trialBodyLengthMm: 12000, trialInterCarGapMm: 1000,
    speedMmPerSecond: { minimum: 14000, cruise: 20000, maximum: 26000 },
    accelerationMmPerSecondSquared: 5000, decelerationMmPerSecondSquared: 5000,
    speedEffectPolicy: 'PRESERVE_ORIGINAL_EFFECTS' };
}

/** Called by the owned START producer, with its existing ownership snapshot/CAS. */
export async function captureOrdinaryRailSource(input: unknown) {
  const origin = originSchema.parse(input), definition = ordinaryRailTrialDefinition();
  origin.selectedCarriages.sort((a, b) => a.slot - b.slot);
  const packet = { version: 'combat.ordinary-rail-source.v1.20261027', ...origin, definition,
    definitionHash: await sha256Hex(json(definition)), status: 'TRIAL_PROFILE_ISSUED_GEOMETRY_UNBOUND',
    // Account loadout IDs do not establish physical instances or engine inclusion.
    physicalConsist: null, geometry: null, initialMotion: null };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** Pins the approved capacity policy, without claiming any measured geometry is ready. */
export async function captureOrdinaryRailSourceV2(input: unknown) {
  const original = await captureOrdinaryRailSource(input);
  const definition = { ...original.packet.definition, version: 'combat.ordinary-rail-trial.v2.20261027',
    finiteRail: { version: 'ORDINARY-FINITE-RAIL-CAPACITY-v1.20261027', minimumLengthMm: 500000,
      initialTailArcMm: 20000, previewLookaheadMs: 10000, endMarginMm: 20000,
      maximumTravelIncludesSpeedEffects: true, prePreviewTravelBudgetRequired: true } };
  const packet = { ...original.packet, version: 'combat.ordinary-rail-source.v2.20261027', definition,
    definitionHash: await sha256Hex(json(definition)) };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** Physical IDs belong to the original run, independently of the shared health target. */
export async function captureOrdinaryRailSourceV3(input: unknown) {
  const original = await captureOrdinaryRailSourceV2(input), p = original.packet;
  const measured = captureOrdinaryTrialPhysicalDefinition();
  const selected = { rulesVersion: 'train-loadout-v1', trainId: p.trainId,
    carriages: p.selectedCarriages.map(({ carriageId, slot }) => ({ carriageId, slot })) };
  if (json(selected) !== json(measured.selectedLoadout)) return original;
  // 128-bit opaque IDs keep the existing bounded START envelope; source integrity retains full SHA-256.
  const instanceIds = await Promise.all(measured.vehicles.map(async (vehicle, order) => (await sha256Hex(json({
    version: 'ordinary.physical-instance.v1.20261027', accountId: p.accountId, runId: p.runId,
    ordinaryOriginHash: p.ordinaryOriginHash, order, selectionId: vehicle.selectionId,
    definitionHash: measured.definitionHash }))).slice(0, 32)));
  const physicalConsist = { version: measured.version, definitionHash: measured.definitionHash, instanceIds,
    totalLengthMm: measured.totalLengthMm, initialTailArcMm: measured.initialTailArcMm,
    initialFrontArcMm: measured.initialFrontArcMm };
  const { version, definitionHash, railSegmentId } = measured.rail;
  const { definition, ...boundOrigin } = p;
  const packet = { ...boundOrigin, version: 'combat.ordinary-rail-source.v3.20261027', definitionVersion: definition.version, physicalConsist,
    geometry: { version, definitionHash, railSegmentId }, status: 'GEOMETRY_BOUND_EXECUTION_UNREADY' };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** New trial STARTs only; the time budget is QA observation, never a victory/timeout rule. */
export async function captureOrdinaryRailSourceV4(input: unknown,
  train: Awaited<ReturnType<typeof readOrdinaryTrainFromOriginal>>) {
  const original = await captureOrdinaryRailSourceV3(input), p = original.packet;
  if (!p.physicalConsist) return original;
  if (!train || !z.object({ initialization: z.strictObject({
      policyVersion: z.literal('TRAIN-INITIAL-HEALTH-RESUME-v1.20261027'),
      departureEffects: z.literal('NO_ACTIVE_DEPARTURE_EFFECTS') }) }).safeParse(train.packet).success
    || train.packet.accountId !== p.accountId || train.packet.runId !== p.runId
    || train.packet.ordinaryOriginHash !== p.ordinaryOriginHash || train.packet.ownedSourceHash !== p.ownedSourceHash
    || train.packet.loadoutRevision !== p.loadoutRevision || train.packet.train.trainId !== p.trainId)
    throw new ActivityError(409, 'ordinary_rail_initial_motion_unavailable');
  const definition = await captureOrdinaryFirstPlayerRailDefinition(input);
  // Original origin already pins this ownership hash; do not repeat it in the bounded v4 envelope.
  const { ownedSourceHash: _ownedSourceHash, ...boundOrigin } = p;
  void _ownedSourceHash;
  const packet = { ...boundOrigin, version: 'combat.ordinary-rail-source.v4.20261028',
    definitionVersion: definition.version, definitionHash: await sha256Hex(json(definition)),
    geometry: { version: 'ordinary.finite-rail-authoring.v2.20261028',
      definitionHash: 'eeeb0da41505f285ffe497ba789c5610f8c2fab38ec4d1950549cac91b2b00bc',
      railSegmentId: 'ordinary-first-straight-v2' },
    initialMotion: { speedMmPerSecond: 20000, accelerationMmPerSecondSquared: 0 },
    status: 'MOTION_BOUND_CAPACITY_UNVERIFIED' };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function captureOrdinaryFirstPlayerRailDefinition(input: unknown) {
  const { packet } = await captureOrdinaryRailSourceV2(input);
  return { ...packet.definition, version: 'combat.ordinary-rail-trial.v3.20261028',
    firstPlayerTrial: { approval: 'ORDINARY-FIRST-PLAYER-TRAVEL-BUDGET-v1.20261028',
      gunnerObservationMs: 30000, preWarningDelayMs: 2000, warningMs: 8000, postImpactObservationMs: 2000,
      observationMs: 42000, authoredLengthMm: 1200000, productTimeLimit: false,
      initialSpeedMmPerSecond: 20000, initialAccelerationMmPerSecondSquared: 0,
      departureEffectsTakePrecedence: true, progressCheckpointTakesPrecedence: true,
      capacityRequiresOriginalSpeedEffects: true } };
}

/** Reads only an authoritative immutable START, never client geometry or today's loadout. */
export async function readOrdinaryRailFromOriginal(original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>,
  train: Awaited<ReturnType<typeof readOrdinaryTrainFromOriginal>>) {
  try {
    const snapshot = JSON.parse(original.startSnapshotJson);
    if (!Object.hasOwn(snapshot, 'ordinaryRailSource')) return null;
    if (!train) throw new Error('train source missing');
    const admission = admissionSchema.parse(snapshot.admission);
    if (admission.loadoutRevision !== original.origin.loadoutRevision
      || admission.loadout.trainId !== train.packet.train.trainId
      || json(admission.ownedState.train) !== json(train.packet.train)
      || admission.ownedState.carriages.length !== admission.loadout.carriages.length) throw new Error('departure mismatch');
    // Reuse the original selected ownership, never infer physical instances from HP targets.
    const selectedCarriages = admission.loadout.carriages.map(({ carriageId, slot }) => {
      const owned = admission.ownedState.carriages.find(row => row.carriageId === carriageId);
      if (!owned) throw new Error('selected carriage missing');
      return { ...owned, slot };
    });
    const version = z.object({ packet: z.object({ version: z.enum(['combat.ordinary-rail-source.v1.20261027',
      'combat.ordinary-rail-source.v2.20261027', 'combat.ordinary-rail-source.v3.20261027',
      'combat.ordinary-rail-source.v4.20261028']) }) }).parse(snapshot.ordinaryRailSource).packet.version;
    const capture = version === 'combat.ordinary-rail-source.v4.20261028' ? (input: unknown) => captureOrdinaryRailSourceV4(input, train)
      : version === 'combat.ordinary-rail-source.v3.20261027' ? captureOrdinaryRailSourceV3
      : version === 'combat.ordinary-rail-source.v2.20261027' ? captureOrdinaryRailSourceV2 : captureOrdinaryRailSource;
    const expected = await capture({ accountId: original.origin.accountId, runId: original.origin.runId,
      trainId: train.packet.train.trainId, ordinaryOriginHash: original.originHash, ownedSourceHash: original.origin.ownedSourceHash,
      loadoutRevision: original.origin.loadoutRevision, selectedCarriages });
    if (json(expected) !== json(snapshot.ordinaryRailSource)) throw new Error('source mismatch');
    return expected;
  } catch { throw new ActivityError(409, 'ordinary_rail_source_invalid'); }
}
