/** Approved equipment merchant price v1. The caller owns stock, wallet and purchase receipts. */
export interface EquipmentMerchantPriceInput {
  readonly star: number;
  readonly do03Rank: number;
  readonly x08Rank: number;
  /** Successful positive-price equipment purchases at this merchant node. */
  readonly priorPaidPurchases: number;
}

export interface EquipmentMerchantPrice {
  readonly basePrice: number;
  readonly discountPercent: number;
  readonly price: number;
  readonly x08Applied: boolean;
}

const BASE_PRICES = [8, 16, 32] as const;

export function calculateEquipmentMerchantPrice(input: EquipmentMerchantPriceInput): EquipmentMerchantPrice {
  if (!Number.isInteger(input.star) || input.star < 1 || input.star > 3
    || !Number.isInteger(input.do03Rank) || input.do03Rank < 0 || input.do03Rank > 4
    || !Number.isInteger(input.x08Rank) || input.x08Rank < 0 || input.x08Rank > 2
    || !Number.isSafeInteger(input.priorPaidPurchases) || input.priorPaidPurchases < 0) {
    throw new Error('equipment_merchant_price_invalid');
  }

  const basePrice = BASE_PRICES[input.star - 1]!;
  const x08Applied = input.priorPaidPurchases < input.x08Rank;
  const discountPercent = input.do03Rank * 2 + (x08Applied ? 5 : 0);
  return {
    basePrice,
    discountPercent,
    price: Math.max(1, Math.ceil(basePrice * (100 - discountPercent) / 100)),
    x08Applied,
  };
}
