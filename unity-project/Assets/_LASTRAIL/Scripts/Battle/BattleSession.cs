using System;
using System.Collections.Generic;
using LastRail.Core.Simulation;

namespace LastRail.Battle
{
    public sealed class BattleSession
    {
        private readonly List<IBattleSystem> systems = new List<IBattleSystem>(16);

        public BattleSession(SimulationClock clock)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public SimulationClock Clock { get; }

        public void RegisterSystem(IBattleSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            systems.Add(system);
        }

        public float Tick(float realDeltaSeconds)
        {
            var simulationDeltaSeconds = Clock.Tick(realDeltaSeconds);
            if (simulationDeltaSeconds <= 0f)
            {
                return simulationDeltaSeconds;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                systems[i].Tick(simulationDeltaSeconds);
            }

            return simulationDeltaSeconds;
        }
    }
}
