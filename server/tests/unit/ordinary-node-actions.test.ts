import { expect, it } from 'vitest';
import { cooldownTimeSpanToNodeSteps, cooldownNodeStepsToTimeSpan } from '../../apps/game-api/src/ordinary-node-actions';

it.each([[0,0],[1,1],[499999,1],[500000,1],[500001,2],[40000000,80]])(
  'converts TimeSpan remainder %s into original 50ms logical steps %s', (duration, steps) => {
    expect(cooldownTimeSpanToNodeSteps(duration,500000)).toBe(steps);
    const restored = cooldownNodeStepsToTimeSpan(steps,500000);
    expect(restored).toBeGreaterThanOrEqual(duration);
    expect(restored-duration).toBeLessThan(500000);
    expect(cooldownTimeSpanToNodeSteps(restored,500000)).toBe(steps);
  });
it('rejects invalid clock/overflow and rounds large integers without floating-point addition', () => {
  for (const step of [0,1,-10000,10000.5]) expect(() => cooldownTimeSpanToNodeSteps(1,step)).toThrow();
  expect(() => cooldownTimeSpanToNodeSteps(-1,500000)).toThrow();
  expect(() => cooldownNodeStepsToTimeSpan(Number.MAX_SAFE_INTEGER,500000)).toThrow();
  expect(cooldownTimeSpanToNodeSteps(Number.MAX_SAFE_INTEGER,10000)).toBe(900719925475);
});
