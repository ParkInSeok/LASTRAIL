import { describe, expect, it } from 'vitest';
import { bProfileRulesVersion, validateBPlacement, resolveCombatPolicyBoundary } from '../../apps/game-api/src/combat-b-profile-rules';
const source = { accountId: 'a', selectedCarriages: [{ slot: 0, carriageId: 'car0' }, { slot: 1, carriageId: 'car1' }], ownedOperatorIds: ['eve', 'iris'] };
const placement = () => ({ version: bProfileRulesVersion, mountedTurrets: [], placements: [
  { slot: 0, carriageId: 'car0', occupant: { kind: 'OPERATOR', operatorId: 'eve' } },
  { slot: 1, carriageId: 'car1', occupant: { kind: 'CARGO' } },
] });
describe('B approved placement and policy boundary only', () => {
 it('accepts operator plus optional cargo, without granting START readiness', () => {
  const r = validateBPlacement(placement(), source);
  expect(r.version).toBe(bProfileRulesVersion);
  expect(r.operatorCount).toBe(1);
  expect(r.startReady).toBe(false);
  expect(r.camera).toBe('VERTICAL_FIXED');
  expect(r.interior).toBe('SPECIFIC_NODES_RETAINED');
 });
 it('accepts selected carriages occupied solely by operators', () => {
  const p = placement(); p.placements[1] = { slot: 1, carriageId: 'car1', occupant: { kind: 'OPERATOR', operatorId: 'iris' } };
  expect(validateBPlacement(p, source).operatorCount).toBe(2);
 });
 it('rejects mounted carriage turret while not banning Eve summon IDs', () => {
  expect(() => validateBPlacement({ ...placement(), mountedTurrets: [{ turretId: 'eve_sentry' }] }, source)).toThrow('combat_b_mounted_turret_forbidden');
  expect(validateBPlacement(placement(), source).operatorCount).toBe(1);
 });
 it.each(['train.loadout.v1', 'combat.owned-train-intake.v1', 'future'])('rejects new placement version %s', version => {
  expect(() => validateBPlacement({ ...placement(), version }, source)).toThrow('combat_b_version_conflict');
 });
 it('rejects foreign operator and carriage without mutating either source', () => {
  const p = placement(); p.placements[0]!.occupant = { kind: 'OPERATOR', operatorId: 'foreign' };
  const before = JSON.stringify({ p, source });
  expect(() => validateBPlacement(p, source)).toThrow('combat_b_operator_not_owned');
  expect(JSON.stringify({ p, source })).toBe(before);
  expect(() => validateBPlacement({ ...placement(), placements: [{ slot: 0, carriageId: 'foreign', occupant: { kind: 'OPERATOR', operatorId: 'eve' } }] }, source)).toThrow('combat_b_selected_carriage_mismatch');
 });
 it('rejects duplicate occupied slot and duplicate operator', () => {
  const p = placement();
  expect(() => validateBPlacement({ ...p, placements: [p.placements[0], p.placements[0]] }, source)).toThrow('combat_b_placement_duplicate');
  expect(() => validateBPlacement({ ...p, placements: [p.placements[0], { slot: 1, carriageId: 'car1', occupant: { kind: 'OPERATOR', operatorId: 'eve' } }] }, source)).toThrow('combat_b_placement_duplicate');
 });
 it('rejects more than five operators with otherwise valid source', () => {
  const placements = Array.from({ length: 6 }, (_, slot) => ({ slot, carriageId: 'c' + slot, occupant: { kind: 'OPERATOR', operatorId: 'o' + slot } }));
  expect(() => validateBPlacement({ ...placement(), placements }, { accountId: 'a', selectedCarriages: placements.map(p => ({ slot: p.slot, carriageId: p.carriageId })), ownedOperatorIds: placements.map(p => p.occupant.operatorId) })).toThrow('combat_b_operator_limit');
 });
 it('rejects missing selected slot, invalid occupant, and unknown client effect input', () => {
  expect(() => validateBPlacement({ ...placement(), placements: [placement().placements[0]] }, source)).toThrow('combat_b_selected_carriage_mismatch');
  expect(() => validateBPlacement({ ...placement(), effects: ['damage'] }, source)).toThrow('combat_b_request_invalid');
  expect(() => validateBPlacement({ ...placement(), placements: [{ slot: 0, carriageId: 'car0', occupant: { kind: 'TURRET' } }] }, source)).toThrow('combat_b_request_invalid');
 });
 it('requires an operator, no invented cargo reward or stat fields', () => {
  expect(() => validateBPlacement({ ...placement(), placements: placement().placements.map(p => ({ ...p, occupant: { kind: 'CARGO' } })) }, source)).toThrow('combat_b_operator_limit');
  expect(validateBPlacement(placement(), source)).not.toHaveProperty('cargoMultiplier');
 });
 it('rejects malformed authoritative input rather than accepting client ownership', () => {
  expect(() => validateBPlacement(placement(), { ...source, selectedCarriages: [source.selectedCarriages[0], source.selectedCarriages[0]] })).toThrow('combat_b_source_invalid');
  expect(() => validateBPlacement(placement(), { ...source, accountId: '' })).toThrow('combat_b_source_invalid');
 });
 it('returns detached value, preserves existing investment, and does not rewrite snapshots', () => {
  const p = placement(), before = JSON.stringify(p);
  const r = validateBPlacement(p, source); r.placements[0]!.carriageId = 'changed';
  expect(JSON.stringify(p)).toBe(before);
  const snapshot = { combatPolicyVersion: 'A-original', turrets: [{ level: 10 }], hp: 600 };
  const bytes = JSON.stringify(snapshot);
  expect(resolveCombatPolicyBoundary({ purpose: 'RESTORE', snapshotPolicyVersion: snapshot.combatPolicyVersion })).toEqual({ classification: 'HISTORICAL_PINNED', policyVersion: 'A-original', startReady: false });
  expect(JSON.stringify(snapshot)).toBe(bytes);
 });
 it('requires explicit B new version, preserves missing/unknown historical version', () => {
  expect(resolveCombatPolicyBoundary({ purpose: 'NEW_B', snapshotPolicyVersion: bProfileRulesVersion }).classification).toBe('B_RULES_ONLY');
  expect(() => resolveCombatPolicyBoundary({ purpose: 'NEW_B' })).toThrow('combat_b_version_conflict');
  expect(() => resolveCombatPolicyBoundary({ purpose: 'NEW_B', snapshotPolicyVersion: 'A' })).toThrow('combat_b_version_conflict');
  expect(resolveCombatPolicyBoundary({ purpose: 'RESTORE' }).policyVersion).toBe(null);
  expect(resolveCombatPolicyBoundary({ purpose: 'RESTORE', snapshotPolicyVersion: 'future' }).policyVersion).toBe('future');
 });
});