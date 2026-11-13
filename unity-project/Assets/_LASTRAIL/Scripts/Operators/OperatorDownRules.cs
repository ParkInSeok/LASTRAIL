using System;

namespace LastRail.Operators
{
    public static class OperatorDownRules
    {
        public const float FirstDownRecoveryDelaySeconds = 12.0f;
        public const float FirstDownRecoveryHpRatio = 0.25f;
        public const float SecondDownRecoveryDelaySeconds = 20.0f;
        public const float SecondDownRecoveryHpRatio = 0.20f;

        public static OperatorDownRecoveryPlan GetRecoveryPlan(int downCountThisWave)
        {
            if (downCountThisWave <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(downCountThisWave), downCountThisWave, "Down count must start at one.");
            }

            if (downCountThisWave == 1)
            {
                return new OperatorDownRecoveryPlan(
                    OperatorDownState.DownRecovering,
                    FirstDownRecoveryDelaySeconds,
                    FirstDownRecoveryHpRatio);
            }

            if (downCountThisWave == 2)
            {
                return new OperatorDownRecoveryPlan(
                    OperatorDownState.DownRecovering,
                    SecondDownRecoveryDelaySeconds,
                    SecondDownRecoveryHpRatio);
            }

            return new OperatorDownRecoveryPlan(OperatorDownState.WaveIncapacitated, 0f, 0f);
        }
    }
}
