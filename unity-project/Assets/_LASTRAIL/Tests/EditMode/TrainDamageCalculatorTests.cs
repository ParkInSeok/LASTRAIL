using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainDamageCalculatorTests
    {
        [Test]
        public void CalculateTransferredDamageToTrain_ReturnsThirtyPercentOfCarriageFinalDamage()
        {
            var transferredDamage = TrainDamageCalculator.CalculateTransferredDamageToTrain(
                carriageFinalDamage: 1000.0f,
                trainArmorDamageReductionRatio: 0.0f);

            Assert.That(transferredDamage, Is.EqualTo(300.0f).Within(0.0001f));
        }

        [Test]
        public void CalculateTransferredDamageToTrain_DoesNotApplyTrainArmorAgain()
        {
            var withoutTrainArmor = TrainDamageCalculator.CalculateTransferredDamageToTrain(1000.0f, 0.0f);
            var withTrainArmor = TrainDamageCalculator.CalculateTransferredDamageToTrain(1000.0f, 0.50f);

            Assert.That(withTrainArmor, Is.EqualTo(withoutTrainArmor).Within(0.0001f));
            Assert.That(withTrainArmor, Is.EqualTo(300.0f).Within(0.0001f));
        }

        [Test]
        public void ApplyTransferredDamageToTrainHp_ClampsAtZero()
        {
            var remainingHp = TrainDamageCalculator.ApplyTransferredDamageToTrainHp(
                trainTotalHp: 200.0f,
                carriageFinalDamage: 1000.0f,
                trainArmorDamageReductionRatio: 0.0f);

            Assert.That(remainingHp, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void CalculateTransferredDamageToTrain_RejectsNegativeDamageOrArmorRatio()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TrainDamageCalculator.CalculateTransferredDamageToTrain(-1.0f, 0.0f));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TrainDamageCalculator.CalculateTransferredDamageToTrain(100.0f, -0.1f));
        }
    }
}
