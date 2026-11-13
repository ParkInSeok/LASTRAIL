using NUnit.Framework;
using LastRail.Core.Simulation;

namespace LastRail.Tests.EditMode
{
    public sealed class SimulationClockTests
    {
        [Test]
        public void Tick_AdvancesSimulationTimeByRequestedSpeedWhenNotPaused()
        {
            var clock = new SimulationClock();
            clock.SetRequestedSpeed(SimulationSpeed.Double);

            var delta = clock.Tick(0.5f);

            Assert.That(delta, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(clock.SimulationTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(clock.RequestedSpeed, Is.EqualTo(SimulationSpeed.Double));
            Assert.That(clock.EffectiveSpeedMultiplier, Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void Tick_ReturnsZeroWhilePausedAndKeepsRequestedSpeed()
        {
            var clock = new SimulationClock();
            clock.SetRequestedSpeed(SimulationSpeed.OnePointFive);
            clock.SetPaused(true);

            var delta = clock.Tick(2.0f);

            Assert.That(delta, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(clock.SimulationTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(clock.RequestedSpeed, Is.EqualTo(SimulationSpeed.OnePointFive));
            Assert.That(clock.EffectiveSpeedMultiplier, Is.EqualTo(0.0f).Within(0.0001f));

            clock.SetPaused(false);
            Assert.That(clock.Tick(1.0f), Is.EqualTo(1.5f).Within(0.0001f));
        }
    }
}
