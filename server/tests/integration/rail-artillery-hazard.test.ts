import { expect, it } from 'vitest';
import { RailArtilleryHazard, ExactRailArtilleryHazard, verifyResolvedArtilleryImpact } from '../../apps/game-api/src/rail-artillery-hazard';
import { resolveNumericDefenseBudget } from '../../apps/game-api/src/combat-numeric-defense';

// Geometry, timing and damage here are test data, not published balance.
const definition = { version: 'rail.artillery-hazard.v1.20261023', runId: 'run', eventId: 'shot', trainId: 'train',
  railSegmentId: 'main', carriageIds: ['tail'], startMm: 100, endMm: 200, previewAtMs: 0,
  timing: { kind: 'SINGLE', impactAtMs: 10 }, damage: 30 };
const frame = (atMs: number, spans = [{ carriageId: 'tail', railSegmentId: 'main', startMm: 150, endMm: 250 }]) => ({
  runId: 'run', trainId: 'train', atMs, spans,
});
it('recomputes the actual node artillery collision and numeric result without authorizing HP', () => {
  const original = { ...definition, version: 'rail.artillery-hazard.v2.20261027', damage: { numerator: '253', denominator: '4' } };
  const numeric = { version: 'combat.numeric-defense-budget.v1', base: original.damage, effects: [],
    defense: { numerator: '1000', denominator: '1' }, weakening: [], penetration: [],
    reductions: { equipment: [], guardStance: [], independent: [] } };
  const request = { frame: frame(10), numeric, tick: 2, protected: false };
  const result = { collision: new ExactRailArtilleryHazard(original).resolve(request.frame),
    calculation: resolveNumericDefenseBudget(numeric), hpLoss: 31, absorbed: 0, hpAfter: 69, trainDefeatedNow: false };
  expect(verifyResolvedArtilleryImpact(original, { request, result }, 5).collision.outcome).toBe('HIT');
  for (const altered of [{ ...result, collision: { ...result.collision, outcome: 'MISSED' } },
    { ...result, calculation: { ...result.calculation, ordinaryBeforeShield: '32' } }, { ...result, hpLoss: 32 }])
    expect(() => verifyResolvedArtilleryImpact(original, { request, result: altered }, 5)).toThrow();
  expect(() => verifyResolvedArtilleryImpact(original, { request: { ...request, tick: 3 }, result }, 5)).toThrow();
  expect(() => verifyResolvedArtilleryImpact(original, { request: { ...request, protected: true }, result }, 5)).toThrow();
  const missed = { ...request, frame: frame(10, [{ carriageId: 'tail', railSegmentId: 'branch', startMm: 150, endMm: 250 }]) };
  const missResult = { ...result, collision: new ExactRailArtilleryHazard(original).resolve(missed.frame), hpLoss: 0, hpAfter: 100 };
  expect(verifyResolvedArtilleryImpact(original, { request: missed, result: missResult }, 5).collision.outcome).toBe('MISSED');
  expect(() => verifyResolvedArtilleryImpact(original, { request: missed, result: { ...missResult, hpLoss: 1 } }, 5)).toThrow();
  const oversized = { ...original, damage: { numerator: '2147483648', denominator: '1' } };
  const oversizedNumeric = { ...numeric, base: oversized.damage, defense: { numerator: '0', denominator: '1' } };
  expect(() => verifyResolvedArtilleryImpact(oversized, { request: { ...missed, numeric: oversizedNumeric }, result: {
    ...missResult, collision: new ExactRailArtilleryHazard(oversized).resolve(missed.frame),
    calculation: resolveNumericDefenseBudget(oversizedNumeric),
  } }, 5)).toThrow();
});

it('preserves rational raw damage through one multi-carriage impact, restore and the existing defense pipeline', () => {
  const exactDefinition = { ...definition, version: 'rail.artillery-hazard.v2.20261027', carriageIds: ['head', 'tail'],
    damage: { numerator: '253', denominator: '4' } };
  const owner = new ExactRailArtilleryHazard(exactDefinition);
  const input = frame(10, ['head', 'tail'].map(carriageId => ({ carriageId, railSegmentId: 'main', startMm: 150, endMm: 250 })));
  const result = owner.resolve(input);
  expect(result.rawDamageIntent).toEqual({ numerator: '253', denominator: '4' });
  expect(result.intersectingCarriageIds).toEqual(['head', 'tail']);
  const restored = ExactRailArtilleryHazard.restore(owner.snapshot());
  expect(restored.resolve(input)).toEqual(result);
  expect(restored.snapshot().receipts).toHaveLength(1);
  const defended = resolveNumericDefenseBudget({ version: 'combat.numeric-defense-budget.v1', base: result.rawDamageIntent,
    effects: [], defense: { numerator: '1000', denominator: '1' }, weakening: [], penetration: [],
    reductions: { equipment: [], guardStance: [], independent: [] } });
  expect(defended.exact).toEqual({ numerator: '253', denominator: '8' });
  expect(defended.ordinaryBeforeShield).toBe('31');
  const bad = owner.snapshot(); bad.receipts[0]!.result.rawDamageIntent.numerator = '252';
  expect(() => ExactRailArtilleryHazard.restore(bad)).toThrow('restore_result');
  expect(() => new RailArtilleryHazard(exactDefinition)).toThrow();
});

