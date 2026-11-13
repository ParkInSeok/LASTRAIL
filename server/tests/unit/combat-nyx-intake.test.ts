import { expect, it } from 'vitest';
import { captureNyxApprovedSource } from '../../apps/game-api/src/combat-nyx-approved-source';
import { prepareNyxIntake, validateNyxZoneTrace } from '../../apps/game-api/src/combat-nyx-intake';
const scope = { accountId: 'a', runId: 'r', nodeId: 'n', attemptGeneration: 1, snapshotHash: 'a'.repeat(64) };
const formation = ['op_reina', 'op_kyle', 'op_eve', 'op_sera', 'op_nyx'].map((operatorId, slot) => ({ operatorId, slot }));
const exact = (n: number) => ({ numerator: String(n), denominator: '1' });
const create = (zoneId = 'z', atMs = 0) => ({ kind: 'CREATE', zoneId, atMs, ability: 'ACTIVE', attack: exact(100), skillMagnitude: exact(1) });
const tick = (index: number, budget = 30) => ({ kind: 'TICK', zoneId: 'z', atMs: index * 1000, index,
  eligibleTargetIds: ['b', 'a', 'c'], focusTargetId: null,
  targets: ['a', 'b'].map(targetId => ({ targetId, attackBudget: exact(budget), hpLoss: exact(1) })) });
async function intake() { return prepareNyxIntake({ scope, formation, approvedSource: await captureNyxApprovedSource() }); }
it('binds the requested slots and rejects a substituted source or Mira without enabling START', async () => {
  const source = await captureNyxApprovedSource();
  const value = await intake();
  expect(value.packet.formation).toEqual(formation); expect(value.packet.canStart).toBe(false);
  await expect(prepareNyxIntake({ scope, formation: [...formation.slice(0, 4), { slot: 4, operatorId: 'op_mira' }], approvedSource: source })).rejects.toThrow();
  source.packet.active.totalTickAttackBps++;
  await expect(prepareNyxIntake({ scope, formation, approvedSource: source })).rejects.toThrow();
});
it('validates divided budgets and the third-tick passive while rejecting double budget and early passive', async () => {
  const source = await intake();
  expect(validateNyxZoneTrace(source, { scope, events: [create(), tick(1), tick(2), tick(3, 33)] }).validatedTicks).toBe(3);
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), tick(1, 60)] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), tick(1, 33)] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), tick(2)] })).toThrow();
});
it('resets only the departed target streak and discards empty ticks without catch-up', async () => {
  const third = tick(3); third.targets[1]!.attackBudget = exact(33);
  const events = [create(), tick(1), tick(2), { kind: 'LEAVE', zoneId: 'z', atMs: 2500, targetId: 'a' }, third,
    { kind: 'TICK', zoneId: 'z', atMs: 4000, index: 4, eligibleTargetIds: [], focusTargetId: null, targets: [] }, tick(5)];
  expect(validateNyxZoneTrace(await intake(), { scope, events }).validatedTicks).toBe(5);
});
it('enforces last tick before natural expiry, replacement/down priority, and generation isolation', async () => {
  const source = await intake(), ticks = Array.from({ length: 6 }, (_, i) => tick(i + 1, i < 2 ? 30 : 33));
  expect(validateNyxZoneTrace(source, { scope, events: [create(), ...ticks, { kind: 'END', zoneId: 'z', atMs: 6000, reason: 'EXPIRY' }] }).activeZoneId).toBeNull();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), tick(1), { kind: 'END', zoneId: 'z', atMs: 1000, reason: 'CASTER_DOWN' }] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), tick(1), create('new', 1000)] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope: { ...scope, attemptGeneration: 2 }, events: [] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), create('new', 2500)] })).toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), ...ticks,
    { kind: 'END', zoneId: 'z', atMs: 6000, reason: 'EXPIRY' }, create('new', 6000)] })).toThrow();
});

it('uses the strongest period delay only for new reservations and respects Nyx control immunity', async () => {
  const source = await intake();
  const reserve = { kind: 'RESERVE', zoneId: 'z', atMs: 0, attackId: 'shot', basePeriod: exact(1000),
    period: exact(1150), inZone: true, controlImmune: false, otherPeriodIncreaseBps: 0 };
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), reserve] })).not.toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), { ...reserve, otherPeriodIncreaseBps: 3000, period: exact(1300) }] })).not.toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), { ...reserve, controlImmune: true, period: exact(1000) }] })).not.toThrow();
  expect(() => validateNyxZoneTrace(source, { scope, events: [create(), reserve, { ...reserve, period: exact(1000) }] })).toThrow();
});

it('uses the ultimate three-target budget, focus ordering, zero-loss reset, and isolated display copies', async () => {
  const source = await intake();
  source.packet.approvedSource.packet.active.totalTickAttackBps = 999999;
  const first = tick(1); first.targets[0]!.hpLoss = exact(0);
  const third = tick(3); third.targets[1]!.attackBudget = exact(33);
  expect(validateNyxZoneTrace(source, { scope, events: [create(), first, tick(2), third] }).validatedTicks).toBe(3);
  const ultimate = { ...tick(1), eligibleTargetIds: ['a', 'b', 'c', 'd'], focusTargetId: 'd',
    targets: ['d', 'a', 'b'].map(targetId => ({ targetId, attackBudget: exact(40), hpLoss: exact(1) })) };
  expect(validateNyxZoneTrace(source, { scope, events: [{ ...create(), ability: 'ULTIMATE' }, ultimate] }).validatedTicks).toBe(1);
});
