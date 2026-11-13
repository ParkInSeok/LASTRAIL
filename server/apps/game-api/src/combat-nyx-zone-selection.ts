import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { captureNyxApprovedSourceV3 } from './combat-nyx-approved-source';

const id = z.string().min(1).max(200), positive = z.number().int().positive().safe();
const point = z.strictObject({ x: z.number().int().safe(), y: z.number().int().safe(), z: z.number().int().safe() });
const scope = z.strictObject({ accountId: id, runId: id, nodeId: id, attemptGeneration: positive,
  snapshotHash: z.string().regex(/^[a-f0-9]{64}$/u) });
const zone = z.strictObject({ zoneId: id, kind: z.enum(['ALLIED_TRAIN_INTRUDERS', 'ENEMY_SELECTED_CARRIAGE']),
  carId: id, frameId: id, ownerGeneration: positive, representativeCenterWorldMm: point,
  localBoundary: z.strictObject({ minMm: point, maxMm: point }).refine(b =>
    b.minMm.x < b.maxMm.x && b.minMm.y < b.maxMm.y && b.minMm.z < b.maxMm.z),
  valid: z.boolean() });
const acceptance = z.strictObject({ scope, authorityRevision: positive, footWorldMm: point,
  zone, lineOfSight: z.boolean(), eligible: z.boolean() });
const creation = z.strictObject({ scope, zone });
const fail = (): never => { throw new Error('nyx_zone_selection_conflict'); };

/** Validates supplied authority observations only; never treats caller JSON as registered geometry or live START authority. */
export async function acceptNyxZoneSelection(input: unknown) {
  const value = acceptance.parse(input), approved = await captureNyxApprovedSourceV3();
  const range = BigInt(approved.packet.zone.selectionGeometry.maximumDistanceMm);
  const distanceSquared = (['x', 'y', 'z'] as const).reduce((sum, axis) => {
    const delta = BigInt(value.footWorldMm[axis]) - BigInt(value.zone.representativeCenterWorldMm[axis]);
    return sum + delta * delta;
  }, 0n);
  if (!value.zone.valid || !value.lineOfSight || !value.eligible || distanceSquared > range * range) return fail();
  return new NyxZoneSelection(value, approved.sourceHash);
}

class NyxZoneSelection {
  readonly #accepted: z.infer<typeof acceptance>;
  readonly approvedSourceHash: string;
  constructor(accepted: z.infer<typeof acceptance>, sourceHash: string) {
    this.#accepted = structuredClone(accepted); this.approvedSourceHash = sourceHash;
  }
  /** No active-zone mutation: the live owner replaces its old zone only after successful creation. */
  validateCreation(input: unknown) {
    const current = creation.parse(input), original = this.#accepted;
    if (canonicalJson(current.scope) !== canonicalJson(original.scope) || !current.zone.valid
      || current.zone.zoneId !== original.zone.zoneId || current.zone.kind !== original.zone.kind
      || current.zone.carId !== original.zone.carId || current.zone.frameId !== original.zone.frameId
      || current.zone.ownerGeneration !== original.zone.ownerGeneration
      || canonicalJson(current.zone.localBoundary) !== canonicalJson(original.zone.localBoundary)) return fail();
    return { zone: structuredClone(original.zone), scope: structuredClone(original.scope),
      authorityRevision: original.authorityRevision, approvedSourceHash: this.approvedSourceHash,
      canStart: false, validation: 'SUPPLIED_ZONE_OBSERVATIONS_ONLY' as const };
  }
}

export async function captureNyxZoneSelectionSource() {
  const approved = await captureNyxApprovedSourceV3();
  const packet = { version: 'combat.nyx-zone-selection.v1.20261107', approvedVersion: approved.packet.version,
    approvedSourceHash: approved.sourceHash, geometry: approved.packet.zone.selectionGeometry,
    scope: approved.packet.zone.targetScope, canStart: false, midCombatPersistence: false };
  return { packet, sourceHash: await sha256Hex(canonicalJson(packet)) };
}
