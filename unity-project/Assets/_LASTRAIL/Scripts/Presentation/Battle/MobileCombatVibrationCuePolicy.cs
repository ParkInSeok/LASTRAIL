namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatVibrationCuePolicy
    {
        private MobileCombatVibrationCuePolicy(
            bool isEnabledByDefault,
            string lightHitCueId,
            string heavyHitCueId,
            string skillCueId,
            string ultimateCueId,
            string bossPhaseCueId)
        {
            IsEnabledByDefault = isEnabledByDefault;
            LightHitCueId = lightHitCueId;
            HeavyHitCueId = heavyHitCueId;
            SkillCueId = skillCueId;
            UltimateCueId = ultimateCueId;
            BossPhaseCueId = bossPhaseCueId;
        }

        public bool IsEnabledByDefault { get; }
        public string LightHitCueId { get; }
        public string HeavyHitCueId { get; }
        public string SkillCueId { get; }
        public string UltimateCueId { get; }
        public string BossPhaseCueId { get; }

        public static MobileCombatVibrationCuePolicy LaunchDefault()
        {
            return new MobileCombatVibrationCuePolicy(
                isEnabledByDefault: true,
                lightHitCueId: "vibration_light_hit",
                heavyHitCueId: "vibration_heavy_hit",
                skillCueId: "vibration_skill_ready",
                ultimateCueId: "vibration_ultimate_ready",
                bossPhaseCueId: "vibration_boss_phase");
        }
    }
}
