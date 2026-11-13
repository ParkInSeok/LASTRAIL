using System;
using System.Collections.Generic;

namespace LastRail.Train
{
    public static class CarriageLoadoutRules
    {
        public static CarriageLoadoutValidationResult Validate(
            IReadOnlyList<CarriageType> loadout,
            int unlockedSlotCount)
        {
            if (loadout == null)
            {
                throw new ArgumentNullException(nameof(loadout));
            }

            if (unlockedSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockedSlotCount), unlockedSlotCount, "Unlocked slot count must be positive.");
            }

            if (loadout.Count != unlockedSlotCount)
            {
                return CarriageLoadoutValidationResult.Invalid("Unlocked carriage slots must all be filled exactly once.");
            }

            var combatCount = 0;
            var gunCount = 0;
            var medicalCount = 0;
            var defenseCount = 0;
            var cargoCount = 0;

            for (var i = 0; i < loadout.Count; i++)
            {
                switch (loadout[i])
                {
                    case CarriageType.Combat:
                        combatCount++;
                        break;
                    case CarriageType.Gun:
                        gunCount++;
                        break;
                    case CarriageType.Medical:
                        medicalCount++;
                        break;
                    case CarriageType.Defense:
                        defenseCount++;
                        break;
                    case CarriageType.Cargo:
                        cargoCount++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(loadout), loadout[i], "Unsupported carriage type.");
                }
            }

            if (combatCount != 1)
            {
                return CarriageLoadoutValidationResult.Invalid("Loadout must include exactly one Combat Car.");
            }

            if (gunCount > MaxCountFor(CarriageType.Gun))
            {
                return CarriageLoadoutValidationResult.Invalid("Loadout exceeds Gun Car maximum count.");
            }

            if (medicalCount > MaxCountFor(CarriageType.Medical))
            {
                return CarriageLoadoutValidationResult.Invalid("Loadout exceeds Medical Car maximum count.");
            }

            if (defenseCount > MaxCountFor(CarriageType.Defense))
            {
                return CarriageLoadoutValidationResult.Invalid("Loadout exceeds Defense Car maximum count.");
            }

            if (cargoCount > MaxCountFor(CarriageType.Cargo))
            {
                return CarriageLoadoutValidationResult.Invalid("Loadout exceeds Cargo Car maximum count.");
            }

            return CarriageLoadoutValidationResult.Valid();
        }

        public static int MaxCountFor(CarriageType carriageType)
        {
            switch (carriageType)
            {
                case CarriageType.Combat:
                    return 1;
                case CarriageType.Gun:
                case CarriageType.Medical:
                case CarriageType.Defense:
                    return 2;
                case CarriageType.Cargo:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(carriageType), carriageType, "Unsupported carriage type.");
            }
        }
    }
}
