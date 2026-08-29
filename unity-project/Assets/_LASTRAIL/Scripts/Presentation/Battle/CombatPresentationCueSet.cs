using System;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatPresentationCueSet
    {
        private CombatPresentationCueSet(
            string damageCueId,
            string hitFlashCueId,
            string hitStopCueId,
            string screenShakeCueId,
            string vfxCueId,
            string sfxCueId,
            string vibrationCueId,
            string waveTransitionCueId,
            string bossPhaseTransitionCueId)
        {
            DamageCueId = RequireText(damageCueId, nameof(damageCueId));
            HitFlashCueId = RequireText(hitFlashCueId, nameof(hitFlashCueId));
            HitStopCueId = RequireText(hitStopCueId, nameof(hitStopCueId));
            ScreenShakeCueId = RequireText(screenShakeCueId, nameof(screenShakeCueId));
            VfxCueId = RequireText(vfxCueId, nameof(vfxCueId));
            SfxCueId = RequireText(sfxCueId, nameof(sfxCueId));
            VibrationCueId = RequireText(vibrationCueId, nameof(vibrationCueId));
            WaveTransitionCueId = NormalizeOptional(waveTransitionCueId);
            BossPhaseTransitionCueId = NormalizeOptional(bossPhaseTransitionCueId);
        }

        public string DamageCueId { get; }
        public string HitFlashCueId { get; }
        public string HitStopCueId { get; }
        public string ScreenShakeCueId { get; }
        public string VfxCueId { get; }
        public string SfxCueId { get; }
        public string VibrationCueId { get; }
        public string WaveTransitionCueId { get; }
        public string BossPhaseTransitionCueId { get; }

        public static CombatPresentationCueSet ForOrdinaryWave(
            string damageCueId,
            string hitFlashCueId,
            string hitStopCueId,
            string screenShakeCueId,
            string vfxCueId,
            string sfxCueId,
            string vibrationCueId,
            string waveTransitionCueId)
        {
            return new CombatPresentationCueSet(
                damageCueId,
                hitFlashCueId,
                hitStopCueId,
                screenShakeCueId,
                vfxCueId,
                sfxCueId,
                vibrationCueId,
                RequireText(waveTransitionCueId, nameof(waveTransitionCueId)),
                null);
        }

        public static CombatPresentationCueSet ForRaidBossPhase(
            string damageCueId,
            string hitFlashCueId,
            string hitStopCueId,
            string screenShakeCueId,
            string vfxCueId,
            string sfxCueId,
            string vibrationCueId,
            string bossPhaseTransitionCueId)
        {
            return new CombatPresentationCueSet(
                damageCueId,
                hitFlashCueId,
                hitStopCueId,
                screenShakeCueId,
                vfxCueId,
                sfxCueId,
                vibrationCueId,
                null,
                RequireText(bossPhaseTransitionCueId, nameof(bossPhaseTransitionCueId)));
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
