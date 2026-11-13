import { expect, it } from 'vitest';
import { replayReinaBarrage, reinaBarrageChargeCommandId, verifyReinaBarrageChargeTransition } from '../../apps/game-api/src/reina-barrage';
import { CombatUltimateState } from '../../apps/game-api/src/combat-ultimate-state';

const target = (id: string, x = 0) => ({ id, generation: 1, alive: true, kind: 'ENEMY', order: x,
  offsetMm: [x, 0, 0], muzzleDeltaMm: [24000, 0, 0], castScopeEligible: true, muzzleClear: true, centerClear: true });
const commit = (count = 4) => ({ kind: 'COMMIT', tick: 16, casterAlive: true, actionEligible: true, charge: 100,
  attack: { numerator: '100', denominator: '1' }, frame: { id: 'enemy-frame', generation: 1, centerMm: [0, 0, 0] },
  candidates: ['main', 'a', 'b', 'c'].slice(0, count).map((id, i) => target(id, i * 100)) });
const impact = (index: number, targets = commit().candidates) => ({ kind: 'IMPACT', tick: [44, 60, 76, 92][index], index,
  frame: { id: 'enemy-frame', generation: 1, exists: true }, targets: targets.map(({ id, generation, alive, offsetMm, centerClear }) => ({ id, generation, alive, offsetMm, centerClear })) });
const input = (operations: unknown[]) => ({ version: 'combat.reina-barrage-replay.v1.20261102', castId: 'cast',
  nodeId: 'node', attemptGeneration: 1, acceptedTick: 0, stepTimeSpanTicks: 500000,
  primary: { id: 'main', generation: 1 }, chargeAtAcceptance: 100, casterAliveAtAcceptance: true,
  actionEligibleAtAcceptance: true, operations });

