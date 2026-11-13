using System;

namespace LastRail.Train
{
    public static class TrainCoreUpgradeCostCatalog
    {
        private static readonly TrainCoreUpgradeCost[] costs =
        {
            new TrainCoreUpgradeCost(1, 2, 200, 100, 0),
            new TrainCoreUpgradeCost(2, 3, 300, 150, 0),
            new TrainCoreUpgradeCost(3, 4, 450, 220, 0),
            new TrainCoreUpgradeCost(4, 5, 650, 320, 0),
            new TrainCoreUpgradeCost(5, 6, 900, 450, 0),
            new TrainCoreUpgradeCost(6, 7, 1200, 600, 0),
            new TrainCoreUpgradeCost(7, 8, 1600, 800, 0),
            new TrainCoreUpgradeCost(8, 9, 2100, 1050, 0),
            new TrainCoreUpgradeCost(9, 10, 2800, 1400, 1)
        };

        public static TrainCoreUpgradeCost[] All => (TrainCoreUpgradeCost[])costs.Clone();

        public static TrainCoreUpgradeCost ForUpgrade(int fromLevel, int toLevel)
        {
            if (fromLevel < 1 || fromLevel > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(fromLevel), fromLevel, "Train core upgrade source level must be between 1 and 9.");
            }

            if (toLevel < 2 || toLevel > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(toLevel), toLevel, "Train core upgrade target level must be between 2 and 10.");
            }

            for (var i = 0; i < costs.Length; i++)
            {
                if (costs[i].FromLevel == fromLevel && costs[i].ToLevel == toLevel)
                {
                    return costs[i];
                }
            }

            throw new InvalidOperationException($"Unsupported train core upgrade path: {fromLevel} -> {toLevel}");
        }

        public static TrainCoreUpgradeCost TotalCostToMax()
        {
            var credits = 0;
            var machineParts = 0;
            var specialCores = 0;

            for (var i = 0; i < costs.Length; i++)
            {
                credits += costs[i].Credits;
                machineParts += costs[i].MachineParts;
                specialCores += costs[i].SpecialCores;
            }

            return new TrainCoreUpgradeCost(1, 10, credits, machineParts, specialCores);
        }
    }
}
