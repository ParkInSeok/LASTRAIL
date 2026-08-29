using System;

namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatHudBossState
    {
        private MobileCombatHudBossState(string bossHudKey, string bossId, bool hasBossPhase, int? bossPhase)
        {
            BossHudKey = RequireText(bossHudKey, nameof(bossHudKey));
            BossId = OptionalText(bossId);
            HasBossPhase = hasBossPhase;
            BossPhase = bossPhase;
        }

        public string BossHudKey { get; }
        public string BossId { get; }
        public bool HasBossPhase { get; }
        public int? BossPhase { get; }

        public static MobileCombatHudBossState HiddenForOrdinary()
        {
            return new MobileCombatHudBossState("boss_hud_hidden", null, false, null);
        }

        public static MobileCombatHudBossState RaidBoss(string bossId, int bossPhase)
        {
            if (bossPhase < 1)
            {
                throw new ArgumentException("Boss phase must be positive.", nameof(bossPhase));
            }

            return new MobileCombatHudBossState("boss_only_raid_hud", RequireText(bossId, nameof(bossId)), true, bossPhase);
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
