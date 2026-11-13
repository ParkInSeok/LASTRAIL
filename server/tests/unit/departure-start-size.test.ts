import { expect, it } from 'vitest';
import { assertOrdinaryStartResponseSize } from '../../apps/game-api/src/departure-start-size';

it('retains the old boundary and accepts the newly required mapped START range', () => {
  for (const size of [65530, 70607]) expect(() => assertOrdinaryStartResponseSize('a'.repeat(size))).not.toThrow();
});

it('accepts the exact encoded boundary and rejects one character over', () => {
  expect(JSON.stringify(JSON.stringify('a'.repeat(131066))).length).toBe(131072);
  expect(() => assertOrdinaryStartResponseSize('a'.repeat(131066))).not.toThrow();
  expect(() => assertOrdinaryStartResponseSize('a'.repeat(131067))).toThrow('ordinary_start_response_too_large');
});
it('checks UTF-8 and escaped storage independently', () => {
  expect(() => assertOrdinaryStartResponseSize('가'.repeat(44000))).toThrow('ordinary_start_response_too_large');
  const value = '"'.repeat(40000);
  expect(Buffer.byteLength(JSON.stringify(value))).toBeLessThan(131072);
  expect(() => assertOrdinaryStartResponseSize(value)).toThrow('ordinary_start_response_too_large');
});

it.each(['\u0085', '\u2028', '\u2029'])('counts Newtonsoft-only escaped line separators (%j)', separator => {
  const value = separator.repeat(22000);
  expect(Buffer.byteLength(JSON.stringify(value))).toBeLessThan(131072);
  expect(JSON.stringify(JSON.stringify(value)).length).toBeLessThan(131072);
  expect(() => assertOrdinaryStartResponseSize(value)).toThrow('ordinary_start_response_too_large');
});
