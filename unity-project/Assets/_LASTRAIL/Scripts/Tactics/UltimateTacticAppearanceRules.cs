using System;

namespace LastRail.Tactics
{
    public static class UltimateTacticAppearanceRules
    {
        public const int AppearanceChancePercent = 10;

        public static bool ShouldOfferUltimateTactic(bool conditionMet, int rollPercent)
        {
            if (rollPercent < 0 || rollPercent >= 100)
            {
                throw new ArgumentOutOfRangeException(nameof(rollPercent), rollPercent, "Roll percent must be between 0 and 99.");
            }

            return conditionMet && rollPercent < AppearanceChancePercent;
        }
    }
}
