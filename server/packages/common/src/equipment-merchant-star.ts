/** Approved v15 merchant-only 1-3 star weights by fixed visit depth. The caller owns RNG and offer persistence. */
const MERCHANT_STAR_WEIGHTS = {
  early: [95, 5, 0],
  middle: [40, 45, 15],
  late: [0, 50, 50],
} as const;

export function selectEquipmentMerchantStar(depth: number, roll: number): 1 | 2 | 3 {
  if (!Number.isInteger(depth) || depth < 1 || depth > 11
    || !Number.isInteger(roll) || roll < 0 || roll >= 100) {
    throw new Error('equipment_merchant_star_invalid');
  }
  const weights = depth <= 4 ? MERCHANT_STAR_WEIGHTS.early
    : depth <= 8 ? MERCHANT_STAR_WEIGHTS.middle : MERCHANT_STAR_WEIGHTS.late;
  if (roll < weights[0]) return 1;
  if (roll < weights[0] + weights[1]) return 2;
  return 3;
}
