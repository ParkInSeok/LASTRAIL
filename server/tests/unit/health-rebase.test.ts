import { expect, it } from 'vitest';
import { HealthRebaseState } from '../../packages/common/src/health-rebase';

const create = (hp: number, maxHp: number) => HealthRebaseState.create({ policyVersion: 'health.max-hp-rebase.v1', hp, maxHp });

it('preserves the original ratio through repeated pure changes without rounding loss', () => {
  const state = create(333, 1000);
  for (let i = 0; i < 100; i++) {
    expect(state.rebaseMaximum(1500)).toMatchObject({ kind: 'MAX_HP_REBASE', hp: 499, maxHp: 1500 });
    expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 333 });
  }
  expect(state.snapshot()).toMatchObject({ anchorHp: 333, anchorMaxHp: 1000 });
});

it('preserves minimum-one survival but allows real damage to down the actor', () => {
  const state = create(1, 1000);
  expect(state.rebaseMaximum(100)).toMatchObject({ hp: 1 });
  expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 1 });
  state.rebaseMaximum(100);
  state.recordActualHp(0);
  expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 0 });
  expect(state.snapshot()).toMatchObject({ anchorHp: 0, anchorMaxHp: 100 });
});

it('anchors actual damage or healing at the effective maximum', () => {
  const state = create(500, 1000);
  state.rebaseMaximum(2000);
  state.recordActualHp(800);
  expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 400 });
  const healed = create(500, 1000);
  healed.recordActualHp(600);
  expect(healed.rebaseMaximum(2000)).toMatchObject({ hp: 1200 });
});

it('does not reset the anchor for unchanged HP, including zero loss or shield-only absorption', () => {
  const state = create(333, 1000);
  state.rebaseMaximum(1500);
  state.recordActualHp(499);
  expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 333 });
});

it('preserves the approved fixed healing interaction at a lower maximum', () => {
  const state = create(500, 1000);
  state.rebaseMaximum(500);
  state.recordActualHp(350);
  expect(state.rebaseMaximum(1000)).toMatchObject({ hp: 700 });
});

it('keeps HP zero for pure max changes and handles full health', () => {
  expect(create(0, 1000).rebaseMaximum(2000)).toMatchObject({ hp: 0 });
  expect(create(1000, 1000).rebaseMaximum(2000)).toMatchObject({ hp: 2000 });
});

it('restores the exact anchor and returns detached serializable snapshots', () => {
  const state = create(333, 1000);
  state.rebaseMaximum(1500);
  const snapshot = state.snapshot(), copy = JSON.parse(JSON.stringify(snapshot));
  snapshot.anchorHp = 1;
  const restored = HealthRebaseState.restore(copy);
  expect(restored.rebaseMaximum(1000)).toMatchObject({ hp: 333 });
  expect(state.snapshot().anchorHp).toBe(333);
});

it('uses exact multiplication even beyond safe-number product precision', () => {
  const max = Number.MAX_SAFE_INTEGER;
  const state = create(max - 1, max);
  expect(state.rebaseMaximum(max - 2).hp).toBe(max - 3);
  expect(state.rebaseMaximum(max).hp).toBe(max - 1);
});

it('prepares a final maximum on a candidate without exposing intermediate changes to the live owner', () => {
  const live = create(500, 1000), before = live.snapshot();
  const candidate = HealthRebaseState.restore(before);
  candidate.rebaseMaximum(2000);
  expect(live.snapshot()).toEqual(before); // Failed enclosing transaction discards only the candidate.
  const committed = HealthRebaseState.restore(candidate.snapshot());
  expect(committed.snapshot()).toMatchObject({ hp: 1000, maxHp: 2000, anchorHp: 500, anchorMaxHp: 1000 });
  expect(committed.rebaseMaximum(1000).hp).toBe(500);
});

it.each([0, -1, 1.5, Infinity, Number.MAX_SAFE_INTEGER + 1])('rejects invalid maximum %s without mutating state', value => {
  const state = create(500, 1000), before = state.snapshot();
  expect(() => state.rebaseMaximum(value)).toThrow();
  expect(state.snapshot()).toEqual(before);
});

it.each([-1, 1001, 1.5, NaN])('rejects invalid actual HP %s without mutating state', hp => {
  const state = create(500, 1000), before = state.snapshot();
  expect(() => state.recordActualHp(hp)).toThrow();
  expect(state.snapshot()).toEqual(before);
});

it('rejects legacy, inconsistent and unsupported saved states instead of upgrading them', () => {
  const snapshot = create(500, 1000).snapshot();
  for (const invalid of [{ hp: 500, maxHp: 1000 }, { ...snapshot, version: 'legacy-v1' },
    { ...snapshot, hp: 501 }, { ...snapshot, anchorHp: 1001 }, { ...snapshot, anchorMaxHp: 0 }]) {
    expect(() => HealthRebaseState.restore(invalid)).toThrow();
  }
  expect(() => HealthRebaseState.create({ policyVersion: 'legacy-v1', hp: 500, maxHp: 1000 })).toThrow();
});
