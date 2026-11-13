using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class PursuitPressureCalculatorTests
    {
        [Test]
        public void CalculatePressureMultiplier_ReturnsReferenceMultiplierForSpeedBands()
        {
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(0.75f), Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(0.85f), Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.00f), Is.EqualTo(1.00f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.10f), Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.20f), Is.EqualTo(0.70f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.50f), Is.EqualTo(0.70f).Within(0.0001f));
        }

        [Test]
        public void CalculatePressureMultiplier_UsesNearestLowerReferenceBand()
        {
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(0.84f), Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(0.99f), Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.09f), Is.EqualTo(1.00f).Within(0.0001f));
            Assert.That(PursuitPressureCalculator.CalculatePressureMultiplier(1.19f), Is.EqualTo(0.85f).Within(0.0001f));
        }

        [Test]
        public void CalculatePressureMultiplier_RejectsNegativeSpeedIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PursuitPressureCalculator.CalculatePressureMultiplier(-0.1f));
        }
    }
}
