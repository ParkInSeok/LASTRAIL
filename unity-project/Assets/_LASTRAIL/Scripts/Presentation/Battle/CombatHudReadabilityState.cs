namespace LastRail.Presentation.Battle
{
    public sealed class CombatHudReadabilityState
    {
        private CombatHudReadabilityState(
            bool hasSeparateHudLayer,
            bool keepsTouchControlsReadable,
            bool prioritizesReadableCombatText)
        {
            HasSeparateHudLayer = hasSeparateHudLayer;
            KeepsTouchControlsReadable = keepsTouchControlsReadable;
            PrioritizesReadableCombatText = prioritizesReadableCombatText;
        }

        public bool HasSeparateHudLayer { get; }
        public bool KeepsTouchControlsReadable { get; }
        public bool PrioritizesReadableCombatText { get; }

        public static CombatHudReadabilityState LaunchDefault()
        {
            return new CombatHudReadabilityState(
                hasSeparateHudLayer: true,
                keepsTouchControlsReadable: true,
                prioritizesReadableCombatText: true);
        }
    }
}
