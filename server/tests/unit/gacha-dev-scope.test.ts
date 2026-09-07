import { describe, expect, it } from "vitest";
import { isDevGachaScopeAllowed } from "../../apps/game-api/src/gacha-dev-scope";
import { launchGachaPool, launchGachaPolicyVersion } from "../../packages/balance/src/gacha-policy";
const input = { environment: "DEV", accountId: "synthetic-account", nowMs: 1000,
  snapshot: JSON.stringify({ ...launchGachaPool, policyVersion: launchGachaPolicyVersion, devTestScope: { environment: "DEV", accountId: "synthetic-account", expiresAtMs: 2000 } }) };
describe("DEV dedicated banner scope", () => {
  it("allows only the authenticated target and approved exact pool in DEV", () => {
    expect(isDevGachaScopeAllowed(input)).toBe(true);
    expect(isDevGachaScopeAllowed({ ...input, accountId: "another-account" })).toBe(false);
    expect(isDevGachaScopeAllowed({ ...input, environment: "PRD" })).toBe(false);
    expect(isDevGachaScopeAllowed({ ...input, nowMs: 2000 })).toBe(false);
  });
  it("fails closed on missing scope, malformed data and a changed pool", () => {
    for (const snapshot of ["{", "null", JSON.stringify(launchGachaPool), input.snapshot.replace('"weight":', '"weight":0,"ignored":')]) {
      expect(isDevGachaScopeAllowed({ ...input, snapshot })).toBe(false);
    }
  });
});
