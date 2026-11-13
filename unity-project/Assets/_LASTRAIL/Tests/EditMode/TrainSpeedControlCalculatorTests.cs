using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainSpeedControlCalculatorTests
    {
        [Test]
        public void CalculateCurrentSpeedIndex_AppliesCruiseAndLowMultipliers()
        {
            Assert.That(
                TrainSpeedControlCalculator.CalculateCurrentSpeedIndex(1.10f, TrainSpeedControlMode.Cruise, emergencyAccelerationActive: false),
                Is.EqualTo(1.10f).Within(0.0001f));

            Assert.That(
                TrainSpeedControlCalculator.CalculateCurrentSpeedIndex(1.10f, TrainSpeedControlMode.Low, emergencyAccelerationActive: false),
                Is.EqualTo(0.77f).Within(0.0001f));
        }

        [Test]
        public void CalculateCurrentSpeedIndex_AppliesEmergencyAccelerationAfterControlMode()
        {
            Assert.That(
                TrainSpeedControlCalculator.CalculateCurrentSpeedIndex(1.00f, TrainSpeedControlMode.Cruise, emergencyAccelerationActive: true),
                Is.EqualTo(1.35f).Within(0.0001f));

            Assert.That(
                TrainSpeedControlCalculator.CalculateCurrentSpeedIndex(1.00f, TrainSpeedControlMode.Low, emergencyAccelerationActive: true),
                Is.EqualTo(0.945f).Within(0.0001f));
        }

        [Test]
        public void CalculateCurrentSpeedIndex_ClampsTemporaryCurrentSpeedToHardCap()
        {
            var currentSpeed = TrainSpeedControlCalculator.CalculateCurrentSpeedIndex(
                1.20f,
                TrainSpeedControlMode.Cruise,
                emergencyAccelerationActive: true);

            Assert.That(currentSpeed, Is.EqualTo(1.50f).Within(0.0001f));
        }
    }
}
