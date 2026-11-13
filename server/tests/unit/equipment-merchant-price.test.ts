import { describe, expect, it } from 'vitest';
import { calculateEquipmentMerchantPrice } from '../../packages/common/src/equipment-merchant-price';

describe('equipment merchant price v1', () => {
  it('prices only the approved 1-3 star goods', () => {
    expect([1, 2, 3].map(star => calculateEquipmentMerchantPrice({ star, do03Rank: 0, x08Rank: 0, priorPaidPurchases: 0 }).price))
      .toEqual([8, 16, 32]);
    for (const star of [0, 4, 5, 1.5]) {
      expect(() => calculateEquipmentMerchantPrice({ star, do03Rank: 0, x08Rank: 0, priorPaidPurchases: 0 }))
        .toThrow('equipment_merchant_price_invalid');
    }
  });

  it('adds DO03 and first-purchase X08 discounts, then rounds up', () => {
    expect([1, 2, 3].map(star => calculateEquipmentMerchantPrice({ star, do03Rank: 4, x08Rank: 2, priorPaidPurchases: 0 }).price))
      .toEqual([7, 14, 28]);
    expect(calculateEquipmentMerchantPrice({ star: 3, do03Rank: 4, x08Rank: 2, priorPaidPurchases: 1 }).price).toBe(28);
    expect(calculateEquipmentMerchantPrice({ star: 3, do03Rank: 4, x08Rank: 2, priorPaidPurchases: 2 }).price).toBe(30);
    expect(calculateEquipmentMerchantPrice({ star: 2, do03Rank: 0, x08Rank: 1, priorPaidPurchases: 1 }).price).toBe(16);
  });

  it('rejects unsupported ranks and counters instead of silently changing a price', () => {
    for (const override of [{ do03Rank: 5 }, { x08Rank: 3 }, { priorPaidPurchases: -1 }, { priorPaidPurchases: 0.5 }]) {
      expect(() => calculateEquipmentMerchantPrice({ star: 1, do03Rank: 0, x08Rank: 0, priorPaidPurchases: 0, ...override }))
        .toThrow('equipment_merchant_price_invalid');
    }
  });
});
