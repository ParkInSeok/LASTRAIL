using System;

namespace LastRail.Gacha
{
    public sealed class GachaPostPityStateDto
    {
        public GachaPostPityStateDto(
            int totalPullCount,
            int elitePityCount,
            int pickupElitePityCount,
            int rareOrHigherPityCount,
            int revision)
        {
            RequireNonNegative(totalPullCount, nameof(totalPullCount));
            RequireNonNegative(elitePityCount, nameof(elitePityCount));
            RequireNonNegative(pickupElitePityCount, nameof(pickupElitePityCount));
            RequireNonNegative(rareOrHigherPityCount, nameof(rareOrHigherPityCount));
            RequireNonNegative(revision, nameof(revision));

            TotalPullCount = totalPullCount;
            ElitePityCount = elitePityCount;
            PickupElitePityCount = pickupElitePityCount;
            RareOrHigherPityCount = rareOrHigherPityCount;
            Revision = revision;
        }

        public int TotalPullCount { get; }
        public int ElitePityCount { get; }
        public int PickupElitePityCount { get; }
        public int RareOrHigherPityCount { get; }
        public int Revision { get; }

        private static void RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentException("Value cannot be negative.", name);
            }
        }
    }
}
