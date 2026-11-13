// Isolated CE03 local-protocol candidate. Not registered in public start/catalog.
export function issueLocalCombatSeed(now: number) {
  if (!Number.isSafeInteger(now) || now < 0) throw new Error("invalid seed time");
  const bytes = crypto.getRandomValues(new Uint8Array(4));
  return { version: "combat-seed-local-v1" as const, value: new DataView(bytes.buffer, bytes.byteOffset, 4).getUint32(0, true), byteOrder: "LITTLE_ENDIAN" as const,
    generation: "WEB_CRYPTO_UINT32" as const, simulation: "XORSHIFT32_13_17_5_V1" as const,
    purpose: "LOCAL_COMBAT_SIMULATION" as const, issuedAt: now, lifetime: "ORIGINAL_RUN" as const, zeroNormalization: 0x6D2B79F5 };
}
export function localCombatRandom(seed: number) {
  if (!Number.isInteger(seed) || seed < 0 || seed > 0xFFFFFFFF) throw new Error("invalid uint32 seed");
  let state = seed === 0 ? 0x6D2B79F5 : seed;
  const nextUInt = () => { let x = state; x ^= x << 13; x ^= x >>> 17; x ^= x << 5; state = x >>> 0; return state; };
  return { nextUInt, state: () => state, range(min: number, max: number) {
    if (!Number.isInteger(min) || !Number.isInteger(max) || min < -2147483648 || max > 2147483647 || max <= min || max - min > 2147483647) throw new Error("invalid range");
    return min + nextUInt() % (max - min);
  } };
}
export function restoreLocalCombatRandom(state: number) {
  if (!Number.isInteger(state) || state <= 0 || state > 0xFFFFFFFF) throw new Error("invalid checkpoint uint32 state");
  return localCombatRandom(state);
}
