import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import type { readOrdinaryRailFromOriginal } from './combat-ordinary-rail-source';

type Original = Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>;
type Rail = Awaited<ReturnType<typeof readOrdinaryRailFromOriginal>>;
const json = (value: unknown) => canonicalJson(z.json().parse(value));

/** Approved layout slots, not measured foot heights or permissions to enter the engine. */
export function captureOrdinaryRoofDefinition() {
  return { version: 'combat.ordinary-roof-layout.v1.20261027', approval: 'ORDINARY-FIVE-ROOF-THREE-TWO-v1.20261027',
    scope: 'FIRST_TRAIN_A_COMBAT_GUN_ONLY', footHeightSource: 'MEASURED_ROOF_SURFACE_PLUS_ACTOR_FOOT_OFFSET',
    existingPlacementAndCheckpointTakePrecedence: true,
    slots: [
      { slotId: 'combat-front', physicalOrder: 1, localXmm: 3000, localZmm: 0 },
      { slotId: 'combat-middle', physicalOrder: 1, localXmm: 0, localZmm: 0 },
      { slotId: 'combat-rear', physicalOrder: 1, localXmm: -3000, localZmm: 0 },
      { slotId: 'gun-front', physicalOrder: 2, localXmm: 3000, localZmm: 0 },
      { slotId: 'gun-rear', physicalOrder: 2, localXmm: -3000, localZmm: 0 },
    ] };
}

/** Consumes the already validated START origin; no placement commands or current-lobby reads. */
export async function captureOrdinaryRoofSource(origin: Original['origin'], originHash: string, rail: Rail) {
  // shortcut: ordinary START has no explicit placement owner; bind that owner before supporting explicit placements.
  if (!rail || !rail.packet.physicalConsist) return null;
  if (rail.packet.accountId !== origin.accountId || rail.packet.runId !== origin.runId
    || rail.packet.ordinaryOriginHash !== originHash) throw new ActivityError(409, 'ordinary_roof_source_invalid');
  const definition = captureOrdinaryRoofDefinition();
  const packet = { version: 'combat.ordinary-roof-source.v1.20261027', definitionVersion: definition.version,
    definitionHash: await sha256Hex(json(definition)), railSourceHash: rail.sourceHash,
    operatorIds: [...origin.actors].sort((a, b) => a.slot - b.slot).map(a => a.operatorId),
    status: 'LAYOUT_BOUND_ANCHORS_UNVERIFIED' };
  return { packet, sourceHash: await sha256Hex(json(packet)) };
}

export async function readOrdinaryRoofFromOriginal(original: Original, rail: Rail) {
  try {
    const snapshot = JSON.parse(original.startSnapshotJson);
    if (!Object.hasOwn(snapshot, 'ordinaryRoofSource')) return null;
    const expected = await captureOrdinaryRoofSource(original.origin, original.originHash, rail);
    if (!expected || json(expected) !== json(snapshot.ordinaryRoofSource)) throw new Error('source mismatch');
    return expected;
  } catch { throw new ActivityError(409, 'ordinary_roof_source_invalid'); }
}
