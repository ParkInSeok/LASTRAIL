using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunStartResponseDto
    {
        public RunStartResponseDto(
            string runId,
            string balanceVersion,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            int riskLevel,
            string wavePlanId,
            uint seed,
            string rewardTableId,
            DateTime serverStartedAt,
            bool firstClearEligible,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null,
            string operationPreviewId = null,
            string contentVersion = null,
            RunEncounterType encounterType = RunEncounterType.Ordinary15Wave,
            int? ordinaryTotalWaves = null,
            string raidId = null,
            string bossId = null,
            IReadOnlyList<string> bossPhaseRefs = null,
            IReadOnlyList<string> allowedOperatorSlots = null)
        {
            RequireText(runId, nameof(runId));
            RequireText(balanceVersion, nameof(balanceVersion));
            RequireText(operationId, nameof(operationId));
            RequireText(actId, nameof(actId));
            RequireText(regionId, nameof(regionId));
            RequireText(enemyFactionId, nameof(enemyFactionId));
            RequireText(wavePlanId, nameof(wavePlanId));
            RequireText(rewardTableId, nameof(rewardTableId));

            RunId = runId;
            BalanceVersion = balanceVersion;
            OperationId = operationId;
            ActId = actId;
            RegionId = regionId;
            EnemyFactionId = enemyFactionId;
            RiskLevel = riskLevel;
            WavePlanId = wavePlanId;
            Seed = seed;
            RewardTableId = rewardTableId;
            ServerStartedAt = serverStartedAt;
            FirstClearEligible = firstClearEligible;
            StartAugmentPackageRef = NormalizeOptional(startAugmentPackageRef);
            StartTacticPackageRef = NormalizeOptional(startTacticPackageRef);
            OperationPreviewId = NormalizeOptional(operationPreviewId);
            ContentVersion = NormalizeOptional(contentVersion);
            EncounterType = encounterType;
            OrdinaryTotalWaves = ordinaryTotalWaves;
            RaidId = NormalizeOptional(raidId);
            BossId = NormalizeOptional(bossId);
            BossPhaseRefs = CopyOptionalStrings(bossPhaseRefs, nameof(bossPhaseRefs));
            AllowedOperatorSlots = CopyOptionalStrings(allowedOperatorSlots, nameof(allowedOperatorSlots));
        }

        public string RunId { get; }
        public string BalanceVersion { get; }
        public string OperationId { get; }
        public string ActId { get; }
        public string RegionId { get; }
        public string EnemyFactionId { get; }
        public int RiskLevel { get; }
        public string WavePlanId { get; }
        public uint Seed { get; }
        public string RewardTableId { get; }
        public DateTime ServerStartedAt { get; }
        public bool FirstClearEligible { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }
        public string OperationPreviewId { get; }
        public string ContentVersion { get; }
        public RunEncounterType EncounterType { get; }
        public int? OrdinaryTotalWaves { get; }
        public string RaidId { get; }
        public string BossId { get; }
        public IReadOnlyList<string> BossPhaseRefs { get; }
        public IReadOnlyList<string> AllowedOperatorSlots { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static IReadOnlyList<string> CopyOptionalStrings(IReadOnlyList<string> values, string name)
        {
            if (values == null)
            {
                return Array.Empty<string>();
            }

            var copy = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    throw new ArgumentException("Value cannot be empty.", name);
                }

                copy[i] = values[i].Trim();
            }

            return copy;
        }
    }
}
