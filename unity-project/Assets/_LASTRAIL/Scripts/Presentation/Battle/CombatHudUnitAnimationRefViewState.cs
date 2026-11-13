using System;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatHudUnitAnimationRefViewState
    {
        public CombatHudUnitAnimationRefViewState(CombatUnitAnimationState source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            UnitId = source.UnitId;
            UnitRole = source.UnitRole;
            AnimationKey = source.AnimationKey;
            AnimationCueId = source.AnimationCueId;
        }

        public string UnitId { get; }
        public CombatUnitRole UnitRole { get; }
        public string AnimationKey { get; }
        public string AnimationCueId { get; }
        public bool IsPresentationOnly => true;
        public bool HasAnimationTimelinePayload => false;
        public bool HasFramePayload => false;
    }
}
