using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainWeightCalculatorTests
    {
        [Test]
        public void TotalWeight_AddsLocomotiveBaseAndCarriageWeights()
        {
            var carriages = new[]
            {
                CarriageType.Combat,
                CarriageType.Gun,
                CarriageType.Medical,
                CarriageType.Defense,
                CarriageType.Cargo
            };

            var totalWeight = TrainWeightCalculator.TotalWeight(carriages);

            Assert.That(totalWeight, Is.EqualTo(106));
        }

        [Test]
        public void WeightFor_ReturnsReferenceWeightByCarriageType()
        {
            Assert.That(TrainWeightCalculator.WeightFor(CarriageType.Medical), Is.EqualTo(10));
            Assert.That(TrainWeightCalculator.WeightFor(CarriageType.Combat), Is.EqualTo(12));
            Assert.That(TrainWeightCalculator.WeightFor(CarriageType.Cargo), Is.EqualTo(14));
            Assert.That(TrainWeightCalculator.WeightFor(CarriageType.Gun), Is.EqualTo(18));
            Assert.That(TrainWeightCalculator.WeightFor(CarriageType.Defense), Is.EqualTo(22));
        }
    }
}
