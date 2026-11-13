import { expect, it } from 'vitest';
import { replayOrdinaryGunnerHealth } from '../../apps/game-api/src/ordinary-gunner-health-replay';
const original = { gunnerIds: ['a', 'b'], hp: { numerator: '201', denominator: '2' } };
const hit = (eventId: string, targetId = 'a', atMs = 1) => ({ eventId, targetId, atMs,
  protected: false, shieldRemaining: 0, numeric: { version: 'combat.numeric-defense-budget.v1',
    base: { numerator: '60', denominator: '1' }, defense: { numerator: '0', denominator: '1' },
    effects: [], weakening: [], penetration: [], reductions: { equipment: [], guardStance: [], independent: [] } } });
it('executes original gunner HP, clamps overkill and issues one defeat per target', () => {
  const result = replayOrdinaryGunnerHealth(original, [hit('one'), hit('two', 'a', 2), hit('late', 'a', 3)]);
  expect(result.targets).toEqual([{ targetId: 'a', hp: 0, maxHp: 100 }, { targetId: 'b', hp: 100, maxHp: 100 }]);
  expect(result.receipts.map(r => r.hpLoss)).toEqual([60, 40, 0]);
  expect(result.defeats).toEqual([{ gunnerId: 'a', eventId: 'two', atMs: 2 }]);
  expect(replayOrdinaryGunnerHealth(original, [hit('one')]).targets[0]!.hp).toBe(40);
});
it('does not convert immunity into loss, and rejects unsupported or ambiguous events', () => {
  expect(replayOrdinaryGunnerHealth(original, [{ ...hit('immune'), protected: true }]).receipts[0]!.hpLoss).toBe(0);
  for (const events of [[hit('same'), hit('same')], [hit('later', 'a', 2), hit('earlier')],
    [hit('foreign', 'train')], [{ ...hit('shield'), shieldRemaining: 1 }]]) {
    expect(() => replayOrdinaryGunnerHealth(original, events)).toThrow();
  }
  expect(() => replayOrdinaryGunnerHealth({ ...original, gunnerIds: ['a', 'a'] }, [])).toThrow();
});
it('reuses exact defense arithmetic and rejects values outside the original runtime range', () => {
  const event = hit('defended');
  event.numeric.defense.numerator = '1000';
  expect(replayOrdinaryGunnerHealth(original, [event]).receipts[0]!.hpLoss).toBe(30);
  event.numeric.base.numerator = '4294967296';
  expect(() => replayOrdinaryGunnerHealth(original, [event])).toThrow('ordinary_gunner_damage_range');
  expect(() => replayOrdinaryGunnerHealth({ ...original, hp: { numerator: '1', denominator: '2' } }, [])).toThrow();
});
