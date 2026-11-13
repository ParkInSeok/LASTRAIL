import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { captureOwnedTrainBaseDefinitions } from './combat-owned-base-definitions';
import { calculateOwnedTrainPermanentStats } from './combat-owned-train-profile';
import { ActivityError } from './activity-policy';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';

const id = z.string().min(1).max(128), revision = z.number().int().positive().safe();
const originSchema = z.strictObject({ accountId: id, runId: id, ordinaryOriginHash: z.string().regex(/^[a-f0-9]{64}$/u),
  ownedSourceHash: z.string().regex(/^sha256:[a-f0-9]{64}$/u), loadoutRevision: revision,
  train: z.strictObject({ trainId: id, level: z.number().int().min(1).max(10), revision }) });
const json = (value: unknown) => canonicalJson(z.json().parse(value));

/** The caller supplies its already-read owned profile and commits the same ownership CAS in START. */
export async function captureOrdinaryTrainSource(input: unknown) {
  const origin = originSchema.parse(input), definition = captureOwnedTrainBaseDefinitions();
  const packet = { version: 'combat.ordinary-train-source.v1.20261027', ...origin,
    definitionVersion: definition.version, definitionHash: await sha256Hex(json(definition)),
    permanentStats: calculateOwnedTrainPermanentStats(origin.train.level) };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

/** New STARTs only. The ordinary START owner excludes commander ranks and pins
 * research/equipment applicability before run effects can be acquired. */
export async function captureOrdinaryTrainSourceV2(input: unknown) {
  const original = await captureOrdinaryTrainSource(input);
  const packet = { ...original.packet, version: 'combat.ordinary-train-source.v2.20261027',
    initialization: { policyVersion: 'TRAIN-INITIAL-HEALTH-RESUME-v1.20261027',
      departureEffects: 'NO_ACTIVE_DEPARTURE_EFFECTS' as const } };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function readOrdinaryTrainFromOriginal(original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>) {
  try {
    const snapshot = JSON.parse(original.startSnapshotJson);
    if (!Object.hasOwn(snapshot, 'ordinaryTrainSource')) return null;
    const stored = z.object({ packet: z.object({ version: z.enum(['combat.ordinary-train-source.v1.20261027',
      'combat.ordinary-train-source.v2.20261027']), train: originSchema.shape.train }) }).parse(snapshot.ordinaryTrainSource);
    const capture = stored.packet.version === 'combat.ordinary-train-source.v2.20261027'
      ? captureOrdinaryTrainSourceV2 : captureOrdinaryTrainSource;
    const expected = await capture({ accountId: original.origin.accountId, runId: original.origin.runId,
      ordinaryOriginHash: original.originHash, ownedSourceHash: original.origin.ownedSourceHash,
      loadoutRevision: original.origin.loadoutRevision, train: stored.packet.train });
    if (json(expected) !== json(snapshot.ordinaryTrainSource)) throw new Error('source mismatch');
    return expected;
  } catch { throw new ActivityError(409, 'ordinary_train_source_invalid'); }
}
