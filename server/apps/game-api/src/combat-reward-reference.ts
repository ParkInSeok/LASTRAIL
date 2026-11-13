import { canonicalJson, sha256Hex } from "@lastrail/common";
import { parseRunRewardsConfig, type RunRiskTier } from "../../../packages/balance/src/index";
import { ActivityError } from "./activity-policy";
export async function combatRewardReference(configJson: string, balanceVersion: number, operationId: string, risk: string) {
  try {
    const config = parseRunRewardsConfig(JSON.parse(configJson).runRewards);
    if (!Number.isSafeInteger(balanceVersion) || balanceVersion < 1 || !Object.hasOwn(config.operations, operationId)
      || !Object.hasOwn(config.riskMultipliersBps, risk)) throw new Error("source missing");
    return { version: "run-reward-ref-v1" as const, balanceVersion: String(balanceVersion), operationId, risk: risk as RunRiskTier,
      scope: "BASE_RUN_REWARDS" as const, hashAlgorithm: "SHA-256" as const, canonicalization: "canonical-json-v1" as const,
      configSha256: await sha256Hex(canonicalJson(config)) };
  } catch { throw new ActivityError(503, "combat_reward_source_invalid"); }
}
export async function validateCombatRewardReference(reference: unknown, configJson: string, balanceVersion: number, operationId: string, risk: string) {
  const expected = await combatRewardReference(configJson, balanceVersion, operationId, risk);
  if (JSON.stringify(reference) !== JSON.stringify(expected)) {
    // Key order is not semantic; strict canonical equality also rejects extra fields.
    if (!reference || typeof reference !== "object" || canonicalJson(reference as Parameters<typeof canonicalJson>[0]) !== canonicalJson(expected)) throw new ActivityError(409, "combat_reward_reference_changed");
  }
  return expected;
}