it('keeps exact misses zero and rejects noncanonical, zero or numeric exact-hazard damage', () => {
  const exactDefinition = { ...definition, version: 'rail.artillery-hazard.v2.20261027', damage: { numerator: '253', denominator: '4' } };
  const owner = new ExactRailArtilleryHazard(exactDefinition);
  expect(owner.resolve(frame(10, [{ carriageId: 'tail', railSegmentId: 'branch', startMm: 150, endMm: 250 }]))).toMatchObject({
    outcome: 'MISSED', rawDamageIntent: { numerator: '0', denominator: '1' } });
  for (const damage of [63.25, { numerator: '506', denominator: '8' }, { numerator: '0', denominator: '1' }, { numerator: '1', denominator: '0' }])
    expect(() => new ExactRailArtilleryHazard({ ...exactDefinition, damage })).toThrow();
});

it('binds restored launched threats to the separately retained original announcement', () => {
  const original = { ...definition, version: 'rail.artillery-hazard.v2.20261027', damage: { numerator: '253', denominator: '4' } };
  const owner = new ExactRailArtilleryHazard(original);
  const pending = owner.snapshot();
  const restored = ExactRailArtilleryHazard.restoreBound(pending, original);
  expect(restored.nextImpactAtMs()).toBe(10);
  const result = restored.resolve(frame(10));
  expect(ExactRailArtilleryHazard.restoreBound(restored.snapshot(), original).resolve(frame(10))).toEqual(result);
  for (const altered of [{ ...original, runId: 'other' }, { ...original, eventId: 'other' },
    { ...original, damage: { numerator: '999', denominator: '1' } },
    { ...original, timing: { kind: 'SINGLE', impactAtMs: 11 } },
    { ...original, startMm: 99 }, { ...original, trainId: 'other' }]) {
    // Each forged snapshot is internally consistent; self-validation alone cannot bind its origin.
    const forged = new ExactRailArtilleryHazard(altered).snapshot();
    expect(ExactRailArtilleryHazard.restore(forged).snapshot()).toEqual(forged);
    expect(() => ExactRailArtilleryHazard.restoreBound(forged, original)).toThrow('original_definition_conflict');
  }
  const before = owner.snapshot();
  expect(() => ExactRailArtilleryHazard.restoreBound(pending, null)).toThrow();
  expect(owner.snapshot()).toEqual(before);
});

it('hits once when the head passed but the tail still intersects, independent of carriage count', () => {
  const owner = new RailArtilleryHazard({ ...definition, carriageIds: ['head', 'middle', 'tail'] });
  const result = owner.resolve(frame(10, [
    { carriageId: 'head', railSegmentId: 'main', startMm: 300, endMm: 400 },
    { carriageId: 'tail', railSegmentId: 'main', startMm: 150, endMm: 250 },
    { carriageId: 'middle', railSegmentId: 'main', startMm: 190, endMm: 290 },
  ]));
  expect(result).toMatchObject({ outcome: 'HIT', rawDamageIntent: 30, intersectingCarriageIds: ['middle', 'tail'] });
  expect(owner.nextImpactAtMs()).toBeNull();
});

it('uses each carriage original segment while the train straddles a branch', () => {
  const owner = new RailArtilleryHazard({ ...definition, carriageIds: ['head', 'tail'] });
  const result = owner.resolve(frame(10, [
    { carriageId: 'head', railSegmentId: 'branch', startMm: 120, endMm: 180 },
    { carriageId: 'tail', railSegmentId: 'main', startMm: 150, endMm: 170 },
  ]));
  expect(result.intersectingCarriageIds).toEqual(['tail']);
  expect(new RailArtilleryHazard(definition).resolve(frame(10, [
    { carriageId: 'tail', railSegmentId: 'branch', startMm: 120, endMm: 180 },
  ])).outcome).toBe('MISSED');
});

