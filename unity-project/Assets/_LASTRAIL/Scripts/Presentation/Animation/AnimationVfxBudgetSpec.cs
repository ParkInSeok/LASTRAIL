using System;

namespace LastRail.Presentation.Animation
{
    public sealed class AnimationVfxBudgetSpec
    {
        private AnimationVfxBudgetSpec(
            int largeVfxConcurrentLimit,
            int hitSparkConcurrentMin,
            int hitSparkConcurrentMax,
            float fullScreenFlashMaxSeconds,
            float bossPhaseVfxMinSeconds,
            float bossPhaseVfxMaxSeconds,
            int screenShakeTierCount,
            bool hudReadabilityFirst)
        {
            LargeVfxConcurrentLimit = RequirePositive(largeVfxConcurrentLimit, nameof(largeVfxConcurrentLimit));
            HitSparkConcurrentMin = RequirePositive(hitSparkConcurrentMin, nameof(hitSparkConcurrentMin));
            HitSparkConcurrentMax = RequireAtLeast(hitSparkConcurrentMax, HitSparkConcurrentMin, nameof(hitSparkConcurrentMax));
            FullScreenFlashMaxSeconds = RequirePositive(fullScreenFlashMaxSeconds, nameof(fullScreenFlashMaxSeconds));
            BossPhaseVfxMinSeconds = RequirePositive(bossPhaseVfxMinSeconds, nameof(bossPhaseVfxMinSeconds));
            BossPhaseVfxMaxSeconds = RequireAtLeast(bossPhaseVfxMaxSeconds, BossPhaseVfxMinSeconds, nameof(bossPhaseVfxMaxSeconds));
            ScreenShakeTierCount = RequirePositive(screenShakeTierCount, nameof(screenShakeTierCount));
            HudReadabilityFirst = hudReadabilityFirst;
        }

        public int LargeVfxConcurrentLimit { get; }
        public int HitSparkConcurrentMin { get; }
        public int HitSparkConcurrentMax { get; }
        public float FullScreenFlashMaxSeconds { get; }
        public float BossPhaseVfxMinSeconds { get; }
        public float BossPhaseVfxMaxSeconds { get; }
        public int ScreenShakeTierCount { get; }
        public bool HudReadabilityFirst { get; }

        public static AnimationVfxBudgetSpec ActOneLaunchRecommended()
        {
            return new AnimationVfxBudgetSpec(
                largeVfxConcurrentLimit: 2,
                hitSparkConcurrentMin: 8,
                hitSparkConcurrentMax: 12,
                fullScreenFlashMaxSeconds: 0.15f,
                bossPhaseVfxMinSeconds: 1.0f,
                bossPhaseVfxMaxSeconds: 2.0f,
                screenShakeTierCount: 3,
                hudReadabilityFirst: true);
        }

        private static int RequirePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name, "Value must be positive.");
            }

            return value;
        }

        private static float RequirePositive(float value, string name)
        {
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Value must be positive.");
            }

            return value;
        }

        private static int RequireAtLeast(int value, int minimum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentOutOfRangeException(name, "Value must be greater than or equal to minimum.");
            }

            return value;
        }

        private static float RequireAtLeast(float value, float minimum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentOutOfRangeException(name, "Value must be greater than or equal to minimum.");
            }

            return value;
        }
    }
}
