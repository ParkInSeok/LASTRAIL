using NUnit.Framework;
using LastRail.Operators;

namespace LastRail.Tests.EditMode
{
    public sealed class OperatorDownRulesTests
    {
        [Test]
        public void GetRecoveryPlan_ReturnsFirstAndSecondDownRecoveryRules()
        {
            var first = OperatorDownRules.GetRecoveryPlan(downCountThisWave: 1);
            var second = OperatorDownRules.GetRecoveryPlan(downCountThisWave: 2);

            Assert.That(first.State, Is.EqualTo(OperatorDownState.DownRecovering));
            Assert.That(first.RecoveryDelaySeconds, Is.EqualTo(12.0f).Within(0.0001f));
            Assert.That(first.RecoveryHpRatio, Is.EqualTo(0.25f).Within(0.0001f));

            Assert.That(second.State, Is.EqualTo(OperatorDownState.DownRecovering));
            Assert.That(second.RecoveryDelaySeconds, Is.EqualTo(20.0f).Within(0.0001f));
            Assert.That(second.RecoveryHpRatio, Is.EqualTo(0.20f).Within(0.0001f));
        }

        [Test]
        public void GetRecoveryPlan_ReturnsWaveIncapacitatedForThirdAndLaterDown()
        {
            var third = OperatorDownRules.GetRecoveryPlan(downCountThisWave: 3);
            var fourth = OperatorDownRules.GetRecoveryPlan(downCountThisWave: 4);

            Assert.That(third.State, Is.EqualTo(OperatorDownState.WaveIncapacitated));
            Assert.That(third.RecoveryDelaySeconds, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(third.RecoveryHpRatio, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(fourth.State, Is.EqualTo(OperatorDownState.WaveIncapacitated));
        }

        [Test]
        public void GetRecoveryPlan_RejectsNonPositiveDownCount()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => OperatorDownRules.GetRecoveryPlan(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => OperatorDownRules.GetRecoveryPlan(-1));
        }
    }
}
