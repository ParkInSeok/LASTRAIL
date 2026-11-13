using System;

namespace LastRail.Train
{
    public static class TrainSpeedControlCalculator
    {
        public const float CruiseMultiplier = 1.00f;
        public const float LowMultiplier = 0.70f;
        public const float EmergencyAccelerationMultiplier = 1.35f;

        public static float CalculateCurrentSpeedIndex(
            float baseSpeedIndex,
            TrainSpeedControlMode controlMode,
            bool emergencyAccelerationActive)
        {
            if (baseSpeedIndex < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSpeedIndex), baseSpeedIndex, "Base speed index cannot be negative.");
            }

            var currentSpeed = baseSpeedIndex * MultiplierFor(controlMode);
            if (emergencyAccelerationActive)
            {
                currentSpeed *= EmergencyAccelerationMultiplier;
            }

            return Math.Min(currentSpeed, TrainSpeedCalculator.TemporaryCurrentSpeedHardCap);
        }

        private static float MultiplierFor(TrainSpeedControlMode controlMode)
        {
            switch (controlMode)
            {
                case TrainSpeedControlMode.Cruise:
                    return CruiseMultiplier;
                case TrainSpeedControlMode.Low:
                    return LowMultiplier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controlMode), controlMode, "Unsupported train speed control mode.");
            }
        }
    }
}
