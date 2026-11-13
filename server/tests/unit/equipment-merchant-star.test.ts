import { describe, expect, it } from 'vitest';
import { selectEquipmentMerchantStar } from '../../packages/common/src/equipment-merchant-star';

describe('equipment merchant star distribution v15', () => {
  it('uses the approved 1-3 star distribution for every visit depth', () => {
    const count = (depth: number) => {
      const stars = Array.from({ length: 100 }, (_, roll) => selectEquipmentMerchantStar(depth, roll));
      return [1, 2, 3, 4, 5].map(star => stars.filter(value => value === star).length);
    };
    for (const depth of [1, 4]) expect(count(depth)).toEqual([95, 5, 0, 0, 0]);
    for (const depth of [5, 8]) expect(count(depth)).toEqual([40, 45, 15, 0, 0]);
    for (const depth of [9, 11]) expect(count(depth)).toEqual([0, 50, 50, 0, 0]);
  });

  it('uses exact integer roll boundaries without substituting a fourth-star draw', () => {
    expect([39, 40, 84, 85, 99].map(roll => selectEquipmentMerchantStar(5, roll)))
      .toEqual([1, 2, 2, 3, 3]);
    expect([0, 49, 50, 99].map(roll => selectEquipmentMerchantStar(9, roll)))
      .toEqual([2, 2, 3, 3]);
  });

  it('rejects final and out-of-range depths and rolls', () => {
    for (const depth of [0, 12, 1.5, Number.NaN]) {
      expect(() => selectEquipmentMerchantStar(depth, 0)).toThrow('equipment_merchant_star_invalid');
    }
    for (const roll of [-1, 100, 0.5, Number.NaN]) {
      expect(() => selectEquipmentMerchantStar(1, roll)).toThrow('equipment_merchant_star_invalid');
    }
  });
});
