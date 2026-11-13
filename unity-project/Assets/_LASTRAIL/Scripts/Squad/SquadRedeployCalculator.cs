using System;

namespace LastRail.Squad
{
    public static class SquadRedeployCalculator
    {
        public const float SecondsPerAdjacentCarriage = 1.5f;
        public const float CombatCarFirstSegmentMultiplier = 0.75f;
        public const float DisabledCombatCarMultiplier = 1.25f;

        public static float CalculateRedeployTime(
            int fromSlotIndex,
            int toSlotIndex,
            bool startsFromCombatCar,
            bool combatCarDisabled)
        {
            if (fromSlotIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fromSlotIndex), fromSlotIndex, "Source slot index cannot be negative.");
            }

            if (toSlotIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toSlotIndex), toSlotIndex, "Target slot index cannot be negative.");
            }

            var segmentCount = Math.Abs(toSlotIndex - fromSlotIndex);
            if (segmentCount == 0)
            {
                return 0f;
            }

            var firstSegmentTime = SecondsPerAdjacentCarriage;
            if (startsFromCombatCar)
            {
                firstSegmentTime *= CombatCarFirstSegmentMultiplier;
            }

            var totalTime = firstSegmentTime + ((segmentCount - 1) * SecondsPerAdjacentCarriage);
            if (combatCarDisabled)
            {
                totalTime *= DisabledCombatCarMultiplier;
            }

            return totalTime;
        }
    }
}
