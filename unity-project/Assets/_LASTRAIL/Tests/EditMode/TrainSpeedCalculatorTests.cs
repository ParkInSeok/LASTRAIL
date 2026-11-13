using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainSpeedCalculatorTests
    {
        [Test]
        public void CalculateBaseSpeedIndex_UsesEngineOutputMinusTotalWeightFormula()
        {
            var speed = TrainSpeedCalculator.CalculateBaseSpeedIndex(engineOutput: 65, totalWeight: 60);

            Assert.That(speed, Is.EqualTo(1.05f).Within(0.0001f));
        }

        [Test]
        public void CalculateBaseSpeedIndex_ClampsToReferenceBounds()
        {
            Assert.That(TrainSpeedCalculator.CalculateBaseSpeedIndex(30, 120), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(TrainSpeedCalculator.CalculateBaseSpeedIndex(120, 60), Is.EqualTo(1.20f).Within(0.0001f));
        }

        [Test]
        public void Classify_ReturnsReferenceSpeedState()
        {
            Assert.That(TrainSpeedCalculator.Classify(0.80f), Is.EqualTo(TrainSpeedState.Heavy));
            Assert.That(TrainSpeedCalculator.Classify(0.90f), Is.EqualTo(TrainSpeedState.Slow));
            Assert.That(TrainSpeedCalculator.Classify(1.00f), Is.EqualTo(TrainSpeedState.Standard));
            Assert.That(TrainSpeedCalculator.Classify(1.10f), Is.EqualTo(TrainSpeedState.Fast));
            Assert.That(TrainSpeedCalculator.Classify(1.15f), Is.EqualTo(TrainSpeedState.HighMobility));
        }
    }
}
