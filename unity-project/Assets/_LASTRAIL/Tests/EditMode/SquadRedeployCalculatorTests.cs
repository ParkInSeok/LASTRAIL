using NUnit.Framework;
using LastRail.Squad;

namespace LastRail.Tests.EditMode
{
    public sealed class SquadRedeployCalculatorTests
    {
        [Test]
        public void CalculateRedeployTime_ReturnsZeroWhenAlreadyAtTargetCarriage()
        {
            var time = SquadRedeployCalculator.CalculateRedeployTime(
                fromSlotIndex: 2,
                toSlotIndex: 2,
                startsFromCombatCar: true,
                combatCarDisabled: true);

            Assert.That(time, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void CalculateRedeployTime_UsesOnePointFiveSecondsPerAdjacentCarriage()
        {
            Assert.That(
                SquadRedeployCalculator.CalculateRedeployTime(0, 1, false, false),
                Is.EqualTo(1.5f).Within(0.0001f));

            Assert.That(
                SquadRedeployCalculator.CalculateRedeployTime(0, 3, false, false),
                Is.EqualTo(4.5f).Within(0.0001f));
        }

        [Test]
        public void CalculateRedeployTime_AppliesCombatCarFirstSegmentBonusWhenDepartingCombatCar()
        {
            Assert.That(
                SquadRedeployCalculator.CalculateRedeployTime(0, 1, true, false),
                Is.EqualTo(1.125f).Within(0.0001f));

            Assert.That(
                SquadRedeployCalculator.CalculateRedeployTime(0, 3, true, false),
                Is.EqualTo(4.125f).Within(0.0001f));
        }

        [Test]
        public void CalculateRedeployTime_AppliesDisabledCombatCarMultiplierToTotalTime()
        {
            var time = SquadRedeployCalculator.CalculateRedeployTime(
                fromSlotIndex: 0,
                toSlotIndex: 3,
                startsFromCombatCar: true,
                combatCarDisabled: true);

            Assert.That(time, Is.EqualTo(5.15625f).Within(0.0001f));
        }

        [Test]
        public void CalculateRedeployTime_RejectsNegativeSlotIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                SquadRedeployCalculator.CalculateRedeployTime(-1, 0, false, false));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                SquadRedeployCalculator.CalculateRedeployTime(0, -1, false, false));
        }
    }
}
