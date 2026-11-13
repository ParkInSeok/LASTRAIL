using System;

namespace LastRail.Train
{
    public static class TrainSpeedCalculator
    {
        public const float MinimumBaseSpeedIndex = 0.75f;
        public const float MaximumBaseSpeedIndex = 1.20f;
        public const float TemporaryCurrentSpeedHardCap = 1.50f;

        public static float CalculateBaseSpeedIndex(int engineOutput, int totalWeight)
        {
            var unclamped = 1f + ((engineOutput - totalWeight) * 0.01f);
            return Clamp(unclamped, MinimumBaseSpeedIndex, MaximumBaseSpeedIndex);
        }

        public static TrainSpeedState Classify(float speedIndex)
        {
            if (speedIndex < 0.85f)
            {
                return TrainSpeedState.Heavy;
            }

            if (speedIndex < 0.95f)
            {
                return TrainSpeedState.Slow;
            }

            if (speedIndex < 1.05f)
            {
                return TrainSpeedState.Standard;
            }

            if (speedIndex < 1.15f)
            {
                return TrainSpeedState.Fast;
            }

            return TrainSpeedState.HighMobility;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