it('treats spatial contact as intersection and never moves the preview target', () => {
  const owner = new RailArtilleryHazard(definition);
  expect(owner.resolve(frame(10, [{ carriageId: 'tail', railSegmentId: 'main', startMm: 200, endMm: 300 }])).outcome).toBe('HIT');
  expect(owner.snapshot().definition).toEqual(definition);
});

it('resolves only defined residual ticks in the half-open time interval', () => {
  const owner = new RailArtilleryHazard({ ...definition,
    timing: { kind: 'RESIDUAL', startAtMs: 10, endAtMs: 30, intervalMs: 10, maximumImpacts: 5 } });
  expect(() => owner.resolve(frame(11))).toThrow('impact_time');
  expect(owner.resolve(frame(10)).rawDamageIntent).toBe(30);
  expect(owner.nextImpactAtMs()).toBe(20);
  expect(owner.resolve(frame(20)).rawDamageIntent).toBe(30);
  expect(owner.nextImpactAtMs()).toBeNull();
  expect(() => owner.resolve(frame(30))).toThrow('impact_time');
});

it('restores the original receipt without applying another hit and rejects changed replay contents', () => {
  const owner = new RailArtilleryHazard(definition), input = frame(10), original = owner.resolve(input);
  const snapshot = owner.snapshot(), restored = RailArtilleryHazard.restore(snapshot);
  expect(restored.resolve(input)).toEqual(original);
  expect(restored.snapshot()).toEqual(snapshot);
  expect(() => restored.resolve(frame(10, [{ carriageId: 'tail', railSegmentId: 'main', startMm: 201, endMm: 250 }]))).toThrow('event_conflict');
  const corrupt = structuredClone(snapshot);
  corrupt.receipts[0]!.result.rawDamageIntent = 999;
  expect(() => RailArtilleryHazard.restore(corrupt)).toThrow();
});

it('requires complete explicit geometry and matching scope; failure preserves state', () => {
  const owner = new RailArtilleryHazard(definition), before = owner.snapshot();
  expect(() => owner.resolve(frame(10, []))).toThrow();
  expect(() => owner.resolve({ ...frame(10), runId: 'other' })).toThrow('scope');
  expect(() => owner.resolve({ ...frame(10), trainId: 'other' })).toThrow('scope');
  expect(owner.snapshot()).toEqual(before);
});

it('bounds residual hit count and exposes detached snapshots', () => {
  const owner = new RailArtilleryHazard({ ...definition,
    timing: { kind: 'RESIDUAL', startAtMs: 10, endAtMs: 100, intervalMs: 10, maximumImpacts: 1 } });
  const copy = owner.snapshot(); copy.definition.startMm = 999;
  const result = owner.resolve(frame(10)); result.intersectingCarriageIds.length = 0;
  expect(owner.snapshot().definition.startMm).toBe(100);
  expect(owner.snapshot().receipts[0]!.result.intersectingCarriageIds).toEqual(['tail']);
  expect(owner.nextImpactAtMs()).toBeNull();
});

it('does not accept missing tail geometry or double charge split spans of one carriage', () => {
  const owner = new RailArtilleryHazard({ ...definition, carriageIds: ['head', 'tail'] });
  expect(() => owner.resolve(frame(10, [
    { carriageId: 'head', railSegmentId: 'branch', startMm: 250, endMm: 300 },
  ]))).toThrow('incomplete_consist');
  const input = frame(10, [
    { carriageId: 'head', railSegmentId: 'branch', startMm: 250, endMm: 300 },
    { carriageId: 'tail', railSegmentId: 'main', startMm: 150, endMm: 170 },
    { carriageId: 'tail', railSegmentId: 'main', startMm: 170, endMm: 190 },
  ]);
  const result = owner.resolve(input);
  expect(result.rawDamageIntent).toBe(30);
  expect(result.intersectingCarriageIds).toEqual(['tail']);
  expect(owner.resolve({ ...input, spans: [...input.spans].reverse() })).toEqual(result);
});

it('rejects a duplicated restored impact and does not skip a missed scheduled impact', () => {
  const owner = new RailArtilleryHazard({ ...definition,
    timing: { kind: 'RESIDUAL', startAtMs: 10, endAtMs: 40, intervalMs: 10, maximumImpacts: 3 } });
  expect(() => owner.resolve(frame(20))).toThrow('impact_time');
  owner.resolve(frame(10, [{ carriageId: 'tail', railSegmentId: 'main', startMm: 201, endMm: 250 }]));
  const snapshot = owner.snapshot(); snapshot.receipts.push(structuredClone(snapshot.receipts[0]!));
  expect(() => RailArtilleryHazard.restore(snapshot)).toThrow('restore_order');
  expect(owner.nextImpactAtMs()).toBe(20);
});
