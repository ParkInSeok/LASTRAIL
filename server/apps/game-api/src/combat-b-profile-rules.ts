import { z } from 'zod';
import { ActivityError } from './activity-policy';

/** Approved structural B rules only; no admission, stats, persistence, or effects.
 * Remove this independent intake after a versioned B START owner consumes it.
 */
export const bProfileRulesVersion = 'combat.b-profile-rules.v1.20261002';
const id = z.string().min(1).max(128);
const slot = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const carriage = z.strictObject({ slot, carriageId: id });
const occupant = z.discriminatedUnion('kind', [
  z.strictObject({ kind: z.literal('OPERATOR'), operatorId: id }),
  z.strictObject({ kind: z.literal('CARGO') }),
]);
const request = z.strictObject({
  version: id, mountedTurrets: z.array(z.unknown()).max(128),
  placements: z.array(z.strictObject({ ...carriage.shape, occupant })).min(1).max(128),
});
const ownerSource = z.strictObject({
  accountId: id, selectedCarriages: z.array(carriage).min(1).max(128),
  ownedOperatorIds: z.array(id).max(1000),
}).refine(s => new Set(s.selectedCarriages.map(c => c.slot)).size === s.selectedCarriages.length
  && new Set(s.ownedOperatorIds).size === s.ownedOperatorIds.length);

/** source must come from authenticated server-owned selected-slot/ownership data.
 * This pure port cannot authenticate a caller or certify an arbitrary input owner.
 */
export function validateBPlacement(input: unknown, source: unknown) {
  const parsed = request.safeParse(input);
  if (!parsed.success) throw new ActivityError(400, 'combat_b_request_invalid');
  const owned = ownerSource.safeParse(source);
  if (!owned.success) throw new ActivityError(503, 'combat_b_source_invalid');
  const p = parsed.data, s = owned.data;
  if (p.version !== bProfileRulesVersion) throw new ActivityError(409, 'combat_b_version_conflict');
  if (p.mountedTurrets.length) throw new ActivityError(409, 'combat_b_mounted_turret_forbidden');
  const operators = p.placements.flatMap(c => c.occupant.kind === 'OPERATOR' ? [c.occupant.operatorId] : []);
  if (new Set(p.placements.map(c => c.slot)).size !== p.placements.length
    || new Set(operators).size !== operators.length) throw new ActivityError(409, 'combat_b_placement_duplicate');
  if (operators.length < 1 || operators.length > 5) throw new ActivityError(409, 'combat_b_operator_limit');
  if (p.placements.length !== s.selectedCarriages.length || p.placements.some(c =>
    !s.selectedCarriages.some(selected => selected.slot === c.slot && selected.carriageId === c.carriageId))) {
    throw new ActivityError(409, 'combat_b_selected_carriage_mismatch');
  }
  if (operators.some(operatorId => !s.ownedOperatorIds.includes(operatorId))) {
    throw new ActivityError(403, 'combat_b_operator_not_owned');
  }
  return { version: bProfileRulesVersion, accountId: s.accountId,
    camera: 'VERTICAL_FIXED' as const, interior: 'SPECIFIC_NODES_RETAINED' as const,
    placements: p.placements.sort((a, b) => a.slot - b.slot), operatorCount: operators.length,
    mountedTurrets: [] as never[], startReady: false as const,
    unbound: ['B_PERSISTED_SELECTION_AND_START_BINDING', 'B_DEFINITIONS_AND_EXECUTION'] };
}

/** Classification only, never a parser/restorer or migration of historical data.
 * Actual restore still validates its original schema, receipts, and permissions.
 */
export function resolveCombatPolicyBoundary(input: unknown) {
  const parsed = z.strictObject({
    purpose: z.enum(['NEW_B', 'RESTORE']), snapshotPolicyVersion: id.optional(),
  }).safeParse(input);
  if (!parsed.success) throw new ActivityError(400, 'combat_b_boundary_invalid');
  const { purpose, snapshotPolicyVersion } = parsed.data;
  if (purpose === 'NEW_B' && snapshotPolicyVersion !== bProfileRulesVersion) {
    throw new ActivityError(409, 'combat_b_version_conflict');
  }
  return { classification: snapshotPolicyVersion === bProfileRulesVersion ? 'B_RULES_ONLY' as const : 'HISTORICAL_PINNED' as const,
    policyVersion: snapshotPolicyVersion ?? null, startReady: false as const };
}