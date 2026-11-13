using System;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatUnitAnimationState
    {
        public CombatUnitAnimationState(
            string unitId,
            CombatUnitRole unitRole,
            string animationKey,
            string animationCueId)
        {
            UnitId = RequireText(unitId, nameof(unitId));
            UnitRole = unitRole;
            AnimationKey = RequireText(animationKey, nameof(animationKey));
            AnimationCueId = RequireText(animationCueId, nameof(animationCueId));
        }

        public string UnitId { get; }
        public CombatUnitRole UnitRole { get; }
        public string AnimationKey { get; }
        public string AnimationCueId { get; }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