it.each([1, 2, 3, 4])('resolves four committed pulses for %i original targets', count => {
  const c = commit(count), result = replayReinaBarrage(input([c, ...[0, 1, 2, 3].map(i => impact(i, c.candidates))]));
  expect(result.consumedCharge).toBe(100);
  expect(result.impactTicks).toEqual([44, 60, 76, 92]);
  expect(result.unresolvedImpacts).toBe(0);
  expect(result.pulses.flatMap(p => p.hits).reduce((sum, h) => sum + h.damageBps, 0)).toBe(100000 + (count - 1) * 30000);
  expect(result.pulses[0]!.hits[0]!.rawDamage).toEqual({ numerator: '250', denominator: '1' });
});
it('cancels before commit without spending and keeps committed pulses after caster down', () => {
  expect(replayReinaBarrage(input([{ kind: 'CANCEL', tick: 8, reason: 'CASTER_DOWN' }]))).toMatchObject({ consumedCharge: 0, unresolvedImpacts: 0 });
  const c = commit(); c.casterAlive = false;
  expect(replayReinaBarrage(input([c]))).toMatchObject({ consumedCharge: 0, unresolvedImpacts: 0 });
  expect(replayReinaBarrage(input([commit(), { kind: 'CANCEL', tick: 20, reason: 'CASTER_DOWN' }, impact(0)])))
    .toMatchObject({ consumedCharge: 100, unresolvedImpacts: 3 });
});
it('keeps the fixed set through exit/reentry and main death, but cancels on original frame loss', () => {
  const outside = commit().candidates; outside[0]!.alive = false; outside[1]!.offsetMm = [3001, 0, 0];
  const returned = commit().candidates; returned[0]!.alive = false;
  const result = replayReinaBarrage(input([commit(), impact(0, outside), impact(1, returned),
    { kind: 'CANCEL', tick: 65, reason: 'FRAME_LOST' }]));
  expect(result.pulses[0]!.hits.map(h => h.targetId)).toEqual(['b', 'c']);
  expect(result.pulses[1]!.hits.map(h => h.targetId)).toEqual(['a', 'b', 'c']);
  expect(result).toMatchObject({ consumedCharge: 100, unresolvedImpacts: 0 });
});
it('rejects duplicate/out-of-order impacts and never rounds by accumulating frames', () => {
  expect(() => replayReinaBarrage(input([commit(), impact(0), impact(0)]))).toThrow();
  expect(() => replayReinaBarrage(input([commit(), impact(1)]))).toThrow();
  const c = commit(); c.candidates[0]!.muzzleDeltaMm = [24001, 0, 0];
  expect(replayReinaBarrage(input([c])).consumedCharge).toBe(0);
  const rounded = { ...input([]), stepTimeSpanTicks: 700000, operations: [{ ...commit(), tick: 12 }] };
  expect(replayReinaBarrage(rounded).impactTicks).toEqual([32, 44, 55, 67]);
  expect(() => replayReinaBarrage(input([commit(), { kind: 'CANCEL', tick: 45, reason: 'FRAME_LOST' }]))).toThrow();
});
it('uses the exact radius boundary, original generations and fixed frame without accepting new targets', () => {
  const c = commit(2); c.candidates[1]!.offsetMm = [3000, 0, 0];
  expect(replayReinaBarrage(input([c, impact(0, c.candidates)])).pulses[0]!.hits).toHaveLength(2);
  const changed = impact(0, c.candidates); changed.targets[1]!.generation = 2;
  expect(replayReinaBarrage(input([c, changed])).pulses[0]!.hits.map(h => h.targetId)).toEqual(['main']);
  const blocked = impact(0, c.candidates); blocked.targets[1]!.centerClear = false;
  expect(replayReinaBarrage(input([c, blocked])).pulses[0]!.hits.map(h => h.targetId)).toEqual(['main']);
  expect(() => replayReinaBarrage(input([c, { ...impact(0, c.candidates), frame: { id: 'other', generation: 1, exists: true } }]))).toThrow();
  expect(() => replayReinaBarrage(input([c, impact(0, [...c.candidates, target('new')])]))).toThrow();
});
it('selects at most three additions by distance, original order then stable identity', () => {
  const c = commit(1);
  c.candidates.push(target('z', 100), target('c', 100), target('b', 100), target('a', 100),
    { ...target('structure', 0), kind: 'STRUCTURE' }, { ...target('blocked', 0), muzzleClear: false });
  expect(replayReinaBarrage(input([c])).selected.map(t => t.id)).toEqual(['main', 'a', 'b', 'c']);
  expect(() => replayReinaBarrage({ ...input([]), chargeAtAcceptance: 0 })).toThrow();
});
it('reuses the original charge owner at commit without charging cancellation or replay twice', async () => {
  const scope = { accountId: 'account', runId: 'run' };
  const cast = input([commit(), ...[0, 1, 2, 3].map(i => impact(i))]), replay = replayReinaBarrage(cast);
  const owner = CombatUltimateState.start('op_reina');
  for (let i = 0; i < 25; i++) owner.credit({ actionId: `basic-${i}`, hitOrdinal: 0, sourceActorId: 'op_reina',
    sourceAlive: true, sourceKind: 'BASIC', mode: 'ORDINARY', resolvedDamage: 1 });
  const before = owner.snapshot(), commandId = await reinaBarrageChargeCommandId(scope, replay);
  owner.cast(commandId, true);
  const transition = { before, after: owner.snapshot() };
  expect(await verifyReinaBarrageChargeTransition(scope, replay, 100, transition)).toMatchObject({ accepted: true, before: 100, after: 0 });
  expect(await verifyReinaBarrageChargeTransition(scope, replay, 100, transition)).toMatchObject({ after: 0 });
  expect(before.charge).toBe(100); expect(transition.after.casts).toHaveLength(1);
  await expect(verifyReinaBarrageChargeTransition(scope, replay, 100, undefined)).rejects.toThrow();
  await expect(verifyReinaBarrageChargeTransition(scope, replay, 104, transition)).rejects.toThrow();
  await expect(verifyReinaBarrageChargeTransition({ ...scope, runId: 'other' }, replay, 100, transition)).rejects.toThrow();
  await expect(verifyReinaBarrageChargeTransition(scope, replay, 100, { before: transition.after, after: transition.after })).rejects.toThrow();
  const cancelled = replayReinaBarrage(input([{ kind: 'CANCEL', tick: 8, reason: 'CASTER_DOWN' }]));
  expect(await verifyReinaBarrageChargeTransition(scope, cancelled, undefined, undefined)).toBeNull();
  await expect(verifyReinaBarrageChargeTransition(scope, cancelled, undefined, transition)).rejects.toThrow();
});
it('keeps each committed barrage independent after recovery ends', () => {
  const firstInput = input([commit()]);
  const first = replayReinaBarrage(firstInput);
  expect(first.recoveryUntilTick).toBe(22);
  expect(first.impactTicks[0]).toBe(44);
  expect(first.unresolvedImpacts).toBe(4);
  const nextCommit = commit(); nextCommit.tick = 38;
  nextCommit.frame.id = 'second-frame';
  const second = replayReinaBarrage({ ...input([nextCommit]), castId: 'second-cast', acceptedTick: 22 });
  expect(second.frame!.id).toBe('second-frame');
  expect(second.impactTicks[0]).toBe(66);
  expect(replayReinaBarrage(firstInput)).toEqual(first);
});
