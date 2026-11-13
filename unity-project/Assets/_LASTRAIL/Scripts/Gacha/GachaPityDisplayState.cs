using System;

namespace LastRail.Gacha
{
    public sealed class GachaPityDisplayState
    {
        public const int DefaultEliteGuaranteeThreshold = 30;
        public const int DefaultPickupEliteGuaranteeThreshold = 60;
        public const int DefaultRareOrHigherGuaranteeThreshold = 10;

        public GachaPityDisplayState(
            string bannerId,
            int totalPullCount,
            int elitePityCount,
            int pickupElitePityCount,
            int rareOrHigherPityCount)
        {
            RequireText(bannerId, nameof(bannerId));
            RequireNonNegative(totalPullCount, nameof(totalPullCount));
            RequireNonNegative(elitePityCount, nameof(elitePityCount));
            RequireNonNegative(pickupElitePityCount, nameof(pickupElitePityCount));
            RequireNonNegative(rareOrHigherPityCount, nameof(rareOrHigherPityCount));

            BannerId = bannerId;
            TotalPullCount = totalPullCount;
            ElitePityCount = elitePityCount;
            PickupElitePityCount = pickupElitePityCount;
            RareOrHigherPityCount = rareOrHigherPityCount;
        }

        public string BannerId { get; }
        public int TotalPullCount { get; }
        public int ElitePityCount { get; }
        public int PickupElitePityCount { get; }
        public int RareOrHigherPityCount { get; }
        public int EliteGuaranteeThreshold => DefaultEliteGuaranteeThreshold;
        public int PickupEliteGuaranteeThreshold => DefaultPickupEliteGuaranteeThreshold;
        public int RareOrHigherGuaranteeThreshold => DefaultRareOrHigherGuaranteeThreshold;
        public int PullsUntilEliteGuarantee => Remaining(DefaultEliteGuaranteeThreshold, ElitePityCount);
        public int PullsUntilPickupEliteGuarantee => Remaining(DefaultPickupEliteGuaranteeThreshold, PickupElitePityCount);
        public int PullsUntilRareOrHigherGuarantee => Remaining(DefaultRareOrHigherGuaranteeThreshold, RareOrHigherPityCount);

        private static int Remaining(int threshold, int currentCount)
        {
            return Math.Max(0, threshold - currentCount);
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static void RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentException("Value cannot be negative.", name);
            }
        }
    }
}
