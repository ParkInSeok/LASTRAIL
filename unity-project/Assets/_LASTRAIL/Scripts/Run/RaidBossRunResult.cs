using System;

namespace LastRail.Run
{
    public sealed class RaidBossRunResult
    {
        public RaidBossRunResult(
            string raidId,
            string bossId,
            bool bossDefeated,
            int completedPhaseCount,
            int encounterDurationSeconds)
        {
            RequireText(raidId, nameof(raidId));
            RequireText(bossId, nameof(bossId));

            if (completedPhaseCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedPhaseCount), "Completed phase count cannot be negative.");
            }

            if (encounterDurationSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterDurationSeconds), "Encounter duration cannot be negative.");
            }

            RaidId = raidId;
            BossId = bossId;
            BossDefeated = bossDefeated;
            CompletedPhaseCount = completedPhaseCount;
            EncounterDurationSeconds = encounterDurationSeconds;
        }

        public string RaidId { get; }
        public string BossId { get; }
        public bool BossDefeated { get; }
        public int CompletedPhaseCount { get; }
        public int EncounterDurationSeconds { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
