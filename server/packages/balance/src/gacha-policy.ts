export type GachaPolicyResult = {
  resultId: string;
  resultRarity: "ELITE" | "RARE" | "COMMON";
  resultType: "ELITE_OPERATOR" | "RARE_OPERATOR" | "ELITE_OPERATOR_SHARD" | "RARE_OPERATOR_SHARD" | "OPERATOR_TOKEN" | "TRAINING_MATERIAL" | "WORK_SKILL_MATERIAL" | "RARE_OPERATOR_TOKEN" | "TACTICAL_MATERIAL";
  weight: number;
  quantity?: number;
  isPickup?: boolean;
};
export type GachaPolicyPool = { results: GachaPolicyResult[] };

const gcd = (a: bigint, b: bigint): bigint => b === 0n ? a : gcd(b, a % b);
const validId = (id: unknown): id is string => typeof id === "string" && /^[a-z][a-z0-9_]{1,127}$/u.test(id);

export function validateGachaPool(pool: GachaPolicyPool, pickupOperatorId: string | null): boolean {
  const rows = pool.results;
  if (!Array.isArray(rows) || rows.length === 0 || new Set(rows.map((r) => r.resultId)).size !== rows.length) return false;
  if (rows.some((r) => !validId(r.resultId) || !Number.isSafeInteger(r.weight) || r.weight <= 0)) return false;
  const total = rows.reduce((sum, r) => sum + r.weight, 0);
  if (!Number.isSafeInteger(total) || total > 2 ** 32) return false;
  const elites = rows.filter((r) => r.resultRarity === "ELITE");
  if (!elites.length || elites.some((r) => r.resultType !== "ELITE_OPERATOR")) return false;
  if (elites.reduce((sum, r) => sum + r.weight, 0) * 25 !== total) return false;
  if (pickupOperatorId !== null && !elites.some((r) => r.resultId === pickupOperatorId)) return false;
  const other = elites.find((r) => r.resultId !== pickupOperatorId);
  if (other && elites.some((r) => r.weight !== other.weight * (r.resultId === pickupOperatorId ? 2 : 1))) return false;
  return rows.every((r) => Boolean(r.isPickup) === (r.resultId === pickupOperatorId));
}

export function buildGachaPool(input: {
  eliteOperatorIds: string[];
  pickupOperatorId: string | null;
  nonEliteResults: GachaPolicyResult[];
}): GachaPolicyPool {
  const ids = input.eliteOperatorIds;
  const extras = input.nonEliteResults;
  if (!Array.isArray(ids) || !ids.length || ids.some((id) => !validId(id)) || new Set(ids).size !== ids.length) throw new Error("invalid_elite_pool");
  if (input.pickupOperatorId !== null && (typeof input.pickupOperatorId !== "string" || !ids.includes(input.pickupOperatorId))) throw new Error("invalid_pickup");
  if (!Array.isArray(extras) || !extras.length || extras.some((r) => !validId(r.resultId) || r.resultRarity === "ELITE"
    || (r.resultRarity !== "RARE" && r.resultRarity !== "COMMON") || r.isPickup === true
    || !Number.isSafeInteger(r.weight) || r.weight <= 0
    || (r.quantity !== undefined && (!Number.isSafeInteger(r.quantity) || r.quantity <= 0)))) throw new Error("invalid_non_elite_pool");
  if (new Set([...ids, ...extras.map((r) => r.resultId)]).size !== ids.length + extras.length) throw new Error("duplicate_candidate");
  const n = BigInt(ids.length);
  const nonEliteTotal = extras.reduce((sum, r) => sum + BigInt(r.weight), 0n);
  const eliteMasses = ids.map((id) => input.pickupOperatorId === null ? nonEliteTotal * (n + 1n) : nonEliteTotal * n * (id === input.pickupOperatorId ? 2n : 1n));
  const extraMasses = extras.map((r) => BigInt(r.weight) * 24n * n * (n + 1n));
  const masses = [...eliteMasses, ...extraMasses];
  const divisor = masses.reduce(gcd);
  const normalized = masses.map((mass) => mass / divisor);
  if (normalized.reduce((sum, mass) => sum + mass, 0n) > 2n ** 32n) throw new Error("pool_integer_domain_too_large");
  const results: GachaPolicyResult[] = ids.map((id, index) => ({ resultId: id, resultRarity: "ELITE", resultType: "ELITE_OPERATOR", quantity: 1, weight: Number(normalized[index]!), isPickup: id === input.pickupOperatorId }));
  results.push(...extras.map((r, index) => ({ ...r, weight: Number(normalized[ids.length + index]!), isPickup: false })));
  const pool = { results };
  if (!validateGachaPool(pool, input.pickupOperatorId)) throw new Error("invalid_generated_pool");
  return pool;
}

export const launchEliteOperatorIds = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris", "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden"];
// Explicit user-approved non-elite allocation. Internal relative mass is not a
// public response field. Fixed elite mass is generated separately above.
const launchNonEliteResults: GachaPolicyResult[] = [
  { resultId: "op_signal_runner", resultRarity: "RARE", resultType: "RARE_OPERATOR", quantity: 1, weight: 500 },
  { resultId: "cur_operator_token", resultRarity: "RARE", resultType: "RARE_OPERATOR_TOKEN", quantity: 10, weight: 800 },
  { resultId: "mat_rare_manual", resultRarity: "RARE", resultType: "TRAINING_MATERIAL", quantity: 1, weight: 1200 },
  { resultId: "mat_scrap", resultRarity: "COMMON", resultType: "TRAINING_MATERIAL", quantity: 1, weight: 4200 },
  { resultId: "mat_work_skill_basic", resultRarity: "COMMON", resultType: "WORK_SKILL_MATERIAL", quantity: 1, weight: 2500 },
  { resultId: "mat_tactical_data_pack", resultRarity: "COMMON", resultType: "TACTICAL_MATERIAL", quantity: 1, weight: 1100 },
];
export const launchGachaPolicyVersion = "gacha-act1-20260907-elite15-v1";
export const launchGachaPool = buildGachaPool({ eliteOperatorIds: launchEliteOperatorIds, pickupOperatorId: "op_vex", nonEliteResults: launchNonEliteResults });
