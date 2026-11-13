using System;

namespace LastRail.Train
{
    public static class PursuitPressureCalculator
    {
        public static float CalculatePressureMultiplier(float currentSpeedIndex)
        {
            if (currentSpeedIndex < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(currentSpeedIndex), currentSpeedIndex, "Current speed index cannot be negative.");
            }

            if (currentSpeedIndex >= 1.20f)
            {
                return 0.70f;
            }

            if (currentSpeedIndex >= 1.10f)
            {
                return 0.85f;
            }

            if (currentSpeedIndex >= 1.00f)
            {
                return 1.00f;
            }

            if (currentSpeedIndex >= 0.85f)
            {
                return 1.15f;
            }

            return 1.30f;
        }
    }
}
