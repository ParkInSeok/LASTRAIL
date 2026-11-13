using NUnit.Framework;
using LastRail.Boarding;

namespace LastRail.Tests.EditMode
{
    public sealed class BoardingApproachCalculatorTests
    {
        [Test]
        public void CalculateFinalApproachTime_MultipliesBaseTimeByCurrentSpeedIndex()
        {
            var finalTime = BoardingApproachCalculator.CalculateFinalApproachTime(
                baseApproachTimeSeconds: 8.0f,
                currentSpeedIndex: 1.10f);

            Assert.That(finalTime, Is.EqualTo(8.8f).Within(0.0001f));
        }

        [Test]
        public void CalculateFinalApproachTime_ClampsSpeedEffectToReferenceRange()
        {
            Assert.That(
                BoardingApproachCalculator.CalculateFinalApproachTime(8.0f, 0.50f),
                Is.EqualTo(6.0f).Within(0.0001f));

            Assert.That(
                BoardingApproachCalculator.CalculateFinalApproachTime(8.0f, 1.50f),
                Is.EqualTo(10.0f).Within(0.0001f));
        }

        [Test]
        public void CalculateFinalApproachTime_RejectsNegativeBaseTime()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                BoardingApproachCalculator.CalculateFinalApproachTime(-1.0f, 1.0f));
        }
    }
}
