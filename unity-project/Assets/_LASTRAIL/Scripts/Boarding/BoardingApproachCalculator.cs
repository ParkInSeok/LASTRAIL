using System;

namespace LastRail.Boarding
{
    public static class BoardingApproachCalculator
    {
        public const float MinimumSpeedEffect = 0.75f;
        public const float MaximumSpeedEffect = 1.25f;

        public static float CalculateFinalApproachTime(float baseApproachTimeSeconds, float currentSpeedIndex)
        {
            if (baseApproachTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseApproachTimeSeconds), baseApproachTimeSeconds, "Base approach time cannot be negative.");
            }

            var speedEffect = Clamp(currentSpeedIndex, MinimumSpeedEffect, MaximumSpeedEffect);
            return baseApproachTimeSeconds * speedEffect;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
