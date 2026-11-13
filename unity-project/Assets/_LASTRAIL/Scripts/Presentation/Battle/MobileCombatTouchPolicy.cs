namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatTouchPolicy
    {
        private MobileCombatTouchPolicy(
            int minimumTouchTargetDp,
            bool requiresSafeAreaPadding,
            bool supportsBottomTacticalGestures,
            bool keepsCriticalActionsAwayFromInsets)
        {
            MinimumTouchTargetDp = minimumTouchTargetDp;
            RequiresSafeAreaPadding = requiresSafeAreaPadding;
            SupportsBottomTacticalGestures = supportsBottomTacticalGestures;
            KeepsCriticalActionsAwayFromInsets = keepsCriticalActionsAwayFromInsets;
        }

        public int MinimumTouchTargetDp { get; }
        public bool RequiresSafeAreaPadding { get; }
        public bool SupportsBottomTacticalGestures { get; }
        public bool KeepsCriticalActionsAwayFromInsets { get; }

        public static MobileCombatTouchPolicy LaunchDefault()
        {
            return new MobileCombatTouchPolicy(
                minimumTouchTargetDp: 48,
                requiresSafeAreaPadding: true,
                supportsBottomTacticalGestures: true,
                keepsCriticalActionsAwayFromInsets: true);
        }
    }
}
