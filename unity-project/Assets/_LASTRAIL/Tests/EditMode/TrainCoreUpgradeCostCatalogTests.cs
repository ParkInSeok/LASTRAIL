using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainCoreUpgradeCostCatalogTests
    {
        [Test]
        public void All_ReturnsLevelOneToTenUpgradeCosts()
        {
            var costs = TrainCoreUpgradeCostCatalog.All;

            Assert.That(costs.Length, Is.EqualTo(9));
            AssertCost(costs[0], 1, 2, 200, 100, 0);
            AssertCost(costs[1], 2, 3, 300, 150, 0);
            AssertCost(costs[2], 3, 4, 450, 220, 0);
            AssertCost(costs[3], 4, 5, 650, 320, 0);
            AssertCost(costs[4], 5, 6, 900, 450, 0);
            AssertCost(costs[5], 6, 7, 1200, 600, 0);
            AssertCost(costs[6], 7, 8, 1600, 800, 0);
            AssertCost(costs[7], 8, 9, 2100, 1050, 0);
            AssertCost(costs[8], 9, 10, 2800, 1400, 1);
        }

        [Test]
        public void ForUpgrade_ReturnsRequestedUpgradeCost()
        {
            var cost = TrainCoreUpgradeCostCatalog.ForUpgrade(8, 9);

            AssertCost(cost, 8, 9, 2100, 1050, 0);
        }

        [Test]
        public void TotalCostToMax_ReturnsReferenceTotals()
        {
            var total = TrainCoreUpgradeCostCatalog.TotalCostToMax();

            Assert.That(total.Credits, Is.EqualTo(10200));
            Assert.That(total.MachineParts, Is.EqualTo(5090));
            Assert.That(total.SpecialCores, Is.EqualTo(1));
        }

        [Test]
        public void ForUpgrade_RejectsUnsupportedUpgradePath()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TrainCoreUpgradeCostCatalog.ForUpgrade(0, 1));
            Assert.Throws<System.InvalidOperationException>(() => TrainCoreUpgradeCostCatalog.ForUpgrade(1, 3));
        }

        private static void AssertCost(
            TrainCoreUpgradeCost cost,
            int fromLevel,
            int toLevel,
            int credits,
            int machineParts,
            int specialCores)
        {
            Assert.That(cost.FromLevel, Is.EqualTo(fromLevel));
            Assert.That(cost.ToLevel, Is.EqualTo(toLevel));
            Assert.That(cost.Credits, Is.EqualTo(credits));
            Assert.That(cost.MachineParts, Is.EqualTo(machineParts));
            Assert.That(cost.SpecialCores, Is.EqualTo(specialCores));
        }
    }
}
