namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatHudReadabilityPolicy
    {
        private MobileCombatHudReadabilityPolicy(
            bool preventsTextOverflow,
            bool keepsHudAboveAnimationLayer,
            bool preservesBattlefieldVisibility,
            bool preservesCurrentCarTacticalVisibility)
        {
            PreventsTextOverflow = preventsTextOverflow;
            KeepsHudAboveAnimationLayer = keepsHudAboveAnimationLayer;
            PreservesBattlefieldVisibility = preservesBattlefieldVisibility;
            PreservesCurrentCarTacticalVisibility = preservesCurrentCarTacticalVisibility;
        }

        public bool PreventsTextOverflow { get; }
        public bool KeepsHudAboveAnimationLayer { get; }
        public bool PreservesBattlefieldVisibility { get; }
        public bool PreservesCurrentCarTacticalVisibility { get; }

        public static MobileCombatHudReadabilityPolicy LaunchDefault()
        {
            return new MobileCombatHudReadabilityPolicy(
                preventsTextOverflow: true,
                keepsHudAboveAnimationLayer: true,
                preservesBattlefieldVisibility: true,
                preservesCurrentCarTacticalVisibility: true);
        }
    }
}
