import { launchGachaPool, launchGachaPolicyVersion, validateGachaPool } from "../../../packages/balance/src/gacha-policy";

export function isDevGachaScopeAllowed(input: { environment: string; accountId: string; nowMs: number; snapshot: string }): boolean {
  if (input.environment !== "DEV") return false;
  try {
    const pool = JSON.parse(input.snapshot);
    const scope = pool?.devTestScope;
    return scope?.environment === "DEV" && typeof scope.accountId === "string"
      && scope.accountId === input.accountId && Number.isSafeInteger(scope.expiresAtMs)
      && scope.expiresAtMs > input.nowMs && pool.policyVersion === launchGachaPolicyVersion
      && validateGachaPool(pool, "op_vex")
      && JSON.stringify(pool.results) === JSON.stringify(launchGachaPool.results);
  } catch { return false; }
}
