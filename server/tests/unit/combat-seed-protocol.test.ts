import { describe, expect, it, vi } from "vitest";
import { issueLocalCombatSeed, localCombatRandom, restoreLocalCombatRandom } from "../../apps/game-api/src/combat-seed-protocol";
describe("isolated CE03 simulation protocol candidate", () => {
  it("matches existing Unity xorshift32 vectors and preserves restart state", () => {
    const rng = localCombatRandom(1);
    expect([rng.nextUInt(), rng.nextUInt(), rng.nextUInt()]).toEqual([270369, 67634689, 2647435461]);
    const restored = restoreLocalCombatRandom(rng.state()); expect(restored.nextUInt()).toBe(rng.nextUInt());
    expect(() => restoreLocalCombatRandom(0)).toThrow();
    expect(localCombatRandom(0).nextUInt()).toBe(localCombatRandom(0x6D2B79F5).nextUInt());
    expect(localCombatRandom(1).range(10, 20)).toBe(19);
    for (const value of [-1, 4294967296, 0.5]) expect(() => localCombatRandom(value)).toThrow();
    expect(() => rng.range(-2147483648, 2147483647)).toThrow();
  });
  it("draws uint32 from Web Crypto independently of run IDs and records protocol purpose", () => {
    const spy = vi.spyOn(crypto, "getRandomValues");
    try {
      const issued = issueLocalCombatSeed(100);
      expect(spy).toHaveBeenCalledOnce();
      expect(issued).toMatchObject({ generation: "WEB_CRYPTO_UINT32", simulation: "XORSHIFT32_13_17_5_V1", issuedAt: 100, lifetime: "ORIGINAL_RUN", purpose: "LOCAL_COMBAT_SIMULATION" });
      expect(Number.isInteger(issued.value)).toBe(true); expect(issued.value).toBeGreaterThanOrEqual(0); expect(issued.value).toBeLessThanOrEqual(4294967295);
    } finally { spy.mockRestore(); }
  });
  it("decodes exactly four little-endian bytes without int32 truncation", () => {
    const spy = vi.spyOn(crypto, "getRandomValues");
    try {
      for (const [bytes, expected] of [[[0, 0, 0, 0], 0], [[0, 0, 0, 128], 2147483648], [[255, 255, 255, 255], 4294967295]] as const) {
        spy.mockImplementation(buffer => { if (!(buffer instanceof Uint8Array) || buffer.length !== 4) throw new Error("expected4bytes"); buffer.set(bytes); return buffer; });
        const seed = issueLocalCombatSeed(123); expect(seed.value).toBe(expected); expect(seed.byteOrder).toBe("LITTLE_ENDIAN");
      }
    } finally { spy.mockRestore(); }
  });
});
