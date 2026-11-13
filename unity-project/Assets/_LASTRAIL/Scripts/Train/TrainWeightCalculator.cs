using System;
using System.Collections.Generic;

namespace LastRail.Train
{
    public static class TrainWeightCalculator
    {
        public const int LocomotiveBaseWeight = 30;

        public static int TotalWeight(IReadOnlyList<CarriageType> carriages)
        {
            if (carriages == null)
            {
                throw new ArgumentNullException(nameof(carriages));
            }

            var total = LocomotiveBaseWeight;
            for (var i = 0; i < carriages.Count; i++)
            {
                total += WeightFor(carriages[i]);
            }

            return total;
        }

        public static int WeightFor(CarriageType carriageType)
        {
            switch (carriageType)
            {
                case CarriageType.Medical:
                    return 10;
                case CarriageType.Combat:
                    return 12;
                case CarriageType.Cargo:
                    return 14;
                case CarriageType.Gun:
                    return 18;
                case CarriageType.Defense:
                    return 22;
                default:
                    throw new ArgumentOutOfRangeException(nameof(carriageType), carriageType, "Unsupported carriage type.");
            }
        }
    }
}
