import { expect, it } from 'vitest';
import { resolveNyxMitigationBudget } from '../../apps/game-api/src/combat-nyx-mitigation';
import { resolveNumericDefenseBudget } from '../../apps/game-api/src/combat-numeric-defense';
const exact = (n: number) => ({ numerator: String(n), denominator: '1' });
const input = (defense = 250, valueBps = 800) => ({ version: 'combat.nyx-mitigation-budget.v1.20261107',
  original: { version: 'combat.numeric-defense-budget.v1', base: exact(1000), defense: exact(defense), effects: [],
    weakening: [] as { sourceId: string; valueBps: number }[], penetration: [] as { sourceId: string; valueBps: number }[],
    reductions: { equipment: [] as { sourceId: string; valueBps: number }[], guardStance: [], independent: [] } },
  mitigationRateReductions: [{ sourceId: 'zone', valueBps }] });
it.each([[250, 800, '880'], [250, 1500, '950'], [53, 800, '1000'], [53, 1500, '1000'], [0, 1500, '1000']])('applies approved mitigation points for DEF%s/%sbps', (defense, bps, damage) => {
  const value = input(defense, bps), original = structuredClone(value);
  expect(resolveNyxMitigationBudget(value).ordinaryBeforeShield).toBe(damage);
  expect(value).toEqual(original);
  expect(resolveNyxMitigationBudget(value).effectiveDefense).toEqual(exact(defense));
});
it('preserves legacy results when absent and applies after weakening/penetration before other reductions', () => {
  const value = input(); value.mitigationRateReductions = [];
  expect(resolveNyxMitigationBudget(value).exact).toEqual(resolveNumericDefenseBudget(value.original).exact);
  expect(resolveNumericDefenseBudget(value.original).ordinaryBeforeShield).toBe('800');
  expect(resolveNumericDefenseBudget(input(53).original).ordinaryBeforeShield).toBe('949');
  value.original.weakening = [{ sourceId: 'weak', valueBps: 2000 }];
  value.original.penetration = [{ sourceId: 'pen', valueBps: 3000 }];
  value.mitigationRateReductions = [{ sourceId: 'zone', valueBps: 800 }];
  expect(resolveNyxMitigationBudget(value).ordinaryBeforeShield).toBe('957');
  value.original.reductions.equipment = [{ sourceId: 'eq', valueBps: 5000 }];
  expect(resolveNyxMitigationBudget(value).ordinaryBeforeShield).toBe('478');
});
it('uses the strongest eligible source once and rejects changed/cross-group identities or unsupported magnitudes', () => {
  const value = input(); value.mitigationRateReductions.push({ sourceId: 'ult', valueBps: 1500 }, { sourceId: 'ult', valueBps: 1500 });
  expect(resolveNyxMitigationBudget(value).ordinaryBeforeShield).toBe('950');
  value.mitigationRateReductions.push({ sourceId: 'ult', valueBps: 800 });
  expect(() => resolveNyxMitigationBudget(value)).toThrow();
  const collision = input(); collision.original.weakening = [{ sourceId: 'zone', valueBps: 800 }];
  expect(() => resolveNyxMitigationBudget(collision)).toThrow();
  expect(() => resolveNyxMitigationBudget(input(250, 1600))).toThrow();
});
