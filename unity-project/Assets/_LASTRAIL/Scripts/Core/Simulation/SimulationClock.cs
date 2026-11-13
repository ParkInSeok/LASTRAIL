using System;

namespace LastRail.Core.Simulation
{
    public sealed class SimulationClock
    {
        private SimulationSpeed requestedSpeed = SimulationSpeed.Normal;
        private bool isPaused;

        public float SimulationTime { get; private set; }

        public SimulationSpeed RequestedSpeed => requestedSpeed;

        public bool IsPaused => isPaused;

        public float EffectiveSpeedMultiplier => isPaused ? 0f : ToMultiplier(requestedSpeed);

        public void SetRequestedSpeed(SimulationSpeed speed)
        {
            if (!Enum.IsDefined(typeof(SimulationSpeed), speed))
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unsupported simulation speed.");
            }

            requestedSpeed = speed;
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public float Tick(float realDeltaSeconds)
        {
            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Delta time cannot be negative.");
            }

            var simulationDeltaSeconds = realDeltaSeconds * EffectiveSpeedMultiplier;
            SimulationTime += simulationDeltaSeconds;
            return simulationDeltaSeconds;
        }

        private static float ToMultiplier(SimulationSpeed speed)
        {
            return speed switch
            {
                SimulationSpeed.Normal => 1f,
                SimulationSpeed.OnePointFive => 1.5f,
                SimulationSpeed.Double => 2f,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unsupported simulation speed.")
            };
        }
    }
}
