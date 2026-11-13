using NUnit.Framework;
using LastRail.Battle;
using LastRail.Core.Simulation;

namespace LastRail.Tests.EditMode
{
    public sealed class BattleSessionTests
    {
        [Test]
        public void Tick_DispatchesSimulationDeltaFromClockToRegisteredSystems()
        {
            var clock = new SimulationClock();
            clock.SetRequestedSpeed(SimulationSpeed.OnePointFive);
            var session = new BattleSession(clock);
            var system = new RecordingBattleSystem();
            session.RegisterSystem(system);

            var delta = session.Tick(2.0f);

            Assert.That(delta, Is.EqualTo(3.0f).Within(0.0001f));
            Assert.That(system.TickCount, Is.EqualTo(1));
            Assert.That(system.LastDelta, Is.EqualTo(3.0f).Within(0.0001f));
        }

        private sealed class RecordingBattleSystem : IBattleSystem
        {
            public int TickCount { get; private set; }
            public float LastDelta { get; private set; }

            public void Tick(float simulationDeltaSeconds)
            {
                TickCount++;
                LastDelta = simulationDeltaSeconds;
            }
        }
    }
}
