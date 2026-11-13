using NUnit.Framework;
using LastRail.Operators;

namespace LastRail.Tests.EditMode
{
    public sealed class BlockCalculatorTests
    {
        [Test]
        public void BlockCapacityFor_ReturnsKyleThreeAndOtherOperatorsOne()
        {
            Assert.That(BlockCalculator.BlockCapacityFor("operator_kyle"), Is.EqualTo(3));
            Assert.That(BlockCalculator.BlockCapacityFor("operator_reina"), Is.EqualTo(1));
            Assert.That(BlockCalculator.BlockCapacityFor("operator_mira"), Is.EqualTo(1));
        }

        [Test]
        public void TotalBlockWeight_SumsEnemyBlockWeights()
        {
            var enemies = new[]
            {
                new BlockTarget("enemy_normal", 1),
                new BlockTarget("enemy_heavy", 2),
                new BlockTarget("enemy_special", 3)
            };

            Assert.That(BlockCalculator.TotalBlockWeight(enemies), Is.EqualTo(6));
        }

        [Test]
        public void CanBlock_ReturnsWhetherEnemyWeightsFitCapacity()
        {
            var normalAndHeavy = new[]
            {
                new BlockTarget("enemy_normal", 1),
                new BlockTarget("enemy_heavy", 2)
            };
            var overCapacity = new[]
            {
                new BlockTarget("enemy_normal", 1),
                new BlockTarget("enemy_heavy", 2),
                new BlockTarget("enemy_extra", 1)
            };

            Assert.That(BlockCalculator.CanBlock("operator_kyle", normalAndHeavy), Is.EqualTo(true));
            Assert.That(BlockCalculator.CanBlock("operator_kyle", overCapacity), Is.EqualTo(false));
            Assert.That(BlockCalculator.CanBlock("operator_reina", normalAndHeavy), Is.EqualTo(false));
        }

        [Test]
        public void BlockTarget_RejectsInvalidStableIdOrWeight()
        {
            Assert.Throws<System.ArgumentException>(() => new BlockTarget("", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BlockTarget("enemy_normal", 0));
        }
    }
}
